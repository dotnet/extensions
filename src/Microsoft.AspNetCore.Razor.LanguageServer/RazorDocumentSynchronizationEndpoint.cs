// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.Embedded.MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorDocumentSynchronizationEndpoint : ITextDocumentSyncHandler
    {
        private SynchronizationCapability _capability;
        private readonly VSCodeLogger _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RemoteTextLoaderFactory _remoteTextLoaderFactory;
        private readonly RazorProjectService _projectService;

        public RazorDocumentSynchronizationEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RemoteTextLoaderFactory remoteTextLoaderFactory,
            RazorProjectService projectService,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (remoteTextLoaderFactory == null)
            {
                throw new ArgumentNullException(nameof(remoteTextLoaderFactory));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentResolver = documentResolver;
            _remoteTextLoaderFactory = remoteTextLoaderFactory;
            _projectService = projectService;
            _logger = logger;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Incremental;

        public void SetCapability(SynchronizationCapability capability)
        {
            _logger.Log("Setting Capability");

            _capability = capability;
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(notification.TextDocument.Uri.AbsolutePath, out var documentSnapshot);

                return documentSnapshot;
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            var sourceText = await document.GetTextAsync();
            sourceText = ApplyContentChanges(notification.ContentChanges, sourceText);

            await Task.Factory.StartNew(
                () => _projectService.UpdateDocument(document.FilePath, sourceText),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        // Internal for testing
        internal SourceText ApplyContentChanges(IEnumerable<TextDocumentContentChangeEvent> contentChanges, SourceText sourceText)
        {
            foreach (var change in contentChanges)
            {
                var linePosition = new LinePosition((int)change.Range.Start.Line, (int)change.Range.Start.Character);
                var position = sourceText.Lines.GetPosition(linePosition);
                var textSpan = new TextSpan(position, change.RangeLength);
                var textChange = new TextChange(textSpan, change.Text);

                _logger.Log("Applying " + textChange);

                // If there happens to be multiple text changes we generate a new source text for each one. Due to the
                // differences in VSCode and Roslyn's representation we can't pass in all changes simultaneously because
                // ordering may differ.
                sourceText = sourceText.WithChanges(textChange);
            }

            return sourceText;
        }

        public async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            var sourceText = SourceText.From(notification.TextDocument.Text);

            await Task.Factory.StartNew(
                () => _projectService.OpenDocument(notification.TextDocument.Uri.AbsolutePath, sourceText),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            var textLoader = _remoteTextLoaderFactory.Create(notification.TextDocument.Uri.AbsolutePath);
            await Task.Factory.StartNew(
                () => _projectService.CloseDocument(notification.TextDocument.Uri.AbsolutePath, textLoader),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
        {
            _logger.Log("Saved Document");

            return Unit.Task;
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new TextDocumentAttributes(uri, "razor");
        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = RazorDocument.Selector,
                SyncKind = Change
            };
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = RazorDocument.Selector,
            };
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = RazorDocument.Selector,
                IncludeText = true
            };
        }
    }
}
