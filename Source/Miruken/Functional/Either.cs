namespace Miruken.Functional
{
    using System;

    public interface IEither
    {
        object Value  { get; }
        
        public interface ILeft  : IEither { }
        public interface IRight : IEither { }
    }

    public abstract class Either<TL, TR> : IEither
    {
        public abstract object Value  { get; }

        public abstract void Match(Action<TL> matchLeft, Action<TR> matchRight);
        public abstract T Match<T>(Func<TL, T> matchLeft, Func<TR, T> matchRight);

        public TL LeftOrDefault() => Match(l => l, _ => default);
        public TR RightOrDefault() => Match(_ => default, r => r);

        public static implicit operator Either<TL, TR>(TL left) => new Left(left);
        public static implicit operator Either<TL, TR>(TR right) => new Right(right);

        public sealed class Left : Either<TL, TR>, IEither.ILeft
        {
            private readonly TL _left;

            public Left(TL left)
            {
                _left = left;
            }
            
            public override object Value => _left;

            public override void Match(Action<TL> matchLeft, Action<TR> matchRight)
            {
                if (matchLeft == null)
                    throw new ArgumentNullException(nameof(matchLeft));
                matchLeft(_left);
            }

            public override T Match<T>(Func<TL, T> matchLeft, Func<TR, T> matchRight)
            {
                if (matchLeft == null)
                    throw new ArgumentNullException(nameof(matchLeft));
                return matchLeft(_left);
            }

            public Either<TL, TUr> Select<TUr>(Func<TR, TUr> selector) =>
                new Either<TL, TUr>.Left(_left);

            public Either<TL, TVr> SelectMany<TUr, TVr>(
                Func<TR, Either<TL, TUr>> selector,
                Func<TR, TUr, TVr>        projector) =>
                    new Either<TL, TVr>.Left(_left);
        }
        
        public sealed class Right : Either<TL, TR>, IEither.IRight
        {
            private readonly TR _right;

            public Right(TR right)
            {
                _right = right;
            }
            
            public override object Value  => _right;

            public override void Match(Action<TL> matchLeft, Action<TR> matchRight)
            {
                if (matchRight == null)
                    throw new ArgumentNullException(nameof(matchLeft));
                matchRight(_right);
            }

            public override T Match<T>(Func<TL, T> matchLeft, Func<TR, T> matchRight)
            {
                if (matchRight == null)
                    throw new ArgumentNullException(nameof(matchLeft));
                return matchRight(_right);
            }

            public Either<TL, TUr> Select<TUr>(Func<TR, TUr> selector)
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));
                return new Either<TL, TUr>.Right(selector(_right));
            }

            public Either<TL, TVr> SelectMany<TUr, TVr>(
                Func<TR, Either<TL, TUr>> selector,
                Func<TR, TUr, TVr>        projector)
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));
                
                if (projector == null)
                    throw new ArgumentNullException(nameof(projector));
                
                var result = selector(_right);

                return result.Match(
                    left => (Either<TL, TVr>) new Either<TL, TVr>.Left(left),
                    right => new Either<TL, TVr>.Right(projector(_right, right)));
            }
        }
    }
}
