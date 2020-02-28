// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using LSPFormattingOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.FormattingOptions;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class HtmlFormatter
    {
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ILanguageServer _server;

        public HtmlFormatter(
            ILanguageServer languageServer,
            FilePathNormalizer filePathNormalizer)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _server = languageServer;
            _filePathNormalizer = filePathNormalizer;
        }

        public async Task<TextEdit[]> FormatAsync(
            RazorCodeDocument codeDocument,
            Range range,
            Uri uri,
            LSPFormattingOptions options)
        {
            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.Html,
                ProjectedRange = range,
                HostDocumentFilePath = _filePathNormalizer.Normalize(uri.AbsolutePath),
                Options = options
            };

            var result = await _server.Client.SendRequest<RazorDocumentRangeFormattingParams, RazorDocumentRangeFormattingResponse>(
                LanguageServerConstants.RazorRangeFormattingEndpoint, @params);

            return result.Edits;
        }
    }
}
