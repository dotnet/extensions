// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.VisualStudio.LiveShare.Razor.Guest;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(ProjectHierarchyInspector))]
    internal class DefaultProjectHierarchyInspector : ProjectHierarchyInspector
    {
        private readonly LiveShareSessionAccessor _sessionAccessor;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        [ImportingConstructor]
        public DefaultProjectHierarchyInspector(
            LiveShareSessionAccessor sessionAccessor,
            JoinableTaskContext joinableTaskContext)
        {
            if (sessionAccessor is null)
            {
                throw new ArgumentNullException(nameof(sessionAccessor));
            }

            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _sessionAccessor = sessionAccessor;
            _joinableTaskFactory = joinableTaskContext.Factory;
        }

        public override bool HasCapability(string documentMoniker, IVsHierarchy hierarchy, string capability)
        {
            if (_sessionAccessor.IsGuestSessionActive)
            {
                var remoteHasCapability = RemoteHasCapability(documentMoniker, capability);
                return remoteHasCapability;
            }

            var localHasCapability = LocalHasCapability(hierarchy, capability);
            return localHasCapability;
        }

        private static bool LocalHasCapability(IVsHierarchy hierarchy, string capability)
        {
            if (hierarchy == null)
            {
                return false;
            }

            try
            {
                var hasCapability = hierarchy.IsCapabilityMatch(capability);
                return hasCapability;
            }
            catch (NotSupportedException)
            {
                // IsCapabilityMatch throws a NotSupportedException if it can't create a
                // BooleanSymbolExpressionEvaluator COM object
                return false;
            }
            catch (ObjectDisposedException)
            {
                // IsCapabilityMatch throws an ObjectDisposedException if the underlying hierarchy has been disposed
                return false;
            }
        }

        private bool RemoteHasCapability(string documentMoniker, string capability)
        {
            // On a guest box. The project hierarchy is not fully populated. We need to ask the host machine
            // questions on hierarchy capabilities.
            return _joinableTaskFactory.Run(async () =>
            {
                var remoteHierarchyService = await _sessionAccessor.Session.GetRemoteServiceAsync<IRemoteHierarchyService>(nameof(IRemoteHierarchyService), CancellationToken.None).ConfigureAwait(false);
                var documentMonikerUri = _sessionAccessor.Session.ConvertLocalPathToSharedUri(documentMoniker);
                var hasCapability = await remoteHierarchyService.HasCapabilityAsync(documentMonikerUri, capability, CancellationToken.None).ConfigureAwait(false);
                return hasCapability;
            });
        }

    }
}
