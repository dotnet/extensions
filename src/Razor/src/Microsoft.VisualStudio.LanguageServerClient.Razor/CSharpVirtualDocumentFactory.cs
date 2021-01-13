// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(VirtualDocumentFactory))]
    internal class CSharpVirtualDocumentFactory : VirtualDocumentFactoryBase
    {
        private static readonly IReadOnlyDictionary<object, object> _languageBufferProperties = new Dictionary<object, object>
        {
            { LanguageClientConstants.ClientNamePropertyKey, "RazorCSharp" }
        };

        private static IContentType _csharpContentType;

        [ImportingConstructor]
        public CSharpVirtualDocumentFactory(
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService textBufferFactory,
            ITextDocumentFactoryService textDocumentFactory,
            FileUriProvider fileUriProvider)
            : base(contentTypeRegistry, textBufferFactory, textDocumentFactory, fileUriProvider)
        {
        }

        protected override IContentType LanguageContentType
        {
            get
            {
                if (_csharpContentType == null)
                {
                    var contentType = ContentTypeRegistry.GetContentType(RazorLSPConstants.CSharpContentTypeName);
                    _csharpContentType = new RemoteContentDefinitionType(contentType);
                }

                return _csharpContentType;
            }
        }

        protected override string HostDocumentContentTypeName => RazorLSPConstants.RazorLSPContentTypeName;
        protected override string LanguageFileNameSuffix => RazorLSPConstants.VirtualCSharpFileNameSuffix;
        protected override IReadOnlyDictionary<object, object> LanguageBufferProperties => _languageBufferProperties;
        protected override VirtualDocument CreateVirtualDocument(Uri uri, ITextBuffer textBuffer) => new CSharpVirtualDocument(uri, textBuffer);

        private class RemoteContentDefinitionType : IContentType
        {
            private static readonly IReadOnlyList<string> ExtendedBaseContentTypes = new[]
            {
                "code-languageserver-base",
                CodeRemoteContentDefinition.CodeRemoteContentTypeName
            };

            private readonly IContentType _innerContentType;

            internal RemoteContentDefinitionType(IContentType innerContentType)
            {
                if (innerContentType is null)
                {
                    throw new ArgumentNullException(nameof(innerContentType));
                }

                _innerContentType = innerContentType;
                TypeName = innerContentType.TypeName;
                DisplayName = innerContentType.DisplayName;
            }

            public string TypeName { get; }

            public string DisplayName { get; }

            public IEnumerable<IContentType> BaseTypes => _innerContentType.BaseTypes;

            public bool IsOfType(string type)
            {
                return ExtendedBaseContentTypes.Contains(type) || _innerContentType.IsOfType(type);
            }
        }
    }
}
