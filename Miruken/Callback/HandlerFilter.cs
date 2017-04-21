using System;

namespace Miruken.Callback
{
    public delegate bool HandlerFilterDelegate(
        object callback, IHandler composer, Func<bool> proceed
    );

    public class HandlerFilter : HandlerDecorator
    {
        private readonly HandlerFilterDelegate _filter;
        private readonly bool _reentrant;

        public HandlerFilter(
            IHandler handler, HandlerFilterDelegate filter
            ) : this(handler, filter, false)
        {           
        }

        public HandlerFilter(
            IHandler handler, HandlerFilterDelegate filter, 
            bool reentrant) : base(handler)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _filter    = filter;
            _reentrant = reentrant;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            if (!_reentrant && callback is Composition) {                                                                                              
                return base.HandleCallback(callback, greedy, composer);                                                                                                   
            }    
            return _filter(callback, composer, () => BaseHandle(callback, greedy, composer));  
        }

        private bool BaseHandle(object callback, bool greedy, IHandler composer)
        {
            return base.HandleCallback(callback, greedy, composer);
        }
    }
}
