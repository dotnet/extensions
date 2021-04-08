// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class RangeExtensions
    {
        public static readonly Range UndefinedRange = new Range
        {
            Start = new Position(-1, -1),
            End = new Position(-1, -1)
        };

        public static bool OverlapsWith(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var overlapStart = range.Start;
            if (range.Start.CompareTo(other.Start) < 0)
            {
                overlapStart = other.Start;
            }

            var overlapEnd = range.End;
            if (range.End.CompareTo(other.End) > 0)
            {
                overlapEnd = other.End;
            }

            // Empty ranges do not overlap with any range.
            return overlapStart.CompareTo(overlapEnd) < 0;
        }

        public static bool LineOverlapsWith(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var overlapStart = range.Start.Line;
            if (range.Start.Line.CompareTo(other.Start.Line) < 0)
            {
                overlapStart = other.Start.Line;
            }

            var overlapEnd = range.End.Line;
            if (range.End.Line.CompareTo(other.End.Line) > 0)
            {
                overlapEnd = other.End.Line;
            }

            return overlapStart.CompareTo(overlapEnd) <= 0;
        }

        public static Range Overlap(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var overlapStart = range.Start;
            if (range.Start.CompareTo(other.Start) < 0)
            {
                overlapStart = other.Start;
            }

            var overlapEnd = range.End;
            if (range.End.CompareTo(other.End) > 0)
            {
                overlapEnd = other.End;
            }

            // Empty ranges do not overlap with any range.
            if (overlapStart.CompareTo(overlapEnd) < 0)
            {
                return new Range(overlapStart, overlapEnd);
            }

            return null;
        }

        public static bool Contains(this Range range, Range other)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return range.Start.CompareTo(other.Start) <= 0 && range.End.CompareTo(other.End) >= 0;
        }

        public static TextSpan AsTextSpan(this Range range, SourceText sourceText)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var start = sourceText.Lines[(int)range.Start.Line].Start + (int)range.Start.Character;
            var end = sourceText.Lines[(int)range.End.Line].Start + (int)range.End.Character;
            return new TextSpan(start, end - start);
        }

        public static bool IsUndefined(this Range range)
        {
            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            return range == UndefinedRange;
        }
    }
}
