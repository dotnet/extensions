// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DelegatingDocumentExtractionClientTests
{
    [Fact]
    public void RequiresInnerDocumentExtractionClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingDocumentExtractionClient(null!));
    }

    [Fact]
    public async Task ExtractAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        using var expectedDocument = new MemoryStream();
        var expectedMediaType = "application/pdf";
        var expectedOptions = new DocumentExtractionOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<DocumentExtractionResult>();
        var expectedResponse = new DocumentExtractionResult([]);
        using var inner = new TestDocumentExtractionClient
        {
            ExtractAsyncCallback = (document, mediaType, options, cancellationToken) =>
            {
                Assert.Same(expectedDocument, document);
                Assert.Same(expectedMediaType, mediaType);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);

        // Act
        var resultTask = delegating.ExtractAsync(expectedDocument, expectedMediaType, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task ExtractPagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        using var expectedDocument = new MemoryStream();
        var expectedMediaType = "application/pdf";
        var expectedOptions = new DocumentExtractionOptions();
        using var cts = new CancellationTokenSource();
        DocumentExtractionPageResult[] expectedUpdates =
        [
            new(new DocumentPage(1, "page one")),
            new(new DocumentPage(2, "page two")),
        ];

        using var inner = new TestDocumentExtractionClient
        {
            ExtractPagesAsyncCallback = (document, mediaType, options, cancellationToken) =>
            {
                Assert.Same(expectedDocument, document);
                Assert.Same(expectedMediaType, mediaType);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldAsync(expectedUpdates);
            }
        };

        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);

        // Act
        List<DocumentExtractionPageResult> received = [];
        await foreach (var update in delegating.ExtractPagesAsync(expectedDocument, expectedMediaType, expectedOptions, cts.Token))
        {
            received.Add(update);
        }

        // Assert
        Assert.Equal(expectedUpdates, received);
    }

    private static async IAsyncEnumerable<DocumentExtractionPageResult> YieldAsync(IEnumerable<DocumentExtractionPageResult> updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestDocumentExtractionClient();
        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestDocumentExtractionClient();
        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);

        // Act
        var client = delegating.GetService<DelegatingDocumentExtractionClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestDocumentExtractionClient();
        using var inner = new TestDocumentExtractionClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);

        // Act
        var client = delegating.GetService<IDocumentExtractionClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestDocumentExtractionClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingDocumentExtractionClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    private sealed class NoOpDelegatingDocumentExtractionClient(IDocumentExtractionClient innerClient)
        : DelegatingDocumentExtractionClient(innerClient);
}
