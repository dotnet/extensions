// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Moq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Test;

public class AutoActivationHostedServiceTests
{
    [Fact]
    public void Ctor_Throws_WhenOptionsValueIsNull()
    {
        var options = Microsoft.Extensions.Options.Options.Create<AutoActivatorOptions>(null!);
        Assert.Throws<ArgumentException>(() => new AutoActivationHostedService(Mock.Of<IServiceProvider>(), options));
    }
}
