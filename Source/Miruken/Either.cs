namespace Miruken
{
    using System;

    public class Either<TL, TR>
    {
        private readonly TL _left;
        private readonly TR _right;
        private readonly bool _isLeft;

        public Either(TL left)
        {
            _left = left;
            _isLeft = true;
        }

        public Either(TR right)
        {
            _right = right;
            _isLeft = false;
        }

        public void Match(Action<TL> matchLeft, Action<TR> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));

            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));

            if (_isLeft)
                matchLeft(_left);
            else
                matchRight(_right);
        }

        public T Match<T>(Func<TL, T> matchLeft, Func<TR, T> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));

            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));

            return _isLeft ? matchLeft(_left) : matchRight(_right);
        }

        public TL LeftOrDefault() => Match(l => l, r => default(TL));
        public TR RightOrDefault() => Match(l => default(TR), r => r);

        public static implicit operator Either<TL, TR>(TL left) => new Either<TL, TR>(left);
        public static implicit operator Either<TL, TR>(TR right) => new Either<TL, TR>(right);
    }

}
