// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class RedactorAbstractionsExtensionsTest
{
    [Fact]
    public void Redaction_Extensions_Throws_ArgumentNullException_When_Redactor_Is_Null()
    {
        string s = null!;

        Assert.Throws<ArgumentNullException>(() => RedactionAbstractionsExtensions.AppendRedacted(null!, NullRedactor.Instance, s));
        Assert.Throws<ArgumentNullException>(() => RedactionAbstractionsExtensions.AppendRedacted(new StringBuilder(), null!, ""));
    }

    [Fact]
    [SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast", Justification = "Cast is required to call extension method.")]
    public void Redaction_Extensions_Return_Zero_On_Null_Input_Value()
    {
        Assert.Equal(string.Empty, NullRedactor.Instance.Redact((string?)null));
        Assert.Equal(0, NullRedactor.Instance.GetRedactedLength((string?)null));
        Assert.Equal(0, NullRedactor.Instance.Redact((string)null!, new char[0]));
    }

    [Fact]
    public void When_Passed_Null_Value_String_Builder_Extensions_Does_Not_Append_To_String_Builder()
    {
        var sb = new StringBuilder();
        var redactor = NullRedactor.Instance;

        sb.AppendRedacted(NullRedactor.Instance,
#if NETCOREAPP3_1_OR_GREATER
            null);
#else
    (string?)null);
#endif

        Assert.Equal(0, sb.Length);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void User_Can_Get_String_From_IRedactor_Using_Extension_Method_With_Different_Input_Length(int length)
    {
        var data = new string('*', length);

        Redactor r = NullRedactor.Instance;

        var redacted = r.Redact(data);

        Assert.Equal(data, redacted);
    }

    [Fact]
    public void Return_Quickly_When_User_Tries_To_Append_Empty_Span_Using_StringBuilder_Extensions()
    {
        var sb = new StringBuilder();

        sb.AppendRedacted(NullRedactor.Instance, string.Empty);

        Assert.Empty(sb.ToString());
    }

    [Fact]
    public void Get_Redacted_String_API_Returns_Equivalent_Output_As_Span_Overload()
    {
        var data = new string('3', 3);
        var r = NullRedactor.Instance;

        var lengthFromExtension = r.GetRedactedLength(data);
        var length = r.GetRedactedLength(data);

        Assert.Equal(lengthFromExtension, length);
    }

    [Fact]
    public void Redact_Extension_String_Span_Works_The_Same_Way_As_Native_Method()
    {
        var data = new string('3', 3);

        Span<char> extBuffer = stackalloc char[3];
        Span<char> buffer = stackalloc char[3];

        var r = NullRedactor.Instance;
        var extensionWritten = r.Redact(data, extBuffer);
        var written = r.Redact(data, buffer);

        Assert.Equal(extensionWritten, written);
        Assert.Equal(extBuffer.ToString(), buffer.ToString());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void User_Can_Use_String_Builder_Extensions_To_Append_Redacted_Strings(int length)
    {
        var data = new string('*', length);
        var data2 = new string('c', length);

        var sb = new StringBuilder();
        var r = NullRedactor.Instance;

        sb.AppendRedacted(r, data)
          .AppendRedacted(r, data2);

        var redactedData = sb.ToString();

        Assert.Contains(data, redactedData);
        Assert.Contains(data2, redactedData);
    }

#if NET6_0_OR_GREATER
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void SpanFormattable_Format_And_Redacts_Data(int inputSize)
    {
        var data = new string('&', inputSize);

        var spanFormattable = new FakeSpanFormattable(data);

        var r = NullRedactor.Instance;

        var redacted = r.Redact(spanFormattable, null, null);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void SpanFormattable_Format_And_Redacts_Data_With_Destination_Buffer(int inputSize)
    {
        var data = new string('^', inputSize);

        var spanFormattable = new FakeSpanFormattable(data);

        var r = NullRedactor.Instance;

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
    public void Formattable_Format_And_Redacts_Data()
    {
        var data = Guid.NewGuid().ToString();

        var formattable = new FakeFormattable(data);

        Redactor r = NullRedactor.Instance;

        var redacted = r.Redact(formattable, null, null);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Fact]
    public void Formattable_Format_And_Redacts_Data_With_Destination_Buffer()
    {
        var data = Guid.NewGuid().ToString();

        var spanFormattable = new FakeFormattable(data);

        var r = NullRedactor.Instance;

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
    public void Object_Format_And_Redacts_Data()
    {
        var data = Guid.NewGuid().ToString();

        var obj = new FakeObject(data);

        var r = NullRedactor.Instance;

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(obj);
        var redactedDirectly = r.Redact(data);

        Assert.Equal(redactedDirectly, redacted);
    }

    [Fact]
    public void Object_Format_And_Redacts_Data_With_Destination_Buffer()
    {
        var data = Guid.NewGuid().ToString();

        var obj = new FakeObject(data);

        var r = NullRedactor.Instance;

        var buffer = new char[data.Length];
        var bufferDirect = new char[data.Length];

        var redacted = r.Redact(obj, buffer);
        var redactedDirectly = r.Redact(data, bufferDirect);

        for (var i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(buffer[i], bufferDirect[i]);
        }
    }
}
