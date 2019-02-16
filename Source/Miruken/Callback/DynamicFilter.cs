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
        Task<TRes> IFilter<TCb, TRes>.Next(TCb callback,
            object rawCallback, MemberBinding member, 
            IHandler composer, Next<TRes> next,
            IFilterProvider provider)
        {
            var dispatch = DynamicNext.GetOrAdd(GetType(), GetDynamicNext);
            if (dispatch == null) return next();
            var args = ResolveArgs(dispatch, callback, rawCallback,
                member, composer, next, provider);
            if (args == null) return next(proceed: false);
            return (Task<TRes>)dispatch.Invoke(this, args);
        }

        private static object[] ResolveArgs(MemberDispatch dispatch,
            TCb callback, object rawCallback, MemberBinding member,
            IHandler composer, Next<TRes> next, IFilterProvider provider)
        {
            var arguments = dispatch.Arguments;
            if (arguments.Length == 2)
                return new object[] { callback, next };

            var parent   = callback as Inquiry;
            var resolved = new object[arguments.Length];
            resolved[0] = callback;

            for (var i = 1; i < arguments.Length; ++i)
            {
                var argument = arguments[i];
                switch (i)
                {
                    case 1:
                        resolved[1] = argument.ArgumentType == typeof(object)
                                    ? rawCallback : next;
                        continue;
                    case 2 when ReferenceEquals(resolved[1], rawCallback):
                        resolved[2] = next;
                        continue;
                }
                var argumentType = argument.LogicalType;
                if (argumentType == typeof(IHandler))
                    resolved[i] = composer;
                else if (argumentType.Is<MemberBinding>())
                    resolved[i] = member;
                else if (argumentType.IsInstanceOfType(provider))
                    resolved[i] = provider;
                else if (argumentType == typeof(object))
                {
                    throw new InvalidOperationException(
                        $"Object dependency '{argument.Parameter.Name}' is not allowed");
                }
                else
                {
                    var resolver = argument.Resolver ?? ResolvingAttribute.Default;
                    resolver.ValidateArgument(argument);
                    var arg = resolved[i] = resolver.ResolveArgument(parent, argument, composer);
                    if (arg == null && !argument.IsOptional) return null;
                }
            }

            return resolved;
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
