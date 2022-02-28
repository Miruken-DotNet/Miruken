namespace Miruken.Api.Once;

using System.Threading.Tasks;
using Callback;

public interface IOnceStrategy
{
    Task Complete(Once once, IHandler composer);
}