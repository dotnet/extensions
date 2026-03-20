// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

#pragma warning disable CA2214 // Do not call overridable methods in constructors

namespace Microsoft.Extensions.AI;

public abstract class VideoGeneratorIntegrationTests : IDisposable
{
    private readonly IVideoGenerator? _generator;

    protected VideoGeneratorIntegrationTests()
    {
        _generator = CreateGenerator();
    }

    public void Dispose()
    {
        _generator?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract IVideoGenerator? CreateGenerator();

    [ConditionalFact]
    public virtual async Task GenerateVideosAsync_SingleVideoGeneration()
    {
        SkipIfNotEnabled();

        var options = new VideoGenerationOptions
        {
            Count = 1
        };

        var response = await _generator.GenerateVideosAsync("A simple animation of a bouncing ball", options);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Contents);

        var content = Assert.Single(response.Contents);
        switch (content)
        {
            case UriContent uc:
                Assert.StartsWith("http", uc.Uri.Scheme, StringComparison.Ordinal);
                break;

            case DataContent dc:
                Assert.False(dc.Data.IsEmpty);
                Assert.StartsWith("video/", dc.MediaType, StringComparison.Ordinal);
                break;

            default:
                Assert.Fail($"Unexpected content type: {content.GetType()}");
                break;
        }
    }

    [ConditionalFact]
    public virtual async Task GenerateVideosAsync_MultipleVideos()
    {
        SkipIfNotEnabled();

        var options = new VideoGenerationOptions
        {
            Count = 2
        };

        var response = await _generator.GenerateVideosAsync("A cat sitting on a table", options);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Contents);
        Assert.Equal(2, response.Contents.Count);

        foreach (var content in response.Contents)
        {
            Assert.IsType<DataContent>(content);
            var dataContent = (DataContent)content;
            Assert.False(dataContent.Data.IsEmpty);
            Assert.StartsWith("video/", dataContent.MediaType, StringComparison.Ordinal);
        }
    }

    [MemberNotNull(nameof(_generator))]
    protected void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];

        if (skipIntegration is not null || _generator is null)
        {
            throw new SkipTestException("Generator is not enabled.");
        }
    }
}
