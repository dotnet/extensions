// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.AI.DocumentIntelligence;
using LlamaParse;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public class DocumentPipelineTests
{
    public static TheoryData<FileInfo[], IngestionDocumentReader, IngestionChunker<string>> FilesAndReaders
    {
        get
        {
            FileInfo[] nonMarkdownFiles =
            {
                new(Path.Combine("TestFiles", "Sample.pdf")),
                new(Path.Combine("TestFiles", "Sample.docx"))
            };

            FileInfo[] markdownFiles =
            {
                new(Path.Combine("TestFiles", "Sample.md")),
            };

            List<IngestionDocumentReader> documentReaders = CreateReaders();
            List<IngestionChunker<string>> documentChunkers = CreateChunkers();

            TheoryData<FileInfo[], IngestionDocumentReader, IngestionChunker<string>> theoryData = new();
            foreach (IngestionDocumentReader reader in documentReaders)
            {
                FileInfo[] filePaths = reader switch
                {
                    MarkdownReader => markdownFiles,
                    _ => nonMarkdownFiles
                };

                foreach (IngestionChunker<string> chunker in documentChunkers)
                {
                    theoryData.Add(filePaths, reader, chunker);
                }
            }

            return theoryData;
        }
    }

    [Theory]
    [MemberData(nameof(FilesAndReaders))]
    public async Task CanProcessDocuments(FileInfo[] files, IngestionDocumentReader reader, IngestionChunker<string> chunker)
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

        using IngestionPipeline<string> pipeline = new(reader, chunker, vectorStoreWriter)
        {
            DocumentProcessors = { RemovalProcessor.Footers, RemovalProcessor.EmptySections }
        };
        await pipeline.ProcessAsync(files);

        Assert.True(embeddingGenerator.WasCalled, "Embedding generator should have been called.");

        Dictionary<string, object?>[] retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => files.Any(info => info.FullName == (string)record["documentid"]!), top: 1000)
            .ToArrayAsync();

        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Length; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.NotEmpty((string)retrieved[i]["content"]!);
            Assert.Contains((string)retrieved[i]["documentid"]!, files.Select(info => info.FullName));
        }

        AssertActivities(activities, "ProcessFiles");
    }

    public static TheoryData<IngestionDocumentReader> Readers => new(CreateReaders());

    [Theory]
    [MemberData(nameof(Readers))]
    public async Task CanProcessDocumentsInDirectory(IngestionDocumentReader reader)
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        IngestionChunker<string> documentChunker = new HeaderChunker(new(CreateTokenizer()));
        TestEmbeddingGenerator<string> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<string> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        using IngestionPipeline<string> pipeline = new(reader, documentChunker, vectorStoreWriter)
        {
            DocumentProcessors = { RemovalProcessor.Footers, RemovalProcessor.EmptySections }
        };

        DirectoryInfo directory = new("TestFiles");
        string searchPattern = reader switch
        {
            MarkdownReader => "*.md",
            _ => "*.docx"
        };
        await pipeline.ProcessAsync(directory, searchPattern);

        Assert.True(embeddingGenerator.WasCalled, "Embedding generator should have been called.");

        Dictionary<string, object?>[] retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => ((string)record["documentid"]!).StartsWith(directory.FullName), top: 1000)
            .ToArrayAsync();

        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Length; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.NotEmpty((string)retrieved[i]["content"]!);
            Assert.StartsWith(directory.FullName, (string)retrieved[i]["documentid"]!);
        }

        AssertActivities(activities, "ProcessDirectory");
    }

    [Fact]
    public async Task ChunksCanBeMoreThanJustText()
    {
        MarkdownReader reader = new();
        IngestionChunker<DataContent> imageChunker = new ImageChunker();
        TestEmbeddingGenerator<DataContent> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<DataContent> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<DataContent>.DimensionCount);
        using IngestionPipeline<DataContent> pipeline = new(reader, imageChunker, vectorStoreWriter);

        Assert.False(embeddingGenerator.WasCalled);
        await pipeline.ProcessAsync([new FileInfo(Path.Combine("TestFiles", "SampleWithImage.md"))]);

        Dictionary<string, object?>[] retrieved = await vectorStoreWriter.VectorStoreCollection
            .GetAsync(record => ((string)record["documentid"]!).EndsWith("SampleWithImage.md"), top: 100)
            .ToArrayAsync();

        Assert.True(embeddingGenerator.WasCalled);
        Assert.NotEmpty(retrieved);
        for (int i = 0; i < retrieved.Length; i++)
        {
            Assert.NotEmpty((string)retrieved[i]["key"]!);
            Assert.EndsWith("SampleWithImage.md", (string)retrieved[i]["documentid"]!);
        }
    }

    public class ImageChunker : IngestionChunker<DataContent>
    {
        public override IAsyncEnumerable<IngestionChunk<DataContent>> ProcessAsync(IngestionDocument document, CancellationToken cancellationToken = default)
            => document.EnumerateContent()
                    .OfType<IngestionDocumentImage>()
                    .Select(image => new IngestionChunk<DataContent>
                    (
                        content: new(image.Content.GetValueOrDefault(), image.MediaType!),
                        document: document
                    ))
                    .ToAsyncEnumerable();
    }

    [Fact]
    public async Task CanTraceExceptions()
    {
        List<Activity> activities = [];
        using TracerProvider tracerProvider = CreateTraceProvider(activities);

        IngestionChunker<string> documentChunker = new SectionChunker(new(CreateTokenizer()));
        TestEmbeddingGenerator<string> embeddingGenerator = new();
        InMemoryVectorStoreOptions options = new()
        {
            EmbeddingGenerator = embeddingGenerator
        };
        using InMemoryVectorStore testVectorStore = new(options);
        using VectorStoreWriter<string> vectorStoreWriter = new(testVectorStore, dimensionCount: TestEmbeddingGenerator<string>.DimensionCount);

        using IngestionPipeline<string> pipeline = new(new ThrowingReader(), documentChunker, vectorStoreWriter);

        await Assert.ThrowsAsync<ExpectedException>(() => pipeline.ProcessAsync([new FileInfo("ReaderWillThrowAnyway.cs")]));
        AssertErrorActivities(activities);
        activities.Clear();

        await Assert.ThrowsAsync<ExpectedException>(() => pipeline.ProcessAsync(new DirectoryInfo(".")));
        AssertErrorActivities(activities);
        activities.Clear();
    }

    private static List<IngestionDocumentReader> CreateReaders()
    {
        List<IngestionDocumentReader> readers = new()
        {
            new MarkdownReader(),
            new MarkItDownReader(),
        };

#if RELEASE // running these takes a lot of time (and costs money), so only do it in release builds as using the 2 above is usually sufficient to detect bugs.
        if (Environment.GetEnvironmentVariable("LLAMACLOUD_API_KEY") is string llamaKey && !string.IsNullOrEmpty(llamaKey))
        {
            LlamaParse.Configuration configuration = new()
            {
                ApiKey = llamaKey,
                ItemsToExtract = ItemType.Table,
            };

            readers.Add(new LlamaParseReader(new LlamaParseClient(new HttpClient(), configuration)));
        }

        if (Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INT_KEY") is string adiKey && !string.IsNullOrEmpty(adiKey)
            && Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INT_ENDPOINT") is string endpoint && !string.IsNullOrEmpty(endpoint))
        {
            AzureKeyCredential credential = new(adiKey);
            DocumentIntelligenceClient client = new(new Uri(endpoint), credential);

            readers.Add(new DocumentIntelligenceReader(client));
        }
#endif

        return readers;
    }

    private static Tokenizer CreateTokenizer() => TiktokenTokenizer.CreateForModel("gpt-4");

    private static List<IngestionChunker<string>> CreateChunkers() => [
        // Chunk size comes from https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-chunk-documents#text-split-skill-example
        new HeaderChunker(new(CreateTokenizer())),
        new SectionChunker(new(CreateTokenizer()))
    ];

    private static TracerProvider CreateTraceProvider(List<Activity> activities)
        => Sdk.CreateTracerProviderBuilder()
            .AddSource("Experimental.Microsoft.Extensions.DataIngestion")
            .ConfigureResource(r => r.AddService("inmemory-test"))
            .AddInMemoryExporter(activities)
            .Build();

    private static void AssertActivities(List<Activity> activities, string rootActivityName)
    {
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Experimental.Microsoft.Extensions.DataIngestion", a.Source.Name));
        Assert.Single(activities, a => a.OperationName == rootActivityName);
        Assert.Contains(activities, a => a.OperationName == "ProcessFile");
        Assert.Contains(activities, a => a.OperationName == "ReadDocument");
        Assert.Contains(activities, a => a.OperationName == "ProcessDocument");
    }

    private static void AssertErrorActivities(List<Activity> activities)
    {
        Assert.NotEmpty(activities);
        Assert.All(activities, a => Assert.Equal("Experimental.Microsoft.Extensions.DataIngestion", a.Source.Name));
        Assert.All(activities, a => Assert.Equal(ActivityStatusCode.Error, a.Status));
        Assert.All(activities, a => Assert.Equal(ExpectedException.ExceptionMessage, a.StatusDescription));
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

        public ExpectedException() : base(ExceptionMessage) { }
    }
}
