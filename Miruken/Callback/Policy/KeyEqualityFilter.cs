namespace Miruken.Callback.Policy
{
    using System;

    public class KeyEqualityFilter<Cb> : ICallbackFilter
    {
        private readonly Func<Cb, object> _key;

        public KeyEqualityFilter(object match, Func<Cb, object> key)
        {
            Match = match;
            _key  = key;
        }

        public object Match { get; }

        public bool Accepts(object callback, IHandler composer)
        {
            return Equals(Match, _key((Cb)callback));
        }
    }
}
