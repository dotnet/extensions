// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageEndpoint :
        IRazorLanguageQueryHandler,
        IRazorMapToDocumentRangesHandler,
        IRazorMapToDocumentEditsHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly RazorFormattingService _razorFormattingService;
        private readonly ILogger _logger;

        public RazorLanguageEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            DocumentVersionCache documentVersionCache,
            RazorDocumentMappingService documentMappingService,
            RazorFormattingService razorFormattingService,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (documentMappingService == null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (razorFormattingService == null)
            {
                throw new ArgumentNullException(nameof(razorFormattingService));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _documentVersionCache = documentVersionCache;
            _documentMappingService = documentMappingService;
            _razorFormattingService = razorFormattingService;
            _logger = loggerFactory.CreateLogger<RazorLanguageEndpoint>();
        }

        public async Task<RazorLanguageQueryResponse> Handle(RazorLanguageQueryParams request, CancellationToken cancellationToken)
        {
            int? documentVersion = null;
            DocumentSnapshot documentSnapshot = null;
            await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.Uri.GetAbsoluteOrUNCPath(), out documentSnapshot);

                Debug.Assert(documentSnapshot != null, "Failed to get the document snapshot, could not map to document ranges.");

                if (!_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out documentVersion))
                {
                    // This typically happens for closed documents.
                    documentVersion = null;
                }

                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            var sourceText = await documentSnapshot.GetTextAsync();
            var linePosition = new LinePosition(request.Position.Line, request.Position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var responsePosition = request.Position;

            if (codeDocument.IsUnsupported())
            {
                // All language queries on unsupported documents return Html. This is equivalent to what pre-VSCode Razor was capable of.
                return new RazorLanguageQueryResponse()
                {
                    Kind = RazorLanguageKind.Html,
                    Position = responsePosition,
                    PositionIndex = hostDocumentIndex,
                    HostDocumentVersion = documentVersion,
                };
            }

            var responsePositionIndex = hostDocumentIndex;

            var languageKind = _documentMappingService.GetLanguageKind(codeDocument, hostDocumentIndex);
            if (languageKind == RazorLanguageKind.CSharp)
            {
                if (_documentMappingService.TryMapToProjectedDocumentPosition(codeDocument, hostDocumentIndex, out var projectedPosition, out var projectedIndex))
                {
                    // For C# locations, we attempt to return the corresponding position
                    // within the projected document
                    responsePosition = projectedPosition;
                    responsePositionIndex = projectedIndex;
                }
                else
                {
                    // It no longer makes sense to think of this location as C#, since it doesn't
                    // correspond to any position in the projected document. This should not happen
                    // since there should be source mappings for all the C# spans.
                    languageKind = RazorLanguageKind.Razor;
                    responsePositionIndex = hostDocumentIndex;
                }
            }

            _logger.LogTrace($"Language query request for ({request.Position.Line}, {request.Position.Character}) = {languageKind} at ({responsePosition.Line}, {responsePosition.Character})");

            return new RazorLanguageQueryResponse()
            {
                Kind = languageKind,
                Position = responsePosition,
                PositionIndex = responsePositionIndex,
                HostDocumentVersion = documentVersion
            };
        }

        public async Task<RazorMapToDocumentRangesResponse> Handle(RazorMapToDocumentRangesParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            int? documentVersion = null;
            DocumentSnapshot documentSnapshot = null;
            await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.RazorDocumentUri.GetAbsoluteOrUNCPath(), out documentSnapshot);

                Debug.Assert(documentSnapshot != null, "Failed to get the document snapshot, could not map to document ranges.");

                if (documentSnapshot is null ||
                    !_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out documentVersion))
                {
                    documentVersion = null;
                }
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            if (request.Kind != RazorLanguageKind.CSharp)
            {
                // All other non-C# requests map directly to where they are in the document.
                return new RazorMapToDocumentRangesResponse()
                {
                    Ranges = request.ProjectedRanges,
                    HostDocumentVersion = documentVersion,
                };
            }

            if (documentSnapshot is null)
            {
                return null;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            var ranges = new Range[request.ProjectedRanges.Length];
            for (var i = 0; i < request.ProjectedRanges.Length; i++)
            {
                var projectedRange = request.ProjectedRanges[i];
                if (codeDocument.IsUnsupported() ||
                    !_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, projectedRange, request.MappingBehavior, out var originalRange))
                {
                    // All language queries on unsupported documents return Html. This is equivalent to what pre-VSCode Razor was capable of.
                    ranges[i] = RangeExtensions.UndefinedRange;
                    continue;
                }

                ranges[i] = originalRange;
            }

            return new RazorMapToDocumentRangesResponse()
            {
                Ranges = ranges,
                HostDocumentVersion = documentVersion,
            };
        }

        public async Task<RazorMapToDocumentEditsResponse> Handle(RazorMapToDocumentEditsParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            int? documentVersion = null;
            DocumentSnapshot documentSnapshot = null;
            await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(request.RazorDocumentUri.GetAbsoluteOrUNCPath(), out documentSnapshot);

                Debug.Assert(documentSnapshot != null, "Failed to get the document snapshot, could not map to document ranges.");

                if (!_documentVersionCache.TryGetDocumentVersion(documentSnapshot, out documentVersion))
                {
                    documentVersion = null;
                }
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return new RazorMapToDocumentEditsResponse()
                {
                    TextEdits = Array.Empty<TextEdit>(),
                    HostDocumentVersion = documentVersion
                };
            }

            if (request.TextEditKind == TextEditKind.FormatOnType)
            {
                var mappedEdits = await _razorFormattingService.ApplyFormattedEditsAsync(
                    request.RazorDocumentUri, documentSnapshot, request.Kind, request.ProjectedTextEdits, request.FormattingOptions, cancellationToken);

                return new RazorMapToDocumentEditsResponse()
                {
                    TextEdits = mappedEdits,
                    HostDocumentVersion = documentVersion,
                };
            }
            else if (request.TextEditKind == TextEditKind.Snippet)
            {
                var mappedEdits = await _razorFormattingService.ApplyFormattedEditsAsync(
                    request.RazorDocumentUri,
                    documentSnapshot,
                    request.Kind,
                    request.ProjectedTextEdits,
                    request.FormattingOptions,
                    cancellationToken,
                    bypassValidationPasses: true,
                    collapseEdits: true);

                return new RazorMapToDocumentEditsResponse()
                {
                    TextEdits = mappedEdits,
                    HostDocumentVersion = documentVersion,
                };
            }

            if (request.Kind != RazorLanguageKind.CSharp)
            {
                // All other non-C# requests map directly to where they are in the document.
                return new RazorMapToDocumentEditsResponse()
                {
                    TextEdits = request.ProjectedTextEdits,
                    HostDocumentVersion = documentVersion,
                };
            }

            var edits = new List<TextEdit>();
            for (var i = 0; i < request.ProjectedTextEdits.Length; i++)
            {
                var projectedRange = request.ProjectedTextEdits[i].Range;
                if (!_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, projectedRange, out var originalRange))
                {
                    // Can't map range. Discard this edit.
                    continue;
                }

                var edit = new TextEdit()
                {
                    Range = originalRange,
                    NewText = request.ProjectedTextEdits[i].NewText
                };

                edits.Add(edit);
            }

            return new RazorMapToDocumentEditsResponse()
            {
                TextEdits = edits.ToArray(),
                HostDocumentVersion = documentVersion,
            };
        }
    }
}
