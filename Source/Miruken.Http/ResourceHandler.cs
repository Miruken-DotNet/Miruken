namespace Miruken.Http
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Callback;
    using Functional;
    using Infrastructure;
    using Newtonsoft.Json;

    public abstract class ResourceHandler : Handler
    {
        private const string KeepResponseOpen = "Miruken.Http.KeepOpen";

        protected HttpContent GetContent<TRequest>(
            TRequest content, ResourceRequest request)
        {
            var formatter = request.Formatter;
            switch (content)
            {
                case string stringContent:
                {
                    var sc = new StringContent(stringContent);
                    if (formatter != null)
                        sc.Headers.ContentType = formatter.SupportedMediaTypes.First();
                    return sc;
                }
                case Stream streamContent:
                {
                    var sc = new StreamContent(streamContent);
                    if (formatter != null)
                        sc.Headers.ContentType = formatter.SupportedMediaTypes.First();
                    return sc;
                }
                case IEnumerable<KeyValuePair<string, string>> nameValues:
                    return new FormUrlEncodedContent(nameValues);
                case byte[] bytes:
                    return new ByteArrayContent(bytes);
            }
            return new ObjectContent<TRequest>(content, formatter ?? HttpFormatters.Json);
        }

        protected static HttpRequestMessage GetRequest(
            object request, HttpMethod method)
        {
            var httpRequest = request as HttpRequestMessage;
            if (httpRequest != null && httpRequest.Method != method)
                throw new ArgumentException(
                    $"Expected HttpRequestMessage with {method} method but got {httpRequest.Method}");
            return httpRequest;
        }

        protected async Task<TResource> ExtractResource<TResource>(
            HttpResponseMessage response, HttpOptions options)
        {
            try
            {
                var result = ReadResponse<TResource>(response, options, true);
                if (result == null)
                {
                    response.EnsureSuccessStatusCode();
                    return default;
                }
                return await result;
            }
            finally
            {
                var properties = response.RequestMessage.Properties;
                if (properties.TryGetValue(KeepResponseOpen, out var dispose))
                    properties.Remove(KeepResponseOpen);
                if (!Equals(dispose, true))
                    response.Dispose();
            }
        }

        protected async Task<Either<TL, TR>> ReadEither<TL, TR>(
            HttpResponseMessage response, HttpOptions options, bool success)
        {
            var content = response.Content;
            await content.LoadIntoBufferAsync();

            try
            {
                var left = ReadResponse<TL>(response, options, success);
                if (left != null) return await left;
            }
            catch
            {
                await RewindHttpContent(content);
            }

            var right = ReadResponse<TR>(response, options, success);
            return right != null ? await right : (Either<TL, TR>)null;
        }

        protected async Task<Tuple<T1, T2>> ReadTuple<T1, T2>(
             HttpResponseMessage response, HttpOptions options, bool success)
        {
            var content = response.Content;
            await content.LoadIntoBufferAsync();

            var t1 = ReadResponse<T1>(response, options, success);
            if (t1 == null) return null;

            await RewindHttpContent(content);
            var t2 = ReadResponse<T2>(response, options, success);

            return t2 != null ? Tuple.Create(await t1, await t2) : null;
        }

        protected async Task<Try<TE, TR>> ReadTry<TE, TR>(
            HttpResponseMessage response, HttpOptions options, bool success)
        {
            if (response.IsSuccessStatusCode)
                return await ReadResponse<TR>(response, options, true);

            if (!response.IsSuccessStatusCode && typeof(TE) == typeof(Exception))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    return new Try<Exception, TR>(ex) as Try<TE, TR>;
                }
            }

            var left = ReadResponse<TE>(response, options, false);
            if (left != null)
            {
                var error = await left;
                if (error == null)
                    response.EnsureSuccessStatusCode();
                return error;
            }
            return null;
        }

        private Task<TResponse> ReadResponse<TResponse>(
            HttpResponseMessage response, HttpOptions options, bool success)
        {
            if (typeof(TResponse) == typeof(HttpResponseMessage))
            {
                if (response.IsSuccessStatusCode == success)
                {
                    response.RequestMessage.Properties[KeepResponseOpen] = true;
                    return Task.FromResult(response) as Task<TResponse>;
                }
                return null;
            }

            var content      = response.Content;
            var readerMethod = GetReaderMethod(typeof(TResponse));
            if (readerMethod != null)
            {
                var read = Readers.GetOrAdd(typeof(TResponse), either =>
                {
                    var args   = either.GetGenericArguments();
                    var method = readerMethod.MakeGenericMethod(args);
                    return (Func<object, object, object, object, object>)
                        RuntimeHelper.CompileMethod(method,
                        typeof(Func<object, object, object, object, object>));
                });
                return (Task<TResponse>)read(this, response, options, success);
            }

            if (response.IsSuccessStatusCode != success)
                return null;

            var responseType = typeof(TResponse);

            if (responseType == typeof(string))
                return content.ReadAsStringAsync() as Task<TResponse>;
            if (responseType == typeof(Stream))
                return content.ReadAsStreamAsync() as Task<TResponse>;
            if (responseType == typeof(byte[]))
                return content.ReadAsByteArrayAsync() as Task<TResponse>;
            if (responseType.IsSimpleType())
            {
                return content.ReadAsStringAsync().ContinueWith(
                    t => JsonConvert.DeserializeObject<TResponse>(t.Result));
            }

            var formatters = options?.Formatters ?? HttpFormatters.Default;
            return content.ReadAsAsync<TResponse>(formatters);
        }

        private static async Task RewindHttpContent(HttpContent content)
        {
            var stream = await content.ReadAsStreamAsync();
            stream.Seek(0, SeekOrigin.Begin);
        }

        private static MethodInfo GetReaderMethod(Type resourceType)
        {
            if (!resourceType.IsGenericType) return null;
            var genericDefinition = resourceType.GetGenericTypeDefinition();
            if (genericDefinition == typeof(Either<,>))
                return ReadEitherMethod;
            if (genericDefinition == typeof(Try<,>))
                return ReadTryMethod;
            return genericDefinition == typeof(Tuple<,>)
                 ? ReadTupleMethod : null;
        }

        protected static void SetResponseUri(
            ResourceResponse response, HttpResponseMessage httpResponse)
        {
            var location = httpResponse.Headers.Location;
            if (location != null)
                response.ResourceUri = location;
        }

     
        private static readonly ConcurrentDictionary<Type,
            Func<object, object, object, object, object>> Readers
            = new ConcurrentDictionary<Type,
                Func<object, object, object, object, object>>();

        private static readonly MethodInfo ReadEitherMethod =
            typeof(ResourceHandler).GetMethod(nameof(ReadEither),
                BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo ReadTupleMethod =
            typeof(ResourceHandler).GetMethod(nameof(ReadTuple),
                BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo ReadTryMethod =
            typeof(ResourceHandler).GetMethod(nameof(ReadTry),
                BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
