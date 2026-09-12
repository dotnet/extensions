// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class ConfigureOptionsDocumentExtractionClientTests
{
    [Fact]
    public void ConfigureOptionsDocumentExtractionClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsDocumentExtractionClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsDocumentExtractionClient(new TestDocumentExtractionClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestDocumentExtractionClient();
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        DocumentExtractionOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        DocumentExtractionOptions? returnedOptions = null;
        DocumentExtractionResult expectedResult = new([new DocumentPage(1, "blue whale")]);
        using CancellationTokenSource cts = new();

        using IDocumentExtractionClient innerClient = new TestDocumentExtractionClient
        {
            ExtractAsyncCallback = (document, mediaType, options, cancellationToken) =>
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
        var result = await client.ExtractAsync(document, "application/pdf", providedOptions, cts.Token);
        Assert.Same(expectedResult, result);
    }
}
