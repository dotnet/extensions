// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Moq;
using OmniSharp.MSBuild.Logging;
using OmniSharp.MSBuild.Notification;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class TagHelperRefreshTriggerTest : OmniSharpTestBase
    {
        public TagHelperRefreshTriggerTest()
        {
            Workspace = TestWorkspace.Create();
            var projectRoot1 = ProjectRootElement.Create("/path/to/project.csproj");
            Project1Instance = new ProjectInstance(projectRoot1);
            ProjectManager = CreateProjectSnapshotManager();
            Project1 = new OmniSharpHostProject(projectRoot1.ProjectFileLocation.File, RazorConfiguration.Default, "TestRootNamespace");

            var solution = Workspace.CurrentSolution.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "Project1",
                    "Project1",
                    LanguageNames.CSharp,
                    filePath: Project1.FilePath));
            Workspace.TryApplyChanges(solution);
        }

        public TimeSpan WaitDelay
        {
            get
            {
                if (Debugger.IsAttached)
                {
                    return TimeSpan.MaxValue;
                }

                return TimeSpan.FromMilliseconds(250);
            }
        }

        public Workspace Workspace { get; }

        public OmniSharpProjectSnapshotManagerBase ProjectManager { get; }

        public OmniSharpHostProject Project1 { get; }

        public ProjectInstance Project1Instance { get; }

        public ImmutableArray<MSBuildDiagnostic> EmptyDiagnostics => Enumerable.Empty<MSBuildDiagnostic>().ToImmutableArray();

        [Fact]
        public async Task ProjectLoaded_TriggersUpdate()
        {
            // Arrange
            await RunOnForegroundAsync(() => ProjectManager.ProjectAdded(Project1));
            var mre = new ManualResetEventSlim(initialState: false);
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Callback<Project, OmniSharpProjectSnapshot>((_, __) => mre.Set());
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object);
            var args = new ProjectLoadedEventArgs(
                null,
                Project1Instance,
                EmptyDiagnostics,
                isReload: false,
                projectIdIsDefinedInSolution: false,
                sourceFiles: Enumerable.Empty<string>().ToImmutableArray());

            // Act
            refreshTrigger.ProjectLoaded(args);

            // Assert
            var result = mre.Wait(WaitDelay);
            Assert.True(result);
        }

        [Fact]
        public async Task ProjectLoaded_BatchesUpdates()
        {
            // Arrange
            await RunOnForegroundAsync(() => ProjectManager.ProjectAdded(Project1));
            var mre = new ManualResetEventSlim(initialState: false);
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Callback<Project, OmniSharpProjectSnapshot>((_, __) =>
                {
                    if (mre.IsSet)
                    {
                        throw new XunitException("Should not have been called twice.");
                    }

                    mre.Set();
                });
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object, enqueueDelay: 10);
            var args = new ProjectLoadedEventArgs(
                null,
                Project1Instance,
                EmptyDiagnostics,
                isReload: false,
                projectIdIsDefinedInSolution: false,
                sourceFiles: Enumerable.Empty<string>().ToImmutableArray());

            // Act
            refreshTrigger.ProjectLoaded(args);
            refreshTrigger.ProjectLoaded(args);
            refreshTrigger.ProjectLoaded(args);
            refreshTrigger.ProjectLoaded(args);

            // Assert
            var result = mre.Wait(WaitDelay);
            Assert.True(result);
        }

        [Fact]
        public async Task RazorDocumentOutputChanged_TriggersUpdate()
        {
            // Arrange
            await RunOnForegroundAsync(() => ProjectManager.ProjectAdded(Project1));
            var mre = new ManualResetEventSlim(initialState: false);
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Callback<Project, OmniSharpProjectSnapshot>((_, __) => mre.Set());
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object);
            var args = new RazorFileChangeEventArgs("/path/to/obj/file.cshtml.g.cs", "obj/file.cshtml.g.cs", Project1Instance, RazorFileChangeKind.Added);

            // Act
            refreshTrigger.RazorDocumentOutputChanged(args);

            // Assert
            var result = mre.Wait(WaitDelay);
            Assert.True(result);
        }

        [Fact]
        public async Task RazorDocumentOutputChanged_BatchesUpdates()
        {
            // Arrange
            await RunOnForegroundAsync(() => ProjectManager.ProjectAdded(Project1));
            var mre = new ManualResetEventSlim(initialState: false);
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Callback<Project, OmniSharpProjectSnapshot>((_, __) =>
                {
                    if (mre.IsSet)
                    {
                        throw new XunitException("Should not have been called twice.");
                    }

                    mre.Set();
                });
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object, enqueueDelay: 10);
            var args = new RazorFileChangeEventArgs("/path/to/obj/file.cshtml.g.cs", "obj/file.cshtml.g.cs", Project1Instance, RazorFileChangeKind.Added);

            // Act
            refreshTrigger.RazorDocumentOutputChanged(args);
            refreshTrigger.RazorDocumentOutputChanged(args);
            refreshTrigger.RazorDocumentOutputChanged(args);
            refreshTrigger.RazorDocumentOutputChanged(args);

            // Assert
            var result = mre.Wait(WaitDelay);
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAfterDelayAsync_NoWorkspaceProject_Noops()
        {
            // Arrange
            var workspace = TestWorkspace.Create();
            var projectManager = CreateProjectSnapshotManager();
            await RunOnForegroundAsync(() => ProjectManager.ProjectAdded(Project1));
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Throws<XunitException>();
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object, workspace);

            // Act & Assert
            await RunOnForegroundAsync(() => refreshTrigger.UpdateAfterDelayAsync(Project1.FilePath));
        }

        [Fact]
        public async Task UpdateAfterDelayAsync_NoProjectSnapshot_Noops()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var workspaceStateGenerator = new Mock<OmniSharpProjectWorkspaceStateGenerator>();
            workspaceStateGenerator.Setup(generator => generator.Update(It.IsAny<Project>(), It.IsAny<OmniSharpProjectSnapshot>()))
                .Throws<XunitException>();
            var refreshTrigger = CreateRefreshTrigger(workspaceStateGenerator.Object);

            // Act & Assert
            await RunOnForegroundAsync(() => refreshTrigger.UpdateAfterDelayAsync(Project1Instance.ProjectFileLocation.File));
        }

        private TagHelperRefreshTrigger CreateRefreshTrigger(OmniSharpProjectWorkspaceStateGenerator workspaceStateGenerator, Workspace workspace = null, int enqueueDelay = 1)
        {
            workspace = workspace ?? Workspace;
            var refreshTrigger = new TagHelperRefreshTrigger(Dispatcher, workspace, workspaceStateGenerator)
            {
                EnqueueDelay = enqueueDelay,
            };

            refreshTrigger.Initialize(ProjectManager);

            return refreshTrigger;
        }
    }
}
