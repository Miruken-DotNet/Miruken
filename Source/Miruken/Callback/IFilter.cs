namespace Miruken.Callback
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Policy;
    using Policy.Bindings;

    public delegate Task<Res> Next<Res>(
        IHandler composer = null, bool proceed = true);

    public interface IFilter
    {
        int? Order { get; set; }
    }

    public interface IFilter<in TCb, TRes> : IFilter
    {
        Task<TRes> Next(
            TCb callback, MemberBinding member,
            IHandler composer, Next<TRes> next,
            IFilterProvider provider = null);
    }

    public interface IFilterProvider
    {
        bool Required { get;}

        IEnumerable<IFilter> GetFilters(
            MemberBinding binding, MemberDispatch dispatcher,
            Type callbackType, IHandler composer);
    }

    public interface IValidateFilterProvider
    {
        void Validate(MemberBinding binding);
    }

    public interface IFiltered
    {
        IEnumerable<IFilterProvider> Filters { get; }
        void AddFilters(params IFilterProvider[] providers);
        void AddFilters(params Type[] filterTypes);
    }
}
