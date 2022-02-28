namespace Miruken.Api.Route;

using System;
using System.Collections.Generic;
using System.Linq;
using Callback;
using Concurrency;
using Schedule;

[Unmanaged]
public class BatchRouter : Handler, IBatching
{
    private readonly Dictionary<string, List<Pending>> _groups;

    public BatchRouter()
    {
        _groups = new Dictionary<string, List<Pending>>();
    }

    [Handles]
    public Promise<object> Route(Batched<Routed> batched, Command command) =>
        Route(batched.Callback, batched.RawCallback as Command ?? command);

    [Handles]
    public Promise<object> Route(Routed routed, Command command)
    {
        var route = routed.Route;
        if (!_groups.TryGetValue(route, out var group))
        {
            group = new List<Pending>();
            _groups.Add(route, group);
        }
        var message = command.Many
            ? new Publish(routed.Message)
            : routed.Message;
        var request = new Pending(message);
        group.Add(request);
        return request.Promise;
    }

    public object Complete(IHandler composer)
    {
        var complete = Promise.All(_groups.Select(group =>
        {
            var uri      = group.Key;
            var requests = group.Value;
            var messages = requests.Select(r => r.Message).ToArray();
            return composer.Send(new Concurrent {Requests = messages}
                .RouteTo(uri)).Then((result, s) =>
            {
                var responses = result.Responses;
                for (var i = responses.Length; i < requests.Count; ++i)
                    requests[i].Promise.Cancel();
                return Tuple.Create(uri, responses
                    .Select((response, i) => response.Match(
                        failure =>
                        {
                            requests[i].Reject(failure, s);
                            return failure;
                        },
                        success =>
                        {
                            requests[i].Resolve(success, s);
                            return success;
                        })).ToArray());
            }).Catch((ex, _) =>
            {
                requests.ForEach(r => r.Promise.Cancel());
                return Promise.Rejected(ex);
            });
        }).Cast<object>().ToArray());
        _groups.Clear();
        return complete;
    }

    private class Pending
    {
        public object                           Message { get; }
        public Promise<object>                  Promise { get; }
        public Promise<object>.ResolveCallbackT Resolve { get; private set; }
        public RejectCallback                   Reject  { get; private set; }

        public Pending(object message)
        {
            Message = message;
            Promise = new Promise<object>(ChildCancelMode.Any, (resolve, reject) =>
            {
                Resolve = resolve;
                Reject  = reject;
            });
        }
    }
}