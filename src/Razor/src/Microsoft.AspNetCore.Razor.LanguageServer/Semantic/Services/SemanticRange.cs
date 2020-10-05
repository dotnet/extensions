// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class SemanticRange : IComparable<SemanticRange>
    {
        public SemanticRange(int kind, Range range, int modifier)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            Kind = kind;
            Modifier = modifier;
            Range = range;
        }

        public Range Range { get; set; }

        public int Kind { get; set; }

        public int Modifier { get; set; }

        public int CompareTo(SemanticRange other)
        {
            if (other is null)
            {
                return 1;
            }

            if (other.Range is null && Range is null)
            {
                return 0;
            }

            Debug.Assert(Range.Start.CompareTo(other.Range.Start) == 0 || !Range.OverlapsWith(other.Range));

            // Since overlapping SemanticRanges are STRICTLY FORBIDDEN we need only compare the starts 
            return Range.Start.CompareTo(other.Range.Start);
        }
    }
}
