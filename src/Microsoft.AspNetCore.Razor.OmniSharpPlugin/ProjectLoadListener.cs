// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
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

        private const string MSBuildProjectFullPathPropertyName = "MSBuildProjectFullPath";
        private const string DebugRazorOmnisharpPluginPropertyName = "_DebugRazorOmnisharpPlugin_";
        private readonly ILogger _logger;

        [ImportingConstructor]
        public ProjectLoadListener(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<ProjectLoadListener>();
        }

        public void ProjectLoaded(ProjectLoadedEventArgs args)
        {
            try
            {
                HandleDebug(args.ProjectInstance);

                if (!TryResolveRazorConfigurationPath(args.ProjectInstance, out var configPath))
                {
                    return;
                }

                var projectFilePath = args.ProjectInstance.GetPropertyValue(MSBuildProjectFullPathPropertyName);
                if (string.IsNullOrEmpty(projectFilePath))
                {
                    // This should never be true but we're being extra careful.
                    return;
                }

                var projectConfiguration = new RazorProjectConfiguration()
                {
                    ProjectFilePath = projectFilePath,

                    // TODO: Work ;)
                };

                var serializedOutput = JsonConvert.SerializeObject(
                    projectConfiguration,
                    Formatting.Indented);

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

        private static void HandleDebug(ProjectInstance projectInstance)
        {
            var debugPlugin = projectInstance.GetPropertyValue(DebugRazorOmnisharpPluginPropertyName);
            if (!string.IsNullOrEmpty(debugPlugin) && string.Equals(debugPlugin, "true", StringComparison.OrdinalIgnoreCase))
            {
                Debugger.Launch();
                Debugger.Break();
            }
        }

        // Internal for testing
        internal static bool TryResolveRazorConfigurationPath(ProjectInstance projectInstance, out string path)
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
