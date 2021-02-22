// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    [Export(typeof(RazorBreakpointResolver))]
    internal class DefaultRazorBreakpointResolver : RazorBreakpointResolver
    {
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public DefaultRazorBreakpointResolver(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider)
        {
            if (fileUriProvider is null)
            {
                throw new ArgumentNullException(nameof(fileUriProvider));
            }

            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            _fileUriProvider = fileUriProvider;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
        }

        public override async Task<Range> TryResolveBreakpointRangeAsync(ITextBuffer textBuffer, int lineIndex, int characterIndex, CancellationToken cancellationToken)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (!_fileUriProvider.TryGet(textBuffer, out var documentUri))
            {
                // Not an addressable Razor document. Do not allow a breakpoint here. In practice this shouldn't happen, just being defensive.
                return null;
            }

            if (!_documentManager.TryGetDocument(documentUri, out var documentSnapshot))
            {
                // No associated Razor document. Do not allow a breakpoint here. In practice this shouldn't happen, just being defensive.
                return null;
            }

            var lspPosition = new Position(lineIndex, characterIndex);
            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, lspPosition, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                // Can't map the position, invalid breakpoint location.
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (projectionResult.LanguageKind != RazorLanguageKind.CSharp)
            {
                // We only allow breakpoints in C#
                return null;
            }

            if (!documentSnapshot.TryGetVirtualDocument<CSharpVirtualDocumentSnapshot>(out var virtualDocument))
            {
                Debug.Fail($"Some how there's no C# document associated with the host Razor document {documentUri.OriginalString} when validating breakpoint locations.");
                return null;
            }

            var sourceText = virtualDocument.Snapshot.AsText();
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
            if (!RazorBreakpointSpans.TryGetBreakpointSpan(syntaxTree, projectionResult.PositionIndex, cancellationToken, out var csharpBreakpointSpan))
            {
                return null;
            }

            virtualDocument.Snapshot.GetLineAndCharacter(csharpBreakpointSpan.Start, out var startLineIndex, out var startCharacterIndex);
            virtualDocument.Snapshot.GetLineAndCharacter(csharpBreakpointSpan.End, out var endLineIndex, out var endCharacterIndex);

            var projectedRange = new[]
            {
                new Range()
                {
                    Start = new Position(startLineIndex, startCharacterIndex),
                    End = new Position(endLineIndex, endCharacterIndex),
                },
            };
            var hostDocumentMapping = await _documentMappingProvider.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, documentUri, projectedRange, cancellationToken).ConfigureAwait(false);
            if (hostDocumentMapping == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var hostDocumentRange = hostDocumentMapping.Ranges.FirstOrDefault();
            return hostDocumentRange;
        }
    }
}
