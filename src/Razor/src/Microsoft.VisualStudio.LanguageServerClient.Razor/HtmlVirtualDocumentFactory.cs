// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(VirtualDocumentFactory))]
    internal class HtmlVirtualDocumentFactory : VirtualDocumentFactory
    {
        // Internal for testing
        internal const string HtmlLSPContentTypeName = "htmlyLSP";
        internal const string VirtualHtmlFileNameSuffix = "__virtual.html";

        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly ITextBufferFactoryService _textBufferFactory;
        private readonly FileUriProvider _fileUriProvider;
        private IContentType _htmlLSPContentType;

        [ImportingConstructor]
        public HtmlVirtualDocumentFactory(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService textBufferFactory,
            FileUriProvider filePathProvider)
        {
            if (contentTypeRegistry is null)
            {
                throw new ArgumentNullException(nameof(contentTypeRegistry));
            }

            if (textBufferFactory is null)
            {
                throw new ArgumentNullException(nameof(textBufferFactory));
            }

            if (filePathProvider is null)
            {
                throw new ArgumentNullException(nameof(filePathProvider));
            }

            _contentTypeRegistry = contentTypeRegistry;
            _textBufferFactory = textBufferFactory;
            _fileUriProvider = filePathProvider;
        }

        private IContentType HtmlLSPContentType
        {
            get
            {
                if (_htmlLSPContentType == null)
                {
                    _htmlLSPContentType = _contentTypeRegistry.GetContentType(HtmlLSPContentTypeName);
                }

                return _htmlLSPContentType;
            }
        }

        public override bool TryCreateFor(ITextBuffer hostDocumentBuffer, out VirtualDocument virtualDocument)
        {
            if (hostDocumentBuffer is null)
            {
                throw new ArgumentNullException(nameof(hostDocumentBuffer));
            }

            if (!hostDocumentBuffer.ContentType.IsOfType(RazorLSPContentTypeDefinition.Name))
            {
                // Another content type we don't care about.
                virtualDocument = null;
                return false;
            }

            var hostDocumentUri = _fileUriProvider.GetOrCreate(hostDocumentBuffer);

            // Index.cshtml => Index.cshtml__virtual.html
            var virtualHtmlFilePath = hostDocumentUri + VirtualHtmlFileNameSuffix;
            var virtualHtmlUri = new Uri(virtualHtmlFilePath);

            var htmlBuffer = _textBufferFactory.CreateTextBuffer(HtmlLSPContentType);
            virtualDocument = new HtmlVirtualDocument(virtualHtmlUri, htmlBuffer);
            return true;
        }
    }
}
