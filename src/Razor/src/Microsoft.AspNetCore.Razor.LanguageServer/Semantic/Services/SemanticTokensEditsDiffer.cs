// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#pragma warning disable CS0618
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
                                Data = ImmutableArray<int>.Empty,
                                DeleteCount = 1,
                            });
                        }
                        break;
                    case DiffEdit.Type.Insert:
                        if (current != null &&
                            current.Data.HasValue &&
                            current.Data.Value.Any() &&
                            current.Start == diff.Position)
                        {
                            current.Data = current.Data.Append(NewArray[diff.NewTextPosition.Value]).ToImmutableArray();
                        }
                        else
                        {
                            var semanticTokensEdit = new SemanticTokensEdit
                            {
                                Start = diff.Position,
                                Data = ImmutableArray.Create(NewArray[diff.NewTextPosition.Value]),
                                DeleteCount = 0,
                            };
                            results.Add(semanticTokensEdit);
                        }
                        break;
                }
            }

            return results;
        }
    }
}
