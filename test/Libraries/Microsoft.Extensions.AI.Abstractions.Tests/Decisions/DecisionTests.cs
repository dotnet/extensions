// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionTests
{
    private static readonly byte[] _imageBytes = [0x89, 0x50, 0x4E, 0x47];

    [Fact]
    public void Questions_AcceptTextOrContentForInstructionsAndCriteria()
    {
        BinaryDecisionQuestion textBinary = new("binary", "Is it urgent?", "Urgent", "Not urgent");
        Assert.Equal(DecisionKind.Binary, textBinary.Kind);
        Assert.Equal("Is it urgent?", Assert.IsType<TextContent>(Assert.Single(textBinary.Instructions)).Text);
        Assert.Equal("Urgent", Assert.IsType<TextContent>(Assert.Single(textBinary.TrueDescription!)).Text);

        BinaryDecisionQuestion descriptionsOnly = new("binary", trueDescription: "Yes", falseDescription: "No");
        Assert.Empty(descriptionsOnly.Instructions);

        DataContent reference = new(_imageBytes, "image/png");
        ChoiceDecisionQuestion contentChoice = new(
            "choice",
            [new TextContent("Which product is shown?"), reference],
            [new DecisionCandidate("a", [new TextContent("Product A"), new UriContent("https://example.com/a.png", "image/png")]), new("b", "Product B")]);
        Assert.Same(reference, contentChoice.Instructions[1]);
        Assert.IsType<UriContent>(contentChoice.Candidates[0].Description[1]);
        Assert.Equal("Product B", Assert.IsType<TextContent>(Assert.Single(contentChoice.Candidates[1].Description)).Text);

        ScoreDecisionQuestion score = new("score", (string?)null, [new("low", "Low"), new("high", [new TextContent("High")])]);
        Assert.Empty(score.Instructions);
        Assert.Equal(DecisionKind.Score, score.Kind);
    }

    [Fact]
    public void Questions_SnapshotContentAndRejectInvalidShapes()
    {
        List<AIContent> instructions = [new TextContent("Original")];
        ChoiceDecisionQuestion question = new("choice", instructions, [new("a", "A")]);
        instructions.Add(new TextContent("Added later"));
        Assert.Single(question.Instructions);

        Assert.Throws<ArgumentException>(() => new BinaryDecisionQuestion("binary"));
        Assert.Throws<ArgumentException>(() => new BinaryDecisionQuestion("binary", trueDescription: "Only true"));
        Assert.ThrowsAny<ArgumentException>(() => new BinaryDecisionQuestion(" ", "Instructions"));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionQuestion("choice", "Pick", []));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionQuestion("choice", "Pick", [new("a", "A"), new("a", "Again")]));
        Assert.Throws<ArgumentException>(() => new ScoreDecisionQuestion("score", "Rate", [new("only", "Only")]));
        Assert.Throws<ArgumentException>(() => new ScoreDecisionQuestion("score", "Rate", [new("x", "X"), new("x", "Again")]));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionCandidate("a", " "));
        Assert.Throws<ArgumentException>(() => new DecisionCandidate("a", Array.Empty<AIContent>()));
        Assert.Throws<ArgumentException>(() => new DecisionScoreLevel("a", [null!]));
    }

    [Fact]
    public void Request_RequiresStateOrContentsAndUniqueQuestions()
    {
        DecisionQuestion question = new BinaryDecisionQuestion("q", "Question?");
        JsonElement state = JsonSerializer.SerializeToElement(new TicketState("Refund", 3), TestJsonSerializerContext.Default.TicketState);

        DecisionRequest stateOnly = new(state, [question]);
        Assert.Empty(stateOnly.Contents);
        Assert.Equal("Refund", stateOnly.State!.Value.GetProperty("subject").GetString());

        DecisionRequest contentOnly = new(null, [question], [new DataContent(_imageBytes, "image/png")]);
        Assert.Null(contentOnly.State);
        Assert.IsType<DataContent>(Assert.Single(contentOnly.Contents));

        Assert.Throws<ArgumentException>(() => new DecisionRequest(null, [question]));
        Assert.Throws<ArgumentException>(() => new DecisionRequest(null, [question], []));
        Assert.Throws<ArgumentException>(() => new DecisionRequest(default(JsonElement), [question]));
        Assert.Throws<ArgumentException>(() => new DecisionRequest(state, []));
        Assert.Throws<ArgumentException>(() => new DecisionRequest(state, [question, new BinaryDecisionQuestion("q", "Again?")]));
        Assert.Throws<ArgumentException>(() => new DecisionRequest(state, [question], [null!]));
    }

    [Fact]
    public void Answers_ValidateProbabilitiesWithoutRequiringNormalization()
    {
        ChoiceDecisionAnswer choice = new("choice", "a", [new("a", 0.2), new("b", 0.2)]);
        Assert.Equal(DecisionKind.Choice, choice.Kind);
        Assert.Equal("a", choice.SelectedCandidateId);

        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryDecisionAnswer("binary", -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryDecisionAnswer("binary", 1.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BinaryDecisionAnswer("binary", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionProbability("a", double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionAnswer("choice", "c", [new("a", 0.5), new("b", 0.5)]));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionAnswer("choice", "a", [new("a", 0.5), new("a", 0.5)]));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionAnswer("choice", "a", []));
    }

    [Fact]
    public void ScoreAnswer_ComputesNormalizedExpectedScoreAndValidatesScore()
    {
        ScoreDecisionAnswer score = new("score", 1, [new("low", 0.2), new("medium", 0.2), new("high", 0.4)]);
        Assert.Equal(1.25, score.ExpectedScore, 10);
        Assert.Equal(1, score.Score);

        Assert.Null(new ScoreDecisionAnswer("score", null, [new("low", 1), new("high", 0)]).Score);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreDecisionAnswer("score", 2, [new("low", 0.5), new("high", 0.5)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreDecisionAnswer("score", double.NaN, [new("low", 0.5), new("high", 0.5)]));
        Assert.Throws<ArgumentException>(() => new ScoreDecisionAnswer("score", null, [new("low", 0), new("high", 0)]));
    }

    [Fact]
    public void UnansweredReason_UsesCaseInsensitiveEquality()
    {
        DecisionUnansweredReason custom = new("Provider_Filter");
        Assert.Equal(new DecisionUnansweredReason("provider_filter"), custom);
        Assert.True(custom != DecisionUnansweredReason.Refusal);
        Assert.Equal("refusal", DecisionUnansweredReason.Refusal.ToString());
        Assert.Equal("refusal", default(DecisionUnansweredReason).Value);
        Assert.ThrowsAny<ArgumentException>(() => new DecisionUnansweredReason(" "));
    }

    [Fact]
    public void GetAnswer_ReturnsMixedAnsweredAndUnansweredAnswers()
    {
        DecisionResponse response = CreateMixedResponse();

        UnansweredDecisionAnswer unanswered = Assert.IsType<UnansweredDecisionAnswer>(response.GetAnswer("score"));
        Assert.Equal(DecisionKind.Score, unanswered.Kind);
        Assert.Equal(DecisionUnansweredReason.Refusal, unanswered.Reason);
        Assert.IsType<ChoiceDecisionAnswer>(response.GetAnswer("choice"));
        Assert.Throws<KeyNotFoundException>(() => response.GetAnswer("missing"));
    }

    [Fact]
    public void Response_DefaultsToEmptyAnswers()
    {
        DecisionResponse response = new(null);
        Assert.Empty(response.Answers);
        Assert.Empty(new DecisionResponse().Answers);
    }

    [Fact]
    public void Json_RoundTripsMultimodalRequestsUsingDefaultOptions()
    {
        DecisionRequest request = new(
            JsonSerializer.SerializeToElement(new TicketState("Refund", 3), TestJsonSerializerContext.Default.TicketState),
            CreateRequest().Questions,
            [
                new TextContent("Customer message"),
                new DataContent(_imageBytes, "image/png"),
                new UriContent("https://example.com/receipt.jpg", "image/jpeg"),
                new DataContent(_imageBytes, "application/octet-stream"),
            ]);

        string json = JsonSerializer.Serialize(request, AIJsonUtilities.DefaultOptions);
        DecisionRequest roundTripped = JsonSerializer.Deserialize<DecisionRequest>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal(3, roundTripped.State!.Value.GetProperty("priority").GetInt32());
        Assert.Collection(
            roundTripped.Contents,
            c => Assert.Equal("Customer message", Assert.IsType<TextContent>(c).Text),
            c => Assert.Equal("image/png", Assert.IsType<DataContent>(c).MediaType),
            c => Assert.Equal("image/jpeg", Assert.IsType<UriContent>(c).MediaType),
            c => Assert.Equal(_imageBytes, Assert.IsType<DataContent>(c).Data.ToArray()));
        Assert.Collection(
            roundTripped.Questions,
            q => Assert.Equal("Yes", Assert.IsType<TextContent>(Assert.Single(Assert.IsType<BinaryDecisionQuestion>(q).TrueDescription!)).Text),
            q => Assert.IsType<DataContent>(Assert.IsType<ChoiceDecisionQuestion>(q).Candidates[1].Description[1]),
            q => Assert.Equal(["low", "medium", "high"], Assert.IsType<ScoreDecisionQuestion>(q).Levels.Select(static l => l.Id)));
    }

    [Fact]
    public void Json_RoundTripsCustomContentRegisteredByTheApplication()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(AIJsonUtilities.DefaultOptions.TypeInfoResolver, TestJsonSerializerContext.Default),
        };
        options.AddAIContentType<CustomContent>("decisionTestCustom");

        DecisionRequest request = new(null, [new BinaryDecisionQuestion("q", "Question?")], [new CustomContent { Value = "custom" }]);
        string json = JsonSerializer.Serialize(request, options);
        DecisionRequest roundTripped = JsonSerializer.Deserialize<DecisionRequest>(json, options)!;

        Assert.Contains("decisionTestCustom", json, StringComparison.Ordinal);
        Assert.Equal("custom", Assert.IsType<CustomContent>(Assert.Single(roundTripped.Contents)).Value);
    }

    [Fact]
    public void Json_RoundTripsMixedResponsesPreservingOrderAndKind()
    {
        DecisionResponse response = CreateMixedResponse();
        response.ResponseId = "response";
        response.ModelId = "model";
        response.Usage = new() { InputTokenCount = 10 };
        response.Answers[0].AdditionalProperties = new() { ["confidence"] = "high" };

        string json = JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions);
        DecisionResponse roundTripped = JsonSerializer.Deserialize<DecisionResponse>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal(3, roundTripped.Answers.Count);
        Assert.Equal("response", roundTripped.ResponseId);
        Assert.Equal("model", roundTripped.ModelId);
        Assert.Equal(10, roundTripped.Usage!.InputTokenCount);
        Assert.Equal(0.75, Assert.IsType<BinaryDecisionAnswer>(roundTripped.Answers[0]).TrueProbability);
        Assert.True(roundTripped.Answers[0].AdditionalProperties!.ContainsKey("confidence"));
        Assert.Equal("b", Assert.IsType<ChoiceDecisionAnswer>(roundTripped.Answers[1]).SelectedCandidateId);
        UnansweredDecisionAnswer unanswered = Assert.IsType<UnansweredDecisionAnswer>(roundTripped.Answers[2]);
        Assert.Equal(DecisionKind.Score, unanswered.Kind);
        Assert.Equal(DecisionUnansweredReason.Refusal, unanswered.Reason);
    }

    [Fact]
    public void Options_CloneCopiesValues()
    {
        Func<IDecisionClient, object?> factory = static _ => new object();
        DecisionOptions options = new()
        {
            ModelId = "model",
            AdditionalProperties = new() { ["key"] = "value" },
            RawRepresentationFactory = factory,
        };

        DecisionOptions clone = options.Clone();

        Assert.Equal("model", clone.ModelId);
        Assert.NotSame(options.AdditionalProperties, clone.AdditionalProperties);
        Assert.Equal("value", clone.AdditionalProperties!["key"]);
        Assert.Same(factory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void ClientException_ReportsTransience()
    {
        Assert.False(new DecisionClientException("failure").IsTransient);
        Assert.True(new DecisionClientException("throttled", isTransient: true).IsTransient);
    }

    [Fact]
    public void GetService_ReturnsTypedService()
    {
        using RecordingDecisionClient client = new(_ => CreateMixedResponse());
        Assert.Same(client, client.GetService<RecordingDecisionClient>());
        Assert.Null(client.GetService<string>());
        Assert.Throws<ArgumentNullException>(() => ((IDecisionClient)null!).GetService<object>());
    }

    [Fact]
    public async Task GetDecisionAsync_SerializesStateAndPassesContents()
    {
        DecisionOptions options = new() { ModelId = "model" };
        DecisionResponse expected = CreateMixedResponse();
        using RecordingDecisionClient client = new(_ => expected);
        using CancellationTokenSource cts = new();

        DecisionResponse response = await client.GetDecisionAsync(
            new TicketState("Refund", 3),
            TestJsonSerializerContext.Default.TicketState,
            CreateRequest().Questions,
            [new DataContent(_imageBytes, "image/png")],
            options,
            cts.Token);

        Assert.Same(expected, response);
        Assert.Equal("Refund", client.Request!.State!.Value.GetProperty("subject").GetString());
        Assert.IsType<DataContent>(Assert.Single(client.Request.Contents));
        Assert.Same(options, client.Options);
        Assert.Equal(cts.Token, client.CancellationToken);
    }

    private static DecisionRequest CreateRequest() =>
        new(
            JsonSerializer.SerializeToElement(new TicketState("Refund", 3), TestJsonSerializerContext.Default.TicketState),
            [
                new BinaryDecisionQuestion("binary", "Is it urgent?", "Yes", "No"),
                new ChoiceDecisionQuestion("choice", "Which team?", [new("a", "Billing"), new("b", [new TextContent("Support"), new DataContent(_imageBytes, "image/png")])]),
                new ScoreDecisionQuestion("score", "How severe?", [new("low", "Low"), new("medium", "Medium"), new("high", "High")]),
            ]);

    private static DecisionResponse CreateMixedResponse() =>
        new([
            new BinaryDecisionAnswer("binary", 0.75),
            new ChoiceDecisionAnswer("choice", "b", [new("a", 0.1), new("b", 0.9)]),
            new UnansweredDecisionAnswer("score", DecisionKind.Score) { Reason = DecisionUnansweredReason.Refusal },
        ]);

    internal sealed record TicketState(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("priority")] int Priority);

    internal sealed class CustomContent : AIContent
    {
        public string? Value { get; set; }
    }

    private sealed class RecordingDecisionClient(Func<DecisionRequest, DecisionResponse> responseFactory) : IDecisionClient
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
