namespace Miruken.Callback.Policy
{
    using Concurrency;

    public interface IArgumentResolver
    {
        bool IsOptional { get;  }

        void ValidateArgument(Argument argument);

        object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler);

        object ResolveArgumentAsync(Inquiry parent,
            Argument argument, IHandler handler);
    }
}