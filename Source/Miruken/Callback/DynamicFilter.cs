﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Policy;

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
        TRes IFilter<TCb, TRes>.Next(TCb callback, MethodBinding method, 
            IHandler composer, NextDelegate<TRes> next)
        {
            var dispatch = DynamicNext.GetOrAdd(GetType(), GetDynamicNext);
            if (dispatch == null) return next();
            var args = ResolveArgs(dispatch, callback, method, composer, next);
            return (TRes)dispatch.Invoke(this, args);
        }

        private static object[] ResolveArgs(MethodDispatch dispatch,
            TCb callback, MethodBinding method, IHandler composer,
            NextDelegate<TRes> next)
        {
            var arguments = dispatch.Arguments;
            if (arguments.Length == 2)
                return new object[] { callback, next };

            List<Argument> culprits = null;
            var args = new object[arguments.Length];

            if (!composer.All(bundle =>
            {
                for (var i = 2; i < arguments.Length; ++i)
                {
                    var index        = i;
                    var argument     = arguments[i];
                    var argumentType = argument.ArgumentType;
                    var optional     = argument.Optional;
                    var resolver     = argument.Resolver ?? ResolvingAttribute.Default;
                    if (argumentType == typeof(IHandler))
                        args[i] = composer;
                    else if (argumentType.IsInstanceOfType(method))
                        args[i] = method;
                    else
                        bundle.Add(h => args[index] = resolver.ResolveArgument(
                            argument, optional ? h.BestEffort() : h, composer),
                            (ref bool resolved) =>
                            {
                                if (!resolved)
                                    (culprits ?? (culprits = new List<Argument>()))
                                        .Add(argument);
                                return false;
                            });
                }
            }))
            {
                var errors = new StringBuilder("Missing dependencies");
                foreach (var culprit in culprits)
                    errors.Append($" {culprit.Parameter.Name}:{culprit.ParameterType}");
                throw new InvalidOperationException(errors.ToString());
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
            if (method.Name != "Next") return false;
            var parameters = method.GetParameters();
            if (parameters.Length < 2) return false;
            return parameters[0].ParameterType == typeof(TCb) &&
                   parameters[1].ParameterType == typeof(NextDelegate<TRes>);                
        }
    }
}
