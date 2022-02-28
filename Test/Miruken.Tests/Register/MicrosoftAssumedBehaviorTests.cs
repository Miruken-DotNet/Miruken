namespace Miruken.Tests.Register;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MicrosoftAssumedBehaviorTests : AssumedBehaviorTests
{
    protected override IServiceProvider CreateServiceProvider(
        IServiceCollection serviceCollection)
    {
        return serviceCollection.BuildServiceProvider();
    }
}