// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public abstract class HostProjectShim
    {
        public abstract RazorConfiguration Configuration { get; }

        public abstract string FilePath { get; }

        public static HostProjectShim Create(string filePath, RazorConfiguration configuration)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var hostProject = new HostProject(filePath, configuration);
            return new DefaultHostProjectShim(hostProject);
        }
    }
}
