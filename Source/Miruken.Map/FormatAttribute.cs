namespace Miruken.Map
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        Inherited = false)]
    public class FormatAttribute : Attribute, IFormatMatching
    {
        public FormatAttribute(object format)
        {
            Format = format;
            if (format == null)
                throw new ArgumentNullException(nameof(format));
        }

        public object Format { get; }

        public bool Matches(object format)
        {
            return Equals(Format, format);
        }
    }
}
