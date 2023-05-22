// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Metering.Internal;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Metering.Test;

public class EventCountersValidatorTest
{
    private const string FieldName = nameof(EventCountersCollectorOptions.Counters);

    private readonly EventCountersValidator _validator = new();

    [Fact]
    public void ShouldValidateCustomNamedOptions_NullSet()
    {
        const string OptionsName = "MyCustomName";

        var options = new EventCountersCollectorOptions();
        options.Counters["key"] = null!;

        var result = _validator.Validate(OptionsName, options);
        Assert.True(result.Failed);
        Assert.Equal($"Counters[\"key\"]: The {OptionsName}.{FieldName}[\"key\"] field is required.", result.FailureMessage);
    }

    [Fact]
    public void ShouldValidateCustomNamedOptions_EmptySet()
    {
        const string OptionsName = nameof(EventCountersCollectorOptions);

        var options = new EventCountersCollectorOptions();
        options.Counters["key"] = new HashSet<string>();

        var result = _validator.Validate(string.Empty, options);
        Assert.True(result.Failed);
        Assert.Equal($"Counters[\"key\"]: The field {OptionsName}.{FieldName}[\"key\"] length must be greater or equal than 1.", result.FailureMessage);
    }
}
