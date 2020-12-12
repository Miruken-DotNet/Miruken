namespace Miruken.Callback
{
    using System;
    using Concurrency;
    using Infrastructure;
    using Policy;

    public class OptionsAttribute : ResolvingAttribute
    {
        public static readonly OptionsAttribute
            Instance = new OptionsAttribute();
        
        public override bool IsOptional => true;
        
        public override void ValidateArgument(Argument argument)
        {
            var openType = argument.ParameterType
                .GetOpenTypeConformance(typeof(Options<>));
            if (openType == null)
            {
                throw new ArgumentException(
                    "Options parameters must extend Options<T>.");
            }    
            
            if (!argument.ParameterType.HasDefaultConstructor())
            {
                throw new ArgumentException(
                    "Options parameters must have a default constructor.");
            }
        }
        
        protected override object Resolve(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            var options = Activator.CreateInstance((Type) key);
            return handler.Handle(options, true) ? options : null;
        }

        protected override Promise ResolveAsync(
            Inquiry parent, Argument argument,
            object key, IHandler handler)
        {
            var options = Resolve(parent, argument, key, handler);
            return Promise.Resolved(options);
        }
    }
}