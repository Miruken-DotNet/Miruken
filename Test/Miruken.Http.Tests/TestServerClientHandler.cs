namespace Miruken.Http.Tests;

using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Callback;
using Microsoft.AspNetCore.TestHost;
    
[Unmanaged]
public class TestServerClientHandler : DelegatingHandler
{
    private readonly HttpMessageHandler _handler;

    [Provides]
    public TestServerClientHandler(TestServer server)
    {
        _handler = server.CreateHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // TestServer.CreateClient does not allow customizations
        // so we hijack it in the context of a DelegatingHandler

        return (Task<HttpResponseMessage>)_handler.GetType()
            .InvokeMember("SendAsync",
                BindingFlags.Public   | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.InvokeMethod,
                null, _handler, new object[] { request, cancellationToken });
    }
}