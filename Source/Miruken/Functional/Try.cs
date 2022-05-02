namespace Miruken.Functional;

using System;

public abstract class Try<Tf, Ts> : EitherCore<Tf, Ts>
{
    public Tf FailureOrDefault() => Match(l => l, _ => default);
    public Ts SuccessOrDefault() => Match(_ => default, r => r);

    public static implicit operator Try<Tf, Ts>(Tf failure) => new Failure(failure);
    public static implicit operator Try<Tf, Ts>(Ts success) => new Success(success);

    public sealed class Failure : Try<Tf, Ts>, IEither.ILeft
    {
        private readonly Tf _failure;

        public Failure(Tf failure)
        {
            _failure = failure;
        }
            
        public override object Value  => _failure;
        public override bool   IsLeft => true;
            
        public override void Match(Action<Tf> matchFailure, Action<Ts> matchSuccess)
        {
            if (matchFailure == null)
                throw new ArgumentNullException(nameof(matchFailure));
            matchFailure(_failure);
        }

        public override T Match<T>(Func<Tf, T> matchFailure, Func<Ts, T> matchSuccess)
        {
            if (matchFailure == null)
                throw new ArgumentNullException(nameof(matchFailure));
            return matchFailure(_failure);
        }

        public Try<Tf, TUs> Select<TUs>(Func<Ts, TUs> selector) =>
            new Try<Tf, TUs>.Failure(_failure);

        public Try<Tf, TVs> SelectMany<TUs, TVs>(
            Func<Ts, Try<Tf, TUs>> selector,
            Func<Ts, TUs, TVs>     projector) =>
            new Try<Tf, TVs>.Failure(_failure);
            
        public static implicit operator Tf(Failure failure)
        {
            return failure != null ? failure._failure : default;
        }
    }
        
    public sealed class Success : Try<Tf, Ts>, IEither.IRight
    {
        private readonly Ts _success;

        public Success(Ts success)
        {
            _success = success;
        }
            
        public override object Value  => _success;
        public override bool   IsLeft => false;
            
        public override void Match(Action<Tf> matchFailure, Action<Ts> matchSuccess)
        {
            if (matchSuccess == null)
                throw new ArgumentNullException(nameof(matchSuccess));
            matchSuccess(_success);
        }

        public override T Match<T>(Func<Tf, T> matchFailure, Func<Ts, T> matchSuccess)
        {
            if (matchSuccess == null)
                throw new ArgumentNullException(nameof(matchSuccess));
            return matchSuccess(_success);
        }

        public Try<Tf, TUs> Select<TUs>(Func<Ts, TUs> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return new Try<Tf, TUs>.Success(selector(_success));
        }

        public Try<Tf, TVs> SelectMany<TUs, TVs>(
            Func<Ts, Try<Tf, TUs>> selector,
            Func<Ts, TUs, TVs>     projector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
                
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
                
            var result = selector(_success);

            return result.Match(
                failure => (Try<Tf, TVs>)new Try<Tf, TVs>.Failure(failure),
                success => new Try<Tf, TVs>.Success(projector(_success, success)));
        }
            
        public static implicit operator Ts(Success success)
        {
            return success != null ? success._success : default;
        }
    }
}