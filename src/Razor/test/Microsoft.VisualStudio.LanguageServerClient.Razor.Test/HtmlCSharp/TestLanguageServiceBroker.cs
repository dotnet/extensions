// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class TestLanguageServiceBroker : ILanguageServiceBroker2
    {
        private readonly Action<string, string> _callback;

#pragma warning disable CS0067 // The event is never used
        public event EventHandler<LanguageClientLoadedEventArgs> LanguageClientLoaded;
        public event AsyncEventHandler<LanguageClientNotifyEventArgs> ClientNotifyAsync;
#pragma warning restore CS0067 // The event is never used

        public IEnumerable<ILanguageClientInstance> ActiveLanguageClients => throw new NotImplementedException();

        public IStreamingRequestBroker<CompletionParams, CompletionList> CompletionBroker => throw new NotImplementedException();

        public IRequestBroker<CompletionItem, CompletionItem> CompletionResolveBroker => throw new NotImplementedException();

        public IStreamingRequestBroker<ReferenceParams, object[]> ReferencesBroker => throw new NotImplementedException();

        public IRequestBroker<TextDocumentPositionParams, object[]> ImplementationBroker => throw new NotImplementedException();

        public IRequestBroker<TextDocumentPositionParams, object[]> TypeDefinitionBroker => throw new NotImplementedException();

        public IRequestBroker<TextDocumentPositionParams, object[]> DefinitionBroker => throw new NotImplementedException();

        public IRequestBroker<TextDocumentPositionParams, Hover> HoverBroker => throw new NotImplementedException();

        public IRequestBroker<RenameParams, WorkspaceEdit> RenameBroker => throw new NotImplementedException();

        public IRequestBroker<DocumentFormattingParams, TextEdit[]> DocumentFormattingBroker => throw new NotImplementedException();

        public IRequestBroker<DocumentRangeFormattingParams, TextEdit[]> RangeFormattingBroker => throw new NotImplementedException();

        public IRequestBroker<DocumentOnTypeFormattingParams, TextEdit[]> OnTypeFormattingBroker => throw new NotImplementedException();

        public IRequestBroker<ExecuteCommandParams, object> ExecuteCommandBroker => throw new NotImplementedException();

        public IRequestBroker<CodeActionParams, SumType<Command, CodeAction>[]> CodeActionsBroker => throw new NotImplementedException();

        public IStreamingRequestBroker<DocumentHighlightParams, DocumentHighlight[]> DocumentHighlightBroker => throw new NotImplementedException();

        public IRequestBroker<SignatureHelpParams, SignatureHelp> SignatureHelpBroker => throw new NotImplementedException();

        public IRequestBroker<DocumentSymbolParams, SymbolInformation[]> DocumentSymbolBroker => throw new NotImplementedException();

        public IStreamingRequestBroker<WorkspaceSymbolParams, SymbolInformation[]> WorkspaceSymbolBroker => throw new NotImplementedException();

        public IRequestBroker<FoldingRangeParams, FoldingRange[]> FoldingRangeBroker => throw new NotImplementedException();

        public IRequestBroker<GetTextDocumentWithContextParams, ActiveProjectContexts> ProjectContextBroker => throw new NotImplementedException();

        public IEnumerable<Lazy<ILanguageClient, IContentTypeMetadata>> FactoryLanguageClients => throw new NotImplementedException();

        public IEnumerable<Lazy<ILanguageClient, IContentTypeMetadata>> LanguageClients => throw new NotImplementedException();

        public IRequestBroker<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem> OnAutoInsertBroker => throw new NotImplementedException();

        public IRequestBroker<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem> OnTypeRenameBroker => throw new NotImplementedException();

        public IRequestBroker<CodeAction, CodeAction> CodeActionsResolveBroker => throw new NotImplementedException();

        public IStreamingRequestBroker<DocumentDiagnosticsParams, DiagnosticReport[]> DocumentDiagnosticsBroker => throw new NotImplementedException();

        public IStreamingRequestBroker<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport[]> WorkspaceDiagnosticsBroker => throw new NotImplementedException();

        public IRequestBroker<KindAndModifier, IconMapping> KindDescriptionResolveBroker => throw new NotImplementedException();

        public TestLanguageServiceBroker(Action<string, string> callback)
        {
            _callback = callback;
        }

        public Task LoadAsync(ILanguageClientMetadata metadata, ILanguageClient client)
        {
            throw new NotImplementedException();
        }

        public Task<(ILanguageClient, JToken)> RequestAsync(
            string[] contentTypes,
            Func<JToken, bool> capabilitiesFilter,
            string method,
            JToken parameters,
            CancellationToken cancellationToken)
        {
            // We except it to be called with only one content type.
            var contentType = Assert.Single(contentTypes);

            _callback?.Invoke(contentType, method);

            return Task.FromResult<(ILanguageClient, JToken)>((null, null));
        }

        public IEnumerable<(Uri, JToken)> GetAllDiagnostics()
        {
            throw new NotImplementedException();
        }

        public JToken GetDiagnostics(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task<JToken> RequestAsync(ILanguageClient languageClient, string method, JToken parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<(ILanguageClient, JToken)>> RequestMultipleAsync(string[] contentTypes, Func<JToken, bool> capabilitiesFilter, string method, JToken parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void AddCustomBufferContentTypes(IEnumerable<string> contentTypes)
        {
            throw new NotImplementedException();
        }

        public void RemoveCustomBufferContentTypes(IEnumerable<string> contentTypes)
        {
            throw new NotImplementedException();
        }

        public void AddLanguageClients(IEnumerable<Lazy<ILanguageClient, IContentTypeMetadata>> items)
        {
            throw new NotImplementedException();
        }

        public void RemoveLanguageClients(IEnumerable<Lazy<ILanguageClient, IContentTypeMetadata>> items)
        {
            throw new NotImplementedException();
        }

        public Task LoadAsync(IContentTypeMetadata contentType, ILanguageClient client)
        {
            throw new NotImplementedException();
        }

        public Task OnDidOpenTextDocumentAsync(ITextSnapshot snapShot)
        {
            throw new NotImplementedException();
        }

        public Task OnDidCloseTextDocumentAsync(ITextSnapshot snapShot)
        {
            throw new NotImplementedException();
        }

        public Task OnDidChangeTextDocumentAsync(ITextSnapshot before, ITextSnapshot after, IEnumerable<ITextChange> textChanges)
        {
            throw new NotImplementedException();
        }

        public Task OnDidSaveTextDocumentAsync(ITextDocument document)
        {
            throw new NotImplementedException();
        }

        public Task<(ILanguageClient, TOut)> RequestAsync<TIn, TOut>(string[] contentTypes, Func<ServerCapabilities, bool> capabilitiesFilter, LspRequest<TIn, TOut> method, TIn parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TOut> RequestAsync<TIn, TOut>(ILanguageClient languageClient, LspRequest<TIn, TOut> method, TIn parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<(ILanguageClient, TOut)>> RequestMultipleAsync<TIn, TOut>(string[] contentTypes, Func<ServerCapabilities, bool> capabilitiesFilter, LspRequest<TIn, TOut> method, TIn parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
