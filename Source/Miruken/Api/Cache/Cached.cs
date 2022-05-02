namespace Miruken.Api.Cache;

using System;
using Api;

public enum CacheAction
{
    Refresh = 0,
    Invalidate
}

public class Cached<TResponse> : RequestDecorator<TResponse>
{
    public Cached()
    {
    }

    public Cached(IRequest<TResponse> request)
        : base(request)
    {
        TimeToLive = TimeSpan.FromDays(1);
    }

    public CacheAction? Action     { get; init; }
    public TimeSpan     TimeToLive { get; init; }
}