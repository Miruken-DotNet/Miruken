namespace Miruken.Security
{
    using System.Security.Principal;
    using Callback;

    public interface IAccessDecision
    {
        bool Allow(IPrincipal principal, IHandler composer);
    }
}
