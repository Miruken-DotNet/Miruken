﻿namespace Miruken.Validate.FluentValidation
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Callback;
    using global::FluentValidation;
    using global::FluentValidation.Results;
    using global::FluentValidation.Validators;
    using Infrastructure;

    public class FluentValidationValidator : Handler
    {
        [Provides, Singleton]
        public FluentValidationValidator()
        {        
        }

        [Validates(Scope = Scopes.Any)]
        public async Task Validate<T>(T target, Validation validation, IHandler composer)
        {
            var validators = composer.ResolveAll<IValidator<T>>();
            if (validators.Length == 0) return;

            var outcome = validation.Outcome;
            var scope   = validation.ScopeMatcher;
            var context = scope != null && scope != EqualsScopeMatcher.Default
                ? new ValidationContext<T>(target, null, new ScopeSelector(scope))
                : new ValidationContext<T>(target);
            context.SetValidation(validation);
            context.SetComposer(composer);

            Array.Sort(validators, OrderedComparer<IValidator<T>>.Instance);

            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(context)
                    .ConfigureAwait(false);
                if (!result.IsValid)
                {
                    AddErrors(result, outcome);
                    if (validation.StopOnFailure)
                        break;
                }
            }
        }

        private static void AddErrors(ValidationResult result, ValidationOutcome outcome)
        {
            foreach (var error in result.Errors)
            {
                var child   = error as OutcomeFailure;
                var failure = child?.FailedOutcome ?? (object)error.ErrorMessage;
                outcome.AddError(error.PropertyName, failure);
            }
        }
    }

    public static class FluentValidatorExtensions
    {
        private const string ComposerKey = "Miruken.Composer";
        private const string ValidationKey = "Miruken.Validation";

        public static void SetValidation(
            this IValidationContext context, Validation validation)
        {
            context.RootContextData[ValidationKey] = validation;
        }

        public static Validation GetValidation(this IValidationContext context)
        {
            return context.RootContextData.TryGetValue(ValidationKey, out var validation)
                ? (Validation)validation
                : null;
        }

        public static void SetComposer(this IValidationContext context, IHandler composer)
        {
            context.RootContextData[ComposerKey] = composer;
        }

        public static IHandler GetComposer(this IValidationContext context)
        {
            return context.RootContextData.TryGetValue(ComposerKey, out var composer)
                ? (IHandler)composer
                : null;
        }

        public static IRuleBuilderOptions<T, TProp> Valid<T, TProp>(
            this IRuleBuilder<T, TProp> builder)
        {
            return builder.SetValidator(FluentValidator<TProp>.Instance);
        }

        public static IRuleBuilderOptions<T, TProperty> WithComposer<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, TProperty, IHandler, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return ruleBuilder.Must((target, prop, ctx) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer == null || predicate(target, prop, composer);
            });
        }

        public static IRuleBuilderOptions<T, TProperty> WithComposerAsync<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, TProperty, IHandler, CancellationToken, Task<bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return ruleBuilder.MustAsync((target, prop, ctx, token) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer == null
                    ? Task.FromResult(true)
                    : predicate(target, prop, composer, token);
            });
        }

        public static IRuleBuilderOptions<T, TProperty> WithoutComposer<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, TProperty, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return ruleBuilder.Must((target, prop, ctx) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer != null || predicate(target, prop);
            });
        }

        public static IRuleBuilderOptions<T, TProperty> WithoutComposerAsync<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, TProperty, CancellationToken, Task<bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return ruleBuilder.MustAsync((target, prop, ctx, token) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer != null
                     ? Task.FromResult(true)
                     : predicate(target, prop, token);
            });
        }

        public static IRuleBuilderInitial<T, TProperty>
            WithComposerCustom<T, TProperty>(
            this IRuleBuilder<T, TProperty> ruleBuilder,
            Action<TProperty, CustomContext, IHandler> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return ruleBuilder.Custom((prop, ctx) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                if (composer != null)
                    action(prop, ctx, composer);
            });
        }

        public static IRuleBuilderInitial<T, TProperty>
            WithComposerCustomAsync<T, TProperty>(
                this IRuleBuilder<T, TProperty> ruleBuilder,
                Func<TProperty, CustomContext, CancellationToken, IHandler, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return ruleBuilder.CustomAsync((prop, ctx, cancel) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer != null
                     ? action(prop, ctx, cancel, composer)
                     : Task.CompletedTask;
            });
        }

        public static IRuleBuilderInitial<T, TProperty>
            WithoutComposerCustom<T, TProperty>(
                this IRuleBuilder<T, TProperty> ruleBuilder,
                Action<TProperty, CustomContext> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return ruleBuilder.Custom((prop, ctx) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                if (composer == null) action(prop, ctx);
            });
        }

        public static IRuleBuilderInitial<T, TProperty>
            WithoutComposerCustomAsync<T, TProperty>(
                this IRuleBuilder<T, TProperty> ruleBuilder,
                Func<TProperty, CustomContext, CancellationToken, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return ruleBuilder.CustomAsync((prop, ctx, cancel) =>
            {
                var composer = ctx.ParentContext?.GetComposer();
                return composer == null
                     ? action(prop, ctx, cancel)
                     : Task.CompletedTask;
            });
        }
    }
}
