// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    /// <summary>
    /// Resolves and remaps the code action, without running formatting passes.
    /// </summary>
    internal class UnformattedRemappingCSharpCodeActionResolver : CSharpCodeActionResolver
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly RazorDocumentMappingService _documentMappingService;

        public UnformattedRemappingCSharpCodeActionResolver(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            ClientNotifierServiceBase languageServer,
            DocumentVersionCache documentVersionCache,
            RazorDocumentMappingService documentMappingService)
            : base(languageServer)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _documentVersionCache = documentVersionCache ?? throw new ArgumentNullException(nameof(documentVersionCache));
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
        }

        public override string Action => LanguageServerConstants.CodeActions.UnformattedRemap;

        public async override Task<CodeAction> ResolveAsync(
            CSharpCodeActionParams csharpParams,
            CodeAction codeAction,
            CancellationToken cancellationToken)
        {
            if (csharpParams is null)
            {
                throw new ArgumentNullException(nameof(csharpParams));
            }

            if (codeAction is null)
            {
                throw new ArgumentNullException(nameof(codeAction));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var resolvedCodeAction = await ResolveCodeActionWithServerAsync(codeAction, cancellationToken).ConfigureAwait(false);
            if (resolvedCodeAction.Edit?.DocumentChanges is null)
            {
                // Unable to resolve code action with server, return original code action
                return codeAction;
            }

            if (resolvedCodeAction.Edit.DocumentChanges.Count() != 1)
            {
                // We don't yet support multi-document code actions, return original code action
                Debug.Fail($"Encountered an unsupported multi-document code action edit with ${codeAction.Title}.");
                return codeAction;
            }

            var documentChanged = resolvedCodeAction.Edit.DocumentChanges.First();
            if (!documentChanged.IsTextDocumentEdit)
            {
                // Only Text Document Edit changes are supported currently, return original code action
                return codeAction;
            }

            var textEdit = documentChanged.TextDocumentEdit.Edits.FirstOrDefault();
            if (textEdit is null)
            {
                // No text edit available
                return codeAction;
            }

            var (documentSnapshot, documentVersion) = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(csharpParams.RazorFileUri.ToUri().GetAbsoluteOrUNCPath(), out var documentSnapshot);

                _documentVersionCache.TryGetDocumentVersion(documentSnapshot, out var version);

                return (documentSnapshot, version);
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);

            if (documentSnapshot is null)
            {
                return codeAction;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return codeAction;
            }

            if (!_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, textEdit.Range, MappingBehavior.Inclusive, out var originalRange))
            {
                // Text edit failed to map
                return codeAction;
            }

            textEdit.Range = originalRange;

            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier()
            {
                Uri = csharpParams.RazorFileUri,
                Version = documentVersion
            };

            resolvedCodeAction.Edit = new WorkspaceEdit()
            {
                DocumentChanges = new[] {
                    new WorkspaceEditDocumentChange(
                        new TextDocumentEdit()
                        {
                            TextDocument = codeDocumentIdentifier,
                            Edits = new[] { textEdit },
                        }
                    )
                }
            };

            return resolvedCodeAction;
        }
    }
}
