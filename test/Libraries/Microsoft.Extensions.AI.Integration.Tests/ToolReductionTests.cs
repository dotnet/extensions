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
    public void EmbeddingToolReductionStrategy_Constructor_ThrowsWhenToolLimitIsLessThanOrEqualToZero()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmbeddingToolReductionStrategy(gen, toolLimit: 0));
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_NoReduction_WhenToolsBelowLimit()
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
    public async Task EmbeddingToolReductionStrategy_NoReduction_WhenOptionalToolsBelowLimit()
    {
        // 1 required + 2 optional, limit = 2 (optional count == limit) => original list returned
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2)
        {
            IsRequiredTool = t => t.Name == "Req"
        };

        var tools = CreateTools("Req", "Opt1", "Opt2");
        var result = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "anything") },
            new ChatOptions { Tools = tools });

        Assert.Same(tools, result);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_Reduces_ToLimit_BySimilarity()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);

        var tools = CreateTools("Weather", "Translate", "Math", "Jokes");
        var options = new ChatOptions { Tools = tools };

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "Can you do some weather math for forecasting?")
        };

        var reduced = (await strategy.SelectToolsForRequestAsync(messages, options)).ToList();

        Assert.Equal(2, reduced.Count);
        Assert.Contains(reduced, t => t.Name == "Weather");
        Assert.Contains(reduced, t => t.Name == "Math");
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_PreserveOriginalOrdering_ReordersAfterSelection()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2)
        {
            PreserveOriginalOrdering = true
        };

        var tools = CreateTools("Math", "Translate", "Weather");
        var reduced = (await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "Explain weather math please") },
            new ChatOptions { Tools = tools })).ToList();

        Assert.Equal(2, reduced.Count);
        Assert.Equal("Math", reduced[0].Name);
        Assert.Equal("Weather", reduced[1].Name);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_Caching_AvoidsReEmbeddingTools()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);

        var tools = CreateTools("Weather", "Math", "Jokes");
        var messages = new[] { new ChatMessage(ChatRole.User, "weather") };

        _ = await strategy.SelectToolsForRequestAsync(messages, new ChatOptions { Tools = tools });
        int afterFirst = gen.TotalValueInputs;

        _ = await strategy.SelectToolsForRequestAsync(messages, new ChatOptions { Tools = tools });
        int afterSecond = gen.TotalValueInputs;

        // +1 for second query embedding only
        Assert.Equal(afterFirst + 1, afterSecond);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_OptionsNullOrNoTools_ReturnsEmptyOrOriginal()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);

        var empty = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "anything") }, null);
        Assert.Empty(empty);

        var options = new ChatOptions { Tools = [] };
        var result = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "weather") }, options);
        Assert.Same(options.Tools, result);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_CustomSimilarity_InvertsOrdering()
    {
        using var gen = new VectorBasedTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            Similarity = (q, t) => -t.Span[0]
        };

        var highTool = new SimpleTool("HighScore", "alpha");
        var lowTool = new SimpleTool("LowScore", "beta");
        gen.VectorSelector = text => text.Contains("alpha") ? 10f : 1f;

        var reduced = (await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "Pick something") },
            new ChatOptions { Tools = [highTool, lowTool] })).ToList();

        Assert.Single(reduced);
        Assert.Equal("LowScore", reduced[0].Name);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_TieDeterminism_PrefersLowerOriginalIndex()
    {
        // Generator returns identical vectors so similarity ties; we expect original order preserved
        using var gen = new ConstantEmbeddingGenerator(3);
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 2);

        var tools = CreateTools("T1", "T2", "T3", "T4");
        var reduced = (await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "any") },
            new ChatOptions { Tools = tools })).ToList();

        Assert.Equal(2, reduced.Count);
        Assert.Equal("T1", reduced[0].Name);
        Assert.Equal("T2", reduced[1].Name);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_DefaultEmbeddingTextSelector_EmptyDescription_UsesNameOnly()
    {
        using var recorder = new RecordingEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(recorder, toolLimit: 1);

        var target = new SimpleTool("ComputeSum", description: "");
        var filler = new SimpleTool("Other", "Unrelated");
        _ = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "math") },
            new ChatOptions { Tools = [target, filler] });

        Assert.Contains("ComputeSum", recorder.Inputs);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_DefaultEmbeddingTextSelector_EmptyName_UsesDescriptionOnly()
    {
        using var recorder = new RecordingEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(recorder, toolLimit: 1);

        var target = new SimpleTool("", description: "Translates between languages.");
        var filler = new SimpleTool("Other", "Unrelated");
        _ = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "translate") },
            new ChatOptions { Tools = [target, filler] });

        Assert.Contains("Translates between languages.", recorder.Inputs);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_CustomEmbeddingTextSelector_Applied()
    {
        using var recorder = new RecordingEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(recorder, toolLimit: 1)
        {
            ToolEmbeddingTextSelector = t => $"NAME:{t.Name}|DESC:{t.Description}"
        };

        var target = new SimpleTool("WeatherTool", "Gets forecast.");
        var filler = new SimpleTool("Other", "Irrelevant");
        _ = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "weather") },
            new ChatOptions { Tools = [target, filler] });

        Assert.Contains("NAME:WeatherTool|DESC:Gets forecast.", recorder.Inputs);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_MessagesEmbeddingTextSelector_CustomFiltersMessages()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);

        var tools = CreateTools("Weather", "Math", "Translate");

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "Please tell me the weather tomorrow."),
            new ChatMessage(ChatRole.Assistant, "Sure, I can help."),
            new ChatMessage(ChatRole.User, "Now instead solve a math problem.")
        };

        strategy.MessagesEmbeddingTextSelector = msgs => msgs.LastOrDefault()?.Text ?? string.Empty;

        var reduced = (await strategy.SelectToolsForRequestAsync(
            messages,
            new ChatOptions { Tools = tools })).ToList();

        Assert.Single(reduced);
        Assert.Equal("Math", reduced[0].Name);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_MessagesEmbeddingTextSelector_InvokedOnce()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);

        var tools = CreateTools("Weather", "Math");
        int invocationCount = 0;

        strategy.MessagesEmbeddingTextSelector = msgs =>
        {
            invocationCount++;
            return string.Join("\n", msgs.Select(m => m.Text));
        };

        _ = await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "weather and math") },
            new ChatOptions { Tools = tools });

        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_DefaultMessagesEmbeddingTextSelector_IncludesReasoningContent()
    {
        using var recorder = new RecordingEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(recorder, toolLimit: 1);
        var tools = CreateTools("Weather", "Math");

        var reasoningLine = "Thinking about the best way to get tomorrow's forecast...";
        var answerLine = "Tomorrow will be sunny.";
        var userLine = "What's the weather tomorrow?";

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, userLine),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextReasoningContent(reasoningLine),
                new TextContent(answerLine)
            ])
        };

        _ = await strategy.SelectToolsForRequestAsync(messages, new ChatOptions { Tools = tools });

        string queryInput = recorder.Inputs[0];

        Assert.Contains(userLine, queryInput);
        Assert.Contains(reasoningLine, queryInput);
        Assert.Contains(answerLine, queryInput);

        var userIndex = queryInput.IndexOf(userLine, StringComparison.Ordinal);
        var reasoningIndex = queryInput.IndexOf(reasoningLine, StringComparison.Ordinal);
        var answerIndex = queryInput.IndexOf(answerLine, StringComparison.Ordinal);
        Assert.True(userIndex >= 0 && reasoningIndex > userIndex && answerIndex > reasoningIndex);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_DefaultMessagesEmbeddingTextSelector_SkipsNonTextContent()
    {
        using var recorder = new RecordingEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(recorder, toolLimit: 1);
        var tools = CreateTools("Alpha", "Beta");

        var textOnly = "Provide translation.";
        var messages = new[]
        {
            new ChatMessage(ChatRole.User,
            [
                new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream"),
                new TextContent(textOnly)
            ])
        };

        _ = await strategy.SelectToolsForRequestAsync(messages, new ChatOptions { Tools = tools });

        var queryInput = recorder.Inputs[0];
        Assert.Contains(textOnly, queryInput);
        Assert.DoesNotContain("application/octet-stream", queryInput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_RequiredToolAlwaysIncluded()
    {
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            IsRequiredTool = t => t.Name == "Core"
        };

        var tools = CreateTools("Core", "Weather", "Math");
        var reduced = (await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "math") },
            new ChatOptions { Tools = tools })).ToList();

        Assert.Equal(2, reduced.Count); // required + one optional (limit=1)
        Assert.Contains(reduced, t => t.Name == "Core");
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_MultipleRequiredTools_ExceedLimit_AllRequiredIncluded()
    {
        // 3 required, limit=1 => expect 3 required + 1 ranked optional = 4 total
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            IsRequiredTool = t => t.Name.StartsWith("R", StringComparison.Ordinal)
        };

        var tools = CreateTools("R1", "R2", "R3", "Weather", "Math");
        var reduced = (await strategy.SelectToolsForRequestAsync(
            new[] { new ChatMessage(ChatRole.User, "weather math") },
            new ChatOptions { Tools = tools })).ToList();

        Assert.Equal(4, reduced.Count);
        Assert.Equal(3, reduced.Count(t => t.Name.StartsWith("R")));
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

        using var client = inner.AsBuilder().UseToolReduction(strategy).Build();

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
                return EmptyAsyncEnumerable<ChatResponseUpdate>();
            }
        };

        using var client = inner.AsBuilder().UseToolReduction(strategy).Build();

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

    [Fact]
    public async Task EmbeddingToolReductionStrategy_EmptyQuery_NoReduction()
    {
        // Arrange: more tools than limit so we'd normally reduce, but query is empty -> return full list unchanged.
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1);

        var tools = CreateTools("ToolA", "ToolB", "ToolC");
        var options = new ChatOptions { Tools = tools };

        // Empty / whitespace message text produces empty query.
        var messages = new[] { new ChatMessage(ChatRole.User, "   ") };

        // Act
        var result = await strategy.SelectToolsForRequestAsync(messages, options);

        // Assert: same reference (no reduction), and generator not invoked at all.
        Assert.Same(tools, result);
        Assert.Equal(0, gen.TotalValueInputs);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_EmptyQuery_NoReduction_WithRequiredTool()
    {
        // Arrange: required tool + optional tools; still should return original set when query is empty.
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            IsRequiredTool = t => t.Name == "Req"
        };

        var tools = CreateTools("Req", "Optional1", "Optional2");
        var options = new ChatOptions { Tools = tools };

        var messages = new[] { new ChatMessage(ChatRole.User, "   ") };

        // Act
        var result = await strategy.SelectToolsForRequestAsync(messages, options);

        // Assert
        Assert.Same(tools, result);
        Assert.Equal(0, gen.TotalValueInputs);
    }

    [Fact]
    public async Task EmbeddingToolReductionStrategy_EmptyQuery_ViaCustomMessagesSelector_NoReduction()
    {
        // Arrange: force empty query through custom selector returning whitespace.
        using var gen = new DeterministicTestEmbeddingGenerator();
        var strategy = new EmbeddingToolReductionStrategy(gen, toolLimit: 1)
        {
            MessagesEmbeddingTextSelector = _ => "   "
        };

        var tools = CreateTools("One", "Two");
        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "This content will be ignored by custom selector.")
        };

        // Act
        var result = await strategy.SelectToolsForRequestAsync(messages, new ChatOptions { Tools = tools });

        // Assert: no reduction and no embeddings generated.
        Assert.Same(tools, result);
        Assert.Equal(0, gen.TotalValueInputs);
    }

    private static List<AITool> CreateTools(params string[] names) =>
        names.Select(n => (AITool)new SimpleTool(n, $"Description about {n}")).ToList();

#pragma warning disable CS1998
    private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
    {
        yield break;
    }
#pragma warning restore CS1998

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

                vec[^1] = 1f; // bias
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

    private sealed class RecordingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public List<string> Inputs { get; } = new();

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Embedding<float>>();
            foreach (var v in values)
            {
                Inputs.Add(v);

                // Basic 2-dim vector (length encodes a bit of variability)
                list.Add(new Embedding<float>(new float[] { v.Length, 1f }));
            }

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
            // No-op
        }
    }

    private sealed class VectorBasedTestEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Func<string, float> VectorSelector { get; set; } = _ => 1f;
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var list = new List<Embedding<float>>();
            foreach (var v in values)
            {
                list.Add(new Embedding<float>(new float[] { VectorSelector(v), 1f }));
            }

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
            // No-op
        }
    }

    private sealed class ConstantEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly float[] _vector;
        public ConstantEmbeddingGenerator(int dims)
        {
            _vector = Enumerable.Repeat(1f, dims).ToArray();
        }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var list = new List<Embedding<float>>();
            foreach (var _ in values)
            {
                list.Add(new Embedding<float>(_vector));
            }

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
            // No-op
        }
    }

    private sealed class TestChatClient : IChatClient
    {
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? GetResponseAsyncCallback { get; set; }
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? GetStreamingResponseAsyncCallback { get; set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            (GetResponseAsyncCallback ?? throw new InvalidOperationException())(messages, options, cancellationToken);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            (GetStreamingResponseAsyncCallback ?? throw new InvalidOperationException())(messages, options, cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
            // No-op
        }
    }
}
