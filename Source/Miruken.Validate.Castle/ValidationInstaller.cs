namespace Miruken.Validate.Castle
{
    using System;
    using System.Reflection;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::FluentValidation;
    using Miruken.Castle;

    public class ValidationInstaller : FeatureInstaller
    {
        private Action<ComponentRegistration> _configure;

        public ValidationInstaller()
            : base(typeof(IValidator<>).Assembly)
        {           
        }

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

        protected override void InstallFeature(Assembly assembly)
        {
            var validators = Classes.FromAssembly(assembly)
                .BasedOn(typeof(IValidator<>))
                .WithServiceBase();
            if (_configure != null)
                validators.Configure(_configure);
            Container.Register(validators);
        }
    }
}
