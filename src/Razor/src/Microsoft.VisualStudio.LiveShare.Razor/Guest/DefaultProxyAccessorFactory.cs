// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(ProxyAccessor), RazorLanguage.Name, Constants.GuestOnlyWorkspaceLayer)]
    public class DefaultProxyAccessorFactory : ILanguageServiceFactory
    {
        private readonly LiveShareClientProvider _liveShareClientProvider;
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public DefaultProxyAccessorFactory(
            LiveShareClientProvider liveShareClientProvider,
            JoinableTaskContext joinableTaskContext)
        {
            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            if (joinableTaskContext == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _liveShareClientProvider = liveShareClientProvider;
            _joinableTaskContext = joinableTaskContext;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            var proxyAccessor = new DefaultProxyAccessor(_liveShareClientProvider, _joinableTaskContext.Factory);
            return proxyAccessor;
        }
    }
}
