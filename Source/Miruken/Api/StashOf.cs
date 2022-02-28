namespace Miruken.Api;

using System;
using System.Threading.Tasks;
using Callback;

public class StashOf<T>
    where T : class
{
    private readonly IHandler _handler;

    public StashOf(IHandler handler)
    {
        _handler = handler
                   ?? throw new ArgumentNullException(nameof(handler));
    }

    public T Value
    {
        get => _handler.StashTryGet<T>();
        set => _handler.StashPut(value);
    }

    public T GetOrPut(T value)
    {
        return _handler.StashGetOrPut(value);
    }

    public T GetOrPut(Func<IHandler, T> put)
    {
        if (put == null)
            throw new ArgumentNullException(nameof(put));
        return _handler.StashGetOrPut(() => put(_handler));
    }

    public Task<T> GetOrPut(Func<IHandler, Task<T>> put)
    {
        if (put == null)
            throw new ArgumentNullException(nameof(put));
        return _handler.StashGetOrPut(() => put(_handler));
    }

    public void Drop()
    {
        _handler.StashDrop<T>();
    }

    public static implicit operator T(StashOf<T> stashOf)
    {
        return stashOf.Value;
    }
}