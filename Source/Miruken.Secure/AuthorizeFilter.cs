namespace Miruken.Secure
{
    using System;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

    public class AuthorizeFilter<TCb, TRes> : DynamicFilter<TCb, TRes>
    {
        public AuthorizeFilter()
        {
            Order = Stage.Authorization;
        }

        public Task<TRes> Next(
            TCb callback, Next<TRes> next,
            IPrincipal principal, IHandler composer,
            MethodBinding method, IFilterProvider provider)
        {
            var authorize = provider as AuthorizeAttribute;
            if (authorize?.AllowAnonymous != true)
                principal.RequireAuthenticatedClaims();
            var policy        = GetPolicy(callback, method);
            var authorization = new Authorization(callback, principal, policy);
            if (!composer.Handle(authorization))
            {
                if (authorize?.NoAccessPolicy != true)
                    AccessDenied();
                return next();
            }
            return authorization.Result.Then((canAccess, s) =>
            {
                if (!canAccess) AccessDenied();
                return next();
            });
        }

        private static object GetPolicy(TCb callback, MethodBinding method)
        {
            var policy = method.Dispatcher.Attributes
                .OfType<AccessPolicyAttribute>().FirstOrDefault();
            if (policy == null && callback is HandleMethod)
            {
                var m     = method.Dispatcher.Method;
                var owner = m.ReflectedType ?? m.DeclaringType;
                var delim = owner != null ? ":" : "";
                return $"{owner?.FullName ?? ""}{delim}{m.Name}";
            };
            return policy?.Policy;
        }

        private static void AccessDenied()
        {
            throw new UnauthorizedAccessException(
                "Authorization has been denied");
        }
    }
}
