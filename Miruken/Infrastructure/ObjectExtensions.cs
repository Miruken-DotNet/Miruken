namespace Miruken.Infrastructure
{
    using System.Collections.Generic;

    public static class ObjectExtensions
    {
        public static Dictionary<string, object> ToPropertiesDictionary(this object obj)
        {
            var dct = new Dictionary<string, object>();
            if (obj == null) return dct;
            foreach (var prop in obj.GetType().GetProperties())
            {
                var key = prop.Name;
                var value = prop.GetValue(obj, null);
                dct[key] = value;
            }
            return dct;
        }
    }
}
