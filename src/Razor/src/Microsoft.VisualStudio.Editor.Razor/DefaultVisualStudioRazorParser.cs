// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.Text;
using static Microsoft.VisualStudio.Editor.Razor.BackgroundParser;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;
using Timer = System.Threading.Timer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioRazorParser : VisualStudioRazorParser, IDisposable
    {
        public override event EventHandler<DocumentStructureChangedEventArgs> DocumentStructureChanged;

        // Internal for testing.
        internal TimeSpan IdleDelay = TimeSpan.FromSeconds(3);
        internal Timer _idleTimer;
        internal BackgroundParser _parser;
        internal ChangeReference _latestChangeReference;
        internal RazorSyntaxTreePartialParser _partialParser;

        private readonly object IdleLock = new object();
        private readonly object UpdateStateLock = new object();
        private readonly VisualStudioCompletionBroker _completionBroker;
        private readonly VisualStudioDocumentTracker _documentTracker;
        private readonly ForegroundDispatcher _dispatcher;
        private readonly ProjectSnapshotProjectEngineFactory _projectEngineFactory;
        private readonly ErrorReporter _errorReporter;
        private readonly List<SyntaxTreeRequest> _syntaxTreeRequests;
        private RazorProjectEngine _projectEngine;
        private RazorCodeDocument _codeDocument;
        private ITextSnapshot _snapshot;
        private bool _disposed;
        private ITextSnapshot _latestParsedSnapshot;

        // For testing only
        internal DefaultVisualStudioRazorParser(RazorCodeDocument codeDocument)
        {
            _codeDocument = codeDocument;
        }

        public DefaultVisualStudioRazorParser(
            ForegroundDispatcher dispatcher,
            VisualStudioDocumentTracker documentTracker,
            ProjectSnapshotProjectEngineFactory projectEngineFactory,
            ErrorReporter errorReporter,
            VisualStudioCompletionBroker completionBroker)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            if (projectEngineFactory == null)
            {
                throw new ArgumentNullException(nameof(projectEngineFactory));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            _dispatcher = dispatcher;
            _projectEngineFactory = projectEngineFactory;
            _errorReporter = errorReporter;
            _completionBroker = completionBroker;
            _documentTracker = documentTracker;
            _syntaxTreeRequests = new List<SyntaxTreeRequest>();

            _documentTracker.ContextChanged += DocumentTracker_ContextChanged;
        }

        public override string FilePath => _documentTracker.FilePath;

        public override RazorCodeDocument CodeDocument => _codeDocument;

        public override ITextSnapshot Snapshot => _snapshot;

        public override ITextBuffer TextBuffer => _documentTracker.TextBuffer;

        public override bool HasPendingChanges => _latestChangeReference != null;

        // Used in unit tests to ensure we can be notified when idle starts.
        internal ManualResetEventSlim NotifyForegroundIdleStart { get; set; }

        // Used in unit tests to ensure we can block background idle work.
        internal ManualResetEventSlim BlockBackgroundIdleWork { get; set; }

        public override Task<RazorSyntaxTree> GetLatestSyntaxTreeAsync(ITextSnapshot atOrNewerSnapshot, CancellationToken cancellationToken = default)
        {
            if (atOrNewerSnapshot == null)
            {
                throw new ArgumentNullException(nameof(atOrNewerSnapshot));
            }

            lock (UpdateStateLock)
            {
                if (_disposed ||
                    (_latestParsedSnapshot != null && atOrNewerSnapshot.Version.VersionNumber <= _latestParsedSnapshot.Version.VersionNumber))
                {
                    return Task.FromResult(CodeDocument?.GetSyntaxTree());
                }

                SyntaxTreeRequest request = null;
                for (var i = _syntaxTreeRequests.Count - 1; i >= 0; i--)
                {
                    if (_syntaxTreeRequests[i].Snapshot == atOrNewerSnapshot)
                    {
                        request = _syntaxTreeRequests[i];
                        break;
                    }
                }

                if (request == null)
                {
                    request = new SyntaxTreeRequest(atOrNewerSnapshot, cancellationToken);
                    _syntaxTreeRequests.Add(request);
                }

                return request.Task;
            }
        }

        public override void QueueReparse()
        {
            // Can be called from any thread

            if (_dispatcher.IsForegroundThread)
            {
                ReparseOnForeground(null);
            }
            else
            {
                Task.Factory.StartNew(ReparseOnForeground, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        public void Dispose()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            _documentTracker.ContextChanged -= DocumentTracker_ContextChanged;

            StopIdleTimer();

            lock (UpdateStateLock)
            {
                _disposed = true;
                foreach (var request in _syntaxTreeRequests)
                {
                    request.Cancel();
                }
            }
        }

        // Internal for testing
        internal void DocumentTracker_ContextChanged(object sender, ContextChangeEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (!TryReinitializeParser())
            {
                return;
            }

            // We have a new parser, force a reparse to generate new document information. Note that this
            // only blocks until the reparse change has been queued.
            QueueReparse();
        }

        // Internal for testing
        internal bool TryReinitializeParser()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            if (!_documentTracker.IsSupportedProject)
            {
                // Tracker is either starting up, tearing down or wrongfully instantiated.
                // Either way, the tracker can't act on its associated project, neither can we.
                return false;
            }

            StartParser();

            return true;
        }

        // Internal for testing
        internal void StartParser()
        {
            _dispatcher.AssertForegroundThread();

            // Make sure any tests use the real thing or a good mock. These tests can cause failures
            // that are hard to understand when this throws.
            Debug.Assert(_documentTracker.IsSupportedProject);
            Debug.Assert(_documentTracker.ProjectSnapshot != null);

            _projectEngine = _projectEngineFactory.Create(_documentTracker.ProjectSnapshot, ConfigureProjectEngine);

            Debug.Assert(_projectEngine != null);
            Debug.Assert(_projectEngine.Engine != null);
            Debug.Assert(_projectEngine.FileSystem != null);

            // We might not have a document snapshot in the case of an ephemeral project.
            // If we can't be sure, then just assume it's an MVC view since that's likely anyway.
            var fileKind = _documentTracker.ProjectSnapshot?.GetDocument(_documentTracker.FilePath)?.FileKind ?? FileKinds.Legacy;

            var projectDirectory = Path.GetDirectoryName(_documentTracker.ProjectPath);
            _parser = new BackgroundParser(_projectEngine, FilePath, projectDirectory, fileKind);
            _parser.ResultsReady += OnResultsReady;
            _parser.Start();

            TextBuffer.Changed += TextBuffer_OnChanged;
        }

        // Internal for testing
        internal void StopParser()
        {
            _dispatcher.AssertForegroundThread();

            if (_parser != null)
            {
                // Detatch from the text buffer until we have a new parser to handle changes.
                TextBuffer.Changed -= TextBuffer_OnChanged;

                _parser.ResultsReady -= OnResultsReady;
                _parser.Dispose();
                _parser = null;
            }
        }

        // Internal for testing
        internal void StartIdleTimer()
        {
            _dispatcher.AssertForegroundThread();

            lock (IdleLock)
            {
                if (_idleTimer == null)
                {
                    // Timer will fire after a fixed delay, but only once.
                    _idleTimer = NonCapturingTimer.Create(state => ((DefaultVisualStudioRazorParser)state).Timer_Tick(), this, IdleDelay, Timeout.InfiniteTimeSpan);
                }
            }
        }

        // Internal for testing
        internal void StopIdleTimer()
        {
            // Can be called from any thread.

            lock (IdleLock)
            {
                if (_idleTimer != null)
                {
                    _idleTimer.Dispose();
                    _idleTimer = null;
                }
            }
        }

        private void TextBuffer_OnChanged(object sender, TextContentChangedEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (args.Changes.Count > 0)
            {
                // Idle timers are used to track provisional changes. Provisional changes only last for a single text change. After that normal
                // partial parsing rules apply (stop the timer).
                StopIdleTimer();
            }

            var snapshot = args.After;
            if (!args.TextChangeOccurred(out var changeInformation))
            {
                // Ensure we get a parse for latest snapshot.
                QueueChange(null, snapshot);
                return;
            }

            var change = new SourceChange(changeInformation.firstChange.OldPosition, changeInformation.oldText.Length, changeInformation.newText);
            var result = PartialParseResultInternal.Rejected;
            RazorSyntaxTree partialParseSyntaxTree = null;
            using (_parser.SynchronizeMainThreadState())
            {
                // Check if we can partial-parse
                if (_partialParser != null && _parser.IsIdle)
                {
                    (result, partialParseSyntaxTree) = _partialParser.Parse(change);
                }
            }

            // If partial parsing failed or there were outstanding parser tasks, start a full reparse
            if ((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected)
            {
                QueueChange(change, snapshot);
            }
            else
            {
                TryUpdateLatestParsedSyntaxTreeSnapshot(partialParseSyntaxTree, snapshot);
            }

            if ((result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional)
            {
                StartIdleTimer();
            }
        }

        // Internal for testing
        internal void OnIdle(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            OnNotifyForegroundIdle();

            foreach (var textView in _documentTracker.TextViews)
            {
                if (_completionBroker.IsCompletionActive(textView))
                {
                    // Completion list is still active, need to re-start timer.
                    StartIdleTimer();
                    return;
                }
            }

            QueueReparse();
        }

        // Internal for testing
        internal void ReparseOnForeground(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            var snapshot = TextBuffer.CurrentSnapshot;
            QueueChange(null, snapshot);
        }

        private void QueueChange(SourceChange change, ITextSnapshot snapshot)
        {
            _dispatcher.AssertForegroundThread();

            _latestChangeReference = _parser.QueueChange(change, snapshot);
        }

        private void OnNotifyForegroundIdle()
        {
            if (NotifyForegroundIdleStart != null)
            {
                NotifyForegroundIdleStart.Set();
            }
        }

        private void OnStartingBackgroundIdleWork()
        {
            if (BlockBackgroundIdleWork != null)
            {
                BlockBackgroundIdleWork.Wait();
            }
        }

        private void Timer_Tick()
        {
            try
            {
                _dispatcher.AssertBackgroundThread();

                OnStartingBackgroundIdleWork();

                StopIdleTimer();

                // We need to get back to the UI thread to properly check if a completion is active.
                Task.Factory.StartNew(OnIdle, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                Task.Factory.StartNew(() => _errorReporter.ReportError(ex), CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        // Internal for testing
        internal void OnResultsReady(object sender, BackgroundParserResultsReadyEventArgs args)
        {
            _dispatcher.AssertBackgroundThread();

            UpdateParserState(args.CodeDocument, args.ChangeReference.Snapshot);

            // Jump back to UI thread to notify structure changes.
            Task.Factory.StartNew(OnDocumentStructureChanged, args, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
        }

        // Internal for testing
        internal void OnDocumentStructureChanged(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_disposed)
            {
                return;
            }

            var backgroundParserArgs = (BackgroundParserResultsReadyEventArgs)state;
            if (_latestChangeReference == null || // extra hardening
                _latestChangeReference != backgroundParserArgs.ChangeReference)
            {
                // In the middle of parsing a newer change or about to parse a newer change.
                return;
            }

            if (backgroundParserArgs.ChangeReference.Snapshot != TextBuffer.CurrentSnapshot)
            {
                // Changes have impacted the snapshot after our we recorded our last change reference.
                // This can happen for a multitude of reasons, usually because of a user auto-completing
                // C# statements (causes multiple edits in quick succession). This ensures that our latest
                // parse corresponds to the current snapshot.
                QueueReparse();
                return;
            }

            _latestChangeReference = null;

            var documentStructureChangedArgs = new DocumentStructureChangedEventArgs(
                backgroundParserArgs.ChangeReference.Change,
                backgroundParserArgs.ChangeReference.Snapshot,
                backgroundParserArgs.CodeDocument);
            DocumentStructureChanged?.Invoke(this, documentStructureChangedArgs);
        }

        private void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
            var projectSnapshot = _documentTracker.ProjectSnapshot;
            if (projectSnapshot != null)
            {
                builder.SetCSharpLanguageVersion(projectSnapshot.CSharpLanguageVersion);
            }
            builder.SetRootNamespace(projectSnapshot?.RootNamespace);
            builder.Features.Add(new VisualStudioParserOptionsFeature(_documentTracker.EditorSettings));
            builder.Features.Add(new VisualStudioTagHelperFeature(_documentTracker.TagHelpers));
        }

        private void UpdateParserState(RazorCodeDocument codeDocument, ITextSnapshot snapshot)
        {
            lock (UpdateStateLock)
            {
                if (_snapshot != null && snapshot.Version.VersionNumber < _snapshot.Version.VersionNumber)
                {
                    // Changes flowed out of order due to the slight race condition at the beginning of this method. Our current
                    // CodeDocument and Snapshot are newer then the ones that made it into the lock.
                    return;
                }

                _codeDocument = codeDocument;
                _snapshot = snapshot;
                _partialParser = new RazorSyntaxTreePartialParser(_codeDocument.GetSyntaxTree());
                TryUpdateLatestParsedSyntaxTreeSnapshot(_codeDocument.GetSyntaxTree(), _snapshot);
            }
        }

        private void TryUpdateLatestParsedSyntaxTreeSnapshot(RazorSyntaxTree syntaxTree, ITextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            lock (UpdateStateLock)
            {
                if (_latestParsedSnapshot == null ||
                    _latestParsedSnapshot.Version.VersionNumber < snapshot.Version.VersionNumber)
                {
                    _latestParsedSnapshot = snapshot;

                    CompleteSyntaxTreeRequestsForSnapshot(syntaxTree, snapshot);
                }
            }
        }

        private void CompleteSyntaxTreeRequestsForSnapshot(RazorSyntaxTree syntaxTree, ITextSnapshot snapshot)
        {
            lock (UpdateStateLock)
            {
                if (_syntaxTreeRequests.Count == 0)
                {
                    return;
                }

                var matchingRequests = new List<SyntaxTreeRequest>();
                for (var i = _syntaxTreeRequests.Count - 1; i >= 0; i--)
                {
                    var request = _syntaxTreeRequests[i];
                    if (request.Snapshot.Version.VersionNumber <= snapshot.Version.VersionNumber)
                    {
                        // This change was for a newer snapshot, we can complete the TCS.
                        matchingRequests.Add(request);
                        _syntaxTreeRequests.RemoveAt(i);
                    }
                }

                // The matching requests are in reverse order so we need to invoke them from the back to front.
                for (var i = matchingRequests.Count - 1; i >= 0; i--)
                {
                    // At this point it's possible these requests have been cancelled, if that's the case Complete noops.
                    matchingRequests[i].Complete(syntaxTree);
                }
            }
        }

        private class VisualStudioParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            private readonly EditorSettings _settings;

            public VisualStudioParserOptionsFeature(EditorSettings settings)
            {
                _settings = settings;
            }

            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.IndentSize = _settings.IndentSize;
                options.IndentWithTabs = _settings.IndentWithTabs;
            }
        }

        private class VisualStudioTagHelperFeature : ITagHelperFeature
        {
            private readonly IReadOnlyList<TagHelperDescriptor> _tagHelpers;

            public VisualStudioTagHelperFeature(IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                _tagHelpers = tagHelpers;
            }

            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
            {
                return _tagHelpers;
            }
        }

        // Internal for testing
        internal class SyntaxTreeRequest
        {
            private readonly object CompletionLock = new object();
            private readonly TaskCompletionSource<RazorSyntaxTree> _taskCompletionSource;
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;
            private bool _done;

            public SyntaxTreeRequest(ITextSnapshot snapshot, CancellationToken cancellationToken)
            {
                if (snapshot == null)
                {
                    throw new ArgumentNullException(nameof(snapshot));
                }

                Snapshot = snapshot;
                _taskCompletionSource = new TaskCompletionSource<RazorSyntaxTree>(TaskCreationOptions.RunContinuationsAsynchronously);
                _cancellationTokenRegistration = cancellationToken.Register(Cancel);
                Task = _taskCompletionSource.Task;

                if (cancellationToken.IsCancellationRequested)
                {
                    // If the token was already cancelled we need to bail.
                    Cancel();
                }
            }

            public ITextSnapshot Snapshot { get; }

            public Task<RazorSyntaxTree> Task { get; }

            public void Complete(RazorSyntaxTree syntaxTree)
            {
                if (syntaxTree == null)
                {
                    throw new ArgumentNullException(nameof(syntaxTree));
                }

                lock (CompletionLock)
                {
                    if (_done)
                    {
                        // Request was already cancelled.
                        return;
                    }

                    _done = true;
                    _cancellationTokenRegistration.Dispose();
                    _taskCompletionSource.SetResult(syntaxTree);
                }
            }

            public void Cancel()
            {
                lock (CompletionLock)
                {
                    if (_done)
                    {
                        return;
                    }

                    _taskCompletionSource.TrySetCanceled();
                    _cancellationTokenRegistration.Dispose();
                    _done = true;
                }
            }
        }
    }
}
