// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Telemetry.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

public class RequestLatencyTelemetryOptionsValidatorTest
{
    [Fact]
    public void RequestLatencyOptionsValidator_BadConfig_ReturnsFail()
    {
        var validator = new RequestLatencyTelemetryOptionsValidator();
        var options = new RequestLatencyTelemetryOptions { LatencyDataExportTimeout = TimeSpan.FromSeconds(0) };

        Assert.True(validator.Validate(nameof(RequestLatencyTelemetryOptions), options).Failed);
    }

    [Fact]
    public void RequestLatencyOptionsValidator_CoreectConfig_ReturnsSucess()
    {
        var validator = new RequestLatencyTelemetryOptionsValidator();
        var options = new RequestLatencyTelemetryOptions { LatencyDataExportTimeout = TimeSpan.FromSeconds(1) };
        var validationResult = validator.Validate(nameof(RequestLatencyTelemetryOptions), options);
        Assert.True(validationResult.Succeeded);
    }
}
