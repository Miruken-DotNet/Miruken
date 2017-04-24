namespace Miruken.Callback
{
    using System;
    using System.Linq;

    public class FilterOptions : Options<FilterOptions>
    {
        public bool?             SuppressFilters   { get; set; }
        public Type[]            SuppressedFilters { get; set; }
        public IFilterProvider[] ExtraProviders    { get; set; }
        public IFilter[]         ExtraFilters      { get; set; }

        public override void MergeInto(FilterOptions other)
        {
            if (SuppressFilters.HasValue && !other.SuppressFilters.HasValue)
                other.SuppressFilters = SuppressFilters;

            if (SuppressedFilters != null)
            {
                other.SuppressedFilters = other.SuppressedFilters?
                    .Concat(SuppressedFilters).ToArray() ?? SuppressedFilters;
            }

            if (ExtraProviders != null)
            {
                other.ExtraProviders = other.ExtraProviders?
                    .Concat(ExtraProviders).ToArray() ?? ExtraProviders;
            }

            if (ExtraFilters != null)
            {
                other.ExtraFilters = other.ExtraFilters?
                    .Concat(ExtraFilters).ToArray() ?? ExtraFilters;
            }
        }
    }
}
