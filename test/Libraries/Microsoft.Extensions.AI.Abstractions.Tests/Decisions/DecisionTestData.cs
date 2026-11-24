// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Shared fixtures for decision tests that span multiple decision types.</summary>
internal static class DecisionTestData
{
    public static byte[] ImageBytes { get; } = [0x89, 0x50, 0x4E, 0x47];

    public static JsonElement TicketStateElement =>
        JsonSerializer.SerializeToElement(new TicketState("Refund", 3), TestJsonSerializerContext.Default.TicketState);

    public static DecisionRequest CreateRequest() =>
        new(
        [
            new BinaryDecisionQuestion("binary", "Is it urgent?", "Yes", "No"),
            new ChoiceDecisionQuestion("choice", [new("a", "Billing"), new("b", [new TextContent("Support"), new DataContent(ImageBytes, "image/png")])], "Which team?"),
            new ScoreDecisionQuestion("score", [new("low", "Low"), new("medium", "Medium"), new("high", "High")], "How severe?"),
        ])
        {
            State = TicketStateElement,
        };

    public static DecisionResponse CreateMixedResponse() =>
        new([
            new BinaryDecisionAnswer("binary", 0.75),
            new ChoiceDecisionAnswer("choice", "b", [new("a", 0.1), new("b", 0.9)]),
            new UnansweredDecisionAnswer("score", DecisionKind.Score) { Reason = DecisionUnansweredReason.Refusal },
        ]);

    internal sealed record TicketState(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("priority")] int Priority);

    internal sealed record OrderState(string Status, int DaysLate);

    internal sealed class CustomContent : AIContent
    {
        public string? Value { get; set; }
    }

    internal sealed class RecordingDecisionClient(Func<DecisionRequest, DecisionResponse> responseFactory) : IDecisionClient
    {
        public DecisionRequest? Request { get; private set; }

        public DecisionOptions? Options { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<DecisionResponse> GetDecisionAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            Options = options;
            CancellationToken = cancellationToken;
            return Task.FromResult(responseFactory(request));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}