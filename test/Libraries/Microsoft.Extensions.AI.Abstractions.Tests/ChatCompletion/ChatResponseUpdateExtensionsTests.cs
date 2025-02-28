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

namespace Microsoft.Extensions.AI;

public class ChatResponseUpdateExtensionsTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((List<ChatResponseUpdate>)null!).ToChatResponse());
    }

    public static IEnumerable<object?[]> ToChatResponse_SuccessfullyCreatesResponse_MemberData()
    {
        foreach (bool useAsync in new[] { false, true })
        {
            foreach (bool? coalesceContent in new bool?[] { null, false, true })
            {
                yield return new object?[] { useAsync, coalesceContent };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ToChatResponse_SuccessfullyCreatesResponse_MemberData))]
    public async Task ToChatResponse_SuccessfullyCreatesResponse(bool useAsync, bool? coalesceContent)
    {
        ChatResponseUpdate[] updates =
        [
            new() { Text = "Hello", ResponseId = "12345", CreatedAt = new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), ModelId = "model123" },
            new() { Text = ", ", AuthorName = "Someone", Role = new ChatRole("human"), AdditionalProperties = new() { ["a"] = "b" } },
            new() { Text = "world!", CreatedAt = new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), ChatThreadId = "123", AdditionalProperties = new() { ["c"] = "d" } },

            new() { Contents = [new UsageContent(new() { InputTokenCount = 1, OutputTokenCount = 2 })] },
            new() { Contents = [new UsageContent(new() { InputTokenCount = 4, OutputTokenCount = 5 })] },
        ];

        ChatResponse response = (coalesceContent is bool, useAsync) switch
        {
            (false, false) => updates.ToChatResponse(),
            (false, true) => await YieldAsync(updates).ToChatResponseAsync(),

            (true, false) => updates.ToChatResponse(coalesceContent.GetValueOrDefault()),
            (true, true) => await YieldAsync(updates).ToChatResponseAsync(coalesceContent.GetValueOrDefault()),
        };
        Assert.NotNull(response);

        Assert.NotNull(response.Usage);
        Assert.Equal(5, response.Usage.InputTokenCount);
        Assert.Equal(7, response.Usage.OutputTokenCount);

        Assert.Equal("12345", response.ResponseId);
        Assert.Equal(new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), response.CreatedAt);
        Assert.Equal("model123", response.ModelId);

        Assert.Equal("123", response.ChatThreadId);

        ChatMessage message = response.Message;
        Assert.Equal(new ChatRole("human"), message.Role);
        Assert.Equal("Someone", message.AuthorName);
        Assert.Null(message.AdditionalProperties);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(2, response.AdditionalProperties.Count);
        Assert.Equal("b", response.AdditionalProperties["a"]);
        Assert.Equal("d", response.AdditionalProperties["c"]);

        if (coalesceContent is null or true)
        {
            Assert.Equal("Hello, world!", response.Message.Text);
        }
        else
        {
            Assert.Equal("Hello", response.Message.Contents[0].ToString());
            Assert.Equal(", ", response.Message.Contents[1].ToString());
            Assert.Equal("world!", response.Message.Contents[2].ToString());
        }
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
                updates.Add(new() { Text = text });
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
                updates.Add(new() { Contents = [new DataContent("https://uri", mediaType: "image/png")] });
            }
        }

        ChatResponse response = useAsync ? await YieldAsync(updates).ToChatResponseAsync() : updates.ToChatResponse();
        Assert.NotNull(response);

        ChatMessage message = response.Message;
        Assert.NotNull(message);

        Assert.Equal(expected.Count + (gapLength * ((numSequences - 1) + (gapBeginningEnd ? 2 : 0))), message.Contents.Count);

        TextContent[] contents = message.Contents.OfType<TextContent>().ToArray();
        Assert.Equal(expected.Count, contents.Length);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], contents[i].Text);
        }
    }

    [Fact]
    public async Task ToChatResponse_UsageContentExtractedFromContents()
    {
        ChatResponseUpdate[] updates =
        {
            new() { Text = "Hello, " },
            new() { Text = "world!" },
            new() { Contents = [new UsageContent(new() { TotalTokenCount = 42 })] },
        };

        ChatResponse response = await YieldAsync(updates).ToChatResponseAsync();

        Assert.NotNull(response);

        Assert.NotNull(response.Usage);
        Assert.Equal(42, response.Usage.TotalTokenCount);

        Assert.Equal("Hello, world!", Assert.IsType<TextContent>(Assert.Single(response.Message.Contents)).Text);
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
