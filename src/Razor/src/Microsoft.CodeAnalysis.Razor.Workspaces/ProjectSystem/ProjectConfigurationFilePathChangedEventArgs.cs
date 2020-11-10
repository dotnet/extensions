// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectConfigurationFilePathChangedEventArgs : EventArgs
    {
        public ProjectConfigurationFilePathChangedEventArgs(string projectFilePath, string configurationFilePath)
        {
            if (projectFilePath is null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            ConfigurationFilePath = configurationFilePath;
        }

        public string ProjectFilePath { get; }

        public string ConfigurationFilePath { get; }
    }
}
