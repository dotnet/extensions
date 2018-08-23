// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultRemoteTextLoaderFactory : RemoteTextLoaderFactory
    {
        private const string GetTextDocumentMethod = "getTextDocument";
        private readonly ILanguageServer _router;
        private readonly FilePathNormalizer _filePathNormalizer;

        public DefaultRemoteTextLoaderFactory(
            ILanguageServer router,
            FilePathNormalizer filePathNormalizer)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _router = router;
            _filePathNormalizer = filePathNormalizer;
        }

        public override TextLoader Create(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            return new RemoteTextLoader(normalizedPath, _router);
        }

        private class RemoteTextLoader : TextLoader
        {
            private readonly string _filePath;
            private readonly ILanguageServer _router;

            public RemoteTextLoader(string filePath, ILanguageServer router)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (router == null)
                {
                    throw new ArgumentNullException(nameof(router));
                }

                _filePath = filePath;
                _router = router;
            }

            public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var document = await _router.Client.SendRequest<string, TextDocumentItem>(GetTextDocumentMethod, _filePath);
                var sourceText = SourceText.From(document.Text);
                var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
                return textAndVersion;
            }
        }
    }
}
