namespace Miruken
{
    using System;

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

        public bool IsError { get; }
    }

    public class Try<TR> : Try<Exception, TR>
    {
        public Try(TR result)
             : base(result)
        {
        }

        public Try(Exception exception)
        : base(exception)
        {
        }
    }
}
