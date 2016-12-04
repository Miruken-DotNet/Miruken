namespace SixFlags.CF.Miruken.Context
{
    public class Context : ContextBase<IContext>, IContext
    {
        public Context()
        {   
        }

        protected Context(IContext parent) : base(parent)
        {     
        }

        protected override IContext InternalCreateChild()
        {
            return new Context(this);
        }
    }
}