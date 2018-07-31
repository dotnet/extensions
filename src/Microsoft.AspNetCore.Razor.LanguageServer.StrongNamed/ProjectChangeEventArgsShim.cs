// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public class ProjectChangeEventArgsShim : EventArgs
    {
        public ProjectChangeEventArgsShim(string projectFilePath, ProjectChangeKindShim kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            Kind = kind;
        }

        public ProjectChangeEventArgsShim(string projectFilePath, string documentFilePath, ProjectChangeKindShim kind)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            ProjectFilePath = projectFilePath;
            DocumentFilePath = documentFilePath;
            Kind = kind;
        }

        public string ProjectFilePath { get; }

        public string DocumentFilePath { get; }

        public ProjectChangeKindShim Kind { get; }
    }
}
