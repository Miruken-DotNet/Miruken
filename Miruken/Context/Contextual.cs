namespace Miruken.Context
{
    public class Contextual<TContext> : IContextual<TContext>
        where TContext : class, IContext<TContext>
    {
        private TContext _context;

        public TContext Context
        {
            get { return _context; }
            set
            {
                if (_context == value) return;
                ContextChanging?.Invoke(this, _context, value);
                _context?.RemoveHandlers(this);
                var oldContext = _context;
                _context = value;
                _context?.InsertHandlers(0, this);
                ContextChanged?.Invoke(this, oldContext, _context);
            }
        }

        public event ContextDelegate<TContext> ContextChanging;
        public event ContextDelegate<TContext> ContextChanged;
    }

    public class Contextual : Contextual<IContext>
    {   
    }
}
