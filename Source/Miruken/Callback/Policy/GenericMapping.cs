namespace Miruken.Callback.Policy;

using System;
using System.Collections.Generic;
using System.Linq;

public class GenericMapping
{
    private readonly Tuple<int, int>[] _mapping;

    private const int UseReturn   = -1;
    private const int UseArgument = -2;

    public GenericMapping(Type[] open, IEnumerable<Argument> args, Type returnType = null)
    {
        _mapping = new Tuple<int, int>[open.Length];

        var argSources = args
            .Select((arg, i) => new { i, arg.ParameterType })
            .Where(p => p.ParameterType.ContainsGenericParameters)
            .Select(p => Tuple.Create(p.i, p.ParameterType))
            .ToList();

        if (returnType?.ContainsGenericParameters == true)
            argSources.Add(Tuple.Create(UseReturn, returnType));

        foreach (var (idx, argType) in argSources)
        {
            if (argType.IsGenericParameter)
            {
                if (open.Length == 1 && open[0] == argType)
                    _mapping[0] = Tuple.Create(idx, UseArgument);
                continue;
            }
            var sourceGenericArgs = argType.GetGenericArguments();
            for (var i = 0; i < open.Length; ++i)
            {
                var index = Array.IndexOf(sourceGenericArgs, open[i]);
                if (index >= 0)
                    _mapping[i] = Tuple.Create(idx, index);
            }
            if (!_mapping.Contains(null)) break;
        }

        Complete = !_mapping.Contains(null);
    }

    public bool Complete { get; }

    public Type[] MapTypes(Type[] types, Type returnType = null)
    {
        return _mapping.Select(mapping =>
        {
            var (idx, use) = mapping;
            switch (idx)
            {
                case UseReturn when returnType == null:
                    throw new ArgumentException(
                        "Return type is unknown and cannot infer types");
                case UseReturn:
                    return use != UseArgument
                        ? returnType.GetGenericArguments()[use]
                        : returnType;
            }
            if (use == UseArgument) return types[idx];
            var arg = types[idx];
            if (arg == null)
                throw new ArgumentException($"Argument {idx} is null and cannot infer types");
            return arg.GetGenericArguments()[use];
        }).ToArray();
    }
}