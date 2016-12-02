using System;

namespace Miruken.Callback
{
    public delegate bool CallbackFilter(
        object callback, ICallbackHandler filter, Func<bool> proceed
    );

    public class CallbackHandlerFilter : CallbackHandlerDecorator
    {
        private readonly CallbackFilter _filter;
        private readonly bool _reentrant;

        public CallbackHandlerFilter(
            ICallbackHandler handler, CallbackFilter filter
            ) : this(handler, filter, false)
        {           
        }

        public CallbackHandlerFilter(
            ICallbackHandler handler, CallbackFilter filter, 
            bool reentrant) : base(handler)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            _filter    = filter;
            _reentrant = reentrant;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, ICallbackHandler composer)
        {
            if (!_reentrant && (callback is Composition)) {                                                                                              
                return base.HandleCallback(callback, greedy, composer);                                                                                                   
            }    
            return _filter(callback, composer, () => BaseHandle(callback, greedy, composer));  
        }

        private bool BaseHandle(object callback, bool greedy, ICallbackHandler composer)
        {
            return base.HandleCallback(callback, greedy, composer);
        }
    }
}
