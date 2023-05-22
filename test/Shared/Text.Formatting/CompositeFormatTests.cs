// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;

#pragma warning disable S4056 // Overloads with a "CultureInfo" or an "IFormatProvider" parameter should be used

namespace Microsoft.Shared.Text.Formatting.Test;

public class CompositeFormatTests
{
    private static void CheckExpansion<T>(T arg)
    {
        var format = "{0,256} {1}";

        var expectedResult = string.Format(format, 3.14, arg);
        var cf = CompositeFormat.Parse(format);
        var actualResult1 = cf.Format(null, 3.14, arg);
        var actualResult3 = new StringBuilder().AppendFormat(cf, null, 3.14, arg).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithString<T>(string? expectedResult, string format, T arg)
    {
        var cf = CompositeFormat.Parse(format);
        var actualResult1 = cf.Format(null, arg);
        var actualResult3 = new StringBuilder().AppendFormat((IFormatProvider?)null, cf, arg).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithString<T0, T1>(string? expectedResult, string format, T0 arg0, T1 arg1)
    {
        var cf = CompositeFormat.Parse(format);
        var actualResult1 = cf.Format(null, arg0, arg1);
        var actualResult3 = new StringBuilder().AppendFormat(cf, null, arg0, arg1).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithString<T0, T1, T2>(string? expectedResult, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        var cf = CompositeFormat.Parse(format);
        var actualResult1 = cf.Format(null, arg0, arg1, arg2);
        var actualResult3 = new StringBuilder().AppendFormat(cf, null, arg0, arg1, arg2).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithString<T0, T1, T2>(string? expectedResult, string format, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        var cf = CompositeFormat.Parse(format);
        var actualResult1 = cf.Format(null, arg0, arg1, arg2, args);
        var actualResult3 = new StringBuilder().AppendFormat(cf, null, arg0, arg1, arg2, args).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithString(string? expectedResult, string format, params object?[]? args)
    {
        Assert.True(CompositeFormat.TryParse(format, out var cf, out var error));
        Assert.Null(error);

        var actualResult1 = cf.Format(null, args);
        var actualResult3 = new StringBuilder().AppendFormat(cf, null, args).ToString();

        Assert.Equal(expectedResult, actualResult1);
        Assert.Equal(expectedResult, actualResult3);
    }

    private static void CheckFormatWithSpan<T>(string? expectedResult, string format, T arg)
    {
        var cf = CompositeFormat.Parse(format);
        var s = new Span<char>(new char[Math.Max(65536, (format.Length * 2) + 128)]);
        Assert.True(cf.TryFormat(s, out int charsWritten, null, arg));
        var actualResult = s.Slice(0, charsWritten).ToString();

        Assert.Equal(expectedResult, actualResult);
    }

    private static void CheckFormatWithSpan<T0, T1>(string? expectedResult, string format, T0 arg0, T1 arg1)
    {
        var cf = CompositeFormat.Parse(format);
        var s = new Span<char>(new char[(format.Length * 2) + 128]);
        Assert.True(cf.TryFormat(s, out int charsWritten, null, arg0, arg1));
        var actualResult = s.Slice(0, charsWritten).ToString();

        Assert.Equal(expectedResult, actualResult);
    }

    private static void CheckFormatWithSpan<T0, T1, T2>(string? expectedResult, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        var cf = CompositeFormat.Parse(format);
        var s = new Span<char>(new char[(format.Length * 2) + 128]);
        Assert.True(cf.TryFormat(s, out int charsWritten, null, arg0, arg1, arg2));
        var actualResult = s.Slice(0, charsWritten).ToString();

        Assert.Equal(expectedResult, actualResult);
    }

    private static void CheckFormatWithSpan<T0, T1, T2>(string? expectedResult, string format, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        var cf = CompositeFormat.Parse(format);
        var s = new Span<char>(new char[(format.Length * 2) + 128]);
        Assert.True(cf.TryFormat(s, out int charsWritten, null, arg0, arg1, arg2, args));
        var actualResult = s.Slice(0, charsWritten).ToString();

        Assert.Equal(expectedResult, actualResult);
    }

    private static void CheckFormatWithSpan(string? expectedResult, string format, params object?[]? args)
    {
        var cf = CompositeFormat.Parse(format);
        var s = new Span<char>(new char[(format.Length * 2) + 128]);
        Assert.True(cf.TryFormat(s, out int charsWritten, null, args));
        var actualResult = s.Slice(0, charsWritten).ToString();

        Assert.Equal(expectedResult, actualResult);
    }

    private static void CheckFormat<T>(string format, T arg)
    {
        var expectedResult = string.Format(format, arg);
        CheckFormatWithString(expectedResult, format, arg);
        CheckFormatWithSpan(expectedResult, format, arg);
    }

    private static void CheckFormat<T0, T1>(string format, T0 arg0, T1 arg1)
    {
        var expectedResult = string.Format(format, arg0, arg1);
        CheckFormatWithString(expectedResult, format, arg0, arg1);
        CheckFormatWithSpan(expectedResult, format, arg0, arg1);
    }

    private static void CheckFormat<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    {
        var expectedResult = string.Format(format, arg0, arg1, arg2);
        CheckFormatWithString(expectedResult, format, arg0, arg1, arg2);
        CheckFormatWithSpan(expectedResult, format, arg0, arg1, arg2);
    }

    private static void CheckFormat<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2, params object?[]? args)
    {
        int argLen = 3 + args!.Length;
        var a = new object?[argLen];
        a[0] = arg0;
        a[1] = arg1;
        a[2] = arg2;
        for (int i = 3; i < a.Length; i++)
        {
            a[i] = args![i - 3];
        }

        var expectedResult = string.Format(format, a);
        CheckFormatWithString(expectedResult, format, arg0, arg1, arg2, args);
        CheckFormatWithSpan(expectedResult, format, arg0, arg1, arg2, args);
    }

    private static void CheckFormat(string format, params object?[] args)
    {
        var expectedResult = string.Format(format, args);
        CheckFormatWithString(expectedResult, format, args);
        CheckFormatWithSpan(expectedResult, format, args);
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    [InlineData("XX")]
    public void NoArgs(string format)
    {
        CheckFormat(format);
    }

    [Fact]
    public void NoArgsLarge()
    {
        CheckFormat(new StringBuilder().Append('X', 32767).ToString());
        CheckFormat(new StringBuilder().Append('X', 32768).ToString());
        CheckFormat(new StringBuilder().Append('X', 65535).ToString());
        CheckFormat(new StringBuilder().Append('X', 65536).ToString());
    }

    [Theory]
    [InlineData("{0}", 42)]
    [InlineData("X{0}", 42)]
    [InlineData("{0}Y", 42)]
    [InlineData("X{0}Y", 42)]
    [InlineData("XZ{0}ZY", 42)]
    [InlineData("{0,9}", 42)]
    [InlineData("{0,10}", 42)]
    [InlineData("{0,19}", 42)]
    [InlineData("{0,32767}", 1)]
    public void OneArg(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Fact]
    public void OneArgLarge()
    {
        CheckFormat(new StringBuilder().Append('X', 65535) + "{0}", 42);
        CheckFormat("{0}" + new StringBuilder().Append('X', 65535), 42);
        CheckFormat(new StringBuilder().Append('X', 65535) + "{0}" + new StringBuilder().Append('X', 65535), 42);
    }

    [Theory]
    [InlineData("{0} {1}", 42, 3.14)]
    [InlineData("X{0}{1}", 42, 3.14)]
    [InlineData("{0} {1}Y", 42, 3.14)]
    [InlineData("X{0}{1}Y", 42, 3.14)]
    [InlineData("XZ{0} {1}ZY", 42, 3.14)]
    [InlineData("{0} {1} {0}", 42, 3.14)]
    [InlineData("X{0}{1} {0}", 42, 3.14)]
    [InlineData("{0} {1}Y {0}", 42, 3.14)]
    [InlineData("X{0}{1}Y {0}", 42, 3.14)]
    [InlineData("XZ{0} {1}ZY {0}", 42, 3.14)]
    public void TwoArgs(string format, int arg0, double arg1)
    {
        CheckFormat(format, arg0, arg1);
    }

    [Theory]
    [InlineData("{0} {1} {2}", 42, 3.14, "XX")]
    [InlineData("X{0}{1}{2}", 42, 3.14, "XX")]
    [InlineData("{0} {1} {2}Y", 42, 3.14, "XX")]
    [InlineData("X{0}{1}{2}Y", 42, 3.14, "XX")]
    [InlineData("XZ{0} {1} {2}ZY", 42, 3.14, "XX")]
    public void ThreeArgs(string format, int arg0, double arg1, string arg2)
    {
        CheckFormat(format, arg0, arg1, arg2);
    }

    [Theory]
    [InlineData("{0} {1} {2} {3}", 42, 3.14, "XX", true)]
    [InlineData("X{0}{1}{2}{3}", 42, 3.14, "XX", true)]
    [InlineData("{0} {1} {2} {3}Y", 42, 3.14, "XX", false)]
    [InlineData("X{0}{1}{2}{3}Y", 42, 3.14, "XX", true)]
    [InlineData("XZ{0} {1} {2} {3}ZY", 42, 3.14, "XX", false)]
    public void FourArgs(string format, int arg0, double arg1, string arg2, bool arg3)
    {
        CheckFormat(format, arg0, arg1, arg2, arg3);
    }

    [Fact]
    public void ArgArray()
    {
        CheckFormat("{32767}", new object[32768]);
        CheckFormat("{10}", new object[11]);
        CheckFormat("{19}", new object[20]);

        CheckFormat("", Array.Empty<object>());
        CheckFormat("X", Array.Empty<object>());
        CheckFormat("XY", Array.Empty<object>());

        CheckFormat("{0}", new object[] { 42 });
        CheckFormat("X{0}", new object[] { 42 });
        CheckFormat("{0}Y", new object[] { 42 });
        CheckFormat("X{0}Y", new object[] { 42 });
        CheckFormat("XZ{0}ZY", new object[] { 42 });

        CheckFormat("{0} {1}", new object[] { 42, 3.14 });
        CheckFormat("X{0}{1}", new object[] { 42, 3.14 });
        CheckFormat("{0} {1}Y", new object[] { 42, 3.14 });
        CheckFormat("X{0}{1}Y", new object[] { 42, 3.14 });
        CheckFormat("XZ{0} {1}ZY", new object[] { 42, 3.14 });

        CheckFormat("{0} {1} {2}", new object[] { 42, 3.14, "XX" });
        CheckFormat("X{0}{1}{2}", new object[] { 42, 3.14, "XX" });
        CheckFormat("{0} {1} {2}Y", new object[] { 42, 3.14, "XX" });
        CheckFormat("X{0}{1}{2}Y", new object[] { 42, 3.14, "XX" });
        CheckFormat("XZ{0} {1} {2}ZY", new object[] { 42, 3.14, "XX" });

        CheckFormat("{0} {1} {2} {3}", new object[] { 42, 3.14, "XX", true });
        CheckFormat("X{0}{1}{2}{3}", new object[] { 42, 3.14, "XX", true });
        CheckFormat("{0} {1} {2} {3}Y", new object[] { 42, 3.14, "XX", false });
        CheckFormat("X{0}{1}{2}{3}Y", new object[] { 42, 3.14, "XX", true });
        CheckFormat("XZ{0} {1} {2} {3}ZY", new object[] { 42, 3.14, "XX", false });

        CheckFormat("XZ{0} {1} {2} {3}ZY", new object[] { "42", "3.14", "XX", "false" });

        CheckFormat("XZ{0} {1} {2} {9}ZY", new object[] { "42", "3.14", "XX", 0, 1, 2, 3, 4, 5, "false" });
    }

    [Theory]
    [InlineData("{/}")]
    [InlineData("{:}")]
    [InlineData("{0/}")]
    [InlineData("{0,/}")]
    [InlineData("{0,:}")]
    [InlineData("{0,0/}")]
    [InlineData("{32768}")]
    [InlineData("{0,32768}")]
    [InlineData("{")]
    [InlineData("X{")]
    [InlineData("}")]
    [InlineData("X}")]
    [InlineData("{X}")]
    [InlineData("{100000000000000000000,2}")]
    [InlineData("{0")]
    [InlineData("{0,")]
    [InlineData("{0,}")]
    [InlineData("{0,-")]
    [InlineData("{0,-}")]
    [InlineData("{0,0")]
    [InlineData("{0,0X")]
    [InlineData("{0,1000000000000000000}")]
    [InlineData("{0,0:")]
    [InlineData("{0,0:{")]
    [InlineData("{ 0,0}")]
    [InlineData("{0,0:{{")]
    [InlineData("{0,0:}}")]
    [InlineData("{0,0:{{X}}")]
    [InlineData("{0  ")]
    [InlineData("{0,  ")]
    [InlineData("{0  X")]
    [InlineData("{0,  {")]
    public void BadFormatString(string format)
    {
        var e = Assert.Throws<ArgumentException>(() => _ = CompositeFormat.Parse(format));
        Assert.NotEqual("", e.Message);
        Assert.Equal("format", e.ParamName);

        Assert.False(CompositeFormat.TryParse(format, out var _, out var err));
        Assert.False(string.IsNullOrWhiteSpace(err));
    }

    [Theory]
    [InlineData("{0, 0}", 42)]
    [InlineData("{0 ,0}", 42)]
    [InlineData("{0 }", 42)]
    [InlineData("{0,0 }", 42)]
    [InlineData("{0,0 :x}", 42)]
    [InlineData("{0,0: X}", 42)]
    [InlineData("{0,0:X }", 42)]
    public void CheckWhitespace(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Fact]
    public void CheckWidth()
    {
        for (int width = -10; width < 10; width++)
        {
            CheckFormat($"{{0,{width}}}", "X");
            CheckFormat($"{{0,{width}}}", "XY");
            CheckFormat($"{{0,{width}}}", "XYZ");
        }
    }

    [Theory]
    [InlineData("{{{0}", 42)]
    [InlineData("{{{0}}}", 42)]
    public void CheckEscapes(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Fact]
    public void BadNumArgs()
    {
        var cf = CompositeFormat.Parse("{0} {2}");

        Assert.Throws<ArgumentException>(() => cf.Format(null, 1));
        Assert.Throws<ArgumentException>(() => cf.Format(null, 1, 2));
        Assert.Equal("1 3", cf.Format(null, 1, 2, 3));
        Assert.Equal("1 3", cf.Format(null, 1, 2, 3, 4));
    }

    private struct Custom1 : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return "IFormattable Output";
        }
    }

#if NET6_0_OR_GREATER
    private struct Custom2 : ISpanFormattable
    {
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (destination.Length < 16)
            {
                charsWritten = 0;
                return false;
            }

            "TryFormat Output".AsSpan().CopyTo(destination);
            charsWritten = 16;
            return true;
        }

        // NOTE: If/when this test is built as part of the .NET release,
        //       this should be removed. It's needed because String.Format
        //       doesn't recognize my hacky ISpanFormattable.
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return "TryFormat Output";
        }
    }
#endif

    [Fact]
    public void ArgTypes()
    {
        CheckFormat("{0}", (sbyte)42);
        CheckFormat("{0}", (short)42);
        CheckFormat("{0}", 42);
        CheckFormat("{0}", 42L);
        CheckFormat("{0}", (byte)42);
        CheckFormat("{0}", (ushort)42);
        CheckFormat("{0}", 42U);
        CheckFormat("{0}", 42UL);
        CheckFormat("{0}", 42.0F);
        CheckFormat("{0}", 42.0);
        CheckFormat("{0}", 'x');
        CheckFormat("{0}", new DateTime(2000, 1, 1));
        CheckFormat("{0}", new TimeSpan(42));
        CheckFormat("{0}", true);
        CheckFormat("{0}", new decimal(42.0));
        CheckFormat("{0}", new Guid(new byte[] { 42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
        CheckFormat("{0}", "XYZ");
        CheckFormat("{0}", new object?[] { null });
        CheckFormat("{0}", default(Custom1));

#if NET6_0_OR_GREATER
        CheckFormat("{0}", default(Custom2));
#endif
    }

    [Fact]
    public void BufferExpansion()
    {
        CheckExpansion((sbyte)42);
        CheckExpansion((short)42);
        CheckExpansion(42);
        CheckExpansion(42L);
        CheckExpansion((byte)42);
        CheckExpansion((ushort)42);
        CheckExpansion(42U);
        CheckExpansion(42UL);
        CheckExpansion(42.0F);
        CheckExpansion(42.0);
        CheckExpansion('X');
        CheckExpansion(new DateTime(2000, 1, 1));
        CheckExpansion(new TimeSpan(42));
        CheckExpansion(true);
        CheckExpansion(new decimal(42.0));
        CheckExpansion(new Guid(new byte[] { 42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
        CheckExpansion("XYZ");
        CheckExpansion(new object?[] { null });
        CheckExpansion(default(Custom1));

#if NET6_0_OR_GREATER
        CheckExpansion(default(Custom2));
#endif
    }

    [Theory]
    [InlineData("{0:d}", 0)]
    [InlineData("{0:d}", 5)]
    [InlineData("{0:d}", 10)]
    [InlineData("{0:d}", 15)]
    [InlineData("{0:d}", 100)]
    [InlineData("{0:d}", 123)]
    [InlineData("{0:d}", 1024)]
    [InlineData("{0:d}", -5)]
    [InlineData("{0:d}", -10)]
    [InlineData("{0:d}", -15)]
    [InlineData("{0:d}", -100)]
    [InlineData("{0:d}", -123)]
    [InlineData("{0:d}", -1024)]
    [InlineData("{0:d1}", 0)]
    [InlineData("{0:d1}", 5)]
    [InlineData("{0:d1}", 10)]
    [InlineData("{0:d1}", 15)]
    [InlineData("{0:d1}", 100)]
    [InlineData("{0:d1}", 123)]
    [InlineData("{0:d1}", 1024)]
    [InlineData("{0:d1}", -5)]
    [InlineData("{0:d1}", -10)]
    [InlineData("{0:d1}", -15)]
    [InlineData("{0:d1}", -100)]
    [InlineData("{0:d1}", -123)]
    [InlineData("{0:d1}", -1024)]
    [InlineData("{0:d2}", 0)]
    [InlineData("{0:d2}", 5)]
    [InlineData("{0:d2}", 10)]
    [InlineData("{0:d2}", 15)]
    [InlineData("{0:d2}", 100)]
    [InlineData("{0:d2}", 123)]
    [InlineData("{0:d2}", 1024)]
    [InlineData("{0:d2}", -5)]
    [InlineData("{0:d2}", -10)]
    [InlineData("{0:d2}", -15)]
    [InlineData("{0:d2}", -100)]
    [InlineData("{0:d2}", -123)]
    [InlineData("{0:d2}", -1024)]
    [InlineData("{0:d3}", 0)]
    [InlineData("{0:d3}", 5)]
    [InlineData("{0:d3}", 10)]
    [InlineData("{0:d3}", 15)]
    [InlineData("{0:d3}", 100)]
    [InlineData("{0:d3}", 123)]
    [InlineData("{0:d3}", 1024)]
    [InlineData("{0:d3}", -5)]
    [InlineData("{0:d3}", -10)]
    [InlineData("{0:d3}", -15)]
    [InlineData("{0:d3}", -100)]
    [InlineData("{0:d3}", -123)]
    [InlineData("{0:d3}", -1024)]
    [InlineData("{0:d4}", 0)]
    [InlineData("{0:d4}", 5)]
    [InlineData("{0:d4}", 10)]
    [InlineData("{0:d4}", 15)]
    [InlineData("{0:d4}", 100)]
    [InlineData("{0:d4}", 123)]
    [InlineData("{0:d4}", 1024)]
    [InlineData("{0:d4}", -5)]
    [InlineData("{0:d4}", -10)]
    [InlineData("{0:d4}", -15)]
    [InlineData("{0:d4}", -100)]
    [InlineData("{0:d4}", -123)]
    [InlineData("{0:d4}", -1024)]
    public void TestStringFormatD(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Theory]
    [InlineData("{0,1:d}", 0)]
    [InlineData("{0,1:d}", 5)]
    [InlineData("{0,1:d}", 10)]
    [InlineData("{0,1:d}", 15)]
    [InlineData("{0,1:d}", 100)]
    [InlineData("{0,1:d}", 123)]
    [InlineData("{0,1:d}", 1024)]
    [InlineData("{0,1:d}", -5)]
    [InlineData("{0,1:d}", -10)]
    [InlineData("{0,1:d}", -15)]
    [InlineData("{0,1:d}", -100)]
    [InlineData("{0,1:d}", -123)]
    [InlineData("{0,1:d}", -1024)]
    [InlineData("{0,1:d1}", 0)]
    [InlineData("{0,1:d1}", 5)]
    [InlineData("{0,1:d1}", 10)]
    [InlineData("{0,1:d1}", 15)]
    [InlineData("{0,1:d1}", 100)]
    [InlineData("{0,1:d1}", 123)]
    [InlineData("{0,1:d1}", 1024)]
    [InlineData("{0,1:d1}", -5)]
    [InlineData("{0,1:d1}", -10)]
    [InlineData("{0,1:d1}", -15)]
    [InlineData("{0,1:d1}", -100)]
    [InlineData("{0,1:d1}", -123)]
    [InlineData("{0,1:d1}", -1024)]
    [InlineData("{0,1:d2}", 0)]
    [InlineData("{0,1:d2}", 5)]
    [InlineData("{0,1:d2}", 10)]
    [InlineData("{0,1:d2}", 15)]
    [InlineData("{0,1:d2}", 100)]
    [InlineData("{0,1:d2}", 123)]
    [InlineData("{0,1:d2}", 1024)]
    [InlineData("{0,1:d2}", -5)]
    [InlineData("{0,1:d2}", -10)]
    [InlineData("{0,1:d2}", -15)]
    [InlineData("{0,1:d2}", -100)]
    [InlineData("{0,1:d2}", -123)]
    [InlineData("{0,1:d2}", -1024)]
    [InlineData("{0,1:d3}", 0)]
    [InlineData("{0,1:d3}", 5)]
    [InlineData("{0,1:d3}", 10)]
    [InlineData("{0,1:d3}", 15)]
    [InlineData("{0,1:d3}", 100)]
    [InlineData("{0,1:d3}", 123)]
    [InlineData("{0,1:d3}", 1024)]
    [InlineData("{0,1:d3}", -5)]
    [InlineData("{0,1:d3}", -10)]
    [InlineData("{0,1:d3}", -15)]
    [InlineData("{0,1:d3}", -100)]
    [InlineData("{0,1:d3}", -123)]
    [InlineData("{0,1:d3}", -1024)]
    [InlineData("{0,1:d4}", 0)]
    [InlineData("{0,1:d4}", 5)]
    [InlineData("{0,1:d4}", 10)]
    [InlineData("{0,1:d4}", 15)]
    [InlineData("{0,1:d4}", 100)]
    [InlineData("{0,1:d4}", 123)]
    [InlineData("{0,1:d4}", 1024)]
    [InlineData("{0,1:d4}", -5)]
    [InlineData("{0,1:d4}", -10)]
    [InlineData("{0,1:d4}", -15)]
    [InlineData("{0,1:d4}", -100)]
    [InlineData("{0,1:d4}", -123)]
    [InlineData("{0,1:d4}", -1024)]
    public void TestStringFormatD1(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Theory]
    [InlineData("{0,2:d}", 0)]
    [InlineData("{0,2:d}", 5)]
    [InlineData("{0,2:d}", 10)]
    [InlineData("{0,2:d}", 15)]
    [InlineData("{0,2:d}", 100)]
    [InlineData("{0,2:d}", 123)]
    [InlineData("{0,2:d}", 1024)]
    [InlineData("{0,2:d}", -5)]
    [InlineData("{0,2:d}", -10)]
    [InlineData("{0,2:d}", -15)]
    [InlineData("{0,2:d}", -100)]
    [InlineData("{0,2:d}", -123)]
    [InlineData("{0,2:d}", -1024)]
    [InlineData("{0,2:d1}", 0)]
    [InlineData("{0,2:d1}", 5)]
    [InlineData("{0,2:d1}", 10)]
    [InlineData("{0,2:d1}", 15)]
    [InlineData("{0,2:d1}", 100)]
    [InlineData("{0,2:d1}", 123)]
    [InlineData("{0,2:d1}", 1024)]
    [InlineData("{0,2:d1}", -5)]
    [InlineData("{0,2:d1}", -10)]
    [InlineData("{0,2:d1}", -15)]
    [InlineData("{0,2:d1}", -100)]
    [InlineData("{0,2:d1}", -123)]
    [InlineData("{0,2:d1}", -1024)]
    [InlineData("{0,2:d2}", 0)]
    [InlineData("{0,2:d2}", 5)]
    [InlineData("{0,2:d2}", 10)]
    [InlineData("{0,2:d2}", 15)]
    [InlineData("{0,2:d2}", 100)]
    [InlineData("{0,2:d2}", 123)]
    [InlineData("{0,2:d2}", 1024)]
    [InlineData("{0,2:d2}", -5)]
    [InlineData("{0,2:d2}", -10)]
    [InlineData("{0,2:d2}", -15)]
    [InlineData("{0,2:d2}", -100)]
    [InlineData("{0,2:d2}", -123)]
    [InlineData("{0,2:d2}", -1024)]
    [InlineData("{0,2:d3}", 0)]
    [InlineData("{0,2:d3}", 5)]
    [InlineData("{0,2:d3}", 10)]
    [InlineData("{0,2:d3}", 15)]
    [InlineData("{0,2:d3}", 100)]
    [InlineData("{0,2:d3}", 123)]
    [InlineData("{0,2:d3}", 1024)]
    [InlineData("{0,2:d3}", -5)]
    [InlineData("{0,2:d3}", -10)]
    [InlineData("{0,2:d3}", -15)]
    [InlineData("{0,2:d3}", -100)]
    [InlineData("{0,2:d3}", -123)]
    [InlineData("{0,2:d3}", -1024)]
    [InlineData("{0,2:d4}", 0)]
    [InlineData("{0,2:d4}", 5)]
    [InlineData("{0,2:d4}", 10)]
    [InlineData("{0,2:d4}", 15)]
    [InlineData("{0,2:d4}", 100)]
    [InlineData("{0,2:d4}", 123)]
    [InlineData("{0,2:d4}", 1024)]
    [InlineData("{0,2:d4}", -5)]
    [InlineData("{0,2:d4}", -10)]
    [InlineData("{0,2:d4}", -15)]
    [InlineData("{0,2:d4}", -100)]
    [InlineData("{0,2:d4}", -123)]
    [InlineData("{0,2:d4}", -1024)]
    public void TestStringFormatD2(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Theory]
    [InlineData("{0,3:d}", 0)]
    [InlineData("{0,3:d}", 5)]
    [InlineData("{0,3:d}", 10)]
    [InlineData("{0,3:d}", 15)]
    [InlineData("{0,3:d}", 100)]
    [InlineData("{0,3:d}", 123)]
    [InlineData("{0,3:d}", 1024)]
    [InlineData("{0,3:d}", -5)]
    [InlineData("{0,3:d}", -10)]
    [InlineData("{0,3:d}", -15)]
    [InlineData("{0,3:d}", -100)]
    [InlineData("{0,3:d}", -123)]
    [InlineData("{0,3:d}", -1024)]
    [InlineData("{0,3:d1}", 0)]
    [InlineData("{0,3:d1}", 5)]
    [InlineData("{0,3:d1}", 10)]
    [InlineData("{0,3:d1}", 15)]
    [InlineData("{0,3:d1}", 100)]
    [InlineData("{0,3:d1}", 123)]
    [InlineData("{0,3:d1}", 1024)]
    [InlineData("{0,3:d1}", -5)]
    [InlineData("{0,3:d1}", -10)]
    [InlineData("{0,3:d1}", -15)]
    [InlineData("{0,3:d1}", -100)]
    [InlineData("{0,3:d1}", -123)]
    [InlineData("{0,3:d1}", -1024)]
    [InlineData("{0,3:d2}", 0)]
    [InlineData("{0,3:d2}", 5)]
    [InlineData("{0,3:d2}", 10)]
    [InlineData("{0,3:d2}", 15)]
    [InlineData("{0,3:d2}", 100)]
    [InlineData("{0,3:d2}", 123)]
    [InlineData("{0,3:d2}", 1024)]
    [InlineData("{0,3:d2}", -5)]
    [InlineData("{0,3:d2}", -10)]
    [InlineData("{0,3:d2}", -15)]
    [InlineData("{0,3:d2}", -100)]
    [InlineData("{0,3:d2}", -123)]
    [InlineData("{0,3:d2}", -1024)]
    [InlineData("{0,3:d3}", 0)]
    [InlineData("{0,3:d3}", 5)]
    [InlineData("{0,3:d3}", 10)]
    [InlineData("{0,3:d3}", 15)]
    [InlineData("{0,3:d3}", 100)]
    [InlineData("{0,3:d3}", 123)]
    [InlineData("{0,3:d3}", 1024)]
    [InlineData("{0,3:d3}", -5)]
    [InlineData("{0,3:d3}", -10)]
    [InlineData("{0,3:d3}", -15)]
    [InlineData("{0,3:d3}", -100)]
    [InlineData("{0,3:d3}", -123)]
    [InlineData("{0,3:d3}", -1024)]
    [InlineData("{0,3:d4}", 0)]
    [InlineData("{0,3:d4}", 5)]
    [InlineData("{0,3:d4}", 10)]
    [InlineData("{0,3:d4}", 15)]
    [InlineData("{0,3:d4}", 100)]
    [InlineData("{0,3:d4}", 123)]
    [InlineData("{0,3:d4}", 1024)]
    [InlineData("{0,3:d4}", -5)]
    [InlineData("{0,3:d4}", -10)]
    [InlineData("{0,3:d4}", -15)]
    [InlineData("{0,3:d4}", -100)]
    [InlineData("{0,3:d4}", -123)]
    [InlineData("{0,3:d4}", -1024)]
    public void TestStringFormatD3(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Theory]
    [InlineData("{0,4:d}", 0)]
    [InlineData("{0,4:d}", 5)]
    [InlineData("{0,4:d}", 10)]
    [InlineData("{0,4:d}", 15)]
    [InlineData("{0,4:d}", 100)]
    [InlineData("{0,4:d}", 123)]
    [InlineData("{0,4:d}", 1024)]
    [InlineData("{0,4:d}", -5)]
    [InlineData("{0,4:d}", -10)]
    [InlineData("{0,4:d}", -15)]
    [InlineData("{0,4:d}", -100)]
    [InlineData("{0,4:d}", -123)]
    [InlineData("{0,4:d}", -1024)]
    [InlineData("{0,4:d1}", 0)]
    [InlineData("{0,4:d1}", 5)]
    [InlineData("{0,4:d1}", 10)]
    [InlineData("{0,4:d1}", 15)]
    [InlineData("{0,4:d1}", 100)]
    [InlineData("{0,4:d1}", 123)]
    [InlineData("{0,4:d1}", 1024)]
    [InlineData("{0,4:d1}", -5)]
    [InlineData("{0,4:d1}", -10)]
    [InlineData("{0,4:d1}", -15)]
    [InlineData("{0,4:d1}", -100)]
    [InlineData("{0,4:d1}", -123)]
    [InlineData("{0,4:d1}", -1024)]
    [InlineData("{0,4:d2}", 0)]
    [InlineData("{0,4:d2}", 5)]
    [InlineData("{0,4:d2}", 10)]
    [InlineData("{0,4:d2}", 15)]
    [InlineData("{0,4:d2}", 100)]
    [InlineData("{0,4:d2}", 123)]
    [InlineData("{0,4:d2}", 1024)]
    [InlineData("{0,4:d2}", -5)]
    [InlineData("{0,4:d2}", -10)]
    [InlineData("{0,4:d2}", -15)]
    [InlineData("{0,4:d2}", -100)]
    [InlineData("{0,4:d2}", -123)]
    [InlineData("{0,4:d2}", -1024)]
    [InlineData("{0,4:d3}", 0)]
    [InlineData("{0,4:d3}", 5)]
    [InlineData("{0,4:d3}", 10)]
    [InlineData("{0,4:d3}", 15)]
    [InlineData("{0,4:d3}", 100)]
    [InlineData("{0,4:d3}", 123)]
    [InlineData("{0,4:d3}", 1024)]
    [InlineData("{0,4:d3}", -5)]
    [InlineData("{0,4:d3}", -10)]
    [InlineData("{0,4:d3}", -15)]
    [InlineData("{0,4:d3}", -100)]
    [InlineData("{0,4:d3}", -123)]
    [InlineData("{0,4:d3}", -1024)]
    [InlineData("{0,4:d4}", 0)]
    [InlineData("{0,4:d4}", 5)]
    [InlineData("{0,4:d4}", 10)]
    [InlineData("{0,4:d4}", 15)]
    [InlineData("{0,4:d4}", 100)]
    [InlineData("{0,4:d4}", 123)]
    [InlineData("{0,4:d4}", 1024)]
    [InlineData("{0,4:d4}", -5)]
    [InlineData("{0,4:d4}", -10)]
    [InlineData("{0,4:d4}", -15)]
    [InlineData("{0,4:d4}", -100)]
    [InlineData("{0,4:d4}", -123)]
    [InlineData("{0,4:d4}", -1024)]
    public void TestStringFormatD4(string format, int arg)
    {
        CheckFormat(format, arg);
    }

    [Theory]
    [InlineData("{0}", 1)]
    [InlineData("{0}{1}", 2)]
    [InlineData("{0,3}", 1)]
    [InlineData("{0,3:d}", 1)]
    [InlineData("{0,3:d}{0}", 1)]
    [InlineData("{0,3:d}{1}", 2)]
    [InlineData("{0,3:d}{9}", 10)]
    public void TestNumArgsNeeded(string format, int argsExpected)
    {
        var cf = CompositeFormat.Parse(format);
        Assert.Equal(argsExpected, cf.NumArgumentsNeeded);
    }

    [Fact]
    public void OverflowNoArgs()
    {
        var cf = CompositeFormat.Parse("0123");
        Assert.False(cf.TryFormat(new char[3], out var charsWritten, null, null));
        Assert.Equal(0, charsWritten);

        Assert.True(cf.TryFormat(new char[4], out charsWritten, null, null));
        Assert.Equal(4, charsWritten);
    }

    [Fact]
    public void TemplateFormat()
    {
        var cf = CompositeFormat.Parse("{one} {_two} {t_hree} {one} {f4}".AsSpan(), out var templates);
        Assert.Equal(4, templates.Count);
        Assert.Equal("one", templates[0]);
        Assert.Equal("_two", templates[1]);
        Assert.Equal("t_hree", templates[2]);
        Assert.Equal("f4", templates[3]);
        Assert.Equal(4, cf.NumArgumentsNeeded);
        Assert.Equal("ONE TWO THREE ONE FOUR", cf.Format(null, "ONE", "TWO", "THREE", "FOUR"));

        var ex = Assert.Throws<ArgumentException>(() => CompositeFormat.Parse("{".AsSpan(), out templates));
        Assert.Contains("format string", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => CompositeFormat.Parse("{@".AsSpan(), out templates));
        Assert.Contains("format string", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => CompositeFormat.Parse("{a".AsSpan(), out templates));
        Assert.Contains("format string", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => CompositeFormat.Parse("{_".AsSpan(), out templates));
        Assert.Contains("format string", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => CompositeFormat.Parse("{0}".AsSpan(), out templates));
        Assert.Contains("format string", ex.Message);
    }
}
