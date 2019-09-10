// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Build.Execution;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(ProjectConfigurationProvider))]
    internal class LatestProjectConfigurationProvider : CoreProjectConfigurationProvider
    {
        // Internal for testing
        internal const string RazorGenerateWithTargetPathItemType = "RazorGenerateWithTargetPath";
        internal const string RazorComponentWithTargetPathItemType = "RazorComponentWithTargetPath";
        internal const string RazorTargetPathMetadataName = "TargetPath";
        internal const string RootNamespaceProperty = "RootNamespace";

        private const string RazorLangVersionProperty = "RazorLangVersion";
        private const string RazorDefaultConfigurationProperty = "RazorDefaultConfiguration";
        private const string RazorExtensionItemType = "RazorExtension";
        private const string RazorConfigurationItemType = "RazorConfiguration";
        private const string RazorConfigurationItemTypeExtensionsProperty = "Extensions";

        public override bool TryResolveConfiguration(ProjectConfigurationProviderContext context, out ProjectConfiguration configuration)
        {
            if (!HasRazorCoreCapability(context))
            {
                configuration = null;
                return false;
            }

            if (!HasRazorCoreConfigurationCapability(context))
            {
                // Razor project is < 2.1, we don't handle that.
                configuration = null;
                return false;
            }

            var projectInstance = context.ProjectInstance;
            if (!TryGetConfiguration(projectInstance, out configuration))
            {
                configuration = null;
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetConfiguration(
            ProjectInstance projectInstance,
            out ProjectConfiguration configuration)
        {
            if (!TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration))
            {
                configuration = null;
                return false;
            }

            if (!TryGetLanguageVersion(projectInstance, out var languageVersion))
            {
                configuration = null;
                return false;
            }

            if (!TryGetConfigurationItem(defaultConfiguration, projectInstance.Items, out var configurationItem))
            {
                configuration = null;
                return false;
            }

            var configuredExtensionNames = GetConfiguredExtensionNames(configurationItem);
            var rootNamespace = GetRootNamespace(projectInstance);
            var extensions = GetExtensions(configuredExtensionNames, projectInstance.Items);
            var razorConfiguration = new ProjectSystemRazorConfiguration(languageVersion, configurationItem.EvaluatedInclude, extensions);
            var hostDocuments = GetHostDocuments(projectInstance.Items);

            configuration = new ProjectConfiguration(razorConfiguration, hostDocuments, rootNamespace);
            return true;
        }

        // Internal for testing
        internal static string GetRootNamespace(ProjectInstance projectInstance)
        {
            var rootNamespace = projectInstance.GetPropertyValue(RootNamespaceProperty);
            if (string.IsNullOrEmpty(rootNamespace))
            {
                return null;
            }

            return rootNamespace;
        }

        // Internal for testing
        internal static IReadOnlyList<OmniSharpHostDocument> GetHostDocuments(ICollection<ProjectItemInstance> projectItems)
        {
            var hostDocuments = new HashSet<OmniSharpHostDocument>();

            foreach (var item in projectItems)
            {
                if (item.ItemType == RazorGenerateWithTargetPathItemType)
                {
                    var filePath = item.EvaluatedInclude;
                    var targetPath = item.GetMetadataValue(RazorTargetPathMetadataName);
                    var hostDocument = new OmniSharpHostDocument(filePath, targetPath, FileKinds.Legacy);
                    hostDocuments.Add(hostDocument);
                }
                else if (item.ItemType == RazorComponentWithTargetPathItemType)
                {
                    var filePath = item.EvaluatedInclude;
                    var targetPath = item.GetMetadataValue(RazorTargetPathMetadataName);
                    var fileKind = FileKinds.GetComponentFileKindFromFilePath(filePath);
                    var hostDocument = new OmniSharpHostDocument(filePath, targetPath, fileKind);
                    hostDocuments.Add(hostDocument);
                }
            }

            return hostDocuments.ToList();
        }


        // Internal for testing
        internal static bool TryGetDefaultConfiguration(ProjectInstance projectInstance, out string defaultConfiguration)
        {
            defaultConfiguration = projectInstance.GetPropertyValue(RazorDefaultConfigurationProperty);
            if (string.IsNullOrEmpty(defaultConfiguration))
            {
                defaultConfiguration = null;
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool TryGetLanguageVersion(ProjectInstance projectInstance, out RazorLanguageVersion languageVersion)
        {
            var languageVersionValue = projectInstance.GetPropertyValue(RazorLangVersionProperty);
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
            IEnumerable<ProjectItemInstance> projectItems,
            out ProjectItemInstance configurationItem)
        {
            foreach (var item in projectItems)
            {
                if (item.ItemType == RazorConfigurationItemType && item.EvaluatedInclude == configuration)
                {
                    configurationItem = item;
                    return true;
                }
            }

            configurationItem = null;
            return false;
        }

        // Internal for testing
        internal static string[] GetConfiguredExtensionNames(ProjectItemInstance configurationItem)
        {
            var extensionNamesValue = configurationItem.GetMetadataValue(RazorConfigurationItemTypeExtensionsProperty);

            if (string.IsNullOrEmpty(extensionNamesValue))
            {
                return Array.Empty<string>();
            }

            var configuredExtensionNames = extensionNamesValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return configuredExtensionNames;
        }

        // Internal for testing
        internal static ProjectSystemRazorExtension[] GetExtensions(
            string[] configuredExtensionNames,
            IEnumerable<ProjectItemInstance> projectItems)
        {
            var extensions = new List<ProjectSystemRazorExtension>();

            foreach (var item in projectItems)
            {
                if (item.ItemType != RazorExtensionItemType)
                {
                    // Not a RazorExtension
                    continue;
                }

                var extensionName = item.EvaluatedInclude;
                if (configuredExtensionNames.Contains(extensionName))
                {
                    extensions.Add(new ProjectSystemRazorExtension(extensionName));
                }
            }

            return extensions.ToArray();
        }

        private class ProjectSystemRazorConfiguration : RazorConfiguration
        {
            public ProjectSystemRazorConfiguration(
                RazorLanguageVersion languageVersion,
                string configurationName,
                RazorExtension[] extensions)
            {
                if (languageVersion == null)
                {
                    throw new ArgumentNullException(nameof(languageVersion));
                }

                if (configurationName == null)
                {
                    throw new ArgumentNullException(nameof(configurationName));
                }

                if (extensions == null)
                {
                    throw new ArgumentNullException(nameof(extensions));
                }

                LanguageVersion = languageVersion;
                ConfigurationName = configurationName;
                Extensions = extensions;
            }

            public override string ConfigurationName { get; }

            public override IReadOnlyList<RazorExtension> Extensions { get; }

            public override RazorLanguageVersion LanguageVersion { get; }
        }

        internal class ProjectSystemRazorExtension : RazorExtension
        {
            public ProjectSystemRazorExtension(string extensionName)
            {
                if (extensionName == null)
                {
                    throw new ArgumentNullException(nameof(extensionName));
                }

                ExtensionName = extensionName;
            }

            public override string ExtensionName { get; }
        }
    }
}
