// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class ITextSnapshotExtensions
    {
        public static void GetLineAndCharacter(this ITextSnapshot snapshot, int index, out int lineNumber, out int characterIndex)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var line = snapshot.GetLineFromPosition(index);

            lineNumber = line.LineNumber;
            characterIndex = index - line.Start.Position;
        }

        public static TextExtent? GetWordExtent(
            this ITextSnapshot snapshot,
            int line,
            int character,
            ITextStructureNavigatorSelectorService textStructureNavigatorService)
        {
            if (snapshot == null || textStructureNavigatorService == null)
            {
                return null;
            }

            var navigator = textStructureNavigatorService.GetTextStructureNavigator(snapshot.TextBuffer);
            var textSnapshotLine = snapshot.GetLineFromLineNumber(line);
            var absoluteIndex = textSnapshotLine.Start + character;
            if (absoluteIndex > snapshot.Length)
            {
                Debug.Fail("This should never happen given we're operating on snapshots.");
                return null;
            }

            // Lets walk backwards to the character that caused completion (if one triggered it) to ensure that the "GetExtentOfWord" returns
            // the word we care about and not whitespace following it. For instance:
            //
            //      @Date|\r\n
            var completionCharacterIndex = Math.Max(0, absoluteIndex);
            var completionSnapshotPoint = new SnapshotPoint(snapshot, completionCharacterIndex);
            var wordExtent = navigator.GetExtentOfWord(completionSnapshotPoint);

            return wordExtent;
        }
    }
}
