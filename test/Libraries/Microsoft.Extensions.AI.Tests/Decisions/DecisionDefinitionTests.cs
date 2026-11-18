// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1402 // Test models and their source-generated contexts are co-located.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionDefinitionTests
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
        Assert.Throws<ArgumentException>(() => new DecisionEnumDefinition<Classification>([]));
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

        ChoiceDecisionQuestion question = definition.CreateQuestion("classification", "Classify the request.");
        Assert.Equal("classification", question.Id);
        Assert.Equal("Classify the request.", Assert.IsType<TextContent>(Assert.Single(question.Instructions)).Text);
        Assert.Equal(["approve|v1", "reject|v1"], question.Candidates.Select(static c => c.Id));

        DecisionResponse response = new(
        [
            new ChoiceDecisionAnswer("classification", "reject|v1", [new("approve|v1", 0.25), new("reject|v1", 0.75)]),
        ]);

        Assert.Equal(Classification.Rejected, response.Bind(answers => answers.GetChoice("classification", definition)));
    }

    [Fact]
    public void Bind_MapsMixedAnswersToOwnedResult()
    {
        DecisionResponse response = CreateMixedResponse();

        DecisionResult result = response.Bind(answers =>
        {
            ScoreDecisionAnswer score = answers.GetScore("score");
            return new DecisionResult
            {
                Approved = answers.GetBinary("binary").TrueProbability > 0.5,
                Selected = answers.GetChoice("choice").SelectedCandidateId,
                Score = score.Score!.Value,
                ExpectedScore = score.ExpectedScore,
            };
        });

        Assert.True(result.Approved);
        Assert.Equal("blue|candidate", result.Selected);
        Assert.Equal(1.6, result.Score);
        Assert.Equal(1.6, result.ExpectedScore, precision: 12);
    }

    [Fact]
    public void Bind_RejectsKindMismatchAndPropagatesMapperFailure()
    {
        DecisionResponse response = CreateMixedResponse();

        Assert.Throws<DecisionProtocolException>(() => response.Bind(answers => answers.GetChoice("binary")));
        Assert.Throws<DecisionProtocolException>(() => response.Bind(answers => answers.GetScore("choice")));
        Assert.Throws<DecisionProtocolException>(() => response.Bind(answers => answers.GetBinary("score")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => response.Bind<DecisionResult>(_ => throw new InvalidOperationException("mapper failed")));
        Assert.Equal("mapper failed", exception.Message);

        Assert.Throws<ArgumentNullException>("response", () => ((DecisionResponse)null!).Bind(_ => 0));
        Assert.Throws<ArgumentNullException>("map", () => response.Bind<int>(null!));
    }

    [Fact]
    public void Bind_AllowsUnansweredQuestionsThatTheMappingDoesNotRead()
    {
        DecisionResponse response = CreateMixedResponse();
        response.Answers[2] = new UnansweredDecisionAnswer("score", DecisionKind.Score) { Reason = DecisionUnansweredReason.Refusal };

        string selected = response.Bind(answers => answers.GetChoice("choice").SelectedCandidateId);
        Assert.Equal("blue|candidate", selected);

        DecisionUnansweredException exception = Assert.Throws<DecisionUnansweredException>(
            () => response.Bind(answers => answers.GetScore("score")));
        Assert.Equal(["score"], exception.QuestionIds);
        Assert.Contains("'score'", exception.Message, StringComparison.Ordinal);
        Assert.Same(response, exception.Response);
    }

    [Fact]
    public void Create_UsesExplicitDeclarationsAndMetadataDescriptions()
    {
        DecisionDefinition<TypedDecisionResult> definition = DecisionDefinition<TypedDecisionResult>.Create(
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability, "Explicit refund instruction.");
                builder.Score(result => result.Satisfaction, CreateLevels());
            },
            DecisionTestJsonContext.Default.Options);

        Assert.Equal(["category", "refundRequestProbability", "satisfaction"], definition.Questions.Select(static q => q.Id));
        Assert.Equal("Classify the customer's main concern.", GetInstructions(definition.Questions[0]));
        Assert.Equal("Explicit refund instruction.", GetInstructions(definition.Questions[1]));
        Assert.Equal("Rate the customer's satisfaction.", GetInstructions(definition.Questions[2]));
        Assert.Same(DecisionTestJsonContext.Default.TypedDecisionResult.Type, definition.ResultTypeInfo.Type);
        Assert.Same(definition.ResultTypeInfo.Options, definition.SerializerOptions);
        Assert.True(definition.SerializerOptions.IsReadOnly);

        ChoiceDecisionQuestion category = Assert.IsType<ChoiceDecisionQuestion>(definition.Questions[0]);
        Assert.Equal(["Billing", "Technical", "Account"], category.Candidates.Select(static candidate => candidate.Id));
        Assert.Equal(
            ["Payment, invoice, or charge concerns.", "Product defects, errors, outages, or troubleshooting.", "Sign-in, profile, or account-access concerns."],
            category.Candidates.Select(static candidate => Assert.IsType<TextContent>(Assert.Single(candidate.Description)).Text));
    }

    [Fact]
    public void Create_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>("configure", () => DecisionDefinition<TypedDecisionResult>.Create(null!));
        Assert.Throws<ArgumentException>("configure", () => DecisionDefinition<TypedDecisionResult>.Create(
            _ => { },
            DecisionTestJsonContext.Default.Options));
        Assert.Throws<NotSupportedException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            builder => builder.Choice(result => result.Category),
            DecisionResultOnlyJsonContext.Default.Options));
        Assert.Throws<ArgumentException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            builder => builder.BinaryProbability(result => result.RefundRequestProbability, " "),
            DecisionTestJsonContext.Default.Options));
    }

    [Fact]
    public async Task GetResponseAsync_BindsTypedResultAndRetainsResponse()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request));
        DecisionOptions options = new() { ModelId = "model" };

        DecisionResponse<TypedDecisionResult> response = await client.GetResponseAsync(
            new TypedDecisionState("I need a refund for a defective product."),
            DecisionTestJsonContext.Default.TypedDecisionState,
            definition,
            options: options);

        Assert.Equal(1, client.CallCount);
        Assert.Same(options, client.Options);
        Assert.Equal("I need a refund for a defective product.", client.LastRequest!.State!.Value.GetProperty("message").GetString());
        Assert.Empty(client.LastRequest.Contents);
        Assert.Equal(definition.Questions, client.LastRequest.Questions);
        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal(0.25, response.Result.RefundRequestProbability);
        Assert.Equal(1.6, response.Result.Satisfaction);
        Assert.Equal("test-model", response.Response.ModelId);
        Assert.Equal(3, response.Response.Answers.Count);
        Assert.Equal(
            new Dictionary<TicketCategory, double>
            {
                [TicketCategory.Billing] = 0.2,
                [TicketCategory.Technical] = 0.5,
                [TicketCategory.Account] = 0.3,
            },
            response.GetDistribution(result => result.Category));
        Assert.Throws<ArgumentException>("selector", () => response.GetDistribution(result => result.Nested.Category));
    }

    [Fact]
    public async Task GetResponseAsync_JsonStateOverloadSendsStateAndOptionalContents()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request));
        JsonElement state = JsonSerializer.SerializeToElement(
            new TypedDecisionState("json state"),
            DecisionTestJsonContext.Default.TypedDecisionState);

        DecisionResponse<TypedDecisionResult> response = await client.GetResponseAsync(state, definition);
        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal("json state", client.LastRequest!.State!.Value.GetProperty("message").GetString());
        Assert.Empty(client.LastRequest.Contents);

        _ = await client.GetResponseAsync(state, definition, [new TextContent("supporting text")]);
        Assert.Equal("json state", client.LastRequest.State!.Value.GetProperty("message").GetString());
        Assert.Equal("supporting text", Assert.IsType<TextContent>(Assert.Single(client.LastRequest.Contents)).Text);
    }

    [Fact]
    public async Task GetResponseAsync_ContentsOverloadSendsContentsWithoutState()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request));
        List<AIContent> contents =
        [
            new TextContent("The product crashes when I sign in."),
            new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream"),
        ];

        DecisionResponse<TypedDecisionResult> response = await client.GetResponseAsync(contents, definition);

        Assert.Equal(TicketCategory.Technical, response.Result.Category);
        Assert.Equal(1, client.CallCount);
        Assert.Null(client.LastRequest!.State);
        Assert.Equal(contents, client.LastRequest.Contents);
        Assert.NotSame(contents, client.LastRequest.Contents);

        await Assert.ThrowsAsync<ArgumentNullException>("contents", () => client.GetResponseAsync(null!, definition));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetResponseAsync(Array.Empty<AIContent>(), definition));
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_TypedStateOverloadCombinesStateAndContents()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request));

        _ = await client.GetResponseAsync(
            new TypedDecisionState("typed state"),
            DecisionTestJsonContext.Default.TypedDecisionState,
            definition,
            [new TextContent("supporting text")]);

        Assert.Equal("typed state", client.LastRequest!.State!.Value.GetProperty("message").GetString());
        Assert.Equal("supporting text", Assert.IsType<TextContent>(Assert.Single(client.LastRequest.Contents)).Text);
    }

    [Fact]
    public async Task GetResponseAsync_ValidatesArguments()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request));
        JsonElement state = JsonSerializer.SerializeToElement(new TypedDecisionState("x"), DecisionTestJsonContext.Default.TypedDecisionState);

        await Assert.ThrowsAsync<ArgumentNullException>("client", () => ((IDecisionClient)null!).GetResponseAsync(state, definition));
        await Assert.ThrowsAsync<ArgumentNullException>("definition", () => client.GetResponseAsync(state, (DecisionDefinition<TypedDecisionResult>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>("stateTypeInfo", () => client.GetResponseAsync(
            new TypedDecisionState("x"),
            null!,
            definition));
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_UnansweredBoundQuestionThrowsAndExposesResponse()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request =>
        {
            DecisionResponse response = CreateTypedDefinitionResponse(request);
            response.Answers[1] = new UnansweredDecisionAnswer(request.Questions[1].Id, DecisionKind.Binary)
            {
                Reason = DecisionUnansweredReason.Refusal,
            };
            return response;
        });

        DecisionUnansweredException exception = await Assert.ThrowsAsync<DecisionUnansweredException>(() => client.GetResponseAsync(
            new TypedDecisionState("request"),
            DecisionTestJsonContext.Default.TypedDecisionState,
            definition));

        Assert.Equal(["refundRequestProbability"], exception.QuestionIds);
        Assert.Contains("'refundRequestProbability'", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("'category'", exception.Message, StringComparison.Ordinal);
        UnansweredDecisionAnswer unanswered = Assert.IsType<UnansweredDecisionAnswer>(exception.Response.Answers[1]);
        Assert.Equal(DecisionUnansweredReason.Refusal, unanswered.Reason);
        Assert.IsType<ChoiceDecisionAnswer>(exception.Response.Answers[0]);
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task GetResponseAsync_MultipleUnansweredQuestionsAreAllNamed()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => new DecisionResponse(
            request.Questions.Select(static question => (DecisionAnswer)new UnansweredDecisionAnswer(question.Id, question.Kind)).ToList()));

        DecisionUnansweredException exception = await Assert.ThrowsAsync<DecisionUnansweredException>(
            () => client.GetResponseAsync([new TextContent("request")], definition));

        Assert.Equal(["category", "refundRequestProbability", "satisfaction"], exception.QuestionIds);
    }

    [Fact]
    public void DecisionUnansweredException_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>("response", () => new DecisionUnansweredException(null!, ["a"]));
        Assert.Throws<ArgumentNullException>("questionIds", () => new DecisionUnansweredException(new DecisionResponse(), null!));
    }

    [Theory]
    [InlineData("case")]
    [InlineData("missing")]
    [InlineData("reordered")]
    [InlineData("extra")]
    [InlineData("wrong-kind")]
    [InlineData("wrong-id")]
    [InlineData("count")]
    [InlineData("unanswered-wrong-kind")]
    [InlineData("score-levels")]
    public async Task GetResponseAsync_RejectsResponseNotMatchingSentRequest(string mutation)
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using RecordingDecisionClient client = new(request => CreateMismatchedResponse(request, mutation));

        await Assert.ThrowsAsync<DecisionProtocolException>(() => client.GetResponseAsync(
            new TypedDecisionState("request"),
            DecisionTestJsonContext.Default.TypedDecisionState,
            definition));
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task GetResponseAsync_PropagatesCancellation()
    {
        DecisionDefinition<TypedDecisionResult> definition = CreateTypedDefinition();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request), cancel: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetResponseAsync(
                new TypedDecisionState("cancelled"),
                DecisionTestJsonContext.Default.TypedDecisionState,
                definition,
                cancellationToken: cancellation.Token));
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task ExpectedScore_UsesDistributionAndScoreRequiresReportedValue()
    {
        DecisionDefinition<TypedDecisionResult> expectedDefinition = DecisionDefinition<TypedDecisionResult>.Create(
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability);
                builder.ExpectedScore(result => result.Satisfaction, CreateLevels());
            },
            DecisionTestJsonContext.Default.Options);
        using RecordingDecisionClient client = new(request => CreateTypedDefinitionResponse(request, reportedScore: null));

        DecisionResponse<TypedDecisionResult> expectedResponse = await client.GetResponseAsync(
            new TypedDecisionState("request"),
            DecisionTestJsonContext.Default.TypedDecisionState,
            expectedDefinition);

        Assert.Equal(1.6, expectedResponse.Result.Satisfaction, precision: 12);
        Assert.Null(((ScoreDecisionAnswer)expectedResponse.Response.Answers[2]).Score);

        await Assert.ThrowsAsync<DecisionProtocolException>(() => client.GetResponseAsync(
            new TypedDecisionState("request"),
            DecisionTestJsonContext.Default.TypedDecisionState,
            CreateTypedDefinition()));
    }

    [Fact]
    public void Create_RejectsNestedDuplicateFlagsAndReadOnlySelectors()
    {
        Assert.Throws<ArgumentException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            builder => builder.Choice(result => result.Nested.Category),
            DecisionTestJsonContext.Default.Options));

        Assert.Throws<ArgumentException>(() => DecisionDefinition<TypedDecisionResult>.Create(
            builder =>
            {
                builder.BinaryProbability(result => result.RefundRequestProbability);
                builder.Score(result => result.RefundRequestProbability, CreateLevels());
            },
            DecisionTestJsonContext.Default.Options));

        Assert.Throws<ArgumentException>(() => DecisionDefinition<FlagsResult>.Create(
            builder => builder.Choice(result => result.Value),
            DecisionTestJsonContext.Default.Options));

        Assert.Throws<ArgumentException>(() => DecisionDefinition<ReadOnlyResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability),
            DecisionTestJsonContext.Default.Options));
    }

    [Fact]
    public void Create_UsesPropertyDescriptionAndExplicitInstructionPrecedence()
    {
        DecisionDefinition<PropertyDescriptionResult> propertyDefinition = DecisionDefinition<PropertyDescriptionResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability),
            DecisionTestJsonContext.Default.Options);
        Assert.Equal("Description on the property.", GetInstructions(propertyDefinition.Questions[0]));

        DecisionDefinition<PropertyDescriptionResult> overrideDefinition = DecisionDefinition<PropertyDescriptionResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability, "Explicit instruction."),
            DecisionTestJsonContext.Default.Options);
        Assert.Equal("Explicit instruction.", GetInstructions(overrideDefinition.Questions[0]));
    }

    [Fact]
    public void Create_UsesConfiguredJsonPropertyName()
    {
        DecisionDefinition<JsonNamedResult> definition = DecisionDefinition<JsonNamedResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability),
            DecisionTestJsonContext.Default.Options);

        Assert.Equal("refund_probability", definition.Questions[0].Id);
    }

    [Fact]
    public async Task GetResponseAsync_RejectsRequiredUnmappedProperties()
    {
        DecisionDefinition<RequiredUnmappedResult> definition = DecisionDefinition<RequiredUnmappedResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability),
            DecisionTestJsonContext.Default.Options);
        using RecordingDecisionClient client = new(request => new DecisionResponse([new BinaryDecisionAnswer(request.Questions[0].Id, 0.25)]));

        await Assert.ThrowsAsync<DecisionProtocolException>(() => client.GetResponseAsync([new TextContent("request")], definition));
    }

    [Fact]
    public void Create_RejectsPropertySpecificEnumConverters()
    {
        Assert.Throws<NotSupportedException>(() => DecisionDefinition<PropertyConverterResult>.Create(
            builder => builder.Choice(result => result.Category),
            DecisionTestJsonContext.Default.Options));
    }

    [Fact]
    public async Task Create_UsesConfiguredEnumConverterForCandidateIds()
    {
        DecisionDefinition<CustomEnumResult> definition = DecisionDefinition<CustomEnumResult>.Create(
            builder => builder.Choice(result => result.Category),
            DecisionTestJsonContext.Default.Options);
        ChoiceDecisionQuestion question = Assert.IsType<ChoiceDecisionQuestion>(definition.Questions[0]);
        Assert.Equal(["billing-id", "technical-id", "account-id"], question.Candidates.Select(static candidate => candidate.Id));

        using RecordingDecisionClient client = new(request => new DecisionResponse(
        [
            new ChoiceDecisionAnswer(
                request.Questions[0].Id,
                "technical-id",
                [new("billing-id", 0.2), new("technical-id", 0.5), new("account-id", 0.3)]),
        ]));

        DecisionResponse<CustomEnumResult> response = await client.GetResponseAsync([new TextContent("request")], definition);
        Assert.Equal(CustomCategory.Technical, response.Result.Category);
    }

    [Fact]
    public async Task Create_RejectsMaterializationWhenSerializationGetterMasksActualValue()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = DecisionTestJsonContext.Default.WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(MaskedGetterResult))
                {
                    JsonPropertyInfo property = typeInfo.Properties.Single(static property => property.Name == nameof(MaskedGetterResult.Probability));
                    property.Set = static (obj, value) => ((MaskedGetterResult)obj).Probability = 0;
                    property.Get = static _ => 0.75;
                }
            }),
        };
        DecisionDefinition<MaskedGetterResult> definition = DecisionDefinition<MaskedGetterResult>.Create(
            builder => builder.BinaryProbability(result => result.Probability),
            options);
        using RecordingDecisionClient client = new(request => new DecisionResponse([new BinaryDecisionAnswer(request.Questions[0].Id, 0.75)]));

        await Assert.ThrowsAsync<DecisionProtocolException>(() => client.GetResponseAsync([new TextContent("request")], definition));
        Assert.False(options.IsReadOnly);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetResponseAsync_PreservesGlobalNumberHandlingAndIgnoreSettings(bool zero)
    {
        DecisionResponse<ConfiguredResult> response = await BindConfiguredResult(
            DecisionDefinition<ConfiguredResult>.Create(
                builder =>
                {
                    builder.BinaryProbability(result => result.Probability);
                    builder.Score(result => result.NativeScore, CreateLevels());
                    builder.ExpectedScore(result => result.ExpectedScore, CreateLevels());
                },
                DecisionConfiguredJsonContext.Default.Options),
            zero);

        Assert.Equal(zero ? 0 : 0.25, response.Result.Probability);
        Assert.Equal(zero ? 0 : 1.6, response.Result.NativeScore, precision: 12);
        Assert.Equal(zero ? 0 : 1.6, response.Result.ExpectedScore, precision: 12);
        Assert.Equal(3, response.Response.Answers.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetResponseAsync_PreservesPropertyNumberHandlingAndIgnoreSettings(bool zero)
    {
        DecisionResponse<PropertyConfiguredResult> response = await BindConfiguredResult(
            DecisionDefinition<PropertyConfiguredResult>.Create(
                builder =>
                {
                    builder.BinaryProbability(result => result.Probability);
                    builder.Score(result => result.NativeScore, CreateLevels());
                    builder.ExpectedScore(result => result.ExpectedScore, CreateLevels());
                },
                DecisionTestJsonContext.Default.Options),
            zero);

        Assert.Equal(zero ? 0 : 0.25, response.Result.Probability);
        Assert.Equal(zero ? 0 : 1.6, response.Result.NativeScore, precision: 12);
        Assert.Equal(zero ? 0 : 1.6, response.Result.ExpectedScore, precision: 12);
    }

    private static string GetInstructions(DecisionQuestion question) =>
        Assert.IsType<TextContent>(Assert.Single(question.Instructions)).Text;

    private static DecisionScoreLevel[] CreateLevels() =>
    [
        new("low", "Low"),
        new("medium", "Medium"),
        new("high", "High"),
    ];

    private static async Task<DecisionResponse<TResult>> BindConfiguredResult<TResult>(DecisionDefinition<TResult> definition, bool zero)
    {
        using RecordingDecisionClient client = new(request => CreateConfiguredSerializationResponse(request, zero));
        DecisionResponse<TResult> response = await client.GetResponseAsync([new TextContent("request")], definition);
        Assert.Equal(1, client.CallCount);
        return response;
    }

    private static DecisionResponse CreateConfiguredSerializationResponse(DecisionRequest request, bool zero)
    {
        double[] distribution = zero ? [1, 0, 0] : [0.1, 0.2, 0.7];
        return new DecisionResponse(
        [
            new BinaryDecisionAnswer(request.Questions[0].Id, zero ? 0 : 0.25),
            new ScoreDecisionAnswer(request.Questions[1].Id, zero ? 0 : 1.6, CreateDistribution((ScoreDecisionQuestion)request.Questions[1], distribution)),
            new ScoreDecisionAnswer(request.Questions[2].Id, null, CreateDistribution((ScoreDecisionQuestion)request.Questions[2], distribution)),
        ]);
    }

    private static DecisionProbability[] CreateDistribution(ScoreDecisionQuestion question, double[] values) =>
        values.Select((value, index) => new DecisionProbability(question.Levels[index].Id, value)).ToArray();

    private static DecisionDefinition<TypedDecisionResult> CreateTypedDefinition() =>
        DecisionDefinition<TypedDecisionResult>.Create(
            builder =>
            {
                builder.Choice(result => result.Category);
                builder.BinaryProbability(result => result.RefundRequestProbability);
                builder.Score(result => result.Satisfaction, CreateLevels());
            },
            DecisionTestJsonContext.Default.Options);

    private static DecisionResponse CreateTypedDefinitionResponse(DecisionRequest request, double? reportedScore = 1.6)
    {
        ChoiceDecisionQuestion choice = (ChoiceDecisionQuestion)request.Questions[0];
        return new DecisionResponse(
        [
            new ChoiceDecisionAnswer(
                choice.Id,
                choice.Candidates[1].Id,
                [new(choice.Candidates[0].Id, 0.2), new(choice.Candidates[1].Id, 0.5), new(choice.Candidates[2].Id, 0.3)]),
            new BinaryDecisionAnswer(request.Questions[1].Id, 0.25),
            new ScoreDecisionAnswer(request.Questions[2].Id, reportedScore, CreateDistribution((ScoreDecisionQuestion)request.Questions[2], [0.1, 0.2, 0.7])),
        ])
        {
            ModelId = "test-model",
        };
    }

    private static DecisionResponse CreateMismatchedResponse(DecisionRequest request, string mutation)
    {
        DecisionResponse response = CreateTypedDefinitionResponse(request);
        ChoiceDecisionQuestion choice = (ChoiceDecisionQuestion)request.Questions[0];
        switch (mutation)
        {
            case "case":
                response.Answers[0] = new ChoiceDecisionAnswer(choice.Id, "technical", [new("Billing", 0.2), new("technical", 0.5), new("Account", 0.3)]);
                break;
            case "missing":
                response.Answers[0] = new ChoiceDecisionAnswer(choice.Id, "Technical", [new("Billing", 0.5), new("Technical", 0.5)]);
                break;
            case "reordered":
                response.Answers[0] = new ChoiceDecisionAnswer(choice.Id, "Technical", [new("Technical", 0.5), new("Billing", 0.2), new("Account", 0.3)]);
                break;
            case "extra":
                response.Answers[0] = new ChoiceDecisionAnswer(
                    choice.Id,
                    "Technical",
                    [new("Billing", 0.2), new("Technical", 0.5), new("Account", 0.2), new("Other", 0.1)]);
                break;
            case "wrong-kind":
                response.Answers[0] = new BinaryDecisionAnswer(choice.Id, 0.5);
                break;
            case "wrong-id":
                response.Answers[1] = new BinaryDecisionAnswer("different-question", 0.25);
                break;
            case "count":
                response.Answers.RemoveAt(2);
                break;
            case "unanswered-wrong-kind":
                response.Answers[1] = new UnansweredDecisionAnswer(request.Questions[1].Id, DecisionKind.Score);
                break;
            case "score-levels":
                ScoreDecisionQuestion score = (ScoreDecisionQuestion)request.Questions[2];
                response.Answers[2] = new ScoreDecisionAnswer(score.Id, 1, [new(score.Levels[2].Id, 0.7), new(score.Levels[1].Id, 0.2), new(score.Levels[0].Id, 0.1)]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }

        return response;
    }

    private static DecisionResponse CreateMixedResponse() =>
        new(
        [
            new BinaryDecisionAnswer("binary", 0.75),
            new ChoiceDecisionAnswer("choice", "blue|candidate", [new("red|candidate", 0.2), new("blue|candidate", 0.5), new("green|candidate", 0.3)]),
            new ScoreDecisionAnswer("score", 1.6, [new("low", 0.1), new("medium", 0.2), new("high", 0.7)]),
        ]);

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

    [JsonConverter(typeof(CustomCategoryConverter))]
    internal enum CustomCategory
    {
        Billing,
        Technical,
        Account,
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

    internal sealed class ReadOnlyResult
    {
        public double Probability { get; }
    }

    internal sealed class RequiredUnmappedResult
    {
        public double Probability { get; set; }

        [JsonRequired]
        public string Required { get; set; } = null!;
    }

    internal sealed record CustomEnumResult(CustomCategory Category);

    internal sealed record PropertyConverterResult(
        [property: JsonConverter(typeof(CustomCategoryConverter))] CustomCategory Category);

    internal sealed record FlagsResult(FlagCategory Value);

    internal sealed class MaskedGetterResult
    {
        public double Probability { get; set; }
    }

    internal sealed class DecisionResult
    {
        public bool Approved { get; set; }
        public string? Selected { get; set; }
        public double Score { get; set; }
        public double ExpectedScore { get; set; }
    }

    internal sealed class ConfiguredResult
    {
        public double Probability { get; set; }
        public double NativeScore { get; set; }
        public double ExpectedScore { get; set; }
    }

    internal sealed class PropertyConfiguredResult
    {
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Probability { get; set; }

        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double NativeScore { get; set; }

        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double ExpectedScore { get; set; }
    }

    internal sealed class CustomCategoryConverter : JsonConverter<CustomCategory>
    {
        public override CustomCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetString() switch
            {
                "billing-id" => CustomCategory.Billing,
                "technical-id" => CustomCategory.Technical,
                "account-id" => CustomCategory.Account,
                _ => throw new JsonException(),
            };

        public override void Write(Utf8JsonWriter writer, CustomCategory value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value switch
            {
                CustomCategory.Billing => "billing-id",
                CustomCategory.Technical => "technical-id",
                CustomCategory.Account => "account-id",
                _ => throw new JsonException(),
            });
    }

    internal sealed class RecordingDecisionClient(Func<DecisionRequest, DecisionResponse> responseFactory, bool cancel = false) : IDecisionClient
    {
        public int CallCount { get; private set; }

        public DecisionOptions? Options { get; private set; }

        public DecisionRequest? LastRequest { get; private set; }

        public Task<DecisionResponse> GetResponseAsync(
            DecisionRequest request,
            DecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Options = options;
            LastRequest = request;
            if (cancel)
            {
                return Task.FromCanceled<DecisionResponse>(cancellationToken);
            }

            return Task.FromResult(responseFactory(request));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DecisionDefinitionTests.TypedDecisionResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.TypedDecisionState))]
[JsonSerializable(typeof(DecisionDefinitionTests.TicketCategory))]
[JsonSerializable(typeof(DecisionDefinitionTests.Classification))]
[JsonSerializable(typeof(DecisionDefinitionTests.FlagCategory))]
[JsonSerializable(typeof(DecisionDefinitionTests.CustomCategory))]
[JsonSerializable(typeof(DecisionDefinitionTests.FlagsResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.PropertyDescriptionResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.JsonNamedResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.ReadOnlyResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.RequiredUnmappedResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.CustomEnumResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.PropertyConverterResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.MaskedGetterResult))]
[JsonSerializable(typeof(DecisionDefinitionTests.PropertyConfiguredResult))]
internal sealed partial class DecisionTestJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(DecisionDefinitionTests.ConfiguredResult))]
internal sealed partial class DecisionConfiguredJsonContext : JsonSerializerContext;
