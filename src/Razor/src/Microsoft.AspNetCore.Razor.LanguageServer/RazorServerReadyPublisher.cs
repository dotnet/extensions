// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorServerReadyPublisher : ProjectSnapshotChangeTrigger
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectManager;
        private readonly IClientLanguageServer _languageServer;
        private bool _hasNotified = false;

        public RazorServerReadyPublisher(
            ForegroundDispatcher foregroundDispatcher,
            IClientLanguageServer languageServer)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _languageServer = languageServer;
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;

            _projectManager.Changed += ProjectSnapshotManager_Changed;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            _foregroundDispatcher.AssertForegroundThread();

            var projectSnapshot = args.Newer;
            if (projectSnapshot?.ProjectWorkspaceState != null && !_hasNotified)
            {
                // Un-register this method, we only need to send this once.
                _projectManager.Changed -= ProjectSnapshotManager_Changed;

                var response = _languageServer.SendRequest(LanguageServerConstants.RazorServerReadyEndpoint);
                await response.ReturningVoid(CancellationToken.None);

                _hasNotified = true;
            }
        }
    }
}
