using System;
using System.Collections.Generic;
using System.Linq;

namespace Miruken.Callback
{
    public class Resolution : ICallback
    {
        private readonly List<object> _resolutions;
        private object _result;

        public Resolution(object key)
            : this(key, false)
        {
        }

        public Resolution(object key, bool many)
        {
            Key          = key;
            Many         = many;
            _resolutions = new List<object>();
        }

        public object Key  { get; private set; }

        public bool   Many { get; private set; }

        public ICollection<object> Resolutions
        {
            get { return _resolutions.AsReadOnly(); }
        }

        public Type ResultType
        {
            get { return Key as Type; }
        }

        public object Result
        {
            get
            {
                if (_result != null)
                    return _result;

                if (Many)
                    _result = _resolutions.ToArray();
                else if (_resolutions.Count > 0)
                    _result = _resolutions[0];

                return _result;
            }
            set { _result = value; }
        }

        public void Resolve(params object[] resolutions)
        {
            if (resolutions == null || resolutions.Length == 0 ||
                (!Many && _resolutions.Count > 0))
                return;

            _resolutions.AddRange(resolutions.Where(r => r != null));
            _result = null;
        }

        public bool TryResolve(object item, bool invariant)
        {
            var type = Key as Type;
            if (type == null) return false;

            var compatible = invariant
                           ? type == item.GetType()
                           : type.IsInstanceOfType(item);

            if (!compatible)
                return false;

            Resolve(item);
            return true;	            
        }
    }
}
