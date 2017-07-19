namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class Keys
    {
        private List<Type>   _typed;
        private List<object> _other;

        public IEnumerable<Type> Typed =>
            _typed ?? Enumerable.Empty<Type>();

        public IEnumerable<object> Other =>
            _other ?? Enumerable.Empty<object>();

        public IEnumerable All
        {
            get
            {
                if (_typed != null)
                    foreach (var type in Typed)
                        yield return type;
                if (_other != null)
                    foreach (var other in Other)
                        yield return other;
            }
        }

        public void AddKey(object key)
        {
            var type = key as Type;
            if (type == null)
                (_other ?? (_other = new List<object>())).Add(key);
            else
                (_typed ?? (_typed = new List<Type>())).Add(type);
        }
    }
}