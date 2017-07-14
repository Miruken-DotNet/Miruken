namespace Miruken.Infrastructure
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    #region Delegates

    public delegate void   NoArgsDelegate(object instance);
    public delegate void   OneArgDelegate(object instance, object arg);
    public delegate void   TwoArgsDelegate(object instance, object arg1, object arg2);
    public delegate void   ThreeArgsDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate void   FourArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate void   FiveArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object NoArgsReturnDelegate(object instance);
    public delegate object OneArgReturnDelegate(object instance, object arg);
    public delegate object TwoArgsReturnDelegate(object instance, object arg1, object arg2);
    public delegate object ThreeArgsReturnDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate object FourArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate object FiveArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object PropertyGetDelegate(object instance);

    #endregion

    public static class RuntimeHelper
    {
        private static readonly ConcurrentDictionary<Type, object> 
            DefaultValues = new ConcurrentDictionary<Type, object>();

        public static object GetDefault(Type type)
        {
            return type != null && type.IsValueType && type != typeof(void)
                 ? DefaultValues.GetOrAdd(type, Activator.CreateInstance)
                 : null;
        }

        public static bool IsSimpleType(this Type type)
        {
            if (type == null) return false;
            return type.IsPrimitive ||
                Array.IndexOf(SimpleTypes, type) >= 0 ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() ==
                typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]));
        }

        public static bool IsCollection(object instance)
        {
            return instance is IEnumerable && !(instance is string);
        }

        public static string GetSimpleTypeName(this Type type)
        {
            if (type == null) return "";
            var fullyQualifiedTypeName = type.AssemblyQualifiedName;
            return RemoveAssemblyDetails(fullyQualifiedTypeName);
        }

        public static Type[] GetToplevelInterfaces(this Type type)
        {
            if (type == null) return Array.Empty<Type>();
            var allInterfaces = type.GetInterfaces();
            return allInterfaces.Except(allInterfaces.SelectMany(t => t.GetInterfaces()))
                .ToArray();
        }

        public static bool IsTopLevelInterface(this Type @interface, Type type)
        {
            if (@interface == null || type == null) return false;
            return @interface.IsInterface
                && @interface.IsAssignableFrom(type)
                && type.GetInterfaces().All(
                    i => i == @interface || !@interface.IsAssignableFrom(i));
        }

        public static bool IsClassOf(this Type type, Type @class)
        {
            return @class.IsAssignableFrom(type) ||
                   type.GetOpenTypeConformance(@class) != null;
        }

        public static Type GetOpenTypeConformance(this Type type, Type openType)
        {
            if (openType.IsGenericTypeDefinition)
            {
                if (openType.IsInterface)
                {
                    if (type == openType) return type;
                    if (type.IsGenericType &&
                        type.GetGenericTypeDefinition() == openType)
                        return type;
                    return type.GetInterfaces()
                        .Select(t => GetOpenTypeConformance(t, openType))
                        .FirstOrDefault(t => t != null);
                }
                while (type != typeof(object) &&
                        type?.IsGenericType == true)
                {
                    if (type.GetGenericTypeDefinition() == openType)
                        return type;
                    type = type.BaseType;
                }
            }
            return null;
        }

        public static object ChangeType<T>(object value)
        {
            return ChangeType(value, typeof(T));
        }

        public static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (conversionType.IsGenericType &&
                conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return null;
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            return conversionType == typeof(Guid)
                 ? new Guid(value.ToString()) 
                 : Convert.ChangeType(value, conversionType);
        }

        public static string GetDescription(this MethodInfo method)
        {
            return $"{method.ReflectedType?.FullName}:{method.Name}";
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

        public static NoArgsDelegate CreateCallNoArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 0)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method
                );
            return Expression.Lambda<NoArgsDelegate>(
                methodCall, instance
                ).Compile();
        }

        public static OneArgDelegate CreateCallOneArg(MethodInfo method)
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

        public static TwoArgsDelegate CreateCallTwoArgs(MethodInfo method)
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

        public static ThreeArgsDelegate CreateCallThreeArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 3)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance  = Expression.Parameter(typeof(object), "instance");
            var argument1 = Expression.Parameter(typeof(object), "argument1");
            var argument2 = Expression.Parameter(typeof(object), "argument2");
            var argument3 = Expression.Parameter(typeof(object), "argument3");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType)
                );
            return Expression.Lambda<ThreeArgsDelegate>(
                methodCall, instance, argument1, argument2, argument3
                ).Compile();
        }

        public static FourArgsDelegate CreateCallFourArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 4)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance  = Expression.Parameter(typeof(object), "instance");
            var argument1 = Expression.Parameter(typeof(object), "argument1");
            var argument2 = Expression.Parameter(typeof(object), "argument2");
            var argument3 = Expression.Parameter(typeof(object), "argument3");
            var argument4 = Expression.Parameter(typeof(object), "argument4");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType),
                Expression.Convert(argument4, parameters[3].ParameterType)
                );
            return Expression.Lambda<FourArgsDelegate>(
                methodCall, instance, argument1, argument2, argument3, argument4
                ).Compile();
        }

        public static FiveArgsDelegate CreateCallFiveArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 5)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            var instance  = Expression.Parameter(typeof(object), "instance");
            var argument1 = Expression.Parameter(typeof(object), "argument1");
            var argument2 = Expression.Parameter(typeof(object), "argument2");
            var argument3 = Expression.Parameter(typeof(object), "argument3");
            var argument4 = Expression.Parameter(typeof(object), "argument4");
            var argument5 = Expression.Parameter(typeof(object), "argument5");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType),
                Expression.Convert(argument4, parameters[3].ParameterType),
                Expression.Convert(argument5, parameters[4].ParameterType)
                );
            return Expression.Lambda<FiveArgsDelegate>(
                methodCall, instance, argument1, argument2, argument3, argument4, argument5
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

        public static ThreeArgsReturnDelegate CreateFuncThreeArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 3)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument1  = Expression.Parameter(typeof(object), "argument1");
            var argument2  = Expression.Parameter(typeof(object), "argument2");
            var argument3  = Expression.Parameter(typeof(object), "argument3");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType)
                );
            return Expression.Lambda<ThreeArgsReturnDelegate>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument1, argument2, argument3
                ).Compile();
        }

        public static FourArgsReturnDelegate CreateFuncFourArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 4)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance   = Expression.Parameter(typeof(object), "instance");
            var argument1  = Expression.Parameter(typeof(object), "argument1");
            var argument2  = Expression.Parameter(typeof(object), "argument2");
            var argument3  = Expression.Parameter(typeof(object), "argument3");
            var argument4  = Expression.Parameter(typeof(object), "argument4");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType),
                Expression.Convert(argument4, parameters[3].ParameterType)
                );
            return Expression.Lambda<FourArgsReturnDelegate>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument1, argument2, argument3, argument4
                ).Compile();
        }

        public static FiveArgsReturnDelegate CreateFuncFiveArgs(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            if (parameters.Length != 5)
                throw new ArgumentException($"Method {method.Name} expects {parameters.Length} argument(s)");
            if (method.ReturnType == typeof(void))
                throw new ArgumentException($"Method {method.Name} is void");
            var instance  = Expression.Parameter(typeof(object), "instance");
            var argument1 = Expression.Parameter(typeof(object), "argument1");
            var argument2 = Expression.Parameter(typeof(object), "argument2");
            var argument3 = Expression.Parameter(typeof(object), "argument3");
            var argument4 = Expression.Parameter(typeof(object), "argument4");
            var argument5 = Expression.Parameter(typeof(object), "argument5");
            var methodCall = Expression.Call(
                Expression.Convert(instance, target),
                method,
                Expression.Convert(argument1, parameters[0].ParameterType),
                Expression.Convert(argument2, parameters[1].ParameterType),
                Expression.Convert(argument3, parameters[2].ParameterType),
                Expression.Convert(argument4, parameters[3].ParameterType),
                Expression.Convert(argument3, parameters[4].ParameterType)
                );
            return Expression.Lambda<FiveArgsReturnDelegate>(
                Expression.Convert(methodCall, typeof(object)),
                instance, argument1, argument2, argument3, argument4, argument5
                ).Compile();
        }

        public static Func<T, Ret> CreateGenericFuncNoArgs<T, Ret>(
            string methodName, params Type[] genericParams)
        {
            var instance   = Expression.Parameter(typeof(T), "instance");
            var methodCall = Expression.Call(instance, methodName, genericParams);
            return Expression.Lambda<Func<T, Ret>>(
               methodCall, instance
               ).Compile();
        }

        public static Func<TArg, Ret> CreateStaticFuncOneArg<T, TArg, Ret>(
             string methodName, params Type[] genericParams)
        {
            var arg        = Expression.Parameter(typeof(TArg), "arg");
            var methodCall = Expression.Call(typeof(T), methodName, genericParams, arg);
            return Expression.Lambda<Func<TArg, Ret>>(
               methodCall, arg
               ).Compile();
        }

        public static PropertyGetDelegate CreatePropertyGetter(string name, Type owner)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            var instance = Expression.Parameter(typeof(object), "instance");
            var target   = Expression.Convert(instance, owner);
            return Expression.Lambda<PropertyGetDelegate>(
                Expression.Convert(
                    Expression.Property(target, name),
                    typeof(object)
                ), instance
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

        private static readonly Type[] SimpleTypes = {
            typeof(Enum),           typeof(string),
            typeof(decimal),        typeof(DateTime),
            typeof(DateTimeOffset), typeof(TimeSpan),
            typeof(Guid)
        };
    }
}
