// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsImageGeneratorTests
{
    [Fact]
    public void ConfigureOptionsImageGenerator_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new ConfigureOptionsImageGenerator(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsImageGenerator(new TestImageGenerator(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerGenerator = new TestImageGenerator();
        var builder = innerGenerator.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextGenerator(bool nullProvidedOptions)
    {
        ImageOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        ImageOptions? returnedOptions = null;
        ImageResponse expectedResponse = new([]);
        using CancellationTokenSource cts = new();

        using IImageGenerator innerGenerator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (prompt, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },

        };

        using var generator = innerGenerator
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

        var response1 = await generator.GenerateImagesAsync("test prompt", providedOptions, cts.Token);
        Assert.Same(expectedResponse, response1);
    }
}
