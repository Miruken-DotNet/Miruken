namespace Miruken.Api.Cache
{
    using System;
    using Api;

    public static class CacheExtensions
    {
        public static Cached<TResponse> Cached<TResponse>(
            this IRequest<TResponse> request)
        {
            return new(request);
        }

        public static Cached<TResponse> Cached<TResponse>(
            this IRequest<TResponse> request, TimeSpan timeToLive)
        {
            return new(request)
            {
                TimeToLive = timeToLive
            };
        }

        public static Cached<TResponse> Invalidate<TResponse>(
            this IRequest<TResponse> request)
        {
            return new(request)
            {
                Action = CacheAction.Invalidate
            };
        }

        public static Cached<TResponse> Refresh<TResponse>(
            this IRequest<TResponse> request)
        {
            return new(request)
            {
                Action = CacheAction.Refresh
            };
        }
    }
}
