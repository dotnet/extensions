// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Testing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Test.Internal;

public class FakeStartupTest
{
    [Fact]
    public void MethodsDoNothing()
    {
        var sut = new FakeStartup();

        var exception = Record.Exception(() =>
        {
            sut.Configure(null!);
            sut.Configure(new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()));
            sut.ConfigureServices(null!);
            sut.ConfigureServices(new ServiceCollection());
        });

        Assert.Null(exception);
    }
}
