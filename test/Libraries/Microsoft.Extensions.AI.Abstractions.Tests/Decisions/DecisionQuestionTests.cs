// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using static Microsoft.Extensions.AI.DecisionTestData;

namespace Microsoft.Extensions.AI;

public class DecisionQuestionTests
{
    [Fact]
    public void Questions_AcceptTextOrContentForInstructionsAndCriteria()
    {
        BinaryDecisionQuestion textBinary = new("binary", "Is it urgent?", "Urgent", "Not urgent");
        Assert.Equal(DecisionKind.Binary, textBinary.Kind);
        Assert.Equal("Is it urgent?", Assert.IsType<TextContent>(Assert.Single(textBinary.Instructions)).Text);
        Assert.Equal("Urgent", Assert.IsType<TextContent>(Assert.Single(textBinary.TrueDescription!)).Text);

        BinaryDecisionQuestion descriptionsOnly = new("binary", trueDescription: "Yes", falseDescription: "No");
        Assert.Empty(descriptionsOnly.Instructions);

        DataContent reference = new(ImageBytes, "image/png");
        ChoiceDecisionQuestion contentChoice = new(
            "choice",
            [new DecisionCandidate("a", [new TextContent("Product A"), new UriContent("https://example.com/a.png", "image/png")]), new("b", "Product B")],
            [new TextContent("Which product is shown?"), reference]);
        Assert.Same(reference, contentChoice.Instructions[1]);
        Assert.IsType<UriContent>(contentChoice.Candidates[0].Description[1]);
        Assert.Equal("Product B", Assert.IsType<TextContent>(Assert.Single(contentChoice.Candidates[1].Description)).Text);

        ScoreDecisionQuestion score = new("score", [new("low", "Low"), new("high", [new TextContent("High")])]);
        Assert.Empty(score.Instructions);
        Assert.Equal(DecisionKind.Score, score.Kind);
    }

    [Fact]
    public void ChoiceAndScore_InstructionsAreOptionalAndFollowCandidatesOrLevels()
    {
        DecisionCandidate[] candidates = [new("a", "A"), new("b", "B")];
        DecisionScoreLevel[] levels = [new("low", "Low"), new("high", "High")];

        Assert.Empty(new ChoiceDecisionQuestion("choice", candidates).Instructions);
        Assert.Empty(new ChoiceDecisionQuestion("choice", candidates, (string?)null).Instructions);
        Assert.Empty(new ChoiceDecisionQuestion("choice", candidates, (IList<AIContent>?)null).Instructions);
        Assert.Equal("Pick", Assert.IsType<TextContent>(Assert.Single(new ChoiceDecisionQuestion("choice", candidates, "Pick").Instructions)).Text);
        Assert.Equal("Pick", Assert.IsType<TextContent>(Assert.Single(new ChoiceDecisionQuestion("choice", candidates, instructions: "Pick").Instructions)).Text);

        Assert.Empty(new ScoreDecisionQuestion("score", levels).Instructions);
        Assert.Empty(new ScoreDecisionQuestion("score", levels, (string?)null).Instructions);
        Assert.Empty(new ScoreDecisionQuestion("score", levels, (IList<AIContent>?)null).Instructions);
        Assert.Equal("Rate", Assert.IsType<TextContent>(Assert.Single(new ScoreDecisionQuestion("score", levels, "Rate").Instructions)).Text);
        Assert.Equal("Rate", Assert.IsType<TextContent>(Assert.Single(new ScoreDecisionQuestion("score", levels, [new TextContent("Rate")]).Instructions)).Text);
    }

    [Fact]
    public void Questions_SnapshotContentAndRejectInvalidShapes()
    {
        List<AIContent> instructions = [new TextContent("Original")];
        ChoiceDecisionQuestion question = new("choice", [new("a", "A")], instructions);
        instructions.Add(new TextContent("Added later"));
        Assert.Single(question.Instructions);

        Assert.Throws<ArgumentException>(() => new BinaryDecisionQuestion("binary"));
        Assert.Throws<ArgumentException>(() => new BinaryDecisionQuestion("binary", trueDescription: "Only true"));
        Assert.ThrowsAny<ArgumentException>(() => new BinaryDecisionQuestion(" ", "Instructions"));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionQuestion("choice", [], "Pick"));
        Assert.Throws<ArgumentException>(() => new ChoiceDecisionQuestion("choice", [new("a", "A"), new("a", "Again")], "Pick"));
        Assert.Throws<ArgumentException>(() => new ScoreDecisionQuestion("score", [new("only", "Only")], "Rate"));
        Assert.Throws<ArgumentException>(() => new ScoreDecisionQuestion("score", [new("x", "X"), new("x", "Again")], "Rate"));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionCandidate("a", " "));
        Assert.Throws<ArgumentException>(() => new DecisionCandidate("a", Array.Empty<AIContent>()));
        Assert.Throws<ArgumentException>(() => new DecisionScoreLevel("a", [null!]));
    }

    [Fact]
    public void Questions_ContentListsAreReadOnlySnapshots()
    {
        BinaryDecisionQuestion binary = new("binary", "Is it urgent?", "Yes", "No");
        ChoiceDecisionQuestion choice = new("choice", [new("a", "A"), new("b", "B")], "Pick");
        ScoreDecisionQuestion score = new("score", [new("low", "Low"), new("high", "High")]);

        IList<AIContent>[] lists =
        [
            binary.Instructions, binary.TrueDescription!, binary.FalseDescription!,
            choice.Instructions, choice.Candidates[0].Description,
            score.Instructions, score.Levels[0].Description,
        ];
        foreach (IList<AIContent> list in lists)
        {
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(new TextContent("x")));
        }
    }

    [Fact]
    public void Json_RoundTripsQuestionsUsingDefaultOptions()
    {
        DecisionQuestion[] questions =
        [
            new BinaryDecisionQuestion("binary", "Is it urgent?", "Yes", "No"),
            new ChoiceDecisionQuestion("choice", [new("a", "A"), new("b", [new TextContent("B"), new DataContent(ImageBytes, "image/png")])]),
            new ScoreDecisionQuestion("score", [new("low", "Low"), new("high", "High")], "Rate"),
        ];

        DecisionQuestion[] roundTripped = questions
            .Select(static q => JsonSerializer.Deserialize<DecisionQuestion>(JsonSerializer.Serialize(q, AIJsonUtilities.DefaultOptions), AIJsonUtilities.DefaultOptions)!)
            .ToArray();

        BinaryDecisionQuestion binary = Assert.IsType<BinaryDecisionQuestion>(roundTripped[0]);
        Assert.Equal("No", Assert.IsType<TextContent>(Assert.Single(binary.FalseDescription!)).Text);
        Assert.True(binary.Instructions.IsReadOnly);

        ChoiceDecisionQuestion choice = Assert.IsType<ChoiceDecisionQuestion>(roundTripped[1]);
        Assert.Empty(choice.Instructions);
        Assert.IsType<DataContent>(choice.Candidates[1].Description[1]);

        ScoreDecisionQuestion score = Assert.IsType<ScoreDecisionQuestion>(roundTripped[2]);
        Assert.Equal("Rate", Assert.IsType<TextContent>(Assert.Single(score.Instructions)).Text);
        Assert.Equal(["low", "high"], score.Levels.Select(static l => l.Id));
    }
}