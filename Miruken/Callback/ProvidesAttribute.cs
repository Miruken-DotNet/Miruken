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

        public override bool Validate(
            object callback, IHandler composer, Func<object> dispatch)
        {
            var resolution  =  callback as Resolution;
            if (resolution == null) return false;
            var resolutions = resolution.Resolutions;
            var count       = resolutions.Count;

            var result = dispatch();

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

        static ProvidesAttribute()
        {
            Policy = CovariantPolicy.For<ProvidesAttribute>()
                  .HandlesCallback<Resolution>(r => r.Key,
                      x => x.MatchMethod(x.Return.Optional, x.Callback)
                            .MatchMethod(x.Return.Optional, x.Callback, x.Composer)
                            .MatchMethod(x.Return, x.Composer)
                            .MatchMethod(x.Return)
                  );
        }

        private static readonly CovariantPolicy<ProvidesAttribute, Resolution> Policy;
    }
}
