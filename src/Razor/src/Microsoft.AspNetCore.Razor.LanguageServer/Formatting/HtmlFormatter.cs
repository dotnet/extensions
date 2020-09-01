// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using LSPFormattingOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.FormattingOptions;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class HtmlFormatter
    {
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IClientLanguageServer _server;

        public HtmlFormatter(
            IClientLanguageServer languageServer,
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
            DocumentUri uri,
            LSPFormattingOptions options,
            CancellationToken cancellationToken)
        {
            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.Html,
                ProjectedRange = range,
                HostDocumentFilePath = _filePathNormalizer.Normalize(uri.GetAbsoluteOrUNCPath()),
                Options = options
            };

            var response = _server.SendRequest(LanguageServerConstants.RazorRangeFormattingEndpoint, @params);
            var result = await response.Returning<RazorDocumentRangeFormattingResponse>(cancellationToken);

            return result.Edits;
        }
    }
}
