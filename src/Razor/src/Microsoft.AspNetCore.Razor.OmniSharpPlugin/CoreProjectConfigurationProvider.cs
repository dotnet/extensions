// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class CoreProjectConfigurationProvider : ProjectConfigurationProvider
    {
        // Internal for testing
        internal const string DotNetCoreRazorCapability = "DotNetCoreRazor";
        internal const string DotNetCoreWebCapability = "DotNetCoreWeb";
        internal const string DotNetCoreRazorConfigurationCapability = "DotNetCoreRazorConfiguration";

        protected bool HasRazorCoreCapability(ProjectConfigurationProviderContext context) =>
            context.ProjectCapabilities.Contains(DotNetCoreRazorCapability) ||
            context.ProjectCapabilities.Contains(DotNetCoreWebCapability);

        protected bool HasRazorCoreConfigurationCapability(ProjectConfigurationProviderContext context) =>
            context.ProjectCapabilities.Contains(DotNetCoreRazorConfigurationCapability);
    }
}
