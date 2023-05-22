// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.EnumStrings;

namespace Microsoft.Gen.EnumStrings.Bench;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable R9A033 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance

[MemoryDiagnoser]
public class EnumStrings
{
    private static readonly int[] _randomValues = new int[1000];

    [EnumStrings]
    internal enum Color
    {
        Red,
        Green,
        Blue,
    }

    [Flags]
    [EnumStrings]
    internal enum SmallOptions
    {
        Options1 = 1,
        Options2 = 2,
        Options4 = 4,
        Options8 = 8,
        Options16 = 16,
    }

    [Flags]
    [EnumStrings]
    internal enum LargeOptions
    {
        Options1 = 1,
        Options2 = 2,
        Options4 = 4,
        Options8 = 8,
        Options16 = 16,
        Options32 = 32,
        Options64 = 64,
        Options128 = 128,
    }

    static EnumStrings()
    {
        var r = new Random();
        for (int i = 0; i < _randomValues.Length; i++)
        {
            _randomValues[i] = r.Next();
        }
    }

    [Benchmark]
    public void ToStringColor()
    {
        _ = Color.Red.ToString();
        _ = Color.Green.ToString();
        _ = Color.Blue.ToString();
    }

    [Benchmark]
    public void GetNameColor()
    {
        _ = Enum.GetName(Color.Red);
        _ = Enum.GetName(Color.Green);
        _ = Enum.GetName(Color.Blue);
    }

    [Benchmark]
    public void ToInvariantStringColor()
    {
        _ = Color.Red.ToInvariantString();
        _ = Color.Green.ToInvariantString();
        _ = Color.Blue.ToInvariantString();
    }

    [Benchmark]
    public void ToStringSmallOptions()
    {
        _ = SmallOptions.Options1.ToString();
        _ = SmallOptions.Options2.ToString();
        _ = SmallOptions.Options4.ToString();
        _ = SmallOptions.Options8.ToString();
        _ = SmallOptions.Options16.ToString();

        _ = (SmallOptions.Options1 | SmallOptions.Options16).ToString();
        _ = (SmallOptions.Options2 | SmallOptions.Options4).ToString();
    }

    [Benchmark]
    public void ToInvariantStringSmallOptions()
    {
        _ = SmallOptions.Options1.ToInvariantString();
        _ = SmallOptions.Options2.ToInvariantString();
        _ = SmallOptions.Options4.ToInvariantString();
        _ = SmallOptions.Options8.ToInvariantString();
        _ = SmallOptions.Options16.ToInvariantString();

        _ = (SmallOptions.Options1 | SmallOptions.Options16).ToInvariantString();
        _ = (SmallOptions.Options2 | SmallOptions.Options4).ToInvariantString();
    }

    [Benchmark]
    public void ToStringLargeOptions()
    {
        _ = LargeOptions.Options1.ToString();
        _ = LargeOptions.Options2.ToString();
        _ = LargeOptions.Options4.ToString();
        _ = LargeOptions.Options8.ToString();
        _ = LargeOptions.Options16.ToString();
        _ = LargeOptions.Options32.ToString();
        _ = LargeOptions.Options64.ToString();
        _ = LargeOptions.Options128.ToString();

        _ = (LargeOptions.Options1 | LargeOptions.Options16).ToString();
        _ = (LargeOptions.Options2 | LargeOptions.Options4).ToString();
    }

    [Benchmark]
    public void ToInvariantStringLargeOptions()
    {
        _ = LargeOptions.Options1.ToInvariantString();
        _ = LargeOptions.Options2.ToInvariantString();
        _ = LargeOptions.Options4.ToInvariantString();
        _ = LargeOptions.Options8.ToInvariantString();
        _ = LargeOptions.Options16.ToInvariantString();
        _ = LargeOptions.Options32.ToInvariantString();
        _ = LargeOptions.Options64.ToInvariantString();
        _ = LargeOptions.Options128.ToInvariantString();

        _ = (LargeOptions.Options1 | LargeOptions.Options16).ToInvariantString();
        _ = (LargeOptions.Options2 | LargeOptions.Options4).ToInvariantString();
    }

    // the next two benchmarks aren't representative of expected real-world use cases, but let's see the impact the code gen has relative to naked Enum.ToString

    [Benchmark]
    public void ToStringRandom()
    {
        for (int i = 0; i < _randomValues.Length; i++)
        {
            var o = (LargeOptions)_randomValues[i];
            _ = o.ToString();
        }
    }

    [Benchmark]
    public void ToInvariantStringRandom()
    {
        for (int i = 0; i < _randomValues.Length; i++)
        {
            var o = (LargeOptions)_randomValues[i];
            _ = o.ToInvariantString();
        }
    }
}
