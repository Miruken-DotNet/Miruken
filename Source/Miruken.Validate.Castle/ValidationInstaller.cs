﻿namespace Miruken.Validate.Castle
{
    using System;
    using global::Castle.MicroKernel.Registration;
    using global::Castle.MicroKernel.SubSystems.Configuration;
    using global::Castle.Windsor;
    using global::FluentValidation;
    using Miruken.Castle;

    public class ValidationInstaller : PluginInstaller
    {
        private IWindsorContainer _container;
        private Action<ComponentRegistration> _configure;

        public ValidationInstaller ConfigureValidators(Action<ComponentRegistration> configure)
        {
            _configure += configure;
            return this;
        }

        protected override void Install(IWindsorContainer container, IConfigurationStore store)
        {
            _container = container;
            _container.Register(Component.For<IValidatorFactory>()
                .ImplementedBy<WindsorValidatorFactory>()
                .OnlyNewServices());
        }

        protected override void InstallPlugin(Plugin plugin)
        {
            var validators = Classes.FromAssembly(plugin.Assembly)
                .BasedOn(typeof(IValidator<>))
                .WithServiceBase();
            if (_configure != null)
                validators.Configure(_configure);
            _container.Register(validators);
        }
    }
}