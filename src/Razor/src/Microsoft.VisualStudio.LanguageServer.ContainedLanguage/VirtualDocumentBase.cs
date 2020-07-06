// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class VirtualDocumentBase<T> : VirtualDocument where T : VirtualDocumentSnapshot
    {
        private T _currentSnapshot;
        private long? _hostDocumentSyncVersion;

        protected VirtualDocumentBase(Uri uri, ITextBuffer textBuffer)
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
            _currentSnapshot = GetUpdatedSnapshot();
        }

        public override Uri Uri { get; }

        public override ITextBuffer TextBuffer { get; }

        public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

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

                _currentSnapshot = GetUpdatedSnapshot();
                return _currentSnapshot;
            }

            using var edit = TextBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                edit.Replace(change.OldSpan.Start, change.OldSpan.Length, change.NewText);
            }

            edit.Apply();
            _currentSnapshot = GetUpdatedSnapshot();

            return _currentSnapshot;
        }

        protected abstract T GetUpdatedSnapshot();
    }
}
