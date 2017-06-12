namespace Miruken.Callback
{
    using System;

    public delegate bool ProvidesDelegate(Inquiry inquiry, IHandler composer);

    public class Provider : Handler
    {
        private readonly ProvidesDelegate _provider;

        public Provider(ProvidesDelegate provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            _provider = provider;
        }

        protected override bool HandleCallback(
            object callback, ref bool greedy, IHandler composer)
        {
            var compose = callback as Composition;
            if (compose != null)
                callback = compose.Callback;

            var inquiry = callback as Inquiry;
            return inquiry != null && _provider(inquiry, composer);
        }
    }
}
