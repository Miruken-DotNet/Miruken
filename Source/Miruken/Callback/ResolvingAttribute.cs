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
            IHandler handler)
        {
            var paramType = parameter.ParameterType;
            var type      = paramType;
            var modifiers = GetModifiers(ref type);
            return Resolve(parameter, type, modifiers, handler);
        }

        protected virtual object Resolve(
            ParameterInfo parameter, Type type, ModifierFlags modifiers,
            IHandler handler)
        {
            object dependency;
            var paramType = parameter.ParameterType;
            var isArray   = (modifiers & ModifierFlags.All) > 0;
            var isPromise = (modifiers & ModifierFlags.Promise) > 0;
            var isTask    = (modifiers & ModifierFlags.Task) > 0;
            var isSimple  = (modifiers & ModifierFlags.Simple) > 0;
            var key       = isSimple ? parameter.Name : (object)type;

            if (isArray)
            {
                if (isPromise)
                {
                    var array = handler.ResolveAllAsync(key);
                    if (isSimple)
                        array = array.Then((arr, s) => ConvertArray(arr, type));
                    dependency = array.Coerce(paramType);
                }
                else if (isTask)
                {
                    var array = handler.ResolveAllAsync(key).ToTask();
                    if (isSimple)
                        array = array.ContinueWith(t => ConvertArray(t.Result, type));
                    return array.Coerce(paramType);
                }
                else
                    dependency = ConvertArray(handler.ResolveAll(key), type);
            }
            else if (isPromise)
            {
                var promise = handler.ResolveAsync(key);
                if (isSimple)
                    promise = promise.Then((r,s) => RuntimeHelper.ChangeType(r, type));
               dependency = promise.Coerce(paramType);
            }
            else if (isTask)
            {
                var task = handler.ResolveAsync(key).ToTask();
                if (isSimple)
                    task = task.ContinueWith(t => RuntimeHelper.ChangeType(t.Result, type));
                dependency = task.Coerce(paramType);
            }
            else
            {
                dependency = handler.Resolve(key);
                if (isSimple)
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

        private static object[] ConvertArray(object[] array, Type type)
        {
            var typed = (object[])Array.CreateInstance(type, array.Length);
            for (var i = 0; i < array.Length; ++i)
                typed[i] = RuntimeHelper.ChangeType(array[i], type);
            return typed;
        }
    }
}
