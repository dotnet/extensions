// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RazorDiagnosticFactory = Microsoft.AspNetCore.Razor.Language.RazorDiagnosticFactory;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorDiagnosticsEndpoint :
        IRazorDiagnosticsHandler
    {
        // Internal for testing
        internal static readonly IReadOnlyCollection<string> CSharpDiagnosticsToIgnore = new HashSet<string>()
        {
            "RemoveUnnecessaryImportsFixable",
            "IDE0005_gen", // Using directive is unnecessary
        };

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly ILogger _logger;

        public RazorDiagnosticsEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            DocumentVersionCache documentVersionCache,
            RazorDocumentMappingService documentMappingService,
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

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _documentVersionCache = documentVersionCache;
            _documentMappingService = documentMappingService;
            _logger = loggerFactory.CreateLogger<RazorDiagnosticsEndpoint>();
        }

        public async Task<RazorDiagnosticsResponse> Handle(RazorDiagnosticsParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _logger.LogInformation($"Received {request.Kind:G} diagnostic request for {request.RazorDocumentUri} with {request.Diagnostics.Length} diagnostics.");

            cancellationToken.ThrowIfCancellationRequested();

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
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);

            if (documentSnapshot is null)
            {
                _logger.LogInformation($"Failed to find document {request.RazorDocumentUri}.");

                return new RazorDiagnosticsResponse()
                {
                    Diagnostics = null,
                    HostDocumentVersion = null
                };
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument?.IsUnsupported() != false)
            {
                _logger.LogInformation("Unsupported code document.");
                return new RazorDiagnosticsResponse()
                {
                    Diagnostics = Array.Empty<Diagnostic>(),
                    HostDocumentVersion = documentVersion
                };
            }

            var unmappedDiagnostics = request.Diagnostics;
            var filteredDiagnostics = request.Kind == RazorLanguageKind.CSharp ?
                FilterCSharpDiagnostics(unmappedDiagnostics, codeDocument) :
                await FilterHTMLDiagnosticsAsync(unmappedDiagnostics, codeDocument, documentSnapshot).ConfigureAwait(false);
            if (!filteredDiagnostics.Any())
            {
                _logger.LogInformation("No diagnostics remaining after filtering.");

                return new RazorDiagnosticsResponse()
                {
                    Diagnostics = Array.Empty<Diagnostic>(),
                    HostDocumentVersion = documentVersion
                };
            }

            _logger.LogInformation($"{filteredDiagnostics.Length}/{unmappedDiagnostics.Length} diagnostics remain after filtering.");

            var mappedDiagnostics = MapDiagnostics(
                request.Kind,
                filteredDiagnostics,
                codeDocument);

            _logger.LogInformation($"Returning {mappedDiagnostics.Length} mapped diagnostics.");

            return new RazorDiagnosticsResponse()
            {
                Diagnostics = mappedDiagnostics,
                HostDocumentVersion = documentVersion,
            };
        }

        private static async Task<Diagnostic[]> FilterHTMLDiagnosticsAsync(
            Diagnostic[] unmappedDiagnostics,
            RazorCodeDocument codeDocument,
            DocumentSnapshot documentSnapshot)
        {
            var sourceText = await documentSnapshot.GetTextAsync();
            var syntaxTree = codeDocument.GetSyntaxTree();

            var processedAttributes = new Dictionary<TextSpan, bool>();

            var filteredDiagnostics = unmappedDiagnostics
                .Where(d =>
                    !InAttributeContainingCSharp(d, sourceText, syntaxTree, processedAttributes) &&
                    !ShouldFilterHtmlDiagnosticBasedOnErrorCode(d, sourceText, syntaxTree))
                .ToArray();

            return filteredDiagnostics;
        }

        private static bool ShouldFilterHtmlDiagnosticBasedOnErrorCode(Diagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            if (!diagnostic.Code.HasValue)
            {
                return false;
            }

            return diagnostic.Code.Value.String switch
            {
                HtmlErrorCodes.InvalidNestingErrorCode => IsInvalidNestingWarningWithinComponent(diagnostic, sourceText, syntaxTree),
                HtmlErrorCodes.MissingEndTagErrorCode => FileKinds.IsComponent(syntaxTree.Options.FileKind), // Redundant with RZ9980 in Components
                _ => false,
            };

            static bool IsInvalidNestingWarningWithinComponent(Diagnostic d, SourceText sourceText, RazorSyntaxTree syntaxTree)
            {
                var owner = syntaxTree.GetOwner(sourceText, d.Range.Start);

                var taghelperNode = owner.FirstAncestorOrSelf<MarkupSyntaxNode>(n =>
                    n is MarkupTagHelperElementSyntax);

                return !(taghelperNode is null);
            }
        }

        private static bool InAttributeContainingCSharp(
                Diagnostic d,
                SourceText sourceText,
                RazorSyntaxTree syntaxTree,
                Dictionary<TextSpan, bool> processedAttributes)
        {
            // Examine the _end_ of the diagnostic to see if we're at the
            // start of an (im/ex)plicit expression. Looking at the start
            // of the diagnostic isn't sufficient.
            var owner = syntaxTree.GetOwner(sourceText, d.Range.End);

            var markupAttributeNode = owner.FirstAncestorOrSelf<RazorSyntaxNode>(n =>
                n is MarkupAttributeBlockSyntax ||
                n is MarkupTagHelperAttributeSyntax ||
                n is MarkupMiscAttributeContentSyntax);

            if (markupAttributeNode != null)
            {
                if (!processedAttributes.TryGetValue(markupAttributeNode.FullSpan, out var doesAttributeContainNonMarkup))
                {
                    doesAttributeContainNonMarkup = CheckIfAttributeContainsNonMarkupNodes(markupAttributeNode);
                    processedAttributes.Add(markupAttributeNode.FullSpan, doesAttributeContainNonMarkup);
                }

                return doesAttributeContainNonMarkup;
            }

            return false;

            static bool CheckIfAttributeContainsNonMarkupNodes(RazorSyntaxNode attributeNode)
            {
                // Only allow markup, generic & (non-razor comment) token nodes
                var containsNonMarkupNodes = attributeNode.DescendantNodes()
                    .Any(n => !(n is MarkupBlockSyntax ||
                        n is MarkupSyntaxNode ||
                        n is GenericBlockSyntax ||
                        (n is SyntaxNode sn && sn.IsToken && sn.Kind != SyntaxKind.RazorCommentTransition)));
                return containsNonMarkupNodes;
            }
        }

        private Diagnostic[] FilterCSharpDiagnostics(Diagnostic[] unmappedDiagnostics, RazorCodeDocument codeDocument)
        {
            return unmappedDiagnostics.Where(d =>
                !ShouldFilterCSharpDiagnosticBasedOnErrorCode(d, codeDocument)).ToArray();
        }

        private bool ShouldFilterCSharpDiagnosticBasedOnErrorCode(Diagnostic diagnostic, RazorCodeDocument codeDocument)
        {
            if (!diagnostic.Code.HasValue)
            {
                return false;
            }

            return diagnostic.Code.Value.String switch
            {
                "CS1525" => ShouldIgnoreCS1525(diagnostic, codeDocument),
                _ => CSharpDiagnosticsToIgnore.Contains(diagnostic.Code.Value.String) &&
                        diagnostic.Severity != DiagnosticSeverity.Error,
            };

            bool ShouldIgnoreCS1525(Diagnostic diagnostic, RazorCodeDocument codeDocument)
            {
                if (CheckIfDocumentHasRazorDiagnostic(codeDocument, RazorDiagnosticFactory.TagHelper_EmptyBoundAttribute.Id) &&
                    TryGetOriginalDiagnosticRange(diagnostic.Range, diagnostic.Severity, codeDocument, out var originalRange) &&
                    originalRange.IsUndefined())
                {
                    // Empty attribute values will take the following form in the generated C# document:
                    // __o = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ProgressEventArgs>(this, );
                    // The trailing `)` with no value preceding it, will lead to a C# error which doesn't make sense within the razor file.
                    // The empty attribute value is not directly mappable to Razor, hence we check if the diagnostic has an undefined range.
                    // Note; Error RZ2008 informs the user that the empty attribute value is not allowed.
                    // https://github.com/dotnet/aspnetcore/issues/30480
                    return true;
                }

                return false;
            }
        }

        // Internal & virtual for testing
        internal virtual bool CheckIfDocumentHasRazorDiagnostic(RazorCodeDocument codeDocument, string razorDiagnosticCode)
        {
            return codeDocument.GetSyntaxTree().Diagnostics.Any(d => d.Id.Equals(razorDiagnosticCode, StringComparison.Ordinal));
        }

        private Diagnostic[] MapDiagnostics(
            RazorLanguageKind languageKind,
            IReadOnlyList<Diagnostic> diagnostics,
            RazorCodeDocument codeDocument)
        {
            if (languageKind != RazorLanguageKind.CSharp)
            {
                // All other non-C# requests map directly to where they are in the document.
                return diagnostics.ToArray();
            }

            var mappedDiagnostics = new List<Diagnostic>();

            for (var i = 0; i < diagnostics.Count; i++)
            {
                var diagnostic = diagnostics[i];

                if (!TryGetOriginalDiagnosticRange(diagnostic.Range, diagnostic.Severity, codeDocument, out var originalRange))
                {
                    continue;
                }

                diagnostic.Range = originalRange;
                mappedDiagnostics.Add(diagnostic);
            }

            return mappedDiagnostics.ToArray();
        }

        private bool TryGetOriginalDiagnosticRange(Range projectedRange, DiagnosticSeverity? severity, RazorCodeDocument codeDocument, out Range originalRange)
        {
            if (!_documentMappingService.TryMapFromProjectedDocumentRange(
                    codeDocument,
                    projectedRange,
                    MappingBehavior.Inclusive,
                    out originalRange))
            {
                // Couldn't remap the range correctly.
                // If this isn't an `Error` Severity Diagnostic we can discard it.
                if (severity != DiagnosticSeverity.Error)
                {
                    return false;
                }

                // For `Error` Severity diagnostics we still show the diagnostics to
                // the user, however we set the range to an undefined range to ensure
                // clicking on the diagnostic doesn't cause errors.
                originalRange = RangeExtensions.UndefinedRange;
            }

            return true;
        }
    }
}
