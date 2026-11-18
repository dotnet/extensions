// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Microsoft.Extensions.AI;

public class OpenAIDecisionClientIntegrationTests
{
    private readonly IDecisionClient? _client;

    public OpenAIDecisionClientIntegrationTests()
    {
        string? model = TestRunnerConfiguration.Instance["OpenAI:DecisionModel"];
        if (model is not null)
        {
            _client = IntegrationTestHelpers.GetOpenAIClient()?.GetDecisionsClient().AsIDecisionClient(model);
        }
    }

    [Fact]
    public async Task GetResponseAsync_AnswersAllQuestionKinds()
    {
        SkipIfNotEnabled();

        using JsonDocument state = JsonDocument.Parse("""{ "order": { "status": "delivered", "daysLate": 6, "customerTier": "gold" } }""");
        DecisionRequest request = new(
            state.RootElement,
            [
                new BinaryDecisionQuestion("is_late", "Was the order delivered late?"),
                new ChoiceDecisionQuestion(
                    "action",
                    "Which action should support take?",
                    [new DecisionCandidate("refund", "Issue a refund."), new DecisionCandidate("apology", "Send an apology."), new DecisionCandidate("none", "Take no action.")]),
                new ScoreDecisionQuestion(
                    "severity",
                    "How severe is the delay for the customer?",
                    [new DecisionScoreLevel("low", "Barely noticeable."), new DecisionScoreLevel("medium", "Inconvenient."), new DecisionScoreLevel("high", "Very disruptive.")]),
            ],
            [new TextContent("Gold-tier customers receive priority handling.")]);

        DecisionResponse response = await _client!.GetResponseAsync(request);

        Assert.Equal(3, response.Answers.Count);
        BinaryDecisionAnswer late = Assert.IsType<BinaryDecisionAnswer>(response.Answers[0]);
        Assert.True(late.TrueProbability > 0.5);
        ChoiceDecisionAnswer action = Assert.IsType<ChoiceDecisionAnswer>(response.Answers[1]);
        Assert.Equal(3, action.Probabilities.Count);
        ScoreDecisionAnswer severity = Assert.IsType<ScoreDecisionAnswer>(response.Answers[2]);
        Assert.Equal(3, severity.Probabilities.Count);
        Assert.NotNull(response.Usage);
        Assert.NotNull(response.ModelId);
    }

    [Fact]
    public async Task GetResponseAsync_AcceptsImageContent()
    {
        SkipIfNotEnabled();

        DecisionRequest request = new(
            null,
            [new BinaryDecisionQuestion("is_purple", "Is the dominant color of the image purple?")],
            [new DataContent(ImageDataUri.GetImageDataUri(), "image/png")]);

        DecisionResponse response = await _client!.GetResponseAsync(request);

        BinaryDecisionAnswer answer = Assert.IsType<BinaryDecisionAnswer>(Assert.Single(response.Answers));
        Assert.Equal("is_purple", answer.QuestionId);
        Assert.True(answer.TrueProbability > 0.5);
    }

    private void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];
        Assert.SkipUnless(skipIntegration is null && _client is not null, "Decision client is not enabled.");
    }
}
