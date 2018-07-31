// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorDocumentSynchronizationHandler : LanguageServerHandlerBase, ITextDocumentSyncHandler
    {
        private SynchronizationCapability _capability;
        private readonly ProjectSnapshotManagerShimAccessor _projectSnapshotManagerAccessor;

        public RazorDocumentSynchronizationHandler(
            ILanguageServer router,
            ProjectSnapshotManagerShimAccessor projectSnapshotManagerAccessor) : base(router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Incremental;

        public void SetCapability(SynchronizationCapability capability)
        {
            LogToClient("Setting Capability");

            _capability = capability;
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            LogToClient("Changed Document");

            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
        {
            LogToClient("Opened Document");

            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
        {
            LogToClient("Closed Document");

            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
        {
            LogToClient("Saved Document");

            return Unit.Task;
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            LogToClient("Asked for attributes");

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
