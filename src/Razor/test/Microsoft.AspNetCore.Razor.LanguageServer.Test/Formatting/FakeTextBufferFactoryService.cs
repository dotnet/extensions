// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeTextBufferFactoryService : ITextBufferFactoryService3, ITextImageFactoryService
    {
        private readonly IContentTypeRegistryService _contentTypeRegistryService;

        public FakeTextBufferFactoryService(IContentTypeRegistryService contentTypeRegistryService)
        {
            _contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));
        }

        public IContentType TextContentType => _contentTypeRegistryService.GetContentType(StandardContentTypeNames.Text);

        public IContentType PlaintextContentType => _contentTypeRegistryService.GetContentType("plaintext");

        public IContentType InertContentType => _contentTypeRegistryService.GetContentType(StandardContentTypeNames.Inert);

        public event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;

        public ITextBuffer CreateTextBuffer(SnapshotSpan span, IContentType contentType)
        {
            return CreateTextBuffer(span.GetText(), contentType);
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId)
        {
            return CreateTextBuffer(CreateTextImage(reader, length), contentType);
        }

        public ITextBuffer CreateTextBuffer(ITextImage image, IContentType contentType)
        {
            var result = new FakeTextBuffer((FakeTextImage)image, contentType);
            TextBufferCreated?.Invoke(this, new TextBufferCreatedEventArgs(result));
            return result;
        }

        public ITextBuffer CreateTextBuffer()
        {
            return CreateTextBuffer("", TextContentType);
        }

        public ITextBuffer CreateTextBuffer(IContentType contentType)
        {
            return CreateTextBuffer("", contentType);
        }

        public ITextBuffer CreateTextBuffer(string text, IContentType contentType)
        {
            return CreateTextBuffer(CreateTextImage(text), contentType);
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType)
        {
            return CreateTextBuffer(reader, contentType, length: -1, traceId: "legacy");
        }

        public ITextImage CreateTextImage(string text)
        {
            return new FakeTextImage(text, new FakeTextImageVersion(text.Length));
        }

        public ITextImage CreateTextImage(TextReader reader, long length)
        {
            throw new NotImplementedException();
        }
    }
}
