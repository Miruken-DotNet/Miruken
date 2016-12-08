using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Miruken.Infrastructure
{
    using System;
    using System.Text;

    public static class RuntimeHelper
    {
        private static readonly ConcurrentDictionary<Type, object> 
            DefaultValues = new ConcurrentDictionary<Type, object>();

        public static object GetDefault(Type type)
        {
            return type != null && type.IsValueType
                ? DefaultValues.GetOrAdd(type, Activator.CreateInstance)
                : null;
        }

        public static string GetSimpleTypeName(Type t)
        {
            var fullyQualifiedTypeName = t.AssemblyQualifiedName;
            return RemoveAssemblyDetails(fullyQualifiedTypeName);
        }

        public static MethodInfo SelectMethod(MethodInfo sourceMethod, Type type)
        {
            var key = new KeyValuePair<MethodInfo, Type>(sourceMethod, type);
            return MethodMapping.GetOrAdd(key, k => MatchMethod(k.Key, k.Value));
        }

        private static MethodInfo MatchMethod(MethodInfo sourceMethod, Type type)
        {
            Type[] genericArguments = null;
            if (sourceMethod.IsGenericMethod)
            {
                genericArguments = sourceMethod.GetGenericArguments();
                sourceMethod     = sourceMethod.GetGenericMethodDefinition();
            }
            var declaringType = sourceMethod.DeclaringType;
            MethodInfo methodOnTarget = null;
            if (declaringType.GetTypeInfo().IsInterface &&
                declaringType?.IsAssignableFrom(type) == true)
            {
                var mapping = type.GetTypeInfo().GetRuntimeInterfaceMap(declaringType);
                var index   = Array.IndexOf(mapping.InterfaceMethods, sourceMethod);
                methodOnTarget = mapping.TargetMethods[index];
            }
            else
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                              BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (MethodSignatureComparer.Instance.Equals(method.GetBaseDefinition(), sourceMethod))
                    {
                        methodOnTarget = method;
                        break;
                    }
                }
            }
            if (methodOnTarget == null) return null;

            return genericArguments == null
                 ? methodOnTarget 
                 : methodOnTarget.MakeGenericMethod(genericArguments);
        }

        private static readonly ConcurrentDictionary<KeyValuePair<MethodInfo, Type>, MethodInfo> 
            MethodMapping = new ConcurrentDictionary<KeyValuePair<MethodInfo, Type>, MethodInfo>();

        public static Action<object, object> CreateActionOneArg(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument   = Expression.Parameter(typeof(object), "argument");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument, parameters[0].ParameterType)
                );
            return Expression.Lambda<Action<object, object>>(
                methodCall, instance, argument
                ).Compile();
        }

        public static Action<object, object, object> CreateActionTwoArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument1  = Expression.Parameter(typeof(object), "argument1");
            var argument2  = Expression.Parameter(typeof(object), "argument2");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType)
                );
            return Expression.Lambda<Action<object, object, object>>(
                methodCall, instance, argument1, argument2
                ).Compile();
        }

        public static Func<object, object> CreateFuncNoArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 0)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance = Expression.Parameter(typeof(object), "instance");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method
                );
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(methodCall, typeof(object)),
                instance
                ).Compile();
        }

        public static Func<object, object, object> CreateFuncOneArg(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument   = Expression.Parameter(typeof(object), "argument");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument, parameters[0].ParameterType)
                );
            return Expression.Lambda<Func<object, object, object>>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument
                ).Compile();
        }

        public static Func<object, object, object, object> CreateFuncTwoArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument1  = Expression.Parameter(typeof(object), "argument1");
            var argument2  = Expression.Parameter(typeof(object), "argument2");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType)
                );
            return Expression.Lambda<Func<object, object, object, object>>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument1, argument2
                ).Compile();
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var builder = new StringBuilder();

            var writingAssemblyName     = false;
            var skippingAssemblyDetails = false;

            foreach (var current in fullyQualifiedTypeName)
            {
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                            skippingAssemblyDetails = true;
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                            builder.Append(current);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
