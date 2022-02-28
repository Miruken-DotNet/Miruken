namespace Miruken.Callback;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Concurrency;
using Policy;
using Policy.Bindings;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class Inquiry : ICallback, IAsyncCallback,
    IDispatchCallback, IDispatchCallbackGuard, IBindingScope
{
    private readonly SortedList<int, object> _resolutions;
    private readonly List<object> _promises;
    private object _result;

    public Inquiry(object key, bool many = false)
    {
        Key          = key ?? throw new ArgumentNullException(nameof(key));
        Many         = many;
        Metadata     = new BindingMetadata();
        _resolutions = new SortedList<int, object>(Comparer.Instance);
        _promises    = new List<object>();
    }

    public Inquiry(object key, Inquiry parent, bool many = false)
        : this(key, many)
    {
        Parent = parent;
    }

    public object  Key        { get; }
    public bool    Many       { get; }
    public Inquiry Parent     { get; }
    public bool    WantsAsync { get; set; }
    public bool    IsAsync    { get; private set; }

    public object              Target     { get; private set; }
    public MemberDispatch      Dispatcher { get; private set; }
    public PolicyMemberBinding Binding    { get; private set; }
    public BindingMetadata     Metadata   { get; }

    public CallbackPolicy Policy => Provides.Policy;

    public ICollection<object> Resolutions => _resolutions.Values;

    public Type ResultType =>
        WantsAsync || IsAsync ? typeof(Promise) : null;

    public object Result
    {
        get
        {
            if (_result == null)
            {
                if (IsAsync)
                {
                    _result = Many
                        ? Promise.All(_promises)
                            .Then((_, _) => _resolutions.Values.ToArray())
                        : Promise.All(_promises)
                            .Then((_, _) => _resolutions.Values.FirstOrDefault());
                }
                else
                {
                    _result = Many ? _resolutions.Values.ToArray()
                        : _resolutions.Values.FirstOrDefault();
                }
            }

            if (IsAsync)
            {
                if (!WantsAsync)
                    _result = (_result as Promise)?.Wait();
            }
            else if (WantsAsync)
            {
                if (Many)
                    _result = Promise.Resolved(_result as object[]);
                else
                    _result = Promise.Resolved(_result);
            }

            return _result;
        }
        set
        {
            _result = value;
            IsAsync = _result is Promise or Task;
        }
    }

    public bool Resolve(object resolution, IHandler composer, int? priority = null)
    {
        return Resolve(resolution, false, false, composer, priority);
    }

    public bool Resolve(object resolution, bool strict,
        bool greedy, IHandler composer, int? priority = null)
    {
        if (resolution == null) return false;
        var resolved = strict switch
        {
            false when resolution is object[] array => array.Aggregate(false,
                (s, res) => Include(res, priority, false, greedy, composer) || s),
            false when resolution is ICollection collection => collection.Cast<object>()
                .Aggregate(false, (s, res) => Include(res, priority, false, greedy, composer) || s),
            _ => Include(resolution, priority, strict, greedy, composer)
        };
        if (resolved) _result = null;
        return resolved;
    }

    private bool Include(object resolution, int? priority, bool strict, 
        bool greedy, IHandler composer)
    {
        if (resolution == null) return false;

        var key = priority ?? int.MaxValue;

        var promise = resolution as Promise
                      ?? (resolution as Task)?.ToPromise();

        if (promise?.State == PromiseState.Fulfilled)
        {
            resolution = promise.Wait();
            if (resolution == null) return false;
            promise = null;
        }

        if (promise != null)
        {
            IsAsync = true;
            _promises.Add(promise.Then((result, _) =>
            {
                switch (result)
                {
                    case object[] array:
                        foreach (var r in array.Where(res =>
                                     res != null && IsSatisfied(res, greedy, composer)))
                        {
                            _resolutions.Add(key, r);
                        }
                        break;
                    case ICollection collection:
                        foreach (var r in collection.Cast<object>().Where(res =>
                                     res != null && IsSatisfied(res, greedy, composer)))
                        {
                            _resolutions.Add(key, r);
                        }
                        break;
                    default:
                        if (result != null && IsSatisfied(result, greedy, composer))
                            _resolutions.Add(key, result);
                        break;
                }
            }).Catch((_, _) => (object)null));
        }
        else if (!IsSatisfied(resolution, greedy, composer))
            return false;
        else if (strict)
        {
            _resolutions.Add(key, resolution);
        }
        else switch (resolution)
        {
            case object[] array:
                foreach (var r in array.Where(res =>
                             res != null && IsSatisfied(res, greedy, composer)))
                {
                    _resolutions.Add(key, r);
                }
                break;
            case ICollection collection:
                foreach (var r in collection.Cast<object>().Where(res =>
                             res != null && IsSatisfied(res, greedy, composer)))
                {
                    _resolutions.Add(key, r);
                }
                break;
            default:
                _resolutions.Add(key, resolution);
                break;
        }
        return true;
    }

    protected virtual bool IsSatisfied(object resolution, bool greedy, IHandler composer)
    {
        return true;
    }

    public virtual bool CanDispatch(object target,
        PolicyMemberBinding binding, MemberDispatch dispatcher,
        out IDisposable reset)
    {
        if (InProgress(target, binding, dispatcher))
        {
            reset = null;
            return false;
        }
        reset = new Guard(this, target, binding, dispatcher);
        return true;
    }

    public virtual bool Dispatch(object handler, ref bool greedy, IHandler composer)
    {
        var isGreedy = greedy;
        var handled  = Implied(handler, isGreedy, composer);
        if (handled && !greedy) return true;

        var count = _resolutions.Count + _promises.Count;
        handled = Policy.Dispatch(handler, this, greedy, composer,
            (r, strict, p) => Resolve(r, strict, isGreedy, composer, p)) || handled;
        return handled || _resolutions.Count + _promises.Count > count;
    }

    private bool Implied(object item, bool greedy, IHandler composer)
    {
        if (item == null || Key is not Type type || !Metadata.IsEmpty)
            return false;
        var compatible =  type.IsInstanceOfType(item);
        return compatible && Resolve(item, false, greedy, composer);
    }

    private bool InProgress(object target,
        PolicyMemberBinding binding, MemberDispatch dispatcher)
    {
        return ReferenceEquals(target, Target) &&
               ReferenceEquals(binding, Binding) &&
               ReferenceEquals(dispatcher, Dispatcher) ||
               Parent?.InProgress(target, binding, dispatcher) == true;
    }

    private string DebuggerDisplay
    {
        get
        {
            var many = Many ? "many " : "";
            return $"Inquiry {many}| {Key}";
        }
    }

    private class Comparer : IComparer<int>
    {
        public static readonly Comparer Instance = new();

        public int Compare(int x, int y)
        {
            var result = x.CompareTo(y);
            return result == 0 ? -1 : result;
        }
    }

    private class Guard : IDisposable
    {
        private readonly Inquiry _inquiry;
        private readonly object _target;
        private readonly MemberDispatch _dispatcher;
        private readonly PolicyMemberBinding _binding;

        public Guard(Inquiry inquiry, object target,
            PolicyMemberBinding binding, MemberDispatch dispatcher)
        {
            _inquiry    = inquiry;
            _target     = inquiry.Target;
            _dispatcher = inquiry.Dispatcher;
            _binding    = inquiry.Binding;

            inquiry.Target     = target;
            inquiry.Dispatcher = dispatcher;
            inquiry.Binding    = binding;
        }

        public void Dispose()
        {
            _inquiry.Target     = _target;
            _inquiry.Dispatcher = _dispatcher;
            _inquiry.Binding    = _binding;
        }
    }
}