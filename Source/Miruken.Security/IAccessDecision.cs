namespace Miruken.Security
{
    using System.Security.Principal;

    public interface IAccessDecision
    {
        bool Allow(IPrincipal principal);
    }
}
