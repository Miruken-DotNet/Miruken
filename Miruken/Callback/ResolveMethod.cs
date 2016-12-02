using System;
using System.Collections.Generic;

namespace Miruken.Callback
{
    public class ResolveMethod : ICallback
    {
        private readonly HandleMethod _handleMethod;
        private readonly bool _all;

        public ResolveMethod(HandleMethod handleMethod, bool all)
        {
            _handleMethod = handleMethod;
            _all          = all;
        }

        public Type ResultType
        {
            get { return _handleMethod.ResultType; }
        }

        public object Result
        {
            get { return _handleMethod.Result; }
            set { _handleMethod.Result = value; }
        }

        public bool InvokeResolve(ICallbackHandler composer)
        {
            var targets = composer.ResolveAll(_handleMethod.TargetType);
            return InvokeTargets(targets, composer);
        }

        private bool InvokeTargets(IEnumerable<object> targets, ICallbackHandler composer)
        {
            var handled = false;
            foreach (var target in targets)
            {
                handled = _handleMethod.InvokeOn(target, composer) || handled;
                if (handled && !_all)
                    break;
            }
            return handled;
        }
    }
}
