﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public static class RedactorTest
{
    [Fact]
    [SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast", Justification = "Cast is required to call extension method.")]
    public static void Redaction_Extensions_Return_Zero_On_Null_Input_Value()
    {
        var r = new PassthroughRedactor();

        Assert.Equal(string.Empty, r.Redact((string?)null));
        Assert.Equal(string.Empty, r.Redact<object?>(null));
        Assert.Equal(string.Empty, r.Redact(string.Empty.AsSpan()));
        Assert.Equal(0, r.Redact<string?>(null, new char[0]));
        Assert.Equal(0, r.Redact<object?>(null, new char[0]));
        Assert.True(r.TryRedact<object?>(null, new char[0], out _, string.Empty.AsSpan()));
        Assert.Equal(0, r.GetRedactedLength((string?)null));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public static void User_Can_Get_String_From_IRedactor_Using_Extension_Method_With_Different_Input_Length(int length)
    {
        var data = new string('*', length);

        Redactor r = NullRedactor.Instance;

        var redacted = r.Redact(data);

        Assert.Equal(data, redacted);
    }

    [Fact]
    public static void Get_Redacted_String_API_Returns_Equivalent_Output_As_Span_Overload()
    {
        var data = new string('3', 3);
        var r = NullRedactor.Instance;

        var lengthFromExtension = r.GetRedactedLength(data);
        var length = r.GetRedactedLength(data);

        Assert.Equal(lengthFromExtension, length);
    }

    [Fact]
    public static void Redact_Extension_String_Span_Works_The_Same_Way_As_Native_Method()
    {
        var data = new string('3', 3);

        Span<char> extBuffer = stackalloc char[3];
        Span<char> buffer = stackalloc char[3];

        var r = new PassthroughRedactor();
        var extensionWritten = r.Redact(data, extBuffer);
        var written = r.Redact(data, buffer);

        Assert.Equal(extensionWritten, written);
        Assert.Equal(extBuffer.ToString(), buffer.ToString());
    }

#if NET8_0_OR_GREATER
    // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
    // and as such the API is available in .NET 8 onwards.
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public static void SpanFormattable_Format_And_Redacts_Data(int inputSize)
    {
        var data = new string('&', inputSize);

        var spanFormattable = new TestSpanFormattable(data);

        var r = new PassthroughRedactor();

        var redacted = r.Redact(spanFormattable, null, null);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public static void SpanFormattable_Format_And_Redacts_Data_With_Destination_Buffer(int inputSize)
    {
        var data = new string('^', inputSize);

        var spanFormattable = new TestSpanFormattable(data);

        var r = new PassthroughRedactor();

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(spanFormattable, buffer, null, null);
        var redactedDirectly = r.Redact(data, bufferDirect);

        for (var i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(buffer[i], bufferDirect[i]);
        }
    }
#endif

    [Fact]
    public static void Formattable_Format_And_Redacts_Data()
    {
        var data = Guid.NewGuid().ToString();

        var formattable = new TestFormattable(data);

        var r = new PassthroughRedactor();

        var redacted = r.Redact(formattable, null, null);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Fact]
    public static void Formattable_Format_And_Redacts_Data_With_Destination_Buffer()
    {
        var data = Guid.NewGuid().ToString();

        var spanFormattable = new TestFormattable(data);

        var r = new PassthroughRedactor();

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(spanFormattable, buffer, null, null);
        var redactedDirectly = r.Redact(data, bufferDirect);

        for (var i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(buffer[i], bufferDirect[i]);
        }
    }

    [Fact]
    public static void Object_Format_And_Redacts_Data()
    {
        var data = Guid.NewGuid().ToString();

        var obj = new TestObject(data);

        var r = new PassthroughRedactor();

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(obj);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Fact]
    public static void Object_Format_And_Redacts_Data_With_Destination_Buffer()
    {
        var data = Guid.NewGuid().ToString();

        var obj = new TestObject(data);

        var r = new PassthroughRedactor();

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(obj, buffer);
        var redactedDirectly = r.Redact(data, bufferDirect);

        for (var i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(buffer[i], bufferDirect[i]);
        }
    }

    [Theory]
    [InlineData(35, false)]
    [InlineData(36, true)]
    [InlineData(37, true)]
    public static void TryRedact_BufferSizes_FOrmattable(int bufferSize, bool success)
    {
        var data = Guid.NewGuid();
        var r = new PassthroughRedactor();
        var buffer = new char[bufferSize];

        Assert.Equal(success, r.TryRedact(data, buffer, out int charsWritten, string.Empty.AsSpan(), null));

        if (success)
        {
            Assert.Equal(data.ToString(), new string(buffer, 0, charsWritten));
        }
    }

    [Theory]
    [InlineData(35, false)]
    [InlineData(36, true)]
    [InlineData(37, true)]
    public static void TryRedact_BufferSizes_NonFormattable(int bufferSize, bool success)
    {
        var data = new NonFormatable();
        var r = new PassthroughRedactor();
        var buffer = new char[bufferSize];

        Assert.Equal(success, r.TryRedact(data, buffer, out int charsWritten, string.Empty.AsSpan(), null));

        if (success)
        {
            Assert.Equal(data.ToString(), new string(buffer, 0, charsWritten));
        }
    }

    [Theory]
    [InlineData(28, false)]
    [InlineData(29, true)]
    [InlineData(30, true)]
    public static void TryRedact_BufferSizes_CustomFormat(int bufferSize, bool success)
    {
        var data = new DateTime(1, 2, 3);
        var r = new PassthroughRedactor();
        var buffer = new char[bufferSize];

        Assert.Equal(success, r.TryRedact(data, buffer, out int charsWritten, "R".AsSpan(), null));

        if (success)
        {
            Assert.Equal(data.ToString("R"), new string(buffer, 0, charsWritten));
        }
    }

    private class NonFormatable
    {
        public override string ToString() => "123456789012345678901234567890123456";
    }

    private class PassthroughRedactor : Redactor
    {
        public override int GetRedactedLength(ReadOnlySpan<char> input) => input.Length;

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            source.CopyTo(destination);
            return source.Length;
        }
    }
}
