namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Generic;

    public interface IConfigurationService
    {
        IEnumerable<KeyValuePair<string, string>> GetValues();
        string GetValue(string key, string defaultValue);
        string GetValue(string key);
        Type GetMappedType(string mappedTypeName);
        void Load(string fileName, string applicationStartupPath);
        void LoadRegistry(string path);
        bool TryLoad(string fileName, string applicationStartupPath);
        void SetConfigValues(string terminalNumber, string parkCode, string minMatch, string timeout,string applicationStartupPath);
    }
}
