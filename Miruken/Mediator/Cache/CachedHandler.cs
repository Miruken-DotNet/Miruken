namespace Miruken.Mediator.Cache
{
    using System;
    using System.Collections.Concurrent;
    using Callback;
    using Concurrency;

    public class CachedHandler : Handler
    {
        private static readonly
            ConcurrentDictionary<object, CacheResponse> Cache
                = new ConcurrentDictionary<object, CacheResponse>();

        private struct CacheResponse
        {
            public Promise  Response;
            public DateTime LastUpdated;
        }

        [Mediates]
        public Promise<TResponse> Cached<TResponse>(
            Cached<TResponse> request, IHandler composer)
        {
            if (request.Request == null)
                return Promise<TResponse>.Empty;

            if (request.Action == CacheAction.Invalidate ||
                request.Action == CacheAction.Refresh)
            {
                CacheResponse cached;
                var response = Cache.TryRemove(request.Request, out cached)
                     ? (Promise<TResponse>)cached.Response
                     : Promise<TResponse>.Empty;
                if (request.Action == CacheAction.Invalidate)
                    return response;
            }

            return (Promise<TResponse>)Cache.AddOrUpdate(
                request.Request,   // actual request
                req => RefreshResponse<TResponse>(req, composer),   // add first time
                (req, cached) =>   // update if stale or invalid
                    (cached.Response.State == PromiseState.Rejected  ||
                     cached.Response.State == PromiseState.Cancelled ||
                     DateTime.UtcNow >= cached.LastUpdated + request.TimeToLive)
                ? RefreshResponse<TResponse>(req, composer)
                : cached).Response;
        }

        private static CacheResponse RefreshResponse<TResponse>(
            object request, IHandler composer)
        {
            return new CacheResponse
            {
                Response    = composer.Send((IRequest<TResponse>)request),
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
