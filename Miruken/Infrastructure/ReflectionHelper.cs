using System.Linq.Expressions;
using System.Reflection;

namespace Miruken.Infrastructure
{
    using System;
    using System.Text;

    public static class ReflectionHelper
    {
        public static string GetSimpleTypeName(Type t)
        {
            var fullyQualifiedTypeName = t.AssemblyQualifiedName;
            return RemoveAssemblyDetails(fullyQualifiedTypeName);
        }

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
