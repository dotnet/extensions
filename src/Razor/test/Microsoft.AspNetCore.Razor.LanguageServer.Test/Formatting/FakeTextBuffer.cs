// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextBuffer : ITextBuffer
    {
        private readonly object _lock = new object();
        private FakeTextImage _image;
        private FakeTextVersion _version;

        public FakeTextBuffer(FakeTextImage textImage, IContentType contentType)
        {
            _image = textImage;
            _version = new FakeTextVersion(this, (FakeTextImageVersion)textImage.Version);
            CurrentSnapshot = new FakeTextSnapshot(this, _version, textImage);
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }

        public IContentType ContentType { get; private set; }

        public ITextSnapshot CurrentSnapshot { get; private set; }

        public bool EditInProgress => throw new NotImplementedException();

        public PropertyCollection Properties { get; } = new PropertyCollection();

        public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<TextContentChangedEventArgs> Changed
        {
            add { }
            remove { }
        }

        public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<TextContentChangingEventArgs> Changing
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler PostChanged
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

        public void ChangeContentType(IContentType newContentType, object editTag)
        {
            lock (_lock)
            {
                if (newContentType != ContentType)
                {
                    var oldContentType = ContentType;
                    var oldSnapshot = CurrentSnapshot;

                    ContentType = newContentType;
                    var nextVersion = _version.CreateNext(changes: FakeNormalizedTextChangeCollection.Empty);
                    _version = nextVersion;
                    _image = new FakeTextImage(_image.GetText(), nextVersion.ImageVersion);
                    CurrentSnapshot = new FakeTextSnapshot(this, _version, _image);
                    ContentTypeChanged?.Invoke(this, new ContentTypeChangedEventArgs(oldSnapshot, CurrentSnapshot, oldContentType, newContentType, editTag));
                }
            }
        }

        public bool CheckEditAccess()
        {
            throw new NotImplementedException();
        }

        public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag)
        {
            return new FakeTextEdit(CurrentSnapshot, options, reiteratedVersionNumber, editTag);
        }

        public ITextEdit CreateEdit()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyRegionEdit CreateReadOnlyRegionEdit()
        {
            throw new NotImplementedException();
        }

        public ITextSnapshot Delete(Span deleteSpan)
        {
            throw new NotImplementedException();
        }

        public NormalizedSpanCollection GetReadOnlyExtents(Span span)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshot Insert(int position, string text)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly(int position)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly(int position, bool isEdit)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly(Span span)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly(Span span, bool isEdit)
        {
            throw new NotImplementedException();
        }

        public ITextSnapshot Replace(Span replaceSpan, string replaceWith)
        {
            throw new NotImplementedException();
        }

        public void TakeThreadOwnership()
        {
            throw new NotImplementedException();
        }
    }
}
