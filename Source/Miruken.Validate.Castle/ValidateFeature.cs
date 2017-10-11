namespace Miruken.Validate.Castle
{
    using System;
    using System.Collections.Generic;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::FluentValidation;
    using Miruken.Castle;

    public class ValidateFeature : FeatureInstaller
    {
        private Action<ComponentRegistration> _configure;

        public ValidateFeature ConfigureValidators(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        public override IEnumerable<FromDescriptor> GetFeatures()
        {
            yield return Classes.FromAssemblyContaining<ValidationHandler>();
        }

        public override void InstallFeatures(FromDescriptor from)
        {
            var validators = from
                .BasedOn(typeof(IValidator<>))
                .WithServiceBase();
            if (_configure != null)
                validators.Configure(_configure);
        }

        protected override void Install(IConfigurationStore store)
        {
            Container.Register(Component.For<IValidatorFactory>()
                .ImplementedBy<WindsorValidatorFactory>()
                .OnlyNewServices());
        }
    }
}
