namespace Miruken.Callback
{
    using System;
    using Concurrency;
    using Policy;

    public class ProxyAttribute : ResolvingAttribute
    {
        public static readonly ProxyAttribute
            Instance = new();

        public override void ValidateArgument(Argument argument)
        {
            if (!argument.ParameterType.IsInterface)
            {
                throw new ArgumentException(
                    "Proxy parameters must be interfaces.");
            }

            if (argument.IsEnumerable)
            {
                throw new ArgumentException(
                    "Proxy parameters cannot be collections.");
            }

            if (argument.IsPromise || argument.IsTask)
            {
                throw new ArgumentException(
                    "Proxy parameters cannot be tasks or promises.");
            }
        }

        protected override object Resolve(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return handler.Proxy((Type)key);
        }

        protected override Promise ResolveAsync(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            return Promise.Resolved(handler.Proxy((Type) key));
        }
    }
}
