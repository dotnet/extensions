// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

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
    }
}
