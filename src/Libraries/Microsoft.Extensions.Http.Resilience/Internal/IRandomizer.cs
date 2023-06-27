// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Providers thread safe random support for this package.
/// </summary>
internal interface IRandomizer
{
    /// <summary>
    /// Gets the next random double.
    /// </summary>
    /// <param name="maxValue">The max value.</param>
    /// <returns>The next double.</returns>
    double NextDouble(double maxValue);

    /// <summary>
    /// Gets the next random int.
    /// </summary>
    /// <param name="maxValue">The max value.</param>
    /// <returns>The next int.</returns>
    int NextInt(int maxValue);
}
