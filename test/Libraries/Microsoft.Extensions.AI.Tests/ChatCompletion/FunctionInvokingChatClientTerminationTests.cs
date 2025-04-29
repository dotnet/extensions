// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionInvokingChatClientTerminationTests
{
    [Fact]
    public async Task SingleFunction_ShouldTerminate_EndsProcessingImmediately()
    {
        // Arrange
        var functionCallCount = 0;
        var function = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            functionCallCount++;
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return $"Result {functionCallCount}";
        }, "TerminatingFunction");

        var options = new ChatOptions { Tools = [function] };

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "TerminatingFunction", new Dictionary<string, object?> { { "shouldTerminate", true } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),

            // The following messages should not be processed because the function requested termination
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "TerminatingFunction", new Dictionary<string, object?> { { "shouldTerminate", false } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        // The expected result should only include messages up to the terminating function call
        List<ChatMessage> expectedResult =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "TerminatingFunction", new Dictionary<string, object?> { { "shouldTerminate", true } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
        ];

        List<ChatMessage> expectedStreamingResult =
        [
            new ChatMessage(ChatRole.Tool, [
                new FunctionCallContent("callId1", "TerminatingFunction", new Dictionary<string, object?> { { "shouldTerminate", true } }),
                new FunctionResultContent("callId1", result: "Result 1")
            ]),
        ];

        // Act & Assert
        await InvokeAndAssertAsync(options, plan, expectedResult);
        Assert.Equal(1, functionCallCount); // Only the first function should be called

        // Reset counter for streaming test
        functionCallCount = 0;
        await InvokeAndAssertStreamingAsync(options, plan, expectedStreamingResult);
        Assert.Equal(1, functionCallCount); // Only the first function should be called
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultipleFunctions_ShouldTerminate_EndsProcessingImmediately(bool allowConcurrentInvocation)
    {
        // Arrange
        var function1CallCount = 0;
        var function2CallCount = 0;

        var function1 = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            function1CallCount++;
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return $"Result from function1: {function1CallCount}";
        }, "TerminatingFunction1");

        var function2 = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            function2CallCount++;
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return $"Result from function2: {function2CallCount}";
        }, "TerminatingFunction2");

        var options = new ChatOptions
        {
            Tools = [function1, function2]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = b => b.Use(
            s => new FunctionInvokingChatClient(s) { AllowConcurrentInvocation = allowConcurrentInvocation });

        // Create a plan where the second function in the first batch terminates
        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "TerminatingFunction1", new Dictionary<string, object?> { { "shouldTerminate", false } }),
                new FunctionCallContent("callId2", "TerminatingFunction2", new Dictionary<string, object?> { { "shouldTerminate", true } })
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Result from function1: 1"),
                new FunctionResultContent("callId2", result: "Result from function2: 1")
            ]),

            // The following messages should not be processed because the function requested termination
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "TerminatingFunction1", new Dictionary<string, object?> { { "shouldTerminate", false } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result from function1: 2")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        // The expected result should only include messages up to the terminating function call
        List<ChatMessage> expectedResult =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "TerminatingFunction1", new Dictionary<string, object?> { { "shouldTerminate", false } }),
                new FunctionCallContent("callId2", "TerminatingFunction2", new Dictionary<string, object?> { { "shouldTerminate", true } })
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Result from function1: 1"),
                new FunctionResultContent("callId2", result: "Result from function2: 1")
            ]),
        ];

        // Act & Assert
        await InvokeAndAssertAsync(options, plan, expectedResult, builderFactory: configurePipeline);

        // Both functions in the first batch should be called
        Assert.Equal(1, function1CallCount);
        Assert.Equal(1, function2CallCount);

        // Reset counters for streaming test
        function1CallCount = 0;
        function2CallCount = 0;

        await InvokeAndAssertStreamingAsync(options, plan, expectedResult, configurePipeline: configurePipeline);

        // Both functions in the first batch should be called
        Assert.Equal(1, function1CallCount);
        Assert.Equal(1, function2CallCount);
    }

    [Theory(Skip = "Termination behavior and results to be concluded.")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultipleFunctions_SerialExecution_EarlyTermination(bool allowConcurrentInvocation)
    {
        // Arrange
        var executionOrder = new List<string>();

        var function1 = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            executionOrder.Add("Function1");
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return "Result from function1";
        }, "TerminatingFunction1");

        var function2 = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            executionOrder.Add("Function2");
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return "Result from function2";
        }, "TerminatingFunction2");

        var function3 = AIFunctionFactory.Create((bool shouldTerminate) =>
        {
            executionOrder.Add("Function3");
            if (shouldTerminate)
            {
                FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            }

            return "Result from function3";
        }, "TerminatingFunction3");

        var options = new ChatOptions
        {
            Tools = [function1, function2, function3]
        };

        Func<ChatClientBuilder, ChatClientBuilder> builderFactory = b => b.Use(
            s => new FunctionInvokingChatClient(s) { AllowConcurrentInvocation = allowConcurrentInvocation });

        // Create a plan where the second function terminates
        List<ChatMessage> responsePlan =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "TerminatingFunction1", new Dictionary<string, object?> { { "shouldTerminate", false } }),
                new FunctionCallContent("callId2", "TerminatingFunction2", new Dictionary<string, object?> { { "shouldTerminate", true } }),
                new FunctionCallContent("callId3", "TerminatingFunction3", new Dictionary<string, object?> { { "shouldTerminate", false } })
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId4", "TerminatingFunction1", new Dictionary<string, object?> { { "shouldTerminate", false } })
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        // Act
        await InvokeAndAssertAsync(options, responsePlan, builderFactory: builderFactory);

        // Assert
        if (allowConcurrentInvocation)
        {
            // When concurrent, all functions might be called before we check termination
            Assert.Contains("Function1", executionOrder);
            Assert.Contains("Function2", executionOrder);
            Assert.Contains("Function3", executionOrder);
        }
        else
        {
            // When serial, we should only call Function1 and Function2 (which terminates)
            Assert.Equal(new[] { "Function1", "Function2" }, executionOrder);
        }

        // Reset for streaming test
        executionOrder.Clear();

        await InvokeAndAssertStreamingAsync(options, responsePlan, configurePipeline: builderFactory);

        if (allowConcurrentInvocation)
        {
            // When concurrent, all functions might be called before we check termination
            Assert.Contains("Function1", executionOrder);
            Assert.Contains("Function2", executionOrder);

            // Function3 might or might not be called depending on timing
        }
        else
        {
            // When serial, we should only call Function1 and Function2 (which terminates)
            Assert.Equal(new[] { "Function1", "Function2" }, executionOrder);
        }
    }

    #region Test Helpers

    private static async Task InvokeAndAssertAsync(
        ChatOptions options,
        List<ChatMessage> plan,
        List<ChatMessage>? expectedResult = null,
        Func<ChatClientBuilder, ChatClientBuilder>? builderFactory = null)
    {
        int currentIndex = 0;
        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatContents, chatOptions, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(plan[currentIndex++]));
            }
        };

        builderFactory ??= bf => bf;
        IChatClient service = builderFactory(innerClient.AsBuilder()).UseFunctionInvocation().Build();

        var result = await service.GetResponseAsync(new ChatMessage(ChatRole.User, "hello"), options);

        if (expectedResult != null)
        {
            // Verify the result matches the expected result
            Assert.Equal(expectedResult.Count, result.Messages.Count);
            for (int i = 0; i < expectedResult.Count; i++)
            {
                Assert.Equal(expectedResult[i].Role, result.Messages[i].Role);
                Assert.Equal(expectedResult[i].Contents.Count, result.Messages[i].Contents.Count);
            }
        }
    }

    private static async Task InvokeAndAssertStreamingAsync(
        ChatOptions options,
        List<ChatMessage> plan,
        List<ChatMessage>? expectedResult = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsyncCallback(
            IEnumerable<ChatMessage> chatContents,
            ChatOptions? chatOptions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            var index = chatContents.Count();
            if (index >= plan.Count)
            {
                throw new InvalidOperationException($"Test plan does not include a response for message index {index}");
            }

            yield return new ChatResponseUpdate
            {
                Contents = plan[index].Contents,
                Role = plan[index].Role
            };
        }

        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = GetStreamingResponseAsyncCallback,
        };

        configurePipeline ??= b => b;
        IChatClient service = configurePipeline(innerClient.AsBuilder()).UseFunctionInvocation().Build();

        var result = await service.GetStreamingResponseAsync(new ChatMessage(ChatRole.User, "hello"), options).ToChatResponseAsync();

        if (expectedResult != null)
        {
            // Verify the result matches the expected result
            Assert.Equal(expectedResult.Count, result.Messages.Count);
            for (int i = 0; i < expectedResult.Count; i++)
            {
                Assert.Equal(expectedResult[i].Role, result.Messages[i].Role);
                Assert.Equal(expectedResult[i].Contents.Count, result.Messages[i].Contents.Count);
            }
        }
    }

    #endregion
}
