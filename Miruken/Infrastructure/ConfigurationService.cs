using resx = Miruken.Properties.Resources;

namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Microsoft.Win32;

    public class ConfigurationService : IConfigurationService
    {
        public static ConfigurationService Instance { get; private set; }
        static ConfigurationService()
        {
            Instance = new ConfigurationService();
        }

        private readonly Dictionary<string, string> _values;
        private readonly Dictionary<string, UnityObjectMap> _maps;

        private ConfigurationService()
        {
            _values = new Dictionary<string, string>();
            _maps   = new Dictionary<string, UnityObjectMap>();
        }

        public void SetConfigValues(
            string terminalNumber, string parkCode, string minMatch, string timeout, string applicationStartupPath)
        {
            const string fileName = "server.config";
            var fullpath = applicationStartupPath;
            fullpath = Path.Combine(fullpath, fileName);
            if (!File.Exists(fullpath))
                throw new InvalidOperationException(string.Format(
                    resx.ConfigurationFileNotFound, fileName));
            try
            {
                var doc      = XDocument.Load(fullpath);
                var config   = doc.Descendants("configuration").FirstOrDefault();
                var settings = config.Descendants("appSettings").FirstOrDefault();

                SetValue(settings, "TerminalNumber", terminalNumber);
                SetValue(settings, "ParkCode", parkCode);
                SetValue(settings, "MinMatch", minMatch);
                SetValue(settings, "Timeout", timeout);

                doc.Save(fullpath);
                doc = null;
                GC.Collect();
            }
            catch { }
        }

        private void SetValue(XElement settings, string key, string value)
        {
            var setting = settings.Descendants().FirstOrDefault(p => p.Attribute("key").Value == key);
            if (setting != null)
                setting.Attribute("value").Value = value;
        }

        public bool TryLoad(string fileName, string applicationStartupPath)
        {
            try
            {
                Load(fileName, applicationStartupPath);
                return true;
            }
            catch
            {
                return false;
            }

        }
        public void Load(string fileName, string applicationStartupPath)
        {
            //Allow xml to override
            LoadXml(fileName, applicationStartupPath);
        }

        public void LoadRegistry(string path)
        {
            var appPath = path + @"\appSettings";
            var appSettings = Registry.LocalMachine.OpenSubKey(appPath);
            if (appSettings != null)
            {
                foreach (var key in appSettings.GetValueNames())
                {
                    var value = GetRegistryString(appSettings, key, string.Empty);
                    if (!_values.ContainsKey(key))
                        _values.Add(key, value);
                    else
                        _values[key] = value;
                }
            }

            var unityPath = path + @"\unitySettings";
            var unitySettings = Registry.LocalMachine.OpenSubKey(unityPath);
            if (unitySettings != null)
            {
                foreach (var typeName in unitySettings.GetValueNames())
                {
                    var mapTo = GetRegistryString(unitySettings, typeName, string.Empty);
                    var map = ParseUnityStrings(typeName, mapTo);
                    if (map != null)
                    {
                        if (!_maps.ContainsKey(typeName))
                            _maps.Add(map.TypeName, map);
                        else
                            _maps[typeName] = map;
                    }
                }
            }
        }

        private void LoadXml(string fileName, string applicationStartupPath)
        {
            var fullpath = applicationStartupPath;
            fullpath = Path.Combine(fullpath, fileName);
            if (!File.Exists(fullpath))
                throw new InvalidOperationException(string.Format(resx.ConfigurationFileNotFound, fileName));

            var doc = XDocument.Load(fullpath);
            var config = doc.Descendants("configuration").FirstOrDefault();
            if (config == null)
                throw new InvalidOperationException(
                    string.Format(resx.NoConfigurationSectionFound, fileName));

            ParseAppSettings(config);
            ParseUnityTypes(config);

            doc = null;
            GC.Collect();
        }

        private void ParseAppSettings(XElement config)
        {
            var settings = config.Descendants("appSettings");
            if (settings != null)
            {
                foreach (var setting in settings.Descendants())
                {
                    var key = ParseAttributeValue(setting, "key");
                    var value = ParseAttributeValue(setting, "value");

                    switch (setting.Name.LocalName)
                    {
                        case "add":
                            if (!_values.ContainsKey(key))
                                _values.Add(key, value);
                            else
                                _values[key] = value;
                            break;
                        case "remove":
                            if (_values.ContainsKey(key))
                                _values.Remove(key);
                            break;
                    }
                }
            }
        }

        private void ParseUnityTypes(XElement config)
        {
            var unity = config.Descendants("containers").FirstOrDefault();
            if (unity == null) return;
            foreach (var container in unity.Descendants("container"))
            {
                //TODO: Container specific
                foreach (var type in container.Descendants("types").Descendants("type"))
                {
                    var typeName = ParseAttributeValue(type, "type");
                    var mapTo = ParseAttributeValue(type, "mapTo");
                    var map = ParseUnityStrings(typeName, mapTo);
                    if (map != null)
                    {
                        if (!_maps.ContainsKey(typeName))
                            _maps.Add(map.TypeName, map);
                        else
                            _maps[typeName] = map;
                    }
                }
            }
        }

        private static string ParseAttributeValue(XElement element, string attributeName)
        {
            var attributes = element.Attributes(attributeName).ToArray();
            if (attributes == null || attributes.Length == 0)
                throw new InvalidOperationException(
                    string.Format(resx.AttributeDoesNotExistForConfigurationEntry, attributeName, element.Name.LocalName));

            return attributes[0].Value;
        }

        private static UnityObjectMap ParseUnityStrings(string typeName, string objectName)
        {
            var mapTo = objectName.Split(',');
            if (mapTo.Length < 2) return null;
            var classname = mapTo[0].Trim();
            var assembly  = mapTo[1].Trim();
            return new UnityObjectMap(typeName, classname, assembly);
        }

        public IEnumerable<KeyValuePair<string, string>> GetValues()
        {
            return _values.Select(p => new KeyValuePair<string, string>(p.Key, p.Value));
        }

        public string GetValue(string key)
        {
            return GetValue(key, null);
        }
        public string GetValue(string key, string defaultValue)
        {
            return _values.ContainsKey(key) ? _values[key] : defaultValue;
        }

        public static Type GetType(string combinedTypeAssemblyString)
        {
            if (string.IsNullOrEmpty(combinedTypeAssemblyString))
                throw new ArgumentNullException("typeString");

            var parts = combinedTypeAssemblyString.Split(',');
            if (string.IsNullOrEmpty(parts[0]))
                throw new ArgumentOutOfRangeException("typeString", "missing type name");

            if (string.IsNullOrEmpty(parts[1]))
                throw new ArgumentOutOfRangeException("typeString", "missing assembly name");

            var typeName = parts[0].Trim();
            var assemblyString = parts[1].Trim();
            return GetType(typeName, assemblyString);
        }

        public static Type GetType(string typeName, string assemblyString)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName");
            if (string.IsNullOrEmpty(assemblyString))
                throw new ArgumentNullException("assemblyString");

            Type result = null;
            var assemblyName = new AssemblyName();
            if (assemblyString.EndsWith(".dll") || assemblyString.EndsWith(".exe"))
                assemblyName.CodeBase = assemblyString;
            else
                assemblyName.Name = assemblyString;

            try
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                    result = assembly.GetType(typeName);
            }
            catch { }
            return result;
        }
        public Type GetMappedType(string mappedTypename)
        {
            Type result = null;
            if (_maps.Keys.Contains(mappedTypename))
            {
                var map = _maps[mappedTypename];
                result = GetType(map.ClassName, map.AssemblyName);
            }
            return result;
        }
        public T CreateObject<T>() where T : class
        {
            var tn = typeof(T).FullName;
            if (_maps.ContainsKey(tn))
            {
                var map = _maps[tn];
                var asm = GetAssembly(map.AssemblyName);
                if (asm != null)
                {
                    var inst = CreateInstance<T>(map.ClassName, asm);
                    return inst;
                }
            }
            return null;
        }

        private static Assembly GetAssembly(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            var fullname = Path.Combine(ExecutablePath(), name);
            Assembly asm = null;
            try
            {
                asm = Assembly.LoadFrom(fullname);
            }
            catch { }

            return asm;
        }

        private static T CreateInstance<T>(string name, Assembly assembly) where T : class
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            object inst = null;
            try
            {
                inst = assembly.CreateInstance(name);
            }
            catch { }
            return inst as T;
        }

        public static string ExecutablePath()
        {
            return Path.GetDirectoryName(Assembly.GetCallingAssembly().ManifestModule.FullyQualifiedName);
        }

        public static string ExecutablePath(string fileName)
        {
            return Path.Combine(ExecutablePath(), fileName);
        }

        public static string GetRegistryString(RegistryKey key, string name, string defaultValue)
        {
            var keyValue = defaultValue;
            try
            {
                var kind = key.GetValueKind(name);
                var value = key.GetValue(name);

                if (kind == RegistryValueKind.String)
                {
                    keyValue = value.ToString();
                }
                else if (kind == RegistryValueKind.MultiString)
                {
                    keyValue = ((string[])value)[0];
                }
            }
            catch { }
            return keyValue;
        }
    }

    internal class UnityObjectMap
    {
        public string TypeName     { get; set; }
        public string ClassName    { get; set; }
        public string AssemblyName { get; set; }

        internal UnityObjectMap(string type, string classname, string assmebly)
        {
            TypeName     = type;
            ClassName    = classname;
            AssemblyName = assmebly;
        }
    }
}
