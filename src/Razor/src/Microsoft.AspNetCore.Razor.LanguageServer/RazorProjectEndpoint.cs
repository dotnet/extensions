// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorProjectEndpoint :
        IRazorAddDocumentHandler,
        IRazorRemoveDocumentHandler
    {
        private readonly RazorProjectService _projectService;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public RazorProjectEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            RazorProjectService projectService)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
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
