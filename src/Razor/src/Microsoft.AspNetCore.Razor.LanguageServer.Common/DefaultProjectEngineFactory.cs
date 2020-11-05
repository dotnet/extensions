// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
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
            });
        }
    }
}