// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(ProjectPathProvider), Constants.GuestOnlyWorkspaceLayer)]
    internal class GuestProjectPathProviderFactory : IWorkspaceServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly ITextDocumentFactoryService _textDocumentFactory;
        private readonly LiveShareClientProvider _liveShareClientProvider;

        [ImportingConstructor]
        public GuestProjectPathProviderFactory(
            ForegroundDispatcher foregroundDispatcher,
            JoinableTaskContext joinableTaskContext,
            ITextDocumentFactoryService textDocumentFactory,
            LiveShareClientProvider liveShareClientProvider)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (joinableTaskContext == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (textDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(textDocumentFactory));
            }

            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _joinableTaskContext = joinableTaskContext;
            _textDocumentFactory = textDocumentFactory;
            _liveShareClientProvider = liveShareClientProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            var languageServices = workspaceServices.GetLanguageServices(RazorLanguage.Name);
            var proxyAccessor = languageServices.GetRequiredService<ProxyAccessor>();

            var projectPathProvider = new GuestProjectPathProvider(
                _foregroundDispatcher,
                _joinableTaskContext.Factory,
                _textDocumentFactory,
                proxyAccessor,
                _liveShareClientProvider);

            return projectPathProvider;
        }
    }
}
