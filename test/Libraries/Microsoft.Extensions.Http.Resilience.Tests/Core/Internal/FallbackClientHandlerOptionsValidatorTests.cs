// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;

public class FallbackClientHandlerOptionsValidatorTests
{
    private readonly FallbackClientHandlerOptionsValidator _validator;

    public FallbackClientHandlerOptionsValidatorTests()
    {
        _validator = new FallbackClientHandlerOptionsValidator();
    }

    [Theory]
    [InlineData("https://fallback-uri.com/somepath")]
    [InlineData("https://fallback-uri.com/somepath/someotherpath")]
    [InlineData("https://fallback-uri.com?a")]
    [InlineData("https://fallback-uri.com/somepath?query=value")]
    [InlineData("https://fallback-uri.com?a=1&b=2&c=3")]
    public void Validate_InvalidUri_ShouldReturnFailResult(string input)
    {
        var uri = new Uri(input);
        var options = new FallbackClientHandlerOptions { BaseFallbackUri = uri };
        var result = _validator.Validate(string.Empty, options);

        Assert.True(result.Failed);
        Assert.Equal("Property BaseFallbackUri: must be a base uri, hence it may contain only the schema, host and port.", result.FailureMessage);
    }

    [Fact]
    public void Validate_NullUri_ShouldReturnFailResult()
    {
        var options = new FallbackClientHandlerOptions();
        var result = _validator.Validate(string.Empty, options);

        Assert.True(result.Failed);
        Assert.Contains("Property BaseFallbackUri: must be configured", result.FailureMessage);
    }

    [Fact]
    public void Validate_NullProperties_ShouldReturnFailedResult()
    {
        var options = new FallbackClientHandlerOptions
        {
            FallbackPolicyOptions = null!
        };

        var result = _validator.Validate(string.Empty, options);
        Assert.True(result.Failed);
        Assert.Contains(nameof(FallbackClientHandlerOptions.FallbackPolicyOptions), result.FailureMessage);

        options = new FallbackClientHandlerOptions
        {
            FallbackPolicyOptions = null!
        };

        result = _validator.Validate(string.Empty, options);
        Assert.True(result.Failed);
        Assert.Contains(nameof(FallbackClientHandlerOptions.FallbackPolicyOptions), result.FailureMessage);

        options = new FallbackClientHandlerOptions
        {
            FallbackPolicyOptions = null!
        };

        result = _validator.Validate(string.Empty, options);
        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("https://fallback-uri.com")]
    [InlineData("https://fallback-uri.com:99")]
    [InlineData("http://test")]
    public void Validate_ValidUri_ShouldReturnSuccessResult(string input)
    {
        var uri = new Uri(input);
        var options = new FallbackClientHandlerOptions { BaseFallbackUri = uri };
        var result = _validator.Validate(string.Empty, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RelativeValidUri_ShouldReturnFailure()
    {
        var uri = new Uri("/", UriKind.Relative);
        var options = new FallbackClientHandlerOptions { BaseFallbackUri = uri };

        var result = _validator.Validate(string.Empty, options);

        Assert.True(result.Failed);
        Assert.Equal("Property BaseFallbackUri: must be an absolute uri.", result.FailureMessage);
    }
}
