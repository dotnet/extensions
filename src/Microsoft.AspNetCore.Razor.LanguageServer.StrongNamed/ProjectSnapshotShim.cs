// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{

    public abstract class ProjectSnapshotShim
    {
        public abstract HostProjectShim HostProject { get; }

        public abstract RazorConfiguration Configuration { get; }

        public abstract IEnumerable<string> DocumentFilePaths { get; }

        public abstract string FilePath { get; }

        public abstract bool IsInitialized { get; }

        public abstract VersionStamp Version { get; }

        public abstract Project WorkspaceProject { get; }

        public abstract RazorProjectEngine GetProjectEngine();

        public abstract DocumentSnapshotShim GetDocument(string filePath);

        public abstract Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync();

        public abstract bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result);
    }
}
