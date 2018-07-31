// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultHostProjectShim : HostProjectShim
    {
        public DefaultHostProjectShim(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            InnerHostProject = hostProject;
        }

        public HostProject InnerHostProject { get; }

        public override RazorConfiguration Configuration => InnerHostProject.Configuration;

        public override string FilePath => InnerHostProject.FilePath;
    }
}
