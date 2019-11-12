// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Shared]
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class WorkspaceProjectStateChangeDetector : ProjectSnapshotChangeTrigger
    {
        private readonly ProjectWorkspaceStateGenerator _workspaceStateGenerator;
        private ProjectSnapshotManagerBase _projectManager;

        public int EnqueueDelay { get; set; } = 3 * 1000;

        // We throttle updates to projects to prevent doing too much work while the projects
        // are being initialized.
        //
        // Internal for testing
        internal Dictionary<ProjectId, Task> _deferredUpdates;

        [ImportingConstructor]
        public WorkspaceProjectStateChangeDetector(ProjectWorkspaceStateGenerator workspaceStateGenerator)
        {
            if (workspaceStateGenerator == null)
            {
                throw new ArgumentNullException(nameof(workspaceStateGenerator));
            }

            _workspaceStateGenerator = workspaceStateGenerator;
        }

        // Used in unit tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockDelayedUpdateWorkEnqueue { get; set; }

        private void OnStartingDelayedUpdate()
        {
            if (BlockDelayedUpdateWorkEnqueue != null)
            {
                BlockDelayedUpdateWorkEnqueue.Wait();
                BlockDelayedUpdateWorkEnqueue.Reset();
            }
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
            _projectManager.Workspace.WorkspaceChanged += Workspace_WorkspaceChanged;

            _deferredUpdates = new Dictionary<ProjectId, Task>();

            // This will usually no-op, in the case that another project snapshot change trigger immediately adds projects we want to be able to handle those projects
            InitializeSolution(_projectManager.Workspace.CurrentSolution);
        }

        // Internal for testing, virtual for temporary VSCode workaround
        internal virtual void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            Project project;
            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                    {
                        project = e.NewSolution.GetProject(e.ProjectId);

                        Debug.Assert(project != null);

                        if (TryGetProjectSnapshot(project.FilePath, out var projectSnapshot))
                        {
                            _workspaceStateGenerator.Update(project, projectSnapshot);
                        }
                        break;
                    }

                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        project = e.NewSolution.GetProject(e.ProjectId);

                        if (TryGetProjectSnapshot(project?.FilePath, out var _))
                        {
                            EnqueueUpdate(e.ProjectId);
                        }
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                    {
                        project = e.OldSolution.GetProject(e.ProjectId);
                        Debug.Assert(project != null);

                        if (TryGetProjectSnapshot(project?.FilePath, out var projectSnapshot))
                        {
                            _workspaceStateGenerator.Update(workspaceProject: null, projectSnapshot);
                        }

                        break;
                    }

                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.DocumentReloaded:
                    {
                        // This is the case when a component declaration file changes on disk. We have an MSBuild
                        // generator configured by the SDK that will poke these files on disk when a component
                        // is saved, or loses focus in the editor.
                        project = e.OldSolution.GetProject(e.ProjectId);
                        var document = project.GetDocument(e.DocumentId);

                        if (document.FilePath == null)
                        {
                            return;
                        }

                        // Using EndsWith because Path.GetExtension will ignore everything before .cs
                        // Using Ordinal because the SDK generates these filenames.
                        // Stll have .cshtml.g.cs and .razor.g.cs for Razor.VSCode scenarios.
                        if (document.FilePath.EndsWith(".cshtml.g.cs", StringComparison.Ordinal) ||
                            document.FilePath.EndsWith(".razor.g.cs", StringComparison.Ordinal) ||
                            document.FilePath.EndsWith(".razor", StringComparison.Ordinal) ||

                            // VSCode's background C# document
                            document.FilePath.EndsWith("__bg__virtual.cs"))
                        {
                            EnqueueUpdate(e.ProjectId);
                            return;
                        }

                        // We now know we're not operating directly on a Razor file. However, it's possible the user is operating on a partial class that is associated with a Razor file.

                        if (IsPartialComponentClass(document))
                        {
                            EnqueueUpdate(e.ProjectId);
                        }

                        break;
                    }

                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:

                    if (e.OldSolution != null)
                    {
                        foreach (var p in e.OldSolution.Projects)
                        {

                            if (TryGetProjectSnapshot(p?.FilePath, out var projectSnapshot))
                            {
                                _workspaceStateGenerator.Update(workspaceProject: null, projectSnapshot);
                            }
                        }
                    }

                    InitializeSolution(e.NewSolution);
                    break;
            }
        }

        // Internal for testing
        internal bool IsPartialComponentClass(Document document)
        {
            if (!document.TryGetSyntaxRoot(out var root))
            {
                return false;
            }

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            if (!classDeclarations.Any())
            {
                return false;
            }

            if (!document.TryGetSemanticModel(out var semanticModel))
            {
                // This will occasionally return false resulting in us not refreshing TagHelpers for component partial classes. This means there are situations when a user's
                // TagHelper definitions will not immediately update but we will eventually acheive omniscience.
                return false;
            }

            var icomponentType = semanticModel.Compilation.GetTypeByMetadataName(ComponentsApi.IComponent.MetadataName);
            if (icomponentType == null)
            {
                // IComponent is not available in the compilation.
                return false;
            }

            foreach (var classDeclaration in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                if (classSymbol == null)
                {
                    continue;
                }

                if (ComponentDetectionConventions.IsComponent(classSymbol, icomponentType))
                {
                    return true;
                }
            }

            return false;
        }

        // Virtual for temporary VSCode workaround
        protected virtual void InitializeSolution(Solution solution)
        {
            Debug.Assert(solution != null);

            foreach (var project in solution.Projects)
            {
                if (TryGetProjectSnapshot(project?.FilePath, out var projectSnapshot))
                {
                    _workspaceStateGenerator.Update(project, projectSnapshot);
                }
            }
        }

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            if (args.Kind == ProjectChangeKind.ProjectAdded)
            {
                var associatedWorkspaceProject = _projectManager
                    .Workspace
                    .CurrentSolution
                    .Projects
                    .FirstOrDefault(project => FilePathComparer.Instance.Equals(args.ProjectFilePath, project.FilePath));

                if (associatedWorkspaceProject != null)
                {
                    _workspaceStateGenerator.Update(associatedWorkspaceProject, args.Newer);
                }
            }
        }

        private void EnqueueUpdate(ProjectId projectId)
        {
            // A race is not possible here because we use the main thread to synchronize the updates
            // by capturing the sync context.
            if (!_deferredUpdates.TryGetValue(projectId, out var update) || update.IsCompleted)
            {
                _deferredUpdates[projectId] = UpdateAfterDelay(projectId);
            }
        }

        private async Task UpdateAfterDelay(ProjectId projectId)
        {
            await Task.Delay(EnqueueDelay);

            OnStartingDelayedUpdate();

            var solution = _projectManager.Workspace.CurrentSolution;
            var workspaceProject = solution.GetProject(projectId);
            if (workspaceProject != null && TryGetProjectSnapshot(workspaceProject.FilePath, out var projectSnapshot))
            {
                _workspaceStateGenerator.Update(workspaceProject, projectSnapshot);
            }
        }

        private bool TryGetProjectSnapshot(string projectFilePath, out ProjectSnapshot projectSnapshot)
        {
            if (projectFilePath == null)
            {
                projectSnapshot = null;
                return false;
            }

            projectSnapshot = _projectManager.GetLoadedProject(projectFilePath);
            return projectSnapshot != null;
        }
    }
}
