// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal abstract class RazorProjectService
    {
        public abstract void AddDocument(SourceText sourceText, string filePath);

        public abstract void RemoveDocument(string filePath);

        public abstract void UpdateDocument(SourceText sourceText, string filePath);

        public abstract void AddProject(string filePath, RazorConfiguration configuration);

        public abstract void RemoveProject(string filePath);
    }
}
