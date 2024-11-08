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

public class StreamingChatCompletionUpdateExtensionsTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((List<StreamingChatCompletionUpdate>)null!).ToChatCompletion());
    }

    public static IEnumerable<object?[]> ToChatCompletion_SuccessfullyCreatesCompletion_MemberData()
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
    [MemberData(nameof(ToChatCompletion_SuccessfullyCreatesCompletion_MemberData))]
    public async Task ToChatCompletion_SuccessfullyCreatesCompletion(bool useAsync, bool? coalesceContent)
    {
        StreamingChatCompletionUpdate[] updates =
        [
            new() { ChoiceIndex = 0, Text = "Hello", CompletionId = "12345", CreatedAt = new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), ModelId = "model123" },
            new() { ChoiceIndex = 1, Text = "Hey", CompletionId = "12345", CreatedAt = new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), ModelId = "model124" },

            new() { ChoiceIndex = 0, Text = ", ", AuthorName = "Someone", Role = ChatRole.User, AdditionalProperties = new() { ["a"] = "b" } },
            new() { ChoiceIndex = 1, Text = ", ", AuthorName = "Else", Role = ChatRole.System, AdditionalProperties = new() { ["g"] = "h" } },

            new() { ChoiceIndex = 0, Text = "world!", CreatedAt = new DateTimeOffset(2, 2, 3, 4, 5, 6, TimeSpan.Zero), AdditionalProperties = new() { ["c"] = "d" } },
            new() { ChoiceIndex = 1, Text = "you!", Role = ChatRole.Tool, CreatedAt = new DateTimeOffset(3, 2, 3, 4, 5, 6, TimeSpan.Zero), AdditionalProperties = new() { ["e"] = "f", ["i"] = 42 } },

            new() { ChoiceIndex = 0, Contents = new[] { new UsageContent(new() { InputTokenCount = 1, OutputTokenCount = 2 }) } },
            new() { ChoiceIndex = 3, Contents = new[] { new UsageContent(new() { InputTokenCount = 4, OutputTokenCount = 5 }) } },
        ];

        ChatCompletion completion = (coalesceContent is bool, useAsync) switch
        {
            (false, false) => updates.ToChatCompletion(),
            (false, true) => await YieldAsync(updates).ToChatCompletionAsync(),

            (true, false) => updates.ToChatCompletion(coalesceContent.GetValueOrDefault()),
            (true, true) => await YieldAsync(updates).ToChatCompletionAsync(coalesceContent.GetValueOrDefault()),
        };
        Assert.NotNull(completion);

        Assert.Equal("12345", completion.CompletionId);
        Assert.Equal(new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), completion.CreatedAt);
        Assert.Equal("model123", completion.ModelId);
        Assert.Same(Assert.IsType<UsageContent>(updates[6].Contents[0]).Details, completion.Usage);

        Assert.Equal(3, completion.Choices.Count);

        ChatMessage message = completion.Choices[0];
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("Someone", message.AuthorName);
        Assert.NotNull(message.AdditionalProperties);
        Assert.Equal(2, message.AdditionalProperties.Count);
        Assert.Equal("b", message.AdditionalProperties["a"]);
        Assert.Equal("d", message.AdditionalProperties["c"]);

        message = completion.Choices[1];
        Assert.Equal(ChatRole.System, message.Role);
        Assert.Equal("Else", message.AuthorName);
        Assert.NotNull(message.AdditionalProperties);
        Assert.Equal(3, message.AdditionalProperties.Count);
        Assert.Equal("h", message.AdditionalProperties["g"]);
        Assert.Equal("f", message.AdditionalProperties["e"]);
        Assert.Equal(42, message.AdditionalProperties["i"]);

        message = completion.Choices[2];
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Null(message.AuthorName);
        Assert.Null(message.AdditionalProperties);
        Assert.Same(updates[7].Contents[0], Assert.Single(message.Contents));

        if (coalesceContent is null or true)
        {
            Assert.Equal("Hello, world!", completion.Choices[0].Text);
            Assert.Equal("Hey, you!", completion.Choices[1].Text);
            Assert.Null(completion.Choices[2].Text);
        }
        else
        {
            Assert.Equal("Hello", completion.Choices[0].Contents[0].ToString());
            Assert.Equal(", ", completion.Choices[0].Contents[1].ToString());
            Assert.Equal("world!", completion.Choices[0].Contents[2].ToString());

            Assert.Equal("Hey", completion.Choices[1].Contents[0].ToString());
            Assert.Equal(", ", completion.Choices[1].Contents[1].ToString());
            Assert.Equal("you!", completion.Choices[1].Contents[2].ToString());

            Assert.Null(completion.Choices[2].Text);
        }
    }

    public static IEnumerable<object[]> ToChatCompletion_Coalescing_VariousSequenceAndGapLengths_MemberData()
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
    [MemberData(nameof(ToChatCompletion_Coalescing_VariousSequenceAndGapLengths_MemberData))]
    public async Task ToChatCompletion_Coalescing_VariousSequenceAndGapLengths(bool useAsync, int numSequences, int sequenceLength, int gapLength, bool gapBeginningEnd)
    {
        List<StreamingChatCompletionUpdate> updates = [];

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
                updates.Add(new() { Contents = [new ImageContent("https://uri")] });
            }
        }

        ChatCompletion completion = useAsync ? await YieldAsync(updates).ToChatCompletionAsync() : updates.ToChatCompletion();
        Assert.Single(completion.Choices);

        ChatMessage message = completion.Message;
        Assert.Equal(expected.Count + (gapLength * ((numSequences - 1) + (gapBeginningEnd ? 2 : 0))), message.Contents.Count);

        TextContent[] contents = message.Contents.OfType<TextContent>().ToArray();
        Assert.Equal(expected.Count, contents.Length);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], contents[i].Text);
        }
    }

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> YieldAsync(IEnumerable<StreamingChatCompletionUpdate> updates)
    {
        foreach (StreamingChatCompletionUpdate update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
