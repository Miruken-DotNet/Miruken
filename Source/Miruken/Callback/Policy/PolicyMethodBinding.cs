namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;

    public delegate bool AcceptResultDelegate(
        object result, MethodBinding binding);

    public delegate PolicyMethodBinding BindMethodDelegate(
        CallbackPolicy policy,
        PolicyMethodBindingInfo policyMethodBindingInfo);

    public class PolicyMethodBindingInfo
    {
        public PolicyMethodBindingInfo(
            MethodRule rule, MethodDispatch dispatch,
            CategoryAttribute category)
        {
            Rule          = rule;
            Dispatch      = dispatch;
            Category      = category;
            InKey         = category.InKey;
            OutKey        = category.OutKey;
            CallbackIndex = null;
        }

        public object            InKey;
        public object            OutKey;
        public int?              CallbackIndex;
        public MethodRule        Rule           { get; }
        public MethodDispatch    Dispatch       { get; }
        public CategoryAttribute Category       { get; }
    }

    public class PolicyMethodBinding : MethodBinding
    {
        public PolicyMethodBinding(CallbackPolicy policy,
                                   PolicyMethodBindingInfo bindingInfo)
            : base(bindingInfo.Dispatch)
        {
            Policy        = policy;
            Rule          = bindingInfo.Rule;
            Category      = bindingInfo.Category;
            CallbackIndex = bindingInfo.CallbackIndex;
            Key           = policy.CreateKey(bindingInfo);
        }

        public MethodRule        Rule          { get; }
        public CategoryAttribute Category      { get; }
        public CallbackPolicy    Policy        { get; }
        public int?              CallbackIndex { get; }
        public object            Key           { get; }

        public bool Approves(object callback)
        {
            return Category.Approve(callback, this);
        }

        public override bool Dispatch(object target, object callback, 
            IHandler composer, ResultsDelegate results = null)
        {
            object result;
            var resultType = Policy.ResultType?.Invoke(callback);
            return Invoke(target, callback, composer, resultType,
                results, out result);
        }

        public Type CloseHandlerType(Type handlerType, object key)
        {
            var type = key as Type;
            if (type == null || !handlerType.IsGenericTypeDefinition)
                return handlerType;
            var index = CallbackIndex;
            if (!index.HasValue) return null;
            var callback = Dispatcher.Arguments[index.Value];
            var mapping  = new GenericMapping(
                handlerType.GetGenericArguments(),
                new [] { callback });
            if (mapping.Complete)
            {
                var closed = mapping.MapTypes(new[] { type });
                return handlerType.MakeGenericType(closed);
            }
            return null;
        }

        private bool Invoke(object target, object callback,
            IHandler composer, Type resultType, ResultsDelegate results,
            out object result)
        {
            var args = ResolveArgs(callback, composer);
            if (args == null)
            {
                result = null;
                return false;
            }

            if ((callback as IFilterCallback)?.AllowFiltering == false)
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

            var targetFilter   = target as IFilter;
            var targetFilters  = targetFilter != null
                ? new [] {new FilterInstancesProvider(targetFilter)}
                : null;

            var filters = composer.GetOrderedFilters(
                this, callbackType, logicalType, Filters,
                dispatcher.Owner.Filters, Policy.Filters, targetFilters)
                .ToArray();

            object baseResult = this;

            if (filters.Length == 0)
                result = baseResult = dispatcher.Invoke(target, args, resultType);
            else if (!MethodPipeline.GetPipeline(callbackType, logicalType)
                .Invoke(this, target, actualCallback, comp => 
                    baseResult = dispatcher.Invoke(
                        target, UpdateArgs(args, composer, comp),
                        resultType), composer, filters, out result))
                return false;

            var testResult = ReferenceEquals(baseResult, this) ? result : baseResult;
            var accepted   = Policy.AcceptResult?.Invoke(testResult, this)
                          ?? testResult != null;
            if (accepted && (result != null))
            {
                var asyncCallback = callback as IAsyncCallback;
                result = CoerceResult(result, returnType, asyncCallback?.WantsAsync);
                return results?.Invoke(result, Category.Strict) != false;
            }

            return accepted;
        }

        private object[] ResolveArgs(object callback, IHandler composer)
        {
            var numRuleArgs = Rule.Args.Length;
            var arguments   = Dispatcher.Arguments;
            if (arguments.Length == numRuleArgs)
                return Rule.ResolveArgs(callback);

            var args = new object[arguments.Length];

            if (!composer.All(bundle =>
            {
                for (var i = numRuleArgs; i < arguments.Length; ++i)
                {
                    var index        = i;
                    var argument     = arguments[i];
                    var argumentType = argument.ArgumentType;
                    var optional     = argument.Optional;
                    var resolver     = argument.Resolver ?? ResolvingAttribute.Default;
                    if (argumentType == typeof(IHandler))
                        args[i] = composer;
                    else if (argumentType.IsInstanceOfType(this))
                        args[i] = this;
                    else
                        bundle.Add(h => args[index] = 
                            resolver.ResolveArgument(argument, h, composer),
                            (ref bool resolved) =>
                            {
                                resolved = resolved || optional;
                                return false;
                            });
                }
            })) return null;

            var ruleArgs = Rule.ResolveArgs(callback);
            Array.Copy(ruleArgs, args, ruleArgs.Length);
            return args;
        }

        private object GetCallbackInfo(object callback, object[] args,
            MethodDispatch dispatcher, out Type callbackType)
        {
            if (CallbackIndex.HasValue)
            {
                var index = CallbackIndex.Value;
                callbackType = dispatcher.Arguments[index].ArgumentType;
                return args[index];
            }
            callbackType = callback.GetType();
            return callback;
        }

        private static object[] UpdateArgs(object[] args,
            IHandler oldComposer, IHandler newComposer)
        {
            return ReferenceEquals(oldComposer, newComposer) ? args
                 : args.Select(arg => ReferenceEquals(arg, oldComposer)
                 ? newComposer : arg).ToArray();
        }
    }
}
