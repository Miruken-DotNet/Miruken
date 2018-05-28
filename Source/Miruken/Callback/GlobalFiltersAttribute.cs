namespace Miruken.Callback
{
    public class GlobalFiltersAttribute : FilterAttribute
    {
        public GlobalFiltersAttribute()
            : base(typeof(IGlobalFilter<,>))
        {
            Many = true;
        }
    }
}
