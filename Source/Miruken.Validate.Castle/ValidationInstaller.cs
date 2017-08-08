namespace Miruken.Validate.Castle
{
    using System;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::FluentValidation;
    using Miruken.Castle;

    public class ValidationInstaller : FeatureInstaller
    {
        private Action<ComponentRegistration> _configure;

        public ValidationInstaller ConfigureValidators(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void Install(IConfigurationStore store)
        {
            Container.Register(Component.For<IValidatorFactory>()
                .ImplementedBy<WindsorValidatorFactory>()
                .OnlyNewServices());
        }

        public override void InstallFeatures(FromDescriptor from)
        {
            var validators = from
                .BasedOn(typeof(IValidator<>))
                .WithServiceBase();
            if (_configure != null)
                validators.Configure(_configure);
        }

        public static FromDescriptor StandardFeatures =>
            Classes.FromAssemblyContaining<ValidationHandler>();
    }
}
