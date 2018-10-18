// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    // This is a verbatim copy of Blazor's project engine implementation.

    internal class ProjectEngineFactory_Blazor : IProjectEngineFactory
    {
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                configure?.Invoke(b);
                new BlazorExtensionInitializer().Initialize(b);

                var classifier = b.Features.OfType<ComponentDocumentClassifierPass>().Single();
                classifier.MangleClassNames = true;
            });
        }
    }
}
