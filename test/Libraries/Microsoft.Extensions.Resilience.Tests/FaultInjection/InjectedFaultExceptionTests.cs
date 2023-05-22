// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using AutoFixture;
using Microsoft.Extensions.Resilience.FaultInjection;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.FaultInjection.Test;

public class InjectedFaultExceptionTests
{
    [Fact]
    public void Ctor_Empy()
    {
        var exception = new InjectedFaultException();

        Assert.NotNull(exception);
    }

    [Fact]
    public void Ctor_WithMessage()
    {
        var message = new Fixture().Create<string>();
        var exception = new InjectedFaultException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Ctor_WithMessageAndInnerException()
    {
        var message = new Fixture().Create<string>();
        var innerException = new Fixture().Create<Exception>();

        var exception = new InjectedFaultException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}
