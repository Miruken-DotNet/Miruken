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
            _filter    = filter ?? throw new ArgumentNullException(nameof(filter));
            _reentrant = reentrant;
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            if (!_reentrant && callback is Composition) {                                                                                              
                return base.HandleCallback(callback, ref greedy, composer);                                                                                                   
            }
            var g = greedy;
            var handled = _filter(callback, composer,
                () => BaseHandle(callback, ref g, composer));
            greedy = g;
            return handled;
        }

        private bool BaseHandle(object callback, ref bool greedy, IHandler composer)
        {
            return base.HandleCallback(callback, ref greedy, composer);
        }
    }
}
