namespace Miruken.Map
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class FormatAttribute : Attribute, IFormatMatching
    {
        public FormatAttribute(object format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            Format = format;
        }

        public object Format { get; }

        public bool Matches(object format)
        {
            return Equals(Format, format);
        }
    }
}
