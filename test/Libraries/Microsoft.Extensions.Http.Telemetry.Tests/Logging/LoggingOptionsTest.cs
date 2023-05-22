// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class LoggingOptionsTest
{
    private readonly LoggingOptions _sut;

    public LoggingOptionsTest()
    {
        _sut = new LoggingOptions();
    }

    [Fact]
    public void CanConstruct_Class()
    {
        var sut = new LoggingOptions();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void CanSetAndGet_BodySizeLimit()
    {
        const int TestSizeLimit = 1;

        _sut.BodySizeLimit = TestSizeLimit;

        _sut.BodySizeLimit.Should().Be(TestSizeLimit);
    }

    [Fact]
    public void CanSetAndGet_BodyReadTimeout()
    {
        var testTimeout = TimeSpan.FromMinutes(1);

        _sut.BodyReadTimeout = testTimeout;

        testTimeout.Should().Be(testTimeout);
    }

    [Fact]
    public void CanSetAndGet_RequestBodyContentTypes()
    {
        var testContentTypes = new HashSet<string> { "application/xml" };

        _sut.RequestBodyContentTypes = testContentTypes;

        _sut.RequestBodyContentTypes.Should().BeEquivalentTo(testContentTypes);
    }

    [Fact]
    public void CanSetAndGet_ResponseBodyContentTypes()
    {
        var testContentTypes = new HashSet<string> { "application/xml" };

        _sut.ResponseBodyContentTypes = testContentTypes;

        _sut.ResponseBodyContentTypes.Should().BeEquivalentTo(testContentTypes);
    }

    [Fact]
    public void CanSetAndGet_RequestHeaders()
    {
        var testHeaders = new Dictionary<string, DataClassification>
        {
            { "header 1", SimpleClassifications.PrivateData },
            { "header 2", SimpleClassifications.PrivateData }
        };

        _sut.RequestHeadersDataClasses = testHeaders;

        _sut.RequestHeadersDataClasses.Should().BeEquivalentTo(testHeaders);
    }

    [Fact]
    public void CanSetAndGet_ResponseHeaders()
    {
        var testHeaders = new Dictionary<string, DataClassification>
        {
            { "header 1", SimpleClassifications.PrivateData },
            { "header 2", SimpleClassifications.PrivateData }
        };

        _sut.ResponseHeadersDataClasses = testHeaders;

        _sut.ResponseHeadersDataClasses.Should().BeEquivalentTo(testHeaders);
    }

    [Theory]
    [CombinatorialData]
    public void CanSetAndGet_FormatRequestPath(
        [CombinatorialValues(OutgoingPathLoggingMode.Structured, OutgoingPathLoggingMode.Formatted)]
        OutgoingPathLoggingMode testValue)
    {
        _sut.RequestPathLoggingMode = testValue;

        _sut.RequestPathLoggingMode.Should().Be(testValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanSetAndGet_LogRequestStart(bool testValue)
    {
        _sut.LogRequestStart = testValue;

        _sut.LogRequestStart.Should().Be(testValue);
    }

    [Fact]
    public void CanAndAndGet_RouteTemplateParametersToRedact()
    {
        var paramsToRedacts = new Dictionary<string, DataClassification>
        {
            { "foo", SimpleClassifications.PrivateData },
            { "bar", SimpleClassifications.PrivateData },
        };

        _sut.RouteParameterDataClasses.Add("foo", SimpleClassifications.PrivateData);
        _sut.RouteParameterDataClasses.Add("bar", SimpleClassifications.PrivateData);

        _sut.RouteParameterDataClasses.Should().BeEquivalentTo(paramsToRedacts);
    }
}
