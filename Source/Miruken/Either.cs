namespace Miruken
{
    using System;

    public interface IEither
    {
        bool   IsLeft { get; }
        object Left   { get; set; }
        object Right  { get; set; }
    }

    public class Either<TL, TR> : IEither
    {
        private bool _isLeft;

        public Either(TL left)
        {
            Left    = left;
            _isLeft = true;
        }

        public Either(TR right)
        {
            Right   = right;
            _isLeft = false;
        }

        private Either()
        {
        }

        public TL Left  { get; private set; }
        public TR Right { get; private set; }

        bool IEither.IsLeft => _isLeft;

        object IEither.Left
        {
            get { return Left; }
            set
            {
                if (value is TL)
                {
                    Left    = (TL)value;
                    _isLeft = true;
                }
                else
                    throw new ArgumentException("Could not infer left");
            }
        }

        object IEither.Right
        {
            get { return Right; }
            set
            {
                if (value is TR)
                {
                    Right   = (TR)value;
                    _isLeft = false;
                }
                else
                    throw new ArgumentException("Could not infer right");
            }
        }

        public void Match(Action<TL> matchLeft, Action<TR> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));

            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));

            if (_isLeft)
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

            return _isLeft ? matchLeft(Left) : matchRight(Right);
        }

        public TL LeftOrDefault() => Match(l => l, r => default(TL));
        public TR RightOrDefault() => Match(l => default(TR), r => r);

        public static implicit operator Either<TL, TR>(TL left) => new Either<TL, TR>(left);
        public static implicit operator Either<TL, TR>(TR right) => new Either<TL, TR>(right);
    }
}
