namespace Miruken.Callback
{
    using System;
    using Policy.Bindings;

    public class CallbackContext
    {
        public CallbackContext(
            object callback, IHandler composer, MemberBinding binding)
        {
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Composer = composer ?? throw new ArgumentNullException(nameof(composer));
            Binding  = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public object        Callback  { get; }
        public IHandler      Composer  { get; }
        public MemberBinding Binding   { get; }
        public bool          Unhandled { get; private set; }

        public void NotHandled()
        {
            Unhandled = true;
        }

        public TRet NotHandled<TRet>(TRet result = default)
        {
            Unhandled = true;
            return result;
        }
    }
}
