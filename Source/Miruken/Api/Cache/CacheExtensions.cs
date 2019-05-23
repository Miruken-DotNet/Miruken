namespace Miruken.Api.Cache
{
    using System;
    using Api;

    public static class CacheExtensions
    {
        public static Cached<TResponse> Cached<TResponse>(
            this IRequest<TResponse> request)
        {
            return new Cached<TResponse>(request);
        }

        public static Cached<TResponse> Cached<TResponse>(
            this IRequest<TResponse> request, TimeSpan timeToLive)
        {
            return new Cached<TResponse>(request)
            {
                TimeToLive = timeToLive
            };
        }

        public static Cached<TResponse> Invalidate<TResponse>(
            this IRequest<TResponse> request)
        {
            return new Cached<TResponse>(request)
            {
                Action = CacheAction.Invalidate
            };
        }

        public static Cached<TResponse> Refresh<TResponse>(
            this IRequest<TResponse> request)
        {
            return new Cached<TResponse>(request)
            {
                Action = CacheAction.Refresh
            };
        }
    }
}
