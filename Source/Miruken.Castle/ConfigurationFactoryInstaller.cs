namespace Miruken.Castle
{
    using System;
    using System.Configuration;
    using System.Linq;
    using global::Castle.Components.DictionaryAdapter;
    using global::Castle.MicroKernel.Registration;

    public class ConfigurationFactoryInstaller : FeatureInstaller
    {
        private readonly DictionaryAdapterFactory _configFactory;

        public ConfigurationFactoryInstaller()
        {
            _configFactory  = new DictionaryAdapterFactory();
        }

        protected override void InstallFeature(FeatureAssembly feature)
        {
            var appSettings = ConfigurationManager.AppSettings;

            Container.Register(Types.FromAssembly(feature.Assembly)
                .Where(IsConfiguration)
                .Configure(reg => reg.UsingFactoryMethod(
                    (k,m,c) => _configFactory.GetAdapter(m.Services.First(), appSettings))
                ));
        }

        protected virtual bool IsConfiguration(Type type)
        {
            return type.IsInterface && type.Name.EndsWith("Config");
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property)]
    public class Update : DictionaryBehaviorAttribute, IDictionaryPropertySetter
    {
        public bool SetPropertyValue(IDictionaryAdapter dictionaryAdapter,
            string key, ref object value, PropertyDescriptor property)
        {
            try
            {
                var configuration = ConfigurationManager.
                    OpenExeConfiguration(ConfigurationUserLevel.None);
                configuration.AppSettings.Settings[key].Value = value?.ToString();
                configuration.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                // No configuration or permission
            }
            return true;
        }
    }
}
