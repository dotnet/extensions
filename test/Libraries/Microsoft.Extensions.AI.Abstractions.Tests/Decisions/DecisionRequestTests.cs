// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using static Microsoft.Extensions.AI.DecisionTestData;

namespace Microsoft.Extensions.AI;

public class DecisionRequestTests
{
    [Fact]
    public void Constructor_RequiresNonemptyUniqueQuestions()
    {
        DecisionQuestion question = new BinaryDecisionQuestion("q", "Question?");

        DecisionRequest request = new([question]);
        Assert.Same(question, Assert.Single(request.Questions));
        Assert.Null(request.State);
        Assert.Empty(request.Contents);

        Assert.Throws<ArgumentNullException>("questions", () => new DecisionRequest(null!));
        Assert.Throws<ArgumentException>("questions", () => new DecisionRequest([]));
        Assert.Throws<ArgumentException>("questions", () => new DecisionRequest([null!]));
        Assert.Throws<ArgumentException>("questions", () => new DecisionRequest([question, new BinaryDecisionQuestion("q", "Again?")]));
    }

    [Fact]
    public void Constructor_SnapshotsQuestions()
    {
        List<DecisionQuestion> questions = [new BinaryDecisionQuestion("q", "Question?")];
        DecisionRequest request = new(questions);
        questions.Add(new BinaryDecisionQuestion("later", "Later?"));

        Assert.Single(request.Questions);
    }

    [Fact]
    public void State_ClonesValueAndDistinguishesAbsentFromJsonNull()
    {
        DecisionRequest request = new([new BinaryDecisionQuestion("q", "Question?")]);

        JsonElement state;
        using (JsonDocument document = JsonDocument.Parse("""{"subject":"Refund"}"""))
        {
            request.State = document.RootElement;
            state = request.State!.Value;
        }

        Assert.Equal("Refund", state.GetProperty("subject").GetString());

        request.State = JsonSerializer.SerializeToElement<object?>(null, AIJsonUtilities.DefaultOptions);
        Assert.Equal(JsonValueKind.Null, request.State!.Value.ValueKind);

        request.State = null;
        Assert.Null(request.State);

        Assert.Throws<ArgumentException>("value", () => request.State = default(JsonElement));
        Assert.Null(request.State);
    }

    [Fact]
    public void Contents_SnapshotsAndIsReadOnly()
    {
        DecisionRequest request = new([new BinaryDecisionQuestion("q", "Question?")]);
        DataContent image = new(ImageBytes, "image/png");
        List<AIContent> contents = [image];

        request.Contents = contents;
        contents.Add(new TextContent("Added later"));

        Assert.Same(image, Assert.Single(request.Contents));
        Assert.NotSame(contents, request.Contents);
        Assert.True(request.Contents.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => request.Contents.Add(new TextContent("x")));
        Assert.Throws<NotSupportedException>(() => request.Contents[0] = new TextContent("x"));

        request.Contents = null;
        Assert.Empty(request.Contents);

        request.Contents = [image];
        Assert.Throws<ArgumentException>("value", () => request.Contents = [null!]);
        Assert.Same(image, Assert.Single(request.Contents));
    }

    [Fact]
    public void Request_AllowsIncompleteInputsBecauseProvidersValidateAtExecution()
    {
        DecisionRequest request = new([new BinaryDecisionQuestion("q", "Question?")]) { Contents = [] };

        Assert.Null(request.State);
        Assert.Empty(request.Contents);
    }

    [Fact]
    public void Json_RoundTripsMultimodalRequestsUsingDefaultOptions()
    {
        DecisionRequest request = CreateRequest();
        request.Contents =
        [
            new TextContent("Customer message"),
            new DataContent(ImageBytes, "image/png"),
            new UriContent("https://example.com/receipt.jpg", "image/jpeg"),
            new DataContent(ImageBytes, "application/octet-stream"),
        ];

        string json = JsonSerializer.Serialize(request, AIJsonUtilities.DefaultOptions);
        DecisionRequest roundTripped = JsonSerializer.Deserialize<DecisionRequest>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal(3, roundTripped.State!.Value.GetProperty("priority").GetInt32());
        Assert.True(roundTripped.Contents.IsReadOnly);
        Assert.Collection(
            roundTripped.Contents,
            c => Assert.Equal("Customer message", Assert.IsType<TextContent>(c).Text),
            c => Assert.Equal("image/png", Assert.IsType<DataContent>(c).MediaType),
            c => Assert.Equal("image/jpeg", Assert.IsType<UriContent>(c).MediaType),
            c => Assert.Equal(ImageBytes, Assert.IsType<DataContent>(c).Data.ToArray()));
        Assert.Collection(
            roundTripped.Questions,
            q => Assert.Equal("Yes", Assert.IsType<TextContent>(Assert.Single(Assert.IsType<BinaryDecisionQuestion>(q).TrueDescription!)).Text),
            q => Assert.IsType<DataContent>(Assert.IsType<ChoiceDecisionQuestion>(q).Candidates[1].Description[1]),
            q => Assert.Equal(["low", "medium", "high"], Assert.IsType<ScoreDecisionQuestion>(q).Levels.Select(static l => l.Id)));
    }

    [Fact]
    public void Json_RoundTripsStateOnlyAndContentsOnlyRequests()
    {
        DecisionQuestion[] questions = [new BinaryDecisionQuestion("q", "Question?")];

        DecisionRequest stateOnly = new(questions) { State = TicketStateElement };
        string stateJson = JsonSerializer.Serialize(stateOnly, AIJsonUtilities.DefaultOptions);
        DecisionRequest stateRoundTripped = JsonSerializer.Deserialize<DecisionRequest>(stateJson, AIJsonUtilities.DefaultOptions)!;
        Assert.Equal("Refund", stateRoundTripped.State!.Value.GetProperty("subject").GetString());
        Assert.Empty(stateRoundTripped.Contents);

        DecisionRequest contentsOnly = new(questions) { Contents = [new DataContent(ImageBytes, "image/png")] };
        string contentsJson = JsonSerializer.Serialize(contentsOnly, AIJsonUtilities.DefaultOptions);
        using (JsonDocument document = JsonDocument.Parse(contentsJson))
        {
            Assert.False(document.RootElement.TryGetProperty("state", out _));
        }

        DecisionRequest contentsRoundTripped = JsonSerializer.Deserialize<DecisionRequest>(contentsJson, AIJsonUtilities.DefaultOptions)!;
        Assert.Null(contentsRoundTripped.State);
        Assert.IsType<DataContent>(Assert.Single(contentsRoundTripped.Contents));
    }

    [Fact]
    public void Json_WritesExplicitNullStateAndReadsItAsAbsent()
    {
        DecisionRequest request = new([new BinaryDecisionQuestion("q", "Question?")])
        {
            State = JsonSerializer.SerializeToElement<object?>(null, AIJsonUtilities.DefaultOptions),
        };

        string json = JsonSerializer.Serialize(request, AIJsonUtilities.DefaultOptions);
        DecisionRequest roundTripped = JsonSerializer.Deserialize<DecisionRequest>(json, AIJsonUtilities.DefaultOptions)!;

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("state").ValueKind);
        Assert.Null(roundTripped.State);
    }

    [Fact]
    public void Json_RoundTripsCustomContentRegisteredByTheApplication()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(AIJsonUtilities.DefaultOptions.TypeInfoResolver, TestJsonSerializerContext.Default),
        };
        options.AddAIContentType<CustomContent>("decisionTestCustom");

        DecisionRequest request = new([new BinaryDecisionQuestion("q", "Question?")]) { Contents = [new CustomContent { Value = "custom" }] };
        string json = JsonSerializer.Serialize(request, options);
        DecisionRequest roundTripped = JsonSerializer.Deserialize<DecisionRequest>(json, options)!;

        Assert.Contains("decisionTestCustom", json, StringComparison.Ordinal);
        Assert.Equal("custom", Assert.IsType<CustomContent>(Assert.Single(roundTripped.Contents)).Value);
    }
}