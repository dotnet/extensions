// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
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
                razorConfiguration = RazorConfiguration.Default;
                LogToClient($"Could not resolve Razor configuration '{notification.ConfigurationName}'. Falling back to default configuration '{razorConfiguration.ConfigurationName}'.");
            }

            await AddProjectOnForegroundAsync(notification, razorConfiguration);
            LogToClient($"Added project '{notification.FilePath}' to the Razor project system.");

            return Unit.Value;
        }

        public async Task<Unit> Handle(RazorAddDocumentParams notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            await AddDocumentOnForegroundAsync(notification.TextDocument);

            return Unit.Value;
        }

        private Task AddDocumentOnForegroundAsync(TextDocumentItem textDocument)
        {
            return Task.Factory.StartNew(() =>
            {
                var textDocumentPath = NormalizePath(textDocument.Uri.AbsolutePath);
                if (!TryResolveProject(textDocumentPath, out var projectSnapshot))
                {
                    // TODO: Support non-project based documents.
                    LogToClient($"Could not resolve project for document '{textDocument.Uri.LocalPath}'.");
                    return;
                }

                var hostDocument = HostDocumentShim.Create(textDocumentPath, textDocumentPath);
                var sourceText = SourceText.From(textDocument.Text);
                var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
                var textLoader = TextLoader.From(textAndVersion);
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(projectSnapshot.HostProject, hostDocument, textLoader);
                LogToClient($"Added document '{textDocumentPath}' to project {projectSnapshot.FilePath} in the Razor project system.");
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private bool TryResolveProject(string textDocumentPath, out ProjectSnapshotShim projectSnapshot)
        {
            var projects = _projectSnapshotManagerAccessor.Instance.Projects;
            for (var i = 0; i < projects.Count; i++)
            {
                var projectDirectory = NormalizePath(new FileInfo(projects[i].FilePath).Directory.FullName);
                if (textDocumentPath.StartsWith(projectDirectory))
                {
                    projectSnapshot = projects[i];
                    return true;
                }
            }

            projectSnapshot = null;
            return false;
        }

        private string NormalizePath(string filePath)
        {
            var decodedPath = WebUtility.UrlDecode(filePath);
            var normalized = decodedPath.Replace('\\', '/');
            return normalized;
        }

        private Task AddProjectOnForegroundAsync(RazorAddProjectParams notification, RazorConfiguration razorConfiguration)
        {
            return Task.Factory.StartNew(() =>
            {
                var normalizedPath = NormalizePath(notification.FilePath);
                var hostProject = HostProjectShim.Create(notification.FilePath, razorConfiguration);
                _projectSnapshotManagerAccessor.Instance.HostProjectAdded(hostProject);
            }, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }
    }
}
