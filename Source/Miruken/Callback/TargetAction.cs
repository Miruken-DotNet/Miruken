namespace Miruken.Callback;

using System;
using System.Linq;
using System.Reflection;
using Functional;
using Policy;

public delegate object[] ResolveArgs(Argument[] args);
public delegate bool TargetAction<in T>(T target, ResolveArgs args);

public class TargetActionBuilder<T, Tr>
{
    private readonly Func<TargetAction<T>, Tr> _notify;

    public TargetActionBuilder(Func<TargetAction<T>, Tr> notify)
    {
        _notify = notify ?? throw new ArgumentNullException(nameof(notify));
    }

    public Tr Invoke(Action<T> block)
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

    public Tuple<Tr, Maybe<Ts>> Invoke<Ts>(Func<T, Ts> block)
    {
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Array.Empty<Argument>());
            if (args is not { Length: 0 }) return false;
            s = block(t);
            return true;
        }), s);
    }

    public Tr Invoke<Ta>(Action<T, Ta> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 1 }) return false;
            block(t, (Ta)args[0]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta, Ts>(Func<T, Ta, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 1 }) return false;
            s = block(t, (Ta)args[0]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2>(Action<T, Ta1, Ta2> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 2 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ts>(Func<T, Ta1, Ta2, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 2 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2, Ta3>(Action<T, Ta1, Ta2, Ta3> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 3 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ta3, Ts>(Func<T, Ta1, Ta2, Ta3, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 3 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2, Ta3, Ta4>(Action<T, Ta1, Ta2, Ta3, Ta4> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 4 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ta3, Ta4, Ts>(Func<T, Ta1, Ta2, Ta3, Ta4, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 4 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2, Ta3, Ta4, Ta5>(Action<T, Ta1, Ta2, Ta3, Ta4, Ta5> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 5 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ta3, Ta4, Ta5, Ts>(Func<T, Ta1, Ta2, Ta3, Ta4, Ta5, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 5 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2, Ta3, Ta4, Ta5, Ta6>(Action<T, Ta1, Ta2, Ta3, Ta4, Ta5, Ta6> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 6 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4], (Ta6)args[5]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ts>(Func<T, Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 6 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4], (Ta6)args[5]);
            return true;
        }), s);
    }

    public Tr Invoke<Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ta7>(Action<T, Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ta7> block)
    {
        return _notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 7 }) return false;
            block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4], (Ta6)args[5], (Ta7)args[6]);
            return true;
        });
    }

    public Tuple<Tr, Maybe<Ts>> Invoke<Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ta7, Ts>(Func<T, Ta1, Ta2, Ta3, Ta4, Ta5, Ta6, Ta7, Ts> block)
    {
        var s = Maybe<Ts>.Nothing;
        return Tuple.Create(_notify((t, resolveArgs) =>
        {
            var args = resolveArgs(Arguments(block));
            if (args is not { Length: 7 }) return false;
            s = block(t, (Ta1)args[0], (Ta2)args[1], (Ta3)args[2], (Ta4)args[3], (Ta5)args[4], (Ta6)args[5], (Ta7)args[6]);
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