// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal class DefaultRazorProjectHost : RazorProjectHostBase
    {
        private const string RazorLangVersionProperty = "RazorLangVersion";
        private const string RazorDefaultConfigurationProperty = "RazorDefaultConfiguration";
        private const string RazorExtensionItemType = "RazorExtension";
        private const string RazorConfigurationItemType = "RazorConfiguration";
        private const string RazorConfigurationItemTypeExtensionsProperty = "Extensions";
        private const string RootNamespaceProperty = "RootNamespace";
        private const string ContentItemType = "Content";

        private IReadOnlyList<string> _currentRazorFilePaths = Array.Empty<string>();

        public DefaultRazorProjectHost(
            DotNetProject project,
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManagerBase projectSnapshotManager)
            : base(project, foregroundDispatcher, projectSnapshotManager)
        {
        }

        protected override async Task OnProjectChangedAsync()
        {
            ForegroundDispatcher.AssertBackgroundThread();

            await ExecuteWithLockAsync(async () =>
            {
                var projectProperties = DotNetProject.MSBuildProject.EvaluatedProperties;
                var projectItems = DotNetProject.MSBuildProject.EvaluatedItems;

                if (TryGetConfiguration(projectProperties, projectItems, out var configuration))
                {
                    TryGetRootNamespace(projectProperties, out var rootNamespace);
                    var hostProject = new HostProject(DotNetProject.FileName.FullPath, configuration, rootNamespace);
                    await UpdateHostProjectUnsafeAsync(hostProject).ConfigureAwait(false);
                    UpdateDocuments(hostProject, projectItems);
                }
                else
                {
                    // Ok we can't find a configuration. Let's assume this project isn't using Razor then.
                    await UpdateHostProjectUnsafeAsync(null).ConfigureAwait(false);
                }
            });
        }

        internal IReadOnlyList<string> GetRazorDocuments(string projectDirectory, IEnumerable<IMSBuildItemEvaluated> projectItems)
        {
            var documentFilePaths = projectItems
                .Where(IsRazorDocumentItem)
                .Select(item => GetAbsolutePath(projectDirectory, item.Include))
                .ToList();

            return documentFilePaths;
        }

        private void UpdateDocuments(HostProject hostProject, IEnumerable<IMSBuildItemEvaluated> projectItems)
        {
            var projectDirectory = Path.GetDirectoryName(hostProject.FilePath);
            var documentFilePaths = GetRazorDocuments(projectDirectory, projectItems);

            var oldFiles = _currentRazorFilePaths;
            var newFiles = documentFilePaths.ToImmutableHashSet();
            var addedFiles = newFiles.Except(oldFiles);
            var removedFiles = oldFiles.Except(newFiles);

            _currentRazorFilePaths = documentFilePaths;

            _ = Task.Factory.StartNew(() =>
              {
                  foreach (var document in removedFiles)
                  {
                      RemoveDocument(hostProject, document);
                  }

                  foreach (var document in addedFiles)
                  {
                      var relativeFilePath = document.Substring(projectDirectory.Length + 1);
                      AddDocument(hostProject, document, relativeFilePath);
                  }
              },
            CancellationToken.None,
            TaskCreationOptions.None,
            ForegroundDispatcher.ForegroundScheduler);
        }

        // Internal for testing
        internal static bool IsRazorDocumentItem(IMSBuildItemEvaluated item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Name != ContentItemType)
            {
                // We only inspect content items for Razor documents.
                return false;
            }

            if (item.Include == null)
            {
                return false;
            }


            if (!item.Include.EndsWith(".razor", StringComparison.Ordinal) && !item.Include.EndsWith(".cshtml", StringComparison.Ordinal))
            {
                // Doesn't have a Razor looking file extension
                return false;
            }

            return true;
        }

        private string GetAbsolutePath(string projectDirectory, string relativePath)
        {
            if (!Path.IsPathRooted(relativePath))
            {
                relativePath = Path.Combine(projectDirectory, relativePath);
            }

            // Normalize the path separator characters in case they're mixed
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);

            return relativePath;
        }

        // Internal for testing
        internal static bool TryGetConfiguration(
            IMSBuildEvaluatedPropertyCollection projectProperties,
            IEnumerable<IMSBuildItemEvaluated> projectItems,
            out RazorConfiguration configuration)
        {
            if (!TryGetDefaultConfiguration(projectProperties, out var defaultConfiguration))
            {
                configuration = null;
                return false;
            }

            if (!TryGetLanguageVersion(projectProperties, out var languageVersion))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfigurationItem(defaultConfiguration, projectItems, out var configurationItem))
            {
                configuration = null;
                return false;
            }

            var extensionNames = GetExtensionNames(configurationItem);
            var extensions = GetExtensions(extensionNames, projectItems);
            configuration = new ProjectSystemRazorConfiguration(languageVersion, configurationItem.Include, extensions);
            return true;
        }


        // Internal for testing
        internal static bool TryGetDefaultConfiguration(IMSBuildEvaluatedPropertyCollection projectProperties, out string defaultConfiguration)
        {
            defaultConfiguration = projectProperties.GetValue(RazorDefaultConfigurationProperty);
            if (string.IsNullOrEmpty(defaultConfiguration))
            {
                defaultConfiguration = null;
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetLanguageVersion(IMSBuildEvaluatedPropertyCollection projectProperties, out RazorLanguageVersion languageVersion)
        {
            var languageVersionValue = projectProperties.GetValue(RazorLangVersionProperty);
            if (string.IsNullOrEmpty(languageVersionValue))
            {
                languageVersion = null;
                return false;
            }

            if (!RazorLanguageVersion.TryParse(languageVersionValue, out languageVersion))
            {
                languageVersion = RazorLanguageVersion.Latest;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetConfigurationItem(
            string configuration,
            IEnumerable<IMSBuildItemEvaluated> projectItems,
            out IMSBuildItemEvaluated configurationItem)
        {
            foreach (var item in projectItems)
            {
                if (item.Name == RazorConfigurationItemType && item.Include == configuration)
                {
                    configurationItem = item;
                    return true;
                }
            }

            configurationItem = null;
            return false;
        }

        // Internal for testing
        internal static string[] GetExtensionNames(IMSBuildItemEvaluated configurationItem)
        {
            var extensionNamesValue = configurationItem.Metadata.GetValue(RazorConfigurationItemTypeExtensionsProperty);

            if (string.IsNullOrEmpty(extensionNamesValue))
            {
                return Array.Empty<string>();
            }

            return extensionNamesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Internal for testing
        internal static ProjectSystemRazorExtension[] GetExtensions(
            string[] configuredExtensionNames,
            IEnumerable<IMSBuildItemEvaluated> projectItems)
        {
            var extensions = new List<ProjectSystemRazorExtension>();

            foreach (var item in projectItems)
            {
                if (item.Name != RazorExtensionItemType)
                {
                    // Not a RazorExtension
                    continue;
                }

                var extensionName = item.Include;
                if (configuredExtensionNames.Contains(extensionName))
                {
                    extensions.Add(new ProjectSystemRazorExtension(extensionName));
                }
            }

            return extensions.ToArray();
        }

        // Internal for testing
        internal static bool TryGetRootNamespace(IMSBuildEvaluatedPropertyCollection projectProperties, out string rootNamespace)
        {
            rootNamespace = projectProperties.GetValue(RootNamespaceProperty);
            if (string.IsNullOrEmpty(rootNamespace))
            {
                rootNamespace = null;
                return false;
            }

            return true;
        }
    }
}
