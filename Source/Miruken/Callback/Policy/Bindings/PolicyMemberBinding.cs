namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Infrastructure;
    using Rules;

    public delegate PolicyMemberBinding BindMemberDelegate(
        CallbackPolicy policy,
        PolicyMemberBindingInfo policyMemberBindingInfo);

    public class PolicyMemberBindingInfo
    {
        public PolicyMemberBindingInfo(
            MethodRule rule, MemberDispatch dispatch,
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
        public MethodRule        Rule     { get; }
        public MemberDispatch    Dispatch { get; }
        public CategoryAttribute Category { get; }
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class PolicyMemberBinding : MemberBinding
    {
        public PolicyMemberBinding(CallbackPolicy policy,
                                   PolicyMemberBindingInfo bindingInfo)
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
            var resultType = Policy.GetResultType?.Invoke(callback);
            return Invoke(target, callback, composer, resultType, results);
        }

        public Type CloseHandlerType(Type handlerType, object key)
        {
            var type = key as Type;
            if (type == null || !handlerType.IsGenericTypeDefinition)
                return handlerType;
            var index = CallbackIndex;
            if (index.HasValue)
            {
                var callback = Dispatcher.Arguments[index.Value];
                var mapping  = new GenericMapping(
                    handlerType.GetGenericArguments(),
                    new[] { callback });
                if (mapping.Complete)
                {
                    var closed = mapping.MapTypes(new[] { type });
                    return handlerType.MakeGenericType(closed);
                }
            }
            else if (!Dispatcher.IsVoid)
            {
                var mapping = new GenericMapping(
                    handlerType.GetGenericArguments(),
                    Array.Empty<Argument>(), Dispatcher.ReturnType);
                if (mapping.Complete)
                {
                    var closed = mapping.MapTypes(Array.Empty<Type>(), type);
                    return handlerType.MakeGenericType(closed);
                }
            }
            return null;
        }

        private bool Invoke(object target, object callback,
            IHandler composer, Type resultType, ResultsDelegate results)
        {
            object result;
            var args = Rule?.ResolveArgs(callback) ?? Array.Empty<object>();
            var dispatcher = Dispatcher.CloseDispatch(args, resultType);

            if ((callback as IDispatchCallbackGuard)
                ?.CanDispatch(target, dispatcher) == false)
                return false;

            if (CallbackIndex.HasValue)
            {
                var index = CallbackIndex.Value;
                if (!dispatcher.Arguments[index].IsInstanceOf(args[index]))
                    return false;
            }

            var returnType = dispatcher.ReturnType;

            if ((callback as IFilterCallback)?.CanFilter == false)
            {
                args = ResolveArgs(dispatcher, callback, args, 
                                   composer, out var completed);
                if (completed)
                {
                    result = dispatcher.Invoke(target, args, resultType);
                    return Accept(callback, result, returnType, results);
                }
                return false;
            }

            var actualCallback = GetCallbackInfo(
                callback, args, dispatcher, out var callbackType);

            var targetFilters  = target is IFilter targetFilter
                ? new [] {new FilterInstancesProvider(true, targetFilter)}
                : null;

            var filters = composer.GetOrderedFilters(
                this, dispatcher, callbackType, Filters,
                dispatcher.Owner.Filters, Policy.Filters, targetFilters);

            if (filters == null) return false;

            if (filters.Count == 0)
            {
                args = ResolveArgs(dispatcher, callback, args, 
                                   composer, out var completed);
                if (!completed) return false;
                result = dispatcher.Invoke(target, args, resultType);
            }
            else if (!dispatcher.GetPipeline(callbackType).Invoke(
                this, target, actualCallback, (IHandler comp, out bool completed) =>
                {
                    args = ResolveArgs(dispatcher, callback, args,
                                       comp, out completed);
                    if (!completed) return null;
                    var baseResult = dispatcher.Invoke(target, args, resultType);
                    completed = Policy.AcceptResult?.Invoke(baseResult, this)
                             ?? baseResult != null;
                    return baseResult;
                }, composer, filters, out result))
                return false;

            return Accept(callback, result, returnType, results);
        }

        private bool Accept(object callback, object result,
            Type returnType, ResultsDelegate results)
        {
            var accepted = Policy.AcceptResult?.Invoke(result, this)
                           ?? result != null;
            if (accepted && (result != null))
            {
                var asyncCallback = callback as IAsyncCallback;
                result = CoerceResult(result, returnType, asyncCallback?.WantsAsync);
                if (result != null)
                    return results?.Invoke(result, Category.Strict) != false;
            }
            return accepted;
        }

        private object[] ResolveArgs(MemberDispatch dispatcher,
            object callback, object[] ruleArgs, IHandler composer,
            out bool completed)
        {
            completed = true;
            var arguments = dispatcher.Arguments;
            if (arguments.Length == ruleArgs.Length)
                return ruleArgs;

            var parent = callback as Inquiry;
            var args   = new object[arguments.Length];

            for (var i = ruleArgs.Length; i < arguments.Length; ++i)
            {
                var argument     = arguments[i];
                var argumentType = argument.ArgumentType;
                if (argumentType == typeof(IHandler))
                    args[i] = composer;
                else if (argumentType.Is<MemberBinding>())
                    args[i] = this;
                else if (argumentType == typeof(object))
                {
                    completed = false;
                    return null;
                }
                else
                {
                    var resolver = argument.Resolver ?? ResolvingAttribute.Default;
                    resolver.ValidateArgument(argument);
                    var arg = args[i] = resolver.ResolveArgument(parent, argument, composer);
                    if (arg == null && !argument.IsOptional)
                    {
                        completed = false;
                        return null;
                    }
                }
            }

            Array.Copy(ruleArgs, args, ruleArgs.Length);
            return args;
        }

        private object GetCallbackInfo(object callback, object[] args,
            MemberDispatch dispatcher, out Type callbackType)
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
                return $"{category} | {Dispatcher.Member}";
            }
        }

        public static readonly IComparer<PolicyMemberBinding>
            OrderByArity = new CompareByArity();

        private class CompareByArity : IComparer<PolicyMemberBinding>
        {
            public int Compare(PolicyMemberBinding x, PolicyMemberBinding y)
            {
                if (x == null) return 1;
                if (y == null) return -1;
                return y.Dispatcher.Arity - x.Dispatcher.Arity;
            }
        }
    }
}
