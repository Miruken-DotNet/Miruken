namespace Miruken.Callback
{
    using System.Linq;

    public class BatchInquiry : IDispatchCallback
    {
        private readonly Inquiry[] _inquiries;

        public BatchInquiry(params Inquiry[] inquiries)
        {
            _inquiries = inquiries;
        }

        public bool Dispatch(Handler handler, bool greedy, IHandler composer)
        {
            return _inquiries.AsParallel().All((IDispatchCallback inquiry) =>
                inquiry.Dispatch(handler, greedy, composer));
        }
    }
}
