namespace Miruken.Infrastructure
{
    using System;
    using System.Text;

    public static class ReflectionHelper
    {
        public static string GetSimpleTypeName(Type t)
        {
            var fullyQualifiedTypeName = t.AssemblyQualifiedName;
            return RemoveAssemblyDetails(fullyQualifiedTypeName);
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var builder = new StringBuilder();

            var writingAssemblyName     = false;
            var skippingAssemblyDetails = false;

            foreach (var current in fullyQualifiedTypeName)
            {
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                            skippingAssemblyDetails = true;
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                            builder.Append(current);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
