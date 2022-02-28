﻿namespace Miruken.Callback;

using System;
using System.Collections.Generic;

[Unmanaged]
public class ServiceProviderWrapper : Handler
{
    private readonly IServiceProvider _provider;

    public ServiceProviderWrapper(IServiceProvider provider)
    {
        _provider = provider ??
                    throw new ArgumentNullException(nameof(provider));
    }

    [Provides]
    public object Provide(Inquiry inquiry)
    {
        if (inquiry.Key is not Type type || !inquiry.Metadata.IsEmpty) return null;
        if (!inquiry.Many) return _provider.GetService(type);
        var manyType = typeof(IEnumerable<>).MakeGenericType(type);
        return _provider.GetService(manyType);
    }
}