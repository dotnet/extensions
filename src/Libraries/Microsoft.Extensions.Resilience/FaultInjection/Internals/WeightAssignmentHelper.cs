// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Resilience.FaultInjection;

internal static class WeightAssignmentHelper
{
    public static double GetWeightSum(Dictionary<string, double> weightAssignments)
    {
        return weightAssignments.Sum(pair => pair.Value);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Not intended to be cryptographically secure.")]
    public static double GenerateRandom(double maxValue)
    {
        var random = new Random();
        return random.NextDouble() * maxValue;
    }

    public static bool IsUnderMax(double value, double maxValue)
    {
        return value <= maxValue;
    }
}
