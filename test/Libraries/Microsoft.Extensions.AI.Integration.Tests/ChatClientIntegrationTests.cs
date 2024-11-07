﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.TestUtilities;
using OpenTelemetry.Trace;
using Xunit;

#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA2214 // Do not call overridable methods in constructors

namespace Microsoft.Extensions.AI;

public abstract class ChatClientIntegrationTests : IDisposable
{
    private readonly IChatClient? _chatClient;

    protected ChatClientIntegrationTests()
    {
        _chatClient = CreateChatClient();
    }

    public void Dispose()
    {
        _chatClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract IChatClient? CreateChatClient();

    [ConditionalFact]
    public virtual async Task CompleteAsync_SingleRequestMessage()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync("What's the biggest animal?");

        Assert.Contains("whale", response.Message.Text, StringComparison.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_MultipleRequestMessages()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync(
        [
            new(ChatRole.User, "Pick a city, any city"),
            new(ChatRole.Assistant, "Seattle"),
            new(ChatRole.User, "And another one"),
            new(ChatRole.Assistant, "Jakarta"),
            new(ChatRole.User, "What continent are they each in?"),
        ]);

        Assert.Single(response.Choices);
        Assert.Contains("America", response.Message.Text);
        Assert.Contains("Asia", response.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task CompleteStreamingAsync_SingleStreamingResponseChoice()
    {
        SkipIfNotEnabled();

        IList<ChatMessage> chatHistory =
        [
            new(ChatRole.User, "Quote, word for word, Neil Armstrong's famous words.")
        ];

        StringBuilder sb = new();
        await foreach (var chunk in _chatClient.CompleteStreamingAsync(chatHistory))
        {
            sb.Append(chunk.Text);
        }

        string responseText = sb.ToString();
        Assert.Contains("one small step", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("one giant leap", responseText, StringComparison.OrdinalIgnoreCase);

        // The input list is left unaugmented.
        Assert.Single(chatHistory);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_UsageDataAvailable()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync("Explain in 10 words how AI works");

        Assert.Single(response.Choices);
        Assert.True(response.Usage?.InputTokenCount > 1);
        Assert.True(response.Usage?.OutputTokenCount > 1);
        Assert.Equal(response.Usage?.InputTokenCount + response.Usage?.OutputTokenCount, response.Usage?.TotalTokenCount);
    }

    [ConditionalFact]
    public virtual async Task CompleteStreamingAsync_UsageDataAvailable()
    {
        SkipIfNotEnabled();

        var response = _chatClient.CompleteStreamingAsync("Explain in 10 words how AI works", new()
        {
            AdditionalProperties = new()
            {
                ["stream_options"] = new Dictionary<string, object> { ["include_usage"] = true, },
            },
        });

        List<StreamingChatCompletionUpdate> chunks = [];
        await foreach (var chunk in response)
        {
            chunks.Add(chunk);
        }

        Assert.True(chunks.Count > 1);

        UsageContent usage = chunks.SelectMany(c => c.Contents).OfType<UsageContent>().Single();
        Assert.True(usage.Details.InputTokenCount > 1);
        Assert.True(usage.Details.OutputTokenCount > 1);
        Assert.Equal(usage.Details.InputTokenCount + usage.Details.OutputTokenCount, usage.Details.TotalTokenCount);
    }

    protected virtual string? GetModel_MultiModal_DescribeImage() => null;

    [ConditionalFact]
    public virtual async Task MultiModal_DescribeImage()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync(
            [
                new(ChatRole.User,
                [
                    new TextContent("What does this logo say?"),
                    new ImageContent(GetImageDataUri()),
                ])
            ],
            new() { ModelId = GetModel_MultiModal_DescribeImage() });

        Assert.Single(response.Choices);
        Assert.True(response.Message.Text?.IndexOf("net", StringComparison.OrdinalIgnoreCase) >= 0, response.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task FunctionInvocation_AutomaticallyInvokeFunction_Parameterless()
    {
        SkipIfNotEnabled();

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        int secretNumber = 42;

        var response = await chatClient.CompleteAsync("What is the current secret number?", new()
        {
            Tools = [AIFunctionFactory.Create(() => secretNumber, "GetSecretNumber")]
        });

        Assert.Single(response.Choices);
        Assert.Contains(secretNumber.ToString(), response.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task FunctionInvocation_AutomaticallyInvokeFunction_WithParameters_NonStreaming()
    {
        SkipIfNotEnabled();

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        var response = await chatClient.CompleteAsync("What is the result of SecretComputation on 42 and 84?", new()
        {
            Tools = [AIFunctionFactory.Create((int a, int b) => a * b, "SecretComputation")]
        });

        Assert.Single(response.Choices);
        Assert.Contains("3528", response.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task FunctionInvocation_AutomaticallyInvokeFunction_WithParameters_Streaming()
    {
        SkipIfNotEnabled();

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        var response = chatClient.CompleteStreamingAsync("What is the result of SecretComputation on 42 and 84?", new()
        {
            Tools = [AIFunctionFactory.Create((int a, int b) => a * b, "SecretComputation")]
        });

        StringBuilder sb = new();
        await foreach (var chunk in response)
        {
            sb.Append(chunk.Text);
        }

        Assert.Contains("3528", sb.ToString());
    }

    protected virtual bool SupportsParallelFunctionCalling => true;

    [ConditionalFact]
    public virtual async Task FunctionInvocation_SupportsMultipleParallelRequests()
    {
        SkipIfNotEnabled();
        if (!SupportsParallelFunctionCalling)
        {
            throw new SkipTestException("Parallel function calling is not supported by this chat client");
        }

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        // The service/model isn't guaranteed to request two calls to GetPersonAge in the same turn, but it's common that it will.
        var response = await chatClient.CompleteAsync("How much older is Elsa than Anna? Return the age difference as a single number.", new()
        {
            Tools = [AIFunctionFactory.Create((string personName) =>
            {
                return personName switch
                {
                    "Elsa" => 21,
                    "Anna" => 18,
                    _ => 30,
                };
            }, "GetPersonAge")]
        });

        Assert.True(
            Regex.IsMatch(response.Message.Text ?? "", @"\b(3|three)\b", RegexOptions.IgnoreCase),
            $"Doesn't contain three: {response.Message.Text}");
    }

    [ConditionalFact]
    public virtual async Task FunctionInvocation_RequireAny()
    {
        SkipIfNotEnabled();

        int callCount = 0;
        var tool = AIFunctionFactory.Create(() =>
        {
            callCount++;
            return 123;
        }, "GetSecretNumber");

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        var response = await chatClient.CompleteAsync("Are birds real?", new()
        {
            Tools = [tool],
            ToolMode = ChatToolMode.RequireAny,
        });

        Assert.Single(response.Choices);
        Assert.True(callCount >= 1);
    }

    [ConditionalFact]
    public virtual async Task FunctionInvocation_RequireSpecific()
    {
        SkipIfNotEnabled();

        bool shieldsUp = false;
        var getSecretNumberTool = AIFunctionFactory.Create(() => 123, "GetSecretNumber");
        var shieldsUpTool = AIFunctionFactory.Create(() => shieldsUp = true, "ShieldsUp");

        using var chatClient = new FunctionInvokingChatClient(_chatClient);

        // Even though the user doesn't ask for the shields to be activated, verify that the tool is invoked
        var response = await chatClient.CompleteAsync("What's the current secret number?", new()
        {
            Tools = [getSecretNumberTool, shieldsUpTool],
            ToolMode = ChatToolMode.RequireSpecific(shieldsUpTool.Metadata.Name),
        });

        Assert.True(shieldsUp);
    }

    [ConditionalFact]
    public virtual async Task Caching_OutputVariesWithoutCaching()
    {
        SkipIfNotEnabled();

        var message = new ChatMessage(ChatRole.User, "Pick a random number, uniformly distributed between 1 and 1000000");
        var firstResponse = await _chatClient.CompleteAsync([message]);
        Assert.Single(firstResponse.Choices);

        var secondResponse = await _chatClient.CompleteAsync([message]);
        Assert.NotEqual(firstResponse.Message.Text, secondResponse.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task Caching_SamePromptResultsInCacheHit_NonStreaming()
    {
        SkipIfNotEnabled();

        using var chatClient = new DistributedCachingChatClient(
            _chatClient,
            new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())));

        var message = new ChatMessage(ChatRole.User, "Pick a random number, uniformly distributed between 1 and 1000000");
        var firstResponse = await chatClient.CompleteAsync([message]);
        Assert.Single(firstResponse.Choices);

        // No matter what it said before, we should see identical output due to caching
        for (int i = 0; i < 3; i++)
        {
            var secondResponse = await chatClient.CompleteAsync([message]);
            Assert.Equal(firstResponse.Message.Text, secondResponse.Message.Text);
        }

        // ... but if the conversation differs, we should see different output
        message.Text += "!";
        var thirdResponse = await chatClient.CompleteAsync([message]);
        Assert.NotEqual(firstResponse.Message.Text, thirdResponse.Message.Text);
    }

    [ConditionalFact]
    public virtual async Task Caching_SamePromptResultsInCacheHit_Streaming()
    {
        SkipIfNotEnabled();

        using var chatClient = new DistributedCachingChatClient(
            _chatClient,
            new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())));

        var message = new ChatMessage(ChatRole.User, "Pick a random number, uniformly distributed between 1 and 1000000");
        StringBuilder orig = new();
        await foreach (var update in chatClient.CompleteStreamingAsync([message]))
        {
            orig.Append(update.Text);
        }

        // No matter what it said before, we should see identical output due to caching
        for (int i = 0; i < 3; i++)
        {
            StringBuilder second = new();
            await foreach (var update in chatClient.CompleteStreamingAsync([message]))
            {
                second.Append(update.Text);
            }

            Assert.Equal(orig.ToString(), second.ToString());
        }

        // ... but if the conversation differs, we should see different output
        message.Text += "!";
        StringBuilder third = new();
        await foreach (var update in chatClient.CompleteStreamingAsync([message]))
        {
            third.Append(update.Text);
        }

        Assert.NotEqual(orig.ToString(), third.ToString());
    }

    [ConditionalFact]
    public virtual async Task Caching_BeforeFunctionInvocation_AvoidsExtraCalls()
    {
        SkipIfNotEnabled();

        int functionCallCount = 0;
        var getTemperature = AIFunctionFactory.Create([Description("Gets the current temperature")] () =>
        {
            functionCallCount++;
            return $"{100 + functionCallCount} degrees celsius";
        }, "GetTemperature");

        // First call executes the function and calls the LLM
        using var chatClient = new ChatClientBuilder()
            .ConfigureOptions(options => options.Tools = [getTemperature])
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .UseFunctionInvocation()
            .UseCallCounting()
            .Use(CreateChatClient()!);

        var llmCallCount = chatClient.GetService<CallCountingChatClient>();
        var message = new ChatMessage(ChatRole.User, "What is the temperature?");
        var response = await chatClient.CompleteAsync([message]);
        Assert.Contains("101", response.Message.Text);

        // First LLM call tells us to call the function, second deals with the result
        Assert.Equal(2, llmCallCount!.CallCount);

        // Second call doesn't execute the function or call the LLM, but rather just returns the cached result
        var secondResponse = await chatClient.CompleteAsync([message]);
        Assert.Equal(response.Message.Text, secondResponse.Message.Text);
        Assert.Equal(1, functionCallCount);
        Assert.Equal(2, llmCallCount!.CallCount);
    }

    [ConditionalFact]
    public virtual async Task Caching_AfterFunctionInvocation_FunctionOutputUnchangedAsync()
    {
        SkipIfNotEnabled();

        // This means that if the function call produces the same result, we can avoid calling the LLM
        // whereas if the function call produces a different result, we do call the LLM

        var functionCallCount = 0;
        var getTemperature = AIFunctionFactory.Create([Description("Gets the current temperature")] () =>
        {
            functionCallCount++;
            return "58 degrees celsius";
        }, "GetTemperature");

        // First call executes the function and calls the LLM
        using var chatClient = new ChatClientBuilder()
            .ConfigureOptions(options => options.Tools = [getTemperature])
            .UseFunctionInvocation()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .UseCallCounting()
            .Use(CreateChatClient()!);

        var llmCallCount = chatClient.GetService<CallCountingChatClient>();
        var message = new ChatMessage(ChatRole.User, "What is the temperature?");
        var response = await chatClient.CompleteAsync([message]);
        Assert.Contains("58", response.Message.Text);

        // First LLM call tells us to call the function, second deals with the result
        Assert.Equal(1, functionCallCount);
        Assert.Equal(2, llmCallCount!.CallCount);

        // Second time, the calls to the LLM don't happen, but the function is called again
        var secondResponse = await chatClient.CompleteAsync([message]);
        Assert.Equal(response.Message.Text, secondResponse.Message.Text);
        Assert.Equal(2, functionCallCount);
        Assert.Equal(2, llmCallCount!.CallCount);
    }

    [ConditionalFact]
    public virtual async Task Caching_AfterFunctionInvocation_FunctionOutputChangedAsync()
    {
        SkipIfNotEnabled();

        // This means that if the function call produces the same result, we can avoid calling the LLM
        // whereas if the function call produces a different result, we do call the LLM

        var functionCallCount = 0;
        var getTemperature = AIFunctionFactory.Create([Description("Gets the current temperature")] () =>
        {
            functionCallCount++;
            return $"{80 + functionCallCount} degrees celsius";
        }, "GetTemperature");

        // First call executes the function and calls the LLM
        using var chatClient = new ChatClientBuilder()
            .ConfigureOptions(options => options.Tools = [getTemperature])
            .UseFunctionInvocation()
            .UseDistributedCache(new MemoryDistributedCache(Options.Options.Create(new MemoryDistributedCacheOptions())))
            .UseCallCounting()
            .Use(CreateChatClient()!);

        var llmCallCount = chatClient.GetService<CallCountingChatClient>();
        var message = new ChatMessage(ChatRole.User, "What is the temperature?");
        var response = await chatClient.CompleteAsync([message]);
        Assert.Contains("81", response.Message.Text);

        // First LLM call tells us to call the function, second deals with the result
        Assert.Equal(1, functionCallCount);
        Assert.Equal(2, llmCallCount!.CallCount);

        // Second time, the first call to the LLM don't happen, but the function is called again,
        // and since its output now differs, we no longer hit the cache so the second LLM call does happen
        var secondResponse = await chatClient.CompleteAsync([message]);
        Assert.Contains("82", secondResponse.Message.Text);
        Assert.Equal(2, functionCallCount);
        Assert.Equal(3, llmCallCount!.CallCount);
    }

    [ConditionalFact]
    public virtual async Task Logging_LogsCalls_NonStreaming()
    {
        SkipIfNotEnabled();

        CapturingLogger logger = new();

        using var chatClient =
            new LoggingChatClient(CreateChatClient()!, logger);

        await chatClient.CompleteAsync([new(ChatRole.User, "What's the biggest animal?")]);

        Assert.Collection(logger.Entries,
            entry => Assert.Contains("What\\u0027s the biggest animal?", entry.Message),
            entry => Assert.Contains("whale", entry.Message));
    }

    [ConditionalFact]
    public virtual async Task Logging_LogsCalls_Streaming()
    {
        SkipIfNotEnabled();

        CapturingLogger logger = new();

        using var chatClient =
            new LoggingChatClient(CreateChatClient()!, logger);

        await foreach (var update in chatClient.CompleteStreamingAsync("What's the biggest animal?"))
        {
            // Do nothing with the updates
        }

        Assert.Contains(logger.Entries, e => e.Message.Contains("What\\u0027s the biggest animal?"));
        Assert.Contains(logger.Entries, e => e.Message.Contains("whale"));
    }

    [ConditionalFact]
    public virtual async Task Logging_LogsFunctionCalls_NonStreaming()
    {
        SkipIfNotEnabled();

        CapturingLogger logger = new();

        using var chatClient =
            new FunctionInvokingChatClient(
                new LoggingChatClient(CreateChatClient()!, logger));

        int secretNumber = 42;
        await chatClient.CompleteAsync(
            "What is the current secret number?",
            new ChatOptions { Tools = [AIFunctionFactory.Create(() => secretNumber, "GetSecretNumber")] });

        Assert.Collection(logger.Entries,
            entry => Assert.Contains("What is the current secret number?", entry.Message),
            entry => Assert.Contains("\"name\": \"GetSecretNumber\"", entry.Message),
            entry => Assert.Contains($"\"result\": {secretNumber}", entry.Message),
            entry => Assert.Contains(secretNumber.ToString(), entry.Message));
    }

    [ConditionalFact]
    public virtual async Task Logging_LogsFunctionCalls_Streaming()
    {
        SkipIfNotEnabled();

        CapturingLogger logger = new();

        using var chatClient =
            new FunctionInvokingChatClient(
                new LoggingChatClient(CreateChatClient()!, logger));

        int secretNumber = 42;
        await foreach (var update in chatClient.CompleteStreamingAsync(
            "What is the current secret number?",
            new ChatOptions { Tools = [AIFunctionFactory.Create(() => secretNumber, "GetSecretNumber")] }))
        {
            // Do nothing with the updates
        }

        Assert.Contains(logger.Entries, e => e.Message.Contains("What is the current secret number?"));
        Assert.Contains(logger.Entries, e => e.Message.Contains("\"name\": \"GetSecretNumber\""));
        Assert.Contains(logger.Entries, e => e.Message.Contains($"\"result\": {secretNumber}"));
    }

    [ConditionalFact]
    public virtual async Task OpenTelemetry_CanEmitTracesAndMetrics()
    {
        SkipIfNotEnabled();

        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var chatClient = new ChatClientBuilder()
            .UseOpenTelemetry(sourceName: sourceName)
            .Use(CreateChatClient()!);

        var response = await chatClient.CompleteAsync([new(ChatRole.User, "What's the biggest animal?")]);

        var activity = Assert.Single(activities);
        Assert.StartsWith("chat", activity.DisplayName);
        Assert.StartsWith("http", (string)activity.GetTagItem("server.address")!);
        Assert.Equal(chatClient.Metadata.ProviderUri?.Port, (int)activity.GetTagItem("server.port")!);
        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);
        Assert.NotEqual(0, (int)activity.GetTagItem("gen_ai.response.input_tokens")!);
        Assert.NotEqual(0, (int)activity.GetTagItem("gen_ai.response.output_tokens")!);

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutput()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<Person>("""
            Who is described in the following sentence?
            Jimbo Smith is a 35-year-old programmer from Cardiff, Wales.
            """);

        Assert.Equal("Jimbo Smith", response.Result.FullName);
        Assert.Equal(35, response.Result.AgeInYears);
        Assert.Contains("Cardiff", response.Result.HomeTown);
        Assert.Equal(JobType.Programmer, response.Result.Job);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputArray()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<Person[]>("""
            Who are described in the following sentence?
            Jimbo Smith is a 35-year-old software developer from Cardiff, Wales.
            Josh Simpson is a 25-year-old software developer from Newport, Wales.
            """);

        Assert.Equal(2, response.Result.Length);
        Assert.Contains(response.Result, x => x.FullName == "Jimbo Smith");
        Assert.Contains(response.Result, x => x.FullName == "Josh Simpson");
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputInteger()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<int>("""
            There were 14 abstractions for AI programming, which was too many.
            To fix this we added another one. How many are there now?
            """);

        Assert.Equal(15, response.Result);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputString()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<string>("""
            The software developer, Jimbo Smith, is a 35-year-old from Cardiff, Wales.
            What's his full name?
            """);

        Assert.Equal("Jimbo Smith", response.Result);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputBool_True()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<bool>("""
            Jimbo Smith is a 35-year-old software developer from Cardiff, Wales.
            Is there at least one software developer from Cardiff?
            """);

        Assert.True(response.Result);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputBool_False()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<bool>("""
            Jimbo Smith is a 35-year-old software developer from Cardiff, Wales.
            Can we be sure that he is a medical doctor?
            """);

        Assert.False(response.Result);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutputEnum()
    {
        SkipIfNotEnabled();

        var response = await _chatClient.CompleteAsync<Architecture>("""
            I'm using a Macbook Pro with an M2 chip. What architecture am I using?
            """);

        Assert.Equal(Architecture.Arm64, response.Result);
    }

    [ConditionalFact]
    public virtual async Task CompleteAsync_StructuredOutput_WithFunctions()
    {
        SkipIfNotEnabled();

        var expectedPerson = new Person
        {
            FullName = "Jimbo Smith",
            AgeInYears = 35,
            HomeTown = "Cardiff",
            Job = JobType.Programmer,
        };

        using var chatClient = new FunctionInvokingChatClient(_chatClient);
        var response = await chatClient.CompleteAsync<Person>(
            "Who is person with ID 123?", new ChatOptions
            {
                Tools = [AIFunctionFactory.Create((int personId) =>
                {
                    Assert.Equal(123, personId);
                    return expectedPerson;
                }, "GetPersonById")]
            });

        Assert.NotSame(expectedPerson, response.Result);
        Assert.Equal(expectedPerson.FullName, response.Result.FullName);
        Assert.Equal(expectedPerson.AgeInYears, response.Result.AgeInYears);
        Assert.Equal(expectedPerson.HomeTown, response.Result.HomeTown);
        Assert.Equal(expectedPerson.Job, response.Result.Job);
    }

    private class Person
    {
#pragma warning disable S1144, S3459 // Unassigned members should be removed
        public string? FullName { get; set; }
        public int AgeInYears { get; set; }
        public string? HomeTown { get; set; }
        public JobType Job { get; set; }
#pragma warning restore S1144, S3459 // Unused private types or members should be removed
    }

    private enum JobType
    {
        Surgeon,
        PopStar,
        Programmer,
        Unknown,
    }

    private static Uri GetImageDataUri()
    {
        using Stream? s = typeof(ChatClientIntegrationTests).Assembly.GetManifestResourceStream("Microsoft.Extensions.AI.dotnet.png");
        Assert.NotNull(s);
        MemoryStream ms = new();
        s.CopyTo(ms);
        return new Uri($"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}");
    }

    [MemberNotNull(nameof(_chatClient))]
    protected void SkipIfNotEnabled()
    {
        if (_chatClient is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }
}
