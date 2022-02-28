namespace Miruken.Http.Authorize;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Callback;

public static class BasicExtensions
{
    public static IHandler Basic(
        this IHandler handler, NetworkCredential credential,
        string domainSeparator = "")
    {
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));
        return handler.Pipeline((request, cancel, _, next) =>
        {
            Basic(request, credential, domainSeparator);
            return next();
        });
    }

    public static IHandler Basic(
        this IHandler handler, Func<IHandler, NetworkCredential> credential,
        string domainSeparator = "")
    {
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));
        return handler.Pipeline((request, cancel, composer, next) =>
        {
            Basic(request, credential(composer), domainSeparator);
            return next();
        });
    }

    public static IHandler Basic(this IHandler handler,
        string userName, string password)
    {
        return handler.Basic(new NetworkCredential(userName, password));
    }

    public static IHandler Basic(this IHandler handler,
        string userName, string password, string domain,
        string domainSeparator = "")
    {
        return handler.Basic(
            new NetworkCredential(userName, password, domain),
            domainSeparator);
    }

    private static void Basic(
        HttpRequestMessage request, NetworkCredential credential,
        string domainSeparator)
    {
        var domain = credential.Domain;
        var value  = $"{credential.UserName}:{credential.Password}";
        if (!string.IsNullOrEmpty(domain))
            value = $"{domain}{domainSeparator}{value}";
        var basic = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
        request.Headers.Authorization = basic;
    }
}