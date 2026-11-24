// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Extensions.AI.DecisionTestData;

namespace Microsoft.Extensions.AI;

public class DecisionClientExtensionsTests
{
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
            TestJsonSerializerContext.Default.Options,
            CreateRequest().Questions,
            [new DataContent(ImageBytes, "image/png")],
            options,
            cts.Token);

        Assert.Same(expected, response);
        Assert.Equal("Refund", client.Request!.State!.Value.GetProperty("subject").GetString());
        Assert.IsType<DataContent>(Assert.Single(client.Request.Contents));
        Assert.Same(options, client.Options);
        Assert.Equal(cts.Token, client.CancellationToken);
    }

    [Fact]
    public async Task GetDecisionAsync_DefaultOptions_SerializesState()
    {
        using RecordingDecisionClient client = new(_ => CreateMixedResponse());
        DecisionQuestion[] questions = [new BinaryDecisionQuestion("binary", "Is it late?")];

        _ = await client.GetDecisionAsync("late order", questions, [new DataContent(ImageBytes, "image/png")]);
        Assert.Equal(JsonValueKind.String, client.Request!.State!.Value.ValueKind);
        Assert.Equal("late order", client.Request.State.Value.GetString());
        Assert.IsType<DataContent>(Assert.Single(client.Request.Contents));
        Assert.Same(questions[0], Assert.Single(client.Request.Questions));

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            _ = await client.GetDecisionAsync(new OrderState("delivered", 6), questions);
            Assert.Equal("delivered", client.Request!.State!.Value.GetProperty("status").GetString());
            Assert.Equal(6, client.Request.State.Value.GetProperty("daysLate").GetInt32());
            Assert.Empty(client.Request.Contents);
        }
        else
        {
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => client.GetDecisionAsync(new OrderState("delivered", 6), questions));
        }
    }

    [Fact]
    public async Task GetDecisionAsync_SerializerOptions_UsesSuppliedOptions()
    {
        using RecordingDecisionClient client = new(_ => CreateMixedResponse());
        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = new UpperCaseNamingPolicy(),
            TypeInfoResolver = TestJsonSerializerContext.Default,
        };
        DecisionQuestion[] questions = [new BinaryDecisionQuestion("binary", "Is it late?")];

        _ = await client.GetDecisionAsync(new OrderState("delivered", 6), serializerOptions, questions);

        Assert.Equal("delivered", client.Request!.State!.Value.GetProperty("STATUS").GetString());
        Assert.Equal(6, client.Request.State.Value.GetProperty("DAYSLATE").GetInt32());
    }

    [Fact]
    public async Task GetDecisionAsync_State_ValidatesArguments()
    {
        using RecordingDecisionClient client = new(_ => CreateMixedResponse());
        DecisionQuestion[] questions = [new BinaryDecisionQuestion("binary", "Is it late?")];
        OrderState state = new("delivered", 6);

        _ = await Assert.ThrowsAsync<ArgumentNullException>("client", () => ((IDecisionClient)null!).GetDecisionAsync(state, questions));
        _ = await Assert.ThrowsAsync<ArgumentNullException>("questions", () => client.GetDecisionAsync(state, null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>("serializerOptions", () => client.GetDecisionAsync(state, null!, questions));

        List<AIContent> contentList = [new TextContent("late")];
        AIContent[] contentArray = [new TextContent("late")];
        _ = await Assert.ThrowsAsync<ArgumentException>("state", () => client.GetDecisionAsync(new TextContent("late"), questions));
        _ = await Assert.ThrowsAsync<ArgumentException>("state", () => client.GetDecisionAsync(contentList, questions));
        _ = await Assert.ThrowsAsync<ArgumentException>("state", () => client.GetDecisionAsync(contentArray, questions));
        _ = await Assert.ThrowsAsync<ArgumentException>("state", () => client.GetDecisionAsync<object>(contentList, questions));
        _ = await Assert.ThrowsAsync<ArgumentException>("state", () => client.GetDecisionAsync(contentList, AIJsonUtilities.DefaultOptions, questions));
        Assert.Null(client.Request);
    }

    private sealed class UpperCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToUpperInvariant();
    }
}