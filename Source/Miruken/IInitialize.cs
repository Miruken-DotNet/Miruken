namespace Miruken
{
    using System;
    using Concurrency;

    public interface IInitialize
    {
        bool Initialized { get; set; }

        Promise Initialize();

        void FailedInitialize(Exception exception = null);
    }
}
