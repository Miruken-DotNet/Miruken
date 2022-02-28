namespace Miruken.Infrastructure;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using FastExpressionCompiler;

public static class RuntimeHelper
{
    private static readonly ConcurrentDictionary<Type, object>
        DefaultValues = new();

    public static object GetDefault(Type type)
    {
        return type is {IsValueType: true} && type != typeof(void)
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
               type.IsGenericType && type.GetGenericTypeDefinition() ==
               typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]);
    }

    public static bool IsGenericEnumerable(this Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }

    public static bool IsCollection(object instance)
    {
        return instance is IEnumerable and not string;
    }

    public static bool IsEnumDefined(object e)
    {
        try
        {
            var _ = decimal.Parse(e.ToString());
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

    public static Type[] GetTopLevelInterfaces(this Type type)
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
        var gpa = genericArgType.GenericParameterAttributes;
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
            if (conversionType == null)
                throw new InvalidOperationException("Could not get underlying nullable type.");
        }

        if (conversionType == typeof(Guid))
            return value != null ? new Guid(value.ToString()) : Guid.Empty;

        return Convert.ChangeType(value, conversionType);
    }

    public static T[] ChangeArrayType<T>(Array array)
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

    public static MethodInfo SelectMethod(
        MethodInfo sourceMethod, Type type,
        BindingFlags binding = BindingFlags.Public)
    {
        var key = new KeyValuePair<MethodInfo, Type>(sourceMethod, type);
        return MethodMapping.GetOrAdd(key, k => MatchMethod(k.Key, k.Value, binding));
    }

    private static MethodInfo MatchMethod(
        MethodInfo sourceMethod, Type type, BindingFlags binding)
    {
        Type[] genericArguments = null;
        if (sourceMethod.IsGenericMethod)
        {
            genericArguments = sourceMethod.GetGenericArguments();
            sourceMethod = sourceMethod.GetGenericMethodDefinition();
        }
        var declaringType = sourceMethod.DeclaringType;
        MethodInfo methodOnTarget = null;
        if (declaringType.GetTypeInfo().IsInterface &&
            declaringType?.IsAssignableFrom(type) == true)
        {
            var mapping = type.GetTypeInfo().GetRuntimeInterfaceMap(declaringType);
            var index = Array.IndexOf(mapping.InterfaceMethods, sourceMethod);
            methodOnTarget = mapping.TargetMethods[index];
        }
        else
        {
            var methods = type.GetMethods(BindingFlags.Instance | binding);
            foreach (var method in methods)
            {
                if (MethodSignatureComparer.Instance.Equals(
                        method.GetBaseDefinition(), sourceMethod))
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
        MethodMapping = new();

    public static Delegate CompileMethod(MethodInfo method, Type delegateType)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        var isStatic = method.IsStatic;
        var target = method.ReflectedType;
        if (!isStatic && target == null)
            throw new ArgumentException("Instance method requires a target");
        var parameters = method.GetParameters();
        var arguments = CreateArguments(parameters.Length + (isStatic ? 0 : 1));
        var methodCall = isStatic ?
            Expression.Call(method, arguments.Select((arg, index) =>
                (Expression)Expression.Convert(arg, parameters[index].ParameterType)).ToArray())
            : Expression.Call(Expression.Convert(arguments[0], target), method,
                arguments.Skip(1).Select((arg, index) =>
                        (Expression)Expression.Convert(arg, parameters[index].ParameterType))
                    .ToArray());
        var lambda = method.ReturnType == typeof(void)
            ? Expression.Lambda(delegateType, methodCall, arguments)
            : Expression.Lambda(delegateType,
                Expression.Convert(methodCall, typeof(object)),
                arguments);
        return lambda.TryCompileWithoutClosure<Delegate>();
    }

    public static Delegate CompileConstructor(ConstructorInfo constructor, Type delegateType)
    {
        if (constructor == null)
            throw new ArgumentNullException(nameof(constructor));
        var parameters = constructor.GetParameters();
        var arguments = CreateArguments(parameters.Length);
        var constructorCall = Expression.New(
            constructor,
            arguments.Select((arg, index) => 
                    (Expression)Expression.Convert(arg, parameters[index].ParameterType))
                .ToArray());
        return Expression.Lambda(delegateType,
            Expression.Convert(constructorCall, typeof(object)),
            arguments
        ).TryCompileWithoutClosure<Delegate>();
    }

    public static Func<T, TRet> CreateGenericFuncNoArgs<T, TRet>(
        string methodName, params Type[] genericParams)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var methodCall = Expression.Call(instance, methodName, genericParams);
        return Expression.Lambda<Func<T, TRet>>(
            methodCall, instance).TryCompileWithoutClosure<Func<T, TRet>>();
    }

    public static Func<TArg, TRet> CreateStaticFuncOneArg<T, TArg, TRet>(
        string methodName, params Type[] genericParams)
    {
        var arg = Expression.Parameter(typeof(TArg), "arg");
        var methodCall = Expression.Call(typeof(T), methodName, genericParams, arg);
        return Expression.Lambda<Func<TArg, TRet>>(methodCall, arg)
            .TryCompileWithoutClosure<Func<TArg, TRet>>();
    }

    public static Func<object, object> CreatePropertyGetter(string name, Type owner)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException(@"Name cannot be empty", nameof(name));
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        var instance = Expression.Parameter(typeof(object), "instance");
        var target = Expression.Convert(instance, owner);
        return Expression.Lambda<Func<object, object>>(
            Expression.Convert(
                Expression.Property(target, name),
                typeof(object)
            ), instance
        ).TryCompileWithoutClosure<Func<object, object>>();
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

        var writingAssemblyName = false;
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