// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(ProjectConfigurationProvider))]
    internal class SystemWebConfigurationProvider : CoreProjectConfigurationProvider
    {
        // Internal for testing
        internal const string ReferencePathWithRefAssembliesItemType = "ReferencePathWithRefAssemblies";
        internal const string SystemWebRazorAssemblyFileName = "System.Web.Razor.dll";

        public override bool TryResolveConfiguration(ProjectConfigurationProviderContext context, out ProjectConfiguration configuration)
        {
            if (HasRazorCoreCapability(context))
            {
                configuration = null;
                return false;
            }

            var compilationReferences = context.ProjectInstance.GetItems(ReferencePathWithRefAssembliesItemType);
            foreach (var compilationReference in compilationReferences)
            {
                var assemblyFullPath = compilationReference.EvaluatedInclude;
                if (assemblyFullPath.EndsWith(SystemWebRazorAssemblyFileName, FilePathComparison.Instance))
                {
                    var potentialPathSeparator = assemblyFullPath[assemblyFullPath.Length - SystemWebRazorAssemblyFileName.Length - 1];
                    if (potentialPathSeparator == '/' || potentialPathSeparator == '\\')
                    {
                        configuration = new ProjectConfiguration(UnsupportedRazorConfiguration.Instance, Array.Empty<OmniSharpHostDocument>(), rootNamespace: null);
                        return true;
                    }
                }
            }

            configuration = null;
            return false;
        }
    }
}
