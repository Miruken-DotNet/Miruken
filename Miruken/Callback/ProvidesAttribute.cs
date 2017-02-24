namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Infrastructure;

    public class ProvidesAttribute : CovariantDefinition
    {
        private bool _passResolution;
        private bool _passComposer;
        private bool _isVoid;
        private Delegate _delegate;

        public ProvidesAttribute()
        {
        }

        public ProvidesAttribute(object key)
        {
            Key = key;
        }

        public object Key { get; }

        protected override bool Configure(MethodInfo method)
        {
            var keyType = Key as Type;
            var callbackType = keyType;
            var returnType = method.ReturnType;
            _isVoid = returnType == typeof(void);

            if (!_isVoid)
            {
                if (returnType.IsArray)
                    returnType = returnType.GetElementType();
                if (keyType == null)
                    callbackType = returnType;
                else if (!returnType.IsAssignableFrom(keyType))
                    return false;
            }

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
                    if (typeof(Resolution).IsAssignableFrom(parameters[0].ParameterType))
                        _passResolution = true;
                    else if (!_isVoid && typeof(IHandler).IsAssignableFrom(parameters[0].ParameterType))
                        _passComposer = true;
                    else
                        return false;
                    break;
                default:
                    if (_isVoid || parameters.Length != 0)
                        return false;
                    break;
            }

            if (callbackType != null && !ConfigureCallbackType(method, callbackType))
                return false;

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
            if (resolution == null) return false;

            var typeKey = resolution.Key as Type;
            if (!SatisfiesCovariantType(typeKey))
                return false;

            object result = null;
            var resolutions = resolution.Resolutions;
            var count = resolutions.Count;

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
                        resolved = SatisfiesCovariant(item) &&
                                   resolution.Resolve(item, composer)
                                || resolved;
                        if (resolved && !resolution.Many)
                            break;
                    }
                    return resolved;
                }
                return SatisfiesCovariant(result) &&
                    resolution.Resolve(result, composer);
            }

            return resolutions.Count > count;
        }
    }
}
