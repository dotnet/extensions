// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models.Proposals;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services
{
    internal class SemanticTokensEditsDiffer : TextDiffer
    {
        private SemanticTokensEditsDiffer(IReadOnlyList<int> oldArray, ImmutableArray<int> newArray)
        {
            if (oldArray is null)
            {
                throw new ArgumentNullException(nameof(oldArray));
            }

            OldArray = oldArray;
            NewArray = newArray;
        }

        private IReadOnlyList<int> OldArray { get; }
        private ImmutableArray<int> NewArray { get; }

        protected override int OldTextLength => OldArray.Count;
        protected override int NewTextLength => NewArray.Length;

        protected override bool ContentEquals(int oldTextIndex, int newTextIndex)
        {
            return OldArray[oldTextIndex] == NewArray[newTextIndex];
        }

        public static SemanticTokensFullOrDelta ComputeSemanticTokensEdits(
            SemanticTokens newTokens,
            IReadOnlyList<int> previousResults)
        {
            var differ = new SemanticTokensEditsDiffer(previousResults, newTokens.Data);
            var diffs = differ.ComputeDiff();
            var edits = differ.ProcessEdits(diffs);
            var result = new SemanticTokensDelta
            {
                ResultId = newTokens.ResultId,
                Edits = edits,
            };

            return result;
        }

        private Container<SemanticTokensEdit> ProcessEdits(IReadOnlyList<DiffEdit> diffs)
        {
            var razorResults = new List<RazorSemanticTokensEdit>();
            foreach (var diff in diffs)
            {
                var current = razorResults.Count > 0 ? razorResults[razorResults.Count - 1] : null;
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
                            razorResults.Add(new RazorSemanticTokensEdit
                            {
                                Start = diff.Position,
                                DeleteCount = 1,
                            });
                        }
                        break;
                    case DiffEdit.Type.Insert:
                        if (current != null &&
                            current.Data != null &&
                            current.Data.Count > 0 &&
                            current.Start == diff.Position)
                        {
                            current.Data.Add(NewArray[diff.NewTextPosition!.Value]);
                        }
                        else
                        {
                            var semanticTokensEdit = new RazorSemanticTokensEdit
                            {
                                Start = diff.Position,
                                Data = new List<int>
                                {
                                    NewArray[diff.NewTextPosition!.Value],
                                },
                                DeleteCount = 0,
                            };
                            razorResults.Add(semanticTokensEdit);
                        }
                        break;
                }
            }

            var results = razorResults.Select(e => e.ToSemanticTokensEdit());
            return results.ToList();
        }

        // We need to have a shim class because SemanticTokensEdit.Data is Immutable, so if we operate on it directly then every time we append an element we're allocating an entire new array.
        // In some large (but not implausibly so) copy-paste scenarios that can cause long delays and large allocations.
        private class RazorSemanticTokensEdit
        {
            public int Start { get; set; }
            public int DeleteCount { get; set; }
            public IList<int>? Data { get; set; }

            // Since we need to add to the Data object during ProcessEdits but return an "ImmutableArray" in the end lets wait until the end to convert.
            public SemanticTokensEdit ToSemanticTokensEdit()
            {
                return new SemanticTokensEdit
                {
                    Data = Data?.ToImmutableArray(),
                    Start = Start,
                    DeleteCount = DeleteCount,
                };
            }
        }
    }
}
