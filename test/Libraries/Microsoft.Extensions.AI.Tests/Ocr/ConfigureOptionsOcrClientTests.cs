// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsOcrClientTests
{
    [Fact]
    public void ConfigureOptionsOcrClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsOcrClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsOcrClient(new TestOcrClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestOcrClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        OcrOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        OcrOptions? returnedOptions = null;
        OcrResult expectedResult = new([new OcrPage(0, "blue whale")]);
        using CancellationTokenSource cts = new();

        using IOcrClient innerClient = new TestOcrClient
        {
            ExtractAsyncCallback = (document, mediaType, options, progress, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResult);
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

        using var document = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = await client.ExtractAsync(document, "application/pdf", providedOptions, null, cts.Token);
        Assert.Same(expectedResult, result);
    }
}
