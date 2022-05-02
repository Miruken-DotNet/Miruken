namespace Miruken.Functional;

using System;

public interface IEither
{
    object Value  { get; }
    bool   IsLeft { get; }
        
    public interface ILeft  : IEither { }
    public interface IRight : IEither { }
}

public abstract class EitherCore<Tl, Tr> : IEither
{
    public abstract object Value  { get; }
    public abstract bool   IsLeft { get; }
    public abstract void Match(Action<Tl> matchLeft, Action<Tr> matchRight);
    public abstract T Match<T>(Func<Tl, T> matchLeft, Func<Tr, T> matchRight);
}

public abstract class Either<Tl, Tr> : EitherCore<Tl, Tr>
{
    public Tl LeftOrDefault() => Match(l => l, _ => default);
    public Tr RightOrDefault() => Match(_ => default, r => r);

    public static implicit operator Either<Tl, Tr>(Tl left) => new Left(left);
    public static implicit operator Either<Tl, Tr>(Tr right) => new Right(right);
        
    public sealed class Left : Either<Tl, Tr>, IEither.ILeft
    {
        private readonly Tl _left;

        public Left(Tl left)
        {
            _left = left;
        }
            
        public override object Value  => _left;
        public override bool   IsLeft => true;

        public override void Match(Action<Tl> matchLeft, Action<Tr> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));
            matchLeft(_left);
        }

        public override T Match<T>(Func<Tl, T> matchLeft, Func<Tr, T> matchRight)
        {
            if (matchLeft == null)
                throw new ArgumentNullException(nameof(matchLeft));
            return matchLeft(_left);
        }

        public Either<Tl, TUr> Select<TUr>(Func<Tr, TUr> selector) =>
            new Either<Tl, TUr>.Left(_left);

        public Either<Tl, TVr> SelectMany<TUr, TVr>(
            Func<Tr, Either<Tl, TUr>> selector,
            Func<Tr, TUr, TVr>        projector) =>
            new Either<Tl, TVr>.Left(_left);

        public static implicit operator Tl(Left left)
        {
            return left != null ? left._left : default;
        }
    }
        
    public sealed class Right : Either<Tl, Tr>, IEither.IRight
    {
        private readonly Tr _right;

        public Right(Tr right)
        {
            _right = right;
        }
            
        public override object Value  => _right;
        public override bool   IsLeft => false;
            
        public override void Match(Action<Tl> matchLeft, Action<Tr> matchRight)
        {
            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));
            matchRight(_right);
        }

        public override T Match<T>(Func<Tl, T> matchLeft, Func<Tr, T> matchRight)
        {
            if (matchRight == null)
                throw new ArgumentNullException(nameof(matchRight));
            return matchRight(_right);
        }

        public Either<Tl, TUr> Select<TUr>(Func<Tr, TUr> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return new Either<Tl, TUr>.Right(selector(_right));
        }

        public Either<Tl, TVr> SelectMany<TUr, TVr>(
            Func<Tr, Either<Tl, TUr>> selector,
            Func<Tr, TUr, TVr>        projector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
                
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
                
            var result = selector(_right);

            return result.Match(
                left => (Either<Tl, TVr>) new Either<Tl, TVr>.Left(left),
                right => new Either<Tl, TVr>.Right(projector(_right, right)));
        }
            
        public static implicit operator Tr(Right right)
        {
            return right != null ? right._right : default;
        }
    }
}