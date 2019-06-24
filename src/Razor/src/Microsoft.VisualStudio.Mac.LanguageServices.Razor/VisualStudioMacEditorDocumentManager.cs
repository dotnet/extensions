// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor.Documents;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    internal class VisualStudioMacEditorDocumentManager : EditorDocumentManagerBase
    {
        public VisualStudioMacEditorDocumentManager(
            ForegroundDispatcher foregroundDispatcher,
            FileChangeTrackerFactory fileChangeTrackerFactory)
            : base(foregroundDispatcher, fileChangeTrackerFactory)
        {
        }

        protected override ITextBuffer GetTextBufferForOpenDocument(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // VS for Mac doesn't currently support find all references support yet.
            return null;
        }

        protected override void OnDocumentOpened(EditorDocument document)
        {
        }

        protected override void OnDocumentClosed(EditorDocument document)
        {
        }

        public void HandleDocumentOpened(string filePath, ITextBuffer textBuffer)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (!TryGetMatchingDocuments(filePath, out var documents))
                {
                    // This isn't a document that we're interesting in.
                    return;
                }

                BufferLoaded(textBuffer, filePath, documents);
            }
        }

        public void HandleDocumentClosed(string filePath)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                if (!TryGetMatchingDocuments(filePath, out var documents))
                {
                    return;
                }

                // We have to deal with some complications here due to renames and event ordering and such.
                // We might see multiple documents open for a cookie (due to linked files), but only one of them 
                // has been renamed. In that case, we just process the change that we know about.
                var matchingFilePaths = documents.Select(d => d.DocumentFilePath);
                var filePaths = new HashSet<string>(matchingFilePaths, FilePathComparer.Instance);

                foreach (var file in filePaths)
                {
                    DocumentClosed(file);
                }
            }
        }

        public void HandleDocumentRenamed(string fromFilePath, string toFilePath, ITextBuffer textBuffer)
        {
            ForegroundDispatcher.AssertForegroundThread();

            if (string.Equals(fromFilePath, toFilePath, FilePathComparison.Instance))
            {
                return;
            }

            lock (_lock)
            {
                // Treat a rename as a close + reopen.
                //
                // Due to ordering issues, we could see a partial rename. This is why we need to pass the new
                // file path here.
                DocumentClosed(fromFilePath);
            }

            DocumentOpened(toFilePath, textBuffer);
        }

        public void BufferLoaded(ITextBuffer textBuffer, string filePath, EditorDocument[] documents)
        {
            ForegroundDispatcher.AssertForegroundThread();

            lock (_lock)
            {
                for (var i = 0; i < documents.Length; i++)
                {
                    DocumentOpened(filePath, textBuffer);
                }
            }
        }
    }
}
