// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
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
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectInstanceEvaluator _projectInstanceEvaluator;
        private readonly BufferManager _bufferManager;
        private readonly ILogger<ComponentRefreshTrigger> _logger;
        private OmniSharpProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public ComponentRefreshTrigger(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            ProjectInstanceEvaluator projectInstanceEvaluator,
            OmniSharpWorkspace omniSharpWorkspace,
            ILoggerFactory loggerFactory) : this(foregroundDispatcher, projectInstanceEvaluator, loggerFactory)
        {
            if (omniSharpWorkspace == null)
            {
                throw new ArgumentNullException(nameof(omniSharpWorkspace));
            }

            _bufferManager = omniSharpWorkspace.BufferManager;
        }

        // Internal constructor for testing
        internal ComponentRefreshTrigger(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            ProjectInstanceEvaluator projectInstanceEvaluator,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectInstanceEvaluator == null)
            {
                throw new ArgumentNullException(nameof(projectInstanceEvaluator));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectInstanceEvaluator = projectInstanceEvaluator;
            _logger = loggerFactory.CreateLogger<ComponentRefreshTrigger>();
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

        public async void RazorDocumentOutputChanged(RazorFileChangeEventArgs args)
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

            try
            {
                // Force update the OmniSharp Workspace for component declaration changes.

                var componentDeclarationLocation = args.FilePath;
                Request request = null;

                if (args.Kind == RazorFileChangeKind.Removed)
                {
                    // Document was deleted, clear the workspace content for it.
                    request = new Request()
                    {
                        FileName = componentDeclarationLocation,
                        Buffer = string.Empty,
                    };
                }
                else
                {
                    request = new UpdateBufferRequest()
                    {
                        FileName = componentDeclarationLocation,
                        FromDisk = true,
                    };
                }

                await _bufferManager.UpdateBufferAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error when determining if file " + args.FilePath + " was a Razor component or not: " + ex);
            }
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
    }
}
