// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal class DefaultProjectSnapshotHandleStore : ProjectSnapshotHandleStore
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly ProxyAccessor _proxyAccessor;

        // Internal for testing
        internal JoinableTask TestInitializationTask;

        private IReadOnlyList<ProjectSnapshotHandleProxy> _projectHandles;
        private bool _triggerChangeAfterInitialize;

        public DefaultProjectSnapshotHandleStore(
            ForegroundDispatcher foregroundDispatcher,
            JoinableTaskFactory joinableTaskFactory,
            ProxyAccessor proxyAccessor)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (joinableTaskFactory == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskFactory));
            }

            if (proxyAccessor == null)
            {
                throw new ArgumentNullException(nameof(proxyAccessor));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _joinableTaskFactory = joinableTaskFactory;
            _proxyAccessor = proxyAccessor;
        }

        public override event EventHandler<ProjectProxyChangeEventArgs> Changed;

        private bool Initialized => _projectHandles != null;

        public override IReadOnlyList<ProjectSnapshotHandleProxy> GetProjectHandles()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (!Initialized)
            {
                // Projects requested before initialization
                _triggerChangeAfterInitialize = true;

                return Array.Empty<ProjectSnapshotHandleProxy>();
            }

            return _projectHandles;
        }

        public void InitializeProjects()
        {
            _foregroundDispatcher.AssertForegroundThread();

            // We wire the changed event up early because any changed events that fire will ensure we have the most
            // up-to-date state.
            var snapshotManagerProxy = _proxyAccessor.GetProjectSnapshotManagerProxy();
            snapshotManagerProxy.Changed += HostProxyStateManager_Changed;

            TestInitializationTask = _joinableTaskFactory.RunAsync(async () =>
            {
                var state = await snapshotManagerProxy.GetStateAsync(CancellationToken.None);

                await _joinableTaskFactory.SwitchToMainThreadAsync();

                if (Initialized)
                {
                    // State was initialized from the changed event firing, that state will be more up-to-date than this.
                    return;
                }

                UpdateProjects(state);

                if (_triggerChangeAfterInitialize)
                {
                    // Someone requested the set of projects prior to us being initialized. Let listeners know that projects
                    // have been added. This way we don't have to block on the foreground thread for initialization; however,
                    // this also assumes that anyone listening re-computes their state when we trigger project changes
                    // (a correct assumption).

                    for (var i = 0; i < _projectHandles.Count; i++)
                    {
                        var args = new ProjectProxyChangeEventArgs(_projectHandles[i].FilePath, ProjectProxyChangeKind.ProjectAdded);
                        OnChanged(args);
                    }
                }
            });
        }

        // Internal for testing
        internal async void HostProxyStateManager_Changed(object sender, ProjectManagerProxyChangeEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (_foregroundDispatcher.IsForegroundThread)
            {
                UpdateProjectsAndTriggerChangeForeground();
            }
            else
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                UpdateProjectsAndTriggerChangeForeground();
            }

            void UpdateProjectsAndTriggerChangeForeground()
            {
                _foregroundDispatcher.AssertForegroundThread();

                UpdateProjects(args.State);

                // We will have already triggered change events to ensure listeners are up-to-date.
                _triggerChangeAfterInitialize = false;

                OnChanged(args.Change);
            }
        }

        // Internal for testing
        internal void UpdateProjects(ProjectSnapshotManagerProxyState state)
        {
            _foregroundDispatcher.AssertForegroundThread();

            _projectHandles = state.ProjectHandles;
        }

        private void OnChanged(ProjectProxyChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            Changed?.Invoke(this, args);
        }
    }
}
