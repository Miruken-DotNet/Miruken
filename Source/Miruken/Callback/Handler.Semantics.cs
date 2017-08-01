﻿using System;

namespace Miruken.Callback
{
    [Flags]
    public enum CallbackOptions
    {
        None       = 0,
        Duck       = 1 << 0,
        Strict     = 1 << 1,
        Resolve    = 1 << 2,
        Broadcast  = 1 << 3,
        BestEffort = 1 << 4,
        Notify     = Broadcast | BestEffort,
        ResolveAll = Resolve | Broadcast
    }

    public class CallbackSemantics : Composition
    {
        private CallbackOptions _options;
        private CallbackOptions _specified;

        public CallbackSemantics()
            : this(CallbackOptions.None)
        {    
        }

        public CallbackSemantics(CallbackOptions options)
        {
            _options = _specified = options;
        }

        public bool HasOption(CallbackOptions options)
        {
            return (_options & options) == options;
        }

        public void SetOption(CallbackOptions options, bool enabled)
        {
            _options = enabled
                     ? _options | options
                     : _options & ~options;
            _specified = _specified | options;
        }

        public bool IsSpecified(CallbackOptions options)
        {
            return (_specified & options) == options;
        }

        public void MergeInto(CallbackSemantics semantics)
        {
            MergeInto(semantics, CallbackOptions.Duck);
            MergeInto(semantics, CallbackOptions.Strict);
            MergeInto(semantics, CallbackOptions.Resolve);
            MergeInto(semantics, CallbackOptions.BestEffort);
            MergeInto(semantics, CallbackOptions.Broadcast);
        }

        private void MergeInto(CallbackSemantics semantics, CallbackOptions option)
        {
            if (IsSpecified(option) && !semantics.IsSpecified(option))
                semantics.SetOption(option, HasOption(option));
        }
    }

    public class CallbackSemanticsHandler : Handler, IDecorator
    {
        private readonly IHandler _handler;
        private readonly CallbackSemantics _semantics;

        public CallbackSemanticsHandler(
            IHandler handler, CallbackOptions options)
        {
            _handler   = handler;
            _semantics = new CallbackSemantics(options);
        }

        object IDecorator.Decoratee => _handler;

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            if (Composition.IsComposed<CallbackSemantics>(callback))
                return false;

            var semantics = callback as CallbackSemantics;
            if (semantics != null)
            {
                _semantics.MergeInto(semantics);
                if (greedy)
                    _handler.Handle(callback, ref greedy, composer);
                return true;
            }

            if (callback is Composition)
                return _handler.Handle(callback, ref greedy, composer);

            if (_semantics.HasOption(CallbackOptions.Broadcast))
                greedy = true;

            if (_semantics.HasOption(CallbackOptions.BestEffort))
            {
                try
                {
                    _handler.Handle(callback, ref greedy, composer);
                    return true;
                }
                catch (RejectedException)
                {
                    return true;
                }
            }

            if (_semantics.HasOption(CallbackOptions.Resolve))
                callback = GetResolvingCallback(callback, greedy);

            return _handler.Handle(callback, ref greedy, composer);
        }

        private static object GetResolvingCallback(object callback, bool greedy)
        {
            var resolving = callback as IResolveCallback;
            if (resolving != null)
                return resolving.GetCallback(greedy) ?? callback;
            var dispatch = callback as IDispatchCallback;
            var policy   = dispatch?.Policy ?? HandlesAttribute.Policy;
            var handlers = policy.GetHandlers(callback);
            var bundle   = new Bundle(greedy);
            foreach (var handler in handlers)
                bundle.Add(h => h.Handle(new Resolve(handler, greedy, callback)));
            return bundle;
        }
    }

    public static class CallbackSemanticExtensions
    {
        public static CallbackSemantics GetSemantics(this IHandler handler)
        {
            var semantics = new CallbackSemantics();
            return handler.Handle(semantics, true) ? semantics : null;           
        }

        public static IHandler Semantics(
            this IHandler handler, CallbackOptions options)
        {
            return handler == null ? null 
                 : new CallbackSemanticsHandler(handler, options);
        }

        #region Semantics

        public static IHandler Duck(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Duck);
        }

        public static IHandler Strict(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Strict);
        }

        public static IHandler Resolve(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Resolve);
        }

        public static IHandler ResolveAll(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.ResolveAll);
        }

        public static IHandler Broadcast(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Broadcast);
        }

        public static IHandler BestEffort(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.BestEffort);
        }

        public static IHandler Notify(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Notify);
        }

        #endregion
    }
}
