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

    // Methods

    public delegate void   NoArgsDelegate(object instance);
    public delegate void   OneArgDelegate(object instance, object arg);
    public delegate void   TwoArgsDelegate(object instance, object arg1, object arg2);
    public delegate void   ThreeArgsDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate void   FourArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate void   FiveArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate void   SixArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate void   SevenArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    // Functions

    public delegate object NoArgsReturnDelegate(object instance);
    public delegate object OneArgReturnDelegate(object instance, object arg);
    public delegate object TwoArgsReturnDelegate(object instance, object arg1, object arg2);
    public delegate object ThreeArgsReturnDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate object FourArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate object FiveArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object SixArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate object SevenArgsReturnDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);
    public delegate object PropertyGetDelegate(object instance);

    // Constructors

    public delegate object NoArgsCtorDelegate();
    public delegate object OneArgCtorDelegate(object arg);
    public delegate object TwoArgsCtorDelegate(object arg1, object arg2);
    public delegate object ThreeArgsCtorDelegate(object arg1, object arg2, object arg3);
    public delegate object FourArgsCtorDelegate(object arg1, object arg2, object arg3, object arg4);
    public delegate object FiveArgsCtorDelegate(object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object SixArgsCtorDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate object SevenArgsCtorDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

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

        public static bool Is<T>(this Type type)
        {
            return type != null && typeof(T).IsAssignableFrom(type);
        }

        public static bool Is(this Type type, Type assignable)
        {
            return type != null && assignable.IsAssignableFrom(type);
        }

        public static bool IsSimpleType(this Type type)
        {
            if (type == null) return false;
            return type.IsPrimitive || type.IsEnum ||
                Array.IndexOf(SimpleTypes, type) >= 0 ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() ==
                typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]));
        }

        public static bool IsCollection(object instance)
        {
            return instance is IEnumerable && !(instance is string);
        }

        public static bool IsEnumDefined(object e)
        {
            try
            {
                decimal.Parse(e.ToString());
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool HasDefaultConstructor(this Type t)
        {
            return t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;
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
                while (type != null && type != typeof(object))
                {
                    if (type.IsGenericType &&
                        type.GetGenericTypeDefinition() == openType)
                        return type;
                    type = type.BaseType;
                }
            }
            return null;
        }

        public static bool SatisfiesGenericParameterConstraints(
            this Type genericArgType, Type proposedType)
        {
            var gpa         = genericArgType.GenericParameterAttributes;
            var constraints = gpa & GenericParameterAttributes.SpecialConstraintMask;

            if (constraints != GenericParameterAttributes.None)
            {
                if ((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0 &&
                    proposedType.IsValueType)
                    return false;

                if ((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 &&
                    !proposedType.IsValueType)
                    return false;

                if ((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                    proposedType.GetConstructor(Type.EmptyTypes) == null)
                    return false;
            }

            var typeConstraints = genericArgType.GetGenericParameterConstraints();
            return typeConstraints.Length == 0 || typeConstraints.Any(proposedType.Is);
        }

        public static object ChangeType<T>(object value)
        {
            return ChangeType(value, typeof(T));
        }

        public static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (value != null && conversionType.IsInstanceOfType(value))
                return value;

            if (conversionType.IsEnum)
            {
                if (value is string enumString)
                    return Enum.Parse(conversionType, enumString);
                var val = Convert.ChangeType(value, Enum.GetUnderlyingType(conversionType));
                var obj = Enum.ToObject(conversionType, val);
                if (!IsEnumDefined(obj))
                    throw new InvalidCastException($"{value} is not a valid {conversionType.GetSimpleTypeName()}");
                return obj;
            }

            if (conversionType.IsGenericType &&
                conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) return null;
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (conversionType == typeof(Guid))
                return value != null ? new Guid(value.ToString()) : Guid.Empty;

            return Convert.ChangeType(value, conversionType);
        }

        public static T[] ChangeArrayType<T>(Array array, Type conversionType)
        {
            return (T[])ChangeArrayType(array, typeof(T));
        }

        public static Array ChangeArrayType(Array array, Type conversionType)
        {
            var typed = Array.CreateInstance(conversionType, array.Length);
            for (var i = 0; i < array.Length; ++i)
                typed.SetValue(ChangeType(array.GetValue(i), conversionType), i);
            return typed;
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

        #region Action factories     

        public static TDel CreateCall<TDel>(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var target = method.ReflectedType;
            if (target == null || method.IsStatic)
                throw new NotSupportedException("Only instance methods supported");
            var parameters = method.GetParameters();
            var arguments  = CreateArguments(parameters.Length + 1);
            var methodCall = Expression.Call(
                Expression.Convert(arguments[0], target), method,
                arguments.Skip(1).Select((arg, index) =>
                        Expression.Convert(arg, parameters[index].ParameterType))
                    .ToArray());
            var lambda = method.ReturnType == typeof(void)
                       ? Expression.Lambda<TDel>(methodCall, arguments)
                       : Expression.Lambda<TDel>(
                            Expression.Convert(methodCall, typeof(object)),
                            arguments);
            return lambda.Compile();
        }

        public static NoArgsDelegate CreateCallNoArgs(MethodInfo method)
        {
            return CreateCall<NoArgsDelegate>(method);
        }

        public static OneArgDelegate CreateCallOneArg(MethodInfo method)
        {
            return CreateCall<OneArgDelegate>(method);
        }

        public static TwoArgsDelegate CreateCallTwoArgs(MethodInfo method)
        {
            return CreateCall<TwoArgsDelegate>(method);
        }

        public static ThreeArgsDelegate CreateCallThreeArgs(MethodInfo method)
        {
            return CreateCall<ThreeArgsDelegate>(method);
        }

        public static FourArgsDelegate CreateCallFourArgs(MethodInfo method)
        {
            return CreateCall<FourArgsDelegate>(method);
        }

        public static FiveArgsDelegate CreateCallFiveArgs(MethodInfo method)
        {
            return CreateCall<FiveArgsDelegate>(method);
        }

        public static SixArgsDelegate CreateCallSixArgs(MethodInfo method)
        {
            return CreateCall<SixArgsDelegate>(method);
        }

        public static SevenArgsDelegate CreateCallSevenArgs(MethodInfo method)
        {
            return CreateCall<SevenArgsDelegate>(method);
        }

        #endregion

        #region Function factories

        public static NoArgsReturnDelegate CreateFuncNoArgs(MethodInfo method)
        {
            return CreateCall<NoArgsReturnDelegate>(method);
        }

        public static OneArgReturnDelegate CreateFuncOneArg(MethodInfo method)
        {
            return CreateCall<OneArgReturnDelegate>(method);
        }

        public static TwoArgsReturnDelegate CreateFuncTwoArgs(MethodInfo method)
        {
            return CreateCall<TwoArgsReturnDelegate>(method);
        }

        public static ThreeArgsReturnDelegate CreateFuncThreeArgs(MethodInfo method)
        {
            return CreateCall<ThreeArgsReturnDelegate>(method);
        }

        public static FourArgsReturnDelegate CreateFuncFourArgs(MethodInfo method)
        {
            return CreateCall<FourArgsReturnDelegate>(method);
        }

        public static FiveArgsReturnDelegate CreateFuncFiveArgs(MethodInfo method)
        {
            return CreateCall<FiveArgsReturnDelegate>(method);
        }

        public static SixArgsReturnDelegate CreateFuncSixArgs(MethodInfo method)
        {
            return CreateCall<SixArgsReturnDelegate>(method);
        }

        public static SevenArgsReturnDelegate CreateFuncSevenArgs(MethodInfo method)
        {
            return CreateCall<SevenArgsReturnDelegate>(method);
        }

        #endregion

        #region Constructor factories

        public static TDel CreateCtor<TDel>(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));
            var parameters = constructor.GetParameters();
            var arguments  = CreateArguments(parameters.Length);
            var constructorCall = Expression.New(
                constructor,
                arguments.Select((arg, index) =>
                        Expression.Convert(arg, parameters[index].ParameterType))
                    .ToArray());
            return Expression.Lambda<TDel>(
                Expression.Convert(constructorCall, typeof(object)),
                arguments
            ).Compile();
        }

        public static NoArgsCtorDelegate CreateCtorNoArgs(ConstructorInfo constructor)
        {
            return CreateCtor<NoArgsCtorDelegate>(constructor);
        }

        public static OneArgCtorDelegate CreateCtorOneArg(ConstructorInfo constructor)
        {
            return CreateCtor<OneArgCtorDelegate>(constructor);
        }

        public static TwoArgsCtorDelegate CreateCtorTwoArgs(ConstructorInfo constructor)
        {
            return CreateCtor<TwoArgsCtorDelegate>(constructor);
        }

        public static ThreeArgsCtorDelegate CreateCtorThreeArgs(ConstructorInfo constructor)
        {
            return CreateCtor<ThreeArgsCtorDelegate>(constructor);
        }

        public static FourArgsCtorDelegate CreateCtorFourArgs(ConstructorInfo constructor)
        {
            return CreateCtor<FourArgsCtorDelegate>(constructor);
        }

        public static FiveArgsCtorDelegate CreateCtorFiveArgs(ConstructorInfo constructor)
        {
            return CreateCtor<FiveArgsCtorDelegate>(constructor);
        }

        public static SixArgsCtorDelegate CreateCtorSixArgs(ConstructorInfo constructor)
        {
            return CreateCtor<SixArgsCtorDelegate>(constructor);
        }

        public static SevenArgsCtorDelegate CreateCtorSevenArgs(ConstructorInfo constructor)
        {
            return CreateCtor<SevenArgsCtorDelegate>(constructor);
        }

        #endregion

        public static Func<T, TRet> CreateGenericFuncNoArgs<T, TRet>(
            string methodName, params Type[] genericParams)
        {
            var instance   = Expression.Parameter(typeof(T), "instance");
            var methodCall = Expression.Call(instance, methodName, genericParams);
            return Expression.Lambda<Func<T, TRet>>(
               methodCall, instance
               ).Compile();
        }

        public static Func<TArg, TRet> CreateStaticFuncOneArg<T, TArg, TRet>(
             string methodName, params Type[] genericParams)
        {
            var arg        = Expression.Parameter(typeof(TArg), "arg");
            var methodCall = Expression.Call(typeof(T), methodName, genericParams, arg);
            return Expression.Lambda<Func<TArg, TRet>>(
               methodCall, arg
               ).Compile();
        }

        public static PropertyGetDelegate CreatePropertyGetter(string name, Type owner)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(@"Name cannot be empty", nameof(name));
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

        public static T[] Normalize<T>(this T[] array)
        {
            if (array == null) return null;
            return array.Length == 0 ? Array.Empty<T>() : array;
        }

        private static ParameterExpression[] CreateArguments(int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => Expression.Parameter(typeof(object), $"argument{index}"))
                .ToArray();
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
