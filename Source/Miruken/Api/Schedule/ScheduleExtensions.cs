namespace Miruken.Api.Schedule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Api;
    using Callback;
    using Concurrency;

    public static class ScheduleExtensions
    {
        public static Promise<ScheduledResult> Sequential(
            this IHandler handler, params object[] requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Sequential
                 {
                     Requests = requests
                 });
        }

        public static Promise<ScheduledResult> Sequential(
            this IHandler handler, IEnumerable<object> requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            var requestsArray = requests.ToArray();
            return handler == null || requestsArray.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Sequential
                 {
                     Requests = requestsArray
                 });
        }

        public static Promise<ScheduledResult> Concurrent(
            this IHandler handler, params object[] requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Concurrent
                 {
                     Requests = requests
                 });
        }

        public static Promise<ScheduledResult> Concurrent(
            this IHandler handler, IEnumerable<object> requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            var requestsArray = requests.ToArray();
            return handler == null || requestsArray.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Concurrent
                 {
                     Requests = requestsArray
                 });
        }

        public static Promise<ScheduledResult> Parallel(
            this IHandler handler, params object[] requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Parallel
                 {
                     Requests = requests
                 });
        }

        public static Promise<ScheduledResult> Parallel(
            this IHandler handler, IEnumerable<object> requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            var requestsArray = requests.ToArray();
            return handler == null || requestsArray.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Parallel
                 {
                     Requests = requestsArray
                 });
        }
    }
}
