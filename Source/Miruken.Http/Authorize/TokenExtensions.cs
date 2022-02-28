namespace Miruken.Http.Authorize;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Callback;

public static class TokenExtensions
{
    public static IHandler Token(this IHandler handler,
        string token, string scheme = "Bearer")
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException(
                $"{scheme} token cannot be empty", nameof(token));
        return handler.Pipeline((request, cancel, _, next) =>
        {
            Token(request, scheme, token);
            return next();
        });
    }

    public static IHandler Token(this IHandler handler,
        Func<IHandler, string> token, string scheme = "Bearer")
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));
        return handler.Pipeline((request, cancel, composer, next) =>
        {
            Token(request, scheme, token(composer));
            return next();
        });
    }

    public static IHandler Token(this IHandler handler,
        Func<CancellationToken, IHandler, Task<string>> token,
        string scheme = "Bearer")
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));
        return handler.Pipeline(async (request, cancel, composer, next) =>
        {
            Token(request, scheme, await token(cancel, composer));
            return await next();
        });
    }
        
    private static void Token(HttpRequestMessage request, string scheme, string token)
    {
        var oauth = new AuthenticationHeaderValue(scheme, token);
        request.Headers.Authorization = oauth;
    }
}