// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Http.Resilience.Internal;

#pragma warning disable CA5394 // Do not use insecure randomness
#pragma warning disable CA1852 // Seal internal types

internal class Randomizer
{
#if NET6_0_OR_GREATER
    public virtual double NextDouble(double maxValue) => Random.Shared.NextDouble() * maxValue;

    public virtual int NextInt(int maxValue) => Random.Shared.Next(maxValue);
#else
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());

    public virtual double NextDouble(double maxValue) => _randomInstance.Value!.NextDouble() * maxValue;

    public virtual int NextInt(int maxValue) => _randomInstance.Value!.Next(maxValue);
#endif
}
