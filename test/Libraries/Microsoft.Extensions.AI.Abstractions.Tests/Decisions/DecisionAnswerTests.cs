// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionAnswerTests
{
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
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionAnswer("choice", "a", [new("a", 0.5), new("a", 0.5)]));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionAnswer("choice", "a", [null!]));
    }

    [Fact]
    public void Answers_AcceptPartialDistributionsBecauseFitIsCheckedAgainstTheQuestion()
    {
        Assert.Empty(new ChoiceDecisionAnswer("choice", "a", []).Probabilities);
        Assert.Equal("c", new ChoiceDecisionAnswer("choice", "c", [new("a", 0.5), new("b", 0.5)]).SelectedCandidateId);
        Assert.Equal(7, new ScoreDecisionAnswer("score", 7, [new("low", 0.5), new("high", 0.5)]).Score);
        Assert.Empty(new ScoreDecisionAnswer("score", 1, []).Probabilities);
    }

    [Fact]
    public void ScoreAnswer_ComputesExpectedScoreByLevelIdAndValidatesScore()
    {
        ScoreDecisionQuestion question = new("score", [new("low", "Low"), new("medium", "Medium"), new("high", "High")], "How satisfied?");

        ScoreDecisionAnswer score = new("score", 1, [new("low", 0.2), new("medium", 0.2), new("high", 0.4)]);
        Assert.Equal(1.25, score.GetExpectedScore(question)!.Value, 10);
        Assert.Equal(1, score.Score);

        ScoreDecisionAnswer sortedByProbability = new("score", null, [new("high", 0.4), new("low", 0.2), new("medium", 0.2)]);
        Assert.Equal(1.25, sortedByProbability.GetExpectedScore(question)!.Value, 10);

        ScoreDecisionAnswer partialWithUnknown = new("score", null, [new("high", 0.5), new("other", 0.5)]);
        Assert.Equal(2, partialWithUnknown.GetExpectedScore(question)!.Value, 10);

        Assert.Null(new ScoreDecisionAnswer("score", null, [new("low", 0), new("high", 0)]).GetExpectedScore(question));
        Assert.Null(new ScoreDecisionAnswer("score", 1, []).GetExpectedScore(question));
        Assert.Throws<ArgumentNullException>(() => score.GetExpectedScore(null!));
        Assert.Throws<ArgumentException>(() => score.GetExpectedScore(new ScoreDecisionQuestion("other", question.Levels, "Other?")));

        Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreDecisionAnswer("score", -1, [new("low", 0.5), new("high", 0.5)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreDecisionAnswer("score", double.NaN, [new("low", 0.5), new("high", 0.5)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreDecisionAnswer("score", double.PositiveInfinity, []));
    }
}