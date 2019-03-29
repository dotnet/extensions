// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal class ProjectEngineFactory_3_0 : IProjectEngineFactory
    {
        private const string AssemblyName = "Microsoft.AspNetCore.Mvc.Razor.Extensions";

        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            // Rewrite the assembly name into a full name just like this one, but with the name of the MVC design time assembly.
            var assemblyName = new AssemblyName(typeof(RazorProjectEngine).Assembly.FullName);
            assemblyName.Name = AssemblyName;

            var extension = new AssemblyExtension(configuration.ConfigurationName, Assembly.Load(assemblyName));
            var initializer = extension.CreateInitializer();

            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                CompilerFeatures.Register(b);

                initializer.Initialize(b);
                configure?.Invoke(b);

                var componentDocumentClassifier = b.Features.OfType<ComponentDocumentClassifierPass>().FirstOrDefault();
                if (componentDocumentClassifier != null)
                {
                    componentDocumentClassifier.MangleClassNames = true;
                }
            });
        }
    }
}
