// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Http.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class HttpHeadersRedactorTests
{
    [Theory]
    [ClassData(typeof(HttpHeadersTestData))]
    public void Redact_Works_Correctly(IEnumerable<string> input, string expected)
    {
        var redactorProvider = new FakeRedactorProvider(new FakeRedactorOptions { RedactionFormat = "Redacted:{0}" });
        var headersRedactor = new HttpHeadersRedactor(redactorProvider);

        var actual = headersRedactor.Redact(input, SimpleClassifications.PrivateData);

        actual.Should().Be(expected);
    }

    internal class HttpHeadersTestData : TheoryData<IEnumerable<string>, string>
    {
        public HttpHeadersTestData()
        {
            string longStr = new('z', 312);

            Add(new LinkedList<string>(new List<string> { "aaa", "bbb", "ccc" }), "Redacted:aaa,Redacted:bbb,Redacted:ccc");
            Add(new LinkedList<string>(new List<string> { "aaa", "bbb", null! }), "Redacted:aaa,Redacted:bbb,");
            Add(new LinkedList<string>(new List<string> { "aaa", null!, null! }), "Redacted:aaa,,");
            Add(new LinkedList<string>(new List<string> { null!, null!, null! }), ",,");
            Add(new LinkedList<string>(new List<string> { null! }), string.Empty);
            Add(new LinkedList<string>(new List<string> { "aaa" }), "Redacted:aaa");
            Add(new LinkedList<string>(new List<string>()), string.Empty);
            Add(new LinkedList<string>(new List<string> { longStr, "bbb", "ccc" }), $"Redacted:{longStr},Redacted:bbb,Redacted:ccc");

            Add(new[] { "aaa", "bbb", "ccc" }, "Redacted:aaa,Redacted:bbb,Redacted:ccc");
            Add(new[] { "aaa", "bbb", null! }, "Redacted:aaa,Redacted:bbb,");
            Add(new[] { "aaa", null!, null! }, "Redacted:aaa,,");
            Add(new[] { (string)null!, null!, null! }, ",,");
            Add(new[] { (string)null! }, string.Empty);
            Add(new[] { "aaa" }, "Redacted:aaa");
            Add(new string[] { }, string.Empty);
            Add(null!, TelemetryConstants.Unknown);
            Add(new[] { longStr, "bbb", "ccc" }, $"Redacted:{longStr},Redacted:bbb,Redacted:ccc");
        }
    }
}
