// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServerClient.Razor;

namespace Microsoft.VisualStudio.Text
{
    internal static class TextBufferExtensions
    {
        private static string HostDocumentVersionMarked = "__Razor_HostDocumentVersionMarker__";

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

        public static bool IsRazorLSPBuffer(this ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            var matchesContentType = textBuffer.ContentType.IsOfType(RazorLSPConstants.RazorLSPContentTypeName);
            return matchesContentType;
        }

        public static void MakeEmptyEdit(this ITextBuffer textBuffer)
        {
            var bufferLength = textBuffer.CurrentSnapshot.Length;
            using var edit = textBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
            edit.Insert(bufferLength, " ");
            edit.Apply();

            using var revertEdit = textBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
            revertEdit.Delete(bufferLength, 1);
            revertEdit.Apply();
        }
    }
}
