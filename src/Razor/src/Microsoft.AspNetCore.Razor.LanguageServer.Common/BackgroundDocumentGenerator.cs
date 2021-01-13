// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal class BackgroundDocumentGenerator : ProjectSnapshotChangeTrigger
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly IEnumerable<DocumentProcessedListener> _documentProcessedListeners;
        private readonly Dictionary<string, DocumentSnapshot> _work;
        private ProjectSnapshotManagerBase _projectManager;
        private Timer _timer;

        public BackgroundDocumentGenerator(
            ForegroundDispatcher foregroundDispatcher,
            IEnumerable<DocumentProcessedListener> documentProcessedListeners)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentProcessedListeners == null)
            {
                throw new ArgumentNullException(nameof(documentProcessedListeners));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentProcessedListeners = documentProcessedListeners;
            _work = new Dictionary<string, DocumentSnapshot>(StringComparer.Ordinal);
        }

        // For testing only
        protected BackgroundDocumentGenerator(
            ForegroundDispatcher foregroundDispatcher)
        {
            _foregroundDispatcher = foregroundDispatcher;
            _work = new Dictionary<string, DocumentSnapshot>(StringComparer.Ordinal);
            _documentProcessedListeners = Enumerable.Empty<DocumentProcessedListener>();
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

        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        public bool IsScheduledOrRunning => _timer != null;

        // Used in tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockBackgroundWorkStart { get; set; }

        // Used in tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkStarting { get; set; }

        // Used in unit tests to ensure we can know when background has captured its current workload.
        public ManualResetEventSlim NotifyBackgroundCapturedWorkload { get; set; }

        // Used in tests to ensure we can control when background work completes.
        public ManualResetEventSlim BlockBackgroundWorkCompleting { get; set; }

        // Used in tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkCompleted { get; set; }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;

            _projectManager.Changed += ProjectSnapshotManager_Changed;

            foreach (var documentProcessedListener in _documentProcessedListeners)
            {
                documentProcessedListener.Initialize(_projectManager);
            }
        }

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

        // Internal for testing
        internal void Enqueue(DocumentSnapshot document)
        {
            _foregroundDispatcher.AssertForegroundThread();

            lock (_work)
            {
                // We only want to store the last 'seen' version of any given document. That way when we pick one to process
                // it's always the best version to use.
                _work[document.FilePath] = document;

                StartWorker();
            }
        }

        private void StartWorker()
        {
            // Access to the timer is protected by the lock in Synchronize and in Timer_Tick
            if (_timer == null)
            {
                // Timer will fire after a fixed delay, but only once.
                _timer = new Timer(Timer_Tick, null, Delay, Timeout.InfiniteTimeSpan);
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Timer_Tick(object state)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                _foregroundDispatcher.AssertBackgroundThread();

                OnStartingBackgroundWork();

                KeyValuePair<string, DocumentSnapshot>[] work;
                lock (_work)
                {
                    work = _work.ToArray();
                    _work.Clear();
                }

                OnBackgroundCapturedWorkload();

                for (var i = 0; i < work.Length; i++)
                {
                    var document = work[i].Value;
                    try
                    {
                        await document.GetGeneratedOutputAsync();
                    }
                    catch (Exception ex)
                    {
                        ReportError(ex);
                    }
                }

                OnCompletingBackgroundWork();

                await Task.Factory.StartNew(
                    () =>
                    {
                        NotifyDocumentsProcessed(work);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler);

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
                ReportError(ex);

                _timer?.Dispose();
                _timer = null;
            }
        }

        private void NotifyDocumentsProcessed(KeyValuePair<string, DocumentSnapshot>[] work)
        {
            for (var i = 0; i < work.Length; i++)
            {
                foreach (var documentProcessedTrigger in _documentProcessedListeners)
                {
                    var document = work[i].Value;
                    documentProcessedTrigger.DocumentProcessed(document);
                }
            }
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.ProjectAdded:
                    {
                        var projectSnapshot = args.Newer;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            var document = projectSnapshot.GetDocument(documentFilePath);
                            Enqueue(document);
                        }

                        break;
                    }
                case ProjectChangeKind.ProjectChanged:
                    {
                        var projectSnapshot = args.Newer;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            var document = projectSnapshot.GetDocument(documentFilePath);
                            Enqueue(document);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentAdded:
                    {
                        var projectSnapshot = args.Newer;
                        var document = projectSnapshot.GetDocument(args.DocumentFilePath);
                        Enqueue(document);

                        foreach (var relatedDocument in projectSnapshot.GetRelatedDocuments(document))
                        {
                            Enqueue(relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentChanged:
                    {
                        var projectSnapshot = args.Newer;
                        var document = projectSnapshot.GetDocument(args.DocumentFilePath);
                        Enqueue(document);

                        foreach (var relatedDocument in projectSnapshot.GetRelatedDocuments(document))
                        {
                            Enqueue(relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentRemoved:
                    {
                        var olderProject = args.Older;
                        var document = olderProject.GetDocument(args.DocumentFilePath);

                        foreach (var relatedDocument in olderProject.GetRelatedDocuments(document))
                        {
                            var newerRelatedDocument = args.Newer.GetDocument(relatedDocument.FilePath);
                            Enqueue(newerRelatedDocument);
                        }
                        break;
                    }
                case ProjectChangeKind.ProjectRemoved:
                    {
                        // ignore
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unknown ProjectChangeKind {args.Kind}");
            }
        }

        private void ReportError(Exception ex)
        {
            GC.KeepAlive(Task.Factory.StartNew(
                () => _projectManager.ReportError(ex),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler));
        }
    }
}
