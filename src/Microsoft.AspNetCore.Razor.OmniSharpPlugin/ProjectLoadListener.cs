// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmnisharpPlugin
{
    [Export(typeof(IMSBuildEventSink))]
    internal class ProjectLoadListener : IMSBuildEventSink
    {
        // Internal for testing
        internal const string IntermediateOutputPathPropertyName = "IntermediateOutputPath";
        internal const string MSBuildProjectDirectoryPropertyName = "MSBuildProjectDirectory";
        internal const string RazorConfigurationFileName = "project.razor.json";
        internal const string ProjectCapabilityItemType = "ProjectCapability";

        private const string MSBuildProjectFullPathPropertyName = "MSBuildProjectFullPath";
        private const string DebugRazorOmnisharpPluginPropertyName = "_DebugRazorOmnisharpPlugin_";
        private const string TargetFrameworkPropertyName = "TargetFramework";
        private readonly ILogger _logger;
        private readonly IEnumerable<RazorConfigurationProvider> _projectConfigurationProviders;

        [ImportingConstructor]
        public ProjectLoadListener(
            [ImportMany] IEnumerable<RazorConfigurationProvider> projectConfigurationProviders,
            ILoggerFactory loggerFactory)
        {
            if (projectConfigurationProviders == null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationProviders));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<ProjectLoadListener>();
            _projectConfigurationProviders = projectConfigurationProviders;
        }

        public void ProjectLoaded(ProjectLoadedEventArgs args)
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

                var targetFramework = args.ProjectInstance.GetPropertyValue(TargetFrameworkPropertyName);
                if (string.IsNullOrEmpty(projectFilePath))
                {
                    // This should never be true but we're being extra careful.
                    return;
                }

                var razorConfiguration = GetRazorConfiguration(args.ProjectInstance);
                var projectConfiguration = new RazorProjectConfiguration()
                {
                    ProjectFilePath = projectFilePath,
                    Configuration = razorConfiguration,
                    TargetFramework = targetFramework,
                };

                var serializedOutput = JsonConvert.SerializeObject(
                    projectConfiguration,
                    Formatting.Indented,
                    JsonConverterCollectionExtensions.RazorConverters.ToArray());

                try
                {
                    File.WriteAllText(configPath, serializedOutput);
                }
                catch (Exception)
                {
                    // TODO: Add retry.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected exception got thrown from the Razor plugin: " + ex);
            }
        }

        // Internal for testing
        internal RazorConfiguration GetRazorConfiguration(ProjectInstance projectInstance)
        {
            var projectCapabilities = projectInstance
                .GetItems(ProjectCapabilityItemType)
                .Select(capability => capability.EvaluatedInclude)
                .ToList();

            var context = new RazorConfigurationProviderContext(projectCapabilities, projectInstance);
            foreach (var projectConfigurationProvider in _projectConfigurationProviders)
            {
                if (projectConfigurationProvider.TryResolveConfiguration(context, out var configuration))
                {
                    return configuration;
                }
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

            path = Path.Combine(intermediateOutputPath, RazorConfigurationFileName);
            return true;
        }
    }
}
