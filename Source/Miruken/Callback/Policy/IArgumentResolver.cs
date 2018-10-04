namespace Miruken.Callback.Policy
{
    public interface IArgumentResolver
    {
        bool IsOptional { get;  }

        void ValidateArgument(Argument argument);

        object ResolveArgument(Inquiry parent,
            Argument argument, IHandler handler, IHandler composer);
    }
}