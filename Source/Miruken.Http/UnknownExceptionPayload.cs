namespace Miruken.Http
{
    using System;

    public class UnknownExceptionPayload : Exception
    {
        public UnknownExceptionPayload(object payload)
            : base($"Unable to map the exception payload '{payload?.GetType()}'")
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public object Payload { get; }
    }
}
