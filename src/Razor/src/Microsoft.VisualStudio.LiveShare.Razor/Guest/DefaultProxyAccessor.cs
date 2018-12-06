// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class DefaultProxyAccessor : ProxyAccessor
    {
        private readonly LiveShareClientProvider _liveShareClientProvider;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private IProjectSnapshotManagerProxy _projectSnapshotManagerProxy;
        private IProjectHierarchyProxy _projectHierarchyProxy;

        public DefaultProxyAccessor(
            LiveShareClientProvider liveShareClientProvider,
            JoinableTaskFactory joinableTaskFactory)
        {
            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            if (joinableTaskFactory == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskFactory));
            }

            _liveShareClientProvider = liveShareClientProvider;
            _joinableTaskFactory = joinableTaskFactory;
        }

        // Testing constructor
        private protected DefaultProxyAccessor()
        {
        }

        public override IProjectSnapshotManagerProxy GetProjectSnapshotManagerProxy()
        {
            if (_projectSnapshotManagerProxy == null)
            {
                _projectSnapshotManagerProxy = CreateServiceProxy<IProjectSnapshotManagerProxy>();
            }

            return _projectSnapshotManagerProxy;
        }

        public override IProjectHierarchyProxy GetProjectHierarchyProxy()
        {
            if (_projectHierarchyProxy == null)
            {
                _projectHierarchyProxy = CreateServiceProxy<IProjectHierarchyProxy>();
            }

            return _projectHierarchyProxy;
        }

        // Internal virtual for testing
        internal virtual TProxy CreateServiceProxy<TProxy>() where TProxy : class
        {
            return _joinableTaskFactory.Run(() => _liveShareClientProvider.CreateServiceProxyAsync<TProxy>());
        }
    }
}
