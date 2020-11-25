// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormatter
    {
        private const string MarkerId = "RazorMarker";

        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ClientNotifierServiceBase _server;

        public CSharpFormatter(
            RazorDocumentMappingService documentMappingService,
            ClientNotifierServiceBase languageServer,
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
            FormattingContext context,
            Range rangeToFormat,
            CancellationToken cancellationToken,
            bool formatOnClient = false)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (rangeToFormat is null)
            {
                throw new ArgumentNullException(nameof(rangeToFormat));
            }

            Range projectedRange = null;
            if (rangeToFormat != null && !_documentMappingService.TryMapToProjectedDocumentRange(context.CodeDocument, rangeToFormat, out projectedRange))
            {
                return Array.Empty<TextEdit>();
            }

            TextEdit[] edits;
            if (formatOnClient)
            {
                edits = await FormatOnClientAsync(context, projectedRange, cancellationToken);
            }
            else
            {
                edits = await FormatOnServerAsync(context, projectedRange, cancellationToken);
            }

            var mappedEdits = MapEditsToHostDocument(context.CodeDocument, edits);
            return mappedEdits;
        }

        public async Task<Dictionary<int, int>> GetCSharpIndentationAsync(FormattingContext context, IEnumerable<int> projectedDocumentLocations, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (projectedDocumentLocations is null)
            {
                throw new ArgumentNullException(nameof(projectedDocumentLocations));
            }

            // Sorting ensures we count the marker offsets correctly.
            projectedDocumentLocations = projectedDocumentLocations.OrderBy(l => l);

            var indentations = await GetCSharpIndentationCoreAsync(context, projectedDocumentLocations, cancellationToken);
            return indentations;
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
            FormattingContext context,
            Range projectedRange,
            CancellationToken cancellationToken)
        {
            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = projectedRange,
                HostDocumentFilePath = _filePathNormalizer.Normalize(context.Uri.GetAbsoluteOrUNCPath()),
                Options = context.Options
            };

            var response = await _server.SendRequestAsync(LanguageServerConstants.RazorRangeFormattingEndpoint, @params);
            var result = await response.Returning<RazorDocumentRangeFormattingResponse>(cancellationToken);

            return result?.Edits ?? Array.Empty<TextEdit>();
        }

        private async Task<TextEdit[]> FormatOnServerAsync(
            FormattingContext context,
            Range projectedRange,
            CancellationToken cancellationToken)
        {
            var csharpSourceText = context.CodeDocument.GetCSharpSourceText();
            var spanToFormat = projectedRange.AsTextSpan(csharpSourceText);
            var root = await context.CSharpWorkspaceDocument.GetSyntaxRootAsync(cancellationToken);
            var workspace = context.CSharpWorkspace;

            // Formatting options will already be set in the workspace.
            var changes = CodeAnalysis.Formatting.Formatter.GetFormattedTextChanges(root, spanToFormat, workspace);

            var edits = changes.Select(c => c.AsTextEdit(csharpSourceText)).ToArray();
            return edits;
        }

        private async Task<Dictionary<int, int>> GetCSharpIndentationCoreAsync(FormattingContext context, IEnumerable<int> projectedDocumentLocations, CancellationToken cancellationToken)
        {
            var (indentationMap, syntaxTree) = InitializeIndentationData(context, projectedDocumentLocations, cancellationToken);

            var root = await syntaxTree.GetRootAsync(cancellationToken);

            root = AttachAnnotations(indentationMap, projectedDocumentLocations, root);

            // At this point, we have added all the necessary markers and attached annotations.
            // Let's invoke the C# formatter and hope for the best.
            var formattedRoot = CodeAnalysis.Formatting.Formatter.Format(root, context.CSharpWorkspace);
            var formattedText = formattedRoot.GetText();

            var desiredIndentationMap = new Dictionary<int, int>();

            // Assuming the C# formatter did the right thing, let's extract the indentation offset from
            // the line containing trivia and token that has our attached annotations.
            ExtractTriviaAnnotations();
            ExtractTokenAnnotations();

            return desiredIndentationMap;

            void ExtractTriviaAnnotations()
            {
                var formattedTriviaList = formattedRoot.GetAnnotatedTrivia(MarkerId);
                foreach (var trivia in formattedTriviaList)
                {
                    // We only expect one annotation because we built the entire trivia with a single annotation.
                    var annotation = trivia.GetAnnotations(MarkerId).Single();
                    if (!int.TryParse(annotation.Data, out var projectedIndex))
                    {
                        continue;
                    }

                    var line = formattedText.Lines.GetLineFromPosition(trivia.SpanStart);
                    var offset = GetIndentationOffsetFromLine(context, line);

                    desiredIndentationMap[projectedIndex] = offset;
                }
            }

            void ExtractTokenAnnotations()
            {
                var formattedTokenList = formattedRoot.GetAnnotatedTokens(MarkerId);
                foreach (var token in formattedTokenList)
                {
                    // There could be multiple annotations per token because a token can span multiple lines.
                    // E.g, a multiline string literal.
                    var annotations = token.GetAnnotations(MarkerId);
                    foreach (var annotation in annotations)
                    {
                        if (!int.TryParse(annotation.Data, out var projectedIndex))
                        {
                            continue;
                        }

                        var indentationMapData = indentationMap[projectedIndex];
                        var line = formattedText.Lines.GetLineFromPosition(token.SpanStart + indentationMapData.CharacterOffset);
                        var offset = GetIndentationOffsetFromLine(context, line);

                        desiredIndentationMap[projectedIndex] = offset;
                    }
                }
            }
        }

        private (Dictionary<int, IndentationMapData>, SyntaxTree) InitializeIndentationData(
            FormattingContext context,
            IEnumerable<int> projectedDocumentLocations,
            CancellationToken cancellationToken)
        {
            // The approach we're taking here is to add markers only when absolutely necessary.
            // We'll attach annotations to tokens directly when possible.

            var indentationMap = new Dictionary<int, IndentationMapData>();
            var marker = "/*__marker__*/";
            var markerString = $"{context.NewLineString}{marker}{context.NewLineString}";
            var changes = new List<TextChange>();

            var previousMarkerOffset = 0;
            foreach (var projectedDocumentIndex in projectedDocumentLocations)
            {
                var useMarker = char.IsWhiteSpace(context.CSharpSourceText[projectedDocumentIndex]);
                if (useMarker)
                {
                    // We want to add a marker here because the location points to a whitespace
                    // which will not get preserved during formatting.

                    // position points to the start of the /*__marker__*/ comment.
                    var position = projectedDocumentIndex + context.NewLineString.Length;
                    var change = new TextChange(new TextSpan(projectedDocumentIndex, 0), markerString);
                    changes.Add(change);

                    indentationMap.Add(projectedDocumentIndex, new IndentationMapData()
                    {
                        OriginalProjectedDocumentIndex = projectedDocumentIndex,
                        AnnotationAttachIndex = position + previousMarkerOffset,
                        MarkerKind = MarkerKind.Trivia,
                    });

                    // We have added a marker. This means we need to account for the length of the marker in future calculations.
                    previousMarkerOffset += markerString.Length;
                }
                else
                {
                    // No marker needed. Let's attach the annotation directly at the given location.
                    indentationMap.Add(projectedDocumentIndex, new IndentationMapData()
                    {
                        OriginalProjectedDocumentIndex = projectedDocumentIndex,
                        AnnotationAttachIndex = projectedDocumentIndex + previousMarkerOffset,
                        MarkerKind = MarkerKind.Token,
                    });
                }
            }

            var changedText = context.CSharpSourceText.WithChanges(changes);
            var syntaxTree = CSharpSyntaxTree.ParseText(changedText, cancellationToken: cancellationToken);
            return (indentationMap, syntaxTree);
        }

        private SyntaxNode AttachAnnotations(
            Dictionary<int, IndentationMapData> indentationMap,
            IEnumerable<int> projectedDocumentLocations,
            SyntaxNode root)
        {
            foreach (var projectedDocumentIndex in projectedDocumentLocations)
            {
                var indentationMapData = indentationMap[projectedDocumentIndex];
                var annotation = new SyntaxAnnotation(MarkerId, $"{projectedDocumentIndex}");

                if (indentationMapData.MarkerKind == MarkerKind.Trivia)
                {
                    var trackingTrivia = root.FindTrivia(indentationMapData.AnnotationAttachIndex, findInsideTrivia: true);
                    var annotatedTrivia = trackingTrivia.WithAdditionalAnnotations(annotation);
                    root = root.ReplaceTrivia(trackingTrivia, annotatedTrivia);
                }
                else
                {
                    var trackingToken = root.FindToken(indentationMapData.AnnotationAttachIndex, findInsideTrivia: true);
                    var annotatedToken = trackingToken.WithAdditionalAnnotations(annotation);
                    root = root.ReplaceToken(trackingToken, annotatedToken);

                    // Since a token can span multiple lines, we need to keep track of the offset within the token span.
                    // We will use this later when determining the exact line within a token.
                    indentationMapData.CharacterOffset = indentationMapData.AnnotationAttachIndex - trackingToken.SpanStart;
                }
            }

            return root;
        }

        private int GetIndentationOffsetFromLine(FormattingContext context, TextLine line)
        {
            var offset = line.GetFirstNonWhitespaceOffset() ?? 0;
            if (!context.Options.InsertSpaces)
            {
                // Normalize to spaces because the rest of the formatting pipeline operates based on the assumption.
                offset *= (int)context.Options.TabSize;
            }

            return offset;
        }

        private class IndentationMapData
        {
            public int OriginalProjectedDocumentIndex { get; set; }

            public int AnnotationAttachIndex { get; set; }

            public int CharacterOffset { get; set; }

            public MarkerKind MarkerKind { get; set; }

            public override string ToString()
            {
                return $"Original: {OriginalProjectedDocumentIndex}, MarkerAdjusted: {AnnotationAttachIndex}, Kind: {MarkerKind}, TokenOffset: {CharacterOffset}";
            }
        }

        private enum MarkerKind
        {
            Trivia,
            Token
        }
    }
}
