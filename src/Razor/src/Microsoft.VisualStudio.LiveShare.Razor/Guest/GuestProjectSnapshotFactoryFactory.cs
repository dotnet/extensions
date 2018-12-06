// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(ProjectSnapshotFactory), RazorLanguage.Name, Constants.GuestOnlyWorkspaceLayer)]
    internal class GuestProjectSnapshotFactoryFactory : ILanguageServiceFactory
    {
        private readonly LiveShareClientProvider _liveShareClientProvider;

        [ImportingConstructor]
        public GuestProjectSnapshotFactoryFactory(LiveShareClientProvider liveShareClientProvider)
        {
            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            _liveShareClientProvider = liveShareClientProvider;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var projectSnapshotFactory = new GuestProjectSnapshotFactory(languageServices.WorkspaceServices.Workspace, _liveShareClientProvider);
            return projectSnapshotFactory;
        }
    }
}
