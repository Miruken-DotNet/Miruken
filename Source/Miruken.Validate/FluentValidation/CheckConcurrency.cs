namespace Miruken.Validate.FluentValidation
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using global::FluentValidation;
    using global::FluentValidation.Validators;
    using Validate;

    public abstract class CheckConcurrency<TEntity, TRes, TId>
           : CheckConcurrency<TEntity, TRes, UpdateResource<TRes, TId?>, TId>
           where TEntity : class, IIdentifiable<TId>, IVersioned
           where TRes    : Resource<TId?>
           where TId     : struct
    {
    }

    public abstract class CheckConcurrency<TEntity, TRes, TAction, TId>
        : AbstractValidator<TAction>
        where TEntity : class, IIdentifiable<TId>, IVersioned
        where TAction : UpdateResource<TRes, TId?>
        where TRes    : Resource<TId?>
        where TId     : struct
    {
        protected CheckConcurrency()
        {
            RuleFor(c => c).CustomAsync(BeExpectedVersion);
        }

        protected abstract Task<TEntity> GetEntity(
            TAction action, TRes resource, IHandler composer);

        private async Task BeExpectedVersion(TAction action, CustomContext context,
            CancellationToken cancellation)
        {
            var resource = action.Resource;
            if (resource == null) return;

            var composer = context.ParentContext?.GetComposer();
            if (composer == null)
                throw new InvalidOperationException("Composer is required");

            var entity = await composer.Proxy<IStash>()
                .GetOrPut(async () => await GetEntity(action, resource, composer));

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

            if (resource.RowVersion?.SequenceEqual(entity.Version) == false)
                throw new OptimisticConcurrencyException(
                    $"Concurrency exception detected for {entity.GetType()} with id {entity.Id}.");
        }
    }
}
