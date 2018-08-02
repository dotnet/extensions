// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorProjectEndpoint : IRazorAddProjectHandler
    {
        private readonly RazorProjectService _projectService;
        private readonly RazorConfigurationResolver _configurationResolver;
        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly VSCodeLogger _logger;

        public RazorProjectEndpoint(
            ForegroundDispatcherShim foregroundDispatcher,
            RazorConfigurationResolver configurationResolver,
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
                razorConfiguration = RazorConfiguration.Default;
                _logger.Log($"Could not resolve Razor configuration '{request.ConfigurationName}'. Falling back to default configuration '{razorConfiguration.ConfigurationName}'.");
            }

            await Task.Factory.StartNew(
                () => _projectService.AddProject(request.FilePath, razorConfiguration),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler);

            return Unit.Value;
        }
    }
}
