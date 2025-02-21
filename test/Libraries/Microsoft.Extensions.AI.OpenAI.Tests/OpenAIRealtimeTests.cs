// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ComponentModel;
using System.Threading.Tasks;
using OpenAI.RealtimeConversation;
using Xunit;

namespace Microsoft.Extensions.AI;

// Note that we're limited on ability to unit-test OpenAIRealtimeExtension, because some of the
// OpenAI types it uses (e.g., ConversationItemStreamingFinishedUpdate) can't be instantiated or
// subclassed from outside. We will mostly have to rely on integration tests for now.

public class OpenAIRealtimeTests
{
    [Fact]
    public void ConvertsAIFunctionToConversationFunctionTool_Basics()
    {
        var input = AIFunctionFactory.Create(() => { }, "MyFunction", "MyDescription");
        var result = input.ToConversationFunctionTool();

        Assert.Equal("MyFunction", result.Name);
        Assert.Equal("MyDescription", result.Description);
    }

    [Fact]
    public void ConvertsAIFunctionToConversationFunctionTool_Parameters()
    {
        var input = AIFunctionFactory.Create(MyFunction);
        var result = input.ToConversationFunctionTool();

        Assert.Equal(nameof(MyFunction), result.Name);
        Assert.Equal("This is a description", result.Description);
        Assert.Equal("""
            {
              "type": "object",
              "properties": {
                "a": {
                  "type": "integer"
                },
                "b": {
                  "description": "Another param",
                  "type": "string"
                },
                "c": {
                  "type": "object",
                  "properties": {
                    "a": {
                      "type": "integer"
                    }
                  },
                  "additionalProperties": false,
                  "required": [
                    "a"
                  ],
                  "default": "null"
                }
              },
              "required": [
                "a",
                "b"
              ]
            }
            """, result.Parameters.ToString());
    }

    [Fact]
    public async Task HandleToolCallsAsync_RejectsNulls()
    {
        var conversationSession = (RealtimeConversationSession)default!;

        // There's currently no way to create a ConversationUpdate instance from outside of the OpenAI
        // library, so we can't validate behavior when a valid ConversationUpdate instance is passed in.

        // Null ConversationUpdate
        using var session = TestRealtimeConversationSession.CreateTestInstance();
        await Assert.ThrowsAsync<ArgumentNullException>(() => conversationSession.HandleToolCallsAsync(
            null!, []));
    }

    [Description("This is a description")]
    private MyType MyFunction(int a, [Description("Another param")] string b, MyType? c = null)
        => throw new NotSupportedException();

    public class MyType
    {
        public int A { get; set; }
    }

    private class TestRealtimeConversationSession : RealtimeConversationSession
    {
        protected internal TestRealtimeConversationSession(RealtimeConversationClient parentClient, Uri endpoint, ApiKeyCredential credential)
            : base(parentClient, endpoint, credential)
        {
        }

        public static TestRealtimeConversationSession CreateTestInstance()
        {
            var credential = new ApiKeyCredential("key");
            return new TestRealtimeConversationSession(
                new RealtimeConversationClient("model", credential),
                new Uri("http://endpoint"), credential);
        }
    }
}
