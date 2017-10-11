﻿namespace Miruken.Callback
{
    using System;
    using Policy;

    public class ProxyAttribute : ResolvingAttribute
    {
        public override void ValidateArgument(Argument argument)
        {
            if (!argument.LogicalType.IsInterface)
                throw new NotSupportedException(
                    "Proxy parameters must be interfaces");

            if (argument.IsArray)
                throw new NotSupportedException(
                    "Proxy parameters cannot be arrays");

            if (argument.IsPromise || argument.IsTask)
                throw new NotSupportedException(
                    "Proxy parameters cannot be tasks or promises");
        }

        protected override object Resolve(object key, IHandler handler)
        {
            return handler.Proxy((Type)key);
        }
    }
}