namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Bindings;
    using Concurrency;
    using Functional;
    using Infrastructure;

    public class Argument
    {
        [Flags]
        public enum Flags
        {
            None     = 0,
            Lazy     = 1 << 0,
            Array    = 1 << 1,
            Simple   = 1 << 2,
            Promise  = 1 << 3,
            Task     = 1 << 4,
            Optional = 1 << 5,
            Maybe    = 1 << 6
        }

        public Argument(ParameterInfo parameter)
        {
            Parameter     = parameter;
            ArgumentFlags = ExtractFlags(ParameterType);
            Attributes    = Attribute.GetCustomAttributes(parameter, false);

            if (Attributes.Length == 0)
                Attributes = Array.Empty<Attribute>();
            else
            {
                foreach (var attribute in Attributes)
                {
                    switch (attribute)
                    {
                        case KeyAttribute key:
                            if (Key != null)
                                throw new InvalidOperationException(
                                    $"Only one KeyAttribute allowed for {parameter.Member}");
                            Key = key.Key;
                            break;
                        case IArgumentResolver resolver:
                            if (Resolver != null)
                                throw new InvalidOperationException(
                                    $"Only one IArgumentResolver allowed for {parameter.Member}");
                            Resolver = resolver;
                            break;
                        case ConstraintAttribute constraint:
                            Constraints += b => b.Require(constraint);
                            break;
                    }        
                }
            }
            if (Resolver?.IsOptional == true ||
                Attributes.OfType<OptionalAttribute>().Any())
                ArgumentFlags |= Flags.Optional;

            if (Resolver == null && ParameterType.IsInterface &&
                (ParameterType.Is<IProtocol>() || ParameterType.Is<IResolving>()))
                Resolver = ProxyAttribute.Instance;

            if (Key == null)
                Key = IsSimple ? Parameter.Name : (object)LogicalType;

            Resolver?.ValidateArgument(this);
        }

        public object                    Key           { get; }
        public ParameterInfo             Parameter     { get; }
        public Flags                     ArgumentFlags { get; }
        public Type                      ArgumentType  { get; private set; }
        public Type                      LogicalType   { get; private set; }
        public Attribute[]               Attributes    { get; }
        public IArgumentResolver         Resolver      { get; }
        public Action<ConstraintBuilder> Constraints   { get; }

        public Type ParameterType => Parameter.ParameterType;
        public bool IsLazy        => ArgumentFlags.HasFlag(Flags.Lazy);
        public bool IsArray       => ArgumentFlags.HasFlag(Flags.Array);
        public bool IsSimple      => ArgumentFlags.HasFlag(Flags.Simple);
        public bool IsPromise     => ArgumentFlags.HasFlag(Flags.Promise);
        public bool IsTask        => ArgumentFlags.HasFlag(Flags.Task);
        public bool IsOptional    => ArgumentFlags.HasFlag(Flags.Optional);
        public bool IsMaybe       => ArgumentFlags.HasFlag(Flags.Maybe);

        public bool IsInstanceOf(object argument) =>
            ParameterType.IsInstanceOfType(argument);

        private Flags ExtractFlags(Type parameterType)
        {
            var flags = Flags.None;
            var type  = parameterType;
            if (ExtractMaybe(ref type))
                flags |= Flags.Maybe;
            if (ExtractLazy(ref type))
                flags |= Flags.Lazy;
            ArgumentType = type;
            if (ExtractPromise(ref type))
                flags |= Flags.Promise;
            if (ExtractTask(ref type))
                flags |= Flags.Task;
            if (ExtractArray(ref type))
                flags |= Flags.Array;
            if (type.IsSimpleType())
                flags |= Flags.Simple;
            LogicalType = type;
            return flags;
        }

        private static bool ExtractMaybe(ref Type type)
        {
            var promise = type.GetOpenTypeConformance(typeof(Maybe<>));
            if (promise == null) return false;
            type = promise.GetGenericArguments()[0];
            return true;
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
