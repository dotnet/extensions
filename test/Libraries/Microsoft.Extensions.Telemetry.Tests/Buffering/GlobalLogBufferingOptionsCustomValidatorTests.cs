// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Buffering.Test;

public class GlobalLogBufferingOptionsCustomValidatorTests
{
    [Fact]
    public void GivenInvalidCategory_Fails()
    {
        var validator = new GlobalLogBufferingOptionsCustomValidator();
        var options = new GlobalLogBufferingOptions
        {
            Rules = new List<LogBufferingFilterRule>
            {
                new LogBufferingFilterRule(categoryName: "**")
            },
        };

        var validationResult = validator.Validate(string.Empty, options);

        Assert.True(validationResult.Failed, validationResult.FailureMessage);
        Assert.Contains(nameof(options.Rules), validationResult.FailureMessage);
    }
}
#endif
