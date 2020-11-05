// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Refactoring.Test
{
    public class RazorComponentRenameEndpointTest : LanguageServerTestBase
    {
        private readonly RazorComponentRenameEndpoint _endpoint;

        public RazorComponentRenameEndpointTest()
        {
            _endpoint = CreateEndpoint();
        }

        [Fact]
        public async Task Handle_Rename_FileManipulationNotSupported_ReturnsNull()
        {
            // Arrange
            var languageServerFeatureOptions = Mock.Of<LanguageServerFeatureOptions>(options => options.SupportsFileManipulation == false);
            var endpoint = CreateEndpoint(languageServerFeatureOptions);
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/First/Component1.razor")
                },
                Position = new Position(2, 1),
                NewName = "Component5"
            };

            // Act
            var result = await endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Rename_WithNamespaceDirective()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/First/Component1.razor")
                },
                Position = new Position(2, 1),
                NewName = "Component5"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.DocumentChanges.Count());
            var renameChange = result.DocumentChanges.ElementAt(0);
            Assert.True(renameChange.IsRenameFile);
            Assert.Equal("file:///c:/First/Component2.razor", renameChange.RenameFile.OldUri);
            Assert.Equal("file:///c:/First/Component5.razor", renameChange.RenameFile.NewUri);
            var editChange = result.DocumentChanges.ElementAt(1);
            Assert.True(editChange.IsTextDocumentEdit);
            Assert.Equal("file:///c:/First/Component1.razor", editChange.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Collection(
                editChange.TextDocumentEdit.Edits,
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(2, edit.Range.Start.Line);
                    Assert.Equal(1, edit.Range.Start.Character);
                    Assert.Equal(2, edit.Range.End.Line);
                    Assert.Equal(11, edit.Range.End.Character);
                },
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(2, edit.Range.Start.Line);
                    Assert.Equal(14, edit.Range.Start.Character);
                    Assert.Equal(2, edit.Range.End.Line);
                    Assert.Equal(24, edit.Range.End.Character);
                });
        }

        [Fact]
        public async Task Handle_Rename_OnComponentParameter_ReturnsNull()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/ComponentWithParam.razor")
                },
                Position = new Position(1, 14),
                NewName = "Test2"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Rename_OnOpeningBrace_ReturnsNull()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/ComponentWithParam.razor")
                },
                Position = new Position(1, 0),
                NewName = "Test2"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Rename_OnComponentNameLeadingEdge_ReturnsResult()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/ComponentWithParam.razor")
                },
                Position = new Position(1, 1),
                NewName = "Test2"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Handle_Rename_OnComponentName_ReturnsResult()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/ComponentWithParam.razor")
                },
                Position = new Position(1, 3),
                NewName = "Test2"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Handle_Rename_OnComponentNameTrailingEdge_ReturnsResult()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/ComponentWithParam.razor")
                },
                Position = new Position(1, 10),
                NewName = "Test2"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Handle_Rename_FullyQualifiedAndNot()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/First/Index.razor")
                },
                Position = new Position(2, 1),
                NewName = "Component5"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.DocumentChanges.Count());
            var renameChange = result.DocumentChanges.ElementAt(0);
            Assert.True(renameChange.IsRenameFile);
            Assert.Equal("file:///c:/First/Component1337.razor", renameChange.RenameFile.OldUri);
            Assert.Equal("file:///c:/First/Component5.razor", renameChange.RenameFile.NewUri);
            var editChange1 = result.DocumentChanges.ElementAt(1);
            Assert.True(editChange1.IsTextDocumentEdit);
            Assert.Equal("file:///c:/First/Index.razor", editChange1.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Collection(
                editChange1.TextDocumentEdit.Edits,
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(2, edit.Range.Start.Line);
                    Assert.Equal(1, edit.Range.Start.Character);
                    Assert.Equal(2, edit.Range.End.Line);
                    Assert.Equal(14, edit.Range.End.Character);
                },
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(2, edit.Range.Start.Line);
                    Assert.Equal(17, edit.Range.Start.Character);
                    Assert.Equal(2, edit.Range.End.Line);
                    Assert.Equal(30, edit.Range.End.Character);
                });

            var editChange2 = result.DocumentChanges.ElementAt(2);
            Assert.True(editChange2.IsTextDocumentEdit);
            Assert.Equal("file:///c:/First/Index.razor", editChange2.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Collection(
                editChange2.TextDocumentEdit.Edits,
                edit =>
                {
                    Assert.Equal("Test.Component5", edit.NewText);
                    Assert.Equal(3, edit.Range.Start.Line);
                    Assert.Equal(1, edit.Range.Start.Character);
                    Assert.Equal(3, edit.Range.End.Line);
                    Assert.Equal(19, edit.Range.End.Character);
                },
                edit =>
                {
                    Assert.Equal("Test.Component5", edit.NewText);
                    Assert.Equal(3, edit.Range.Start.Line);
                    Assert.Equal(22, edit.Range.Start.Character);
                    Assert.Equal(3, edit.Range.End.Line);
                    Assert.Equal(40, edit.Range.End.Character);
                });
        }

        [Fact]
        public async Task Handle_Rename_MultipleFileUsages()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Second/Component3.razor")
                },
                Position = new Position(1, 1),
                NewName = "Component5"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.DocumentChanges.Count());
            var renameChange = result.DocumentChanges.ElementAt(0);
            Assert.True(renameChange.IsRenameFile);
            Assert.Equal("file:///c:/Second/Component3.razor", renameChange.RenameFile.OldUri);
            Assert.Equal("file:///c:/Second/Component5.razor", renameChange.RenameFile.NewUri);
            var editChange1 = result.DocumentChanges.ElementAt(1);
            Assert.True(editChange1.IsTextDocumentEdit);
            Assert.Equal("file:///c:/Second/Component3.razor", editChange1.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Collection(
                editChange1.TextDocumentEdit.Edits,
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(1, edit.Range.Start.Line);
                    Assert.Equal(1, edit.Range.Start.Character);
                    Assert.Equal(1, edit.Range.End.Line);
                    Assert.Equal(11, edit.Range.End.Character);
                },
                edit =>
                {
                    Assert.Equal("Component5", edit.NewText);
                    Assert.Equal(1, edit.Range.Start.Line);
                    Assert.Equal(14, edit.Range.Start.Character);
                    Assert.Equal(1, edit.Range.End.Line);
                    Assert.Equal(24, edit.Range.End.Character);
                });
            var editChange2 = result.DocumentChanges.ElementAt(2);
            Assert.True(editChange2.IsTextDocumentEdit);
            Assert.Equal("file:///c:/Second/Component4.razor", editChange2.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Equal(2, editChange2.TextDocumentEdit.Edits.Count());
        }

        [Fact]
        public async Task Handle_Rename_DifferentDirectories()
        {
            // Arrange
            var request = new RenameParams
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = new Uri("file:///c:/Dir1/Directory1.razor")
                },
                Position = new Position(1, 1),
                NewName = "TestComponent"
            };

            // Act
            var result = await _endpoint.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.DocumentChanges.Count());
            var renameChange = result.DocumentChanges.ElementAt(0);
            Assert.True(renameChange.IsRenameFile);
            Assert.Equal("file:///c:/Dir2/Directory2.razor", renameChange.RenameFile.OldUri);
            Assert.Equal("file:///c:/Dir2/TestComponent.razor", renameChange.RenameFile.NewUri);
            var editChange = result.DocumentChanges.ElementAt(1);
            Assert.True(editChange.IsTextDocumentEdit);
            Assert.Equal("file:///c:/Dir1/Directory1.razor", editChange.TextDocumentEdit.TextDocument.Uri.ToString());
            Assert.Collection(
                editChange.TextDocumentEdit.Edits,
                edit =>
                {
                    Assert.Equal("TestComponent", edit.NewText);
                    Assert.Equal(1, edit.Range.Start.Line);
                    Assert.Equal(1, edit.Range.Start.Character);
                    Assert.Equal(1, edit.Range.End.Line);
                    Assert.Equal(11, edit.Range.End.Character);
                },
                edit =>
                {
                    Assert.Equal("TestComponent", edit.NewText);
                    Assert.Equal(1, edit.Range.Start.Line);
                    Assert.Equal(14, edit.Range.Start.Character);
                    Assert.Equal(1, edit.Range.End.Line);
                    Assert.Equal(24, edit.Range.End.Character);
                });
        }

        private static IEnumerable<TagHelperDescriptor> CreateRazorComponentTagHelperDescriptors(string assemblyName, string namespaceName, string tagName)
        {
            var fullyQualifiedName = $"{namespaceName}.{tagName}";
            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, fullyQualifiedName, assemblyName);
            builder.TagMatchingRule(rule => rule.TagName = tagName);
            builder.SetTypeName(fullyQualifiedName);
            yield return builder.Build();

            var fullyQualifiedBuilder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, fullyQualifiedName, assemblyName);
            fullyQualifiedBuilder.TagMatchingRule(rule => rule.TagName = fullyQualifiedName);
            fullyQualifiedBuilder.SetTypeName(fullyQualifiedName);
            fullyQualifiedBuilder.AddMetadata(ComponentMetadata.Component.NameMatchKey, ComponentMetadata.Component.FullyQualifiedNameMatch);
            yield return fullyQualifiedBuilder.Build();
        }

        private static TestRazorProjectItem CreateProjectItem(string text, string filePath)
        {
            return new TestRazorProjectItem(filePath, fileKind: FileKinds.Component)
            {
                Content = text
            };
        }

        private DocumentSnapshot CreateRazorDocumentSnapshot(RazorProjectEngine projectEngine, TestRazorProjectItem item, string rootNamespaceName, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            var codeDocument = projectEngine.ProcessDesignTime(item);
            
            var namespaceNode = (NamespaceDeclarationIntermediateNode)codeDocument
                .GetDocumentIntermediateNode()
                .FindDescendantNodes<IntermediateNode>()
                .FirstOrDefault(n => n is NamespaceDeclarationIntermediateNode);
            namespaceNode.Content = rootNamespaceName;

            var sourceText = SourceText.From(new string(item.Content));
            var projectWorkspaceState = new ProjectWorkspaceState(tagHelpers, LanguageVersion.Default);
            var projectSnapshot = TestProjectSnapshot.Create("C:/project.csproj", projectWorkspaceState);
            var documentSnapshot = Mock.Of<DocumentSnapshot>(d =>
                d.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                d.FilePath == item.FilePath &&
                d.FileKind == FileKinds.Component &&
                d.GetTextAsync() == Task.FromResult(sourceText) &&
                d.Project == projectSnapshot);
            return documentSnapshot;
        }

        private RazorComponentRenameEndpoint CreateEndpoint(LanguageServerFeatureOptions languageServerFeatureOptions = null)
        {
            var tagHelperDescriptors = new List<TagHelperDescriptor>();
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("First", "First.Components", "Component1"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("First", "Test", "Component2"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("Second", "Second.Components", "Component3"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("Second", "Second.Components", "Component4"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("First", "Test", "Component1337"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("First", "Test.Components", "Directory1"));
            tagHelperDescriptors.AddRange(CreateRazorComponentTagHelperDescriptors("First", "Test.Components", "Directory2"));

            var item1 = CreateProjectItem("@namespace First.Components\n@using Test\n<Component2></Component2>", "c:/First/Component1.razor");
            var item2 = CreateProjectItem("@namespace Test", "c:/First/Component2.razor");
            var item3 = CreateProjectItem("@namespace Second.Components\n<Component3></Component3>", "c:/Second/Component3.razor");
            var item4 = CreateProjectItem("@namespace Second.Components\n<Component3></Component3>\n<Component3></Component3>", "c:/Second/Component4.razor");
            var itemComponentParam = CreateProjectItem("@namespace Second.Components\n<Component3 Title=\"Something\"></Component3>", "c:/Second/Component5.razor");
            var item1337 = CreateProjectItem(string.Empty, "c:/First/Component1337.razor");
            var indexItem = CreateProjectItem("@namespace First.Components\n@using Test\n<Component1337></Component1337>\n<Test.Component1337></Test.Component1337>", "c:/First/Index.razor");

            var itemDirectory1 = CreateProjectItem("@namespace Test.Components\n<Directory2></Directory2>", "c:/Dir1/Directory1.razor");
            var itemDirectory2 = CreateProjectItem("@namespace Test.Components\n<Directory1></Directory1>", "c:/Dir2/Directory2.razor");

            var fileSystem = new TestRazorProjectFileSystem(new[] { item1, item2, item3, item4, itemComponentParam, indexItem, itemDirectory1, itemDirectory2 });

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder => {
                builder.AddDirective(NamespaceDirective.Directive);
                builder.AddTagHelpers(tagHelperDescriptors);
            });
            
            var component1 = CreateRazorDocumentSnapshot(projectEngine, item1, "First.Components", tagHelperDescriptors);
            var component2 = CreateRazorDocumentSnapshot(projectEngine, item2, "Test", tagHelperDescriptors);
            var component3 = CreateRazorDocumentSnapshot(projectEngine, item3, "Second.Components", tagHelperDescriptors);
            var component4 = CreateRazorDocumentSnapshot(projectEngine, item4, "Second.Components", tagHelperDescriptors);
            var componentWithParam = CreateRazorDocumentSnapshot(projectEngine, itemComponentParam, "Second.Components", tagHelperDescriptors);
            var component1337 = CreateRazorDocumentSnapshot(projectEngine, item1337, "Test", tagHelperDescriptors);
            var index = CreateRazorDocumentSnapshot(projectEngine, indexItem, "First.Components", tagHelperDescriptors);
            var directory1Component = CreateRazorDocumentSnapshot(projectEngine, itemDirectory1, "Test.Components", tagHelperDescriptors);
            var directory2Component = CreateRazorDocumentSnapshot(projectEngine, itemDirectory2, "Test.Components", tagHelperDescriptors);

            var firstProject = Mock.Of<ProjectSnapshot>(p =>
                p.FilePath == "c:/First/First.csproj" &&
                p.DocumentFilePaths == new[] { "c:/First/Component1.razor", "c:/First/Component2.razor", itemDirectory1.FilePath, itemDirectory2.FilePath, component1337.FilePath } &&
                p.GetDocument("c:/First/Component1.razor") == component1 &&
                p.GetDocument("c:/First/Component2.razor") == component2 &&
                p.GetDocument(itemDirectory1.FilePath) == directory1Component &&
                p.GetDocument(itemDirectory2.FilePath) == directory2Component &&
                p.GetDocument(component1337.FilePath) == component1337);

            var secondProject = Mock.Of<ProjectSnapshot>(p =>
                p.FilePath == "c:/Second/Second.csproj" &&
                p.DocumentFilePaths == new[] { "c:/Second/Component3.razor", "c:/Second/Component4.razor", index.FilePath } &&
                p.GetDocument("c:/Second/Component3.razor") == component3 &&
                p.GetDocument("c:/Second/Component4.razor") == component4 &&
                p.GetDocument("c:/Second/ComponentWithParam.razor") == componentWithParam &&
                p.GetDocument(index.FilePath) == index);

            var projectSnapshotManager = Mock.Of<ProjectSnapshotManagerBase>(p => p.Projects == new[] { firstProject, secondProject });
            var projectSnapshotManagerAccessor = new TestProjectSnapshotManagerAccessor(projectSnapshotManager);

            var documentResolver = Mock.Of<DocumentResolver>(d =>
                d.TryResolveDocument("c:/First/Component1.razor", out component1) == true &&
                d.TryResolveDocument("c:/First/Component2.razor", out component2) == true &&
                d.TryResolveDocument("c:/Second/Component3.razor", out component3) == true &&
                d.TryResolveDocument("c:/Second/Component4.razor", out component4) == true &&
                d.TryResolveDocument("c:/Second/ComponentWithParam.razor", out componentWithParam) == true &&
                d.TryResolveDocument(index.FilePath, out index) == true &&
                d.TryResolveDocument(component1337.FilePath, out component1337) == true &&
                d.TryResolveDocument(itemDirectory1.FilePath, out directory1Component) == true &&
                d.TryResolveDocument(itemDirectory2.FilePath, out directory2Component) == true);

            var searchEngine = new DefaultRazorComponentSearchEngine(Dispatcher, projectSnapshotManagerAccessor);
            languageServerFeatureOptions = languageServerFeatureOptions ?? Mock.Of<LanguageServerFeatureOptions>(options => options.SupportsFileManipulation == true);
            var endpoint = new RazorComponentRenameEndpoint(Dispatcher, documentResolver, searchEngine, projectSnapshotManagerAccessor, languageServerFeatureOptions);
            return endpoint;
        }

        internal class TestProjectSnapshotManagerAccessor : ProjectSnapshotManagerAccessor
        {
            public TestProjectSnapshotManagerAccessor(ProjectSnapshotManagerBase instance)
            {
                Instance = instance;
            }

            public override ProjectSnapshotManagerBase Instance { get; }
        }
    }
}
