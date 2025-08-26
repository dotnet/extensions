// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable EXTAI0001 // Suppress experimental warnings for testing

namespace Microsoft.Extensions.AI;

public class ChatResponseUpdateExtensionsTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((List<ChatResponseUpdate>)null!).ToChatResponse());
    }

    [Fact]
    public void ApplyUpdate_InvalidArgs_Throws()
    {
        var response = new ChatResponse();
        var update = new ChatResponseUpdate();

        Assert.Throws<ArgumentNullException>("response", () => ((ChatResponse)null!).ApplyUpdate(update));
        Assert.Throws<ArgumentNullException>("update", () => response.ApplyUpdate(null!));
    }

    [Fact]
    public void ApplyUpdate_WithDefaultOptions_UpdatesResponse()
    {
        // Arrange
        var response = new ChatResponse();
        var update = new ChatResponseUpdate(ChatRole.Assistant, "Hello, world!")
        {
            ResponseId = "resp123",
            MessageId = "msg456",
            CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            ModelId = "model789",
            ConversationId = "conv101112",
            FinishReason = ChatFinishReason.Stop
        };

        // Act
        response.ApplyUpdate(update);

        // Assert
        Assert.Equal("resp123", response.ResponseId);
        Assert.Equal("model789", response.ModelId);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero), response.CreatedAt);
        Assert.Equal("conv101112", response.ConversationId);
        Assert.Equal(ChatFinishReason.Stop, response.FinishReason);

        var message = Assert.Single(response.Messages);
        Assert.Equal("msg456", message.MessageId);
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Equal("Hello, world!", message.Text);
    }

    [Fact]
    public void ApplyUpdate_WithOptions_UsesCoalescingOptions()
    {
        // Arrange
        var response = new ChatResponse();
        var existingDataContent = new DataContent("data:text/plain;base64,aGVsbG8=")
        {
            Name = "test-data"
        };
        response.Messages.Add(new ChatMessage(ChatRole.Assistant, [existingDataContent]));

        var newDataContent = new DataContent("data:text/plain;base64,d29ybGQ=")
        {
            Name = "test-data"  // Same name as existing
        };
        var update = new ChatResponseUpdate
        {
            Contents = [newDataContent]
        };

        var options = new ChatResponseUpdateCoalescingOptions
        {
            ReplaceDataContentWithSameName = true
        };

        // Act
        response.ApplyUpdate(update, options);

        // Assert
        var message = Assert.Single(response.Messages);
        var dataContent = Assert.Single(message.Contents.OfType<DataContent>());
        Assert.Equal("data:text/plain;base64,d29ybGQ=", dataContent.Uri);
        Assert.Equal("test-data", dataContent.Name);
    }

    [Fact]
    public void ApplyUpdate_WithNullOptions_WorksCorrectly()
    {
        // Arrange
        var response = new ChatResponse();
        var update = new ChatResponseUpdate(ChatRole.User, "Test message");

        // Act - explicitly pass null options
        response.ApplyUpdate(update, null);

        // Assert
        var message = Assert.Single(response.Messages);
        Assert.Equal("Test message", message.Text);
        Assert.Equal(ChatRole.User, message.Role);
    }

    [Fact]
    public void ApplyUpdates_InvalidArgs_Throws()
    {
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>();

        Assert.Throws<ArgumentNullException>("response", () => ((ChatResponse)null!).ApplyUpdates(updates));
        Assert.Throws<ArgumentNullException>("updates", () => response.ApplyUpdates(null!));
    }

    [Fact]
    public void ApplyUpdates_WithDefaultOptions_UpdatesResponse()
    {
        // Arrange
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello") { MessageId = "msg1", ResponseId = "resp1" },
            new(null, ", ") { MessageId = "msg1" },
            new(null, "world!") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero) },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 10, OutputTokenCount = 5 })] }
        };

        // Act
        response.ApplyUpdates(updates);

        // Assert
        Assert.Equal("resp1", response.ResponseId);
        Assert.NotNull(response.Usage);
        Assert.Equal(10, response.Usage.InputTokenCount);
        Assert.Equal(5, response.Usage.OutputTokenCount);

        var message = Assert.Single(response.Messages);
        Assert.Equal("msg1", message.MessageId);
        Assert.Equal("Hello, world!", message.Text);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), message.CreatedAt);
    }

    [Fact]
    public void ApplyUpdates_WithOptions_UsesCoalescingOptions()
    {
        // Arrange
        var response = new ChatResponse();
        var existingDataContent = new DataContent("data:text/plain;base64,aGVsbG8=")
        {
            Name = "shared-name"
        };
        response.Messages.Add(new ChatMessage(ChatRole.Assistant, [existingDataContent]));

        var sharedNameDataContent = new DataContent("data:text/plain;base64,dXBkYXRl")
        {
            Name = "shared-name"  // Same name
        };
        var otherNameDataContent = new DataContent("data:text/plain;base64,b3RoZXI=")
        {
            Name = "other-name"  // Different name
        };
        var updates = new List<ChatResponseUpdate>
        {
            new() { Contents = [sharedNameDataContent] },
            new() { Contents = [otherNameDataContent] }
        };

        var options = new ChatResponseUpdateCoalescingOptions
        {
            ReplaceDataContentWithSameName = true
        };

        // Act
        response.ApplyUpdates(updates, options);

        // Assert
        var message = Assert.Single(response.Messages);
        Assert.Equal(2, message.Contents.Count);

        var sharedNameContent = message.Contents.OfType<DataContent>().First(c => c.Name == "shared-name");
        Assert.Equal("data:text/plain;base64,dXBkYXRl", sharedNameContent.Uri); // Should be replaced

        var otherNameContent = message.Contents.OfType<DataContent>().First(c => c.Name == "other-name");
        Assert.Equal("data:text/plain;base64,b3RoZXI=", otherNameContent.Uri); // Should be added
    }

    [Fact]
    public void ApplyUpdates_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>();

        // Act
        response.ApplyUpdates(updates);

        // Assert
        Assert.Empty(response.Messages);
    }

    [Fact]
    public void ApplyUpdates_WithNullOptions_WorksCorrectly()
    {
        // Arrange
        var response = new ChatResponse();
        var sharedNameDataContent = new DataContent("data:text/plain;base64,dXBkYXRl")
        {
            Name = "shared-name"  // Same name
        };
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.System, [sharedNameDataContent]),
            new(ChatRole.System, [sharedNameDataContent])
        };

        // Act - explicitly pass null options
        response.ApplyUpdates(updates, null);

        // Assert
        Assert.Single(response.Messages);
        Assert.All(response.Messages[0].Contents, c => Assert.IsType<DataContent>(c));
    }

    [Fact]
    public async Task ApplyUpdatesAsync_InvalidArgs_Throws()
    {
        var response = new ChatResponse();
        var updates = YieldAsync(new List<ChatResponseUpdate>());

        await Assert.ThrowsAsync<ArgumentNullException>("response", () => ((ChatResponse)null!).ApplyUpdatesAsync(updates));
        await Assert.ThrowsAsync<ArgumentNullException>("updates", () => response.ApplyUpdatesAsync(null!));
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WithDefaultOptions_UpdatesResponse()
    {
        // Arrange
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello") { MessageId = "msg1", ResponseId = "resp1" },
            new(null, " async") { MessageId = "msg1" },
            new(null, " world!") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero) },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 15, OutputTokenCount = 8 })] }
        };

        // Act
        await response.ApplyUpdatesAsync(YieldAsync(updates));

        // Assert
        Assert.Equal("resp1", response.ResponseId);
        Assert.NotNull(response.Usage);
        Assert.Equal(15, response.Usage.InputTokenCount);
        Assert.Equal(8, response.Usage.OutputTokenCount);

        var message = Assert.Single(response.Messages);
        Assert.Equal("msg1", message.MessageId);
        Assert.Equal("Hello async world!", message.Text);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero), message.CreatedAt);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WithOptions_UsesCoalescingOptions()
    {
        // Arrange
        var response = new ChatResponse();
        var existingDataContent = new DataContent("data:text/plain;base64,b3JpZ2luYWw=")
        {
            Name = "data-key"
        };
        response.Messages.Add(new ChatMessage(ChatRole.Assistant, [existingDataContent]));

        var newDataContent = new DataContent("data:text/plain;base64,bmV3VmFsdWU=")
        {
            Name = "data-key"  // Same name
        };
        var updates = new List<ChatResponseUpdate>
        {
            new() { Contents = [newDataContent] },
        };

        var options = new ChatResponseUpdateCoalescingOptions
        {
            ReplaceDataContentWithSameName = true
        };

        // Act
        await response.ApplyUpdatesAsync(YieldAsync(updates), options);

        // Assert
        var message = Assert.Single(response.Messages);
        var dataContent = Assert.Single(message.Contents.OfType<DataContent>());
        Assert.Equal("data:text/plain;base64,bmV3VmFsdWU=", dataContent.Uri); // Should be replaced
        Assert.Equal("data-key", dataContent.Name);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WithNullOptions_WorksCorrectly()
    {
        // Arrange
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Async message with null options")
        };

        // Act - explicitly pass null options
        await response.ApplyUpdatesAsync(YieldAsync(updates), null);

        // Assert
        var message = Assert.Single(response.Messages);
        Assert.Equal("Async message with null options", message.Text);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_MultipleMessages_ProcessedCorrectly()
    {
        // Arrange
        var response = new ChatResponse();
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "First") { MessageId = "msg1" },
            new(null, " message") { MessageId = "msg1" },
            new(ChatRole.User, "Second") { MessageId = "msg2" },
            new(null, " message") { MessageId = "msg2" },
            new(ChatRole.Assistant, "Third message") { MessageId = "msg3" }
        };

        // Act
        await response.ApplyUpdatesAsync(YieldAsync(updates));

        // Assert
        Assert.Equal(3, response.Messages.Count);
        Assert.Equal("First message", response.Messages[0].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.Equal("msg1", response.Messages[0].MessageId);

        Assert.Equal("Second message", response.Messages[1].Text);
        Assert.Equal(ChatRole.User, response.Messages[1].Role);
        Assert.Equal("msg2", response.Messages[1].MessageId);

        Assert.Equal("Third message", response.Messages[2].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[2].Role);
        Assert.Equal("msg3", response.Messages[2].MessageId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_SuccessfullyCreatesResponse(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [
            new(ChatRole.Assistant, "Hello") { ResponseId = "someResponse", MessageId = "12345", CreatedAt = new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), ModelId = "model123" },
            new(new("human"), ", ") { AuthorName = "Someone", AdditionalProperties = new() { ["a"] = "b" } },
            new(null, "world!") { CreatedAt = new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), ConversationId = "123", AdditionalProperties = new() { ["c"] = "d" } },

            new() { Contents = [new UsageContent(new() { InputTokenCount = 1, OutputTokenCount = 2 })] },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 4, OutputTokenCount = 5 })] },
        ];

        ChatResponse response = useAsync ?
            updates.ToChatResponse() :
            await YieldAsync(updates).ToChatResponseAsync();
        Assert.NotNull(response);

        Assert.NotNull(response.Usage);
        Assert.Equal(5, response.Usage.InputTokenCount);
        Assert.Equal(7, response.Usage.OutputTokenCount);

        Assert.Equal("someResponse", response.ResponseId);
        Assert.Equal(new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), response.CreatedAt);
        Assert.Equal("model123", response.ModelId);

        Assert.Equal("123", response.ConversationId);

        ChatMessage message = response.Messages.Single();
        Assert.Equal("12345", message.MessageId);
        Assert.Equal(new ChatRole("human"), message.Role);
        Assert.Equal("Someone", message.AuthorName);
        Assert.Null(message.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(2, response.AdditionalProperties.Count);
        Assert.Equal("b", response.AdditionalProperties["a"]);
        Assert.Equal("d", response.AdditionalProperties["c"]);

        Assert.Equal("Hello, world!", response.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_UpdatesProduceMultipleResponseMessages(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [
            
            // First message - ID "msg1"
            new(null, "Hi! ") { CreatedAt = new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.Zero), AuthorName = "Assistant" },
            new(ChatRole.Assistant, "Hello") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), AuthorName = "Assistant" },
            new(null, " from") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 1, 0, TimeSpan.Zero) }, // Later CreatedAt should win
            new(null, " AI") { MessageId = "msg1", AuthorName = "AI Assistant" }, // Later AuthorName should win

            // Second message - ID "msg2" 
            new(ChatRole.User, "How") { MessageId = "msg2", CreatedAt = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero), AuthorName = "User" },
            new(null, " are") { MessageId = "msg2", CreatedAt = new DateTimeOffset(2024, 1, 1, 11, 1, 0, TimeSpan.Zero) },
            new(null, " you?") { MessageId = "msg2", AuthorName = "Human User" }, // Later AuthorName should win

            // Third message - ID "msg3"
            new(ChatRole.Assistant, "I'm doing well,") { MessageId = "msg3", CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero) },
            new(null, " thank you!") { MessageId = "msg3", CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 2, 0, TimeSpan.Zero) }, // Later CreatedAt should win

            // Updates without MessageId should continue the last message (msg3)
            new(null, " How can I help?"),
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.NotNull(response);
        Assert.Equal(3, response.Messages.Count);

        // Verify first message
        ChatMessage message1 = response.Messages[0];
        Assert.Equal("msg1", message1.MessageId);
        Assert.Equal(ChatRole.Assistant, message1.Role);
        Assert.Equal("AI Assistant", message1.AuthorName); // Last value should win
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 10, 1, 0, TimeSpan.Zero), message1.CreatedAt); // Last value should win
        Assert.Equal("Hi! Hello from AI", message1.Text);

        // Verify second message  
        ChatMessage message2 = response.Messages[1];
        Assert.Equal("msg2", message2.MessageId);
        Assert.Equal(ChatRole.User, message2.Role);
        Assert.Equal("Human User", message2.AuthorName); // Last value should win
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 11, 1, 0, TimeSpan.Zero), message2.CreatedAt); // Last value should win
        Assert.Equal("How are you?", message2.Text);

        // Verify third message
        ChatMessage message3 = response.Messages[2];
        Assert.Equal("msg3", message3.MessageId);
        Assert.Equal(ChatRole.Assistant, message3.Role);
        Assert.Null(message3.AuthorName); // No AuthorName set in later updates
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 12, 2, 0, TimeSpan.Zero), message3.CreatedAt); // Last value should win
        Assert.Equal("I'm doing well, thank you! How can I help?", message3.Text);
    }

    public static IEnumerable<object[]> ToChatResponse_Coalescing_VariousSequenceAndGapLengths_MemberData()
    {
        foreach (bool useAsync in new[] { false, true })
        {
            for (int numSequences = 1; numSequences <= 3; numSequences++)
            {
                for (int sequenceLength = 1; sequenceLength <= 3; sequenceLength++)
                {
                    for (int gapLength = 1; gapLength <= 3; gapLength++)
                    {
                        foreach (bool gapBeginningEnd in new[] { false, true })
                        {
                            yield return new object[] { useAsync, numSequences, sequenceLength, gapLength, false };
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ToChatResponse_Coalescing_VariousSequenceAndGapLengths_MemberData))]
    public async Task ToChatResponse_Coalescing_VariousSequenceAndGapLengths(bool useAsync, int numSequences, int sequenceLength, int gapLength, bool gapBeginningEnd)
    {
        List<ChatResponseUpdate> updates = [];

        List<string> expected = [];

        if (gapBeginningEnd)
        {
            AddGap();
        }

        for (int sequenceNum = 0; sequenceNum < numSequences; sequenceNum++)
        {
            StringBuilder sb = new();
            for (int i = 0; i < sequenceLength; i++)
            {
                string text = $"{(char)('A' + sequenceNum)}{i}";
                updates.Add(new(null, text));
                sb.Append(text);
            }

            expected.Add(sb.ToString());

            if (sequenceNum < numSequences - 1)
            {
                AddGap();
            }
        }

        if (gapBeginningEnd)
        {
            AddGap();
        }

        void AddGap()
        {
            for (int i = 0; i < gapLength; i++)
            {
                updates.Add(new() { Contents = [new DataContent("data:image/png;base64,aGVsbG8=")] });
            }
        }

        ChatResponse response = useAsync ? await YieldAsync(updates).ToChatResponseAsync() : updates.ToChatResponse();
        Assert.NotNull(response);

        ChatMessage message = response.Messages.Single();
        Assert.NotNull(message);

        Assert.Equal(expected.Count + (gapLength * ((numSequences - 1) + (gapBeginningEnd ? 2 : 0))), message.Contents.Count);

        TextContent[] contents = message.Contents.OfType<TextContent>().ToArray();
        Assert.Equal(expected.Count, contents.Length);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], contents[i].Text);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_CoalescesTextContentAndTextReasoningContentSeparately(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        {
            new(null, "A"),
            new(null, "B"),
            new(null, "C"),
            new() { Contents = [new TextReasoningContent("D")] },
            new() { Contents = [new TextReasoningContent("E")] },
            new() { Contents = [new TextReasoningContent("F")] },
            new(null, "G"),
            new(null, "H"),
            new() { Contents = [new TextReasoningContent("I")] },
            new() { Contents = [new TextReasoningContent("J")] },
            new(null, "K"),
            new() { Contents = [new TextReasoningContent("L")] },
            new(null, "M"),
            new(null, "N"),
            new() { Contents = [new TextReasoningContent("O")] },
            new() { Contents = [new TextReasoningContent("P")] },
        };

        ChatResponse response = useAsync ? await YieldAsync(updates).ToChatResponseAsync() : updates.ToChatResponse();
        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal(8, message.Contents.Count);
        Assert.Equal("ABC", Assert.IsType<TextContent>(message.Contents[0]).Text);
        Assert.Equal("DEF", Assert.IsType<TextReasoningContent>(message.Contents[1]).Text);
        Assert.Equal("GH", Assert.IsType<TextContent>(message.Contents[2]).Text);
        Assert.Equal("IJ", Assert.IsType<TextReasoningContent>(message.Contents[3]).Text);
        Assert.Equal("K", Assert.IsType<TextContent>(message.Contents[4]).Text);
        Assert.Equal("L", Assert.IsType<TextReasoningContent>(message.Contents[5]).Text);
        Assert.Equal("MN", Assert.IsType<TextContent>(message.Contents[6]).Text);
        Assert.Equal("OP", Assert.IsType<TextReasoningContent>(message.Contents[7]).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_DoesNotCoalesceAnnotatedContent(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        {
            new(null, "A"),
            new(null, "B"),
            new(null, "C"),
            new() { Contents = [new TextContent("D") { Annotations = [new()] }] },
            new() { Contents = [new TextContent("E") { Annotations = [new()] }] },
            new() { Contents = [new TextContent("F") { Annotations = [new()] }] },
            new() { Contents = [new TextContent("G") { Annotations = [] }] },
            new() { Contents = [new TextContent("H") { Annotations = [] }] },
            new() { Contents = [new TextContent("I") { Annotations = [new()] }] },
            new() { Contents = [new TextContent("J") { Annotations = [new()] }] },
            new(null, "K"),
            new() { Contents = [new TextContent("L") { Annotations = [new()] }] },
            new(null, "M"),
            new(null, "N"),
            new() { Contents = [new TextContent("O") { Annotations = [new()] }] },
            new() { Contents = [new TextContent("P") { Annotations = [new()] }] },
        };

        ChatResponse response = useAsync ? await YieldAsync(updates).ToChatResponseAsync() : updates.ToChatResponse();
        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal(12, message.Contents.Count);
        Assert.Equal("ABC", Assert.IsType<TextContent>(message.Contents[0]).Text);
        Assert.Equal("D", Assert.IsType<TextContent>(message.Contents[1]).Text);
        Assert.Equal("E", Assert.IsType<TextContent>(message.Contents[2]).Text);
        Assert.Equal("F", Assert.IsType<TextContent>(message.Contents[3]).Text);
        Assert.Equal("GH", Assert.IsType<TextContent>(message.Contents[4]).Text);
        Assert.Equal("I", Assert.IsType<TextContent>(message.Contents[5]).Text);
        Assert.Equal("J", Assert.IsType<TextContent>(message.Contents[6]).Text);
        Assert.Equal("K", Assert.IsType<TextContent>(message.Contents[7]).Text);
        Assert.Equal("L", Assert.IsType<TextContent>(message.Contents[8]).Text);
        Assert.Equal("MN", Assert.IsType<TextContent>(message.Contents[9]).Text);
        Assert.Equal("O", Assert.IsType<TextContent>(message.Contents[10]).Text);
        Assert.Equal("P", Assert.IsType<TextContent>(message.Contents[11]).Text);
    }

    [Fact]
    public async Task ToChatResponse_UsageContentExtractedFromContents()
    {
        ChatResponseUpdate[] updates =
        {
            new(null, "Hello, "),
            new(null, "world!"),
            new() { Contents = [new UsageContent(new() { TotalTokenCount = 42 })] },
        };

        ChatResponse response = await YieldAsync(updates).ToChatResponseAsync();

        Assert.NotNull(response);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.TotalTokenCount);

        Assert.Equal("Hello, world!", Assert.IsType<TextContent>(Assert.Single(Assert.Single(response.Messages).Contents)).Text);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(IEnumerable<ChatResponseUpdate> updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
