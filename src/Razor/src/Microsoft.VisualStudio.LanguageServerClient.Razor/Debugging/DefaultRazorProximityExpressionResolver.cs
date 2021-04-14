// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    [Export(typeof(RazorProximityExpressionResolver))]
    internal class DefaultRazorProximityExpressionResolver : RazorProximityExpressionResolver
    {
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly CodeAnalysis.Workspace _workspace;
        private readonly MemoryCache<CacheKey, IReadOnlyList<string>> _cache;

        [ImportingConstructor]
        public DefaultRazorProximityExpressionResolver(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            VisualStudioWorkspace workspace) : this(fileUriProvider, documentManager, projectionProvider, (CodeAnalysis.Workspace)workspace)
        {
            if (workspace is null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
        }

        private DefaultRazorProximityExpressionResolver(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            CodeAnalysis.Workspace workspace)
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

            _fileUriProvider = fileUriProvider;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _workspace = workspace;

            // 10 is a magic number where this effectively represents our ability to cache the last 10 "hit" breakpoint locations
            // corresponding proximity expressions which enables us not to go "async" in those re-hit scenarios.
            _cache = new MemoryCache<CacheKey, IReadOnlyList<string>>(sizeLimit: 10);
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

            var cacheKey = new CacheKey(documentSnapshot.Uri, documentSnapshot.Version, lineIndex, characterIndex);
            if (_cache.TryGetValue(cacheKey, out var cachedExpressions))
            {
                // We've seen this request before, no need to go async.
                return cachedExpressions;
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

            var syntaxTree = await virtualDocument.GetCSharpSyntaxTreeAsync(_workspace, cancellationToken).ConfigureAwait(false);
            var proximityExpressions = RazorCSharpProximityExpressionResolverService.GetProximityExpressions(syntaxTree, projectionResult.PositionIndex, cancellationToken)?.ToList();

            // Cache range so if we're asked again for this document/line/character we don't have to go async.
            _cache.Set(cacheKey, proximityExpressions);

            return proximityExpressions;
        }

        public static DefaultRazorProximityExpressionResolver CreateTestInstance(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider) => new DefaultRazorProximityExpressionResolver(fileUriProvider, documentManager, projectionProvider, (CodeAnalysis.Workspace)null);

        private record CacheKey(Uri DocumentUri, int DocumentVersion, int Line, int Character);
    }
}
