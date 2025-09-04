// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolReductionTests
{
    [Fact]
    public async Task Strategy_NoReduction_WhenToolsBelowLimit()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 5);

        var tools = CreateTools("Weather", "Math");
        var options = new ChatOptions { Tools = tools };

        var result = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "Tell me about weather") },
            options);

        Assert.Same(tools, result);
    }

    [Fact]
    public async Task Strategy_Reduces_ToLimit_BySimilarity()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);

        var tools = CreateTools(
            "Weather",
            "Translate",
            "Math",
            "Jokes");

        var options = new ChatOptions { Tools = tools };

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "Can you do some weather math for forecasting?")
        };

        var reduced = (await strategy.SelectToolsForRequestAsync(messages, options)).ToList();

        Assert.Equal(2, reduced.Count);

        // Only assert membership; ordering is an implementation detail when scores tie.
        Assert.Contains(reduced, t => t.Name == "Weather");
        Assert.Contains(reduced, t => t.Name == "Math");
    }

    [Fact]
    public async Task Strategy_PreserveOriginalOrdering_ReordersAfterSelection()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2)
        {
            PreserveOriginalOrdering = true
        };

        var tools = CreateTools("Math", "Translate", "Weather");
        var options = new ChatOptions { Tools = tools };

        var messages = new[] { new ChatMessage(ChatRole.User, "Explain weather math please") };

        var reduced = (await strategy.SelectToolsForRequestAsync(messages, options)).ToList();

        Assert.Equal(2, reduced.Count);
        Assert.Contains(reduced, t => t.Name == "Math");
        Assert.Contains(reduced, t => t.Name == "Weather");

        // With PreserveOriginalOrdering the original relative order (Math before Weather) is maintained.
        Assert.Equal("Math", reduced[0].Name);
        Assert.Equal("Weather", reduced[1].Name);
    }

    [Fact]
    public async Task Strategy_EmptyQuery_FallsBackToFirstN()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);

        var tools = CreateTools("A", "B", "C");
        var options = new ChatOptions { Tools = tools };

        var messages = new[] { new ChatMessage(ChatRole.User, "   ") };

        var reduced = (await strategy.SelectToolsForRequestAsync(messages, options)).ToList();

        Assert.Equal(2, reduced.Count);
        Assert.Equal("A", reduced[0].Name);
        Assert.Equal("B", reduced[1].Name);
    }

    [Fact]
    public async Task Strategy_Caching_AvoidsReEmbeddingTools()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);

        var tools = CreateTools("Weather", "Math", "Jokes");
        var options = new ChatOptions { Tools = tools };
        var messages = new[] { new ChatMessage(ChatRole.User, "weather") };

        _ = await strategy.SelectToolsForRequestAsync(messages, options);
        int afterFirst = gen.TotalValueInputs;

        _ = await strategy.SelectToolsForRequestAsync(messages, options);
        int afterSecond = gen.TotalValueInputs;

        Assert.Equal(afterFirst + 1, afterSecond);
    }

    [Fact]
    public async Task Strategy_CachingDisabled_ReEmbedsToolsEachCall()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            EnableEmbeddingCaching = false
        };

        var tools = CreateTools("Weather", "Math");
        var options = new ChatOptions { Tools = tools };
        var messages = new[] { new ChatMessage(ChatRole.User, "weather") };

        _ = await strategy.SelectToolsForRequestAsync(messages, options);
        int afterFirst = gen.TotalValueInputs;

        _ = await strategy.SelectToolsForRequestAsync(messages, options);
        int afterSecond = gen.TotalValueInputs;

        Assert.Equal(afterFirst + tools.Count + 1, afterSecond);
    }

    [Fact]
    public void Strategy_Constructor_ThrowsWhenToolLimitIsLessThanOrEqualToZero()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmbeddingToolReductionStrategy(gen, toolLimit: 0));
    }

    [Fact]
    public async Task ToolReducingChatClient_ReducesTools_ForGetResponseAsync()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);
        var tools = CreateTools("Weather", "Math", "Translate", "Jokes");

        IList<AITool>? observedTools = null;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, ct) =>
            {
                observedTools = options?.Tools;
                return Task.FromResult(new ChatResponse());
            }
        };

        using var client = inner
            .AsBuilder()
            .UseToolReduction(strategy)
            .Build();

        await client.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, "weather math please") },
            new ChatOptions { Tools = tools });

        Assert.NotNull(observedTools);
        Assert.Equal(2, observedTools!.Count);
        Assert.Contains(observedTools, t => t.Name == "Weather");
        Assert.Contains(observedTools, t => t.Name == "Math");
    }

    [Fact]
    public async Task ToolReducingChatClient_ReducesTools_ForStreaming()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);
        var tools = CreateTools("Weather", "Math");

        IList<AITool>? observedTools = null;

        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (messages, options, ct) =>
            {
                observedTools = options?.Tools;
                return AsyncEnumerable.Empty<ChatResponseUpdate>();
            }
        };

        using var client = inner
            .AsBuilder()
            .UseToolReduction(strategy)
            .Build();

        await foreach (var _ in client.GetStreamingResponseAsync(
            new[] { new ChatMessage(ChatRole.User, "math") },
            new ChatOptions { Tools = tools }))
        {
            // Consume
        }

        Assert.NotNull(observedTools);
        Assert.Single(observedTools!);
        Assert.Equal("Math", observedTools![0].Name);
    }

    private static List<AITool> CreateTools(params string[] names) =>
        names.Select(n => (AITool)new SimpleTool(n, $"Description about {n}")).ToList();

    private sealed class SimpleTool : AITool
    {
        private readonly string _name;
        private readonly string _description;

        public SimpleTool(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public override string Name => _name;
        public override string Description => _description;
    }

    /// <summary>
    /// Deterministic embedding generator producing sparse keyword indicator vectors.
    /// Each dimension corresponds to a known keyword. Cosine similarity then reflects
    /// pure keyword overlap (non-overlapping keywords contribute nothing), avoiding
    /// false ties for tools unrelated to the query.
    /// </summary>
    private sealed class DeterministicTestEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private static readonly string[] _keywords =
        [
            "weather","forecast","temperature","math","calculate","sum","translate","language","joke"
        ];

        // +1 bias dimension (last) to avoid zero magnitude vectors when no keywords present.
        private static int VectorLength => _keywords.Length + 1;

        public int TotalValueInputs { get; private set; }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Embedding<float>>();

            foreach (var v in values)
            {
                TotalValueInputs++;
                var vec = new float[VectorLength];
                if (!string.IsNullOrWhiteSpace(v))
                {
                    var lower = v.ToLowerInvariant();
                    for (int i = 0; i < _keywords.Length; i++)
                    {
                        if (lower.Contains(_keywords[i]))
                        {
                            vec[i] = 1f;
                        }
                    }
                }

                vec[VectorLength - 1] = 1f; // bias
                list.Add(new Embedding<float>(vec));
            }

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            // No-op
        }
    }
}
