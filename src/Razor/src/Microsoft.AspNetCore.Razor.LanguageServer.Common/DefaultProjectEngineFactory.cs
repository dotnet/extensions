// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{

    [ExportCustomProjectEngineFactory("Default", SupportsSerialization = true)]
    internal class DefaultProjectEngineFactory : IProjectEngineFactory
    {
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                CompilerFeatures.Register(b);

                configure?.Invoke(b);

                // See comments on MangleClassNames
                var componentDocumentClassifier = b.Features.OfType<ComponentDocumentClassifierPass>().FirstOrDefault();
                if (componentDocumentClassifier != null)
                {
                    componentDocumentClassifier.MangleClassNames = true;
                }
            });
        }
    }
}