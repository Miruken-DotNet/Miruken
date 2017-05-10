namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;

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
            var resultType = Policy.ResultType?.Invoke(callback);
            var result     = Invoke(target, callback, composer, resultType);
            var accepted   = Policy.AcceptResult?.Invoke(result, this) 
                          ?? result != null;
            return accepted && result != null
                 ? results?.Invoke(result) != false
                 : accepted;
        }

        protected object Invoke(object target, object callback,
            IHandler composer, Type resultType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);

            if (callback is FilterOptions)
                return Dispatcher.Invoke(target, args, resultType);

            Type callbackType;
            var dispatcher     = Dispatcher.CloseMethod(args, resultType);
            var actualCallback = GetCallbackInfo(callback, args, dispatcher, out callbackType);
            var returnType     = dispatcher.ReturnType;
            var asyncCallback  = callback as IAsyncCallback;

            Func<object, Type, object> convertResult = PassResult;
            if (asyncCallback?.WantsAsync == true && !dispatcher.IsPromise)
            {
                var logicalType = dispatcher.LogicalReturnType;
                returnType      = logicalType != typeof(void)
                                ? typeof(Promise<>).MakeGenericType(logicalType)
                                : typeof(Promise<object>);
                convertResult   = dispatcher.IsTask
                                ? (Func<object, Type, object>)PromisifyTask
                                : PromisifyResult;
            }

            var filters = composer
                .GetOrderedFilters(callbackType, returnType, Filters,
                    FilterAttribute.GetFilters(target.GetType(), true), 
                    Policy.Filters)
                .ToArray();

            if (filters.Length == 0)
            {
                return convertResult(
                    dispatcher.Invoke(target, args, resultType),
                    returnType);
            }

            object result;
            bool   completed;

            if (filters.All(filter => filter is IDynamicFilter))
            {
                completed = MethodPipeline.InvokeDynamic(
                    this, target, actualCallback, comp => 
                        convertResult(dispatcher.Invoke(
                        target, GetArgs(callback, args, composer, comp), resultType),
                        returnType),
                    composer, filters.Cast<IDynamicFilter>(), out result);
            }
            else
            {
                var pipeline = MethodPipeline.GetPipeline(callbackType, returnType);
                completed = pipeline.Invoke(
                    this, target, actualCallback, comp => convertResult(dispatcher.Invoke(
                        target, GetArgs(callback, args, composer, comp), resultType),
                        returnType),
                    composer, filters, out result);
            }
  
            return completed ? result : Policy.NoResult;
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

        private static object PromisifyResult(object result, Type promiseType)
        {
            return Promise.Resolved(result).Coerce(promiseType);
        }

        private static object PromisifyTask(object result, Type promiseType)
        {
            return ((Task)result)?.ToPromise().Coerce(promiseType);
        }

        private static object PassResult(object result, Type promiseType)
        {
            return result;
        }
    }
}
