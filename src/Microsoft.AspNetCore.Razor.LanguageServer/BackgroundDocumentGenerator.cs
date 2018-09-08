// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class BackgroundDocumentGenerator : ProjectSnapshotChangeTrigger
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly ILanguageServer _router;
        private readonly VSCodeLogger _logger;
        private readonly Dictionary<string, DocumentSnapshot> _work;
        private ProjectSnapshotManagerBase _projectManager;
        private Timer _timer;

        public BackgroundDocumentGenerator(
            ForegroundDispatcher foregroundDispatcher,
            DocumentVersionCache documentVersionCache,
            ILanguageServer router,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _documentVersionCache = documentVersionCache;
            _router = router;
            _logger = logger;
            _work = new Dictionary<string, DocumentSnapshot>(StringComparer.Ordinal);
        }

        // For testing only
        protected BackgroundDocumentGenerator(
            ForegroundDispatcher foregroundDispatcher,
            VSCodeLogger logger)
        {
            _foregroundDispatcher = foregroundDispatcher;
            _logger = logger;
            _work = new Dictionary<string, DocumentSnapshot>(StringComparer.Ordinal);
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

        private async void Timer_Tick(object state)
        {
            try
            {
                _foregroundDispatcher.AssertBackgroundThread();

                // Timer is stopped.
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

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
                        _logger.Log("Error when processing document: " + document.FilePath);
                    }
                }

                OnCompletingBackgroundWork();

                await Task.Factory.StartNew(
                    () => ReportUnsynchronizableContent(work),
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
            }
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.ProjectAdded:
                    {
                        var projectSnapshot = _projectManager.GetLoadedProject(args.ProjectFilePath);
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            if (_projectManager.IsDocumentOpen(documentFilePath))
                            {
                                var document = projectSnapshot.GetDocument(documentFilePath);
                                Enqueue(document);
                            }
                        }

                        break;
                    }
                case ProjectChangeKind.ProjectChanged:
                    {
                        var projectSnapshot = _projectManager.GetLoadedProject(args.ProjectFilePath);
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            if (_projectManager.IsDocumentOpen(documentFilePath))
                            {
                                var document = projectSnapshot.GetDocument(documentFilePath);
                                Enqueue(document);
                            }
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentAdded:
                    {
                        var project = _projectManager.GetLoadedProject(args.ProjectFilePath);
                        if (_projectManager.IsDocumentOpen(args.DocumentFilePath))
                        {
                            var document = project.GetDocument(args.DocumentFilePath);
                            Enqueue(document);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentChanged:
                    {
                        var project = _projectManager.GetLoadedProject(args.ProjectFilePath);
                        if (_projectManager.IsDocumentOpen(args.DocumentFilePath))
                        {
                            var document = project.GetDocument(args.DocumentFilePath);
                            Enqueue(document);
                        }

                        break;
                    }

                case ProjectChangeKind.ProjectRemoved:
                case ProjectChangeKind.DocumentRemoved:
                    {
                        // ignore
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unknown ProjectChangeKind {args.Kind}");
            }
        }

        // Internal virtual for testing
        internal virtual void ReportUnsynchronizableContent(KeyValuePair<string, DocumentSnapshot>[] work)
        {
            _foregroundDispatcher.AssertForegroundThread();

            // This method deals with reporting unsynchronized content. At this point we've force evaluation of each document
            // in the work queue; however, some documents may be identical versions of the last synchronized document if
            // one's content does not differ. In this case, the output of the two generated documents is the same but we still
            // need to let the client know that we've processed its latest text change. This allows the client to understand
            // when it's operating on out-of-date output.

            for (var i = 0; i < work.Length; i++)
            {
                var document = work[i].Value;

                if (!(document is DefaultDocumentSnapshot defaultDocument))
                {
                    continue;
                }

                if (!_documentVersionCache.TryGetDocumentVersion(document, out var syncVersion))
                {
                    // Document is no longer important.
                    continue;
                }

                var latestSynchronizedDocument = defaultDocument.State.HostDocument.GeneratedCodeContainer.LatestDocument;
                if (latestSynchronizedDocument == null || 
                    latestSynchronizedDocument == document)
                {
                    // Already up-to-date
                    continue;
                }

                if (IdenticalOutputAfterParse(document, latestSynchronizedDocument, syncVersion))
                {
                    // Documents are identical but we didn't synchronize them because they didn't need to be re-evaluated.

                    var request = new UpdateCSharpBufferRequest()
                    {
                        HostDocumentFilePath = document.FilePath,
                        Changes = Array.Empty<TextChange>(),
                        HostDocumentVersion = syncVersion
                    };

                    _router.Client.SendRequest("updateCSharpBuffer", request);
                }
            }
        }

        private bool IdenticalOutputAfterParse(DocumentSnapshot document, DocumentSnapshot latestSynchronizedDocument, long syncVersion)
        {
            return latestSynchronizedDocument.TryGetTextVersion(out var latestSourceVersion) &&
                document.TryGetTextVersion(out var documentSourceVersion) &&
                _documentVersionCache.TryGetDocumentVersion(latestSynchronizedDocument, out var lastSynchronizedVersion) &&
                syncVersion > lastSynchronizedVersion &&
                latestSourceVersion == documentSourceVersion;
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
