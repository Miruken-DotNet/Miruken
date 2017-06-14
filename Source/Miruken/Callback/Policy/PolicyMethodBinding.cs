namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;

    public delegate bool AcceptResultDelegate(
        object result, MethodBinding binding);

    public delegate PolicyMethodBinding BindMethodDelegate(
        MethodRule rule, MethodDispatch dispatch,
        DefinitionAttribute attribute, CallbackPolicy policy);

    public class PolicyMethodBinding : MethodBinding
    {
        private Type _varianceType;

        public PolicyMethodBinding(MethodRule rule,
                                   MethodDispatch dispatch,
                                   DefinitionAttribute attribute,
                                   CallbackPolicy policy)
            : base(dispatch)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Rule       = rule;
            Attribute  = attribute;
            Policy     = policy;
        }

        public MethodRule          Rule          { get; }
        public DefinitionAttribute Attribute     { get; }
        public CallbackPolicy      Policy        { get; }
        public int?                CallbackIndex { get; set; }

        public Type VarianceType
        {
            get { return _varianceType; }
            set
            {
                if (value?.ContainsGenericParameters == true &&
                    !value.IsGenericTypeDefinition)
                    _varianceType = value.GetGenericTypeDefinition();
                else
                    _varianceType = value;
            }
        }

        public object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public override bool Dispatch(object target, object callback, 
            IHandler composer, Func<object, bool> results = null)
        {
            if (Attribute?.Approve(callback, this) == false)
                return false;
            object result;
            var resultType = Policy.ResultType?.Invoke(callback);
            return Invoke(target, callback, composer, resultType,
                results, out result);
        }

        private bool Invoke(object target, object callback,
            IHandler composer, Type resultType, Func<object, bool> results,
            out object result)
        {
            var args = Rule.ResolveArgs(this, callback, composer);

            if (callback is IInvokeCallback)
            {
                result = Dispatcher.Invoke(target, args, resultType);
                return true;
            }

            Type callbackType;
            var dispatcher     = Dispatcher.CloseDispatch(args, resultType);
            var actualCallback = GetCallbackInfo(
                callback, args, dispatcher, out callbackType);
            var returnType     = dispatcher.ReturnType;
            var logicalType    = dispatcher.LogicalReturnType;

            var filters = composer
                .GetOrderedFilters(callbackType, logicalType, Filters,
                    FilterAttribute.GetFilters(target.GetType(), true), 
                    Policy.Filters)
                .ToArray();

            if (filters.Length == 0)
                result = dispatcher.Invoke(target, args, resultType);
            else if (!MethodPipeline.GetPipeline(callbackType, logicalType)
                .Invoke(this, target, actualCallback, comp => dispatcher.Invoke(
                    target, GetArgs(callback, args, composer, comp),
                        resultType), composer, filters, out result))
                return false;

            var accepted = Policy.AcceptResult?.Invoke(result, this)
                        ?? result != null;
            if (accepted && (result != null))
            {
                var asyncCallback = callback as IAsyncCallback;
                result = CoerceResult(result, returnType, asyncCallback?.WantsAsync);
                return results?.Invoke(result) != false;
            }

            return accepted;
        }

        private object GetCallbackInfo(object callback, object[] args,
            MethodDispatch dispatcher, out Type callbackType)
        {
            if (CallbackIndex.HasValue)
            {
                var index = CallbackIndex.Value;
                callbackType = dispatcher.Parameters[index].ParameterType;
                return args[index];
            }
            callbackType = callback.GetType();
            return callback;
        }

        private object[] GetArgs(object callback, object[] args,
            IHandler oldComposer, IHandler newComposer)
        {
            return ReferenceEquals(oldComposer, newComposer) 
                 ? args : Rule.ResolveArgs(this, callback, newComposer);
        }
    }
}
