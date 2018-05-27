﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;

    public class GenericMapping
    {
        private readonly Tuple<int, int>[] _mapping;

        private const int UseReturn   = -1;
        private const int UseArgument = -2;

        public GenericMapping(Type[] open, Argument[] args,
                              Type returnType = null)
        {
            _mapping = new Tuple<int, int>[open.Length];

            var argSources = args
               .Select((arg, i) => new { i, arg.ParameterType })
               .Where(p => p.ParameterType.ContainsGenericParameters)
               .Select(p => Tuple.Create(p.i, p.ParameterType))
               .ToList();

            if (returnType?.ContainsGenericParameters == true)
                argSources.Add(Tuple.Create(UseReturn, returnType));

            foreach (var source in argSources)
            {
                var sourceArg = source.Item2;
                if (sourceArg.IsGenericParameter)
                {
                    if (open.Length == 1 && open[0] == sourceArg)
                        _mapping[0] = Tuple.Create(source.Item1, UseArgument);
                    continue;
                }
                var sourceGenericArgs = sourceArg.GetGenericArguments();
                for (var i = 0; i < open.Length; ++i)
                {
                    if (_mapping[i] != null) continue;
                    var index = Array.IndexOf(sourceGenericArgs, open[i]);
                    if (index >= 0)
                        _mapping[i] = Tuple.Create(source.Item1, index);
                }
                if (!_mapping.Contains(null)) break;
            }

            Complete = !_mapping.Contains(null);
        }

        public bool Complete { get; }

        public Type[] MapArgs(object[] args, Type returnType = null)
        {
            var types = args.Select(arg => arg.GetType()).ToArray();
            return MapTypes(types, returnType);
        }

        public Type[] MapTypes(Type[] types, Type returnType = null)
        {
            return _mapping.Select(mapping =>
            {
                switch (mapping.Item1)
                {
                    case UseReturn when returnType == null:
                        throw new ArgumentException(
                            "Return type is unknown and cannot infer types");
                    case UseReturn:
                        return mapping.Item2 != UseArgument
                            ? returnType.GetGenericArguments()[mapping.Item2]
                            : returnType;
                }
                if (mapping.Item2 == UseArgument)
                    return types[mapping.Item1];
                var arg = types[mapping.Item1];
                if (arg == null)
                    throw new ArgumentException($"Argument {mapping.Item1} is null and cannot infer types");
                return arg.GetGenericArguments()[mapping.Item2];
            }).ToArray();
        }
    }
}
