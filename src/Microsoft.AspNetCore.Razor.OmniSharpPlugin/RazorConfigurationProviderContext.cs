// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Execution;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class RazorConfigurationProviderContext
    {
        public RazorConfigurationProviderContext(
            IReadOnlyList<string> projectCapabilities,
            ProjectInstance projectInstance)
        {
            if (projectCapabilities == null)
            {
                throw new ArgumentNullException(nameof(projectCapabilities));
            }

            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            ProjectCapabilities = projectCapabilities;
            ProjectInstance = projectInstance;
        }

        public IReadOnlyList<string> ProjectCapabilities { get; }

        public ProjectInstance ProjectInstance { get; }
    }
}
