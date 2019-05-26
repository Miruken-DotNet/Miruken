namespace Miruken.Api
{
    public interface IRelationship<out TRel, TId> : IResource<TId>
        where TRel : Resource<TId>
    {
        TRel Related { get; }
    }

    public class Relationship<TRel, TId> : Resource<TId>, IRelationship<TRel, TId>
         where TRel : Resource<TId>
    {
        public TRel Related { get; set; }
    }

    public class RelationshipAction<TRel, TId> : IRequest<Relationship<TRel, TId>>
         where TRel : Resource<TId>
    {
        public Resource<TId> Resource { get; set; }
        public TRel          Related  { get; set; }
    }

    public class UpdateRelationship<TRel, TId> : RelationshipAction<TRel, TId>
        where TRel : Resource<TId>
    {
    }
}
