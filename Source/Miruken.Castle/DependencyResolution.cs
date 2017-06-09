using Miruken.Callback;

namespace Miruken.Castle
{
    public class DependencyResolution : Inquiry
    {
        private IHandler _handler;

        public DependencyResolution(object key,
            DependencyResolution parent = null, bool many = false)
            : base(key, many)
        {
            Parent = parent;
        }

        public DependencyResolution Parent { get; }

        public bool Claim(IHandler handler)
        {
            if (IsResolvingDependency(handler)) return false;
            _handler = handler;
            return true;
        }

        public bool IsResolvingDependency(IHandler handler)
        {
            return ReferenceEquals(handler, _handler)
                || (Parent?.IsResolvingDependency(handler) == true);
        }
    }
}
