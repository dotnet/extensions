// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(RazorConfigurationProvider))]
    internal class SystemWebConfigurationProvider : CoreProjectConfigurationProvider
    {
        // Internal for testing
        internal const string ReferencePathWithRefAssembliesItemType = "ReferencePathWithRefAssemblies";
        internal const string SystemWebRazorAssemblyFileName = "System.Web.Razor.dll";

        private const string LegacyRazorAssemblyName = "System.Web.Razor";

        public override bool TryResolveConfiguration(RazorConfigurationProviderContext context, out RazorConfiguration configuration)
        {
            if (HasRazorCoreCapability(context))
            {
                configuration = null;
                return false;
            }

            var compilationReferences = context.ProjectInstance.GetItems(ReferencePathWithRefAssembliesItemType);
            string systemWebRazorReferenceFullPath = null;
            foreach (var compilationReference in compilationReferences)
            {
                var assemblyFullPath = compilationReference.EvaluatedInclude;
                if (assemblyFullPath.Length == SystemWebRazorAssemblyFileName.Length)
                {
                    continue;
                }

                var potentialPathSeparator = assemblyFullPath[assemblyFullPath.Length - SystemWebRazorAssemblyFileName.Length - 1];
                if (potentialPathSeparator == '/' || potentialPathSeparator == '\\')
                {
                    systemWebRazorReferenceFullPath = assemblyFullPath;
                    break;
                }
            }

            if (systemWebRazorReferenceFullPath == null)
            {
                configuration = null;
                return false;
            }

            configuration = UnsupportedRazorConfiguration.Instance;
            return true;
        }
    }
}
