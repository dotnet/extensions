// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;
public class ResiliencePipelineFactoryOptionsValidatorTest
{
    [Fact]
    public void Valid_Ok()
    {
        var validator = new ResiliencePipelineFactoryOptionsValidator<string>();

        Assert.True(validator.Validate("test", CreateValid()).Succeeded);
    }

    [Fact]
    public void InvalidConfiguration_EnsureError()
    {
        var validator = new ResiliencePipelineFactoryOptionsValidator<string>();

        var options = CreateValid();
        options.BuilderActions.Clear();
        Assert.True(validator.Validate("test", options).Failed);
    }

    private static ResiliencePipelineFactoryOptions<string> CreateValid()
    {
        var options = new ResiliencePipelineFactoryOptions<string>();

        options.BuilderActions.Add(builder => { });

        return options;
    }
}
