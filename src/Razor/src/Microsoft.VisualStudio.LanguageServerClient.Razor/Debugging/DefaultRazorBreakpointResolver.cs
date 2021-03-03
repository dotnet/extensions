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
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    [Export(typeof(RazorBreakpointResolver))]
    internal class DefaultRazorBreakpointResolver : RazorBreakpointResolver
    {
        private readonly FileUriProvider _fileUriProvider;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly VisualStudioWorkspaceAccessor _workspaceAccessor;
        private readonly MemoryCache<CacheKey, Range> _cache;

        [ImportingConstructor]
        public DefaultRazorBreakpointResolver(
            FileUriProvider fileUriProvider,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider,
            VisualStudioWorkspaceAccessor workspaceAccessor)
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

            if (workspaceAccessor is null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            _fileUriProvider = fileUriProvider;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
            _workspaceAccessor = workspaceAccessor;

            // 4 is a magic number that was determined based on the functionality of VisualStudio. Currently when you set or edit a breakpoint
            // we get called with two different locations for the same breakpoint. Because of this 2 time call our size must be at least 2,
            // we grow it to 4 just to be safe for lesser known scenarios.
            _cache = new MemoryCache<CacheKey, Range>(sizeLimit: 4);
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

            var cacheKey = new CacheKey(documentSnapshot.Uri, documentSnapshot.Version, lineIndex, characterIndex);
            if (_cache.TryGetValue(cacheKey, out var cachedRange))
            {
                // We've seen this request before, no need to go async.
                return cachedRange;
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

            _workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace);

            var syntaxTree = await virtualDocument.GetCSharpSyntaxTreeAsync(workspace, cancellationToken).ConfigureAwait(false);
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

            // Cache range so if we're asked again for this document/line/character we don't have to go async.
            _cache.Set(cacheKey, hostDocumentRange);

            return hostDocumentRange;
        }

        private record CacheKey(Uri DocumentUri, int DocumentVersion, int Line, int Character);
    }
}
