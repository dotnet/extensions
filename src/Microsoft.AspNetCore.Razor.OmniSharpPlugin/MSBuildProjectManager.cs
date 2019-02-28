// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using OmniSharp;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmnisharpPlugin
{
    [Shared]
    [Export(typeof(IMSBuildEventSink))]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    internal class MSBuildProjectManager : IMSBuildEventSink, IOmniSharpProjectSnapshotManagerChangeTrigger
    {
        // Internal for testing
        internal const string IntermediateOutputPathPropertyName = "IntermediateOutputPath";
        internal const string MSBuildProjectDirectoryPropertyName = "MSBuildProjectDirectory";
        internal const string RazorConfigurationFileName = "project.razor.json";
        internal const string ProjectCapabilityItemType = "ProjectCapability";

        private const string MSBuildProjectFullPathPropertyName = "MSBuildProjectFullPath";
        private const string DebugRazorOmnisharpPluginPropertyName = "_DebugRazorOmnisharpPlugin_";
        private readonly ILogger _logger;
        private readonly IEnumerable<RazorConfigurationProvider> _projectConfigurationProviders;
        private readonly ProjectChangePublisher _projectConfigurationPublisher;
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly OmniSharpWorkspace _workspace;
        private OmniSharpProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public MSBuildProjectManager(
            [ImportMany] IEnumerable<RazorConfigurationProvider> projectConfigurationProviders,
            ProjectChangePublisher projectConfigurationPublisher,
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpWorkspace workspace,
            ILoggerFactory loggerFactory)
        {
            if (projectConfigurationProviders == null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationProviders));
            }

            if (projectConfigurationPublisher == null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationPublisher));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<MSBuildProjectManager>();
            _projectConfigurationProviders = projectConfigurationProviders;
            _projectConfigurationPublisher = projectConfigurationPublisher;
            _foregroundDispatcher = foregroundDispatcher;

            _workspace = workspace;
        }

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
        }

        public async void ProjectLoaded(ProjectLoadedEventArgs args)
        {
            try
            {
                HandleDebug(args.ProjectInstance);

                if (!TryResolveConfigurationOutputPath(args.ProjectInstance, out var configPath))
                {
                    return;
                }

                var projectFilePath = args.ProjectInstance.GetPropertyValue(MSBuildProjectFullPathPropertyName);
                if (string.IsNullOrEmpty(projectFilePath))
                {
                    // This should never be true but we're being extra careful.
                    return;
                }

                _projectConfigurationPublisher.SetPublishFilePath(projectFilePath, configPath);
                var razorConfiguration = GetRazorConfiguration(args.ProjectInstance, _projectConfigurationProviders);
                var project = _workspace.CurrentSolution.GetProject(args.Id);

                await Task.Factory.StartNew(() =>
                {
                    var projectSnapshot = _projectManager.GetLoadedProject(projectFilePath);
                    var hostProject = new OmniSharpHostProject(projectFilePath, razorConfiguration);
                    if (projectSnapshot == null)
                    {
                        _projectManager.ProjectAdded(hostProject);
                    }
                    else
                    {
                        _projectManager.ProjectConfigurationChanged(hostProject);
                    }
                },
                CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected exception got thrown from the Razor plugin: " + ex);
            }
        }

        // Internal for testing
        internal static RazorConfiguration GetRazorConfiguration(
            ProjectInstance projectInstance,
            IEnumerable<RazorConfigurationProvider> projectConfigurationProviders)
        {
            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            if (projectConfigurationProviders == null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationProviders));
            }

            var projectCapabilities = projectInstance
                .GetItems(ProjectCapabilityItemType)
                .Select(capability => capability.EvaluatedInclude)
                .ToList();

            var context = new RazorConfigurationProviderContext(projectCapabilities, projectInstance);
            foreach (var projectConfigurationProvider in projectConfigurationProviders)
            {
                if (projectConfigurationProvider.TryResolveConfiguration(context, out var configuration))
                {
                    return configuration;
                }
            }

            if (FallbackConfigurationProvider.Instance.TryResolveConfiguration(context, out var fallbackConfiguration))
            {
                return fallbackConfiguration;
            }

            return null;
        }

        private void HandleDebug(ProjectInstance projectInstance)
        {
            var debugPlugin = projectInstance.GetPropertyValue(DebugRazorOmnisharpPluginPropertyName);
            if (!string.IsNullOrEmpty(debugPlugin) && string.Equals(debugPlugin, "true", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Waiting for a debugger to attach to the Razor Plugin. Process id: {Process.GetCurrentProcess().Id}");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
                Debugger.Break();
            }
        }

        // Internal for testing
        internal static bool TryResolveConfigurationOutputPath(ProjectInstance projectInstance, out string path)
        {
            var intermediateOutputPath = projectInstance.GetPropertyValue(IntermediateOutputPathPropertyName);
            if (string.IsNullOrEmpty(intermediateOutputPath))
            {
                path = null;
                return false;
            }

            if (!Path.IsPathRooted(intermediateOutputPath))
            {
                // Relative path, need to convert to absolute.
                var projectDirectory = projectInstance.GetPropertyValue(MSBuildProjectDirectoryPropertyName);
                if (string.IsNullOrEmpty(projectDirectory))
                {
                    // This should never be true but we're beign extra careful.
                    path = null;
                    return false;
                }

                intermediateOutputPath = Path.Combine(projectDirectory, intermediateOutputPath);
            }

            intermediateOutputPath = intermediateOutputPath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            path = Path.Combine(intermediateOutputPath, RazorConfigurationFileName);
            return true;
        }
    }
}
