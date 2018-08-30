// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorProjectEndpoint : IRazorAddProjectHandler, IRazorRemoveProjectHandler, IRazorAddDocumentHandler, IRazorRemoveDocumentHandler
    {
        private readonly RazorProjectService _projectService;
        private readonly RazorConfigurationResolver _configurationResolver;
        private readonly RemoteTextLoaderFactory _remoteTextLoaderFactory;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly VSCodeLogger _logger;

        public RazorProjectEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            RazorConfigurationResolver configurationResolver,
            RemoteTextLoaderFactory remoteTextLoaderFactory,
            RazorProjectService projectService,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (configurationResolver == null)
            {
                throw new ArgumentNullException(nameof(configurationResolver));
            }

            if (remoteTextLoaderFactory == null)
            {
                throw new ArgumentNullException(nameof(remoteTextLoaderFactory));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _configurationResolver = configurationResolver;
            _remoteTextLoaderFactory = remoteTextLoaderFactory;
            _projectService = projectService;
            _logger = logger;
        }

        public async Task<Unit> Handle(RazorAddProjectParams request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_configurationResolver.TryResolve(request.ConfigurationName, out var razorConfiguration))
            {
                razorConfiguration = _configurationResolver.Default;
                _logger.Log($"Could not resolve Razor configuration '{request.ConfigurationName}'. Falling back to default configuration '{razorConfiguration.ConfigurationName}'.");
            }

            await Task.Factory.StartNew(
                () => _projectService.AddProject(request.FilePath, razorConfiguration),
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

        public async Task<Unit> Handle(AddDocumentParams request, CancellationToken cancellationToken)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            var textLoader = _remoteTextLoaderFactory.Create(request.FilePath);
            await Task.Factory.StartNew(
                () => _projectService.AddDocument(request.FilePath, textLoader),
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
