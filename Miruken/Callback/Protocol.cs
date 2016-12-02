using System;

namespace Miruken.Callback
{
    public interface IProtocolAdapter
    {
        void Do<T>(Action<T> action) where T : class;

        R Do<T, R>(Func<T, R> func) where T : class;
    }

    public class NullProtocolAdapter : IProtocolAdapter
    {
        public static readonly NullProtocolAdapter 
            Instance = new NullProtocolAdapter();

        private NullProtocolAdapter()
        {        
        }

        public void Do<T>(Action<T> action) where T : class
        {
        }

        public R Do<T, R>(Func<T, R> func) where T : class
        {
            return default(R);
        }
    }

    public class Protocol
    {
        private readonly IProtocolAdapter _adapter;

        public const string Guid = "b753d49c-af1b-40fb-a771-ddfeab1b94b6";

        public Protocol(IProtocolAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException("adapter", string.Format(
                    "Protocol '{0}' was created without an adapter",
                    GetType().FullName));
            }
            _adapter = adapter;
        }

        protected void Do<T>(Action<T> action) where T : class
        {
            _adapter.Do(action);
        }

        protected R Do<T, R>(Func<T, R> func) where T : class
        {
            return _adapter.Do(func);
        }
    }

    public class NullableProtocol : Protocol
    {
        public NullableProtocol(IProtocolAdapter adapter)
            : base(adapter ?? NullProtocolAdapter.Instance)
        {           
        }
    }
}
