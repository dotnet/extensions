// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormatter
    {
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IClientLanguageServer _server;
        private readonly ProjectSnapshotManagerAccessor _projectSnapshotManagerAccessor;

        public CSharpFormatter(
            RazorDocumentMappingService documentMappingService,
            IClientLanguageServer languageServer,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
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

            if (projectSnapshotManagerAccessor is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _documentMappingService = documentMappingService;
            _server = languageServer;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _filePathNormalizer = filePathNormalizer;
        }

        public async Task<TextEdit[]> FormatAsync(
            RazorCodeDocument codeDocument,
            Range range,
            DocumentUri uri,
            FormattingOptions options,
            CancellationToken cancellationToken,
            bool formatOnClient = false)
        {
            Range projectedRange = null;
            if (range != null && !_documentMappingService.TryMapToProjectedDocumentRange(codeDocument, range, out projectedRange))
            {
                return Array.Empty<TextEdit>();
            }

            TextEdit[] edits;
            if (formatOnClient)
            {
                edits = await FormatOnClientAsync(codeDocument, projectedRange, uri, options, cancellationToken);
            }
            else
            {
                edits = await FormatOnServerAsync(codeDocument, projectedRange, uri, options, cancellationToken);
            }

            var mappedEdits = MapEditsToHostDocument(codeDocument, edits);
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

        private async Task<TextEdit[]> FormatOnClientAsync(
            RazorCodeDocument codeDocument,
            Range projectedRange,
            DocumentUri uri,
            FormattingOptions options,
            CancellationToken cancellationToken)
        {
            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = projectedRange,
                HostDocumentFilePath = _filePathNormalizer.Normalize(uri.GetAbsoluteOrUNCPath()),
                Options = options
            };

            var response = _server.SendRequest(LanguageServerConstants.RazorRangeFormattingEndpoint, @params);
            var result = await response.Returning<RazorDocumentRangeFormattingResponse>(cancellationToken);

            return result.Edits;
        }

        private async Task<TextEdit[]> FormatOnServerAsync(
            RazorCodeDocument codeDocument,
            Range projectedRange,
            DocumentUri uri,
            FormattingOptions options,
            CancellationToken cancellationToken)
        {
            var workspace = _projectSnapshotManagerAccessor.Instance.Workspace;
            var csharpOptions = workspace.Options
                .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.TabSize, LanguageNames.CSharp, (int)options.TabSize)
                .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, !options.InsertSpaces);

            var csharpDocument = codeDocument.GetCSharpDocument();
            var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode);
            var sourceText = SourceText.From(csharpDocument.GeneratedCode);
            var root = await syntaxTree.GetRootAsync();
            var spanToFormat = projectedRange.AsTextSpan(sourceText);

            var changes = CodeAnalysis.Formatting.Formatter.GetFormattedTextChanges(root, spanToFormat, workspace, options: csharpOptions);

            var edits = changes.Select(c => c.AsTextEdit(sourceText)).ToArray();
            return edits;
        }
    }
}
