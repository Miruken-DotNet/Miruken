namespace Miruken.Callback
{
    using System;

    public interface ICallback
    {
        Type   ResultType { get; }
        object Result     { get; set; }
    }
}