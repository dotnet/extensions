// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorProjectEndpoint :
        IRazorAddProjectHandler,
        IRazorRemoveProjectHandler,
        IRazorUpdateProjectHandler,
        IRazorAddDocumentHandler,
        IRazorRemoveDocumentHandler
    {
        private readonly RazorProjectService _projectService;
        private readonly ILogger<RazorProjectEndpoint> _logger;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public RazorProjectEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            RazorProjectService projectService,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
            _logger = loggerFactory.CreateLogger<RazorProjectEndpoint>();
        }

        public async Task<Unit> Handle(RazorAddProjectParams request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Factory.StartNew(
                () => _projectService.AddProject(request.FilePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(RazorRemoveProjectParams request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Factory.StartNew(
                () => _projectService.RemoveProject(request.FilePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(RazorUpdateProjectParams request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handle = request.ProjectSnapshotHandle;
            if (handle == null)
            {
                _logger.LogWarning("Could not update project information. This often happens after Razor LanguageServer releases when project formats change. Once project information has been recalculated by OmniSharp this warning should go away.");
                return Unit.Value;
            }

            await Task.Factory.StartNew(
                () => _projectService.UpdateProject(
                    handle.FilePath,
                    handle.Configuration,
                    handle.RootNamespace,
                    handle.ProjectWorkspaceState ?? ProjectWorkspaceState.Default,
                    handle.Documents ?? Array.Empty<DocumentSnapshotHandle>()),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(AddDocumentParams request, CancellationToken cancellationToken)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            await Task.Factory.StartNew(
                () => _projectService.AddDocument(request.FilePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }

        public async Task<Unit> Handle(RemoveDocumentParams request, CancellationToken cancellationToken)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            await Task.Factory.StartNew(
                () => _projectService.RemoveDocument(request.FilePath),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }
    }
}
