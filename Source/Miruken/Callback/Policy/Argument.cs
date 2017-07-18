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
        object ResolveArgument(Argument argument, IHandler handler);
    } 

    public class Argument
    {
        public Argument(ParameterInfo parameter)
        {
            Parameter     = parameter;
            ArgumentType  = parameter.ParameterType;
            LogicalType   = ExtractFlags(ArgumentType);
            Attributes    = Attribute.GetCustomAttributes(parameter, false);
            if (Attributes.Length == 0)
                Attributes = Array.Empty<Attribute>();
            Key      = IsSimple ? Parameter.Name : (object)LogicalType;
            Resolver = Attributes.OfType<IArgumentResolver>().SingleOrDefault();
        }

        public object            Key           { get; }
        public ParameterInfo     Parameter     { get; }
        public Type              ArgumentType  { get; }
        public Type              LogicalType   { get; }
        public Attribute[]       Attributes    { get; }
        public IArgumentResolver Resolver      { get; }

        public bool          IsLazy        { get; private set; }
        public bool          IsArray       { get; private set; }
        public bool          IsSimple      { get; private set; }
        public bool          IsPromise     { get; private set; }
        public bool          IsTask        { get; private set; }

        private Type ExtractFlags(Type parameterType)
        {
            var type  = parameterType;
            IsLazy    = ExtractLazy(ref type);
            IsPromise = ExtractPromise(ref type);
            IsTask    = ExtractTask(ref type);
            IsArray   = ExtractArray(ref type);
            IsSimple  = type.IsSimpleType();
            return type;
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
