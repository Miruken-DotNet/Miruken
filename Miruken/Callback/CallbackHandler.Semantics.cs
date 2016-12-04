using System;

namespace Miruken.Callback
{
    [Flags]
    public enum CallbackOptions
    {
        None      = 0,
        Broadcast = 1 << 0,
        BestEffot = 1 << 1,
        Resolve   = 1 << 2,
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
                     ? (_options | options)
                     : (_options & (~options));
            _specified = _specified | options;
        }

        public bool IsSpecified(CallbackOptions options)
        {
            return (_specified & options) == options;
        }

        public void MergeInto(CallbackSemantics semantics)
        {
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

    public class CallbackSemanticsHandler : CallbackHandlerDecorator
    {
        private readonly CallbackSemantics _semantics;

        public CallbackSemanticsHandler(
            ICallbackHandler handler, CallbackOptions options)
            : base(handler)
        {
            _semantics = new CallbackSemantics(options);
        }

        protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
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
        public static ICallbackHandler Semantics(
            this ICallbackHandler handler, CallbackOptions options)
        {
            return handler == null ? null : 
                new CallbackSemanticsHandler(handler, options);
        }

        #region Semantics

        public static ICallbackHandler Broadcast(this ICallbackHandler handler)
        {
            return Semantics(handler, CallbackOptions.Broadcast);
        }

        public static ICallbackHandler BestEffort(this ICallbackHandler handler)
        {
            return Semantics(handler, CallbackOptions.BestEffot);
        }

        public static ICallbackHandler Notify(this ICallbackHandler handler)
        {
            return Semantics(handler, CallbackOptions.Notify);
        }

        public static ICallbackHandler Resolve(this ICallbackHandler handler)
        {
            return Semantics(handler, CallbackOptions.Resolve);
        }

        #endregion
    }
}
