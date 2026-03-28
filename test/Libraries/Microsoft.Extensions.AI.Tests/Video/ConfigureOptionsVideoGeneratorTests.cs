// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsVideoGeneratorTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        using var generator = new TestVideoGenerator();
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new ConfigureOptionsVideoGenerator(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsVideoGenerator(generator, null!));
    }

    [Fact]
    public async Task ConfigureCallback_ReceivesClonedOptions()
    {
        var originalOptions = new VideoGenerationOptions { ModelId = "original-model" };
        VideoGenerationOptions? capturedOptions = null;

        using var inner = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                capturedOptions = options;
                return Task.FromResult<VideoGenerationOperation>(new TestVideoGenerationOperation());
            }
        };

        using var configured = new ConfigureOptionsVideoGenerator(inner, opts =>
        {
            opts.ModelId = "configured-model";
        });

        await configured.GenerateAsync(new VideoGenerationRequest("Test"), originalOptions);

        Assert.NotNull(capturedOptions);
        Assert.NotSame(originalOptions, capturedOptions);
        Assert.Equal("configured-model", capturedOptions!.ModelId);
        Assert.Equal("original-model", originalOptions.ModelId); // Original unchanged
    }

    [Fact]
    public async Task ConfigureCallback_WithNullOptions_CreatesNewInstance()
    {
        VideoGenerationOptions? capturedOptions = null;

        using var inner = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                capturedOptions = options;
                return Task.FromResult<VideoGenerationOperation>(new TestVideoGenerationOperation());
            }
        };

        using var configured = new ConfigureOptionsVideoGenerator(inner, opts =>
        {
            opts.ModelId = "new-model";
        });

        await configured.GenerateAsync(new VideoGenerationRequest("Test"), null);

        Assert.NotNull(capturedOptions);
        Assert.Equal("new-model", capturedOptions!.ModelId);
    }
}
