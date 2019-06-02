namespace Miruken.Api.Route
{
    using Api;

    public class Routed : MessageDecorator
    {
        public Routed()
        {
        }

        public Routed(object message) : base(message)
        {
        }

        public string Route { get; set; }
        public string Tag   { get; set; }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other?.GetType() != GetType())
                return false;

            return other is Routed otherRouted
                   && Equals(Route, otherRouted.Route)
                   && Equals(Tag, otherRouted.Tag) &&
                   Equals(Message, otherRouted.Message);
        }

        public override int GetHashCode()
        {
            return ((Route?.GetHashCode() ?? 0) * 31 +
                    (Tag?.GetHashCode() ?? 0)) * 31 +
                    Message?.GetHashCode() ?? 0;
        }
    }

    public class RoutedRequest<TResponse> : Routed, IRequest<TResponse>
    {
        public RoutedRequest()
        {
        }

        public RoutedRequest(IRequest<TResponse> request)
            : base(request)
        {
        }
    }
}
