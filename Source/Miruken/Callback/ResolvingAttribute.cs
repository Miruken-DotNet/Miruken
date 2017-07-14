namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;
    using Policy;

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ResolvingAttribute : Attribute
    {
        [Flags]
        public enum ModifierFlags
        {
            None    = 0,
            All     = 1 << 0,
            Lazy    = 1 << 1,
            Task    = 1 << 2,
            Promise = 1 << 3,
            Simple  = 1 << 4,
        }

        public object ResolveParameter(
            PolicyMethodBinding binding, ParameterInfo parameter,
            IHandler composer)
        {
            var paramType = parameter.ParameterType;
            if (paramType == typeof(IHandler))
                return composer;
            if (paramType.IsInstanceOfType(binding))
                return binding;
            var type      = paramType;
            var modifiers = GetModifiers(ref type);
            return ResolveDependency(parameter, type, modifiers, composer);
        }

        protected virtual object ResolveDependency(
            ParameterInfo parameter, Type type, ModifierFlags modifiers,
            IHandler composer)
        {
            object dependency;
            var paramType = parameter.ParameterType;
            var isPromise = (modifiers & ModifierFlags.Promise) > 0;
            var isTask    = (modifiers & ModifierFlags.Task) > 0;
            var isSimple  = (modifiers & ModifierFlags.Simple) > 0;
            var key       = isSimple ? parameter.Name : (object)type;

            if ((modifiers & ModifierFlags.All) > 0)
            {
                if (isPromise)
                    dependency = composer.ResolveAllAsync(key)
                        .Coerce(paramType);
                else if (isTask)
                    dependency = composer.ResolveAllAsync(key).ToTask()
                        .Coerce(paramType);
                else
                    dependency = composer.ResolveAll(key);
            }
            else if (isPromise)
                dependency = composer.ResolveAsync(key)
                    .Coerce(paramType);
            else if (isTask)
                dependency = composer.ResolveAsync(key).ToTask()
                    .Coerce(paramType);
            else
            {
                dependency = composer.Resolve(key);
                if (isSimple && !paramType.IsInstanceOfType(dependency))
                    dependency = RuntimeHelper.ChangeType(dependency, paramType);
            }

            return dependency;
        }

        private static ModifierFlags GetModifiers(ref Type type)
        {
            var modifiers = ModifierFlags.None;
            if (IsLazy(ref type))
                modifiers |= ModifierFlags.Lazy;
            if (IsPromise(ref type))
                modifiers |= ModifierFlags.Promise;
            else if (IsTask(ref type))
                modifiers |= ModifierFlags.Task;
            if (IsArray(ref type))
                modifiers |= ModifierFlags.All;
            if (type.IsSimpleType())
                modifiers |= ModifierFlags.Simple;
            return modifiers;
        }

        private static bool IsPromise(ref Type type)
        {
            var promise = type.GetOpenTypeConformance(typeof(Promise<>));
            if (promise == null) return false;
            type = promise.GetGenericArguments()[0];
            return true;
        }

        private static bool IsTask(ref Type type)
        {
            var task = type.GetOpenTypeConformance(typeof(Task<>));
            if (task == null) return false;
            type = task.GetGenericArguments()[0];
            return true;
        }

        private static bool IsArray(ref Type type)
        {
            if (!type.IsArray) return false;
            type = type.GetElementType();
            return true;
        }

        private static bool IsLazy(ref Type type)
        {
            if (!type.IsGenericType ||
                type.GetGenericTypeDefinition() != typeof(Func<>))
                return false;
            type = type.GetGenericArguments()[0];
            return true;
        }
    }
}
