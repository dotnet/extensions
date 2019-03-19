// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public class DefaultProjectWorkspaceStateGeneratorTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectWorkspaceStateGeneratorTest()
        {
            var tagHelperResolver = new TestTagHelperResolver();
            tagHelperResolver.TagHelpers.Add(TagHelperDescriptorBuilder.Create("ResolvableTagHelper", "TestAssembly").Build());
            ResolvableTagHelpers = tagHelperResolver.TagHelpers;
            var workspaceServices = new List<IWorkspaceService>() { tagHelperResolver };
            var testServices = TestServices.Create(workspaceServices, Enumerable.Empty<ILanguageService>());
            Workspace = TestWorkspace.Create(testServices);
            var projectId = ProjectId.CreateNewId("Test");
            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp,
                TestProjectData.SomeProject.FilePath));
            WorkspaceProject = solution.GetProject(projectId);
            ProjectSnapshot = new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, TestProjectData.SomeProject));
            ProjectWorkspaceStateWithTagHelpers = new ProjectWorkspaceState(new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build(),
            }, default);
        }

        private IReadOnlyList<TagHelperDescriptor> ResolvableTagHelpers { get; }

        private Workspace Workspace { get; }

        private Project WorkspaceProject { get; }

        private DefaultProjectSnapshot ProjectSnapshot { get; }

        private ProjectWorkspaceState ProjectWorkspaceStateWithTagHelpers { get; }

        [ForegroundFact]
        public void Update_StartsUpdateTask()
        {
            // Arrange  
            using (var stateGenerator = new DefaultProjectWorkspaceStateGenerator(Dispatcher))
            {
                stateGenerator.BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false);

                // Act
                stateGenerator.Update(WorkspaceProject, ProjectSnapshot);

                // Assert
                var update = Assert.Single(stateGenerator._updates);
                Assert.False(update.Value.Task.IsCompleted);
            }
        }

        [ForegroundFact]
        public void Update_SoftCancelsIncompleteTaskForSameProject()
        {
            // Arrange  
            using (var stateGenerator = new DefaultProjectWorkspaceStateGenerator(Dispatcher))
            {
                stateGenerator.BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false);
                stateGenerator.Update(WorkspaceProject, ProjectSnapshot);
                var initialUpdate = stateGenerator._updates.Single().Value;

                // Act
                stateGenerator.Update(WorkspaceProject, ProjectSnapshot);

                // Assert
                Assert.True(initialUpdate.Cts.IsCancellationRequested);
            }
        }

        [ForegroundFact]
        public async Task Update_NullWorkspaceProject_ClearsProjectWorkspaceState()
        {
            // Arrange  
            using (var stateGenerator = new DefaultProjectWorkspaceStateGenerator(Dispatcher))
            {
                stateGenerator.NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false);
                var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
                stateGenerator.Initialize(projectManager);
                projectManager.ProjectAdded(ProjectSnapshot.HostProject);
                projectManager.ProjectWorkspaceStateChanged(ProjectSnapshot.FilePath, ProjectWorkspaceStateWithTagHelpers);

                // Act
                stateGenerator.Update(workspaceProject: null, ProjectSnapshot);

                // Jump off the foreground thread so the background work can complete.
                await Task.Run(() => stateGenerator.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

                // Assert
                var newProjectSnapshot = projectManager.GetLoadedProject(ProjectSnapshot.FilePath);
                Assert.Empty(newProjectSnapshot.TagHelpers);
            }
        }

        [ForegroundFact]
        public async Task Update_ResolvesTagHelpersAndUpdatesWorkspaceState()
        {
            // Arrange  
            using (var stateGenerator = new DefaultProjectWorkspaceStateGenerator(Dispatcher))
            {
                stateGenerator.NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false);
                var projectManager = new TestProjectSnapshotManager(Dispatcher, Workspace);
                stateGenerator.Initialize(projectManager);
                projectManager.ProjectAdded(ProjectSnapshot.HostProject);

                // Act
                stateGenerator.Update(WorkspaceProject, ProjectSnapshot);

                // Jump off the foreground thread so the background work can complete.
                await Task.Run(() => stateGenerator.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

                // Assert
                var newProjectSnapshot = projectManager.GetLoadedProject(ProjectSnapshot.FilePath);
                Assert.Equal(ResolvableTagHelpers, newProjectSnapshot.TagHelpers);
            }
        }
    }
}
