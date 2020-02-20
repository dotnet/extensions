// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormatter
    {
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ILanguageServer _server;

        public CSharpFormatter(
            RazorDocumentMappingService documentMappingService,
            ILanguageServer languageServer,
            FilePathNormalizer filePathNormalizer)
        {
            if (documentMappingService is null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _documentMappingService = documentMappingService;
            _server = languageServer;
            _filePathNormalizer = filePathNormalizer;
        }

        public async Task<TextEdit[]> FormatAsync(
            RazorCodeDocument codeDocument,
            Range range,
            Uri uri,
            FormattingOptions options)
        {
            if (!_documentMappingService.TryMapToProjectedDocumentRange(codeDocument, range, out var projectedRange))
            {
                return Array.Empty<TextEdit>();
            }

            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = projectedRange,
                HostDocumentFilePath = _filePathNormalizer.Normalize(uri.AbsolutePath),
                Options = options
            };

            var result = await _server.Client.SendRequest<RazorDocumentRangeFormattingParams, RazorDocumentRangeFormattingResponse>(
                LanguageServerConstants.RazorRangeFormattingEndpoint, @params);

            var mappedEdits = MapEditsToHostDocument(codeDocument, result.Edits);

            return mappedEdits;
        }

        private TextEdit[] MapEditsToHostDocument(RazorCodeDocument codeDocument, TextEdit[] csharpEdits)
        {
            var actualEdits = new List<TextEdit>();
            foreach (var edit in csharpEdits)
            {
                if (_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, edit.Range, out var newRange))
                {
                    actualEdits.Add(new TextEdit()
                    {
                        NewText = edit.NewText,
                        Range = newRange,
                    });
                }
            }

            return actualEdits.ToArray();
        }
    }
}
