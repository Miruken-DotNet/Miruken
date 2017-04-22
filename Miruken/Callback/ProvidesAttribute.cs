namespace Miruken.Callback
{
    using System;
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

        public override CallbackPolicy CallbackPolicy => Policy;

        private class ProvidesMethod : MethodBinding
        {
            public ProvidesMethod(
                MethodRule rule, MethodDispatch dispatch,
                DefinitionAttribute attribute, CallbackPolicy policy)
                : base(rule, dispatch, attribute, policy)
            {
            }

            public override bool Dispatch(
                object target, object callback, IHandler composer)
            {
                var resolution  = (Resolution)callback;
                var resolutions = resolution.Resolutions;
                var returnType  = resolution.Key as Type;
                var count       = resolutions.Count;

                var result = Invoke(target, callback, composer, returnType);

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

        public static readonly CovariantPolicy<Resolution> Policy =
             CovariantPolicy.Create<Resolution>(r => r.Key,
                x => x.MatchMethod(x.Return.OrVoid, x.Callback, x.Composer.Optional)
                      .MatchMethod(x.Return, x.Composer.Optional)
                      .BindMethod((r,d,a,p) => new ProvidesMethod(r,d,a,p))
                );
    }
}
