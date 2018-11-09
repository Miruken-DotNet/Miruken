namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading.Tasks;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    public abstract class DynamicFilter : IFilter
    {
        public int? Order { get; set; }

        protected static readonly ConcurrentDictionary<Type, MethodDispatch>
            DynamicNext = new ConcurrentDictionary<Type, MethodDispatch>();

        protected const BindingFlags Binding = BindingFlags.Instance
                                             | BindingFlags.Public;
    }

    public class DynamicFilter<TCb, TRes> : DynamicFilter, IFilter<TCb, TRes>
    {
        Task<TRes> IFilter<TCb, TRes>.Next(
            TCb callback, MemberBinding member, 
            IHandler composer, Next<TRes> next,
            IFilterProvider provider)
        {
            var dispatch = DynamicNext.GetOrAdd(GetType(), GetDynamicNext);
            if (dispatch == null) return next();
            var args = ResolveArgs(dispatch, callback, member, composer, next, provider);
            if (args == null) return next(proceed: false);
            return (Task<TRes>)dispatch.Invoke(this, args);
        }

        private static object[] ResolveArgs(MemberDispatch dispatch,
            TCb callback, MemberBinding member, IHandler composer,
            Next<TRes> next, IFilterProvider provider)
        {
            var arguments = dispatch.Arguments;
            if (arguments.Length == 2)
                return new object[] { callback, next };

            var args   = new object[arguments.Length];
            var parent = callback as Inquiry;

            for (var i = 2; i < arguments.Length; ++i)
            {
                var argument     = arguments[i];
                var argumentType = argument.ArgumentType;
                if (argumentType == typeof(IHandler))
                    args[i] = composer;
                else if (argumentType.Is<MemberBinding>())
                    args[i] = member;
                else if (argumentType.IsInstanceOfType(provider))
                    args[i] = provider;
                else if (argumentType == typeof(object))
                {
                    throw new InvalidOperationException(
                        $"Object dependency '{argument.Parameter.Name}' is not allowed");
                }
                else
                {
                    var resolver = argument.Resolver ?? ResolvingAttribute.Default;
                    resolver.ValidateArgument(argument);
                    var arg = args[i] = resolver.ResolveArgument(parent, argument, composer);
                    if (arg == null && !argument.IsOptional) return null;
                }
            }

            args[0] = callback;
            args[1] = next;
            return args;
        }

        private static MethodDispatch GetDynamicNext(Type type)
        {
            var members = type.FindMembers(
                MemberTypes.Method, Binding, IsDynamicNext, null);
            if (members.Length > 1)
                throw new InvalidOperationException(
                    $"Found {members.Length} compatible Next methods");
            return members.Length == 0 ? null : new MethodDispatch(
                (MethodInfo)members[0], Array.Empty<Attribute>());
        }

        private static bool IsDynamicNext(MemberInfo member, object criteria)
        {
            if (member.DeclaringType == typeof(object))
                return false;
            var method = (MethodInfo)member;
            if (method.Name != "Next" || method.ReturnType != typeof(Task<TRes>))
                return false;
            var parameters = method.GetParameters();
            if (parameters.Length < 2) return false;
            return parameters[0].ParameterType == typeof(TCb) &&
                   parameters[1].ParameterType == typeof(Next<TRes>);                
        }
    }
}
