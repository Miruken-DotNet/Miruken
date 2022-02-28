namespace Miruken.Callback.Policy;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Infrastructure;

public class GenericMethodDispatch : MemberDispatch
{
    private readonly GenericMapping _mapping;
    private readonly ConcurrentDictionary<MethodInfo, MethodDispatch> _closed;

    public GenericMethodDispatch(
        MethodInfo method, int ruleArgCount,
        Attribute[] attributes = null)
        : base(method, method.ReturnType, attributes)
    {
        if (!method.ContainsGenericParameters)
        {
            throw new ArgumentException(
                $"Method '{Method.GetDescription()}' is not generic");
        }

        _closed  = new ConcurrentDictionary<MethodInfo, MethodDispatch>();
        _mapping = CreateGenericMapping(ruleArgCount);
    }

    public MethodInfo Method => (MethodInfo)Member;

    public override object Invoke(object target, object[] args, Type returnType = null)
    {
        var closedMethod = CloseDispatch(args, returnType);
        return closedMethod.Invoke(target, args, returnType);
    }

    public MemberDispatch CloseDispatch(object[] args, Type returnType = null)
    {
        var closedMethod = ClosedMethod(args, returnType);
        return _closed.GetOrAdd(closedMethod, m => new MethodDispatch(m, Attributes));
    }

    private MethodInfo ClosedMethod(IEnumerable<object> args, Type returnType)
    {
        var types = args.Select((arg, index) =>
        {
            var type = arg.GetType();
            if (type.IsGenericType) return type;
            var paramType = Arguments[index].ParameterType;
            if (!paramType.IsGenericParameter &&
                paramType.ContainsGenericParameters)
                type = type.GetOpenTypeConformance(paramType.GetGenericTypeDefinition());
            return type;
        }).ToArray();
        var argTypes = _mapping.MapTypes(types, returnType);
        return Method.MakeGenericMethod(argTypes);
    }

    private GenericMapping CreateGenericMapping(int ruleArgCount)
    {
        var ruleArgs    = Arguments.Take(ruleArgCount);
        var genericArgs = Method.GetGenericArguments();
        var mapping     = new GenericMapping(genericArgs, ruleArgs, LogicalReturnType);
        if (!mapping.Complete)
        {
            throw new InvalidOperationException(
                $"Type mapping for {Method.GetDescription()} could not be inferred");
        }
        return mapping;
    }
}