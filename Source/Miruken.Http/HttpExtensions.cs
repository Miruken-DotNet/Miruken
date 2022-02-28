namespace Miruken.Http;

using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Api;
using Callback;
using Delete;
using Get;
using Patch;
using Post;
using Put;

public static class HttpExtensions
{
    private static readonly HttpRequestOptionsKey<TimeSpan?>   TimeoutPropertyKey  = new("Miruken.Timeout");
    private static readonly HttpRequestOptionsKey<HttpOptions> OptionsPropertyKey  = new("Miruken.HttpOption");
    private static readonly HttpRequestOptionsKey<IHandler>    ComposerPropertyKey = new("Miruken.Composer");

    public static readonly HttpMethod PatchMethod = new("PATCH");

    public static void SetTimeout(
        this HttpRequestMessage request, TimeSpan? timeout)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        request.Options.Set(TimeoutPropertyKey, timeout);
    }

    public static TimeSpan? GetTimeout(this HttpRequestMessage request)
    {
        return request == null
             ? throw new ArgumentNullException(nameof(request))
             : request.GetOption(TimeoutPropertyKey);
    }

    public static void SetOptions(
        this HttpRequestMessage request, HttpOptions options)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        request.Options.Set(OptionsPropertyKey, options);
    }

    public static HttpOptions GetOptions(this HttpRequestMessage request)
    {
        return request == null
             ? throw new ArgumentNullException(nameof(request))
             : request.GetOption(OptionsPropertyKey);
    }

    public static void SetComposer(
        this HttpRequestMessage request, IHandler composer)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        request.Options.Set(ComposerPropertyKey, composer);
    }

    public static IHandler GetComposer(this HttpRequestMessage request)
    {
        return request == null
             ? throw new ArgumentNullException(nameof(request))
             : request.GetOption(ComposerPropertyKey);
    }

    public static Task<TResponse> HttpGet<TResponse>(
        this IHandler handler, string resourceUri,
        MediaTypeFormatter formatter = null)
    {
        var get = new GetRequest<object, TResponse>
        {
            ResourceUri = resourceUri,
            Formatter   = formatter
        };
        return handler.Send(get).Then((r, _) => r.Resource);
    }

    public static Task<TResource> HttpGet<TResource>(
        this IHandler handler,
        Action<GetRequest<object, TResource>> configure = null)
    {
        var get = new GetRequest<object, TResource>();
        configure?.Invoke(get);
        return handler.Send(get).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpGet<TRequest, TResponse>(
        this IHandler handler, TRequest request,
        Action<GetRequest<TRequest, TResponse>> configure = null)
    {
        var get = new GetRequest<TRequest, TResponse>(request);
        configure?.Invoke(get);
        return handler.Send(get).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPost<TRequest, TResponse>(
        this IHandler handler, TRequest request, string resourceUri,
        MediaTypeFormatter formatter = null)
    {
        var post = new PostRequest<TRequest, TResponse>(request)
        {
            ResourceUri = resourceUri,
            Formatter   = formatter
        };
        return handler.Send(post).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPost<TRequest, TResponse>(
        this IHandler handler, TRequest request,
        Action<PostRequest<TRequest, TResponse>> configure = null)
    {
        var post = new PostRequest<TRequest, TResponse>(request);
        configure?.Invoke(post);
        return handler.Send(post).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPut<TRequest, TResponse>(
        this IHandler handler, TRequest request, string resourceUri,
        MediaTypeFormatter formatter = null)
    {
        var put = new PutRequest<TRequest, TResponse>(request)
        {
            ResourceUri = resourceUri,
            Formatter   = formatter
        };
        return handler.Send(put).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPut<TRequest, TResponse>(
        this IHandler handler, TRequest request,
        Action<PutRequest<TRequest, TResponse>> configure = null)
    {
        var put = new PutRequest<TRequest, TResponse>(request);
        configure?.Invoke(put);
        return handler.Send(put).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPatch<TRequest, TResponse>(
        this IHandler handler, TRequest request, string resourceUri,
        MediaTypeFormatter formatter = null)
    {
        var patch = new PatchRequest<TRequest, TResponse>(request)
        {
            ResourceUri = resourceUri,
            Formatter   = formatter
        };
        return handler.Send(patch).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpPatch<TRequest, TResponse>(
        this IHandler handler, TRequest request,
        Action<PatchRequest<TRequest, TResponse>> configure = null)
    {
        var patch = new PatchRequest<TRequest, TResponse>(request);
        configure?.Invoke(patch);
        return handler.Send(patch).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpDelete<TResponse>(
        this IHandler handler, string resourceUri,
        MediaTypeFormatter formatter = null)
    {
        var delete = new DeleteRequest<object, TResponse>
        {
            ResourceUri = resourceUri,
            Formatter   = formatter
        };
        return handler.Send(delete).Then((r, _) => r.Resource);
    }

    public static Task<TResource> HttpDelete<TResource>(
        this IHandler handler,
        Action<DeleteRequest<object, TResource>> configure = null)
    {
        var delete = new DeleteRequest<object, TResource>();
        configure?.Invoke(delete);
        return handler.Send(delete).Then((r, _) => r.Resource);
    }

    public static Task<TResponse> HttpDelete<TRequest, TResponse>(
        this IHandler handler, TRequest request,
        Action<DeleteRequest<TRequest, TResponse>> configure = null)
    {
        var delete = new DeleteRequest<TRequest, TResponse>(request);
        configure?.Invoke(delete);
        return handler.Send(delete).Then((r, _) => r.Resource);
    }

    private static T GetOption<T>(
        this HttpRequestMessage request, HttpRequestOptionsKey<T> key) =>
        request.Options.TryGetValue(key, out var value) ? value : default;
}