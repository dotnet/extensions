// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal class EmptyTextLoader : TextLoader
    {
        private readonly string _filePath;
        private readonly VersionStamp _version;

        public EmptyTextLoader(string filePath)
        {
            _filePath = filePath;
            _version = VersionStamp.Create(); // Version will never change so this can be reused.
        }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            // Providing an encoding here is important for debuggability. Without this edit-and-continue
            // won't work for projects with Razor files.
            return Task.FromResult(TextAndVersion.Create(SourceText.From(string.Empty, Encoding.UTF8), _version, _filePath));
        }
    }
}