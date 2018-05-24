namespace Miruken.Security
{
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Callback;
    using Callback.Policy;

    public interface IAccessDecision : IProtocol
    {
        Task<bool> Allow(MethodBinding method,
            IPrincipal principal, IHandler composer);
    }
}
