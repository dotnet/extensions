// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(VirtualDocumentFactory))]
    internal class HtmlVirtualDocumentFactory : VirtualDocumentFactoryBase
    {
        private static IContentType _htmlLSPContentType;

        [ImportingConstructor]
        public HtmlVirtualDocumentFactory(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService textBufferFactory,
            ITextDocumentFactoryService textDocumentFactory,
            FileUriProvider filePathProvider)
            : base(contentTypeRegistry, textBufferFactory, textDocumentFactory, filePathProvider)
        {
        }

        protected override IContentType LanguageContentType
        {
            get
            {
                if (_htmlLSPContentType == null)
                {
                    _htmlLSPContentType = ContentTypeRegistry.GetContentType(RazorLSPConstants.HtmlLSPContentTypeName);
                }

                return _htmlLSPContentType;
            }
        }

        protected override string HostDocumentContentTypeName => RazorLSPConstants.RazorLSPContentTypeName;
        protected override string LanguageFileNameSuffix => RazorLSPConstants.VirtualHtmlFileNameSuffix;
        protected override VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer) => new HtmlVirtualDocument(uri, textBuffer);
    }
}
