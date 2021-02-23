// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class SnapshotPointExtensions
    {
        public static Position AsPosition(this SnapshotPoint point)
        {
            var line = point.GetContainingLine();
            var character = point.Position - line.Start.Position;
            var lineNumber = line.LineNumber;
            var position = new Position()
            {
                Character = character,
                Line = lineNumber,
            };

            return position;
        }
    }
}
