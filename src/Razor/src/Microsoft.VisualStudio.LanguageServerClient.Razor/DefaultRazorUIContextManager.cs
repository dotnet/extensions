// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(RazorUIContextManager))]
    internal class DefaultRazorUIContextManager : RazorUIContextManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        [ImportingConstructor]
        public DefaultRazorUIContextManager(SVsServiceProvider serviceProvider, JoinableTaskContext joinableTaskContext)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _serviceProvider = serviceProvider;
            _joinableTaskFactory = joinableTaskContext.Factory;
        }

        public override async Task SetUIContextAsync(Guid uiContextGuid, bool isActive, CancellationToken cancellationToken)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();

            var monitorSelection = _serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            Assumes.Present(monitorSelection);
            var cookieResult = monitorSelection.GetCmdUIContextCookie(uiContextGuid, out var cookie);
            ErrorHandler.ThrowOnFailure(cookieResult);

            var setContextResult = monitorSelection.SetCmdUIContext(cookie, isActive ? 1 : 0);
            ErrorHandler.ThrowOnFailure(setContextResult);
        }
    }
}
