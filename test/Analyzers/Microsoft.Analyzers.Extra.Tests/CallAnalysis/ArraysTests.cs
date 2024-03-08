// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class ArraysTests
{
    [Fact]
    public static async Task Arrays()
    {
        const string Source = @"
            using System;
            using System.Collections.Generic;
            using System.Collections.Immutable;

            namespace Example
            {
                public enum Color { Red, Green, Blue };

                [Flags]
                public enum Bits { One = 1, Two = 2, Three = 4, Four = 16 };

                public class Test
                {
                    public void NoTrigger()
                    {
                        _ = new Dictionary<Bits, string>();
                        _ = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary<int, Color>(new KeyValuePair<int, Color>[0]);
                    }

                    public void Triggers()
                    {
                        _ = /*0+*/new Dictionary<Color, string>()/*-0*/;
                        _ = /*1+*/new Dictionary<sbyte, string>()/*-1*/;
                        _ = /*2+*/new Dictionary<byte, string>()/*-2*/;

                        _ = /*3+*/new SortedDictionary<Color, string>()/*-3*/;
                        _ = /*4+*/new SortedDictionary<sbyte, string>()/*-4*/;
                        _ = /*5+*/new SortedDictionary<byte, string>()/*-5*/;

                        _ = /*6+*/new HashSet<Color>()/*-6*/;
                        _ = /*7+*/new HashSet<sbyte>()/*-7*/;
                        _ = /*8+*/new HashSet<byte>()/*-8*/;

                        _ = /*9+*/new SortedSet<Color>()/*-9*/;
                        _ = /*10+*/new SortedSet<sbyte>()/*-10*/;
                        _ = /*11+*/new SortedSet<byte>()/*-11*/;

                        _ = /*12+*/ImmutableDictionary.Create<Color, string>()/*-12*/;
                        _ = /*13+*/ImmutableDictionary.Create<sbyte, string>()/*-13*/;
                        _ = /*14+*/ImmutableDictionary.Create<byte, string>()/*-14*/;

                        _ = /*15+*/ImmutableDictionary.CreateRange<Color, string>(new KeyValuePair<Color, string>[0])/*-15*/;
                        _ = /*16+*/ImmutableDictionary.CreateRange<sbyte, string>(new KeyValuePair<sbyte, string>[0])/*-16*/;
                        _ = /*17+*/ImmutableDictionary.CreateRange<byte, string>(new KeyValuePair<byte, string>[0])/*-17*/;

                        _ = /*18+*/ImmutableDictionary.CreateBuilder<Color, string>().ToImmutable()/*-18*/;
                        _ = /*19+*/ImmutableDictionary.CreateBuilder<sbyte, string>().ToImmutable()/*-19*/;
                        _ = /*20+*/ImmutableDictionary.CreateBuilder<byte, string>().ToImmutable()/*-20*/;

                        _ = /*21+*/ImmutableSortedDictionary.Create<Color, string>()/*-21*/;
                        _ = /*22+*/ImmutableSortedDictionary.Create<sbyte, string>()/*-22*/;
                        _ = /*23+*/ImmutableSortedDictionary.Create<byte, string>()/*-23*/;

                        _ = /*24+*/ImmutableSortedDictionary.CreateRange<Color, string>(new KeyValuePair<Color, string>[0])/*-24*/;
                        _ = /*25+*/ImmutableSortedDictionary.CreateRange<sbyte, string>(new KeyValuePair<sbyte, string>[0])/*-25*/;
                        _ = /*26+*/ImmutableSortedDictionary.CreateRange<byte, string>(new KeyValuePair<byte, string>[0])/*-26*/;

                        _ = /*27+*/ImmutableSortedDictionary.CreateBuilder<Color, string>().ToImmutable()/*-27*/;
                        _ = /*28+*/ImmutableSortedDictionary.CreateBuilder<sbyte, string>().ToImmutable()/*-28*/;
                        _ = /*29+*/ImmutableSortedDictionary.CreateBuilder<byte, string>().ToImmutable()/*-29*/;

                        _ = /*30+*/ImmutableHashSet.Create<Color>()/*-30*/;
                        _ = /*31+*/ImmutableHashSet.Create<sbyte>()/*-31*/;
                        _ = /*32+*/ImmutableHashSet.Create<byte>()/*-32*/;

                        _ = /*33+*/ImmutableHashSet.CreateRange<Color>(new Color[0])/*-33*/;
                        _ = /*34+*/ImmutableHashSet.CreateRange<sbyte>(new sbyte[0])/*-34*/;
                        _ = /*35+*/ImmutableHashSet.CreateRange<byte>(new byte[0])/*-35*/;

                        _ = /*36+*/ImmutableHashSet.CreateBuilder<Color>().ToImmutable()/*-36*/;
                        _ = /*37+*/ImmutableHashSet.CreateBuilder<sbyte>().ToImmutable()/*-37*/;
                        _ = /*38+*/ImmutableHashSet.CreateBuilder<byte>().ToImmutable()/*-38*/;

                        _ = /*39+*/ImmutableSortedSet.Create<Color>()/*-39*/;
                        _ = /*40+*/ImmutableSortedSet.Create<sbyte>()/*-40*/;
                        _ = /*41+*/ImmutableSortedSet.Create<byte>()/*-41*/;

                        _ = /*42+*/ImmutableSortedSet.CreateRange<Color>(new Color[0])/*-42*/;
                        _ = /*43+*/ImmutableSortedSet.CreateRange<sbyte>(new sbyte[0])/*-43*/;
                        _ = /*44+*/ImmutableSortedSet.CreateRange<byte>(new byte[0])/*-44*/;

                        _ = /*45+*/ImmutableSortedSet.CreateBuilder<Color>().ToImmutable()/*-45*/;
                        _ = /*46+*/ImmutableSortedSet.CreateBuilder<sbyte>().ToImmutable()/*-46*/;
                        _ = /*47+*/ImmutableSortedSet.CreateBuilder<byte>().ToImmutable()/*-47*/;

                        _ = /*48+*/System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary<Color, string>(new KeyValuePair<Color, string>[0], null!)/*-48*/;
                        _ = /*49+*/System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary<sbyte, string>(new KeyValuePair<sbyte, string>[0], null!)/*-49*/;
                        _ = /*50+*/System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary<byte, string>(new KeyValuePair<byte, string>[0], null!)/*-50*/;

                        _ = /*51+*/System.Collections.Frozen.FrozenSet.ToFrozenSet<Color>(new Color[0], null!)/*-51*/;
                        _ = /*52+*/System.Collections.Frozen.FrozenSet.ToFrozenSet<sbyte>(new sbyte[0], null!)/*-52*/;
                        _ = /*53+*/System.Collections.Frozen.FrozenSet.ToFrozenSet<byte>(new byte[0], null!)/*-53*/;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            new[]
            {
                Assembly.GetAssembly(typeof(SortedDictionary<,>))!,
                Assembly.GetAssembly(typeof(ImmutableDictionary<,>))!,
                Assembly.GetAssembly(typeof(FrozenDictionary<,>))!,
            },
            new[] { Source });

        Assert.Equal(54, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.Arrays, d[i]);
        }
    }
}
