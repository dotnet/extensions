// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class WeightAssignmentHelperTest
{
    [Fact]
    public void GetWeightSum_ShouldReturnWeightSum()
    {
        var weights = new Dictionary<string, double>
        {
            { "TestA", 3 },
            { "TestB", 7 }
        };

        var result = WeightAssignmentHelper.GetWeightSum(weights);
        Assert.Equal(10, result);
    }

    [Fact]
    public void GenerateRandom_NumberWithinRange()
    {
        var result = WeightAssignmentHelper.GenerateRandom(10);

        Assert.True(result <= 10);
        Assert.True(result >= 0);
    }

    [Fact]
    public void GenerateRandom_Mutation_Check()
    {
        var result = WeightAssignmentHelper.GenerateRandom(0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void IsUnderMax_ValueUnderMax_ShouldReturnTrue()
    {
        var result = WeightAssignmentHelper.IsUnderMax(2, 5);

        Assert.True(result);
    }

    [Fact]
    public void IsUnderMax_ValueOverMax_ShouldReturnFalse()
    {
        var result = WeightAssignmentHelper.IsUnderMax(6, 5);

        Assert.False(result);
    }

    [Fact]
    public void IsUnderMax_ValueEqualsMax_ShouldReturnTrue()
    {
        var result = WeightAssignmentHelper.IsUnderMax(2, 2);

        Assert.True(result);
    }
}
