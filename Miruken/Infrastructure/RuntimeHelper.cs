namespace Miruken.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    public delegate void   OneArgDelegate(object instance, object arg);
    public delegate void   TwoArgsDelegate(object instance, object arg1, object arg2);
    public delegate object NoArgsReturnDelegate(object instance);
    public delegate object OneArgReturnDelegate(object instance, object arg);
    public delegate object TwoArgsReturnDelegate(object instance, object arg1, object arg2);

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

        public static string GetSimpleTypeName(Type type)
        {
            var fullyQualifiedTypeName = type.AssemblyQualifiedName;
            return RemoveAssemblyDetails(fullyQualifiedTypeName);
        }

        public static Type[] GetToplevelInterfaces(Type type)
        {
            var allInterfaces = type.GetInterfaces();
            return allInterfaces.Except(allInterfaces.SelectMany(t => t.GetInterfaces()))
                .ToArray();
        }

        public static bool IsTopLevelInterface(Type @interface, Type type)
        {
            return @interface.IsInterface
                && @interface.IsAssignableFrom(type)
                && type.GetInterfaces().All(
                    i => i == @interface || !@interface.IsAssignableFrom(i));
        }

        public static MethodInfo SelectMethod(MethodInfo sourceMethod, Type type,
            BindingFlags binding = BindingFlags.Public)
        {
            var key = new KeyValuePair<MethodInfo, Type>(sourceMethod, type);
            return MethodMapping.GetOrAdd(key, k => MatchMethod(k.Key, k.Value, binding));
        }

        private static MethodInfo MatchMethod(MethodInfo sourceMethod, Type type, BindingFlags binding)
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
                var methods = type.GetMethods(BindingFlags.Instance | binding);
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

        public static OneArgDelegate CreateActionOneArg(MethodInfo method)
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
            return Expression.Lambda<OneArgDelegate>(
                methodCall, instance, argument
                ).Compile();
        }

        public static TwoArgsDelegate CreateActionTwoArgs(MethodInfo method)
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
            return Expression.Lambda<TwoArgsDelegate>(
                methodCall, instance, argument1, argument2
                ).Compile();
        }

        public static NoArgsReturnDelegate CreateFuncNoArgs(MethodInfo method)
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
            return Expression.Lambda<NoArgsReturnDelegate>(
                Expression.Convert(methodCall, typeof(object)),
                instance
                ).Compile();
        }

        public static OneArgReturnDelegate CreateFuncOneArg(MethodInfo method)
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
            return Expression.Lambda<OneArgReturnDelegate>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument
                ).Compile();
        }

        public static TwoArgsReturnDelegate CreateFuncTwoArgs(MethodInfo method)
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
            return Expression.Lambda<TwoArgsReturnDelegate>(
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
