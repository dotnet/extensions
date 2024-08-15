// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Shared.StringSplit.Test;

public static class SplitExtensionsTests
{
    public static IEnumerable<object[]> SingleCharData => new List<object[]>
    {
        new object[] { string.Empty, StringSplitOptions.None },
        new object[] { "A", StringSplitOptions.None },
        new object[] { "AA", StringSplitOptions.None },
        new object[] { "/", StringSplitOptions.None },
        new object[] { "A/", StringSplitOptions.None },
        new object[] { "AA/", StringSplitOptions.None },
        new object[] { "/A", StringSplitOptions.None },
        new object[] { "/AA", StringSplitOptions.None },
        new object[] { "AA/B", StringSplitOptions.None },
        new object[] { "AA//", StringSplitOptions.None },
        new object[] { "AA//BB", StringSplitOptions.None },

        new object[] { string.Empty, StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A/", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA/", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA/B", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA//", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA//BB", StringSplitOptions.RemoveEmptyEntries },

#if NET5_0_OR_GREATER
        new object[] { string.Empty, StringSplitOptions.TrimEntries },
        new object[] { " ", StringSplitOptions.TrimEntries },
        new object[] { " A", StringSplitOptions.TrimEntries },
        new object[] { "AA", StringSplitOptions.TrimEntries },
        new object[] { "A A", StringSplitOptions.TrimEntries },
        new object[] { "/", StringSplitOptions.TrimEntries },
        new object[] { " /", StringSplitOptions.TrimEntries },
        new object[] { "A/", StringSplitOptions.TrimEntries },
        new object[] { "A /", StringSplitOptions.TrimEntries },
        new object[] { "AA/", StringSplitOptions.TrimEntries },
        new object[] { "/A", StringSplitOptions.TrimEntries },
        new object[] { " / A ", StringSplitOptions.TrimEntries },
        new object[] { "/AA", StringSplitOptions.TrimEntries },
        new object[] { "AA/B", StringSplitOptions.TrimEntries },
        new object[] { "AA//", StringSplitOptions.TrimEntries },
        new object[] { "AA//BB", StringSplitOptions.TrimEntries },
        new object[] { "   abcde   /fghijk    /lmn", StringSplitOptions.TrimEntries },
#endif
    };

    public static IEnumerable<object[]> MultiCharData => new List<object[]>
    {
        new object[] { string.Empty, StringSplitOptions.None },
        new object[] { "A", StringSplitOptions.None },
        new object[] { "AA", StringSplitOptions.None },
        new object[] { "/", StringSplitOptions.None },
        new object[] { "A\\", StringSplitOptions.None },
        new object[] { "AA/", StringSplitOptions.None },
        new object[] { "/A", StringSplitOptions.None },
        new object[] { "\\AA", StringSplitOptions.None },
        new object[] { "AA/B", StringSplitOptions.None },
        new object[] { "AA/\\", StringSplitOptions.None },
        new object[] { "AA//BB", StringSplitOptions.None },

        new object[] { string.Empty, StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A/", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA\\", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "/AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA/B", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA//", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA//BB", StringSplitOptions.RemoveEmptyEntries },

#if NET5_0_OR_GREATER
        new object[] { string.Empty, StringSplitOptions.TrimEntries },
        new object[] { " ", StringSplitOptions.TrimEntries },
        new object[] { " A", StringSplitOptions.TrimEntries },
        new object[] { "AA", StringSplitOptions.TrimEntries },
        new object[] { "A A", StringSplitOptions.TrimEntries },
        new object[] { "/", StringSplitOptions.TrimEntries },
        new object[] { " /", StringSplitOptions.TrimEntries },
        new object[] { "A/", StringSplitOptions.TrimEntries },
        new object[] { "A /", StringSplitOptions.TrimEntries },
        new object[] { "AA/", StringSplitOptions.TrimEntries },
        new object[] { "/A", StringSplitOptions.TrimEntries },
        new object[] { " / A ", StringSplitOptions.TrimEntries },
        new object[] { "/AA", StringSplitOptions.TrimEntries },
        new object[] { "AA/B", StringSplitOptions.TrimEntries },
        new object[] { "AA//", StringSplitOptions.TrimEntries },
        new object[] { "AA//BB", StringSplitOptions.TrimEntries },
        new object[] { "   abcde   //fghijk    //lmn", StringSplitOptions.TrimEntries },
#endif
    };

    public static IEnumerable<object[]> WhitespaceData => new List<object[]>
    {
        new object[] { string.Empty, StringSplitOptions.None },
        new object[] { "A", StringSplitOptions.None },
        new object[] { "AA", StringSplitOptions.None },
        new object[] { " ", StringSplitOptions.None },
        new object[] { "A ", StringSplitOptions.None },
        new object[] { "AA ", StringSplitOptions.None },
        new object[] { " A", StringSplitOptions.None },
        new object[] { " AA", StringSplitOptions.None },
        new object[] { "AA B", StringSplitOptions.None },
        new object[] { "AA  ", StringSplitOptions.None },
        new object[] { "AA  BB", StringSplitOptions.None },

        new object[] { string.Empty, StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { " ", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "A ", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA ", StringSplitOptions.RemoveEmptyEntries },
        new object[] { " A", StringSplitOptions.RemoveEmptyEntries },
        new object[] { " AA", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA B", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA  ", StringSplitOptions.RemoveEmptyEntries },
        new object[] { "AA  BB", StringSplitOptions.RemoveEmptyEntries },

#if NET5_0_OR_GREATER
        new object[] { string.Empty, StringSplitOptions.TrimEntries },
        new object[] { " ", StringSplitOptions.TrimEntries },
        new object[] { " A", StringSplitOptions.TrimEntries },
        new object[] { "AA", StringSplitOptions.TrimEntries },
        new object[] { "A A", StringSplitOptions.TrimEntries },
        new object[] { " ", StringSplitOptions.TrimEntries },
        new object[] { "  ", StringSplitOptions.TrimEntries },
        new object[] { "A ", StringSplitOptions.TrimEntries },
        new object[] { "A  ", StringSplitOptions.TrimEntries },
        new object[] { "AA ", StringSplitOptions.TrimEntries },
        new object[] { " A", StringSplitOptions.TrimEntries },
        new object[] { "   A ", StringSplitOptions.TrimEntries },
        new object[] { " AA", StringSplitOptions.TrimEntries },
        new object[] { "AA B", StringSplitOptions.TrimEntries },
        new object[] { "AA  ", StringSplitOptions.TrimEntries },
        new object[] { "AA  BB", StringSplitOptions.TrimEntries },
#endif
    };

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

#if NET5_0_OR_GREATER
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
#endif
    };

    [Theory]
    [MemberData(nameof(SingleCharData))]
    public static void SingleChar(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { '/' }, options);

        var actual = new StringRange[20];
        Assert.True(input.TrySplit('/', actual, out int numActuals, options));

        Assert.Equal(expected.Length, numActuals);
        Assert.Equal(expected.Length == 0, input.TrySplit('/', Array.Empty<StringRange>(), out _, options));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], input.Substring(actual[i].Index, actual[i].Count));
        }
    }

    [Theory]
    [MemberData(nameof(MultiCharData))]
    public static void MultiChar(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { '/', '\\' }, options);

        var actual = new StringRange[20];
        Assert.True(input.TrySplit(new[] { '/', '\\' }, actual, out int numActuals, options));

        Assert.Equal(expected.Length, numActuals);
        Assert.Equal(expected.Length == 0, input.TrySplit(new[] { '/', '\\' }, Array.Empty<StringRange>(), out _, options));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], input.Substring(actual[i].Index, actual[i].Count));
        }
    }

    [Theory]
    [MemberData(nameof(WhitespaceData))]
    public static void Whitespace(string input, StringSplitOptions options)
    {
        var expected = input.Split((string[]?)null, options);

        var actual = new StringRange[20];
        Assert.True(input.TrySplit(actual, out int numActuals, options));

        Assert.Equal(expected.Length, numActuals);
        Assert.Equal(expected.Length == 0, input.TrySplit(Array.Empty<StringRange>(), out _, options));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], input.Substring(actual[i].Index, actual[i].Count));
        }
    }

    [Theory]
    [MemberData(nameof(StringData))]
    public static void StringArray(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { "XX", "YY" }, options);

        var actual = new StringRange[20];
        Assert.True(input.TrySplit(new[] { "XX", "YY" }, actual, out int numActuals, StringComparison.Ordinal, options));

        Assert.Equal(expected.Length, numActuals);
        Assert.Equal(expected.Length == 0, input.TrySplit(new[] { "XX", "YY" }, Array.Empty<StringRange>(), out _, StringComparison.Ordinal, options));

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], input.Substring(actual[i].Index, actual[i].Count));
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    [Theory]
    [MemberData(nameof(StringData))]
    public static void Strings(string input, StringSplitOptions options)
    {
        for (int i = 0; i < 2; i++)
        {
            var separator = i == 0 ? "XX" : "YY";

            var expected = input.Split(separator, options);

            var actual = new StringRange[20];
            Assert.True(input.TrySplit(separator, actual, out int numActuals, StringComparison.Ordinal, options));

            Assert.Equal(expected.Length, numActuals);
            Assert.Equal(expected.Length == 0, input.TrySplit(separator, Array.Empty<StringRange>(), out _, StringComparison.Ordinal, options));

            for (int j = 0; j < expected.Length; j++)
            {
                Assert.Equal(expected[j], input.Substring(actual[j].Index, actual[j].Count));
            }
        }
    }
#endif

    [Theory]
    [MemberData(nameof(SingleCharData))]
    public static void VisitSingleChar(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { '/' }, options);

        var actual = new string[20];
        int numActuals = 0;
        input.VisitSplits('/', actual, (s, c, a) =>
        {
            a[c] = s.ToString();
            numActuals++;
        }, options);

        Assert.Equal(expected.Length, numActuals);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Theory]
    [MemberData(nameof(MultiCharData))]
    public static void VisitMultiChar(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { '/', '\\' }, options);

        var actual = new string[20];
        int numActuals = 0;
        input.VisitSplits(new[] { '/', '\\' }, actual, (s, c, a) =>
        {
            a[c] = s.ToString();
            numActuals++;
        }, options);

        Assert.Equal(expected.Length, numActuals);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Theory]
    [MemberData(nameof(WhitespaceData))]
    public static void VisitWhitespace(string input, StringSplitOptions options)
    {
        var expected = input.Split((string[]?)null, options);

        var actual = new string[20];
        int numActuals = 0;
        input.VisitSplits(actual, (s, c, a) =>
        {
            a[c] = s.ToString();
            numActuals++;
        }, options);

        Assert.Equal(expected.Length, numActuals);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Theory]
    [MemberData(nameof(StringData))]
    public static void VisitStringArray(string input, StringSplitOptions options)
    {
        var expected = input.Split(new[] { "XX", "YY" }, options);

        var actual = new string[20];
        int numActuals = 0;
        input.VisitSplits(new[] { "XX", "YY" }, actual, (s, c, a) =>
        {
            a[c] = s.ToString();
            numActuals++;
        }, StringComparison.Ordinal, options);

        Assert.Equal(expected.Length, numActuals);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    [Theory]
    [MemberData(nameof(StringData))]
    public static void VisitStrings(string input, StringSplitOptions options)
    {
        for (int i = 0; i < 2; i++)
        {
            var separator = i == 0 ? "XX" : "YY";

            var expected = input.Split(separator, options);

            var actual = new string[20];
            int numActuals = 0;
            input.VisitSplits(separator, actual, (s, c, a) =>
            {
                a[c] = s.ToString();
                numActuals++;
            }, StringComparison.Ordinal, options);

            Assert.Equal(expected.Length, numActuals);

            for (int j = 0; j < expected.Length; j++)
            {
                Assert.Equal(expected[j], actual[j]);
            }
        }
    }
#endif

    [Fact]
    public static void CheckOpts()
    {
        var ss = new StringRange[1];
        Assert.Throws<ArgumentException>(() => "ABC".TrySplit(new[] { '/', '\\' }, ss, out _, (StringSplitOptions)(-1)));
        Assert.Throws<ArgumentException>(() => "ABC".TrySplit(new[] { "XX", "YY" }, ss, out _, StringComparison.Ordinal, (StringSplitOptions)(-1)));
        Assert.Throws<ArgumentException>(() => "ABC".TrySplit('/', ss, out _, (StringSplitOptions)(-1)));
        Assert.Throws<ArgumentException>(() => "ABC".TrySplit(ss, out _, (StringSplitOptions)(-1)));
    }
}

#endif
