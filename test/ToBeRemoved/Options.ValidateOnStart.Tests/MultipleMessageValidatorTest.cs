// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using System.Linq;
#endif
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation.Test.Helpers;
using Xunit;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Options.Validation.Test;

public class MultipleMessageValidatorTest
{
#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void Validator_That_Inherits_From_BaseValidator_Outputs_List_Of_Failures()
    {
        var validator = new ThreeFailuresMultiErrorValidator();

        var validationResult = validator.Validate(string.Empty, new NestedOptions());
        var failures = validationResult.Failures!.ToArray();

        Assert.True(validationResult.Failed);
        Assert.Equal(3, failures.Length);

        Assert.Contains(ThreeFailuresMultiErrorValidator.FirstErrorMessage, failures[0]);
        Assert.Contains(ThreeFailuresMultiErrorValidator.SecondErrorMessage, failures[1]);
        Assert.Contains(ThreeFailuresMultiErrorValidator.ThirdErrorMessage, failures[2]);
        Assert.Contains(ThreeFailuresMultiErrorValidator.ThirdPropertyName, failures[2]);
    }

    [Fact]
    public void When_Validator_Is_Not_Adding_Any_Failure_Messages_Output_Validation_Succeeds()
    {
        var validator = new ZeroFailuresMultiErrorValidator();

        var validationResult = validator.Validate(string.Empty, new NestedOptions());

        Assert.Null(validationResult.Failures);
        Assert.False(validationResult.Failed);
        Assert.True(validationResult.Succeeded);
    }
#else
    [Fact]
    public void Validator_That_Inherits_From_BaseValidator_Outputs_All_Error_Messages_For_Older_Frameworks()
    {
        var validator = new ThreeFailuresMultiErrorValidator();

        var validationResult = validator.Validate(string.Empty, new NestedOptions());

        Assert.True(validationResult.Failed);
        Assert.Contains(ThreeFailuresMultiErrorValidator.FirstErrorMessage, validationResult.FailureMessage);
        Assert.Contains(ThreeFailuresMultiErrorValidator.SecondErrorMessage, validationResult.FailureMessage);
        Assert.Contains(ThreeFailuresMultiErrorValidator.ThirdErrorMessage, validationResult.FailureMessage);
    }

    [Fact]
    public void When_Validator_Is_Not_Adding_Any_Failure_Messages_Output_Message_Validation_Succeeds()
    {
        var validator = new ZeroFailuresMultiErrorValidator();

        var validationResult = validator.Validate(string.Empty, new NestedOptions());

        Assert.False(validationResult.Failed);
        Assert.True(validationResult.Succeeded);

        Assert.DoesNotContain(ThreeFailuresMultiErrorValidator.FirstErrorMessage, validationResult.FailureMessage);
        Assert.DoesNotContain(ThreeFailuresMultiErrorValidator.SecondErrorMessage, validationResult.FailureMessage);
        Assert.DoesNotContain(ThreeFailuresMultiErrorValidator.ThirdErrorMessage, validationResult.FailureMessage);
    }
#endif

    [Fact]
    public void Success()
    {
        var builder = new ValidateOptionsResultBuilder();
        var vr = builder.Build();
        Assert.True(vr.Succeeded);
    }

    [Fact]
    public void AddErrors_VaidationResult()
    {
        var builder = new ValidateOptionsResultBuilder();
        builder.AddResults(new[]
        {
            new ValidationResult("FAIL1"),
            new ValidationResult("FAIL2"),
        });

        var vr = builder.Build();
        var failures = vr.FailureMessage!.Split(';');

        Assert.True(vr.Failed);
        Assert.Equal(2, failures.Length);
        Assert.Contains("FAIL1", failures[0]);
        Assert.Contains("FAIL2", failures[1]);
    }

#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void AddErrors_VaidateOptionsResult()
    {
        var builder = new ValidateOptionsResultBuilder();
        builder.AddResult(ValidateOptionsResult.Fail(new[] { "FAIL1", "FAIL2" }));

        var vr = builder.Build();
        var failures = vr.FailureMessage!.Split(';');

        Assert.True(vr.Failed);
        Assert.Equal(2, failures.Length);
        Assert.Contains("FAIL1", failures[0]);
        Assert.Contains("FAIL2", failures[1]);
    }
#endif

    [Fact]
    public void NonDefaultContructor()
    {
        var builder = new ValidateOptionsResultBuilder();
        builder.AddError("A");
        builder.AddError("B");
        builder.AddError("C");
        var vr = builder.Build();
        var failures = vr.FailureMessage!.Split(';');

        Assert.True(vr.Failed);
        Assert.Equal(3, failures.Length);
        Assert.Contains("A", failures[0]);
        Assert.Contains("B", failures[1]);
        Assert.Contains("C", failures[2]);
    }
}
