﻿using System;

namespace Miruken.Callback
{
    [Flags]
    public enum CallbackOptions
    {
        None      = 0,
        Broadcast = 1 << 0,
        BestEffot = 1 << 1,
        Duck    = 1 << 2,
        Resolve   = 1 << 3,
        Notify    = Broadcast | BestEffot
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
            MergeInto(semantics, CallbackOptions.BestEffot);
            MergeInto(semantics, CallbackOptions.Broadcast);
            MergeInto(semantics, CallbackOptions.Resolve);
        }

        private void MergeInto(CallbackSemantics semantics, CallbackOptions option)
        {
            if (IsSpecified(option) && !semantics.IsSpecified(option))
                semantics.SetOption(option, HasOption(option));
        }
    }

    public class SemanticsHandler : HandlerDecorator
    {
        private readonly CallbackSemantics _semantics;

        public SemanticsHandler(
            IHandler handler, CallbackOptions options)
            : base(handler)
        {
            _semantics = new CallbackSemantics(options);
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var handled   = false;
            if (Composition.IsComposed<CallbackSemantics>(callback))
                return false;

            var semantics = callback as CallbackSemantics;
            if (semantics != null)
            {
                _semantics.MergeInto(semantics);
                handled = true;
            }
            else if (!greedy)
            {
                if (_semantics.IsSpecified(
                    CallbackOptions.Broadcast | CallbackOptions.Resolve))
                    greedy = _semantics.HasOption(CallbackOptions.Broadcast) &&
                            !_semantics.HasOption(CallbackOptions.Resolve);
                else
                {
                    var cs = new CallbackSemantics();
                    if (Handle(cs, true) && cs.IsSpecified(CallbackOptions.Broadcast))
                        greedy = cs.HasOption(CallbackOptions.Broadcast) &&
                            !cs.HasOption(CallbackOptions.Resolve);
                }
            }

            if (greedy || !handled)
                handled = base.HandleCallback(callback, greedy, composer) || handled;

            return handled;
        }
    }

    public static class CallbackSemanticExtensions
    {
        public static IHandler Semantics(
            this IHandler handler, CallbackOptions options)
        {
            return handler == null ? null : 
                new SemanticsHandler(handler, options);
        }

        #region Semantics

        public static IHandler Broadcast(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Broadcast);
        }

        public static IHandler BestEffort(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.BestEffot);
        }

        public static IHandler Duck(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Duck);
        }

        public static IHandler Notify(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Notify);
        }

        public static IHandler Resolve(this IHandler handler)
        {
            return Semantics(handler, CallbackOptions.Resolve);
        }

        #endregion
    }
}
