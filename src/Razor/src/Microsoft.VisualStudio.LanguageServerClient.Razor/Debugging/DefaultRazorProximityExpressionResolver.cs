// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    [Export(typeof(RazorProximityExpressionResolver))]
    internal class DefaultRazorProximityExpressionResolver : RazorProximityExpressionResolver
    {
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly CSharpProximityExpressionResolver _csharpProximityExpressionResolver;

        [ImportingConstructor]
        public DefaultRazorProximityExpressionResolver(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            CSharpProximityExpressionResolver csharpProximityExpressionResolver)
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

            if (csharpProximityExpressionResolver is null)
            {
                throw new ArgumentNullException(nameof(csharpProximityExpressionResolver));
            }

            _fileUriProvider = fileUriProvider;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _csharpProximityExpressionResolver = csharpProximityExpressionResolver;
        }

        public override async Task<IReadOnlyList<string>> TryResolveProximityExpressionsAsync(ITextBuffer textBuffer, int lineIndex, int characterIndex, CancellationToken cancellationToken)
        {
            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (!_fileUriProvider.TryGet(textBuffer, out var documentUri))
            {
                // Not an addressable Razor document. Do not allow expression resolution here. In practice this shouldn't happen, just being defensive.
                return null;
            }

            if (!_documentManager.TryGetDocument(documentUri, out var documentSnapshot))
            {
                // No associated Razor document. Do not resolve expressions here. In practice this shouldn't happen, just being defensive.
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
                // We only allow proximity expressions in C#
                return null;
            }

            if (!documentSnapshot.TryGetVirtualDocument<CSharpVirtualDocumentSnapshot>(out var virtualDocument))
            {
                Debug.Fail($"Somehow there's no C# document associated with the host Razor document {documentUri.OriginalString} when retrieving proximity expressions.");
                return null;
            }

            var sourceText = virtualDocument.Snapshot.AsText();
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
            var proximityExpressions = _csharpProximityExpressionResolver.GetProximityExpressions(syntaxTree, projectionResult.PositionIndex, cancellationToken);


            return proximityExpressions;
        }
    }
}
