namespace Miruken.Callback
{
    using System.Linq;

    public class FilterOptions : Options<FilterOptions>
    {
        public IFilterProvider[] ExtraFilters { get; set; }

        public override void MergeInto(FilterOptions other)
        {
            if (ExtraFilters != null)
            {
                other.ExtraFilters = other.ExtraFilters?
                    .Concat(ExtraFilters).ToArray() ?? ExtraFilters;
            }
        }
    }
}
