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

        public object Key { get; set; }

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

            var keyType      = Key as Type;
            var callbackType = parameters[0].ParameterType;
            if ((keyType != null && !callbackType.IsAssignableFrom(keyType)) ||
                !ConfigureCallbackType(method, keyType ?? callbackType))
                return false;

            if (!method.ContainsGenericParameters)
            {
                if (_returnsBool)
                {
                    _delegate = _passComposer
                              ? RuntimeHelper.CreateFuncTwoArgs(method)
                              : (Delegate)RuntimeHelper.CreateFuncOneArg(method);
                }
                else
                    _delegate = _passComposer
                              ? RuntimeHelper.CreateActionTwoArgs(method)
                              : (Delegate)RuntimeHelper.CreateActionOneArg(method);
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
                        ? (bool) ((TwoArgsReturnDelegate)_delegate)(handler, callback, composer)
                        : (bool) ((OneArgReturnDelegate)_delegate)(handler, callback);

                if (_passComposer)
                    ((TwoArgsDelegate)_delegate)(handler, callback, composer);
                else
                    ((OneArgDelegate)_delegate)(handler, callback);
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
            var keyType      = Key as Type;
            var returnType   = method.ReturnType;
            var callbackType = returnType;
            if (callbackType.IsArray)
                callbackType = returnType.GetElementType();
            if (keyType != null)
            {
                if (!callbackType.IsAssignableFrom(keyType))
                    return false;
                callbackType = keyType;
            }

            _isVoid = returnType == typeof(void);

            var parameters = method.GetParameters();

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
                        ConfigureCallbackType(method, callbackType);
                    }
                    else
                        return false;
                    break;
                default:
                    if (_isVoid || parameters.Length != 0 ||
                        !ConfigureCallbackType(method, callbackType))
                        return false;
                    break;
            }

            if (!method.ContainsGenericParameters)
            {
                if (_passResolution)
                {
                    if (_passComposer)
                        _delegate = _isVoid 
                                  ? RuntimeHelper.CreateActionTwoArgs(method)
                                  : (Delegate)RuntimeHelper.CreateFuncTwoArgs(method);
                    else
                        _delegate = _isVoid 
                                  ? RuntimeHelper.CreateActionOneArg(method)
                                  : (Delegate)RuntimeHelper.CreateFuncOneArg(method);
                }
                else
                    _delegate = _passComposer
                              ? RuntimeHelper.CreateFuncOneArg(method)
                              : (Delegate)RuntimeHelper.CreateFuncNoArgs(method);
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
                            ((TwoArgsDelegate)_delegate)(handler, callback, composer);
                        else
                            result = ((TwoArgsReturnDelegate)_delegate)(handler, callback, composer);
                    }
                    else if (_isVoid)
                        ((OneArgDelegate)_delegate)(handler, callback);
                    else
                        result = ((OneArgReturnDelegate)_delegate)(handler, callback);
                }
                else
                    result = _passComposer
                           ? ((OneArgReturnDelegate)_delegate)(handler, composer)
                           : ((NoArgsReturnDelegate)_delegate)(handler);
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
                var array = result as object[];
                if (array != null)
                {
                    var resolved = false;
                    foreach (var item in array)
                    {
                        resolved = IsSatisfied(item) &&
                                   resolution.Resolve(item, composer)
                                || resolved;
                        if (resolved && !resolution.Many)
                            break;
                    }
                    return resolved;
                }
                return IsSatisfied(result) &&
                    resolution.Resolve(result, composer);
            }

            return resolutions.Count > count;
        }

        private bool IsSatisfied(object result)
        {
            return !Invariant || CallbackType == null || (CallbackType == result.GetType());
        }
    }

    #endregion
}
