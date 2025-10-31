// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public sealed class IngestionPipelineTests : IDisposable
{
    private readonly FileInfo _withTable;
    private readonly FileInfo _withImage;
    private readonly IReadOnlyList<FileInfo> _sampleFiles;
    private readonly DirectoryInfo _sampleDirectory;

    public IngestionPipelineTests()
    {
        _sampleDirectory = Directory.CreateDirectory(Path.Combine("TestFiles"));

        _withTable = new(Path.Combine("TestFiles", "withTable.md"));
        const string FirstFileContent = """
            # First Document

            This is the content of the first document.

            ## Subsection

            More content in section 1.

            ## Table

            What a nice table!

            | Header1 | Header2 |
            |---------|---------|
            | Cell1   | Cell2   |
            | Cell3   | Cell4   |
            """;
        File.WriteAllText(_withTable.FullName, FirstFileContent);

        _withImage = new(Path.Combine("TestFiles", "withImage.md"));
        string secondFileContent = $"""
            # Second Document

            Content for the second document goes here.

            ## Another Subsection

            Additional content in section 2.

            It comes with an image!

            ![Sample Image](data:image/png;base64,{Convert.ToBase64String(new byte[1000])})
            """;
        File.WriteAllText(_withImage.FullName, secondFileContent);

        _sampleFiles = [_withTable, _withImage];
    }

    public void Dispose()
    {
        _sampleDirectory.Delete(recursive: true);
    }

    [Fact]
    public async Task CanProcessDocuments()
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        TestEmbeddingGenerator<string> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<string> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        using IngestionPipeline<string> pipeline = new(CreateReader(), CreateChunker(), vectorStoreWriter);
        await pipeline.ProcessAsync(_sampleFiles);

        Assert.True(embeddingGenerator.WasCalled, "Embedding generator should have been called.");

        var retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => _sampleFiles.Any(info => info.FullName == (string)record["documentid"]!), top: 1000)
            .ToListAsync();

        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Count; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.NotEmpty((string)retrieved[i]["content"]!);
            Assert.Contains((string)retrieved[i]["documentid"]!, _sampleFiles.Select(info => info.FullName));
        }

        AssertActivities(activities, "ProcessFiles", pipeline);
    }

    [Fact]
    public async Task CanProcessDocumentsInDirectory()
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        TestEmbeddingGenerator<string> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<string> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        using IngestionPipeline<string> pipeline = new(CreateReader(), CreateChunker(), vectorStoreWriter);

        DirectoryInfo directory = new("TestFiles");
        await pipeline.ProcessAsync(directory, "*.md");

        Assert.True(embeddingGenerator.WasCalled, "Embedding generator should have been called.");

        var retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => ((string)record["documentid"]!).StartsWith(directory.FullName), top: 1000)
            .ToListAsync();

        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Count; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.NotEmpty((string)retrieved[i]["content"]!);
            Assert.StartsWith(directory.FullName, (string)retrieved[i]["documentid"]!);
        }

        AssertActivities(activities, "ProcessDirectory", pipeline);
    }

    [Fact]
    public async Task ChunksCanBeMoreThanJustText()
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        TestEmbeddingGenerator<DataContent> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<DataContent> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<DataContent>.DimensionCount);
        using IngestionPipeline<DataContent> pipeline = new(CreateReader(), new ImageChunker(), vectorStoreWriter);

        Assert.False(embeddingGenerator.WasCalled);
        await pipeline.ProcessAsync(_sampleFiles);

        var retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => ((string)record["documentid"]!).EndsWith(_withImage.Name), top: 100)
            .ToListAsync();

        Assert.True(embeddingGenerator.WasCalled);
        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Count; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.EndsWith(_withImage.Name, (string)retrieved[i]["documentid"]!);
        }

        AssertActivities(activities, "ProcessFiles", pipeline);
    }

    internal class ImageChunker : IngestionChunker<DataContent>
    {
        public override IAsyncEnumerable<IngestionChunk<DataContent>> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
            => document.EnumerateContent()
                    .OfType<IngestionDocumentImage>()
                    .Select(image => new IngestionChunk<DataContent>(
                        content: new(image.Content.GetValueOrDefault(), image.MediaType!),
                        document: document))
                    .ToAsyncEnumerable();
    }

    [Fact]
    public async Task CanTraceExceptions()
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        TestEmbeddingGenerator<string> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<string> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        using IngestionPipeline<string> pipeline = new(new ThrowingReader(), CreateChunker(), vectorStoreWriter);

        await Assert.ThrowsAsync<ExpectedException>(() => pipeline.ProcessAsync([new FileInfo("ReaderWillThrowAnyway.cs")]));
        AssertErrorActivities(activities);
        activities.Clear();

        await Assert.ThrowsAsync<ExpectedException>(() => pipeline.ProcessAsync(new DirectoryInfo(".")));
        AssertErrorActivities(activities);
        activities.Clear();
    }

    private class ThrowingReader : IngestionDocumentReader
    {
        public override Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
            => throw new ExpectedException();

        public override Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
            => throw new ExpectedException();
    }

    private class ExpectedException : Exception
    {
        internal const string ExceptionMessage = "An expected exception occurred.";

        public ExpectedException()
            : base(ExceptionMessage)
        {
        }
    }

    private static IngestionDocumentReader CreateReader() => new MarkdownReader();

    private static IngestionChunker<string> CreateChunker() => new HeaderChunker(new(TiktokenTokenizer.CreateForModel("gpt-4")));

    private static TracerProvider CreateTraceProvider(List<Activity> activities)
        => Sdk.CreateTracerProviderBuilder()
            .AddSource("Experimental.Microsoft.Extensions.DataIngestion")
            .ConfigureResource(r => r.AddService("inmemory-test"))
            .AddInMemoryExporter(activities)
            .Build();

    private static void AssertActivities<T>(List<Activity> activities, string rootActivityName, IngestionPipeline<T> pipeline)
    {
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Experimental.Microsoft.Extensions.DataIngestion", a.Source.Name));
        Assert.Single(activities, a => a.OperationName == rootActivityName);
        Assert.Contains(activities, a => a.OperationName == "ProcessFile");
        Assert.Contains(activities, a => a.OperationName == "ReadDocument");

        if (pipeline.DocumentProcessors.Count > 0)
        {
            Assert.Contains(activities, a => a.OperationName == "ProcessDocument");
        }
    }

    private static void AssertErrorActivities(List<Activity> activities)
    {
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Experimental.Microsoft.Extensions.DataIngestion", a.Source.Name));
        Assert.All(activities, a => Assert.Equal(ActivityStatusCode.Error, a.Status));
        Assert.All(activities, a => Assert.Equal(ExpectedException.ExceptionMessage, a.StatusDescription));
    }
}
