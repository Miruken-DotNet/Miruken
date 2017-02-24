namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Infrastructure;

    public class HandlesAttribute : ContravariantDefinition
    {
        private bool _returnsBool;
        private bool _passComposer;
        private Delegate _delegate;

        public HandlesAttribute()
        {
        }

        public HandlesAttribute(object key)
        {
            Key = key;
        }

        public object Key { get; }

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
            if (returnType == typeof(bool))
                _returnsBool = true;
            else if (returnType != typeof(void))
                return false;

            var keyType = Key as Type;
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
                        ? (bool)((TwoArgsReturnDelegate)_delegate)(handler, callback, composer)
                        : (bool)((OneArgReturnDelegate)_delegate)(handler, callback);

                if (_passComposer)
                    ((TwoArgsDelegate)_delegate)(handler, callback, composer);
                else
                    ((OneArgDelegate)_delegate)(handler, callback);
                return true;
            }

            var args = _passComposer
                       ? new[] { callback, composer }
                       : new[] { callback };
            var result = InvokeMethod(handler, callback.GetType(), args);

            return !_returnsBool || (bool)result;
        }
    }
}
