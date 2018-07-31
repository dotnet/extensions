// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Projects
{
    internal class RazorProjectService : LanguageServerHandlerBase, IRazorAddProjectHandler, IRazorAddDocumentHandler
    {
        private readonly ProjectSnapshotManagerShimAccessor _projectSnapshotManagerAccessor;
        private readonly RazorConfigurationResolver _configurationResolver;
        private readonly ForegroundDispatcherShim _foregroundDispatcher;

        public RazorProjectService(
            ProjectSnapshotManagerShimAccessor projectSnapshotManagerAccessor,
            RazorConfigurationResolver configurationResolver,
            ForegroundDispatcherShim foregroundDispatcher,
            ILanguageServer router) : base(router)
        {
            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (configurationResolver == null)
            {
                throw new ArgumentNullException(nameof(configurationResolver));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _configurationResolver = configurationResolver;
            _foregroundDispatcher = foregroundDispatcher;
        }

        public async Task<Unit> Handle(RazorAddProjectParams notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (!_configurationResolver.TryResolve(notification.ConfigurationName, out var razorConfiguration))
            {
                LogToClient($"Could not resolve Razor configuration '{notification.ConfigurationName}'. Falling back to default.");
                razorConfiguration = RazorConfiguration.Default;
            }

            await AddProjectOnForegroundAsync(notification, razorConfiguration);

            return Unit.Value;
        }

        public async Task<Unit> Handle(RazorAddDocumentParams notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            await AddDocumentOnForegroundAsync(notification.ProjectFilePath, notification.TextDocument);

            return Unit.Value;
        }

        private Task AddDocumentOnForegroundAsync(string projectFilePath, TextDocumentItem textDocument)
        {
            return Task.Factory.StartNew(() =>
            {
                var projectSnapshot = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(projectFilePath);
                if (projectSnapshot == null)
                {
                    // No active project to track the document. Treat this as a misc Razor file.
                    return;
                }

                var hostDocument = HostDocumentShim.Create(textDocument.Uri.AbsolutePath, textDocument.Uri.AbsolutePath);
                var sourceText = SourceText.From(textDocument.Text);
                var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
                var textLoader = TextLoader.From(textAndVersion);
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(projectSnapshot.HostProject, hostDocument, textLoader);
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private Task AddProjectOnForegroundAsync(RazorAddProjectParams notification, RazorConfiguration razorConfiguration)
        {
            return Task.Factory.StartNew(() =>
            {
                var hostProject = HostProjectShim.Create(notification.FilePath, razorConfiguration);
                _projectSnapshotManagerAccessor.Instance.HostProjectAdded(hostProject);
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }
    }
}
