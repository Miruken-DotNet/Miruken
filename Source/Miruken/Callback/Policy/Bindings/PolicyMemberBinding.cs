namespace Miruken.Callback.Policy.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Concurrency;
    using Context;
    using Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
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
            IHandler composer, int? priority = null, ResultsDelegate results = null)
        {
            var resultType = Policy.GetResultType?.Invoke(callback);
            return Invoke(target, callback, composer, resultType, priority, results);
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
                    handlerType.GetGenericArguments(), new[] { callback });
                if (mapping.Complete)
                {
                    try
                    {
                        var closed = mapping.MapTypes(new[] { type });
                        return handlerType.MakeGenericType(closed);
                    }
                    catch (ArgumentException)
                    {
                        // Most likely a constraint violation
                        return null;
                    }
                }
            }
            else if (!Dispatcher.IsVoid)
            {
                var mapping = new GenericMapping(
                    handlerType.GetGenericArguments(),
                    Array.Empty<Argument>(), Dispatcher.ReturnType);
                if (mapping.Complete)
                {
                    try
                    {
                        var closed = mapping.MapTypes(Array.Empty<Type>(), type);
                        return handlerType.MakeGenericType(closed);
                    }
                    catch (ArgumentException)
                    {
                        // Most likely a constraint violation
                        return null;
                    }
                }
            }
            return null;
        }

        private bool Invoke(object target, object callback,
            IHandler composer, Type resultType, int? priority, ResultsDelegate results)
        {
            var ruleArgs   = Rule?.ResolveArgs(callback) ?? Array.Empty<object>();
            var dispatcher = Dispatcher is GenericMethodDispatch genericDispatch
                ? genericDispatch.CloseDispatch(ruleArgs, resultType)
                : Dispatcher;

            IDisposable guardScope = null;
            if (callback is IDispatchCallbackGuard guard &&
                !guard.CanDispatch(target, this, dispatcher, out guardScope))
                return false;

            using (guardScope)
            {
                if (CallbackIndex.HasValue)
                {
                    var index = CallbackIndex.Value;
                    if (!dispatcher.Arguments[index].IsInstanceOf(ruleArgs[index]))
                        return false;
                }

                var returnType = dispatcher.ReturnType;

                bool ResolveArgsAndDispatch(
                    IHandler handler, ICollection<(IFilter, IFilterProvider)> f,
                    out object res, out bool isPromise)
                {
                    var args = ResolveArgs(dispatcher, callback, ruleArgs,
                        f, handler, out var context, out var failedArg);

                    switch (args)
                    {
                        case null:
                            isPromise = false;
                            var type = target as Type ?? target.GetType();
                            res = Task.FromException(new InvalidOperationException(
                                $"Failed to resolve argument '{failedArg.Parameter.Name}' of type '{failedArg.ParameterType.FullName}' for signature {dispatcher.Member} in '{type}'"));
                            return false;
                        case object[] array:
                            isPromise = false;
                            res = dispatcher.Invoke(target, array, resultType);
                            return context?.Unhandled != true;
                        case Promise<object[]> promise:
                            isPromise = true;
                            res = promise.Then((array, _) => dispatcher.Invoke(target, array, resultType));
                            return context?.Unhandled != true;
                        default:
                            isPromise = false;
                            res = Task.FromException(new InvalidOperationException(
                                $"Unable to resolve arguments for {dispatcher.Member}"));
                            return false;
                    }
                }

                object result;
                var isAsync = false;

                if ((callback as IFilterCallback)?.CanFilter == false)
                {
                    return ResolveArgsAndDispatch(composer, null, out result, out isAsync) &&
                           Accept(callback, result, returnType, priority, results, isAsync);
                }

                var filterCallback = GetCallbackInfo(
                    callback, ruleArgs, dispatcher, out var callbackType);

                var targetFilters = target is IFilter targetFilter
                    ? new[] { new FilterInstancesProvider(true, targetFilter) }
                    : null;

                var filters = composer.GetOrderedFilters(
                    this, dispatcher, callback, callbackType, Filters,
                    dispatcher.Owner.Filters, Policy.Filters, targetFilters);

                if (filters == null) return false;

                if (filters.Count == 0)
                {
                    if (!ResolveArgsAndDispatch(composer, null, out result, out isAsync))
                        return false;
                }
                else if (!dispatcher.GetPipeline(callbackType).Invoke(
                    this, target, filterCallback, callback, (IHandler comp, out bool completed) =>
                    {
                        if (!ResolveArgsAndDispatch(comp, filters, out var baseResult, out isAsync))
                        {
                            completed = false;
                            return baseResult;
                        }

                        completed = Policy.AcceptResult?.Invoke(baseResult, this)
                                 ?? baseResult != null;

                        return completed ? baseResult
                             : Task.FromException(new NotSupportedException(
                                 $"{dispatcher.Member} not handled"));
                    }, composer, filters, out result))
                    return false;

                return Accept(callback, result, returnType, priority, results, isAsync);
            }
        }

        private bool Accept(object callback, object result,
            Type returnType, int? priority, ResultsDelegate results, bool isAsync)
        {
            var accepted = Policy.AcceptResult?.Invoke(result, this)
                         ?? result != null;
            if (accepted && (result != null))
            {
                var asyncCallback = callback as IAsyncCallback;
                result = CoerceResult(result, returnType,
                    isAsync || asyncCallback?.WantsAsync == true);
                if (result != null)
                    return results?.Invoke(result, Category.Strict, priority) != false;
            }
            return accepted;
        }

        private object ResolveArgs(MemberDispatch dispatcher, object callback,
            object[] ruleArgs, ICollection<(IFilter, IFilterProvider)> filters,
            IHandler composer, out CallbackContext callbackContext, out Argument failedArg)
        {
            failedArg       = null;
            callbackContext = null;

            var arguments = dispatcher.Arguments;
            if (arguments.Length == ruleArgs.Length)
                return ruleArgs;

            var parent   = callback as Inquiry;
            var resolved = new object[arguments.Length];
            var promises = new List<Promise>();

            for (var i = ruleArgs.Length; i < arguments.Length; ++i)
            {
                var index        = i;
                var argument     = arguments[i];
                var argumentType = argument.ArgumentType;

                if (argumentType == typeof(IHandler))
                    resolved[i] = composer;
                else if (argumentType.Is<MemberBinding>())
                    resolved[i] = this;
                else if (argumentType.Is<MemberDispatch>())
                    resolved[i] = dispatcher;
                else if (argumentType == typeof(CallbackContext))
                {
                    resolved[i] = callbackContext ??=
                        new CallbackContext(callback, composer, this);
                }
                else if (argumentType == typeof(object))
                {
                    failedArg = argument;
                    return null;
                }
                else
                {
                    if (argumentType == typeof(IServiceProvider) || argumentType == typeof(IServiceScopeFactory))
                    {
                        var singletonLike = filters?.Any(f =>
                            f.Item2 is SingletonAttribute ||
                            f.Item2 is ContextualAttribute ctx && ctx.Rooted) == true;

                        if (singletonLike)
                        {
                            var context = composer.Resolve<Context>();
                            if (context != null)
                            {
                                resolved[i] = context.Root;
                                continue;
                            }
                        }
                    }

                    var resolver = argument.Resolver ?? ResolvingAttribute.Default;
                    resolver.ValidateArgument(argument);
                    var arg = resolver.ResolveArgumentAsync(parent, argument, composer);
                    if (arg == null && !argument.GetDefaultValue(out arg))
                    {
                        failedArg = argument;
                        return null;
                    }

                    switch (arg)
                    {
                        case Promise promise
                            when !argument.ArgumentFlags.HasFlag(Argument.Flags.Promise):
                            switch (promise.State)
                            {
                                case PromiseState.Fulfilled:
                                    resolved[i] = promise.Wait();
                                    break;
                                case PromiseState.Pending:
                                    promises.Add(promise.Then((res, _) => resolved[index] = res));
                                    break;
                                default:
                                    failedArg = argument;
                                    return null;
                            }
                            break;
                        default:
                            resolved[i] = arg;
                            break;
                    }
                }
            }

            Array.Copy(ruleArgs, resolved, ruleArgs.Length);

            if (promises.Count == 1)
                return promises[0].Then((r, _) => resolved);

            if (promises.Count > 1)
                return Promise.All(promises).Then((r, _) => resolved);

            return resolved;
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
            // ReSharper disable once UnusedMember.Local
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
