namespace Miruken;

public interface IDecorator
{
    object Decoratee { get; }
}

public static class DecoratorExtensions
{
    public static object Decorated(this object source, bool deepest)
    {
        var decorator = source as IDecorator;
        while (decorator != null)
        {
            source    = decorator.Decoratee;
            decorator = source as IDecorator;
        }
        return source;
    }
}