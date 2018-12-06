// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Host
{
    internal class DefaultProjectHierarchyProxy : IProjectHierarchyProxy, ICollaborationService
    {
        private readonly CollaborationSession _session;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private IVsUIShellOpenDocument _openDocumentShell;

        public DefaultProjectHierarchyProxy(
            CollaborationSession session,
            ForegroundDispatcher foregroundDispatcher,
            JoinableTaskFactory joinableTaskFactory)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (joinableTaskFactory == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskFactory));
            }

            _session = session;
            _foregroundDispatcher = foregroundDispatcher;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task<Uri> GetProjectPathAsync(Uri documentFilePath, CancellationToken cancellationToken)
        {
            if (documentFilePath == null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            _foregroundDispatcher.AssertBackgroundThread();

            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (_openDocumentShell == null)
            {
                _openDocumentShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            }

            var hostDocumentFilePath = _session.ConvertSharedUriToLocalPath(documentFilePath);
            var hr = _openDocumentShell.IsDocumentInAProject(hostDocumentFilePath, out var hierarchy, out _, out _, out _);
            if (ErrorHandler.Succeeded(hr) && hierarchy != null)
            {
                ErrorHandler.ThrowOnFailure(((IVsProject)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var path), VSConstants.E_NOTIMPL);

                return _session.ConvertLocalPathToSharedUri(path);
            }

            return null;
        }
    }
}
