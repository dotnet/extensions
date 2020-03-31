// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class CSharpVirtualDocument : VirtualDocument
    {
        private long? _hostDocumentSyncVersion;
        private CSharpVirtualDocumentSnapshot _previousSnapshot;
        private CSharpVirtualDocumentSnapshot _currentSnapshot;

        private bool _hasProvisionalChanges = false;

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
            _previousSnapshot = null;
            _currentSnapshot = UpdateSnapshot();
        }

        public override Uri Uri { get; }

        public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

        public override ITextBuffer TextBuffer { get; }

        public override VirtualDocumentSnapshot CurrentSnapshot => _currentSnapshot;

        public override VirtualDocumentSnapshot Update(IReadOnlyList<TextChange> changes, long hostDocumentVersion, bool provisional = false)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            _hostDocumentSyncVersion = hostDocumentVersion;

            TryRevertProvisionalChanges();
            _hasProvisionalChanges = provisional;

            if (changes.Count == 0)
            {
                _currentSnapshot = UpdateSnapshot();
                return _currentSnapshot;
            }

            using var edit = TextBuffer.CreateEdit();
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                if (change.IsDelete())
                {
                    edit.Delete(change.Span.Start, change.Span.Length);
                }
                else if (change.IsReplace())
                {
                    edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                }
                else if (change.IsInsert())
                {
                    edit.Insert(change.Span.Start, change.NewText);
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

        private bool TryRevertProvisionalChanges()
        {
            if (!_hasProvisionalChanges)
            {
                return false;
            }

            Debug.Assert(_previousSnapshot != null);

            using var revertEdit = TextBuffer.CreateEdit(EditOptions.None, _previousSnapshot.Snapshot.Version.VersionNumber, InviolableEditTag.Instance);
            var previousChanges = _previousSnapshot.Snapshot.Version.Changes;
            for (var i = 0; i < previousChanges.Count; i++)
            {
                var change = previousChanges[i];
                revertEdit.Replace(change.NewSpan, change.OldText);
            }

            revertEdit.Apply();

            _hasProvisionalChanges = false;

            return true;
        }

        private CSharpVirtualDocumentSnapshot UpdateSnapshot()
        {
            _previousSnapshot = _currentSnapshot;
            return new CSharpVirtualDocumentSnapshot(Uri, TextBuffer.CurrentSnapshot, HostDocumentSyncVersion);
        }

        // This indicates that no other entity should respond to the edit event associated with this tag.
        private class InviolableEditTag : IInviolableEditTag
        {
            private InviolableEditTag() { }

            public readonly static IInviolableEditTag Instance = new InviolableEditTag();
        }
    }
}
