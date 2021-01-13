// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable

using System;
using System.Diagnostics;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class SemanticRange : IComparable<SemanticRange>
    {
        public SemanticRange(int kind, Range range, int modifier)
        {
            Kind = kind;
            Modifier = modifier;
            Range = range;
        }

        public Range Range { get; }

        public int Kind { get; }

        public int Modifier { get; }

        public int CompareTo(SemanticRange other)
        {
            if (other is null)
            {
                return 1;
            }

            Debug.Assert(Range.Start.CompareTo(other.Range.Start) == 0 || !Range.OverlapsWith(other.Range), $"{this} overlapped with {other}");

            // Since overlapping SemanticRanges are STRICTLY FORBIDDEN we need only compare the starts 
            return Range.Start.CompareTo(other.Range.Start);
        }

        public override string ToString()
        {
            return $"[Kind: {Kind}, Range: {Range}]";
        }
    }
}
