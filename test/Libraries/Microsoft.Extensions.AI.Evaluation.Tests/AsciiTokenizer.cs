// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.ML.Tokenizers;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

internal class AsciiTokenizer : Tokenizer
{
    public override OperationStatus Decode(
        IEnumerable<int> ids,
        Span<char> destination,
        out int idsConsumed,
        out int charsWritten)
    {
        idsConsumed = 0;
        charsWritten = 0;

        foreach (int id in ids)
        {
            if (charsWritten >= destination.Length)
            {
                return OperationStatus.DestinationTooSmall;
            }

            destination[charsWritten] = (char)id;
            idsConsumed++;
            charsWritten++;
        }

        return OperationStatus.Done;
    }

    protected override EncodeResults<EncodedToken> EncodeToTokens(
        string? text,
        ReadOnlySpan<char> textSpan,
        EncodeSettings settings)
    {
        ReadOnlySpan<char> source = text != null ? text.AsSpan() : textSpan;
        var tokens = new List<EncodedToken>();

        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            tokens.Add(new EncodedToken(id: c, value: c.ToString(), offset: new Range(i, i + 1)));
        }

        return new EncodeResults<EncodedToken> { Tokens = tokens };
    }
}
