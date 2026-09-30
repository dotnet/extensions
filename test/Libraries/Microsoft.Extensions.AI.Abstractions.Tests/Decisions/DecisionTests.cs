// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionTests
{
    [Fact]
    public void EnumDefinition_PreservesExactIdsAndRejectsInvalidMappings()
    {
        DecisionEnumDefinition<Classification> definition = new(
        [
            new(Classification.Approved, "approve|v1", "Approve the request"),
            new(Classification.Rejected, "reject|v1", "Reject the request"),
        ]);

        Assert.Equal(["approve|v1", "reject|v1"], definition.Candidates.Select(static c => c.Id));
        Assert.Equal(Classification.Approved, definition.Parse("approve|v1"));
        Assert.Throws<DecisionProtocolException>(() => definition.Parse("approve"));
        Assert.Throws<ArgumentException>(() => new DecisionEnumDefinition<Classification>(
        [
            new(Classification.Approved, "one", "One"),
            new(Classification.Approved, "two", "Two"),
        ]));
        Assert.Throws<ArgumentException>(() => new DecisionEnumDefinition<Classification>(
        [
            new(Classification.Approved, "duplicate", "One"),
            new(Classification.Rejected, "duplicate", "Two"),
        ]));

        DecisionRequest request = CreateRequest(
            definition.CreateQuestion("classification", "Classify the request."));
        DecisionResponse response = new(
            request,
            [
                new ChoiceDecisionAnswer(
                    "classification",
                    "reject|v1",
                    [
                        new("approve|v1", 0.25),
                        new("reject|v1", 0.75),
                    ]),
            ]);

        Assert.Equal(
            Classification.Rejected,
            response.Bind(
                new DecisionResultBinding<Classification>(
                    TestJsonSerializerContext.Default.Classification,
                    answers => answers.GetChoice("classification", definition))));
        Assert.Equal(["approve|v1", "reject|v1"], ((ChoiceDecisionAnswer)response.Answers[0]).Probabilities.Select(static p => p.Id));
    }

    [Fact]
    public void MixedAnswers_BindToOwnedResultAndRetainRoundedScoreEvidence()
    {
        DecisionResponse response = CreateMixedResponse();

        DecisionResult result = response.Bind(
            new DecisionResultBinding<DecisionResult>(
                TestJsonSerializerContext.Default.DecisionResult,
                answers =>
                {
                    ScoreDecisionAnswer score = answers.GetScore("score");
                    return new DecisionResult
                    {
                        Approved = answers.GetBinary("binary").TrueProbability > 0.5,
                        Selected = answers.GetChoice("choice").SelectedCandidateId,
                        Score = score.Score,
                        ExpectedScore = score.ExpectedScore,
                    };
                }));

        Assert.True(result.Approved);
        Assert.Equal("blue|candidate", result.Selected);
        Assert.Equal(1.58, result.Score);
        Assert.Equal(1.6, result.ExpectedScore, precision: 12);
        Assert.Equal(3, ((ScoreDecisionAnswer)response.Answers[2]).Probabilities.Count);
    }

    [Fact]
    public void ProbabilityValidation_RejectsNullEntriesAndAllowsDeclaredRounding()
    {
        Assert.Throws<DecisionProtocolException>(() => new ChoiceDecisionAnswer(
            "choice",
            "a",
            [new DecisionProbability("a", 1), null!]));

        ChoiceDecisionAnswer twoDecimal = new(
            "choice",
            "a",
            [
                new("a", 0.33),
                new("b", 0.33),
                new("c", 0.33),
            ],
            DecisionPrecision.TwoDecimalPlaces);
        ChoiceDecisionAnswer fourDecimal = new(
            "choice",
            "a",
            [
                new("a", 0.3333),
                new("b", 0.3333),
                new("c", 0.3333),
            ],
            DecisionPrecision.FourDecimalPlaces);

        Assert.Equal(3, twoDecimal.Probabilities.Count);
        Assert.Equal(3, fourDecimal.Probabilities.Count);
        Assert.Throws<DecisionProtocolException>(() => new ChoiceDecisionAnswer(
            "choice",
            "a",
            [
                new("a", 0.33),
                new("b", 0.33),
                new("c", 0.33),
            ]));
    }

    [Fact]
    public void RoundedScoreValidation_UsesProbabilityAndScoreBounds()
    {
        ScoreDecisionAnswer valid = new(
            "score",
            1.58,
            [
                new("low", 0.1),
                new("medium", 0.2),
                new("high", 0.7),
            ],
            DecisionPrecision.TwoDecimalPlaces);

        Assert.Equal(1.58, valid.Score);
        Assert.Throws<DecisionProtocolException>(() => new ScoreDecisionAnswer(
            "score",
            2,
            [
                new("low", 1),
                new("medium", 0),
                new("high", 0),
            ],
            DecisionPrecision.TwoDecimalPlaces));
    }

    [Fact]
    public void DynamicIds_AreCorrelatedPerRequestAndRejectStaleCandidates()
    {
        string activeId = "route|" + Guid.NewGuid().ToString("N");
        ChoiceDecisionQuestion question = new(
            "route",
            "Select the active route.",
            [new DecisionCandidate(activeId, "Current route")]);
        DecisionRequest request = CreateRequest(question);

        DecisionResponse response = new(
            request,
            [new ChoiceDecisionAnswer("route", activeId, [new DecisionProbability(activeId, 1)])]);

        Assert.Equal(activeId, ((ChoiceDecisionAnswer)response.Answers[0]).SelectedCandidateId);
        Assert.Throws<DecisionProtocolException>(() => new DecisionResponse(
            request,
            [new ChoiceDecisionAnswer("route", "stale", [new DecisionProbability("stale", 1)])]));
    }

    [Fact]
    public void FeatureProjection_UsesNamedNativeDoubleCoordinatesAndProvenance()
    {
        DecisionResponse response = CreateMixedResponse();
        DecisionFeatureSchema schema = new(
            "routing-decision-v1",
            2,
            [
                new("binary.true", "binary", DecisionFeatureValueKind.BinaryTrueProbability),
                new("choice.blue", "choice", DecisionFeatureValueKind.ChoiceProbability, "blue|candidate"),
                new("score.high", "score", DecisionFeatureValueKind.ScoreProbability, "high"),
                new("score.value", "score", DecisionFeatureValueKind.Score),
            ]);

        DecisionFeatureVector vector = DecisionFeatureProjection.Project(response, schema);

        Assert.Equal("routing-decision-v1", vector.Schema.Id);
        Assert.Equal(2, vector.Schema.Version);
        Assert.Equal(["binary.true", "choice.blue", "score.high", "score.value"], vector.Values.Select(static value => value.Name));
        Assert.Equal(0.75, vector.Values[0].Value);
        Assert.Equal(0.5, vector.Values[1].Value);
        Assert.Equal(0.7, vector.Values[2].Value);
        Assert.Equal(1.58, vector.Values[3].Value);
        Assert.Equal("test-provider", vector.Provenance!.ProviderName);
    }

    [Fact]
    public void ReflectionDisabledJson_UsesExplicitSourceGeneratedContracts()
    {
        DecisionQuestion question = new BinaryDecisionQuestion("q", "Is this valid?");
        DecisionRequest request = DecisionRequest.Create(
            new DecisionState { Value = "payload" },
            TestJsonSerializerContext.Default.DecisionState,
            [question]);

        string json = JsonSerializer.Serialize(request, TestJsonSerializerContext.Default.DecisionRequest);
        DecisionRequest? roundtrip = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.DecisionRequest);

        Assert.NotNull(roundtrip);
        Assert.Equal("payload", roundtrip.State.GetProperty("value").GetString());
        Assert.Equal("q", roundtrip.Questions[0].Id);
    }

    [Fact]
    public void ReflectionDisabledJson_RoundTripsAnswerAndResponseSnapshots()
    {
        DecisionResponse original = CreateMixedResponse();

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.DecisionResponse);
        DecisionResponse? roundtrip = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.DecisionResponse);

        Assert.NotNull(roundtrip);
        Assert.Equal(3, roundtrip.Answers.Count);
        Assert.Equal("binary", ((BinaryDecisionAnswer)roundtrip.Answers[0]).AdditionalProperties!["kind"]!.ToString());
        Assert.Equal("choice", ((ChoiceDecisionAnswer)roundtrip.Answers[1]).AdditionalProperties!["kind"]!.ToString());
        Assert.Equal("score", ((ScoreDecisionAnswer)roundtrip.Answers[2]).AdditionalProperties!["kind"]!.ToString());
        Assert.Equal("response", roundtrip.AdditionalProperties!["kind"]!.ToString());
        Assert.Equal(1.58, ((ScoreDecisionAnswer)roundtrip.Answers[2]).Score);
    }

    [Fact]
    public async Task ClientCancellation_IsNotConvertedToFabricatedResponse()
    {
        using CancellationClient client = new();
        DecisionRequest request = CreateRequest(new BinaryDecisionQuestion("q", "Is this valid?"));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetResponseAsync(request, cancellationToken: cancellation.Token));
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task Projection_IsSafeToRunConcurrentlyOnAnOwnedResponse()
    {
        DecisionResponse response = CreateMixedResponse();
        DecisionFeatureSchema schema = new(
            "concurrent-v1",
            1,
            [new("choice.blue", "choice", DecisionFeatureValueKind.ChoiceProbability, "blue|candidate")]);

        DecisionFeatureVector[] vectors = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(() => DecisionFeatureProjection.Project(response, schema))));

        Assert.All(vectors, vector => Assert.Equal(0.5, vector.Values[0].Value));
    }

    [Fact]
    public void BindingMapperFailure_IsPropagatedWithoutPartialSuccess()
    {
        DecisionResponse response = CreateMixedResponse();
        DecisionResultBinding<DecisionResult> binding = new(
            TestJsonSerializerContext.Default.DecisionResult,
            _ => throw new InvalidOperationException("mapper failed"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => response.Bind(binding));
        Assert.Equal("mapper failed", exception.Message);
    }

    [Fact]
    public void RequestsAndResponses_OwnCollectionSnapshots()
    {
        List<DecisionQuestion> questions = [new BinaryDecisionQuestion("q", "Is this valid?")];
        DecisionRequest request = CreateRequest(questions.ToArray());
        questions.Clear();

        List<DecisionAnswer> answers = [new BinaryDecisionAnswer("q", 0.75)];
        DecisionResponse response = new(request, answers);
        answers.Clear();

        Assert.Single(request.Questions);
        Assert.Single(response.Answers);
    }

    [Fact]
    public void OptionsClone_OwnsProviderExtensionSnapshot()
    {
        DecisionOptions options = new()
        {
            ModelId = "model",
            AdditionalProperties = new AdditionalPropertiesDictionary { ["mode"] = "original" },
        };

        DecisionOptions clone = options.Clone();
        options.AdditionalProperties["mode"] = "changed";

        Assert.Equal("model", clone.ModelId);
        Assert.Equal("original", clone.AdditionalProperties!["mode"]);
    }

    private static DecisionRequest CreateRequest(params DecisionQuestion[] questions)
    {
        using JsonDocument document = JsonDocument.Parse("{}");
        return new(document.RootElement.Clone(), questions);
    }

    private static DecisionResponse CreateMixedResponse()
    {
        DecisionRequest request = CreateRequest(
            new BinaryDecisionQuestion("binary", "Is the request approved?"),
            new ChoiceDecisionQuestion(
                "choice",
                "Select a color.",
                [
                    new("red|candidate", "Red"),
                    new("blue|candidate", "Blue"),
                    new("green|candidate", "Green"),
                ]),
            new ScoreDecisionQuestion(
                "score",
                "Score the request.",
                [
                    new("low", "Low"),
                    new("medium", "Medium"),
                    new("high", "High"),
                ]));

        return new DecisionResponse(
            request,
            [
                new BinaryDecisionAnswer(
                    "binary",
                    0.75,
                    additionalProperties: new Dictionary<string, object?> { ["kind"] = "binary" }),
                new ChoiceDecisionAnswer(
                    "choice",
                    "blue|candidate",
                    [
                        new("red|candidate", 0.2),
                        new("blue|candidate", 0.5),
                        new("green|candidate", 0.3),
                    ],
                    additionalProperties: new Dictionary<string, object?> { ["kind"] = "choice" }),
                new ScoreDecisionAnswer(
                    "score",
                    1.58,
                    [
                        new("low", 0.1),
                        new("medium", 0.2),
                        new("high", 0.7),
                    ],
                    DecisionPrecision.TwoDecimalPlaces,
                    additionalProperties: new Dictionary<string, object?> { ["kind"] = "score" }),
            ],
            new DecisionProvenance(providerName: "test-provider", modelId: "test-model"),
            additionalProperties: new Dictionary<string, object?> { ["kind"] = "response" });
    }

    internal enum Classification
    {
        Approved,
        Rejected,
    }

    internal sealed class DecisionState
    {
        public string? Value { get; set; }
    }

    internal sealed class DecisionResult
    {
        public bool Approved { get; set; }
        public string? Selected { get; set; }
        public double Score { get; set; }
        public double ExpectedScore { get; set; }
    }

    private sealed class CancellationClient : IDecisionClient
    {
        public int CallCount { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromCanceled<DecisionResponse>(cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
