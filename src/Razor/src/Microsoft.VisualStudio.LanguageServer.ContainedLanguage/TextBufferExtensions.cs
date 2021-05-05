// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Text
{
    internal static class TextBufferExtensions
    {
        private static readonly string HostDocumentVersionMarked = "__MsLsp_HostDocumentVersionMarker__";

        public static void SetHostDocumentSyncVersion(this ITextBuffer textBuffer, long hostDocumentVersion)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            textBuffer.Properties[HostDocumentVersionMarked] = hostDocumentVersion;
        }

        public static bool TryGetHostDocumentSyncVersion(this ITextBuffer textBuffer, out long hostDocumentVersion)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            var result = textBuffer.Properties.TryGetProperty(HostDocumentVersionMarked, out hostDocumentVersion);

            return result;
        }
    }
}
