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

        public bool Dispatch(Handler handler, ref bool greedy, IHandler composer)
        {
            var g = greedy;
            var handled = _inquiries.AsParallel().All((IDispatchCallback inquiry) =>
                inquiry.Dispatch(handler, ref g, composer));
            greedy = g;
            return handled;
        }
    }
}
