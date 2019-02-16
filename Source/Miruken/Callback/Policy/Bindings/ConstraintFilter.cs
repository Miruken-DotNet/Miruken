namespace Miruken.Callback.Policy.Bindings
{
    using System.Threading.Tasks;

    public sealed class ConstraintFilter<TRes> : IFilter<IBindingScope, TRes>
    {
        public int? Order { get; set; } = Stage.Filter;

        public Task<TRes> Next(IBindingScope callback,
             object rawCallback, MemberBinding member,
             IHandler composer, Next<TRes> next,
             IFilterProvider provider = null)
        {
            if (!(provider is ConstraintAttribute attribute))
                return next(proceed: false);
            var metadata = callback.Metadata;
            return !(metadata == null || attribute.Constraint.Matches(metadata))
                 ? next(proceed: false)
                 : next();
        }
    }
}
