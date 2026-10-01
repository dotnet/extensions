// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    [Fact]
    public void DecisionDefinition_UsesExplicitDeclarationsAndMetadataDescriptions()
    {
        DecisionDefinition<TypedDecisionResult> definition = DecisionDefinition<TypedDecisionResult>.Create(
            TestJsonSerializerContext.Default.TypedDecisionResult,
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability, "Explicit refund instruction.");
                builder.Score(
                    result => result.Satisfaction,
                    [
                        new("low", "Low"),
                        new("medium", "Medium"),
                        new("high", "High"),
                    ]);
            });

        Assert.Equal(["category", "refundRequestProbability", "satisfaction"], definition.Questions.Select(static q => q.Id));
        Assert.Equal("Classify the customer's main concern.", definition.Questions[0].Instructions);
        Assert.Equal("Explicit refund instruction.", definition.Questions[1].Instructions);
        Assert.Equal("Rate the customer's satisfaction.", definition.Questions[2].Instructions);

        ChoiceDecisionQuestion category = Assert.IsType<ChoiceDecisionQuestion>(definition.Questions[0]);
        Assert.Equal(["Billing", "Technical", "Account"], category.Candidates.Select(static candidate => candidate.Id));
        Assert.Equal(
            ["Payment, invoice, or charge concerns.", "Product defects, errors, outages, or troubleshooting.", "Sign-in, profile, or account-access concerns."],
            category.Candidates.Select(static candidate => candidate.Description));
    }

    [Fact]
    public async Task DecisionDefinition_BindsTypedResultOnceAndRetainsEvidence()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        DecisionResponse providerResponse = CreateTypedDefinitionResponse();
        using RecordingDecisionClient client = new(providerResponse);
        DecisionOptions options = new() { ModelId = "model" };

        DecisionResponse<TypedDecisionResult> response = await client.GetResponseAsync(
            new TypedDecisionState("I need a refund for a defective product."),
            TestJsonSerializerContext.Default.TypedDecisionState,
            definition,
            options);

        Assert.Equal(1, client.CallCount);
        Assert.Same(options, client.Options);
        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal(0.25, response.Result.RefundRequestProbability);
        Assert.Equal(1.58, response.Result.Satisfaction);
        Assert.Same(providerResponse, response.Evidence);
        Assert.Equal(
            new Dictionary<TicketCategory, double>
            {
                [TicketCategory.Billing] = 0.2,
                [TicketCategory.Technical] = 0.5,
                [TicketCategory.Account] = 0.3,
            },
            response.GetDistribution(result => result.Category));
    }

    [Fact]
    public async Task DecisionDefinition_InferredStateOverloadUsesOneProviderInvocation()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(CreateTypedDefinitionResponse());

        DecisionResponse<TypedDecisionResult> response = await client.GetResponseAsync(
            new TypedDecisionState("Please reverse this payment."),
            definition,
            cancellationToken: CancellationToken.None);

        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public void DecisionDefinition_RejectsNestedDuplicateAndFlagsSelectors()
    {
        Assert.Throws<ArgumentException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            TestJsonSerializerContext.Default.TypedDecisionResult,
            builder => builder.Choice(result => result.Nested.Category)));

        Assert.Throws<ArgumentException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            TestJsonSerializerContext.Default.TypedDecisionResult,
            builder =>
            {
                builder.BinaryProbability(result => result.RefundRequestProbability);
                builder.Score(result => result.RefundRequestProbability, [new("low", "Low"), new("high", "High")]);
            }));

        Assert.Throws<ArgumentException>(() => DecisionDefinition<FlagsResult>.Create(
            TestJsonSerializerContext.Default.FlagsResult,
            builder => builder.Choice(result => result.Value)));
    }

    [Fact]
    public void DecisionDefinition_UsesPropertyDescriptionAndFluentPrecedence()
    {
        DecisionDefinition<PropertyDescriptionResult> propertyDefinition = DecisionDefinition<PropertyDescriptionResult>.Create(
            TestJsonSerializerContext.Default.PropertyDescriptionResult,
            builder => builder.BinaryProbability(result => result.Probability));
        Assert.Equal("Description on the property.", propertyDefinition.Questions[0].Instructions);

        DecisionDefinition<PropertyDescriptionResult> overrideDefinition = DecisionDefinition<PropertyDescriptionResult>.Create(
            TestJsonSerializerContext.Default.PropertyDescriptionResult,
            builder => builder.BinaryProbability(result => result.Probability, "Fluent instruction."));
        Assert.Equal("Fluent instruction.", overrideDefinition.Questions[0].Instructions);
    }

    [Fact]
    public void DecisionDefinition_UsesExactConfiguredJsonPropertyName()
    {
        DecisionDefinition<JsonNamedResult> definition = DecisionDefinition<JsonNamedResult>.Create(
            TestJsonSerializerContext.Default.JsonNamedResult,
            builder => builder.BinaryProbability(result => result.Probability));

        Assert.Equal("refund_probability", definition.Questions[0].Id);
    }

    [Fact]
    public async Task DecisionDefinition_PropagatesCancellationWithoutFabricatedTypedResult()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using RecordingDecisionClient client = new(CreateTypedDefinitionResponse(), cancel: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetResponseAsync(
                new TypedDecisionState("cancelled"),
                TestJsonSerializerContext.Default.TypedDecisionState,
                definition,
                cancellationToken: cancellation.Token));
        Assert.Equal(1, client.CallCount);
    }

    private static DecisionDefinition<TypedDecisionResult> CreateTypedDefinition() =>
        DecisionDefinition<TypedDecisionResult>.Create(
            TestJsonSerializerContext.Default.TypedDecisionResult,
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability);
                builder.Score(
                    result => result.Satisfaction,
                    [
                        new("low", "Low"),
                        new("medium", "Medium"),
                        new("high", "High"),
                    ]);
            });

    private static DecisionResponse CreateTypedDefinitionResponse()
    {
        DecisionRequest request = CreateRequest(
            new ChoiceDecisionQuestion(
                "category",
                "Classify the customer's main concern.",
                [
                    new("Billing", "Payment, invoice, or charge concerns."),
                    new("Technical", "Product defects, errors, outages, or troubleshooting."),
                    new("Account", "Sign-in, profile, or account-access concerns."),
                ]),
            new BinaryDecisionQuestion("refundRequestProbability", "Is the customer requesting a refund or payment reversal?"),
            new ScoreDecisionQuestion(
                "satisfaction",
                "Rate the customer's satisfaction.",
                [
                    new("low", "Low"),
                    new("medium", "Medium"),
                    new("high", "High"),
                ]));

        return new DecisionResponse(
            request,
            [
                new ChoiceDecisionAnswer(
                    "category",
                    "Technical",
                    [
                        new("Billing", 0.2),
                        new("Technical", 0.5),
                        new("Account", 0.3),
                    ]),
                new BinaryDecisionAnswer("refundRequestProbability", 0.25),
                new ScoreDecisionAnswer(
                    "satisfaction",
                    1.58,
                    [
                        new("low", 0.1),
                        new("medium", 0.2),
                        new("high", 0.7),
                    ],
                    DecisionPrecision.TwoDecimalPlaces),
            ]);
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

    internal enum TicketCategory
    {
        [Description("Payment, invoice, or charge concerns.")]
        Billing,

        [Description("Product defects, errors, outages, or troubleshooting.")]
        Technical,

        [Description("Sign-in, profile, or account-access concerns.")]
        Account,
    }

    [Flags]
    internal enum FlagCategory
    {
        None = 0,
        First = 1,
        Second = 2,
    }

    internal sealed record TypedDecisionResult(
        [Description("Classify the customer's main concern.")] TicketCategory Category,
        [Description("Is the customer requesting a refund or payment reversal?")] double RefundRequestProbability,
        [Description("Rate the customer's satisfaction.")] double Satisfaction)
    {
        public NestedResult Nested { get; } = new();
    }

    internal sealed record NestedResult
    {
        public TicketCategory Category { get; init; }
    }

    internal sealed record TypedDecisionState(string Message);

    internal sealed record PropertyDescriptionResult
    {
        [Description("Description on the property.")]
        public double Probability { get; init; }
    }

    internal sealed record JsonNamedResult
    {
        [JsonPropertyName("refund_probability")]
        public double Probability { get; init; }
    }

    internal sealed record FlagsResult(FlagCategory Value);

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

    internal sealed class RecordingDecisionClient : IDecisionClient
    {
        private readonly DecisionResponse _response;
        private readonly bool _cancel;

        public RecordingDecisionClient(DecisionResponse response, bool cancel = false)
        {
            _response = response;
            _cancel = cancel;
        }

        public int CallCount { get; private set; }

        public DecisionOptions? Options { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Options = options;
            if (_cancel)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            return Task.FromResult(_response);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
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
