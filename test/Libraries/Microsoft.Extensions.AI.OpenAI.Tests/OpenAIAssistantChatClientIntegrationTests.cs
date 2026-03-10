// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable S1135 // Track uses of "TODO" tags
#pragma warning disable xUnit1013 // Public method should be marked as test

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TestUtilities;
using OpenAI.Assistants;
using Xunit;

namespace Microsoft.Extensions.AI;

#pragma warning disable OPENAI001 // OpenAI Assistant APIs are experimental

public class OpenAIAssistantChatClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient()
    {
        var openAIClient = IntegrationTestHelpers.GetOpenAIClient();
        if (openAIClient is null)
        {
            return null;
        }

        AssistantClient ac = openAIClient.GetAssistantClient();
        var assistant =
            ac.GetAssistants().FirstOrDefault() ??
            ac.CreateAssistant("gpt-4o-mini");

        return ac.AsIChatClient(assistant.Id);
    }

    public override bool FunctionInvokingChatClientSetsConversationId => true;

    // These tests aren't written in a way that works well with threads.
    public override Task Caching_AfterFunctionInvocation_FunctionOutputChangedAsync() => Task.CompletedTask;
    public override Task Caching_AfterFunctionInvocation_FunctionOutputUnchangedAsync() => Task.CompletedTask;

    // Assistants doesn't support data URIs.
    public override Task MultiModal_DescribeImage() => Task.CompletedTask;
    public override Task MultiModal_DescribePdf() => Task.CompletedTask;

    [ConditionalFact]
    public async Task UseCodeInterpreter_ProducesCodeExecutionResults()
    {
        SkipIfNotEnabled();

        var response = await ChatClient.GetResponseAsync("Use the code interpreter to calculate the square root of 42.", new()
        {
            Tools = [new HostedCodeInterpreterTool()],
        });
        Assert.NotNull(response);

        ChatMessage message = Assert.Single(response.Messages);

        Assert.NotEmpty(message.Text);

        // Validate CodeInterpreterToolCallContent
        var toolCallContent = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolCallContent>().SingleOrDefault();
        Assert.NotNull(toolCallContent);
        if (toolCallContent.CallId is not null)
        {
            Assert.NotEmpty(toolCallContent.CallId);
        }

        if (toolCallContent.Inputs is not null)
        {
            Assert.NotEmpty(toolCallContent.Inputs);
            if (toolCallContent.Inputs.OfType<DataContent>().FirstOrDefault() is { } codeInput)
            {
                Assert.Equal("text/x-python", codeInput.MediaType);
                Assert.NotEmpty(codeInput.Data.ToArray());
            }
        }

        // Validate CodeInterpreterToolResultContent (when present)
        var toolResultContents = response.Messages.SelectMany(m => m.Contents).OfType<CodeInterpreterToolResultContent>().ToList();
        foreach (var toolResultContent in toolResultContents)
        {
            if (toolResultContent.CallId is not null)
            {
                Assert.NotEmpty(toolResultContent.CallId);
            }

            if (toolResultContent.Outputs is not null)
            {
                Assert.NotEmpty(toolResultContent.Outputs);
                if (toolResultContent.Outputs.OfType<TextContent>().FirstOrDefault() is { } resultOutput)
                {
                    Assert.NotEmpty(resultOutput.Text);
                }
            }
        }
    }

    // [Fact] // uncomment and run to clear out _all_ threads in your OpenAI account
    public async Task DeleteAllThreads()
    {
        using HttpClient client = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        // These values need to be filled in. The bearer token needs to be sniffed from a browser
        // session interacting with the dashboard (e.g. use F12 networking tools to look at request headers
        // made to "https://api.openai.com/v1/threads?limit=10" after clicking on Assistants | Threads in the
        // OpenAI portal dashboard).
        client.DefaultRequestHeaders.Add("authorization", $"Bearer sess-ENTERYOURSESSIONTOKEN");
        client.DefaultRequestHeaders.Add("openai-organization", "org-ENTERYOURORGID");
        client.DefaultRequestHeaders.Add("openai-project", "proj_ENTERYOURPROJECTID");

        AssistantClient ac = new(Environment.GetEnvironmentVariable("AI:OpenAI:ApiKey")!);
        while (true)
        {
            string listing = await client.GetStringAsync("https://api.openai.com/v1/threads?limit=100");

            var matches = Regex.Matches(listing, @"thread_\w+");
            if (matches.Count == 0)
            {
                break;
            }

            foreach (Match m in matches)
            {
                var dr = await ac.DeleteThreadAsync(m.Value);
                Assert.True(dr.Value.Deleted);
            }
        }
    }
}
