// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class PositionExtensions
    {
        public static int GetAbsoluteIndex(this Position position, SourceText sourceText)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            var linePosition = new LinePosition(position.Line, position.Character);
            if (linePosition.Line >= sourceText.Lines.Count)
            {
                throw new ArgumentOutOfRangeException($"Line '{position.Line}' outside of the {nameof(sourceText)} range of '{sourceText.Lines.Count}' was queried. The document may not be up to date.");
            }
            var index = sourceText.Lines.GetPosition(linePosition);
            return index;
        }

        public static int CompareTo(this Position position, Position other)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var result = position.Line.CompareTo(other.Line);
            return (result != 0) ? result : position.Character.CompareTo(other.Character);
        }
    }
}
