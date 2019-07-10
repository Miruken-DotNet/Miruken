namespace Miruken.Callback
{
    using Policy.Bindings;

    public class CallbackContext
    {
        public CallbackContext(
            object callback, IHandler composer, MemberBinding binding)
        {
            Callback = callback;
            Composer = composer;
            Binding  = binding;
        }

        public object        Callback  { get; }
        public IHandler      Composer  { get; }
        public MemberBinding Binding   { get; }
        public bool          Unhandled { get; private set; }

        public void NotHandled()
        {
            Unhandled = true;
        }
    }
}
