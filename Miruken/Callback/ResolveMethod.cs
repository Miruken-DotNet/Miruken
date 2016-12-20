namespace Miruken.Callback
{
    public class ResolveMethod : Resolution
    {
        private readonly HandleMethod _handleMethod;

        public ResolveMethod(object key, bool many, HandleMethod handleMethod)
            : base(key, many)
        {
            _handleMethod = handleMethod;
        }

        protected override bool IsSatisfied(object resolution, IHandler composer)
        {
            return _handleMethod.InvokeOn(resolution, composer);
        }
    }
}
