// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using OmniSharp;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    // This entire class is a temporary work around for https://github.com/OmniSharp/omnisharp-roslyn/issues/1443.
    // We hack together a heuristic to detect when Razor documents that shouldn't be added to the workspace are and to then
    // remove them from the workspace. In the primary case we're watching for pre-compiled Razor files that are generated
    // from calling dotnet build and removing them from the workspace once they're added.

    [Shared]
    [Export(typeof(IMSBuildEventSink))]
    public class PrecompiledRazorPageSuppressor : IMSBuildEventSink
    {
        private readonly OmniSharpWorkspace _workspace;

        [ImportingConstructor]
        public PrecompiledRazorPageSuppressor(OmniSharpWorkspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _workspace = workspace;

            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                    var project = args.NewSolution.GetProject(args.ProjectId);
                    var document = project.GetDocument(args.DocumentId);

                    if (document.FilePath == null)
                    {
                        break;
                    }

                    if (document.FilePath.EndsWith(".RazorTargetAssemblyInfo.cs", StringComparison.Ordinal) ||
                        document.FilePath.EndsWith(".RazorAssemblyInfo.cs", StringComparison.Ordinal))
                    {
                        // Razor assembly info. This doesn't catch cases when users have customized their assembly info but captures all of the
                        // default cases for now. Once the omnisharp-roslyn bug has been fixed this entire class can go awy so we're hacking for now.
                        _workspace.RemoveDocument(document.Id);
                        break;
                    }

                    if (!document.FilePath.EndsWith(".cshtml.g.cs", StringComparison.Ordinal) && 
                        !document.FilePath.EndsWith(".razor.g.cs", StringComparison.Ordinal) &&

                        // 2.2 only extension for generated Razor files
                        !document.FilePath.EndsWith("g.cshtml.cs", StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (!document.FilePath.Contains("RazorDeclaration"))
                    {
                        // Razor output file that is not a declaration file.
                        _workspace.RemoveDocument(document.Id);
                        break;
                    }

                    break;
            }
        }

        public void ProjectLoaded(ProjectLoadedEventArgs e)
        {
            // We don't do anything on project load we're just using the IMSBuildEventSink to ensure we're instantiated.
        }
    }
}
