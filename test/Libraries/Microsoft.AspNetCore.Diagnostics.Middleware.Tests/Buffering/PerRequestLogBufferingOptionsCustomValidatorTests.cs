// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Buffering;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Buffering.Test;

public class PerRequestLogBufferingOptionsCustomValidatorTests
{
    [Fact]
    public void GivenInvalidCategory_Fails()
    {
        var validator = new PerRequestLogBufferingOptionsCustomValidator();
        var options = new PerRequestLogBufferingOptions
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
