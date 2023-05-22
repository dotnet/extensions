// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Shared.Pools;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class LogRecordPooledObjectPolicyTest
{
    [Fact]
    public void LogRecordPooledObjectPolicy_ResetsLogRecord()
    {
        var pool = PoolFactory.CreatePool(new LogRecordPooledObjectPolicy());
        var testObject = new Fixture().Create<LogRecord>();
        testObject.RequestHeaders!.Add(new KeyValuePair<string, string>("key1", "value1"));
        testObject.ResponseHeaders!.Add(new KeyValuePair<string, string>("key2", "value2"));
        testObject.EnrichmentProperties!.Add("key3", "value3");

        var logRecord1 = pool.Get();
        logRecord1.Host = testObject.Host;
        logRecord1.Method = testObject.Method;
        logRecord1.Path = testObject.Path;
        logRecord1.Duration = testObject.Duration;
        logRecord1.StatusCode = testObject.StatusCode;
        logRecord1.RequestHeaders = testObject.RequestHeaders;
        logRecord1.ResponseHeaders = testObject.ResponseHeaders;
        logRecord1.RequestBody = testObject.RequestBody;
        logRecord1.ResponseBody = testObject.ResponseBody;
        logRecord1.EnrichmentProperties = testObject.EnrichmentProperties;
        pool.Return(logRecord1);

        var logRecord2 = pool.Get();
        logRecord2.Host.Should().Be(string.Empty);
        logRecord2.Method.Should().Be(default);
        logRecord2.Path.Should().Be(string.Empty);
        logRecord2.Duration.Should().Be(default);
        logRecord2.StatusCode.Should().BeNull();
        logRecord2.RequestHeaders.Should().BeNull();
        logRecord2.ResponseHeaders.Should().BeNull();
        logRecord2.RequestBody.Should().Be(string.Empty);
        logRecord2.ResponseBody.Should().Be(string.Empty);
        logRecord2.EnrichmentProperties.Should().BeNull();
    }
}
