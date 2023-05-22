// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpStandardResilienceOptionsTests
{
    private readonly HttpStandardResilienceOptions _defaultInstance;

    public HttpStandardResilienceOptionsTests()
    {
        _defaultInstance = new HttpStandardResilienceOptions();
    }

    [Fact]
    public void TimeoutSettings_Ok()
    {
        Assert.True(_defaultInstance.AttemptTimeoutOptions.TimeoutInterval < _defaultInstance.TotalRequestTimeoutOptions.TimeoutInterval);
    }

    [Fact]
    public void PropertiesNotNull()
    {
        Assert.NotNull(_defaultInstance.RetryOptions);
        Assert.NotNull(_defaultInstance.AttemptTimeoutOptions);
        Assert.NotNull(_defaultInstance.TotalRequestTimeoutOptions);
        Assert.NotNull(_defaultInstance.CircuitBreakerOptions);
        Assert.NotNull(_defaultInstance.BulkheadOptions);
    }
}
