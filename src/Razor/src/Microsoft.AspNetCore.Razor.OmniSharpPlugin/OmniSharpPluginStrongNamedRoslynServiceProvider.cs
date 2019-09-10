// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using OmniSharp.Services;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    // This service provider is here to enable the OmniSharp process to indirectly utilize internal types that are exposed via the strong named
    // Razor assemblies by re-exporting them via the Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed assembly. For example, we re-export the
    // DefaultTagHelperResolver's factory in the strong named assembly because it's internal to Razor and can only be accessed in a strong named
    // assembly.
    //
    // We're also unable to directly load and discover Roslyn exports in the Microsoft.CodeAnalysis.Razor.Workspaces.dll due to mismatches in 
    // MSBuild metadata dependencies. The expectations of the MSBuild that is loaded with OmniSharp doesn't understand the version that Razor
    // is compiled against. If we could there'd be no need for this class.

    [Shared]
    [Export(typeof(IHostServicesProvider))]
    public class OmniSharpPluginStrongNamedRoslynServiceProvider : IHostServicesProvider
    {
        public OmniSharpPluginStrongNamedRoslynServiceProvider()
        {
            var strongNamedAssembly = Assembly.Load("Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed");
            Assemblies = ImmutableArray.Create(strongNamedAssembly);
        }

        public ImmutableArray<Assembly> Assemblies { get; }
    }
}
