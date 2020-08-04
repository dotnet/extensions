// Copyright(c).NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services
{
    internal class SemanticTokensEditsDiffer : TextDiffer
    {
        private SemanticTokensEditsDiffer(uint[] oldArray, uint[] newArray)
        {
            if (oldArray is null)
            {
                throw new ArgumentNullException(nameof(oldArray));
            }

            if (newArray is null)
            {
                throw new ArgumentNullException(nameof(newArray));
            }

            OldArray = oldArray;
            NewArray = newArray;
        }

        private uint[] OldArray { get; }
        private uint[] NewArray { get; }

        protected override int OldTextLength => OldArray.Length;
        protected override int NewTextLength => NewArray.Length;

        protected override bool ContentEquals(int oldTextIndex, int newTextIndex)
        {
            return OldArray[oldTextIndex] == NewArray[newTextIndex];
        }

        public static SemanticTokensOrSemanticTokensEdits ComputeSemanticTokensEdits(
            SemanticTokens newTokens,
            IReadOnlyList<uint> previousResults)
        {
            var differ = new SemanticTokensEditsDiffer(previousResults.ToArray(), newTokens.Data);
            var diffs = differ.ComputeDiff();
            var edits = differ.ProcessEdits(diffs);
            var result = new SemanticTokensEditCollection
            {
                ResultId = newTokens.ResultId,
                Edits = edits,
            };

            return result;
        }

        private IReadOnlyList<SemanticTokensEdit> ProcessEdits(IReadOnlyList<DiffEdit> diffs)
        {
            var results = new List<SemanticTokensEdit>();
            foreach (var diff in diffs)
            {
                var current = results.Count > 0 ? results[results.Count - 1] : null;
                switch (diff.Operation)
                {
                    case DiffEdit.Type.Delete:
                        if (current != null &&
                            current.Start + current.DeleteCount == diff.Position)
                        {
                            current.DeleteCount += 1;
                        }
                        else
                        {
                            results.Add(new SemanticTokensEdit
                            {
                                Start = diff.Position,
                                Data = Array.Empty<uint>(),
                                DeleteCount = 1,
                            });
                        }
                        break;
                    case DiffEdit.Type.Insert:
                        if (current != null &&
                            current.Data.Any() &&
                            current.Start == diff.Position)
                        {
                            current.Data = current.Data.Append(NewArray[diff.NewTextPosition.Value]);
                        }
                        else
                        {
                            results.Add(new SemanticTokensEdit
                            {
                                Start = diff.Position,
                                Data = new uint[] { NewArray[diff.NewTextPosition.Value] },
                                DeleteCount = 0,
                            });
                        }
                        break;
                }
            }

            return results;
        }
    }
}
