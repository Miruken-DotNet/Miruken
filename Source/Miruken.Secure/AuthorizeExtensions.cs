namespace Miruken.Secure;

using System;
using System.Security.Principal;
using Callback;
using Concurrency;

public static class AuthorizeExtensions
{
    public static bool Authorize(
        this IHandler handler, object target,
        IPrincipal principal, object policy = null)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        return handler.AuthorizeAsync(target, principal, policy)
            .GetAwaiter().GetResult();
    }

    public static Promise<bool> AuthorizeAsync(
        this IHandler handler, object target,
        IPrincipal principal, object policy = null)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
        var options = handler.GetOptions<AuthorizationOptions>();
        if (options?.RequireAuthenticatedUser == true &&
            principal.Identity?.IsAuthenticated != true)
            return Promise.False;
        var authorization = new Authorization(target, principal, policy);
        if (!handler.Provide(principal).Handle(authorization) && 
            options?.RequirePolicy == true)
            return Promise.False;
        return authorization.Result;
    }

    public static IHandler RequireAuthentication(this IHandler handler, bool required = true) =>
        handler == null ? null 
            : new AuthorizationOptions { RequireAuthenticatedUser = required }.Decorate(handler);

    public static IHandler RequireAccess(this IHandler handler, bool required = true) =>
        handler == null ? null 
            : new AuthorizationOptions { RequirePolicy = required }.Decorate(handler);
}