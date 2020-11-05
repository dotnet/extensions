// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class DefaultRazorFormattingService : RazorFormattingService
    {
        private readonly List<IFormattingPass> _formattingPasses;
        private readonly ILogger _logger;

        public DefaultRazorFormattingService(IEnumerable<IFormattingPass> formattingPasses, ILoggerFactory loggerFactory)
        {
            if (formattingPasses is null)
            {
                throw new ArgumentNullException(nameof(formattingPasses));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _formattingPasses = formattingPasses.OrderBy(f => f.Order).ToList();
            _logger = loggerFactory.CreateLogger<DefaultRazorFormattingService>();
        }

        public override async Task<TextEdit[]> FormatAsync(
            DocumentUri uri,
            DocumentSnapshot documentSnapshot,
            Range range,
            FormattingOptions options,
            CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            using var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options, range);

            var result = new FormattingResult(Array.Empty<TextEdit>());
            foreach (var pass in _formattingPasses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = await pass.ExecuteAsync(context, result, cancellationToken);
            }

            var filteredEdits = result.Edits.Where(e => range.LineOverlapsWith(e.Range)).ToArray();
            return filteredEdits;
        }

        public override async Task<TextEdit[]> ApplyFormattedEditsAsync(
            DocumentUri uri,
            DocumentSnapshot documentSnapshot,
            RazorLanguageKind kind,
            TextEdit[] formattedEdits,
            FormattingOptions options,
            CancellationToken cancellationToken,
            bool bypassValidationPasses = false)
        {
            if (kind == RazorLanguageKind.Html)
            {
                // We don't support formatting HTML edits yet.
                return formattedEdits;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            using var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options, isFormatOnType: true);
            var result = new FormattingResult(formattedEdits, kind);

            foreach (var pass in _formattingPasses)
            {
                if (pass.IsValidationPass && bypassValidationPasses)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                result = await pass.ExecuteAsync(context, result, cancellationToken);
            }

            return result.Edits;
        }
    }
}
