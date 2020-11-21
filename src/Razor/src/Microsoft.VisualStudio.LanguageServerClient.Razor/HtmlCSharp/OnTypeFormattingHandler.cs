// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentOnTypeFormattingName)]
    internal class OnTypeFormattingHandler : IRequestHandler<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        private static readonly IReadOnlyList<string> CSharpTriggerCharacters = new[] { "}", ";" };
        private static readonly IReadOnlyList<string> HtmlTriggerCharacters = Array.Empty<string>();
        private static readonly IReadOnlyList<string> AllTriggerCharacters = CSharpTriggerCharacters.Concat(HtmlTriggerCharacters).ToArray();

        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPProjectionProvider _projectionProvider;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;

        [ImportingConstructor]
        public OnTypeFormattingHandler(
            LSPDocumentManager documentManager,
            LSPRequestInvoker requestInvoker,
            LSPProjectionProvider projectionProvider,
            LSPDocumentMappingProvider documentMappingProvider)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            _documentManager = documentManager;
            _requestInvoker = requestInvoker;
            _projectionProvider = projectionProvider;
            _documentMappingProvider = documentMappingProvider;
        }

        public async Task<TextEdit[]> HandleRequestAsync(DocumentOnTypeFormattingParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (!AllTriggerCharacters.Contains(request.Character, StringComparer.Ordinal))
            {
                // Unexpected trigger character.
                return null;
            }

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var triggerCharacterKind = await GetTriggerCharacterLanguageKindAsync(documentSnapshot, request.Position, request.Character, cancellationToken).ConfigureAwait(false);
            if (triggerCharacterKind == null || triggerCharacterKind != RazorLanguageKind.CSharp)
            {
                return null;
            }

            if (!IsApplicableTriggerCharacter(request.Character, triggerCharacterKind.Value))
            {
                // We were triggered but the trigger character doesn't make sense for the current cursor position. Bail.
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                return null;
            }

            var formattingParams = new DocumentOnTypeFormattingParams()
            {
                Character = request.Character,
                Options = request.Options,
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier() { Uri = projectionResult.Uri }
            };

            cancellationToken.ThrowIfCancellationRequested();

            var contentType = triggerCharacterKind.Value.ToContainedLanguageContentType();
            var response = await _requestInvoker.ReinvokeRequestOnServerAsync<DocumentOnTypeFormattingParams, TextEdit[]>(
                Methods.TextDocumentOnTypeFormattingName,
                contentType,
                formattingParams,
                cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var remappedEdits = await _documentMappingProvider.RemapFormattedTextEditsAsync(projectionResult.Uri, response, request.Options, containsSnippet: false, cancellationToken).ConfigureAwait(false);
            return remappedEdits;
        }

        private async Task<RazorLanguageKind?> GetTriggerCharacterLanguageKindAsync(LSPDocumentSnapshot documentSnapshot, Position positionAfterTriggerChar, string triggerCharacter, CancellationToken cancellationToken)
        {
            // request.Character will point to the position after the character that was inserted.
            // For onTypeFormatting, it makes more sense to look up the projection of the character that was inserted.
            var line = documentSnapshot.Snapshot.GetLineFromLineNumber(positionAfterTriggerChar.Line);
            var position = line.Start.Position + positionAfterTriggerChar.Character;
            var point = new SnapshotPoint(documentSnapshot.Snapshot, position);

            // Subtract the trigger character length to go back to the position of the trigger character
            var triggerCharacterPoint = point.Subtract(triggerCharacter.Length);

            var triggerCharacterLine = documentSnapshot.Snapshot.GetLineFromPosition(triggerCharacterPoint.Position);
            var triggerCharacterPosition = new Position(triggerCharacterLine.LineNumber, triggerCharacterPoint.Position - triggerCharacterLine.Start.Position);

            var triggerCharacterProjectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, triggerCharacterPosition, cancellationToken).ConfigureAwait(false);

            return triggerCharacterProjectionResult?.LanguageKind;
        }

        private static bool IsApplicableTriggerCharacter(string triggerCharacter, RazorLanguageKind languageKind)
        {
            if (languageKind == RazorLanguageKind.CSharp)
            {
                return CSharpTriggerCharacters.Contains(triggerCharacter);
            }
            else if (languageKind == RazorLanguageKind.Html)
            {
                return HtmlTriggerCharacters.Contains(triggerCharacter);
            }

            // Unknown trigger character.
            return false;
        }
    }
}
