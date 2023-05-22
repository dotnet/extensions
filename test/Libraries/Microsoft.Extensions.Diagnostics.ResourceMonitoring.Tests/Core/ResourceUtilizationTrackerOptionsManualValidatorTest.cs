// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
#if NETCOREAPP3_1_OR_GREATER
using System.Linq;
#endif
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class ResourceUtilizationTrackerOptionsManualValidatorTest
{
    [Theory]
    [InlineData(6, 5)]
    [InlineData(6, 6)]
    public void Validator_GivenValidOptions_Succeeds(int collectionWindow, int calculationPeriod)
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromSeconds(collectionWindow),
            CalculationPeriod = TimeSpan.FromSeconds(calculationPeriod)
        };

        var validator = new ResourceUtilizationTrackerOptionsManualValidator();
        var isValid = validator.Validate(nameof(options), options).Succeeded;
        Assert.True(isValid);
    }

    [Fact]
    public void Validator_CalculationPeriodBiggerThanCollectionWindow_Fails()
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromSeconds(1),
            CalculationPeriod = TimeSpan.FromSeconds(2)
        };

        var validator = new ResourceUtilizationTrackerOptionsManualValidator();
        var validationResult = validator.Validate(nameof(options), options);

        Assert.True(validationResult.Failed);

#if NETCOREAPP3_1_OR_GREATER
        var failureMessage = validationResult.Failures.Single();
#else
        var failureMessage = validationResult.FailureMessage;
#endif
        Assert.Equal("Property CalculationPeriod: Value must be <= to CollectionWindow (00:00:01), but is 00:00:02.", failureMessage);
    }
}
