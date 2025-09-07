// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using OpenAI.Responses;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIResponseClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.GetOpenAIResponseClient(TestRunnerConfiguration.Instance["OpenAI:ChatModel"] ?? "gpt-4o-mini")
        .AsIChatClient();

    public override bool FunctionInvokingChatClientSetsConversationId => true;

    // Test structure doesn't make sense with Responses.
    public override Task Caching_AfterFunctionInvocation_FunctionOutputUnchangedAsync() => Task.CompletedTask;

    [ConditionalFact]
    public async Task UseWebSearch_AnnotationsReflectResults()
    {
        SkipIfNotEnabled();

        var response = await ChatClient.GetResponseAsync(
            "Write a paragraph about .NET based on at least three recent news articles. Cite your sources.",
            new() { Tools = [new HostedWebSearchTool()] });

        ChatMessage m = Assert.Single(response.Messages);
        TextContent tc = m.Contents.OfType<TextContent>().First();
        Assert.NotNull(tc.Annotations);
        Assert.NotEmpty(tc.Annotations);
        Assert.All(tc.Annotations, a =>
        {
            CitationAnnotation ca = Assert.IsType<CitationAnnotation>(a);
            var regions = Assert.IsType<List<AnnotatedRegion>>(ca.AnnotatedRegions);
            Assert.NotNull(regions);
            Assert.Single(regions);
            var region = Assert.IsType<TextSpanAnnotatedRegion>(regions[0]);
            Assert.NotNull(region);
            Assert.NotNull(region.StartIndex);
            Assert.NotNull(region.EndIndex);
            Assert.NotNull(ca.Url);
            Assert.NotNull(ca.Title);
            Assert.NotEmpty(ca.Title);
        });
    }

    [Fact]
    public async Task RemoteMCP_ListTools()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            // Replace this with HostedMcpServerTool once that's exposed.
            // https://github.com/openai/openai-dotnet/issues/406
            RawRepresentationFactory = (_) =>
            {
                var r = new ResponseCreationOptions();
                r.Tools.Add(GetInternalMcpTool("wiki_tools", "https://mcp.deepwiki.com/mcp"));
                return r;
            }
        };

        ChatResponse response = await CreateChatClient()!.GetResponseAsync("Which tools are available on the wiki_tools MCP server?", chatOptions);

        Assert.Contains("read_wiki_structure", response.Text);
        Assert.Contains("read_wiki_contents", response.Text);
        Assert.Contains("ask_question", response.Text);
    }

    [Fact]
    public async Task RemoteMCP_CallTool()
    {
        SkipIfNotEnabled();

        ChatOptions chatOptions = new()
        {
            // Replace this with HostedMcpServerTool once that's exposed.
            // https://github.com/openai/openai-dotnet/issues/406
            RawRepresentationFactory = (_) =>
            {
                var r = new ResponseCreationOptions();
                r.Tools.Add(GetInternalMcpTool("deepwiki", "https://mcp.deepwiki.com/mcp"));
                return r;
            }
        };

        ChatResponse response = await CreateChatClient()!.GetResponseAsync(
            "Tell me the path to the README.md file for Microsoft.Extensions.AI.Abstractions in the dotnet/extensions repository",
            chatOptions);

        Assert.Contains("src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md", response.Text);

        Type t = GetInternalOpenAIType("OpenAI.Responses.InternalMCPCallItemResource")!;
        IEnumerable<AIContent> contents = response.Messages
            .SelectMany(m => m.Contents
            .Where(c => (c.RawRepresentation?.GetType().Equals(t) ?? false) &&
                t.GetProperty("Name")!.GetValue(c.RawRepresentation)!.Equals("ask_question")));

        object rawRepresentation = Assert.Single(contents).RawRepresentation!;
        string callId = (string)t.GetProperty("Id")!.GetValue(rawRepresentation)!;

        HostedMcpServerToolCallContent mcpToolCall = new(
            callId,
            (string)t.GetProperty("Name")!.GetValue(rawRepresentation)!,
            (string)t.GetProperty("ServerLabel")!.GetValue(rawRepresentation)!,
            JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>((string)t.GetProperty("Arguments")!.GetValue(rawRepresentation)!))
        {
            RawRepresentation = rawRepresentation
        };

        HostedMcpServerToolResultContent mcpToolResult = new(callId)
        {
            Output = (string?)t.GetProperty("Output")!.GetValue(rawRepresentation),
            Error = (string?)t.GetProperty("Error")!.GetValue(rawRepresentation)
        };

        Assert.NotNull(mcpToolResult.Output);
        Assert.Null(mcpToolResult.Error);

        Assert.Equal("ask_question", mcpToolCall.Name);
        Assert.Equal("deepwiki", mcpToolCall.ServerName);
        Assert.NotNull(mcpToolCall.Arguments);
        Assert.Equal("dotnet/extensions", mcpToolCall.Arguments["repoName"]?.ToString());
        Assert.True(mcpToolCall.Arguments.ContainsKey("question"));
    }

    private static Type GetInternalOpenAIType(string fqName)
        => typeof(ResponseTool).Assembly.GetType(fqName)!;

    private static ResponseTool GetInternalMcpTool(string name, string url)
    {
        Type mcpToolType = GetInternalOpenAIType("OpenAI.Responses.InternalMCPTool")!;
        object instance = Activator.CreateInstance(mcpToolType, name, url)!;

        // Disable approvals until we have the necessary abstraction.
        mcpToolType.GetProperty("RequireApproval")?.SetValue(instance, BinaryData.FromString("\"never\""));

        return (ResponseTool)instance;
    }
}
