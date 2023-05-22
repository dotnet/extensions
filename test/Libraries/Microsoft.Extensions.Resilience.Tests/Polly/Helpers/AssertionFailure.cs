// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience.Polly.Test.Helpers;

internal sealed class AssertionFailure
{
    public AssertionFailure(int expected, int actual, string measure)
    {
        if (string.IsNullOrWhiteSpace(measure))
        {
            throw new ArgumentNullException(nameof(measure));
        }

        Expected = expected;
        Actual = actual;
        Measure = measure;
    }

    public int Expected { get; }

    public int Actual { get; }

    public string Measure { get; }
}
