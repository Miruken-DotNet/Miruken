﻿namespace Miruken.Callback.Policy
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
            None       = 0,
            Array      = 1,
            Enumerable = 1 << 1,
            Simple     = 1 << 2,
            Strict     = 1 << 3,
            Promise    = 1 << 4,
            Task       = 1 << 5,
            Optional   = 1 << 6,
            Maybe      = 1 << 7
        }

        public Argument(ParameterInfo parameter)
        {
            var flags = Flags.None;
            Parameter = parameter;
            if (parameter.IsOptional)
                flags |= Flags.Optional;

            Attributes = Attribute.GetCustomAttributes(parameter, false);

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
                        case StrictAttribute _:
                            flags |= Flags.Strict;
                            break;
                        case OptionalAttribute _:
                            flags |= Flags.Optional;
                            break;
                        case IArgumentResolver resolver:
                            if (Resolver != null)
                                throw new InvalidOperationException(
                                    $"Only one IArgumentResolver allowed for {parameter.Member}");
                            Resolver = resolver;
                            if (resolver.IsOptional) flags |= Flags.Optional;
                            break;
                        case ConstraintAttribute constraint:
                            Constraints += b => b.Require(constraint);
                            break;
                    }        
                }
            }
            
            if (Resolver == null)
            {
                if (ParameterType.IsInterface && ParameterType.Is<IProtocol>())
                    Resolver = ProxyAttribute.Instance;
                else
                {
                    var optionsType = ParameterType.GetOpenTypeConformance(typeof(Options<>));
                    if (optionsType != null && ParameterType.HasDefaultConstructor())
                    {
                        Resolver = OptionsAttribute.Instance;
                        flags |= Flags.Optional;
                    }
                }
            }

            ArgumentFlags = ExtractFlags(ParameterType, flags);

            Key ??= IsSimple ? Parameter.Name : (object) LogicalType;

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
        public bool IsArray       => ArgumentFlags.HasFlag(Flags.Array);
        public bool IsEnumerable  => ArgumentFlags.HasFlag(Flags.Enumerable);
        public bool IsSimple      => ArgumentFlags.HasFlag(Flags.Simple);
        public bool IsStrict      => ArgumentFlags.HasFlag(Flags.Strict);
        public bool IsPromise     => ArgumentFlags.HasFlag(Flags.Promise);
        public bool IsTask        => ArgumentFlags.HasFlag(Flags.Task);
        public bool IsOptional    => ArgumentFlags.HasFlag(Flags.Optional);
        public bool IsMaybe       => ArgumentFlags.HasFlag(Flags.Maybe);

        public bool IsInstanceOf(object argument) =>
            ParameterType.IsInstanceOfType(argument);

        public bool GetDefaultValue(out object value)
        {
            if (Parameter.HasDefaultValue)
                value = Parameter.RawDefaultValue;
            else if (IsOptional)
                value = RuntimeHelper.GetDefault(ParameterType);
            else
            {
                value = null;
                return false;
            }
            return true;
        }

        private Flags ExtractFlags(Type parameterType, Flags flags)
        {
            var type = parameterType;
            if (ExtractMaybe(ref type))
                flags |= Flags.Maybe;
            ArgumentType = type;
            if (ExtractPromise(ref type))
                flags |= Flags.Promise;
            if (ExtractTask(ref type))
                flags |= Flags.Task;
            if (!flags.HasFlag(Flags.Strict))
            {
                if (ExtractArray(ref type))
                    flags |= Flags.Array | Flags.Enumerable;
                else if (ExtractEnumerable(ref type))
                    flags |= Flags.Enumerable;
            }
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

        private static bool ExtractEnumerable(ref Type type)
        {
            if (type == typeof(string) ||
                !type.IsGenericEnumerable()) return false;
            type = type.GetGenericArguments().Single();
            return true;
        }
    }
}
