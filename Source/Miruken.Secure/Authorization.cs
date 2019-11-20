namespace Miruken.Secure
{
    using System;
    using System.Diagnostics;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;
    using Concurrency;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class Authorization : ICallbackKey, IDispatchCallback
    {
        private Promise<bool> _result;

        public Authorization(
            object target, IPrincipal principal,
            object key = null)
        {
            Target = target
                ?? throw new ArgumentNullException(nameof(target));
            Principal = principal
                ?? throw new ArgumentNullException(nameof(principal));
            Key = key;
        }

        public object     Key       { get; }
        public object     Target    { get; }
        public IPrincipal Principal { get; }

        public CallbackPolicy Policy => Authorizes.Policy;

        public Promise<bool> Result => _result ?? Promise.True;

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            return _result == null && Policy.Dispatch(
                handler, this, greedy, composer, SetResult);
        }

        private bool SetResult(object result, bool strict, int? priority = null)
        {
            if (_result != null) return false;
            switch (result)
            {
                case bool b:
                    _result = Promise.Resolved(b);
                    break;
                case Task<bool> tb:
                    _result = Promise.Resolved(tb);
                    break;
                case Promise<bool> pb:
                    _result = pb;
                    break;
                default:
                    return false;
            }
            return true;
        }

        private string DebuggerDisplay => 
            $"Authorization | { Key ?? Target}";
    }
}
