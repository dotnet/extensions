// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    // This overrides the default Razor implementation of the ProjectSnapshotManager
    [ExportLanguageServiceFactory(typeof(ProjectSnapshotManager), RazorLanguage.Name, Constants.GuestOnlyWorkspaceLayer)]
    internal class GuestProjectSnapshotManagerFactory : ILanguageServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly LiveShareClientProvider _liveShareClientProvider;

        [ImportingConstructor]
        public GuestProjectSnapshotManagerFactory(
            ForegroundDispatcher foregroundDispatcher,
            LiveShareClientProvider liveShareClientProvider)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _liveShareClientProvider = liveShareClientProvider;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var projectSnapshotStore = languageServices.GetRequiredService<ProjectSnapshotHandleStore>();
            var projectSnapshotFactory = languageServices.GetRequiredService<ProjectSnapshotFactory>();

            var snapshotManager = new GuestProjectSnapshotManager(
                _foregroundDispatcher, 
                languageServices.WorkspaceServices, 
                projectSnapshotStore, 
                projectSnapshotFactory, 
                _liveShareClientProvider, 
                languageServices.WorkspaceServices.Workspace);
            return snapshotManager;
        }
    }
}
