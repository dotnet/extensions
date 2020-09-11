// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class SyntaxNodeExtensions
    {
        public static bool ContainsOnlyWhitespace(this SyntaxNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var tokens = node.GetTokens();

            for (var i = 0; i < tokens.Count; i++)
            {
                var tokenKind = tokens[i].Kind;
                if (tokenKind != SyntaxKind.Whitespace && tokenKind != SyntaxKind.NewLine)
                {
                    return false;
                }
            }

            // All tokens were either whitespace or newlines.
            return true;
        }

        public static LinePositionSpan GetLinePositionSpan(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var start = node.Position;
            var end = node.EndPosition;

            Debug.Assert(start <= source.Length && end <= source.Length, "Node position exceeds source length.");

            if (start == source.Length && node.FullWidth == 0)
            {
                // Marker symbol at the end of the document.
                var location = node.GetSourceLocation(source);
                var position = GetLinePosition(location);
                return new LinePositionSpan(position, position);
            }

            var startLocation = source.Lines.GetLocation(start);
            var endLocation = source.Lines.GetLocation(end);
            var startPosition = GetLinePosition(startLocation);
            var endPosition = GetLinePosition(endLocation);

            return new LinePositionSpan(startPosition, endPosition);

            static LinePosition GetLinePosition(SourceLocation location)
            {
                return new LinePosition(location.LineIndex, location.CharacterIndex);
            }
        }

        public static Range GetRange(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var lineSpan = node.GetLinePositionSpan(source);
            var range = new Range(
                new Position(lineSpan.Start.Line, lineSpan.Start.Character),
                new Position(lineSpan.End.Line, lineSpan.End.Character));

            return range;
        }
    }
}
