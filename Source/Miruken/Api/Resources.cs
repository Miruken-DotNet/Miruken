namespace Miruken.Api
{
    using System;

    public interface IResource<out TId>
    {
        TId       Id         { get; }
        byte[]    RowVersion { get; }
        DateTime? Created    { get; }
        string    CreatedBy  { get; }
        DateTime? Modified   { get; }
        string    ModifiedBy { get; }
    }

    public class Resource<TId> : IResource<TId>
    {
        public TId       Id         { get; set; }
        public byte[]    RowVersion { get; set; }
        public DateTime? Created    { get; set; }
        public string    CreatedBy  { get; set; }
        public DateTime? Modified   { get; set; }
        public string    ModifiedBy { get; set; }
    }

    public interface IResourceAction<TRes, TId> where TRes : Resource<TId>
    {
        TRes Resource { get; set; }
    }

    public class ResourceAction<TRes, TId> : IRequest<TRes>, IResourceAction<TRes, TId>
        where TRes : Resource<TId>
    {
        public ResourceAction()
        {
        }

        public ResourceAction(TRes resource)
        {
            Resource = resource;
        }

        public TRes Resource { get; set; }
    }

    public class UpdateResource<TRes, TId> : ResourceAction<TRes, TId>
        where TRes : Resource<TId>
    {
        public UpdateResource()
        {
        }

        public UpdateResource(TRes resource)
        {
            Resource = resource;
        }
    }
}

