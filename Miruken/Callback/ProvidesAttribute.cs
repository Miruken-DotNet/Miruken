namespace Miruken.Callback
{
    using System.Linq;
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

        private class ResolutionFilter : IDynamicFilter
        {
            public ResolutionFilter()
            {
                Order = int.MinValue;
            }

            public int? Order { get; set; }

            public object Filter(object callback, MethodBinding method, 
                IHandler composer, FilterDelegate<object> proceed)
            {
                var resolution  = callback as Resolution;
                if (resolution == null) return null;
                var resolutions = resolution.Resolutions;
                var count       = resolutions.Count;

                var result = proceed();

                if (result != null)
                {
                    var many = result as object[];
                    if (many != null)
                    {
                        var resolved = false;
                        foreach (var item in many)
                        {
                            resolved = resolution.Resolve(item, composer)
                                    || resolved;
                            if (resolved && !resolution.Many)
                                break;
                        }
                        return resolved ? result : null;
                    }
                    return resolution.Resolve(result, composer) 
                         ? result : null;
                }

                return resolutions.Count > count ? resolutions.Last() : null;
            }
        }

        public static readonly CovariantPolicy<Resolution> Policy =
             CovariantPolicy.Create<Resolution>(r => r.Key,
                x => x.MatchMethod(x.Return.OrVoid, x.Callback, x.Composer.Optional)
                      .MatchMethod(x.Return, x.Composer.Optional)
                      .Filters(new ResolutionFilter())
                );
    }
}
