namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Policy;

    public delegate object[] ResolveArgs(Argument[] args);
    public delegate bool TargetAction<in T>(T target, ResolveArgs args);

    public class TargetActionBuilder<T, R>
    {
        private readonly Func<TargetAction<T>, R> _notify;

        public TargetActionBuilder(Func<TargetAction<T>, R> notify)
        {
            _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        }

        public R Invoke(Action<T> block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Array.Empty<Argument>());
                if (args == null || args.Length != 0) return false;
                block(t);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<S>(Func<T, S> block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Array.Empty<Argument>());
                if (args == null || args.Length != 0) return false;
                s = block(t);
                return true;
            }), s);
        }

        public R Invoke<A>(Action<T, A> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 1) return false;
                block(t, (A)args[0]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A, S>(Func<T, A, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 1) return false;
                s = block(t, (A)args[0]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2>(Action<T, A1, A2> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 2) return false;
                block(t, (A1)args[0], (A2)args[1]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, S>(Func<T, A1, A2, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 2) return false;
                s = block(t, (A1)args[0], (A2)args[1]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2, A3>(Action<T, A1, A2, A3> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 3) return false;
                block(t, (A1)args[0], (A2)args[1], (A3)args[2]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, A3, S>(Func<T, A1, A2, A3, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 3) return false;
                s = block(t, (A1)args[0], (A2)args[1], (A3)args[2]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2, A3, A4>(Action<T, A1, A2, A3, A4> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 4) return false;
                block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, A3, A4, S>(Func<T, A1, A2, A3, A4, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 4) return false;
                s = block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2, A3, A4, A5>(Action<T, A1, A2, A3, A4, A5> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 5) return false;
                block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, A3, A4, A5, S>(Func<T, A1, A2, A3, A4, A5, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 5) return false;
                s = block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2, A3, A4, A5, A6>(Action<T, A1, A2, A3, A4, A5, A6> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 6) return false;
                block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4], (A6)args[5]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, A3, A4, A5, A6, S>(Func<T, A1, A2, A3, A4, A5, A6, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 6) return false;
                s = block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4], (A6)args[5]);
                return true;
            }), s);
        }

        public R Invoke<A1, A2, A3, A4, A5, A6, A7>(Action<T, A1, A2, A3, A4, A5, A6, A7> block)
        {
            return _notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 7) return false;
                block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4], (A6)args[5], (A7)args[6]);
                return true;
            });
        }

        public Tuple<R, Maybe<S>> Invoke<A1, A2, A3, A4, A5, A6, A7, S>(Func<T, A1, A2, A3, A4, A5, A6, A7, S> block)
        {
            var s = Maybe<S>.Nothing;
            return Tuple.Create(_notify((t, resolveArgs) =>
            {
                var args = resolveArgs(Arguments(block));
                if (args == null || args.Length != 7) return false;
                s = block(t, (A1)args[0], (A2)args[1], (A3)args[2], (A4)args[3], (A5)args[4], (A6)args[5], (A7)args[6]);
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
}
