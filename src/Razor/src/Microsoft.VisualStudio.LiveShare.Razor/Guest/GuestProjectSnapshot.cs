// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal class GuestProjectSnapshot : LiveShareProjectSnapshotBase
    {
        private readonly DefaultProjectSnapshot _innerProjectSnapshot;
        private readonly IReadOnlyList<TagHelperDescriptor> _tagHelpers;

        public GuestProjectSnapshot(ProjectState projectState, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (projectState == null)
            {
                throw new ArgumentNullException(nameof(projectState));
            }

            if (tagHelpers == null)
            {
                throw new ArgumentNullException(nameof(tagHelpers));
            }

            var snapshot = new DefaultProjectSnapshot(projectState);
            _innerProjectSnapshot = new DefaultProjectSnapshot(projectState);
            _tagHelpers = tagHelpers;
        }

        public override RazorConfiguration Configuration => _innerProjectSnapshot.Configuration;

        public override IEnumerable<string> DocumentFilePaths => _innerProjectSnapshot.DocumentFilePaths;

        public override string FilePath => _innerProjectSnapshot.FilePath;

        public override bool IsInitialized => true;

        public override VersionStamp Version => _innerProjectSnapshot.Version;

        public override Project WorkspaceProject => null;

        public override DocumentSnapshot GetDocument(string filePath) => _innerProjectSnapshot.GetDocument(filePath);

        public override RazorProjectEngine GetProjectEngine() => _innerProjectSnapshot.GetProjectEngine();

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync() => Task.FromResult(_tagHelpers);

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result)
        {
            result = _tagHelpers;
            return true;
        }
    }
}
