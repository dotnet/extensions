// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Http.Resilience.Internal;

#pragma warning disable CA5394 // Do not use insecure randomness

internal class Randomizer
{
    private static readonly ThreadLocal<Random> _randomInstance = new(() => new Random());

    public virtual double NextDouble(double maxValue) => _randomInstance.Value!.NextDouble() * maxValue;

    public virtual int NextInt(int maxValue) => _randomInstance.Value!.Next(maxValue);
}
