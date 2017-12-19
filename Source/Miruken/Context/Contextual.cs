namespace Miruken.Context
{
    public class Contextual<TContext> : IContextual<TContext>
        where TContext : class, IContext<TContext>
    {
        private TContext _context;

        public TContext Context
        {
            get => _context;
            set
            {
                if (_context == value) return;
                var newContext = value;
                ContextChanging?.Invoke(this, _context, ref newContext);
                _context?.RemoveHandlers(this);
                var oldContext = _context;
                _context = newContext;
                _context?.InsertHandlers(0, this);
                ContextChanged?.Invoke(this, oldContext, _context);
            }
        }

        public event ContextChangingDelegate<TContext> ContextChanging;
        public event ContextChangedDelegate<TContext> ContextChanged;
    }

    public class Contextual : Contextual<IContext>, IContextual
    {   
    }
}
