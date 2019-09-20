// Copyright (c) .NET Fo    undation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
{
    public class OmniSharpWorkspaceProjectStateChangeDetector : IOmniSharpProjectSnapshotManagerChangeTrigger
    {
        public OmniSharpWorkspaceProjectStateChangeDetector(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpProjectWorkspaceStateGenerator workspaceStateGenerator)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspaceStateGenerator == null)
            {
                throw new ArgumentNullException(nameof(workspaceStateGenerator));
            }

            InternalWorkspaceProjectStateChangeDetector = new ForegroundWorkspaceProjectStateChangeDetector(
                foregroundDispatcher.InternalDispatcher,
                workspaceStateGenerator.InternalWorkspaceStateGenerator);
        }

        internal WorkspaceProjectStateChangeDetector InternalWorkspaceProjectStateChangeDetector { get; }

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager)
        {
            InternalWorkspaceProjectStateChangeDetector.Initialize(projectManager.InternalProjectSnapshotManager);
        }

        private class ForegroundWorkspaceProjectStateChangeDetector : WorkspaceProjectStateChangeDetector
        {
            private readonly ForegroundDispatcher _foregroundDispatcher;

            public ForegroundWorkspaceProjectStateChangeDetector(
                ForegroundDispatcher foregroundDispatcher,
                ProjectWorkspaceStateGenerator workspaceStateGenerator) : base(workspaceStateGenerator)
            {
                if (foregroundDispatcher == null)
                {
                    throw new ArgumentNullException(nameof(foregroundDispatcher));
                }

                _foregroundDispatcher = foregroundDispatcher;
            }

            // We override the InitializeSolution in order to enforce calls to this to be on the foreground thread.
            // OmniSharp currently has an issue where they update the Solution on multiple different threads resulting
            // in change events dispatching through the Workspace on multiple different threads. This normalizes
            // that abnormality.
            protected override async void InitializeSolution(Solution solution)
            {
                if (_foregroundDispatcher.IsForegroundThread)
                {
                    base.InitializeSolution(solution);
                    return;
                }

                await Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            base.InitializeSolution(solution);
                        }
                        catch (Exception ex)
                        {
                            Debug.Fail("Unexpected error when initializing solution: " + ex);
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler);
            }

            // We override Workspace_WorkspaceChanged in order to enforce calls to this to be on the foreground thread.
            // OmniSharp currently has an issue where they update the Solution on multiple different threads resulting
            // in change events dispatching through the Workspace on multiple different threads. This normalizes
            // that abnormality.
            internal override async void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                if (_foregroundDispatcher.IsForegroundThread)
                {
                    base.Workspace_WorkspaceChanged(sender, args);
                    return;
                }
                await Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            base.Workspace_WorkspaceChanged(sender, args);
                        }
                        catch (Exception ex)
                        {
                            Debug.Fail("Unexpected error when handling a workspace changed event: " + ex);
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler);
            }
        }
    }
}
