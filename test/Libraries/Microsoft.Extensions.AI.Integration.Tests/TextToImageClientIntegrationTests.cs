// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using Xunit;

#pragma warning disable CA2214 // Do not call overridable methods in constructors

namespace Microsoft.Extensions.AI;

public abstract class ImageClientIntegrationTests : IDisposable
{
    private readonly IImageClient? _client;

    protected ImageClientIntegrationTests()
    {
        _client = CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract IImageClient? CreateClient();

    [ConditionalFact]
    public virtual async Task GenerateImagesAsync_SingleImageGeneration()
    {
        SkipIfNotEnabled();

        var options = new ImageOptions
        {
            Count = 1
        };

        var response = await _client.GenerateImagesAsync("A simple drawing of a house", options);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Contents);
        Assert.Single(response.Contents);

        var content = response.Contents[0];
        Assert.IsType<DataContent>(content);
        var dataContent = (DataContent)content;
        Assert.False(dataContent.Data.IsEmpty);
        Assert.StartsWith("image/", dataContent.MediaType, StringComparison.Ordinal);
    }

    [ConditionalFact]
    public virtual async Task GenerateImagesAsync_MultipleImages()
    {
        SkipIfNotEnabled();

        var options = new ImageOptions
        {
            Count = 2
        };

        var response = await _client.GenerateImagesAsync("A cat sitting on a table", options);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Contents);
        Assert.Equal(2, response.Contents.Count);

        foreach (var content in response.Contents)
        {
            Assert.IsType<DataContent>(content);
            var dataContent = (DataContent)content;
            Assert.False(dataContent.Data.IsEmpty);
            Assert.StartsWith("image/", dataContent.MediaType, StringComparison.Ordinal);
        }
    }

    [ConditionalFact]
    public virtual async Task EditImagesAsync_SingleImage()
    {
        SkipIfNotEnabled();

        var imageData = GetImageData("dotnet.png");
        AIContent[] originalImages = [new DataContent(imageData, "image/png")];

        var options = new ImageOptions
        {
            Count = 1
        };

        var response = await _client.EditImagesAsync(originalImages, "Add a red border and make the background tie-dye", options);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Contents);
        Assert.Single(response.Contents);

        var content = response.Contents[0];
        Assert.IsType<DataContent>(content);
        var dataContent = (DataContent)content;
        Assert.False(dataContent.Data.IsEmpty);
        Assert.StartsWith("image/", dataContent.MediaType, StringComparison.Ordinal);
    }

    private static byte[] GetImageData(string fileName)
    {
        using Stream? s = typeof(ImageClientIntegrationTests).Assembly.GetManifestResourceStream($"Microsoft.Extensions.AI.Resources.{fileName}");
        Assert.NotNull(s);
        using MemoryStream ms = new();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    [MemberNotNull(nameof(_client))]
    protected void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];

        if (skipIntegration is not null || _client is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }
}
