using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Miruken.Infrastructure;

namespace Miruken.Callback
{
    public static class HandlerMetadata
    {
        private static readonly ConcurrentDictionary<Type, HandlerDescriptor>
            _descriptors = new ConcurrentDictionary<Type, HandlerDescriptor>();

        public static HandlerDescriptor GetDescriptor(Type type)
        {
            return _descriptors.GetOrAdd(type, t => new HandlerDescriptor(t));
        }

        public static bool Dispatch(Type definition, Handler handler, object callback, bool greedy, IHandler composer)
        {
            var handled   = false;
            var surrogate = handler.Surrogate;

            if (surrogate != null)
            {
                var descriptor = GetDescriptor(surrogate.GetType());
                handled = descriptor.Dispatch(definition, surrogate, callback, greedy, composer);
            }

            if (!handled || greedy)
            {
                var descriptor = GetDescriptor(handler.GetType());
                handled = descriptor.Dispatch(definition, handler, callback, greedy, composer)
                       || handled;
            }

            return handled;
        }
    }

    public class HandlerDescriptor
    {
        private readonly Dictionary<Type, List<DefinitionAttribute>> _definitions;

        public HandlerDescriptor(Type type)
        {
            foreach (var method in type.GetMethods(Binding))
            {
                if (method.IsSpecialName || method.IsFamily ||
                    method.DeclaringType == typeof (object))
                    continue;

                var definitions = (DefinitionAttribute[])
                    Attribute.GetCustomAttributes(method, typeof (DefinitionAttribute));

                foreach (var definition in definitions)
                {
                    if (!definition.Accepts(method)) continue;

                    if (_definitions == null)
                        _definitions = new Dictionary<Type, List<DefinitionAttribute>>();

                    List<DefinitionAttribute> members;
                    var definitionType = definition.GetType();
                    if (!_definitions.TryGetValue(definitionType, out members))
                    {
                        members = new List<DefinitionAttribute>();
                        _definitions.Add(definitionType, members);
                    }

                    for (var index = 0; index <= members.Count; ++index)
                    {
                        // maintain partial ordering by variance
                        if (definition.Untyped || index >= members.Count)
                            members.Add(definition);
                        else if (definition.CompareTo(members[index]) < 0)
                            members.Insert(index, definition);
                        else continue;
                        break;
                    }
                }
            }
        }

        internal bool Dispatch(Type type, object target, object callback, bool greedy, IHandler composer)
        {
            if (callback == null) return false;

            List<DefinitionAttribute> definitions;
            if (_definitions == null ||
                !_definitions.TryGetValue(type, out definitions))
                return false;

            var dispatched   = false;
            var oldUnhandled = HandleMethod.Unhandled;

            try
            {
                foreach (var definition in definitions)
                {
                    HandleMethod.Unhandled = false;
                    var handled = definition.Dispatch(target, callback, composer);
                    dispatched = (handled && !HandleMethod.Unhandled) || dispatched;
                    if (dispatched && !greedy)
                        return true;
                }
            }
            finally
            {
                HandleMethod.Unhandled = oldUnhandled;
            }

            return dispatched;
        }

        public const BindingFlags Binding = BindingFlags.Instance 
             | BindingFlags.Public | BindingFlags.NonPublic;
    }

    [AttributeUsage(AttributeTargets.Method,
         AllowMultiple = true, Inherited = false)]
    public abstract class DefinitionAttribute 
        : Attribute, IComparable<DefinitionAttribute>
    {
        private int[] _typeMapping;
        private Type _genericCallbackTypeDef;

        public bool Invariant  { get; set; }

        public Type CallbackType { get; private set; }

        protected MethodInfo Method { get; private set; }

        internal bool Accepts(MethodInfo method)
        {
            if (!Configure(method)) return false;
            Method = method;
            return true;
        }

        public bool Untyped => CallbackType == null || CallbackType == typeof(object);

        protected abstract bool Configure(MethodInfo method);

        public abstract int CompareTo(DefinitionAttribute other);

        protected internal abstract bool Dispatch(
            object handler, object callback, IHandler composer);

        protected bool ConfigureCallbackType(MethodInfo method, Type callbackType)
        {
            if (!GetGenericArgMapping(method, callbackType)) return false;
            if (callbackType.IsGenericType && callbackType.ContainsGenericParameters)
                _genericCallbackTypeDef = callbackType.GetGenericTypeDefinition();
            CallbackType = callbackType;
            return true;
        }

        protected bool SatisfiesGenericDef(Type callbackType)
        {
            return _genericCallbackTypeDef != null && callbackType.IsGenericType &&
                   _genericCallbackTypeDef == callbackType.GetGenericTypeDefinition();
        }

        protected object InvokeMethod(object target, Type callbackType, object[] args)
        {
            var method = Method;
            if (callbackType != null && method.ContainsGenericParameters)
            {
                var types    = callbackType.GetGenericArguments();
                var argTypes = _typeMapping?.Select(i => types[i]).ToArray() ?? types;
                method = method.MakeGenericMethod(argTypes);
            }
            return method.Invoke(target, HandlerDescriptor.Binding,
                null, args, CultureInfo.InvariantCulture);
        }

        private bool GetGenericArgMapping(MethodBase method, Type type)
        {
            if (!method.IsGenericMethodDefinition) return true;
            if (!type.ContainsGenericParameters) return false;
            var methodArgs = method.GetGenericArguments();
            var typeArgs   = type.GetGenericArguments();
            if (methodArgs.Length < typeArgs.Length) return false;
            bool valid = true, match = true;
            _typeMapping = methodArgs.Select((a, i) =>
            {
                var index = Array.IndexOf(typeArgs, a);
                valid = valid && (index >= 0);
                match = match && (index == i);
                return index;
            }).ToArray();
            if (match) _typeMapping = null;
            return valid;
        }
    }

    public abstract class ContravariantDefinition : DefinitionAttribute
    {
        protected bool SatisfiesInvariantOrContravariant(object callback)
        {
            var callbackType = callback.GetType();
            return (Invariant && (CallbackType == callbackType)) ||
                   CallbackType.IsInstanceOfType(callback) ||
                   SatisfiesGenericDef(callback.GetType());
        }

        public override int CompareTo(DefinitionAttribute other)
        {
            var otherHandler = other as ContravariantDefinition;
            if (otherHandler == null) return -1;
            if (otherHandler.CallbackType == CallbackType)
                return 0;
            if (CallbackType.IsAssignableFrom(otherHandler.CallbackType))
                return 1;
            return -1;
        }        
    }

    public abstract class CovariantDefinition : DefinitionAttribute
    {
        protected bool SatisfiesCovariantType(Type type)
        {
            if (type == null) return false;
            var callbackType = CallbackType;
            return callbackType == null || callbackType == typeof(object) ||
                (callbackType.IsGenericType && callbackType.ContainsGenericParameters
                    ? SatisfiesGenericDef(type)
                    : type.IsAssignableFrom(callbackType));
        }

        protected bool SatisfiesCovariant(object instance)
        {
            var type = instance.GetType();
            var callbackType = CallbackType;
            if (callbackType == null) return true;
            return Invariant ? callbackType == type
                 : callbackType.IsInstanceOfType(instance) 
                || SatisfiesGenericDef(type);
        }

        public override int CompareTo(DefinitionAttribute other)
        {
            var otherProvider = other as CovariantDefinition;
            if (otherProvider == null) return -1;
            if (otherProvider.CallbackType == CallbackType)
                return 0;
            if (CallbackType == null ||
                !CallbackType.IsAssignableFrom(otherProvider.CallbackType))
                return 1;
            return -1;
        }
    }
}
