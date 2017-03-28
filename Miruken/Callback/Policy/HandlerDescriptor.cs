namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class HandlerDescriptor
    {
        private readonly Dictionary<Type, List<DefinitionAttribute>> _definitions;

        public HandlerDescriptor(IReflect type)
        {
            foreach (var method in type.GetMethods(Binding))
            {
                if (method.IsSpecialName || method.IsFamily ||
                    method.DeclaringType == typeof (object))
                    continue;

                var definitions = (DefinitionAttribute[])
                    Attribute.GetCustomAttributes(method, typeof (DefinitionAttribute));

                foreach (var definition in definitions)
                {
                    definition.Init(method);

                    if (_definitions == null)
                        _definitions = new Dictionary<Type, List<DefinitionAttribute>>();

                    List<DefinitionAttribute> members;
                    var definitionType = definition.GetType();
                    if (!_definitions.TryGetValue(definitionType, out members))
                    {
                        members = new List<DefinitionAttribute>();
                        _definitions.Add(definitionType, members);
                    }

                    for (var index = 0; index <= members.Count; ++index)
                    {
                        // maintain partial ordering by variance
                        if (definition.Untyped || index >= members.Count)
                            members.Add(definition);
                        else if (definition.CompareTo(members[index]) < 0)
                            members.Insert(index, definition);
                        else continue;
                        break;
                    }
                }
            }
        }

        internal bool Dispatch(
            Type type, object target, object callback,
            bool greedy, IHandler composer)
        {
            if (callback == null) return false;

            List<DefinitionAttribute> definitions;
            if (_definitions == null ||
                !_definitions.TryGetValue(type, out definitions))
                return false;

            var dispatched   = false;
            var oldUnhandled = HandleMethod.Unhandled;

            try
            {
                foreach (var definition in definitions)
                {
                    HandleMethod.Unhandled = false;
                    var handled = definition.Dispatch(target, callback, composer);
                    dispatched = (handled && !HandleMethod.Unhandled) || dispatched;
                    if (dispatched && !greedy)
                        return true;
                }
            }
            finally
            {
                HandleMethod.Unhandled = oldUnhandled;
            }

            return dispatched;
        }

        public const BindingFlags Binding = BindingFlags.Instance 
                                          | BindingFlags.Public 
                                          | BindingFlags.NonPublic;
    }
}