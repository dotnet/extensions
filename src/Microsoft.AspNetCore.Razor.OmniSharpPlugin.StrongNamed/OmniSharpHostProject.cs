// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class OmniSharpHostProject
    {
        private readonly HostProject _hostProject;

        public OmniSharpHostProject(string projectFilePath, RazorConfiguration razorConfiguration)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (razorConfiguration == null)
            {
                throw new ArgumentNullException(nameof(razorConfiguration));
            }

            _hostProject = new HostProject(projectFilePath, razorConfiguration);
        }

        public string FilePath => _hostProject.FilePath;

        public RazorConfiguration Configuration => _hostProject.Configuration;
    }
}
