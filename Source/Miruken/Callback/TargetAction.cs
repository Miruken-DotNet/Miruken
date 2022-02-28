namespace Miruken.Callback;

using System;
using System.Linq;
using System.Reflection;
using Functional;
using Policy;

public delegate object[] ResolveArgs(Argument[] args);
public delegate bool TargetAction<in T>(T target, ResolveArgs args);

public class TargetActionBuilder<T, TR>
{
    private readonly Func<TargetAction<T>, TR> _notify;

    public TargetActionBuilder(Func<TargetAction<T>, TR> notify)
    {
        _notify = notify ?? throw new ArgumentNullException(nameof(notify));
    }

    public TR Invoke(Action<T> block)
    {
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Array.Empty<Argument>());
            if (args is not { Length: 0 }) return false;
            block(t);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TS>(Func<T, TS> block)
    {
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Array.Empty<Argument>());
            if (args is not { Length: 0 }) return false;
            s = block(t);
            return true;
        }), s);
    }

    public TR Invoke<TA>(Action<T, TA> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 1 }) return false;
            block(t, (TA)args[0]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA, TS>(Func<T, TA, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 1 }) return false;
            s = block(t, (TA)args[0]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2>(Action<T, TA1, TA2> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 2 }) return false;
            block(t, (TA1)args[0], (TA2)args[1]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TS>(Func<T, TA1, TA2, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 2 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2, TA3>(Action<T, TA1, TA2, TA3> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 3 }) return false;
            block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TA3, TS>(Func<T, TA1, TA2, TA3, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 3 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2, TA3, TA4>(Action<T, TA1, TA2, TA3, TA4> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 4 }) return false;
            block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TA3, TA4, TS>(Func<T, TA1, TA2, TA3, TA4, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 4 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2, TA3, TA4, TA5>(Action<T, TA1, TA2, TA3, TA4, TA5> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 5 }) return false;
            block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TA3, TA4, TA5, TS>(Func<T, TA1, TA2, TA3, TA4, TA5, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 5 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2, TA3, TA4, TA5, TA6>(Action<T, TA1, TA2, TA3, TA4, TA5, TA6> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 6 }) return false;
            block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4], (TA6)args[5]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TA3, TA4, TA5, TA6, TS>(Func<T, TA1, TA2, TA3, TA4, TA5, TA6, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 6 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4], (TA6)args[5]);
            return true;
        }), s);
    }

    public TR Invoke<TA1, TA2, TA3, TA4, TA5, TA6, TA7>(Action<T, TA1, TA2, TA3, TA4, TA5, TA6, TA7> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 7 }) return false;
            block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4], (TA6)args[5], (TA7)args[6]);
            return true;
        });
    }

    public Tuple<TR, Maybe<TS>> Invoke<TA1, TA2, TA3, TA4, TA5, TA6, TA7, TS>(Func<T, TA1, TA2, TA3, TA4, TA5, TA6, TA7, TS> block)
    {
        var s = Maybe<TS>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 7 }) return false;
            s = block(t, (TA1)args[0], (TA2)args[1], (TA3)args[2], (TA4)args[3], (TA5)args[4], (TA6)args[5], (TA7)args[6]);
            return true;
        }), s);
    }

    private static Argument[] Arguments(Delegate block) =>
        block.GetMethodInfo().GetParameters()
            .Skip(1).Select(p => new Argument(p)).ToArray();
}

public class TargetActionBuilder<T> : TargetActionBuilder<T, object>
{
    public TargetActionBuilder(Action<TargetAction<T>> notify)
        : base(action => { notify(action); return null; })
    {
    }
}