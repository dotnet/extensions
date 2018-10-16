// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ProjectEngineFactory_Unsupported : IProjectEngineFactory
    {
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            return RazorProjectEngine.Create(configuration, fileSystem, builder =>
            {
                var csharpLoweringIndex = builder.Phases.IndexOf(builder.Phases.OfType<IRazorCSharpLoweringPhase>().Single());
                builder.Phases[csharpLoweringIndex] = new UnsupportedCSharpLoweringPhase();
            });
        }
    }
}
