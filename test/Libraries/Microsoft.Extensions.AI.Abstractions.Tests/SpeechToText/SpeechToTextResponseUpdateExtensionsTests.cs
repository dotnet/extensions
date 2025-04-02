// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextResponseUpdateExtensionsTests
{
    public static IEnumerable<object[]> ToSpeechToTextResponse_Coalescing_VariousSequenceAndGapLengths_MemberData()
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

    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("updates", () => ((List<SpeechToTextResponseUpdate>)null!).ToSpeechToTextResponse());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToSpeechToTextResponse_SuccessfullyCreatesResponse(bool useAsync)
    {
        SpeechToTextResponseUpdate[] updates =
        [
            new("Hello ") { ModelId = "model123", StartTime = null, AdditionalProperties = new() { ["a"] = "b" } },
            new("human, ") { ModelId = "model123", StartTime = TimeSpan.FromSeconds(10), EndTime = TimeSpan.FromSeconds(20) },
            new("How ") { ModelId = "model123", StartTime = TimeSpan.FromSeconds(22), EndTime = TimeSpan.FromSeconds(23) },
            new("are ") { ModelId = "model123", StartTime = TimeSpan.FromSeconds(23), EndTime = TimeSpan.FromSeconds(24) },
            new([new TextContent("You?")]) { ModelId = "model123", StartTime = TimeSpan.FromSeconds(24), EndTime = TimeSpan.FromSeconds(25), AdditionalProperties = new() { ["c"] = "d" } },
            new() { ResponseId = "someResponse", ModelId = "model123", StartTime = TimeSpan.FromSeconds(25), EndTime = TimeSpan.FromSeconds(35) },
        ];

        SpeechToTextResponse response = useAsync ?
            updates.ToSpeechToTextResponse() :
            await YieldAsync(updates).ToSpeechToTextResponseAsync();

        Assert.NotNull(response);

        Assert.Equal("someResponse", response.ResponseId);
        Assert.Equal(TimeSpan.FromSeconds(10), response.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(35), response.EndTime);
        Assert.Equal("model123", response.ModelId);

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal(2, response.AdditionalProperties.Count);
        Assert.Equal("b", response.AdditionalProperties["a"]);
        Assert.Equal("d", response.AdditionalProperties["c"]);

        Assert.Equal("Hello human, How are You?", response.Text);
    }

    [Theory]
    [MemberData(nameof(ToSpeechToTextResponse_Coalescing_VariousSequenceAndGapLengths_MemberData))]
    public async Task ToSpeechToTextResponse_Coalescing_VariousSequenceAndGapLengths(bool useAsync, int numSequences, int sequenceLength, int gapLength, bool gapBeginningEnd)
    {
        List<SpeechToTextResponseUpdate> updates = [];

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
                updates.Add(new(text));
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

        SpeechToTextResponse response = useAsync ? await YieldAsync(updates).ToSpeechToTextResponseAsync() : updates.ToSpeechToTextResponse();
        Assert.NotNull(response);

        Assert.Equal(expected.Count + (gapLength * ((numSequences - 1) + (gapBeginningEnd ? 2 : 0))), response.Contents.Count);

        TextContent[] contents = response.Contents.OfType<TextContent>().ToArray();
        Assert.Equal(expected.Count, contents.Length);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], contents[i].Text);
        }
    }

    private static async IAsyncEnumerable<SpeechToTextResponseUpdate> YieldAsync(IEnumerable<SpeechToTextResponseUpdate> updates)
    {
        foreach (SpeechToTextResponseUpdate update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
