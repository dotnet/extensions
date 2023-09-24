// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public static class RedactionAbstractionsExtensionsTest
{
    [Fact]
    public static void Redaction_Extensions_Throws_ArgumentNullException_When_Redactor_Is_Null()
    {
        string s = null!;

        Assert.Throws<ArgumentNullException>(() => RedactionStringBuilderExtensions.AppendRedacted(null!, NullRedactor.Instance, s));
        Assert.Throws<ArgumentNullException>(() => RedactionStringBuilderExtensions.AppendRedacted(new StringBuilder(), null!, ""));
    }

    [Fact]
    public static void When_Passed_Null_Value_String_Builder_Extensions_Does_Not_Append_To_String_Builder()
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

    [Fact]
    public static void Return_Quickly_When_User_Tries_To_Append_Empty_Span_Using_StringBuilder_Extensions()
    {
        var sb = new StringBuilder();

        sb.AppendRedacted(NullRedactor.Instance, string.Empty);

        Assert.Empty(sb.ToString());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public static void User_Can_Use_String_Builder_Extensions_To_Append_Redacted_Strings(int length)
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
}
