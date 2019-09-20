// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using OmniSharp;
using OmniSharp.Models;
using OmniSharp.Models.UpdateBuffer;
using OmniSharp.Roslyn;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(IRazorDocumentChangeListener))]
    [Export(typeof(IRazorDocumentOutputChangeListener))]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    internal class ComponentRefreshTrigger : IRazorDocumentChangeListener, IRazorDocumentOutputChangeListener, IOmniSharpProjectSnapshotManagerChangeTrigger
    {
        // Internal for testing
        internal readonly Dictionary<string, Task> _deferredRefreshTasks;

        private const string CompileItemType = "Compile";

        private readonly object _refreshLock = new object();
        private readonly Dictionary<string, OutputRefresh> _pendingOutputRefreshes;
        private readonly Dictionary<string, bool> _lastSeenCompileItems;
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ProjectInstanceEvaluator _projectInstanceEvaluator;
        private readonly UpdateBufferDispatcher _updateBufferDispatcher;
        private readonly ILogger<ComponentRefreshTrigger> _logger;
        private OmniSharpProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public ComponentRefreshTrigger(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            ProjectInstanceEvaluator projectInstanceEvaluator,
            UpdateBufferDispatcher updateBufferDispatcher,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (projectInstanceEvaluator == null)
            {
                throw new ArgumentNullException(nameof(projectInstanceEvaluator));
            }

            if (updateBufferDispatcher == null)
            {
                throw new ArgumentNullException(nameof(updateBufferDispatcher));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _filePathNormalizer = filePathNormalizer;
            _projectInstanceEvaluator = projectInstanceEvaluator;
            _updateBufferDispatcher = updateBufferDispatcher;
            _logger = loggerFactory.CreateLogger<ComponentRefreshTrigger>();
            _deferredRefreshTasks = new Dictionary<string, Task>(FilePathComparer.Instance);
            _pendingOutputRefreshes = new Dictionary<string, OutputRefresh>(FilePathComparer.Instance);
            _lastSeenCompileItems = new Dictionary<string, bool>(FilePathComparer.Instance);
        }

        // Internal settable for testing
        // 250ms between publishes to prevent bursts of changes yet still be responsive to changes.
        internal int EnqueueDelay { get; set; } = 250;

        // Used in unit tests to ensure we can know when refreshes start.
        public ManualResetEventSlim NotifyRefreshWorkStarting { get; set; }

        // Used in unit tests to ensure we can control when refresh work starts.
        public ManualResetEventSlim BlockRefreshWorkStarting { get; set; }

        // Used in unit tests to ensure we can know when refreshes completes.
        public ManualResetEventSlim NotifyRefreshWorkCompleting { get; set; }

        private void OnStartingRefreshWork()
        {
            if (BlockRefreshWorkStarting != null)
            {
                BlockRefreshWorkStarting.Wait();
                BlockRefreshWorkStarting.Reset();
            }

            if (NotifyRefreshWorkStarting != null)
            {
                NotifyRefreshWorkStarting.Set();
            }
        }

        private void OnRefreshWorkCompleting()
        {
            if (NotifyRefreshWorkCompleting != null)
            {
                NotifyRefreshWorkCompleting.Set();
            }
        }

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
        }

        public async void RazorDocumentChanged(RazorFileChangeEventArgs args)
        {
            try
            {
                await RazorDocumentChangedAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error when handling file " + args.FilePath + " document changed: " + ex);
            }
        }

        public void RazorDocumentOutputChanged(RazorFileChangeEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _foregroundDispatcher.AssertBackgroundThread();

            // This is required due to a bug in OmniSharp roslyn https://github.com/OmniSharp/omnisharp-roslyn/issues/1418
            // OmniSharp doesn't directly listen for .cs file changes in the obj folder like VS windows does. Therefore
            // we need to play that part and force buffer updates to indirectly update their workspace to include our Razor
            // declaration files.

            lock (_refreshLock)
            {
                var projectFilePath = args.UnevaluatedProjectInstance.ProjectFileLocation.File;
                if (!_pendingOutputRefreshes.TryGetValue(projectFilePath, out var outputRefresh))
                {
                    outputRefresh = new OutputRefresh();
                    _pendingOutputRefreshes[projectFilePath] = outputRefresh;
                }

                outputRefresh.UpdateWithChange(args);

                if (!_deferredRefreshTasks.TryGetValue(projectFilePath, out var update) || update.IsCompleted)
                {
                    _deferredRefreshTasks[projectFilePath] = RefreshAfterDelay(projectFilePath);
                }
            }
        }

        private async Task RefreshAfterDelay(string projectFilePath)
        {
            await Task.Delay(EnqueueDelay).ConfigureAwait(false);

            OnStartingRefreshWork();

            OutputRefresh outputRefresh;
            lock (_refreshLock)
            {
                if (!_pendingOutputRefreshes.TryGetValue(projectFilePath, out outputRefresh))
                {
                    return;
                }

                _pendingOutputRefreshes.Remove(projectFilePath);
            }

            await RefreshAsync(outputRefresh);
        }

        private async Task RefreshAsync(OutputRefresh refresh)
        {
            // Re-evaluate project instance so we can determine compile items properly.
            var projectInstance = _projectInstanceEvaluator.Evaluate(refresh.ProjectInstance);

            foreach (var documentChangeInfo in refresh.DocumentChangeInfos.Values)
            {
                try
                {
                    // Force update the OmniSharp Workspace for component declaration changes.

                    var componentDeclarationLocation = documentChangeInfo.FilePath;
                    var isCompileItem = IsCompileItem(documentChangeInfo.RelativeFilePath, projectInstance);
                    var wasACompileItem = false;
                    lock (_lastSeenCompileItems)
                    {
                        _lastSeenCompileItems.TryGetValue(documentChangeInfo.FilePath, out wasACompileItem);
                        _lastSeenCompileItems[documentChangeInfo.FilePath] = isCompileItem;
                    }

                    if (!isCompileItem && wasACompileItem)
                    {
                        // Output document should no longer be considered as a compile item, clear the workspace content for it.
                        var request = new Request()
                        {
                            FileName = componentDeclarationLocation,
                            Buffer = string.Empty,
                        };
                        await _updateBufferDispatcher.UpdateBufferAsync(request);
                    }
                    else if (isCompileItem)
                    {
                        // Force update the OmniSharp Workspace for component declaration changes.
                        var request = new UpdateBufferRequest()
                        {
                            FileName = componentDeclarationLocation,
                            FromDisk = true,
                        };
                        await _updateBufferDispatcher.UpdateBufferAsync(request);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unexpected error when updating workspace representation of '" + documentChangeInfo.FilePath + "': " + ex);
                }
            }

            OnRefreshWorkCompleting();
        }

        // Internal for testing
        internal bool IsCompileItem(string filePath, ProjectInstance projectInstance)
        {
            var compileItems = projectInstance.GetItems(CompileItemType);
            foreach (var item in compileItems)
            {
                if (_filePathNormalizer.FilePathsEquivalent(item.EvaluatedInclude, filePath))
                {
                    return true;
                }
            }

            return false;
        }

        // Internal for testing
        internal async Task RazorDocumentChangedAsync(RazorFileChangeEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _foregroundDispatcher.AssertBackgroundThread();

            if (args.Kind == RazorFileChangeKind.Changed)
            {
                // On save we kick off a design time build if the file saved happened to be a component. 
                // This replicates the VS windows world SingleFileGenerator behavior.
                var isComponentFile = await Task.Factory.StartNew(
                    () => IsComponentFile(args.RelativeFilePath, args.UnevaluatedProjectInstance.ProjectFileLocation.File),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler);

                if (isComponentFile)
                {
                    // Evaluation will re-generate the .razor.g.cs files which will indirectly trigger RazorDocumentOutputChanged.
                    _projectInstanceEvaluator.Evaluate(args.UnevaluatedProjectInstance);
                }
            }
        }

        // Internal for testing
        internal bool IsComponentFile(string relativeDocumentFilePath, string projectFilePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var projectSnapshot = _projectManager.GetLoadedProject(projectFilePath);
            if (projectSnapshot == null)
            {
                return false;
            }

            var documentSnapshot = projectSnapshot.GetDocument(relativeDocumentFilePath);
            if (documentSnapshot == null)
            {
                return false;
            }

            var isComponentKind = FileKinds.IsComponent(documentSnapshot.FileKind);
            return isComponentKind;
        }

        private class OutputRefresh
        {
            private readonly Dictionary<string, DocumentChangeInfo> _documentChangeInfos;
            private ProjectInstance _projectInstance;

            public OutputRefresh()
            {
                _documentChangeInfos = new Dictionary<string, DocumentChangeInfo>(FilePathComparer.Instance);
            }

            public ProjectInstance ProjectInstance => _projectInstance;

            public IReadOnlyDictionary<string, DocumentChangeInfo> DocumentChangeInfos => _documentChangeInfos;

            public void UpdateWithChange(RazorFileChangeEventArgs change)
            {
                if (change == null)
                {
                    throw new ArgumentNullException(nameof(change));
                }

                // Always take latest project instance.
                _projectInstance = change.UnevaluatedProjectInstance;
                _documentChangeInfos[change.FilePath] = new DocumentChangeInfo(change.FilePath, change.RelativeFilePath, change.Kind);
            }
        }

        private struct DocumentChangeInfo
        {
            public DocumentChangeInfo(string filePath, string relativeFilePath, RazorFileChangeKind changeKind)
            {
                FilePath = filePath;
                RelativeFilePath = relativeFilePath;
                ChangeKind = changeKind;
            }

            public string FilePath { get; }

            public string RelativeFilePath { get; }

            public RazorFileChangeKind ChangeKind { get; }
        }
    }
}
