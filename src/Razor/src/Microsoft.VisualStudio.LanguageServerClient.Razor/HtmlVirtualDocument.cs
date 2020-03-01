// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal class HtmlVirtualDocument : VirtualDocument
    {
        private readonly ITextBuffer _textBuffer;

        public HtmlVirtualDocument(Uri uri, ITextBuffer textBuffer)
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
            _textBuffer = textBuffer;
        }

        public override Uri Uri { get; }

        public override long? HostDocumentSyncVersion => throw new NotImplementedException();
    }
}
