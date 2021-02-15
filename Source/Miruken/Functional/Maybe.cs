namespace Miruken.Functional
{
    using System;
    using System.Reflection;

    public interface IMaybe
    {
        bool   HasValue { get; }
        object Value    { get; }
    }

    public class Maybe<T> : IMaybe
    {
        public static readonly Maybe<T> Nothing = new();

        private Maybe()
        {
            HasValue = false;
            Value    = default;
        }

        internal Maybe(T value)
        {
            HasValue = true;
            Value    = value;
        }

        public bool HasValue { get; }
        public T    Value    { get; }

        object IMaybe.Value => Value;

        public Maybe<TResult> Select<TResult>(Func<T, TResult> f)
        {
            return SelectMany(x => f(x).Some());
        }

        public Maybe<TResult> SelectMany<TResult>(Func<T, Maybe<TResult>> f)
        {
            return !HasValue ? Maybe<TResult>.Nothing : f(Value);
        }

        public Maybe<TResult> SelectMany<TMaybe, TResult>(
             Func<T, Maybe<TMaybe>> f, Func<T, TMaybe, TResult> g)
        {
            return SelectMany(x => f(x).SelectMany(y => g(x, y).Some()));
        }

        public static implicit operator Maybe<T>(T value)
        {
            return Equals(value, null) ? Nothing : new Maybe<T>(value);
        }
    }

    public static class Maybe
    {
        private static readonly MethodInfo Something =
            typeof(Maybe).GetMethod(nameof(Some),
                BindingFlags.Public | BindingFlags.Static);

        public static Maybe<T> Some<T>(this T value)
        {
            return Equals(value, null)
                 ? throw new ArgumentNullException(nameof(value))
                 : new Maybe<T>(value);
        }

        public static IMaybe DynamicSome(object value)
        {
            return Equals(value, null)
                 ? throw new ArgumentNullException(nameof(value))
                 : (IMaybe)Something.MakeGenericMethod(value.GetType())
                    .Invoke(null, new [] { value });
        }

        public static IMaybe DynamicNothing(Type maybeType)
        {
            return (IMaybe)maybeType.InvokeMember("Nothing",
                    BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static, null,
                    null, Array.Empty<object>());
        }
    }
}
