#if NETSTANDARD
namespace Miruken.Tests.Register
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Miruken.Register;

    [TestClass, Ignore]
    public class MirukenAssumedBehaviorTests : AssumedBehaviorTests
    {
        protected override IServiceProvider CreateServiceProvider(
            IServiceCollection serviceCollection)
        {
            return serviceCollection.AddMiruken().Build();
        }
    }
}
#endif
