﻿namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Concurrency;
    using Infrastructure;

    public static class HandlerCommandExtensions
    {
        public static void Command(this IHandler handler, object callback)
        {
            if (handler == null) return;
            var command = new Command(callback);
            if (!handler.Handle(command))
                throw new NotSupportedException($"{callback.GetType()} not handled");
            if (command.IsAsync)
                ((Promise)command.Result).Wait();
        }

        public static Promise CommandAsync(this IHandler handler, object callback)
        {
            if (handler == null) return Promise.Empty;
            var command = new Command(callback) { WantsAsync = true };
            try
            {
                return handler.Handle(command)
                    ? (Promise)command.Result
                    : Promise.Rejected(new NotSupportedException(
                        $"{callback.GetType()} not handled"));
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }

        public static TRes Command<TRes>(this IHandler handler, object callback)
        {
            if (handler == null) return default(TRes);
            var command = new Command(callback);
            if (!handler.Handle(command))
                throw new NotSupportedException($"{callback.GetType()} not handled");
            var result = command.Result;
            return command.IsAsync 
                 ? (TRes)((Promise)result).Wait()
                 : (result == null 
                    ? (TRes)RuntimeHelper.GetDefault(typeof(TRes)) 
                    : (TRes)result);
        }

        public static Promise<TRes> CommandAsync<TRes>(
            this IHandler handler, object callback)
        {
            if (handler == null)
                return Promise<TRes>.Empty;
            var command = new Command(callback) { WantsAsync = true };
            try
            {
                if (!handler.Handle(command))
                    throw new NotSupportedException($"{callback.GetType()} not handled");
                var promise = (Promise)command.Result;
                return (Promise<TRes>)promise.Coerce(typeof(Promise<TRes>));
            }
            catch (Exception ex)
            {
                return Promise<TRes>.Rejected(ex);
            }
        }

        public static void CommandAll(this IHandler handler, object callback)
        {
            if (handler == null) return;
            if (!handler.Handle(new Command(callback, true), true))
                throw new NotSupportedException($"{callback.GetType()} not handled");
        }

        public static Promise CommandAllAsync(this IHandler handler, object callback)
        {
            if (handler == null) return Promise.Empty;
            var command = new Command(callback, true) { WantsAsync = true };
            try
            {
                if (!handler.Handle(command, true))
                    return Promise.Rejected(new NotSupportedException(
                        $"{callback.GetType()} not handled"));
                return (Promise)command.Result;
            }
            catch (Exception ex)
            {
                return Promise.Rejected(ex);
            }
        }

        public static TRes[] CommandAll<TRes>(
             this IHandler handler, object callback)
        {
            if (handler == null)
                return Array.Empty<TRes>();
            var command = new Command(callback, true) { WantsAsync = true };
            if (!handler.Handle(command, true))
                throw new NotSupportedException(
                    $"{callback.GetType()} not handled");
            var result = command.Result;
            return command.IsAsync
                 ? ((object[])((Promise)result).Wait()).Cast<TRes>().ToArray()
                 : ((object[])result).Cast<TRes>().ToArray();
        }

        public static Promise<TRes[]> CommandAllAsync<TRes>(
            this IHandler handler, object callback)
        {
            if (handler == null)
                return Promise<TRes[]>.Empty;
            var command = new Command(callback, true) { WantsAsync = true };
            try
            {
                if (!handler.Handle(command, true))
                    return Promise<TRes[]>.Rejected(new NotSupportedException(
                        $"{callback.GetType()} not handled"));
                var promise = (Promise)command.Result;
                return promise.Then((results, s) => ((object[])results)
                    .Cast<TRes>().ToArray());
            }
            catch (Exception ex)
            {
                return Promise<TRes[]>.Rejected(ex);
            }
        }
    }
}
