namespace Miruken.Api.Once;

using System.Threading.Tasks;
using Callback;

public sealed class DelegatingOnceStrategy : IOnceStrategy
{
    public static readonly DelegatingOnceStrategy Instance = new();

    private DelegatingOnceStrategy()
    {            
    }

    public Task Complete(Once once, IHandler composer) =>
        composer.With(once).Send(once.Request);
}