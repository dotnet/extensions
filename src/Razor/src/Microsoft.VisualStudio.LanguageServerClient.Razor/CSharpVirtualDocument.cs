// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class CSharpVirtualDocument : VirtualDocument
    {
        private long? _hostDocumentSyncVersion;
        private CSharpVirtualDocumentSnapshot _currentSnapshot;

        public CSharpVirtualDocument(Uri uri, ITextBuffer textBuffer)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            Uri = uri;
            TextBuffer = textBuffer;
            _currentSnapshot = UpdateSnapshot();
        }

        public override Uri Uri { get; }

        public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

        public override ITextBuffer TextBuffer { get; }

        public override VirtualDocumentSnapshot CurrentSnapshot => _currentSnapshot;

        public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, long hostDocumentVersion)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            _hostDocumentSyncVersion = hostDocumentVersion;
            TextBuffer.SetHostDocumentSyncVersion(_hostDocumentSyncVersion.Value);

            if (changes.Count == 0)
            {
                // Even though nothing changed here, we want the synchronizer to be aware of the host document version change.
                // So, let's make an empty edit to invoke the text buffer Changed events.
                TextBuffer.MakeEmptyEdit();

                _currentSnapshot = UpdateSnapshot();
                return _currentSnapshot;
            }

            using var edit = TextBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                if (change.IsDelete())
                {
                    edit.Delete(change.OldSpan.Start, change.OldSpan.Length);
                }
                else if (change.IsReplace())
                {
                    edit.Replace(change.OldSpan.Start, change.OldSpan.Length, change.NewText);
                }
                else if (change.IsInsert())
                {
                    edit.Insert(change.OldSpan.Start, change.NewText);
                }
                else
                {
                    throw new InvalidOperationException("Unknown edit type when updating LSP C# buffer.");
                }
            }

            edit.Apply();
            _currentSnapshot = UpdateSnapshot();

            return _currentSnapshot;
        }

        private CSharpVirtualDocumentSnapshot UpdateSnapshot() => new CSharpVirtualDocumentSnapshot(Uri, TextBuffer.CurrentSnapshot, HostDocumentSyncVersion);
    }
}
