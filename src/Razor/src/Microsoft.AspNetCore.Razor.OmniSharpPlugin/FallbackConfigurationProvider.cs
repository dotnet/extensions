// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal class FallbackConfigurationProvider : CoreProjectConfigurationProvider
    {
        public static FallbackConfigurationProvider Instance = new FallbackConfigurationProvider();

        // Internal for testing
        internal const string ReferencePathWithRefAssembliesItemType = "ReferencePathWithRefAssemblies";
        internal const string MvcAssemblyFileName = "Microsoft.AspNetCore.Mvc.Razor.dll";

        public override bool TryResolveConfiguration(ProjectConfigurationProviderContext context, out ProjectConfiguration configuration)
        {
            if (!HasRazorCoreCapability(context))
            {
                configuration = null;
                return false;
            }

            var compilationReferences = context.ProjectInstance.GetItems(ReferencePathWithRefAssembliesItemType);
            string mvcReferenceFullPath = null;
            foreach (var compilationReference in compilationReferences)
            {
                var assemblyFullPath = compilationReference.EvaluatedInclude;
                if (assemblyFullPath.EndsWith(MvcAssemblyFileName, FilePathComparison.Instance))
                {
                    var potentialPathSeparator = assemblyFullPath[assemblyFullPath.Length - MvcAssemblyFileName.Length - 1];
                    if (potentialPathSeparator == '/' || potentialPathSeparator == '\\')
                    {
                        mvcReferenceFullPath = assemblyFullPath;
                        break;
                    }
                }
            }

            if (mvcReferenceFullPath == null)
            {
                configuration = null;
                return false;
            }

            var version = GetAssemblyVersion(mvcReferenceFullPath);
            if (version == null)
            {
                configuration = null;
                return false;
            }

            var razorConfiguration = FallbackRazorConfiguration.SelectConfiguration(version);
            configuration = new ProjectConfiguration(razorConfiguration, Array.Empty<OmniSharpHostDocument>(), rootNamespace: null);
            return true;
        }

        // Protected virtual for testing
        protected virtual Version GetAssemblyVersion(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new PEReader(stream))
                {
                    var metadataReader = reader.GetMetadataReader();

                    var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                    return assemblyDefinition.Version;
                }
            }
            catch
            {
                // We're purposely silencing any kinds of I/O exceptions here, just in case something wacky is going on.
                return null;
            }
        }
    }
}
