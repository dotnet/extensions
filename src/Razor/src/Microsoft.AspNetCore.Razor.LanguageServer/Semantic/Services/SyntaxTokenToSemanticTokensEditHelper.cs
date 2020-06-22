// Copyright(c).NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services
{
    internal static class SyntaxTokenToSemanticTokensEditHelper
    {
        // The below algorithm was taken from OmniSharp/csharp-language-server-protocol at
        // https://github.com/OmniSharp/csharp-language-server-protocol/blob/bdec4c73240be52fbb25a81f6ad7d409f77b5215/src/Protocol/Document/Proposals/SemanticTokensDocument.cs#L156
        public static SemanticTokensOrSemanticTokensEdits ConvertSyntaxTokensToSemanticEdits(
            SemanticTokens newTokens,
            IReadOnlyList<uint> previousResults)
        {
            var oldData = previousResults;

            if (oldData is null || oldData.Count == 0)
            {
                return newTokens;
            }

            var prevData = oldData;
            var prevDataLength = oldData.Count;
            var dataLength = newTokens.Data.Length;
            var startIndex = 0;
            while (startIndex < dataLength
                && startIndex < prevDataLength
                && prevData[startIndex] == newTokens.Data[startIndex])
            {
                startIndex++;
            }

            if (startIndex < dataLength && startIndex < prevDataLength)
            {
                // Find end index
                var endIndex = 0;
                while (endIndex < dataLength
                    && endIndex < prevDataLength
                    && prevData[prevDataLength - 1 - endIndex] == newTokens.Data[dataLength - 1 - endIndex])
                {
                    endIndex++;
                }

                var newData = ImmutableArray.Create(newTokens.Data, startIndex, dataLength - endIndex - startIndex);
                var result = new SemanticTokensEditCollection
                {
                    ResultId = newTokens.ResultId,
                    Edits = new[] {
                        new SemanticTokensEdit {
                            Start = startIndex,
                            DeleteCount = prevDataLength - endIndex - startIndex,
                            Data = newData
                        }
                    }
                };
                return result;
            }

            if (startIndex < dataLength)
            {
                return new SemanticTokensEditCollection
                {
                    ResultId = newTokens.ResultId,
                    Edits = new[] {
                        new SemanticTokensEdit {
                            Start = startIndex,
                            DeleteCount = 0,
                            Data = ImmutableArray.Create(newTokens.Data, startIndex, newTokens.Data.Length - startIndex)
                        }
                    }
                };
            }

            if (startIndex < prevDataLength)
            {
                return new SemanticTokensEditCollection
                {
                    ResultId = newTokens.ResultId,
                    Edits = new[] {
                        new SemanticTokensEdit {
                            Start = startIndex,
                            DeleteCount = prevDataLength - startIndex
                        }
                    }
                };
            }

            return new SemanticTokensEditCollection
            {
                ResultId = newTokens.ResultId,
                Edits = Array.Empty<SemanticTokensEdit>()
            };
        }
    }
}
