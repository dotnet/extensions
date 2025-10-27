﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable MEAI0001 // Suppress experimental warnings for testing

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
    public void ApplyUpdate_UpdatesResponse()
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

        // Act
        response.ApplyUpdate(update);

        // Assert
        var message = Assert.Single(response.Messages);
        var dataContent = Assert.Single(message.Contents.OfType<DataContent>());
        Assert.Equal("data:text/plain;base64,d29ybGQ=", dataContent.Uri);
        Assert.Equal("test-data", dataContent.Name);
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
    public void ApplyUpdates_UpdatesResponse()
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
    public async Task ApplyUpdatesAsync_InvalidArgs_Throws()
    {
        var response = new ChatResponse();
        var updates = YieldAsync(new List<ChatResponseUpdate>());

        await Assert.ThrowsAsync<ArgumentNullException>("response", () => ((ChatResponse)null!).ApplyUpdatesAsync(updates));
        await Assert.ThrowsAsync<ArgumentNullException>("updates", () => response.ApplyUpdatesAsync(null!));
    }

    [Fact]
    public async Task ApplyUpdatesAsync_UpdatesResponse()
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
            new(ChatRole.Assistant, ", ") { AuthorName = "Someone", AdditionalProperties = new() { ["a"] = "b" } },
            new(null, "world!") { CreatedAt = new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), ConversationId = "123", AdditionalProperties = new() { ["c"] = "d" } },

            new() { Contents = [new UsageContent(new() { InputTokenCount = 1, OutputTokenCount = 2 })] },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 4, OutputTokenCount = 5 })] },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();
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
        Assert.Equal(ChatRole.Assistant, message.Role);
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
    public async Task ToChatResponse_RoleOrIdOrAuthorNameChangeDictatesMessageChange(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [
            new(null, "!") { MessageId = "1" },
            new(ChatRole.Assistant, "a") { MessageId = "1" },
            new(ChatRole.Assistant, "b") { MessageId = "2" },
            new(ChatRole.User, "c") { MessageId = "2" },
            new(ChatRole.User, "d") { MessageId = "2" },
            new(ChatRole.Assistant, "e") { MessageId = "3" },
            new(ChatRole.Tool, "f") { MessageId = "4" },
            new(ChatRole.Tool, "g") { MessageId = "4" },
            new(ChatRole.Tool, "h") { MessageId = "5" },
            new(new("human"), "i") { MessageId = "6" },
            new(new("human"), "j") { MessageId = "7" },
            new(new("human"), "k") { MessageId = "7" },
            new(null, "l") { MessageId = "7" },
            new(null, "m") { MessageId = "8" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();
        Assert.Equal(9, response.Messages.Count);

        Assert.Equal("!a", response.Messages[0].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);

        Assert.Equal("b", response.Messages[1].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[1].Role);

        Assert.Equal("cd", response.Messages[2].Text);
        Assert.Equal(ChatRole.User, response.Messages[2].Role);

        Assert.Equal("e", response.Messages[3].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[3].Role);

        Assert.Equal("fg", response.Messages[4].Text);
        Assert.Equal(ChatRole.Tool, response.Messages[4].Role);

        Assert.Equal("h", response.Messages[5].Text);
        Assert.Equal(ChatRole.Tool, response.Messages[5].Role);

        Assert.Equal("i", response.Messages[6].Text);
        Assert.Equal(new ChatRole("human"), response.Messages[6].Role);

        Assert.Equal("jkl", response.Messages[7].Text);
        Assert.Equal(new ChatRole("human"), response.Messages[7].Role);

        Assert.Equal("m", response.Messages[8].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[8].Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_AuthorNameChangeDictatesMessageBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with AuthorName "Alice"
            new(ChatRole.Assistant, "Hello ") { AuthorName = "Alice" },
            new(null, "from ") { AuthorName = "Alice" },
            new(null, "Alice!"),

            // Second message - AuthorName changes to "Bob"
            new(null, "Hi ") { AuthorName = "Bob" },
            new(null, "from ") { AuthorName = "Bob" },
            new(null, "Bob!"),

            // Third message - AuthorName changes to "Charlie"
            new(ChatRole.Assistant, "Greetings ") { AuthorName = "Charlie" },
            new(null, "from Charlie!") { AuthorName = "Charlie" },

            // Fourth message - AuthorName changes back to "Alice"
            new(null, "Alice again!") { AuthorName = "Alice" },

            // Fifth message - empty/null AuthorName should continue with last message
            new(null, " Still Alice.") { AuthorName = "" },
            new(null, " And more."),
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(4, response.Messages.Count);

        Assert.Equal("Hello from Alice!", response.Messages[0].Text);
        Assert.Equal("Alice", response.Messages[0].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);

        Assert.Equal("Hi from Bob!", response.Messages[1].Text);
        Assert.Equal("Bob", response.Messages[1].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[1].Role);

        Assert.Equal("Greetings from Charlie!", response.Messages[2].Text);
        Assert.Equal("Charlie", response.Messages[2].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[2].Role);

        Assert.Equal("Alice again! Still Alice. And more.", response.Messages[3].Text);
        Assert.Equal("Alice", response.Messages[3].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[3].Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_AuthorNameWithOtherBoundaries(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // Message 1: Role=Assistant, MessageId="1", AuthorName="Alice"
            new(ChatRole.Assistant, "A") { MessageId = "1", AuthorName = "Alice" },
            new(null, "B") { MessageId = "1", AuthorName = "Alice" },

            // Message 2: AuthorName changes to "Bob", same MessageId and Role
            new(null, "C") { MessageId = "1", AuthorName = "Bob" },

            // Message 3: MessageId changes to "2", AuthorName stays "Bob"
            new(null, "D") { MessageId = "2", AuthorName = "Bob" },
            new(null, "E") { MessageId = "2", AuthorName = "Bob" },

            // Message 4: Role changes to User, AuthorName stays "Bob"
            new(ChatRole.User, "F") { MessageId = "2", AuthorName = "Bob" },

            // Message 5: All three boundaries change
            new(ChatRole.Tool, "G") { MessageId = "3", AuthorName = "Charlie" },
            new(null, "H") { MessageId = "3", AuthorName = "Charlie" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(5, response.Messages.Count);

        Assert.Equal("AB", response.Messages[0].Text);
        Assert.Equal("Alice", response.Messages[0].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.Equal("1", response.Messages[0].MessageId);

        Assert.Equal("C", response.Messages[1].Text);
        Assert.Equal("Bob", response.Messages[1].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[1].Role);
        Assert.Equal("1", response.Messages[1].MessageId);

        Assert.Equal("DE", response.Messages[2].Text);
        Assert.Equal("Bob", response.Messages[2].AuthorName);
        Assert.Equal(ChatRole.Assistant, response.Messages[2].Role);
        Assert.Equal("2", response.Messages[2].MessageId);

        Assert.Equal("F", response.Messages[3].Text);
        Assert.Equal("Bob", response.Messages[3].AuthorName);
        Assert.Equal(ChatRole.User, response.Messages[3].Role);
        Assert.Equal("2", response.Messages[3].MessageId);

        Assert.Equal("GH", response.Messages[4].Text);
        Assert.Equal("Charlie", response.Messages[4].AuthorName);
        Assert.Equal(ChatRole.Tool, response.Messages[4].Role);
        Assert.Equal("3", response.Messages[4].MessageId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_EmptyOrNullAuthorNameDoesNotCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with AuthorName "Assistant"
            new(ChatRole.Assistant, "Hello") { AuthorName = "Assistant" },

            // Empty AuthorName should not create new message
            new(null, " world") { AuthorName = "" },

            // Null AuthorName should not create new message
            new(null, "!"),

            // Another empty AuthorName
            new(null, " How") { AuthorName = "" },
            new(null, " are") { AuthorName = "" },

            // Null again
            new(null, " you?") { AuthorName = null },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal("Hello world! How are you?", message.Text);
        Assert.Equal("Assistant", message.AuthorName);
        Assert.Equal(ChatRole.Assistant, message.Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_AuthorNameNullToNonNullDoesNotCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with no AuthorName
            new(ChatRole.Assistant, "Hello") { MessageId = "1" },
            new(null, " there") { MessageId = "1" },

            // AuthorName becomes non-empty but doesn't create boundary
            new(null, " I'm Bob") { MessageId = "1", AuthorName = "Bob" },
            new(null, " speaking") { MessageId = "1", AuthorName = "Bob" },

            // Second message - AuthorName changes to "Alice" creates boundary
            new(null, "Now Alice") { MessageId = "1", AuthorName = "Alice" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(2, response.Messages.Count);

        Assert.Equal("Hello there I'm Bob speaking", response.Messages[0].Text);
        Assert.Equal("Bob", response.Messages[0].AuthorName); // Last AuthorName wins
        Assert.Equal("1", response.Messages[0].MessageId);

        Assert.Equal("Now Alice", response.Messages[1].Text);
        Assert.Equal("Alice", response.Messages[1].AuthorName);
        Assert.Equal("1", response.Messages[1].MessageId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_MessageIdNullToNonNullDoesNotCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with no MessageId
            new(ChatRole.Assistant, "Hello"),
            new(null, " there"),

            // MessageId becomes non-empty but doesn't create boundary
            new(null, " from") { MessageId = "msg1" },
            new(null, " AI") { MessageId = "msg1" },

            // Second message - MessageId changes to different value creates boundary
            new(null, "Next message") { MessageId = "msg2" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(2, response.Messages.Count);

        Assert.Equal("Hello there from AI", response.Messages[0].Text);
        Assert.Equal("msg1", response.Messages[0].MessageId); // Last MessageId wins
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);

        Assert.Equal("Next message", response.Messages[1].Text);
        Assert.Equal("msg2", response.Messages[1].MessageId);
        Assert.Equal(ChatRole.Assistant, response.Messages[1].Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_EmptyMessageIdDoesNotCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with MessageId
            new(ChatRole.Assistant, "Hello") { MessageId = "msg1" },
            new(null, " world") { MessageId = "msg1" },

            // Empty MessageId should not create new message
            new(null, "!") { MessageId = "" },

            // Null MessageId should not create new message
            new(null, " How"),

            // Another message with empty MessageId
            new(null, " are") { MessageId = "" },
            new(null, " you?"),
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal("Hello world! How are you?", message.Text);
        Assert.Equal("msg1", message.MessageId);
        Assert.Equal(ChatRole.Assistant, message.Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_RoleNullToNonNullDoesNotCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with no explicit Role (will default to Assistant)
            new(null, "Hello") { MessageId = "1" },
            new(null, " there") { MessageId = "1" },

            // Role becomes explicit Assistant - shouldn't create boundary
            new(ChatRole.Assistant, " from") { MessageId = "1" },
            new(null, " AI") { MessageId = "1" },

            // Second message - Role changes to User creates boundary
            new(ChatRole.User, "User message") { MessageId = "1" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(2, response.Messages.Count);

        Assert.Equal("Hello there from AI", response.Messages[0].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[0].Role);
        Assert.Equal("1", response.Messages[0].MessageId);

        Assert.Equal("User message", response.Messages[1].Text);
        Assert.Equal(ChatRole.User, response.Messages[1].Role);
        Assert.Equal("1", response.Messages[1].MessageId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_CustomRoleChangesCreateBoundary(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [

            // First message with custom role "agent1"
            new(new ChatRole("agent1"), "Hello") { MessageId = "1" },
            new(null, " from") { MessageId = "1" },
            new(new ChatRole("agent1"), " agent1") { MessageId = "1" },

            // Second message - custom role changes to "agent2"
            new(new ChatRole("agent2"), "Hi") { MessageId = "1" },
            new(null, " from") { MessageId = "1" },
            new(new ChatRole("agent2"), " agent2") { MessageId = "1" },

            // Third message - changes to standard role
            new(ChatRole.Assistant, "Assistant here") { MessageId = "1" },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Equal(3, response.Messages.Count);

        Assert.Equal("Hello from agent1", response.Messages[0].Text);
        Assert.Equal(new ChatRole("agent1"), response.Messages[0].Role);

        Assert.Equal("Hi from agent2", response.Messages[1].Text);
        Assert.Equal(new ChatRole("agent2"), response.Messages[1].Role);

        Assert.Equal("Assistant here", response.Messages[2].Text);
        Assert.Equal(ChatRole.Assistant, response.Messages[2].Role);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_UpdatesProduceMultipleResponseMessages(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        [
            
            // First message - ID "msg1", AuthorName "Assistant"
            new(null, "Hi! ") { CreatedAt = new DateTimeOffset(2023, 1, 1, 10, 0, 0, TimeSpan.Zero), AuthorName = "Assistant" },
            new(ChatRole.Assistant, "Hello") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), AuthorName = "Assistant" },
            new(null, " from") { MessageId = "msg1", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 1, 0, TimeSpan.Zero) }, // Later CreatedAt should win
            new(null, " AI") { MessageId = "msg1", AuthorName = "Assistant" }, // Keep same AuthorName to avoid creating new message

            // Second message - ID "msg1" changes to "msg2", still AuthorName "Assistant" 
            new(null, "More text") { MessageId = "msg2", CreatedAt = new DateTimeOffset(2024, 1, 1, 10, 2, 0, TimeSpan.Zero), AuthorName = "Assistant" },

            // Third message - ID "msg3", Role changes to User
            new(ChatRole.User, "How") { MessageId = "msg3", CreatedAt = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero), AuthorName = "User" },
            new(null, " are") { MessageId = "msg3", CreatedAt = new DateTimeOffset(2024, 1, 1, 11, 1, 0, TimeSpan.Zero) },
            new(null, " you?") { MessageId = "msg3", AuthorName = "User" }, // Keep same AuthorName

            // Fourth message - ID "msg4", Role changes back to Assistant
            new(ChatRole.Assistant, "I'm doing well,") { MessageId = "msg4", CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero) },
            new(null, " thank you!") { MessageId = "msg4", CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 2, 0, TimeSpan.Zero) }, // Later CreatedAt should win

            // Updates without MessageId should continue the last message (msg4)
            new(null, " How can I help?"),
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.NotNull(response);
        Assert.Equal(4, response.Messages.Count);

        // Verify first message
        ChatMessage message1 = response.Messages[0];
        Assert.Equal("msg1", message1.MessageId);
        Assert.Equal(ChatRole.Assistant, message1.Role);
        Assert.Equal("Assistant", message1.AuthorName);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 10, 1, 0, TimeSpan.Zero), message1.CreatedAt); // Last value should win
        Assert.Equal("Hi! Hello from AI", message1.Text);

        // Verify second message  
        ChatMessage message2 = response.Messages[1];
        Assert.Equal("msg2", message2.MessageId);
        Assert.Equal(ChatRole.Assistant, message2.Role);
        Assert.Equal("Assistant", message2.AuthorName);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 10, 2, 0, TimeSpan.Zero), message2.CreatedAt);
        Assert.Equal("More text", message2.Text);

        // Verify third message
        ChatMessage message3 = response.Messages[2];
        Assert.Equal("msg3", message3.MessageId);
        Assert.Equal(ChatRole.User, message3.Role);
        Assert.Equal("User", message3.AuthorName);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 11, 1, 0, TimeSpan.Zero), message3.CreatedAt); // Last value should win
        Assert.Equal("How are you?", message3.Text);

        // Verify fourth message
        ChatMessage message4 = response.Messages[3];
        Assert.Equal("msg4", message4.MessageId);
        Assert.Equal(ChatRole.Assistant, message4.Role);
        Assert.Null(message4.AuthorName); // No AuthorName set
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 12, 2, 0, TimeSpan.Zero), message4.CreatedAt); // Last value should win
        Assert.Equal("I'm doing well, thank you! How can I help?", message4.Text);
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
    public async Task ToChatResponse_CoalescesTextReasoningContentUpToProtectedData(bool useAsync)
    {
        ChatResponseUpdate[] updates =
        {
            new() { Contents = [new TextReasoningContent("A") { ProtectedData = "1" }] },
            new() { Contents = [new TextReasoningContent("B") { ProtectedData = "2" }] },
            new() { Contents = [new TextReasoningContent("C")] },
            new() { Contents = [new TextReasoningContent("D")] },
            new() { Contents = [new TextReasoningContent("E") { ProtectedData = "3" }] },
            new() { Contents = [new TextReasoningContent("F") { ProtectedData = "4" }] },
            new() { Contents = [new TextReasoningContent("G")] },
            new() { Contents = [new TextReasoningContent("H")] },
        };

        ChatResponse response = useAsync ? await YieldAsync(updates).ToChatResponseAsync() : updates.ToChatResponse();
        ChatMessage message = Assert.Single(response.Messages);
        Assert.Equal(5, message.Contents.Count);

        Assert.Equal("A", Assert.IsType<TextReasoningContent>(message.Contents[0]).Text);
        Assert.Equal("1", ((TextReasoningContent)message.Contents[0]).ProtectedData);

        Assert.Equal("B", Assert.IsType<TextReasoningContent>(message.Contents[1]).Text);
        Assert.Equal("2", ((TextReasoningContent)message.Contents[1]).ProtectedData);

        Assert.Equal("CDE", Assert.IsType<TextReasoningContent>(message.Contents[2]).Text);
        Assert.Equal("3", ((TextReasoningContent)message.Contents[2]).ProtectedData);

        Assert.Equal("F", Assert.IsType<TextReasoningContent>(message.Contents[3]).Text);
        Assert.Equal("4", ((TextReasoningContent)message.Contents[3]).ProtectedData);

        Assert.Equal("GH", Assert.IsType<TextReasoningContent>(message.Contents[4]).Text);
        Assert.Null(((TextReasoningContent)message.Contents[4]).ProtectedData);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToChatResponse_AlternativeTimestamps(bool useAsync)
    {
        DateTimeOffset early = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset middle = new(2024, 1, 1, 11, 0, 0, TimeSpan.Zero);
        DateTimeOffset late = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset unixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        ChatResponseUpdate[] updates =
        [

            // Start with an early timestamp
            new(ChatRole.Tool, "a") { MessageId = "4", CreatedAt = early },

            // Unix epoch (as "null") should not overwrite
            new(null, "b") { CreatedAt = unixEpoch },

            // Newer timestamp should overwrite
            new(null, "c") { CreatedAt = middle },

            // Older timestamp should not overwrite
            new(null, "d") { CreatedAt = early },

            // Even newer timestamp should overwrite
            new(null, "e") { CreatedAt = late },

            // Unix epoch should not overwrite again
            new(null, "f") { CreatedAt = unixEpoch },

            // null should not overwrite
            new(null, "g") { CreatedAt = null },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();
        Assert.Single(response.Messages);

        Assert.Equal("abcdefg", response.Messages[0].Text);
        Assert.Equal(ChatRole.Tool, response.Messages[0].Role);
        Assert.Equal(late, response.Messages[0].CreatedAt);
        Assert.Equal(late, response.CreatedAt);
    }

    public static IEnumerable<object?[]> ToChatResponse_TimestampFolding_MemberData()
    {
        // Base test cases
        var testCases = new (string? timestamp1, string? timestamp2, string? expectedTimestamp)[]
        {
            (null, null, null),
            ("2024-01-01T10:00:00Z", null, "2024-01-01T10:00:00Z"),
            (null, "2024-01-01T10:00:00Z", "2024-01-01T10:00:00Z"),
            ("2024-01-01T10:00:00Z", "2024-01-01T11:00:00Z", "2024-01-01T11:00:00Z"),
            ("2024-01-01T11:00:00Z", "2024-01-01T10:00:00Z", "2024-01-01T11:00:00Z"),
            ("2024-01-01T10:00:00Z", "1970-01-01T00:00:00Z", "2024-01-01T10:00:00Z"),
            ("1970-01-01T00:00:00Z", "2024-01-01T10:00:00Z", "2024-01-01T10:00:00Z"),
        };

        // Yield each test case twice, once for useAsync = false and once for useAsync = true
        foreach (var (timestamp1, timestamp2, expectedTimestamp) in testCases)
        {
            yield return new object?[] { false, timestamp1, timestamp2, expectedTimestamp };
            yield return new object?[] { true, timestamp1, timestamp2, expectedTimestamp };
        }
    }

    [Theory]
    [MemberData(nameof(ToChatResponse_TimestampFolding_MemberData))]
    public async Task ToChatResponse_TimestampFolding(bool useAsync, string? timestamp1, string? timestamp2, string? expectedTimestamp)
    {
        DateTimeOffset? first = timestamp1 is not null ? DateTimeOffset.Parse(timestamp1) : null;
        DateTimeOffset? second = timestamp2 is not null ? DateTimeOffset.Parse(timestamp2) : null;
        DateTimeOffset? expected = expectedTimestamp is not null ? DateTimeOffset.Parse(expectedTimestamp) : null;

        ChatResponseUpdate[] updates =
        [
            new(ChatRole.Assistant, "a") { CreatedAt = first },
            new(null, "b") { CreatedAt = second },
        ];

        ChatResponse response = useAsync ?
            await YieldAsync(updates).ToChatResponseAsync() :
            updates.ToChatResponse();

        Assert.Single(response.Messages);
        Assert.Equal("ab", response.Messages[0].Text);
        Assert.Equal(expected, response.Messages[0].CreatedAt);
        Assert.Equal(expected, response.CreatedAt);
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
