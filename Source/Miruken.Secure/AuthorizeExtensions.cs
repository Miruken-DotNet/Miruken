namespace Miruken.Secure
{
    using System;
    using System.Security.Principal;
    using Callback;
    using Concurrency;

    public static class AuthorizeExtensions
    {
        public static bool CanAccess(
            this IHandler handler, object target,
            IPrincipal principal, object policy = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            return handler.CanAccessAsync(target, principal, policy)
                .Wait();
        }

        public static Promise<bool> CanAccessAsync(
            this IHandler handler, object target,
            IPrincipal principal, object policy = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var authorization = new Authorization(target, principal, policy);
            handler.Handle(authorization);
            return authorization.Result;
        }
    }
}
