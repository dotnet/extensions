// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class ProjectConfiguration
    {
        public ProjectConfiguration(RazorConfiguration configuration, IReadOnlyList<OmniSharpHostDocument> documents, string rootNamespace)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            Configuration = configuration;
            Documents = documents;
            RootNamespace = rootNamespace;
        }

        public RazorConfiguration Configuration { get; }

        public IReadOnlyList<OmniSharpHostDocument> Documents { get; }

        public string RootNamespace { get; }
    }
}
