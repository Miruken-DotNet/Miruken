namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Concurrency;
    using Infrastructure;
    using Policy.Bindings;

    public partial class Handler
    {
        object IServiceProvider.GetService(Type service)
        {
            return this.Resolve(service);
        }
    }

    public static class HandlerResolveExtensions
    {
        public static IHandler Infer(this IHandler handler)
        {
            return handler == null ? null : new InferDecorator(handler);
        }

        public static IHandler InferAll(this IHandler handler)
        {
            return handler == null ? null
                 : new CallbackSemanticsDecorator(
                       new InferDecorator(handler),
                       CallbackOptions.Broadcast);
        }

        public static object Resolve(this IHandler handler, object key,
            Action<ConstraintBuilder> constraints = null)
        {
            if (handler == null) return null;
            if (key is Inquiry inquiry)
            {
                if (inquiry.WantsAsync)
                    throw new InvalidOperationException(
                        "Requested Inquiry is asynchronous");
            }
            else
            {
                inquiry = new Inquiry(key);
            }
            ConstraintBuilder.BuildConstraints(inquiry, constraints);
            if (handler.Handle(inquiry))
                return inquiry.Result;
            return key is Type type
                 ? RuntimeHelper.GetDefault(type)
                 : null;
        }

        public static Promise ResolveAsync(this IHandler handler, object key,
            Action<ConstraintBuilder> constraints = null)
        {
            if (handler == null) return null;
            if (key is Inquiry inquiry)
            {
                if (!inquiry.WantsAsync)
                    throw new InvalidOperationException(
                        "Requested Inquiry is synchronous");
            }
            else
            {
                inquiry = new Inquiry(key) { WantsAsync = true };
            }
            ConstraintBuilder.BuildConstraints(inquiry, constraints);
            if (handler.Handle(inquiry))
                return (Promise)inquiry.Result;
            return key is Type type
                 ? Promise.Resolved(RuntimeHelper.GetDefault(type))
                 : Promise.Empty;
        }

        public static T Resolve<T>(this IHandler handler,
            Action<ConstraintBuilder> constraints = null)
        {
            return handler == null ? default
                 : (T)Resolve(handler, typeof(T), constraints);
        }

        public static Promise<T> ResolveAsync<T>(this IHandler handler,
            Action<ConstraintBuilder> constraints = null)
        {
            return handler == null ? Promise<T>.Empty
                 : (Promise<T>)ResolveAsync(handler, typeof(T), constraints)
                 .Coerce(typeof(Promise<T>));
        }

        public static object[] ResolveAll(this IHandler handler, object key,
            Action<ConstraintBuilder> constraints = null)
        {
            if (handler != null)
            {
                if (key is Inquiry inquiry)
                {
                    if (!inquiry.Many)
                        throw new InvalidOperationException(
                            "Requested Inquiry expects a single result");

                    if (inquiry.WantsAsync)
                        throw new InvalidOperationException(
                            "Requested Inquiry is asynchronous");
                }
                else
                {
                    inquiry = new Inquiry(key, true);
                }
                ConstraintBuilder.BuildConstraints(inquiry, constraints);
                if (handler.Handle(inquiry, true))
                {
                    var result = inquiry.Result;
                    return CoerceArray((object[]) result, key);
                }
            }
            return CoerceArray(Array.Empty<object>(), key);
        }

        public static Promise<object[]> ResolveAllAsync(
            this IHandler handler, object key,
            Action<ConstraintBuilder> constraints = null)
        {
            if (handler != null)
            {
                if (key is Inquiry inquiry)
                {
                    if (!inquiry.Many)
                        throw new InvalidOperationException(
                            "Requested Inquiry expects a single result");

                    if (!inquiry.WantsAsync)
                        throw new InvalidOperationException(
                            "Requested Inquiry is synchronous");
                }
                else
                {
                    inquiry = new Inquiry(key, true)
                    {
                        WantsAsync = true
                    };
                }
                ConstraintBuilder.BuildConstraints(inquiry, constraints);
                if (handler.Handle(inquiry, true))
                {
                    var result = inquiry.Result;
                    return ((Promise<object[]>) result)
                        .Then((arr, s) => CoerceArray(arr, key));
                }
            }
            var empty = CoerceArray(Array.Empty<object>(), key);
            return Promise.Resolved(empty);
        }

        public static T[] ResolveAll<T>(this IHandler handler,
            Action<ConstraintBuilder> constraints = null)
        {
            if (handler == null)
                return Array.Empty<T>();
            var results = ResolveAll(handler, typeof(T), constraints);
            return results?.Cast<T>().ToArray() ?? Array.Empty<T>();
        }

        public static Promise<T[]> ResolveAllAsync<T>(this IHandler handler,
            Action<ConstraintBuilder> constraints = null)
        {
            return handler == null ? Promise.Resolved(Array.Empty<T>())
                 : ResolveAllAsync(handler, typeof(T), constraints)
                      .Then((r, s) => r?.Cast<T>().ToArray() 
                                   ?? Array.Empty<T>());
        }

        private static object[] CoerceArray(object[] array, object key)
        {
            var type = key as Type ?? (key as Inquiry)?.Key as Type;
            if (type == null) return array;
            var typed = Array.CreateInstance(type, array.Length);
            array.CopyTo(typed, 0);
            return (object[])typed;
        }
    }
}
