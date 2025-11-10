// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGeneratingChatClientTests
{
    [Fact]
    public void ImageGeneratingChatClient_InvalidArgs_Throws()
    {
        using var innerClient = new TestChatClient();
        using var imageGenerator = new TestImageGenerator();

        Assert.Throws<ArgumentNullException>("innerClient", () => new ImageGeneratingChatClient(null!, imageGenerator));
        Assert.Throws<ArgumentNullException>("imageGenerator", () => new ImageGeneratingChatClient(innerClient, null!));
    }

    [Fact]
    public void UseImageGeneration_WithNullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>("builder", () => ((ChatClientBuilder)null!).UseImageGeneration());
    }

    [Fact]
    public async Task GetResponseAsync_WithoutImageGenerationTool_PassesThrough()
    {
        // Arrange
        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response")));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "dummy function", name: "DummyFunction")]
        };

        // Act
        var response = await client.GetResponseAsync([new(ChatRole.User, "test")], chatOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test response", response.Messages[0].Text);

        // Verify that tools collection still has the original function, not replaced
        Assert.Single(chatOptions.Tools);
        Assert.IsAssignableFrom<AIFunction>(chatOptions.Tools[0]);
    }

    [Fact]
    public async Task GetResponseAsync_WithImageGenerationTool_ReplacesTool()
    {
        // Arrange
        bool innerClientCalled = false;
        ChatOptions? capturedOptions = null;

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                innerClientCalled = true;
                capturedOptions = options;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response")));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = [new HostedImageGenerationTool()]
        };

        // Act
        var response = await client.GetResponseAsync([new(ChatRole.User, "test")], chatOptions);

        // Assert
        Assert.True(innerClientCalled);
        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions.Tools);
        Assert.Equal(3, capturedOptions.Tools.Count);

        // Verify the functions are properly created
        var generateImageFunction = capturedOptions.Tools[0] as AIFunction;
        var editImageFunction = capturedOptions.Tools[1] as AIFunction;
        var getImagesForEditImageFunction = capturedOptions.Tools[2] as AIFunction;

        Assert.NotNull(generateImageFunction);
        Assert.NotNull(editImageFunction);
        Assert.NotNull(getImagesForEditImageFunction);
        Assert.Equal("GenerateImage", generateImageFunction.Name);
        Assert.Equal("EditImage", editImageFunction.Name);
        Assert.Equal("GetImagesForEdit", getImagesForEditImageFunction.Name);
    }

    [Fact]
    public async Task GetResponseAsync_WithMixedTools_ReplacesOnlyImageGenerationTool()
    {
        // Arrange
        bool innerClientCalled = false;
        ChatOptions? capturedOptions = null;

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                innerClientCalled = true;
                capturedOptions = options;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response")));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var dummyFunction = AIFunctionFactory.Create(() => "dummy", name: "DummyFunction");
        var chatOptions = new ChatOptions
        {
            Tools = [dummyFunction, new HostedImageGenerationTool()]
        };

        // Act
        var response = await client.GetResponseAsync([new(ChatRole.User, "test")], chatOptions);

        // Assert
        Assert.True(innerClientCalled);
        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions.Tools);
        Assert.Equal(4, capturedOptions.Tools.Count); // DummyFunction + GenerateImage + EditImage + GetImagesForEdit

        Assert.Same(dummyFunction, capturedOptions.Tools[0]); // Original function preserved
        Assert.IsAssignableFrom<AIFunction>(capturedOptions.Tools[1]); // GenerateImage function
        Assert.IsAssignableFrom<AIFunction>(capturedOptions.Tools[2]); // EditImage function
    }

    [Fact]
    public void UseImageGeneration_ServiceProviderIntegration_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IImageGenerator, TestImageGenerator>();

        using var serviceProvider = services.BuildServiceProvider();
        using var innerClient = new TestChatClient();

        // Act
        using var client = innerClient
            .AsBuilder()
            .UseImageGeneration()
            .Build(serviceProvider);

        // Assert
        Assert.IsType<ImageGeneratingChatClient>(client);
    }

    [Fact]
    public void UseImageGeneration_WithProvidedImageGenerator_Works()
    {
        // Arrange
        using var innerClient = new TestChatClient();
        using var imageGenerator = new TestImageGenerator();

        // Act
        using var client = innerClient
            .AsBuilder()
            .UseImageGeneration(imageGenerator)
            .Build();

        // Assert
        Assert.IsType<ImageGeneratingChatClient>(client);
    }

    [Fact]
    public void UseImageGeneration_WithConfigureCallback_CallsCallback()
    {
        // Arrange
        using var innerClient = new TestChatClient();
        using var imageGenerator = new TestImageGenerator();
        bool configureCallbackInvoked = false;

        // Act
        using var client = innerClient
            .AsBuilder()
            .UseImageGeneration(imageGenerator, configure: c =>
            {
                Assert.NotNull(c);
                configureCallbackInvoked = true;
            })
            .Build();

        // Assert
        Assert.True(configureCallbackInvoked);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithImageGenerationTool_ReplacesTool()
    {
        // Arrange
        bool innerClientCalled = false;
        ChatOptions? capturedOptions = null;

        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                innerClientCalled = true;
                capturedOptions = options;
                return GetUpdatesAsync();
            }
        };

        static async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync()
        {
            await Task.Yield();
            yield return new(ChatRole.Assistant, "test");
        }

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = [new HostedImageGenerationTool()]
        };

        // Act
        await foreach (var update in client.GetStreamingResponseAsync([new(ChatRole.User, "test")], chatOptions))
        {
            // Process updates
        }

        // Assert
        Assert.True(innerClientCalled);
        Assert.NotNull(capturedOptions);
        Assert.NotNull(capturedOptions.Tools);
        Assert.Equal(3, capturedOptions.Tools.Count);
    }

    [Fact]
    public async Task GetResponseAsync_WithNullOptions_DoesNotThrow()
    {
        // Arrange
        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response")));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        // Act & Assert
        var response = await client.GetResponseAsync([new(ChatRole.User, "test")], null);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetResponseAsync_WithEmptyTools_DoesNotModify()
    {
        // Arrange
        ChatOptions? capturedOptions = null;

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                capturedOptions = options;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response")));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = []
        };

        // Act
        await client.GetResponseAsync([new(ChatRole.User, "test")], chatOptions);

        // Assert
        Assert.Same(chatOptions, capturedOptions);
#pragma warning disable CA1508
        Assert.NotNull(capturedOptions?.Tools);
#pragma warning restore CA1508
        Assert.Empty(capturedOptions.Tools);
    }

    [Fact]
    public async Task GetResponseAsync_WithFunctionCallContent_ReplacesWithImageGenerationToolCallContent()
    {
        // Arrange
        var callId = "test-call-id";
        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                var responseMessage = new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent(callId, "GenerateImage", new Dictionary<string, object?> { ["prompt"] = "a cat" })]);
                return Task.FromResult(new ChatResponse(responseMessage));
            },
        };

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = [new HostedImageGenerationTool()]
        };

        // Act
        var response = await client.GetResponseAsync([new(ChatRole.User, "test")], chatOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Messages);
        var message = response.Messages[0];
        Assert.Single(message.Contents);

        var imageToolCallContent = Assert.IsType<ImageGenerationToolCallContent>(message.Contents[0]);
        Assert.Equal(callId, imageToolCallContent.ImageId);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithFunctionCallContent_ReplacesWithImageGenerationToolCallContent()
    {
        // Arrange
        var callId = "test-call-id";
        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                return GetUpdatesAsync();
            }
        };

        async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync()
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant,
                [new FunctionCallContent(callId, "GenerateImage", new Dictionary<string, object?> { ["prompt"] = "a cat" })]);
        }

        using var imageGenerator = new TestImageGenerator();
        using var client = new ImageGeneratingChatClient(innerClient, imageGenerator);

        var chatOptions = new ChatOptions
        {
            Tools = [new HostedImageGenerationTool()]
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var responseUpdate in client.GetStreamingResponseAsync([new(ChatRole.User, "test")], chatOptions))
        {
            updates.Add(responseUpdate);
        }

        // Assert
        Assert.Single(updates);
        var update = updates[0];
        Assert.Single(update.Contents);

        var imageToolCallContent = Assert.IsType<ImageGenerationToolCallContent>(update.Contents[0]);
        Assert.Equal(callId, imageToolCallContent.ImageId);
    }
}
