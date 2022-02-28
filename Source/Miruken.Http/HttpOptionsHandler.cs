namespace Miruken.Http;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Callback;

[Unmanaged]
public class HttpOptionsHandler : DelegatingHandler
{
    [Provides]
    public HttpOptionsHandler()
    {           
    }

    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(100);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options  = request.GetOptions();
        var composer = request.GetComposer();

        using var cts = GetCancellationTokenSource(
            request, options, cancellationToken);
        var cancel = cts?.Token ?? cancellationToken;

        Task<HttpResponseMessage> SendRequest() => base.SendAsync(request, cancel);

        var pipeline = options?.Pipeline != null
            ? options.Pipeline.GetInvocationList()
                .Cast<HttpRequestPipeline>().Reverse()
                .Aggregate((Func<Task<HttpResponseMessage>>) SendRequest,
                    (next, pipe) => () => pipe(
                        request, cancel, composer, next))
            : SendRequest;

        try
        {
            return await pipeline().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException();
        }
    }

    private CancellationTokenSource GetCancellationTokenSource(
        HttpRequestMessage request, HttpOptions options,
        CancellationToken cancellationToken)
    {
        var timeout = request.GetTimeout() ?? options?.Timeout ?? DefaultTimeout;
        if (timeout == Timeout.InfiniteTimeSpan) return null;
        var cts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        return cts;
    }
}