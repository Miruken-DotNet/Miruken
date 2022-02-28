namespace Miruken;

using Concurrency;

public interface IInitialize
{
    Promise Initialize();
}