namespace Miruken.Callback
{
    public class VarianceOptions : Options<VarianceOptions>
    {
        public bool? Invariant { get; set; }

        public override void MergeInto(VarianceOptions other)
        {
            if (Invariant.HasValue && !other.Invariant.HasValue)
                other.Invariant = Invariant;
        }
    }

    public static class VarianceExtensions
    {
        public static IHandler Invariant(this IHandler handler)
        {
            return handler == null ? null :
                new VarianceOptions { Invariant = true }
                .Decorate(handler);
        }
    }
}
