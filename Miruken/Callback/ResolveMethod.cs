using System;

namespace Miruken.Callback
{
    public class ResolveMethod : ICallback
    {
        private readonly HandleMethod _handleMethod;

        public ResolveMethod(HandleMethod handleMethod)
        {
            _handleMethod = handleMethod;
        }

        public Type ResultType => _handleMethod.ResultType;

        public object Result
        {
            get { return _handleMethod.Result; }
            set { _handleMethod.Result = value; }
        }

        public bool InvokeResolve(IHandler handler, IHandler composer)
        {
            var target = handler.Resolve(_handleMethod.Protocol);
            return target != null && _handleMethod.InvokeOn(target, composer);
        }
    }
}
