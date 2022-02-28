namespace Miruken.Api.Cache;

using System;
using System.Collections.Concurrent;
using Api;
using Callback;
using Concurrency;

public class CachedHandler : Handler
{
    private readonly
        ConcurrentDictionary<object, CacheResponse> _cache
            = new();

    private struct CacheResponse
    {
        public Promise  Response;
        public DateTime LastUpdated;
    }

    [Provides, Singleton]
    public CachedHandler()
    {    
    }

    [Handles]
    public Promise<TResponse> Cached<TResponse>(
        Cached<TResponse> request, IHandler composer)
    {
        if (request.Request == null)
            return Promise<TResponse>.Empty;

        if (request.Action is CacheAction.Invalidate or CacheAction.Refresh)
        {
            var response = _cache.TryRemove(request.Request, out var cached)
                ? (Promise<TResponse>)cached.Response
                : Promise<TResponse>.Empty;
            if (request.Action == CacheAction.Invalidate)
                return response;
        }

        return (Promise<TResponse>)_cache.AddOrUpdate(
            request.Request,   // actual request
            req => RefreshResponse<TResponse>(req, composer),   // add first time, 
            (req, cached) =>   // update if stale or invalid
                cached.Response.State is PromiseState.Rejected or PromiseState.Cancelled || DateTime.UtcNow >= cached.LastUpdated + request.TimeToLive
                    ? RefreshResponse<TResponse>(req, composer)
                    : cached).Response;
    }

    private static CacheResponse RefreshResponse<TResponse>(
        object request, IHandler composer)
    {
        var response = composer.Send((IRequest<TResponse>)request);
        var cached   = new CacheResponse
        {
            Response    = response,
            LastUpdated = DateTime.UtcNow
        };
        response.Then((result, _) => cached.Response = Promise.Resolved(result));
        return cached;
    }
}