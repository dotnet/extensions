// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsTextToImageClientTests
{
    [Fact]
    public void ConfigureOptionsTextToImageClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsTextToImageClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsTextToImageClient(new TestTextToImageClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestTextToImageClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        TextToImageOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        TextToImageOptions? returnedOptions = null;
        TextToImageResponse expectedResponse = new([]);
        using CancellationTokenSource cts = new();

        using ITextToImageClient innerClient = new TestTextToImageClient
        {
            GenerateImagesAsyncCallback = (prompt, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },

            EditImagesAsyncCallback = (originalImages, prompt, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },
        };

        using var client = innerClient
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                Assert.NotSame(providedOptions, options);
                if (nullProvidedOptions)
                {
                    Assert.Null(options.ModelId);
                }
                else
                {
                    Assert.Equal(providedOptions!.ModelId, options.ModelId);
                }

                returnedOptions = options;
            })
            .Build();

        var response1 = await client.GenerateImagesAsync("test prompt", providedOptions, cts.Token);
        Assert.Same(expectedResponse, response1);

        AIContent[] images = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];
        var response2 = await client.EditImagesAsync(images, "edit prompt", providedOptions, cts.Token);
        Assert.Same(expectedResponse, response2);
    }
}
