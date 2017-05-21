namespace Miruken.Mediator
{
    using System;
    using Callback;

    public class Oneway : IRequest, IDecorator
    {
        public Oneway()
        {
        }

        public Oneway(object request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Decoratee = request;
        }

        public object Decoratee { get; protected set; }
    }

    public class Oneway<TResp> : Oneway, IRequestDecorator<TResp>
    {
        public Oneway()
        {
        }

        public Oneway(IRequest<TResp> request)
            : base(request)
        {
        }

        public IRequest<TResp> Request
        {
            get { return (IRequest<TResp>)Decoratee; }
            set { Decoratee = value; }
        }
    }
}
