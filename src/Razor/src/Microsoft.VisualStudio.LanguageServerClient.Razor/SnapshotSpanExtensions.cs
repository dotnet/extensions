// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal static class SnapshotSpanExtensions
    {
        public static Range AsRange(this SnapshotSpan span)
        {
            var startPosition = span.Start.AsPosition();
            var endPosition = span.End.AsPosition();
            var range = new Range()
            {
                Start = startPosition,
                End = endPosition,
            };

            return range;
        }
    }
}
