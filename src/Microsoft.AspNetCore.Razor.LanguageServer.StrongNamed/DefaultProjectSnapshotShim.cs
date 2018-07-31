using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultProjectSnapshotShim : ProjectSnapshotShim
    {
        public DefaultProjectSnapshotShim(ProjectSnapshot projectSnapshot)
        {
            if (projectSnapshot == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshot));
            }

            InnerProjectSnapshot = projectSnapshot;
        }

        public ProjectSnapshot InnerProjectSnapshot { get; }

        public override HostProjectShim HostProject
        {
            get
            {
                if (InnerProjectSnapshot is DefaultProjectSnapshot defaultSnapshot)
                {
                    new DefaultHostProjectShim(defaultSnapshot.HostProject);
                }

                return null;
            }
        }

        public override RazorConfiguration Configuration => InnerProjectSnapshot.Configuration;

        public override IEnumerable<string> DocumentFilePaths => InnerProjectSnapshot.DocumentFilePaths;

        public override string FilePath => InnerProjectSnapshot.FilePath;

        public override bool IsInitialized => InnerProjectSnapshot.IsInitialized;

        public override VersionStamp Version => InnerProjectSnapshot.Version;

        public override Project WorkspaceProject => InnerProjectSnapshot.WorkspaceProject;

        public override DocumentSnapshotShim GetDocument(string filePath)
        {
            var document = InnerProjectSnapshot.GetDocument(filePath);
            return new DefaultDocumentSnapshotShim(document);
        }

        public override RazorProjectEngine GetProjectEngine() => InnerProjectSnapshot.GetProjectEngine();

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync() => InnerProjectSnapshot.GetTagHelpersAsync();

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result) => InnerProjectSnapshot.TryGetTagHelpers(out result);
    }
}