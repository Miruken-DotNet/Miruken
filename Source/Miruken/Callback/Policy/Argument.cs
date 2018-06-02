namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public interface IArgumentResolver
    {
        bool IsOptional { get;  }

        void ValidateArgument(Argument argument);

        object ResolveArgument(
            Argument argument, IHandler handler, IHandler composer);
    } 

    public class Argument
    {
        public Argument(ParameterInfo parameter)
        {
            Parameter     = parameter;
            ParameterType = parameter.ParameterType;
            ExtractFlags(ParameterType);
            Attributes    = Attribute.GetCustomAttributes(parameter, false);
            if (Attributes.Length > 0)
            {
                var key = Attributes.OfType<KeyAttribute>().SingleOrDefault();
                if (key != null) Key = key.Key;
                Resolver = Attributes.OfType<IArgumentResolver>().SingleOrDefault();
                Optional = Resolver?.IsOptional == true
                        || Attributes.OfType<OptionalAttribute>().Any();
            }
            else
            {
                Attributes = Array.Empty<Attribute>();
            }
            if (Resolver == null && ParameterType.IsInterface &&
                (ParameterType.Is<IProtocol>() || ParameterType.Is<IResolving>()))
            {
                Resolver = ProxyAttribute.Instance;
            }
            if (Key == null)
                Key = IsSimple ? Parameter.Name : (object)LogicalType;
            Resolver?.ValidateArgument(this);
        }

        public object            Key           { get; }
        public ParameterInfo     Parameter     { get; }
        public Type              ParameterType { get; }
        public Type              ArgumentType  { get; private set; }
        public Type              LogicalType   { get; private set; }
        public Attribute[]       Attributes    { get; }
        public IArgumentResolver Resolver      { get; }
        public bool              Optional      { get; }

        public bool              IsLazy        { get; private set; }
        public bool              IsArray       { get; private set; }
        public bool              IsSimple      { get; private set; }
        public bool              IsPromise     { get; private set; }
        public bool              IsTask        { get; private set; }

        public bool IsInstanceOf(object argument) =>
            ParameterType.IsInstanceOfType(argument);

        private void ExtractFlags(Type parameterType)
        {
            var type     = parameterType;
            IsLazy       = ExtractLazy(ref type);
            ArgumentType = type;
            IsPromise    = ExtractPromise(ref type);
            IsTask       = ExtractTask(ref type);
            IsArray      = ExtractArray(ref type);
            IsSimple     = type.IsSimpleType();
            LogicalType  = type;
        }

        private static bool ExtractPromise(ref Type type)
        {
            var promise = type.GetOpenTypeConformance(typeof(Promise<>));
            if (promise == null) return false;
            type = promise.GetGenericArguments()[0];
            return true;
        }

        private static bool ExtractTask(ref Type type)
        {
            var task = type.GetOpenTypeConformance(typeof(Task<>));
            if (task == null) return false;
            type = task.GetGenericArguments()[0];
            return true;
        }

        private static bool ExtractArray(ref Type type)
        {
            if (!type.IsArray) return false;
            type = type.GetElementType();
            return true;
        }

        private static bool ExtractLazy(ref Type type)
        {
            if (!type.IsGenericType ||
                type.GetGenericTypeDefinition() != typeof(Func<>))
                return false;
            type = type.GetGenericArguments()[0];
            return true;
        }
    }
}
