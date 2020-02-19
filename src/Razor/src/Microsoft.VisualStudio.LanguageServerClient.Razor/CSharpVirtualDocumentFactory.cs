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
    internal class CSharpVirtualDocumentFactory : VirtualDocumentFactory
    {
        // Internal for testing
        internal const string CSharpLSPContentTypeName = "C#_LSP";
        internal const string VirtualCSharpFileNameSuffix = "__virtual.cs";

        private readonly IContentTypeRegistryService _contentTypeRegistry;
        private readonly ITextBufferFactoryService _textBufferFactory;
        private readonly FileUriProvider _fileUriProvider;
        private IContentType _csharpLSPContentType;

        [ImportingConstructor]
        public CSharpVirtualDocumentFactory(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService textBufferFactory,
            FileUriProvider fileUriProvider)
        {
            if (contentTypeRegistry is null)
            {
                throw new ArgumentNullException(nameof(contentTypeRegistry));
            }

            if (textBufferFactory is null)
            {
                throw new ArgumentNullException(nameof(textBufferFactory));
            }

            if (fileUriProvider is null)
            {
                throw new ArgumentNullException(nameof(fileUriProvider));
            }

            _contentTypeRegistry = contentTypeRegistry;
            _textBufferFactory = textBufferFactory;
            _fileUriProvider = fileUriProvider;
        }

        private IContentType CSharpLSPContentType
        {
            get
            {
                if (_csharpLSPContentType == null)
                {
                    _csharpLSPContentType = _contentTypeRegistry.GetContentType(CSharpLSPContentTypeName);
                }

                return _csharpLSPContentType;
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

            // Index.cshtml => Index.cshtml__virtual.cs
            var virtualCSharpFilePath = hostDocumentUri.AbsolutePath + VirtualCSharpFileNameSuffix;
            var virtualCSharpUri = new Uri(virtualCSharpFilePath);

            var csharpBuffer = _textBufferFactory.CreateTextBuffer(CSharpLSPContentType);
            virtualDocument = new CSharpVirtualDocument(virtualCSharpUri, csharpBuffer);
            return true;
        }
    }
}
