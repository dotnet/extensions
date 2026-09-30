// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Test models and their source-generated context are co-located.
#pragma warning disable SA1118 // Long generic constructor arguments are kept readable.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class DecisionFunctionTests
{
    [Fact]
    public async Task AsAIFunction_UsesBoundedSchemaAndPortableResult()
    {
        using RecordingDecisionClient client = new();
        DecisionTask<DecisionToolState, DecisionToolResult> task = CreateTask(
            new DecisionResultBinding<DecisionToolResult>(
                DecisionFunctionJsonContext.Default.DecisionToolResult,
                answers => new()
                {
                    Selected = answers.GetChoice("route").SelectedCandidateId,
                }));

        DecisionOptions options = new() { ModelId = "decision-model" };
        AIFunction function = task.AsAIFunction(
            client,
            options,
            new AIFunctionFactoryOptions
            {
                Name = "choose_route",
                Description = "Choose one route.",
                SerializerOptions = DecisionFunctionJsonContext.Default.Options,
            });
        options.ModelId = "changed-after-creation";

        Assert.Equal("choose_route", function.Name);
        Assert.Equal("Choose one route.", function.Description);
        Assert.Equal(["state"], function.JsonSchema.GetProperty("required").EnumerateArray().Select(static value => value.GetString()));
        Assert.True(function.ReturnJsonSchema!.Value.GetProperty("properties").TryGetProperty("features", out _));
        Assert.False(function.ReturnJsonSchema.Value.GetProperty("properties").TryGetProperty("request", out _));

        JsonElement serializedState = JsonSerializer.SerializeToElement(
            new DecisionToolState { Payload = "sensitive request" },
            DecisionFunctionJsonContext.Default.DecisionToolState);
        JsonElement resultJson = Assert.IsType<JsonElement>(
            await function.InvokeAsync(new AIFunctionArguments { ["state"] = serializedState }));
        Assert.Equal("primary", resultJson.GetProperty("result").GetProperty("selected").GetString());
        Assert.Equal("route.primary", resultJson.GetProperty("features").GetProperty("values")[0].GetProperty("name").GetString());
        Assert.Equal(0.75, resultJson.GetProperty("features").GetProperty("values")[0].GetProperty("value").GetDouble());
        Assert.Equal("test-provider", resultJson.GetProperty("provenance").GetProperty("providerName").GetString());
        Assert.Equal(12, resultJson.GetProperty("usage").GetProperty("inputTokenCount").GetInt32());
        Assert.Equal(1, client.CallCount);
        Assert.Equal("decision-model", client.LastOptions!.ModelId);
        Assert.DoesNotContain("sensitive request", resultJson.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("raw-secret", resultJson.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(0, client.DisposeCount);
    }

    [Fact]
    public async Task AsAIFunction_PropagatesCancellationProviderAndMapperFailures()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using RecordingDecisionClient cancelledClient = new()
        {
            Cancellation = true,
        };
        AIFunction cancelledFunction = CreateTask().AsAIFunction(
            cancelledClient,
            functionOptions: new() { SerializerOptions = DecisionFunctionJsonContext.Default.Options });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledFunction.InvokeAsync(
                new AIFunctionArguments { ["state"] = new DecisionToolState() },
                cancellation.Token).AsTask());
        Assert.Equal(1, cancelledClient.CallCount);

        using RecordingDecisionClient failedClient = new()
        {
            Failure = new DecisionClientException("provider failed", isTransient: true),
        };
        AIFunction failedFunction = CreateTask().AsAIFunction(
            failedClient,
            functionOptions: new() { SerializerOptions = DecisionFunctionJsonContext.Default.Options });
        DecisionClientException providerFailure = await Assert.ThrowsAsync<DecisionClientException>(
            () => failedFunction.InvokeAsync(new AIFunctionArguments { ["state"] = new DecisionToolState() }).AsTask());
        Assert.True(providerFailure.IsTransient);

        using RecordingDecisionClient mapperClient = new();
        AIFunction mapperFunction = CreateTask(
            new DecisionResultBinding<DecisionToolResult>(
                DecisionFunctionJsonContext.Default.DecisionToolResult,
                _ => throw new InvalidOperationException("mapper failed"))).AsAIFunction(
                    mapperClient,
                    functionOptions: new() { SerializerOptions = DecisionFunctionJsonContext.Default.Options });

        InvalidOperationException mapperFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mapperFunction.InvokeAsync(new AIFunctionArguments { ["state"] = new DecisionToolState() }).AsTask());
        Assert.Equal("mapper failed", mapperFailure.Message);
    }

    private static DecisionTask<DecisionToolState, DecisionToolResult> CreateTask(
        DecisionResultBinding<DecisionToolResult>? binding = null)
    {
        ChoiceDecisionQuestion question = new(
            "route",
            "Choose one route.",
            [
                new DecisionCandidate("primary", "Use the primary route."),
                new DecisionCandidate("secondary", "Use the secondary route."),
            ]);

        return new(
            [question],
            DecisionFunctionJsonContext.Default.DecisionToolState,
            binding ?? new DecisionResultBinding<DecisionToolResult>(
                DecisionFunctionJsonContext.Default.DecisionToolResult,
                answers => new() { Selected = answers.GetChoice("route").SelectedCandidateId }),
            DecisionFunctionJsonContext.Default.DecisionFunctionResultDecisionToolResult,
            new DecisionFeatureSchema(
                "decision-tool-v1",
                1,
                [new("route.primary", "route", DecisionFeatureValueKind.ChoiceProbability, "primary")]));
    }

    private sealed class RecordingDecisionClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public int DisposeCount { get; private set; }

        public DecisionOptions? LastOptions { get; private set; }

        public bool Cancellation { get; init; }

        public Exception? Failure { get; init; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastOptions = options;

            if (Failure is not null)
            {
                return Task.FromException<DecisionResponse>(Failure);
            }

            if (Cancellation)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            return Task.FromResult(
                new DecisionResponse(
                    request,
                    [
                        new ChoiceDecisionAnswer(
                            "route",
                            "primary",
                            [
                                new("primary", 0.75),
                                new("secondary", 0.25),
                            ]),
                    ],
                    new DecisionProvenance(providerName: "test-provider", modelId: "test-model", responseId: "response"),
                    new UsageDetails { InputTokenCount = 12, OutputTokenCount = 3 },
                    rawRepresentation: "raw-secret",
                    additionalProperties: new Dictionary<string, object?> { ["secret"] = "raw-secret" }));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => DisposeCount++;
    }

    internal sealed class DecisionToolState
    {
        public string? Payload { get; set; }
    }

    internal sealed class DecisionToolResult
    {
        public string? Selected { get; set; }
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DecisionFunctionTests.DecisionToolState))]
[JsonSerializable(typeof(DecisionFunctionTests.DecisionToolResult))]
[JsonSerializable(typeof(DecisionFunctionResult<DecisionFunctionTests.DecisionToolResult>))]
[JsonSerializable(typeof(DecisionFeatureVector))]
[JsonSerializable(typeof(DecisionFeatureSchema))]
[JsonSerializable(typeof(DecisionFeatureCoordinate))]
[JsonSerializable(typeof(DecisionFeatureValue))]
[JsonSerializable(typeof(DecisionProvenance))]
[JsonSerializable(typeof(UsageDetails))]
internal sealed partial class DecisionFunctionJsonContext : JsonSerializerContext;
