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

        public bool Dispatch(Type type, object handler, object callback,
            bool greedy, IHandler composer)
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
                    var handled = definition.Dispatch(handler, callback, composer);
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

        public const BindingFlags Binding =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
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

        protected bool SatisfiesInvariantOrContravariant(object callback)
        {
            var callbackType = callback.GetType();
            return (Invariant && (CallbackType == callbackType)) ||
                   CallbackType.IsInstanceOfType(callback) ||
                   SatisfiesGenericDef(callback.GetType());
        }

        protected bool SatisfiesCovariant(Type type)
        {
            if (type == null) return false;
            var callbackType = CallbackType;
            return callbackType == null || 
                (callbackType.IsGenericType && callbackType.ContainsGenericParameters
                    ? SatisfiesGenericDef(type)
                    : type.IsAssignableFrom(callbackType));
        }

        private bool SatisfiesGenericDef(Type callbackType)
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

    #region Handles Definition

    public class HandlesAttribute : ContravariantDefinition
    {
        private bool _returnsBool;
        private bool _passComposer;
        private Delegate _delegate;

        protected override bool Configure(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 2)
            {
                if (!typeof(IHandler).IsAssignableFrom(parameters[1].ParameterType))
                    return false;
                _passComposer = true;
            }
            else if (parameters.Length != 1)
                return false;

            var returnType = method.ReturnType;
            if (returnType == typeof (bool))
                _returnsBool = true;
            else if (returnType != typeof (void))
                return false;

            if (!ConfigureCallbackType(method, parameters[0].ParameterType))
                return false;

            if (!method.ContainsGenericParameters)
            {
                if (_returnsBool)
                {
                    _delegate = _passComposer
                              ? ReflectionHelper.CreateFuncTwoArgs(method)
                              : (Delegate)ReflectionHelper.CreateFuncOneArg(method);
                }
                else
                    _delegate = _passComposer
                              ? ReflectionHelper.CreateActionTwoArgs(method)
                              : (Delegate)ReflectionHelper.CreateActionOneArg(method);
            }

            return true;
        }

        protected internal override bool Dispatch(
            object handler, object callback, IHandler composer)
        {
            if (!SatisfiesInvariantOrContravariant(callback))
                return false;

            if (_delegate != null)
            {
                if (_returnsBool)
                    return _passComposer
                        ? (bool) ((Func<object, object, object, object>) _delegate)(handler, callback, composer)
                        : (bool) ((Func<object, object, object>) _delegate)(handler, callback);

                if (_passComposer)
                    ((Action<object, object, object>) _delegate)(handler, callback, composer);
                else
                    ((Action<object, object>)_delegate)(handler, callback);
                return true;
            }

            var args   = _passComposer
                       ? new[] { callback, composer }
                       : new[] { callback };
            var result = InvokeMethod(handler, callback.GetType(), args);

            return !_returnsBool || (bool)result;
        }
    }

    #endregion

    #region Provides Definition

    public class ProvidesAttribute : CovariantDefinition
    {
        private bool _passResolution;
        private bool _passComposer;
        private bool _isVoid;
        private Delegate _delegate;

        public object Key { get; set; }

        protected override bool Configure(MethodInfo method)
        {
            var returnType = method.ReturnType;
            var parameters = method.GetParameters();

            _isVoid  = returnType == typeof(void);

            switch (parameters.Length)
            {
                case 2:
                    if (!typeof(Resolution).IsAssignableFrom(parameters[0].ParameterType) ||
                        !typeof(IHandler).IsAssignableFrom(parameters[1].ParameterType))
                        return false;
                    _passComposer = _passResolution = true;
                    break;
                case 1:
                    if (typeof (Resolution).IsAssignableFrom(parameters[0].ParameterType))
                        _passResolution = true;
                    else if (!_isVoid && typeof (IHandler).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        _passComposer = true;
                        ConfigureCallbackType(method, returnType);
                    }
                    else
                        return false;
                    break;
                default:
                    if (_isVoid || parameters.Length != 0 || !ConfigureCallbackType(method, returnType))
                        return false;
                    break;
            }

            if (!method.ContainsGenericParameters)
            {
                if (_passResolution)
                {
                    if (_passComposer)
                        _delegate = _isVoid 
                                  ? ReflectionHelper.CreateActionTwoArgs(method)
                                  : (Delegate)ReflectionHelper.CreateFuncTwoArgs(method);
                    else
                        _delegate = _isVoid 
                                  ? ReflectionHelper.CreateActionOneArg(method)
                                  : (Delegate)ReflectionHelper.CreateFuncOneArg(method);
                }
                else
                    _delegate = _passComposer
                              ? ReflectionHelper.CreateFuncOneArg(method)
                              : (Delegate)ReflectionHelper.CreateFuncNoArgs(method);
            }

            return true;
        }

        protected internal override bool Dispatch(
            object handler, object callback, IHandler composer)
        {
            var resolution = callback as Resolution;
            if (resolution == null)
                return false;

            var typeKey = resolution.Key as Type;
            if (!SatisfiesCovariant(typeKey))
                return false;

            object result   = null;
            var resolutions = resolution.Resolutions;
            var count       = resolutions.Count;

            if (_delegate != null)
            {
                if (_passResolution)
                {
                    if (_passComposer)
                    {
                        if (_isVoid)
                            ((Action<object, object, object>)_delegate)(handler, callback, composer);
                        else
                            result = ((Func<object, object, object, object>)_delegate)(handler, callback, composer);
                    }
                    else if (_isVoid)
                        ((Action<object, object>)_delegate)(handler, callback);
                    else
                        result = ((Func<object, object, object>)_delegate)(handler, callback);
                }
                else
                    result = _passComposer
                        ? ((Func<object, object, object>) _delegate)(handler, composer)
                        : ((Func<object, object>) _delegate)(handler);
            }
            else
            {
                var parameters = _passResolution && _passComposer
                               ? new[] { callback, composer }
                               : _passResolution ? new[] { callback }
                               : _passComposer ? new[] { composer }
                               : null;
                result = InvokeMethod(handler, typeKey, parameters);
            }

            if (result != null)
            {
                if (Invariant && CallbackType != null &&
                    (CallbackType != result.GetType()))
                    return false;
                resolution.Resolve(result);
            }

            return resolutions.Count > count;
        }
    }

    #endregion
}
