namespace Miruken.Callback;

using System.Linq;

public class FilterOptions : Options<FilterOptions>
{
    public bool? SkipFilters { get; set; }

    public IFilterProvider[] ExtraFilters { get; set; }

    public override void MergeInto(FilterOptions other)
    {
        if (SkipFilters != null && other.SkipFilters == null)
            other.SkipFilters = SkipFilters;

        if (ExtraFilters != null)
        {
            other.ExtraFilters = other.ExtraFilters?
                .Concat(ExtraFilters).ToArray() ?? ExtraFilters;
        }
    }
}