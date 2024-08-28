// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET6_0

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Shared.StringSplit.Test;

public static class SplitExtensionsTests
{
    public static IEnumerable<object[]> StringData => new List<object[]>
    {
        new object[] { string.Empty, StringSplitOptions.None },
        new object[] { "A", StringSplitOptions.None },
        new object[] { "AA", StringSplitOptions.None },
        new object[] { "XX", StringSplitOptions.None },
        new object[] { "AXX", StringSplitOptions.None },
        new object[] { "AAXX", StringSplitOptions.None },
        new object[] { "YYA", StringSplitOptions.None },
        new object[] { "XXAA", StringSplitOptions.None },
        new object[] { "AAXXB", StringSplitOptions.None },
        new object[] { "AAXXYY", StringSplitOptions.None },
        new object[] { "AAXXYYBB", StringSplitOptions.None },

        new object[] { string.Empty, StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "XX", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AXX", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AAYY", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "XXA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "XXAA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AAXXB", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AAXX", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AAYYBB", StringSplitOptions.RemoveEmptyEntries },

        new object[] { string.Empty, StringSplitOptions.TrimEntries },
        new object[] { "XX", StringSplitOptions.TrimEntries },
        new object[] { "YYA", StringSplitOptions.TrimEntries },
        new object[] { "AA", StringSplitOptions.TrimEntries },
        new object[] { "AXXA", StringSplitOptions.TrimEntries },
        new object[] { "YY", StringSplitOptions.TrimEntries },
        new object[] { "XXYY", StringSplitOptions.TrimEntries },
        new object[] { "AXX", StringSplitOptions.TrimEntries },
        new object[] { "AXXYY", StringSplitOptions.TrimEntries },
        new object[] { "AAXX", StringSplitOptions.TrimEntries },
        new object[] { "XA", StringSplitOptions.TrimEntries },
        new object[] { "XXYYXXAXX", StringSplitOptions.TrimEntries },
        new object[] { "XXAA", StringSplitOptions.TrimEntries },
        new object[] { "AAYYB", StringSplitOptions.TrimEntries },
        new object[] { "AAXXYY", StringSplitOptions.TrimEntries },
        new object[] { "AAYYXXBB", StringSplitOptions.TrimEntries },
        new object[] { "   abcde   XXfghijk    YYlmn", StringSplitOptions.TrimEntries },
    };

    [Theory]
    [MemberData(nameof(StringData))]
    public static void Strings(string input, StringSplitOptions options)
    {
        for (int i = 0; i < 2; i++)
        {
            var separator = i == 0 ? "XX" : "YY";

            var expected = input.Split(separator, options);

            var actual = new StringRange[20];
            Assert.True(input.AsSpan().TrySplit(separator, actual, out int numActuals, StringComparison.Ordinal, options));

            Assert.Equal(expected.Length, numActuals);
            Assert.Equal(expected.Length == 0, input.AsSpan().TrySplit(separator, Array.Empty<StringRange>(), out _, StringComparison.Ordinal, options));

            for (int j = 0; j < expected.Length; j++)
            {
                Assert.Equal(expected[j], input.Substring(actual[j].Index, actual[j].Count));
            }
        }
    }
}

#endif
