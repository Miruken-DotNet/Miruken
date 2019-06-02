namespace Miruken.Functional
{
    public class Try<TE, TR> : Either<TE, TR>
    {
        public Try(TR result)
            : base(result)
        {
        }

        public Try(TE error)
            : base(error)
        {
            IsError = true;
        }

        protected Try()
        {
        }

        public bool IsError { get; }

        public static implicit operator Try<TE, TR>(TE error) => new Try<TE, TR>(error);
        public static implicit operator Try<TE, TR>(TR result) => new Try<TE, TR>(result);
    }
}