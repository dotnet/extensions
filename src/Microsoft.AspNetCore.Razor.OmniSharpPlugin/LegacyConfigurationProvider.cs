// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(RazorConfigurationProvider))]
    internal class LegacyConfigurationProvider : CoreProjectConfigurationProvider
    {
        // Internal for testing
        internal const string ReferencePathWithRefAssembliesItemType = "ReferencePathWithRefAssemblies";
        internal const string MvcAssemblyFileName = "Microsoft.AspNetCore.Mvc.Razor.dll";

        private const string MvcAssemblyName = "Microsoft.AspNetCore.Mvc.Razor";

        public override bool TryResolveConfiguration(RazorConfigurationProviderContext context, out RazorConfiguration configuration)
        {
            if (!HasRazorCoreCapability(context))
            {
                configuration = null;
                return false;
            }

            if (HasRazorCoreConfigurationCapability(context))
            {
                // Razor project is >= 2.1, we don't handle that.
                configuration = null;
                return false;
            }

            var compilationReferences = context.ProjectInstance.GetItems(ReferencePathWithRefAssembliesItemType);
            string mvcReferenceFullPath = null;
            foreach (var compilationReference in compilationReferences)
            {
                var assemblyPath = compilationReference.EvaluatedInclude;
                if (compilationReference.EvaluatedInclude.EndsWith(MvcAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                {
                    mvcReferenceFullPath = compilationReference.EvaluatedInclude;
                    break;
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

            configuration = FallbackRazorConfiguration.SelectConfiguration(version);
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
