// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

[DebuggerDisplay("{ToDebugString(),nq}")]
internal readonly struct RationalNumber : IEquatable<RationalNumber>
{
    public static readonly RationalNumber Zero = new(0, 1);

    public RationalNumber(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException("Denominator cannot be zero.");
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public int Numerator { get; }
    public int Denominator { get; }

    public double ToDouble() => (double)Numerator / Denominator;

    public string ToDebugString() => $"{Numerator}/{Denominator}";

    public bool Equals(RationalNumber other)
        => other.Numerator == Numerator && other.Denominator == Denominator;

    public override bool Equals(object? obj) => obj is RationalNumber other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Numerator, Denominator);
}
