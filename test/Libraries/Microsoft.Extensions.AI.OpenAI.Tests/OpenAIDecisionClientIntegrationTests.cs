// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
    public async Task GetDecisionAsync_AnswersAllQuestionKinds()
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

        DecisionResponse response = await _client!.GetDecisionAsync(request);

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
    public async Task GetDecisionAsync_AcceptsImageContent()
    {
        SkipIfNotEnabled();

        DecisionRequest request = new(
            null,
            [new BinaryDecisionQuestion("is_purple", "Is the dominant color of the image purple?")],
            [new DataContent(ImageDataUri.GetImageDataUri(), "image/png")]);

        DecisionResponse response = await _client!.GetDecisionAsync(request);

        BinaryDecisionAnswer answer = Assert.IsType<BinaryDecisionAnswer>(Assert.Single(response.Answers));
        Assert.Equal("is_purple", answer.QuestionId);
        Assert.True(answer.TrueProbability > 0.5);
    }

    [Fact]
    public async Task GetDecisionAsync_RecordState_AnswersAllQuestionKinds()
    {
        SkipIfNotEnabled();

        DecisionResponse response = await _client!.GetDecisionAsync(new Order("delivered", 6, "gold"), CreateOrderQuestions());

        Assert.Equal(3, response.Answers.Count);
        Assert.True(Assert.IsType<BinaryDecisionAnswer>(response.Answers[0]).TrueProbability > 0.5);
        AssertSaneProbabilities(response);
    }

    [Fact]
    public async Task GetDecisionAsync_StringState_AnswersQuestion()
    {
        SkipIfNotEnabled();

        DecisionResponse response = await _client!.GetDecisionAsync(
            "The order was promised for Monday and delivered the following Sunday.",
            [new BinaryDecisionQuestion("is_late", "Was the order delivered late?")]);

        BinaryDecisionAnswer answer = Assert.IsType<BinaryDecisionAnswer>(Assert.Single(response.Answers));
        Assert.Equal("is_late", answer.QuestionId);
        Assert.True(answer.TrueProbability > 0.5);
        AssertSaneProbabilities(response);
    }

    [Fact]
    public async Task GetDecisionAsync_RecordStateWithImageContents_AnswersQuestions()
    {
        SkipIfNotEnabled();

        DecisionResponse response = await _client!.GetDecisionAsync(
            new ColorCheck("purple"),
            [
                new BinaryDecisionQuestion("matches", "Is the dominant color of the image the expected color in the state?"),
                new ChoiceDecisionQuestion(
                    "color",
                    "What is the dominant color of the image?",
                    [new DecisionCandidate("purple", "Purple."), new DecisionCandidate("green", "Green."), new DecisionCandidate("orange", "Orange.")]),
            ],
            contents: [new DataContent(ImageDataUri.GetImageDataUri(), "image/png")]);

        Assert.Equal(2, response.Answers.Count);
        Assert.True(Assert.IsType<BinaryDecisionAnswer>(response.Answers[0]).TrueProbability > 0.5);
        Assert.Equal("purple", Assert.IsType<ChoiceDecisionAnswer>(response.Answers[1]).SelectedCandidateId);
        AssertSaneProbabilities(response);
    }

    private static DecisionQuestion[] CreateOrderQuestions() =>
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
    ];

    private static void AssertSaneProbabilities(DecisionResponse response)
    {
        foreach (DecisionAnswer answer in response.Answers)
        {
            switch (answer)
            {
                case BinaryDecisionAnswer binary:
                    Assert.InRange(binary.TrueProbability, 0, 1);
                    break;
                case ChoiceDecisionAnswer choice:
                    Assert.All(choice.Probabilities, p => Assert.InRange(p.Value, 0, 1));
                    Assert.InRange(choice.Probabilities.Sum(p => p.Value), 0.98, 1.02);
                    break;
                case ScoreDecisionAnswer score:
                    Assert.All(score.Probabilities, p => Assert.InRange(p.Value, 0, 1));
                    Assert.InRange(score.Probabilities.Sum(p => p.Value), 0.98, 1.02);
                    break;
                default:
                    Assert.Fail($"Unexpected answer {answer.GetType().Name} for '{answer.QuestionId}'.");
                    break;
            }
        }
    }

    private void SkipIfNotEnabled()
    {
        string? skipIntegration = TestRunnerConfiguration.Instance["SkipIntegrationTests"];
        Assert.SkipUnless(skipIntegration is null && _client is not null, "Decision client is not enabled.");
    }

    private sealed class Order(string status, int daysLate, string customerTier)
    {
        public string Status { get; } = status;
        public int DaysLate { get; } = daysLate;
        public string CustomerTier { get; } = customerTier;
    }

    private sealed class ColorCheck(string expectedColor)
    {
        public string ExpectedColor { get; } = expectedColor;
    }
}
