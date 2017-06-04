using System;
using System.Collections.Generic;
using System.Reflection;

namespace Miruken.Infrastructure
{
    public class MethodSignatureComparer : IEqualityComparer<MethodInfo>
    {
        public static readonly MethodSignatureComparer Instance = new MethodSignatureComparer();

        public bool EqualGenericParameters(MethodInfo x, MethodInfo y)
        {
            if (x.IsGenericMethod != y.IsGenericMethod) return false;

            if (x.IsGenericMethod)
            {
                var xArgs = x.GetGenericArguments();
                var yArgs = y.GetGenericArguments();

                if (xArgs.Length != yArgs.Length) return false;

                for (var i = 0; i < xArgs.Length; ++i)
                {
                    if (xArgs[i].GetTypeInfo().IsGenericParameter != yArgs[i].GetTypeInfo().IsGenericParameter)
                        return false;

                    if (!xArgs[i].GetTypeInfo().IsGenericParameter && !xArgs[i].Equals(yArgs[i]))
                        return false;
                }
            }

            return true;
        }

        public bool EqualParameters(MethodInfo x, MethodInfo y)
        {
            var xArgs = x.GetParameters();
            var yArgs = y.GetParameters();

            if (xArgs.Length != yArgs.Length)
            {
                return false;
            }

            for (var i = 0; i < xArgs.Length; ++i)
            {
                if (!EqualSignatureTypes(xArgs[i].ParameterType, yArgs[i].ParameterType))
                    return false;
            }

            return true;
        }

        public bool EqualSignatureTypes(Type x, Type y)
        {
            if (x.GetTypeInfo().IsGenericParameter != y.GetTypeInfo().IsGenericParameter)
                return false;

            if (x.GetTypeInfo().IsGenericParameter)
            {
                if (x.GenericParameterPosition != y.GenericParameterPosition)
                    return false;
            }
            else if (!x.Equals(y))
                return false;

            return true;
        }

        public bool Equals(MethodInfo x, MethodInfo y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return EqualNames(x, y) &&
                   EqualGenericParameters(x, y) &&
                   EqualSignatureTypes(x.ReturnType, y.ReturnType) &&
                   EqualParameters(x, y);
        }

        public int GetHashCode(MethodInfo obj)
        {
            return obj.Name.GetHashCode() ^ obj.GetParameters().Length;
        }

        private static bool EqualNames(MethodInfo x, MethodInfo y)
        {
            return x.Name == y.Name;
        }
    }
}
