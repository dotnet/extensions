// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Http.Resilience.Internal;

#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable CPR138 // Random class instances should be shared as statics.

internal sealed class Randomizer : IRandomizer
{
    private static readonly ThreadLocal<Random> _randomInstance = new(() => new Random());

    public double NextDouble(double maxValue) => _randomInstance.Value!.NextDouble() * maxValue;

    public int NextInt(int maxValue) => _randomInstance.Value!.Next(maxValue);
}
