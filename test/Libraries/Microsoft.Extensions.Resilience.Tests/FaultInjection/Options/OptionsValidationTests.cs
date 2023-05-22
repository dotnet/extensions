// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class OptionsValidationTests
{
    private readonly IConfiguration _configurationWithPolicyOptions;

    public OptionsValidationTests()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("configs/appsettings.json");
        _configurationWithPolicyOptions = builder.Build();
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_HttpResponseInjectionPolicyOptions_InjectionRateOutOfRange_ShouldReturnFailure()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTest1")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal("The field FaultInjectionRate must be between 0 and 1.", string.Join("", exception!.Failures));
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_ExceptionPolicyOptions_InjectionRateOutOfRange_ShouldReturnFailure()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTest2")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal("The field FaultInjectionRate must be between 0 and 1.", string.Join("", exception!.Failures));
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_LatencyPolicyOptions_InjectionRateOutOfRange_ShouldReturnFailure()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTest3")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal("The field FaultInjectionRate must be between 0 and 1.", string.Join("", exception!.Failures));
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_LatencyPolicyOptions_LatencyOutOfRange_ShouldReturnFailure()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTest4")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal("The field Latency must be <= to 00:10:00.", string.Join("", exception!.Failures));
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_HttpResponseInjectionPolicyOptions_StatusCodeOutOfRange_ShouldReturnFailure()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTest5")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal("The field StatusCode is invalid.", string.Join("", exception!.Failures));
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_NoOptionsGroupField_ShouldBeAllowed()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsTestNoOptionsGroup")
            .Bind(options);

        Validate(options);
        Assert.Empty(options.ChaosPolicyOptionsGroups);
    }

    [Fact]
    public void ChaosPolicyOptionsValidator_MultipleErrors()
    {
        var options = new FaultInjectionOptions();
        _configurationWithPolicyOptions
            .GetSection("ChaosPolicyOptionsGroupsNegativeTestMultipleErrors")
            .Bind(options);

        var exception = Assert.Throws<OptionsValidationException>(() => Validate(options));
        Assert.Equal(
            "The field Latency must be <= to 00:10:00.; " +
            "The field FaultInjectionRate must be between 0 and 1.", string.Join("; ", exception!.Failures));
    }

    [Fact]
    public void GenerateFailureMessages_NullErrorMessages_ShouldReturnUnknownError()
    {
        var results = new List<ValidationResult>
        {
            new ValidationResult(null!)
        };
        var messages = FaultInjectionOptionsValidator.GenerateFailureMessages(results);

        Assert.Equal("Unknown Error", string.Join("", messages));
    }

    [Fact]
    public void GenerateFailureMessages_EmptyValidationResults_ShouldReturnEmptyCollection()
    {
        var messages = FaultInjectionOptionsValidator.GenerateFailureMessages(new List<ValidationResult>());

        Assert.Empty(messages);
    }

    private static void Validate(FaultInjectionOptions options)
    {
        var optionsValidator = new FaultInjectionOptionsValidator();
        optionsValidator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);
    }
}
