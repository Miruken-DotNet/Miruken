namespace Miruken.Api.Schedule
{
    using Api;
    using Callback;
    using Concurrency;

    public static class ScheduleExtensions
    {
        public static Promise<ScheduledResult> Sequential(
            this IHandler handler, params object[] requests)
        {
            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Sequential
                 {
                     Requests = requests
                 });
        }

        public static Promise<ScheduledResult> Concurrent(
            this IHandler handler, params object[] requests)
        {
            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Concurrent
                 {
                     Requests = requests
                 });
        }

        public static Promise<ScheduledResult> Parallel(
            this IHandler handler, params object[] requests)
        {
            return handler == null || requests.Length == 0
                 ? Promise<ScheduledResult>.Empty
                 : handler.Send(new Parallel
                 {
                     Requests = requests
                 });
        }
    }
}
