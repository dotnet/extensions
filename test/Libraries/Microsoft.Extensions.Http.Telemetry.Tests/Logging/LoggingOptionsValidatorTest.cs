// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class LoggingOptionsValidatorTest
{
    [Fact]
    public void Ctor_CreatesAnInstance()
    {
        var act = () => _ = new LoggingOptionsValidator();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ObjectHasNoIssues_Success()
    {
        var validator = new LoggingOptionsValidator();
        var result = validator.Validate("model", new LoggingOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ObjectHasOneIssues_Fails()
    {
        var validator = new LoggingOptionsValidator();
        var options = new LoggingOptions { BodyReadTimeout = TimeSpan.Zero };

        validator.Validate("model", options).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ObjectHasTwoIssues_Fails()
    {
        var validator = new LoggingOptionsValidator();
        var options = new LoggingOptions { BodySizeLimit = -1 };

        validator.Validate("model", options).Failed.Should().BeTrue();
    }
}
