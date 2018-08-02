namespace Miruken
{
    using System;

    public interface IEither
    {
        bool   IsLeft { get; }
        object Value  { get; }
    }

    public class Either<TL, TR> : IEither
    {
        public Either(TL left)
        {
            Left   = left;
            IsLeft = true;
        }

        public Either(TR right)
        {
            Right  = right;
            IsLeft = false;
        }

        protected Either()
        {
        }

        public TL Left  { get; }
        public TR Right { get; }

        public bool IsLeft { get; }

        object IEither.Value => IsLeft ? Left : (object)Right;

        public void Match(Action<TL> matchLeft, Action<TR> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));

            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));

            if (IsLeft)
                matchLeft(Left);
            else
                matchRight(Right);
        }

        public T Match<T>(Func<TL, T> matchLeft, Func<TR, T> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));

            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));

            return IsLeft ? matchLeft(Left) : matchRight(Right);
        }

        public TL LeftOrDefault() => Match(l => l, r => default);
        public TR RightOrDefault() => Match(l => default, r => r);

        public static implicit operator Either<TL, TR>(TL left) => new Either<TL, TR>(left);
        public static implicit operator Either<TL, TR>(TR right) => new Either<TL, TR>(right);

        public Either<TL, UR> Select<UR>(Func<TR, UR> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return IsLeft
                 ?  new Either<TL, UR>(Left)
                 :  new Either<TL, UR>(selector(Right));
        }

        public Either<TL, VR> SelectMany<UR, VR>(
            Func<TR, Either<TL, UR>> selector,
            Func<TR, UR, VR> projector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));

            if (IsLeft)
                return new Either<TL, VR>(Left);

            var result = selector(Right);
            return result.IsLeft
                 ? new Either<TL, VR>(result.Left)
                 : new Either<TL, VR>(projector(Right, result.Right));
        }
    }

    public class Try<TE, TR> : Either<TE, TR>
    {
        public Try(TR result)
             : base(result)
        {
        }

        public Try(TE error)
            : base(error)
        {
            IsError = true;
        }

        protected Try()
        {
        }

        public bool IsError { get; }

        public static implicit operator Try<TE, TR>(TE error) => new Try<TE, TR>(error);
        public static implicit operator Try<TE, TR>(TR result) => new Try<TE, TR>(result);
    }
}
