// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor
{
    [Shared]
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class BackgroundDocumentGenerator : ProjectSnapshotChangeTrigger
    {
        // Internal for testing
        internal readonly Dictionary<DocumentKey, (ProjectSnapshot project, DocumentSnapshot document)> _work;

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorDynamicFileInfoProvider _infoProvider;
        private ProjectSnapshotManagerBase _projectManager;
        private Timer _timer;

        [ImportingConstructor]
        public BackgroundDocumentGenerator(ForegroundDispatcher foregroundDispatcher, RazorDynamicFileInfoProvider infoProvider)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (infoProvider == null)
            {
                throw new ArgumentNullException(nameof(infoProvider));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _infoProvider = infoProvider;

            _work = new Dictionary<DocumentKey, (ProjectSnapshot project, DocumentSnapshot document)>();
        }

        public bool HasPendingNotifications
        {
            get
            {
                lock (_work)
                {
                    return _work.Count > 0;
                }
            }
        }

        // Used in unit tests to control the timer delay.
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

        public bool IsScheduledOrRunning => _timer != null;

        // Used in unit tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockBackgroundWorkStart { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkStarting { get; set; }

        // Used in unit tests to ensure we can know when background has captured its current workload.
        public ManualResetEventSlim NotifyBackgroundCapturedWorkload { get; set; }

        // Used in unit tests to ensure we can control when background work completes.
        public ManualResetEventSlim BlockBackgroundWorkCompleting { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkCompleted { get; set; }

        // Used in unit tests to ensure we can know when errors are reported
        public ManualResetEventSlim NotifyErrorBeingReported { get; set; }

        private void OnStartingBackgroundWork()
        {
            if (BlockBackgroundWorkStart != null)
            {
                BlockBackgroundWorkStart.Wait();
                BlockBackgroundWorkStart.Reset();
            }

            if (NotifyBackgroundWorkStarting != null)
            {
                NotifyBackgroundWorkStarting.Set();
            }
        }

        private void OnCompletingBackgroundWork()
        {
            if (BlockBackgroundWorkCompleting != null)
            {
                BlockBackgroundWorkCompleting.Wait();
                BlockBackgroundWorkCompleting.Reset();
            }
        }

        private void OnCompletedBackgroundWork()
        {
            if (NotifyBackgroundWorkCompleted != null)
            {
                NotifyBackgroundWorkCompleted.Set();
            }
        }

        private void OnBackgroundCapturedWorkload()
        {
            if (NotifyBackgroundCapturedWorkload != null)
            {
                NotifyBackgroundCapturedWorkload.Set();
            }
        }

        private void OnErrorBeingReported()
        {
            if (NotifyErrorBeingReported != null)
            {
                NotifyErrorBeingReported.Set();
            }
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
        }

        protected virtual async Task ProcessDocument(ProjectSnapshot project, DocumentSnapshot document)
        {
            await document.GetGeneratedOutputAsync().ConfigureAwait(false);
            var container = new DefaultDynamicDocumentContainer(document);
            _infoProvider.UpdateFileInfo(project.FilePath, container);
        }

        public void Enqueue(ProjectSnapshot project, DocumentSnapshot document)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_work)
            {
                if (_projectManager.IsDocumentOpen(document.FilePath))
                {
                    _infoProvider.SuppressDocument(project.FilePath, document.FilePath);
                    return;
                }

                // We only want to store the last 'seen' version of any given document. That way when we pick one to process
                // it's always the best version to use.
                _work[new DocumentKey(project.FilePath, document.FilePath)] = (project, document);

                StartWorker();
            }
        }

        protected virtual void StartWorker()
        {
            // Access to the timer is protected by the lock in Enqueue and in Timer_Tick
            if (_timer == null)
            {

                // Timer will fire after a fixed delay, but only once.
                _timer = NonCapturingTimer.Create(state => ((BackgroundDocumentGenerator)state).Timer_Tick(), this, Delay, Timeout.InfiniteTimeSpan);
            }
        }

        private void Timer_Tick()
        {
            _ = TimerTick();
        }

        private async Task TimerTick()
        {
            try
            {
                _foregroundDispatcher.AssertBackgroundThread();

                // Timer is stopped.
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                OnStartingBackgroundWork();

                KeyValuePair<DocumentKey, (ProjectSnapshot project, DocumentSnapshot document)>[] work;
                lock (_work)
                {
                    work = _work.ToArray();
                    _work.Clear();
                }

                OnBackgroundCapturedWorkload();

                for (var i = 0; i < work.Length; i++)
                {
                    var (project, document) = work[i].Value;
                    try
                    {
                        await ProcessDocument(project, document).ConfigureAwait(false);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Ignore UnauthorizedAccessException. These can occur when a file gets its permissions changed as we're processing it.
                    }
                    catch (IOException)
                    {
                        // Ignore IOException. These can occur when a file was in the middle of being renamed and it dissapears as we're processing it.
                        // This is a common case and does not warrant an activity log entry.
                    }
                    catch (Exception ex)
                    {
                        ReportError(project, ex);
                    }
                }

                OnCompletingBackgroundWork();

                lock (_work)
                {
                    // Resetting the timer allows another batch of work to start.
                    _timer.Dispose();
                    _timer = null;

                    // If more work came in while we were running start the worker again.
                    if (_work.Count > 0)
                    {
                        StartWorker();
                    }
                }

                OnCompletedBackgroundWork();
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                await Task.Factory.StartNew(
                    (p) => ((ProjectSnapshotManagerBase)p).ReportError(ex),
                    _projectManager,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
            }
        }

        private void ReportError(ProjectSnapshot project, Exception ex)
        {
            OnErrorBeingReported();

            GC.KeepAlive(Task.Factory.StartNew(
                (p) => ((ProjectSnapshotManagerBase)p).ReportError(ex, project),
                _projectManager,
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler));
        }

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case ProjectChangeKind.ProjectAdded:
                    {
                        var projectSnapshot = e.Newer;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            Enqueue(projectSnapshot, projectSnapshot.GetDocument(documentFilePath));
                        }

                        break;
                    }
                case ProjectChangeKind.ProjectChanged:
                    {
                        var projectSnapshot = e.Newer;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            Enqueue(projectSnapshot, projectSnapshot.GetDocument(documentFilePath));
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentAdded:
                case ProjectChangeKind.DocumentChanged:
                    {
                        var project = e.Newer;
                        var document = project.GetDocument(e.DocumentFilePath);

                        Enqueue(project, document);
                        foreach (var relatedDocument in project.GetRelatedDocuments(document))
                        {
                            Enqueue(project, relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentRemoved:
                    {
                        // For removals use the old snapshot to find the removed document, so we can figure out 
                        // what the imports were in the new snapshot.
                        var document = e.Older.GetDocument(e.DocumentFilePath);

                        foreach (var relatedDocument in e.Newer.GetRelatedDocuments(document))
                        {
                            Enqueue(e.Newer, relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.ProjectRemoved:
                    {
                        // ignore
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unknown ProjectChangeKind {e.Kind}");
            }
        }
    }
}