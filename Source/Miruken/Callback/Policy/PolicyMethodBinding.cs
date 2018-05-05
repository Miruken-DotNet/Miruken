namespace Miruken.Callback.Policy
{
    using System;
    using System.Diagnostics;
    using System.Linq;

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

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
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
            var resultType = Policy.ResultType?.Invoke(callback);
            return Invoke(target, callback, composer, resultType, results);
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
            IHandler composer, Type resultType, ResultsDelegate results)
        {
            object result;
            var args = Rule.ResolveArgs(callback);

            if ((callback as IFilterCallback)?.CanFilter == false)
            {
                args = ResolveArgs(args, composer, out var completed);
                if (completed)
                    Dispatcher.Invoke(target, args, resultType);
                return completed;
            }

            var dispatcher     = Dispatcher.CloseDispatch(args, resultType);
            var actualCallback = GetCallbackInfo(
                callback, args, dispatcher, out var callbackType);
            var returnType     = dispatcher.ReturnType;
            var logicalType    = dispatcher.LogicalReturnType;

            var targetFilters  = target is IFilter targetFilter
                ? new [] {new FilterInstancesProvider(targetFilter)}
                : null;

            var filters = composer.GetOrderedFilters(
                this, callbackType, logicalType, Filters,
                dispatcher.Owner.Filters, Policy.Filters, targetFilters)
                .ToArray();

            object baseResult = this;

            if (filters.Length == 0)
            {
                args = ResolveArgs(args, composer, out var completed);
                if (!completed) return false;
                result = baseResult = dispatcher.Invoke(target, args, resultType);
            }
            else if (!MethodPipeline.GetPipeline(callbackType, logicalType)
                .Invoke(this, target, actualCallback,
                    (IHandler comp, out bool completed) =>
                        baseResult = dispatcher.Invoke(target,
                        ResolveArgs(args, comp, out completed),
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

        private object[] ResolveArgs(
            object[] ruleArgs, IHandler composer, out bool completed)
        {
            completed = true;
            var arguments = Dispatcher.Arguments;
            if (arguments.Length == ruleArgs.Length)
                return ruleArgs;

            var args = new object[arguments.Length];

            if (!composer.All(bundle =>
            {
                for (var i = ruleArgs.Length; i < arguments.Length; ++i)
                {
                    var index        = i;
                    var argument     = arguments[i];
                    var argumentType = argument.ArgumentType;
                    var optional     = argument.Optional;
                    if (argumentType == typeof(IHandler))
                        args[i] = composer;
                    else if (argumentType.IsInstanceOfType(this))
                        args[i] = this;
                    else
                    {
                        var resolver = argument.Resolver ?? ResolvingAttribute.Default;
                        bundle.Add(h => args[index] =
                                resolver.ResolveArgument(argument, h, composer),
                            (ref bool resolved) =>
                            {
                                resolved = resolved || optional;
                                return false;
                            });
                    }
                }
            }))
            {
                completed = false;
                return null;
            }

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

        private string DebuggerDisplay
        {
            get
            {
                var category = Category.GetType().Name.Replace("Attribute", "");
                return $"{category} | {Dispatcher.Method}";
            }
        }
    }
}
