namespace Miruken.Api.Schedule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;
    using Functional;

    public class Scheduler : Handler
    {
        [Provides, Singleton]
        public Scheduler()
        {           
        }

        [Handles]
        public async Task<ScheduledResult> Concurrent(
            Concurrent concurrent, IHandler composer)
        {
            var requests  = concurrent.Requests;
            var responses = requests != null && requests.Length > 0
                ? await Task.WhenAll(requests.Select(req => Process(req, composer)))
                : Array.Empty<Try<Exception, object>>();
            return new ScheduledResult
            {
                Responses = responses
            };
        }

        [Handles]
        public async Task<ScheduledResult> Sequential(
            Sequential sequential, IHandler composer)
        {
            var requests  = sequential.Requests;
            var responses = new List<Try<Exception, object>>();
            if (requests != null && requests.Length > 0)
            {
                foreach (var req in sequential.Requests)
                {
                    var response = await Process(req, composer);
                    responses.Add(response);
                    if (response.IsError) break;
                }
            }
            return new ScheduledResult
            {
                Responses = responses.ToArray()
            };
        }

        [Handles]
        public async Task<ScheduledResult> Parallel(Parallel parallel, IHandler composer)
        {
            var requests  = parallel.Requests;
            var responses = requests != null && requests.Length > 0
                ? await Task.WhenAll(requests.AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(req => Process(req, composer)).ToArray())
                : Array.Empty<Try<Exception, object>>();
            return new ScheduledResult
            {
                Responses = responses
            };
        }

        [Handles]
        public Task Publish(Publish publish, IHandler composer)
        {
            return composer.Publish(publish.Message);
        }

        private static Task<Try<Exception, object>> 
            Process(object request, IHandler composer)
        {
            return (request is Publish publish
                 ? composer.Publish(publish.Message)
                 : composer.Send(request))
                 .Then((res, _) => new Try<Exception, object>(res))
                 .Catch((ex, _) => new Try<Exception, object>(ex));
        }
    }
}
