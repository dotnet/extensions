// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Test;

public class FakeRedactorOptionsValidatorTest
{
    [Fact]
    public void Validator_Fails_When_Template_Is_Forcefully_Set_To_Null()
    {
        var validator = new FakeRedactorOptionsAutoValidator();
        var options = new FakeRedactorOptions
        {
            RedactionFormat = null!
        };

        var validationResult = validator.Validate(string.Empty, options);

        Assert.True(validationResult.Failed, "Validator passed when it should fail.");
        Assert.False(validationResult.Succeeded, "Validator passed when it should fail.");
        Assert.Contains(nameof(FakeRedactorOptions.RedactionFormat), validationResult.FailureMessage);
    }

    [Theory]
    [InlineData("__________{{{}}2}________")]
    [InlineData("{0}{1}")]
    [InlineData("{{01}{}{}}}{")]
    [InlineData("_}}2{{{3}}}}")]
    [InlineData("{0}{1}{2}{3}{4}")]
    public void FakeRedactorValidator_Fails_Given_Invalid_Template(string format)
    {
        var validator = new FakeRedactorOptionsCustomValidator();
        var options = new FakeRedactorOptions
        {
            RedactionFormat = format
        };

        var validationResult = validator.Validate(string.Empty, options);

        Assert.True(validationResult.Failed, validationResult.FailureMessage);
        Assert.Contains(nameof(options.RedactionFormat), validationResult.FailureMessage);
    }
}
