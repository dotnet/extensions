// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis.Text;
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
        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly RazorProjectService _projectService;

        public RazorDocumentSynchronizationEndpoint(
            ForegroundDispatcherShim foregroundDispatcher,
            RazorProjectService projectService,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
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
            _projectService = projectService;
            _logger = logger;
        }

        // TODO: GO INCREMENTAL
        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public void SetCapability(SynchronizationCapability capability)
        {
            _logger.Log("Setting Capability");

            _capability = capability;
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            var newContent = notification.ContentChanges.Single().Text;
            await Task.Factory.StartNew(
                () => _projectService.UpdateDocument(newContent, notification.TextDocument.Uri.AbsolutePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
        {
            await Task.Factory.StartNew(
                () => _projectService.AddDocument(notification.TextDocument.Text, notification.TextDocument.Uri.AbsolutePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
        {
            await Task.Factory.StartNew(
                () => _projectService.RemoveDocument(notification.TextDocument.Uri.AbsolutePath),
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
            _logger.Log("Asked for attributes");

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
