namespace Miruken.Callback
{
    using System;
    using System.Reflection;
    using Policy;

    public class ProvidesAttribute : DefinitionAttribute
    {
        public ProvidesAttribute()
        {
        }

        public ProvidesAttribute(object key)
        {
            Key = key;
        }

        public override MethodDefinition Match(MethodInfo method)
        {
            return Policy.Match(method, this);
        }

        static ProvidesAttribute()
        {
            Policy = CovariantPolicy.For<ProvidesAttribute>()
                    .HandlesCallback<Resolution>(r => r.Key,
                        x => x.MatchMethod(x.Return.Optional, x.Callback)
                            .MatchMethod(x.Return.Optional, x.Callback, x.Composer)
                            .MatchMethod(x.Return, x.Composer)
                            .MatchMethod(x.Return)
                            .Create((m,r,a,rt) => new ProvidesMethod(m,r,a,rt))
                        );
        }

        private class ProvidesMethod : CovariantMethod<ProvidesAttribute>
        {
            public ProvidesMethod(
                MethodInfo method, MethodRule<ProvidesAttribute> rule,
                ProvidesAttribute attribute, Func<object, Type> returnType)
                : base(method, rule, attribute, returnType)
            {
            }

            protected override bool Verify(object target, object callback,
                IHandler composer)
            {
                var resolution = (Resolution) callback;
                var resolutions = resolution.Resolutions;
                var count = resolutions.Count;

                var result = Invoke(target, callback, composer);

                if (result != null)
                {
                    var array = result as object[];
                    if (array != null)
                    {
                        var resolved = false;
                        foreach (var item in array)
                        {
                            resolved = resolution.Resolve(item, composer)
                                       || resolved;
                            if (resolved && !resolution.Many)
                                break;
                        }
                        return resolved;
                    }
                    return resolution.Resolve(result, composer);
                }

                return resolutions.Count > count;
            }
        }

        private static readonly CovariantPolicy<ProvidesAttribute, Resolution> Policy;
    }
}
