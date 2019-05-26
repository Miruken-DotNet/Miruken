namespace Miruken.Validate.FluentValidation
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using global::FluentValidation;
    using global::FluentValidation.Validators;
    using Validate;

    public abstract class CheckRelatedConcurrency<TId, TEntity, TRel>
        : AbstractValidator<RelationshipAction<TRel, TId?>>
        where TEntity : class, IIdentifiable<TId>, IVersioned
        where TRel : Resource<TId?>
        where TId : struct
    {
        protected CheckRelatedConcurrency()
        {
            RuleFor(c => c).CustomAsync(BeExpectedVersion);
        }

        protected abstract Task<TEntity> GetEntity(
            RelationshipAction<TRel, TId?> action, IHandler composer);

        private async Task BeExpectedVersion(RelationshipAction<TRel, TId?> action,
            CustomContext context, CancellationToken cancellation)
        {
            var resource = action.Resource;

            var composer = context.ParentContext?.GetComposer();
            if (composer == null)
                throw new InvalidOperationException("Composer is required");

            var entity = await composer.Proxy<IStash>()
                .GetOrPut(async () => await GetEntity(action, composer));

            if (entity == null)
            {
                context.AddFailure(typeof(TEntity).Name,
                    $"{typeof(TEntity)} with id {resource.Id} not found.");
                return;
            }

            if (!entity.Id.Equals(resource.Id))
            {
                context.AddFailure($"{entity.GetType()}.Id",
                    $"{entity.GetType()} has id {entity.Id} but expected id {resource.Id}.");
                return;
            }

            if (resource.RowVersion?.SequenceEqual(entity.RowVersion) == false)
                throw new OptimisticConcurrencyException(
                    $"Concurrency exception detected for {entity.GetType()} with id {entity.Id}.");
        }
    }

    public abstract class CheckRelatedConcurrency<TId, TEntity, TRelated, TRel> 
        : AbstractValidator<UpdateRelationship<TRel, TId?>>
        where TEntity  : class, IIdentifiable<TId>, IVersioned
        where TRelated : class, IIdentifiable<TId>, IVersioned
        where TRel     : Resource<TId?>
        where TId : struct
    {
        protected CheckRelatedConcurrency()
        {
            RuleFor(c => c).CustomAsync(BeExpectedVersion);
        }

        protected abstract Task<TEntity> GetEntity(
            UpdateRelationship<TRel, TId?> action, IHandler composer);

        protected abstract Task<TRelated> GetRelated(
            TEntity entity, UpdateRelationship<TRel, TId?> action, IHandler composer);

        private async Task BeExpectedVersion(UpdateRelationship<TRel, TId?> action,
            CustomContext context, CancellationToken cancellation)
        {
            var resource = action.Resource;
            var related  = action.Related;

            var composer = context.ParentContext?.GetComposer();
            if (composer == null)
                throw new InvalidOperationException("Composer is required");

            var entity = await composer.Proxy<IStash>()
                .GetOrPut(async () => await GetEntity(action, composer));

            if (entity == null)
            {
                context.AddFailure(typeof(TEntity).Name,
                    $"{typeof(TEntity)} with id {resource.Id} not found.");
                return;
            }

            var relatedEntity = await composer.Proxy<IStash>()
                .GetOrPut(async () => await GetRelated(entity, action, composer));

            if (relatedEntity == null)
            {
                context.AddFailure(typeof(TRelated).Name,
                    $"{typeof(TRelated)} with id {related.Id} not found.");
                return;
            }

            if (!relatedEntity.Id.Equals(related.Id))
            {
                context.AddFailure($"{related.GetType()}.Id",
                    $"{relatedEntity.GetType()} has id {relatedEntity.Id} but expected id {related.Id}.");
                return;
            }

            if (related.RowVersion?.SequenceEqual(relatedEntity.RowVersion) == false)
                throw new OptimisticConcurrencyException(
                    $"Concurrency exception detected for {relatedEntity.GetType()} with id {relatedEntity.Id}.");
        }
    }
}
