namespace Miruken.Validate.Castle
{
    using System;
    using global::Castle.MicroKernel;
    using global::FluentValidation;

    public class WindsorValidatorFactory : ValidatorFactoryBase
    {
        private readonly IKernel _kernel;

        public WindsorValidatorFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public override IValidator CreateInstance(Type validatorType)
        {
            return _kernel.HasComponent(validatorType)
                 ? _kernel.Resolve(validatorType) as IValidator
                 : null;
        }
    }
}