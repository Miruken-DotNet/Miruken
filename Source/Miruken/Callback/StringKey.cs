namespace Miruken.Callback
{
    using System;

    public class StringKey
    {
        public StringKey(string key, 
            StringComparison comparison = StringComparison.Ordinal)
        {
            Key = key;
            Comparison = comparison;
        }

        public string           Key        { get; }
        public StringComparison Comparison { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            var rawString = obj as string;
            if (rawString == null && obj != null)
            {
                var stringKey = obj as StringKey;
                if (stringKey == null) return false;
                rawString = stringKey.Key;
            }

            return ReferenceEquals(rawString, Key) ||
                string.Compare(Key, rawString, Comparison) == 0;
        }

        public override int GetHashCode()
        {
            return Key?.GetHashCode() ?? 0;
        }
    }
}
