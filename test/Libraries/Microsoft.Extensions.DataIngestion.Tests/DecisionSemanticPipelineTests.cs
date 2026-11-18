// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001 // Decision APIs are intentionally exercised by this proof.
#pragma warning disable SA1402 // Test models and their source-generated context are co-located.
#pragma warning disable SA1118 // Reader input is kept readable in the test.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

public sealed class DecisionSemanticPipelineTests
{
    private static readonly ChoiceDecisionQuestion _question = new(
        "topic",
        "Choose the semantic topic of this chunk.",
        [
            new DecisionCandidate("technical", "Technical content"),
            new DecisionCandidate("other", "Other content"),
        ]);

    [Fact]
    public async Task RealReaderChunkerDecisionProcessorAndWriterPreserveAndSerialize()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".md");
        File.WriteAllText(
            path,
            """
            # First section

            Technical content stays in the first chunk.

            ## Second section

            More content stays in the second chunk.
            """);

        try
        {
            using RecordingDecisionClient decisionClient = new();
            CollectingWriter writer = new();
            using IngestionPipeline<string> pipeline = new(
                new MarkdownReader(),
                CreateChunker(),
                writer);
            pipeline.ChunkProcessors.Add(new MetadataSeedProcessor());
            pipeline.ChunkProcessors.Add(
                new SemanticDecisionChunkProcessor(decisionClient, DecisionPipelineJsonContext.Default.Options));

            IngestionResult result = await pipeline.ProcessAsync([new FileInfo(path)]).SingleAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(2, decisionClient.CallCount);
            Assert.Equal(2, writer.Serialized.Count);

            List<SerializedChunk> records = writer.Serialized
                .Select(json => JsonSerializer.Deserialize(json, DecisionPipelineJsonContext.Default.SerializedChunk)!)
                .ToList();

            Assert.All(records, record =>
            {
                Assert.Equal(path, record.DocumentId);
                Assert.Equal("preserve-me", record.UnrelatedMetadata);
                Assert.Equal("technical", record.Topic);
                Assert.Equal(0.12345678901234567, record.TechnicalProbability);
                Assert.Equal("test-model", record.ModelId);
                Assert.NotEmpty(record.Content);
            });
            Assert.Contains(records, record => record.Context is not null);
            Assert.Contains(records, record => record.Content.Contains("Technical content", StringComparison.Ordinal));
            Assert.Contains(records, record => record.Content.Contains("More content", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DecisionProcessorReportsProviderFailureThroughIngestionResult()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".md");
        File.WriteAllText(path, "# Section\n\nContent.");

        try
        {
            using RecordingDecisionClient decisionClient = new()
            {
                Failure = new DecisionClientException("decision provider failed", isTransient: false),
            };
            CollectingWriter writer = new();
            using IngestionPipeline<string> pipeline = new(
                new MarkdownReader(),
                CreateChunker(),
                writer);
            pipeline.ChunkProcessors.Add(
                new SemanticDecisionChunkProcessor(decisionClient, DecisionPipelineJsonContext.Default.Options));

            IngestionResult result = await pipeline.ProcessAsync([new FileInfo(path)]).SingleAsync();

            Assert.False(result.Succeeded);
            Assert.Same(decisionClient.Failure, result.Exception);
            Assert.Empty(writer.Serialized);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class MetadataSeedProcessor : IngestionChunkProcessor<string>
    {
        public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(
            IAsyncEnumerable<IngestionChunk<string>> chunks,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                chunk.Metadata["unrelated"] = "preserve-me";
                yield return chunk;
            }

        }
    }

    private static IngestionChunker<string> CreateChunker() =>
        new HeaderChunker(new(TiktokenTokenizer.CreateForModel("gpt-4")));

    private sealed class SemanticDecisionChunkProcessor : IngestionChunkProcessor<string>
    {
        private readonly IDecisionClient _client;
        private readonly JsonSerializerOptions _serializerOptions;

        public SemanticDecisionChunkProcessor(IDecisionClient client, JsonSerializerOptions serializerOptions)
        {
            _client = client;
            _serializerOptions = serializerOptions;
        }

        public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(
            IAsyncEnumerable<IngestionChunk<string>> chunks,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                DecisionResponse response = await _client.GetDecisionAsync(
                    new SemanticDecisionState(chunk.Content.Length, chunk.Context is not null),
                    _serializerOptions,
                    [_question],
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                chunk.Metadata["decision.response"] = response;
                yield return chunk;
            }
        }
    }

    private sealed class CollectingWriter : IngestionChunkWriter<string>
    {
        public List<string> Serialized { get; } = [];

        public override async Task WriteAsync(
            IAsyncEnumerable<IngestionChunk<string>> chunks,
            CancellationToken cancellationToken = default)
        {
            await foreach (IngestionChunk<string> chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (chunk.Metadata["decision.response"] is not DecisionResponse response)
                {
                    throw new InvalidOperationException("The decision response was not attached.");
                }

                ChoiceDecisionAnswer topic = Assert.IsType<ChoiceDecisionAnswer>(Assert.Single(response.Answers));

                Serialized.Add(
                    JsonSerializer.Serialize(
                        new SerializedChunk
                        {
                            Content = chunk.Content,
                            DocumentId = chunk.Document.Identifier,
                            Context = chunk.Context,
                            UnrelatedMetadata = (string)chunk.Metadata["unrelated"],
                            Topic = topic.SelectedCandidateId,
                            TechnicalProbability = topic.Probabilities.Single(static p => p.Id == "technical").Value,
                            ModelId = response.ModelId,
                        },
                        DecisionPipelineJsonContext.Default.SerializedChunk));
            }
        }
    }

    private sealed class RecordingDecisionClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public Exception? Failure { get; set; }

        public Task<DecisionResponse> GetDecisionAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (Failure is not null)
            {
                return Task.FromException<DecisionResponse>(Failure);
            }

            return Task.FromResult(
                new DecisionResponse(
                    [
                        new ChoiceDecisionAnswer(
                            "topic",
                            "technical",
                            [
                                new("technical", 0.12345678901234567),
                                new("other", 0.8765432109876543),
                            ]),
                    ])
                {
                    ModelId = "test-model",
                });
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    internal sealed class SemanticDecisionState
    {
        public SemanticDecisionState(int contentLength, bool hasContext)
        {
            ContentLength = contentLength;
            HasContext = hasContext;
        }

        public int ContentLength { get; }

        public bool HasContext { get; }
    }

    internal sealed class SerializedChunk
    {
        public string Content { get; set; } = string.Empty;

        public string DocumentId { get; set; } = string.Empty;

        public string? Context { get; set; }

        public string UnrelatedMetadata { get; set; } = string.Empty;

        public string? Topic { get; set; }

        public double TechnicalProbability { get; set; }

        public string? ModelId { get; set; }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DecisionSemanticPipelineTests.SemanticDecisionState))]
[JsonSerializable(typeof(DecisionSemanticPipelineTests.SerializedChunk))]
internal sealed partial class DecisionPipelineJsonContext : JsonSerializerContext;
