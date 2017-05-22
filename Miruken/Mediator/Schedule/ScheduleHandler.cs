namespace Miruken.Mediator.Schedule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;

    public class ScheduleHandler : Handler
    {
        [Mediates]
        public async Task<ScheduleResult> Concurrent(Concurrent concurrent, IHandler composer)
        {
            var requests  = concurrent.Requests;
            var responses = requests?.Length > 0
                ? await Task.WhenAll(requests.Select(req => Process(req, composer)))
                : Array.Empty<object>();
            return new ScheduleResult
            {
                Responses = responses
            };
        }

        [Mediates]
        public async Task<ScheduleResult> Sequential(Sequential sequential, IHandler composer)
        {
            var responses = new List<object>();
            if (sequential.Requests?.Length > 0)
            {
                foreach (var req in sequential.Requests)
                    responses.Add(await Process(req, composer));
            }
            return new ScheduleResult
            {
                Responses = responses.ToArray()
            };
        }

        [Mediates]
        public ScheduleResult Parallel(Parallel parallel, IHandler composer)
        {
            var requests  = parallel.Requests;
            var responses = requests?.Length > 0
                ? requests.AsParallel().Select(
                    req => Process(req, composer).Result).ToArray()
                : Array.Empty<object>();
            return new ScheduleResult
            {
                Responses = responses
            };
        }

        private static Task<object> Process(object request, IHandler composer)
        {
            return composer.Send(request);
        }
    }
}
