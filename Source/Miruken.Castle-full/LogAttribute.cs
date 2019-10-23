namespace Miruken.Castle
{
    using Callback;

    public class LogAttribute : FilterAttribute
    {
        public LogAttribute() : base(typeof(LogFilter<,>))
        {          
        }
    }
}
