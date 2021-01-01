namespace Miruken.Functional
{
    using System;

    public abstract class Try<TF, TS> : IEither
    {
        public abstract object Value  { get; }

        public abstract void Match(Action<TF> matchFailure, Action<TS> matchSuccess);
        public abstract T Match<T>(Func<TF, T> matchFailure, Func<TS, T> matchSuccess);

        public TF FailureOrDefault() => Match(l => l, _ => default);
        public TS SuccessOrDefault() => Match(_ => default, r => r);

        public static implicit operator Try<TF, TS>(TF failure) => new Failure(failure);
        public static implicit operator Try<TF, TS>(TS success) => new Success(success);

        public sealed class Failure : Try<TF, TS>, IEither.ILeft
        {
            private readonly TF _failure;

            public Failure(TF failure)
            {
                _failure = failure;
            }
            
            public override object Value => _failure;

            public override void Match(Action<TF> matchFailure, Action<TS> matchSuccess)
            {
                if (matchFailure == null)
                    throw new ArgumentNullException(nameof(matchFailure));
                matchFailure(_failure);
            }

            public override T Match<T>(Func<TF, T> matchFailure, Func<TS, T> matchSuccess)
            {
                if (matchFailure == null)
                    throw new ArgumentNullException(nameof(matchFailure));
                return matchFailure(_failure);
            }

            public Try<TF, TUs> Select<TUs>(Func<TS, TUs> selector) =>
                new Try<TF, TUs>.Failure(_failure);

            public Try<TF, TVs> SelectMany<TUs, TVs>(
                Func<TS, Try<TF, TUs>> selector,
                Func<TS, TUs, TVs>     projector) =>
                    new Try<TF, TVs>.Failure(_failure);
        }
        
        public sealed class Success : Try<TF, TS>, IEither.IRight
        {
            private readonly TS _success;

            public Success(TS success)
            {
                _success = success;
            }
            
            public override object Value  => _success;

            public override void Match(Action<TF> matchFailure, Action<TS> matchSuccess)
            {
                if (matchSuccess == null)
                    throw new ArgumentNullException(nameof(matchFailure));
                matchSuccess(_success);
            }

            public override T Match<T>(Func<TF, T> matchFailure, Func<TS, T> matchSuccess)
            {
                if (matchSuccess == null)
                    throw new ArgumentNullException(nameof(matchFailure));
                return matchSuccess(_success);
            }

            public Try<TF, TUs> Select<TUs>(Func<TS, TUs> selector)
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));
                return new Try<TF, TUs>.Success(selector(_success));
            }

            public Try<TF, TVs> SelectMany<TUs, TVs>(
                Func<TS, Try<TF, TUs>> selector,
                Func<TS, TUs, TVs>     projector)
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));
                
                if (projector == null)
                    throw new ArgumentNullException(nameof(projector));
                
                var result = selector(_success);

                return result.Match(
                    failure => (Try<TF, TVs>)new Try<TF, TVs>.Failure(failure),
                    success => new Try<TF, TVs>.Success(projector(_success, success)));
            }
        }
    }
}
