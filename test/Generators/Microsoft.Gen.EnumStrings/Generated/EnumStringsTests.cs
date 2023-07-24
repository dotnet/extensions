// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.EnumStrings.Test;

public static class EnumStringsTests
{
    [Fact]
    public static void TestGeneral()
    {
        Test<Size0>(i => (Size0)i, v => v.ToInvariantString());
        Test<Size1>(i => (Size1)i, v => v.ToInvariantString());
        Test<Size2>(i => (Size2)i, v => v.ToInvariantString());
        Test<Size3>(i => (Size3)i, v => v.ToInvariantString());
        Test<Size4>(i => (Size4)i, v => v.ToInvariantString());
        Test<Size5>(i => (Size5)i, v => v.ToInvariantString());
        Test<Size6>(i => (Size6)i, v => v.ToInvariantString());
        Test<Size7>(i => (Size7)i, v => v.ToInvariantString());

        Test<Flags0>(i => (Flags0)i, v => v.ToInvariantString());
        Test<Flags1>(i => (Flags1)i, v => v.ToInvariantString());
        Test<Flags2>(i => (Flags2)i, v => v.ToInvariantString());
        Test<Flags3>(i => (Flags3)i, v => v.ToInvariantString());
        Test<Flags4>(i => (Flags4)i, v => v.ToInvariantString());
        Test<Flags5>(i => (Flags5)i, v => v.ToInvariantString());
        Test<Flags6>(i => (Flags6)i, v => v.ToInvariantString());
        Test<Flags7>(i => (Flags7)i, v => v.ToInvariantString());
        Test<Flags8>(i => (Flags8)i, v => v.ToInvariantString());

        Test<SByteEnum1>(i => (SByteEnum1)i, v => v.ToInvariantString());
        Test<SByteEnum2>(i => (SByteEnum2)i, v => v.ToInvariantString());
        Test<SByteEnum3>(i => (SByteEnum3)i, v => v.ToInvariantString());

        Test<ByteEnum1>(i => (ByteEnum1)i, v => v.ToInvariantString());
        Test<ByteEnum2>(i => (ByteEnum2)i, v => v.ToInvariantString());
        Test<ByteEnum3>(i => (ByteEnum3)i, v => v.ToInvariantString());

        Test<ShortEnum1>(i => (ShortEnum1)i, v => v.ToInvariantString());
        Test<ShortEnum2>(i => (ShortEnum2)i, v => v.ToInvariantString());
        Test<ShortEnum3>(i => (ShortEnum3)i, v => v.ToInvariantString());

        Test<UShortEnum1>(i => (UShortEnum1)i, v => v.ToInvariantString());
        Test<UShortEnum2>(i => (UShortEnum2)i, v => v.ToInvariantString());
        Test<UShortEnum3>(i => (UShortEnum3)i, v => v.ToInvariantString());

        Test<IntEnum1>(i => (IntEnum1)i, v => v.ToInvariantString());
        Test<IntEnum2>(i => (IntEnum2)i, v => v.ToInvariantString());
        Test<IntEnum3>(i => (IntEnum3)i, v => v.ToInvariantString());

        Test<UIntEnum1>(i => (UIntEnum1)i, v => v.ToInvariantString());
        Test<UIntEnum2>(i => (UIntEnum2)i, v => v.ToInvariantString());
        Test<UIntEnum3>(i => (UIntEnum3)i, v => v.ToInvariantString());

        Test<LongEnum1>(i => (LongEnum1)i, v => v.ToInvariantString());
        Test<LongEnum2>(i => (LongEnum2)i, v => v.ToInvariantString());
        Test<LongEnum3>(i => (LongEnum3)i, v => v.ToInvariantString());

        Test<ULongEnum1>(i => (ULongEnum1)i, v => v.ToInvariantString());
        Test<ULongEnum2>(i => (ULongEnum2)i, v => v.ToInvariantString());
        Test<ULongEnum3>(i => (ULongEnum3)i, v => v.ToInvariantString());

        Test<Options0>(i => (Options0)i, v => NamespaceX.ClassY.MethodZ(v));
        Test<Options1>(i => (Options1)i, v => NamespaceA.ClassB.MethodC(v));

        Test<Overlapping1>(i => (Overlapping1)i, v => v.ToInvariantString());
        Test<Overlapping2>(i => (Overlapping2)i, v => v.ToInvariantString());

        Test<TestClasses.Nested.Fruit>(i => (TestClasses.Nested.Fruit)i, v => v.ToInvariantString());

        Test<Level>(i => (Level)i, v => v.ToInvariantString());
        Test<Medal>(i => (Medal)i, v => v.ToInvariantString());

        Test<Negative0>(i => (Negative0)i, v => v.ToInvariantString());
        Test<Negative1>(i => (Negative1)i, v => v.ToInvariantString());

        Test<NegativeLong0>(i => (NegativeLong0)i, v => v.ToInvariantString());
        Test<NegativeLong1>(i => (NegativeLong1)i, v => v.ToInvariantString());

        static void Test<T>(Func<int, T> convert, Func<T, string> extension)
            where T : notnull
        {
            for (int i = -120; i < 120; i++)
            {
                var v = convert(i);
                Assert.Equal(v.ToString(), extension(v));
            }
        }
    }
}
