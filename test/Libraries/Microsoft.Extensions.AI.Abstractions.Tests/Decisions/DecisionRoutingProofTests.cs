// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Test models and their source-generated context are co-located.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public sealed class DecisionRoutingProofTests
{
    private static readonly ChoiceDecisionQuestion _routeQuestion = new(
        "route",
        "Choose the configured chat route.",
        [
            new DecisionCandidate("primary", "Use the primary route."),
            new DecisionCandidate("secondary", "Use the secondary route."),
        ]);

    [Fact]
    public async Task ConsumerOwnedDecisionSelectorPreservesForwardingAndLifetime()
    {
        ChatMessage[] messages = [new(ChatRole.User, "hello")];
        ChatOptions options = new() { ModelId = "request-model" };
        using CancellationTokenSource cancellation = new();
        using RecordingDecisionClient decisionClient = new("primary");
        using CountingChatClient selectedClient = new();
        Dictionary<string, IChatClient> clients = new(StringComparer.Ordinal)
        {
            ["primary"] = selectedClient,
        };
        IEnumerable<ChatMessage>? forwardedMessages = null;
        ChatOptions? forwardedOptions = null;
        ChatResponse expectedResponse = new(new ChatMessage(ChatRole.Assistant, "selected"));
        selectedClient.GetResponseAsyncCallback = (forwarded, forwardedChatOptions, token) =>
        {
            forwardedMessages = forwarded;
            forwardedOptions = forwardedChatOptions;
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(expectedResponse);
        };

        using RoutingChatClient router = CreateConsumerOwnedRouter(
            decisionClient,
            clients,
            context =>
            {
                Assert.Same(messages, context.Messages);
                context.ChatOptions!.ModelId = "configured-primary";
                return new RoutingDecisionState(
                    context.Messages.Count(),
                    context.ChatOptions.ModelId,
                    context.ChatOptions.Tools is not null);
            });

        ChatResponse actual = await router.GetResponseAsync(messages, options, cancellation.Token);

        Assert.Same(expectedResponse, actual);
        Assert.Same(messages, forwardedMessages);
        Assert.NotSame(options, forwardedOptions);
        Assert.Equal("configured-primary", forwardedOptions!.ModelId);
        Assert.Equal("request-model", options.ModelId);
        Assert.Equal(1, decisionClient.CallCount);
        Assert.Equal(cancellation.Token, decisionClient.LastCancellationToken);

        router.Dispose();
        Assert.Equal(0, selectedClient.DisposeCount);
    }

    [Fact]
    public async Task ConsumerOwnedDecisionSelectorRequiresConfiguredCandidate()
    {
        using RecordingDecisionClient decisionClient = new("secondary");
        using CountingChatClient primaryClient = new();
        using RoutingChatClient router = CreateConsumerOwnedRouter(
            decisionClient,
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["primary"] = primaryClient,
            });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hello")]));

        Assert.Contains("secondary", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, decisionClient.CallCount);
        Assert.Equal(0, primaryClient.CallCount);
    }

    [Fact]
    public async Task ConsumerOwnedDecisionSelectorPropagatesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using RecordingDecisionClient decisionClient = new("primary");
        using CountingChatClient selectedClient = new();
        using RoutingChatClient router = CreateConsumerOwnedRouter(
            decisionClient,
            new Dictionary<string, IChatClient>(StringComparer.Ordinal)
            {
                ["primary"] = selectedClient,
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hello")], cancellationToken: cancellation.Token));

        Assert.Equal(1, decisionClient.CallCount);
        Assert.Equal(0, selectedClient.CallCount);
    }

    private static RoutingChatClient CreateConsumerOwnedRouter(
        IDecisionClient decisionClient,
        IReadOnlyDictionary<string, IChatClient> clients,
        Func<RoutingContext, RoutingDecisionState>? projectState = null)
    {
        projectState ??= static context => new(
            context.Messages.Count(),
            context.ChatOptions?.ModelId,
            context.ChatOptions?.Tools is not null);

        return RoutingChatClient.Create(async (context, cancellationToken) =>
        {
            RoutingDecisionState state = projectState(context);
            DecisionResponse response = await decisionClient.GetResponseAsync(
                state,
                DecisionRoutingJsonContext.Default.RoutingDecisionState,
                [_routeQuestion],
                cancellationToken: cancellationToken);
            string selectedId = ((ChoiceDecisionAnswer)response.GetAnswer("route")).SelectedCandidateId;

            if (!_routeQuestion.Candidates.Any(candidate => string.Equals(candidate.Id, selectedId, StringComparison.Ordinal)) ||
                !clients.TryGetValue(selectedId, out IChatClient? selected))
            {
                throw new InvalidOperationException($"Decision selected unconfigured route '{selectedId}'.");
            }

            return selected;
        });
    }

    private sealed class RecordingDecisionClient : IDecisionClient
    {
        private readonly string _selectedId;

        public RecordingDecisionClient(string selectedId)
        {
            _selectedId = selectedId;
        }

        public int CallCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCancellationToken = cancellationToken;

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            return Task.FromResult(
                new DecisionResponse(
                    request,
                    [
                        new ChoiceDecisionAnswer(
                            "route",
                            _selectedId,
                            [
                                new("primary", _selectedId == "primary" ? 0.8 : 0.2),
                                new("secondary", _selectedId == "secondary" ? 0.8 : 0.2),
                            ]),
                    ]));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class CountingChatClient : IChatClient
    {
        public int CallCount { get; private set; }

        public int DisposeCount { get; private set; }

        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? GetResponseAsyncCallback { get; set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return GetResponseAsyncCallback?.Invoke(messages, options, cancellationToken) ??
                Task.FromResult(new ChatResponse());
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => DisposeCount++;
    }

    internal sealed record RoutingDecisionState(int MessageCount, string? ModelId, bool HasTools);
}

[System.Text.Json.Serialization.JsonSourceGenerationOptions(
    PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
[System.Text.Json.Serialization.JsonSerializable(typeof(DecisionRoutingProofTests.RoutingDecisionState))]
internal sealed partial class DecisionRoutingJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
