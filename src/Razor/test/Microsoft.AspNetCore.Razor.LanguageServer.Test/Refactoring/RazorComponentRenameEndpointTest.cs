// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
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
            Assert.Equal(2, editChange.TextDocumentEdit.Edits.Count());
            var editChangeEdit1 = editChange.TextDocumentEdit.Edits.ElementAt(0);
            Assert.Equal("Component5", editChangeEdit1.NewText);
            Assert.Equal(2, editChangeEdit1.Range.Start.Line);
            Assert.Equal(1, editChangeEdit1.Range.Start.Character);
            Assert.Equal(2, editChangeEdit1.Range.End.Line);
            Assert.Equal(11, editChangeEdit1.Range.End.Character);
            var editChangeEdit2 = editChange.TextDocumentEdit.Edits.ElementAt(1);
            Assert.Equal("Component5", editChangeEdit2.NewText);
            Assert.Equal(2, editChangeEdit2.Range.Start.Line);
            Assert.Equal(14, editChangeEdit2.Range.Start.Character);
            Assert.Equal(2, editChangeEdit2.Range.End.Line);
            Assert.Equal(24, editChangeEdit2.Range.End.Character);
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
            Assert.Equal(2, editChange1.TextDocumentEdit.Edits.Count());
            var editChange1Edit1 = editChange1.TextDocumentEdit.Edits.ElementAt(0);
            Assert.Equal("Component5", editChange1Edit1.NewText);
            Assert.Equal(1, editChange1Edit1.Range.Start.Line);
            Assert.Equal(1, editChange1Edit1.Range.Start.Character);
            Assert.Equal(1, editChange1Edit1.Range.End.Line);
            Assert.Equal(11, editChange1Edit1.Range.End.Character);
            var editChange1Edit2 = editChange1.TextDocumentEdit.Edits.ElementAt(1);
            Assert.Equal("Component5", editChange1Edit2.NewText);
            Assert.Equal(1, editChange1Edit2.Range.Start.Line);
            Assert.Equal(14, editChange1Edit2.Range.Start.Character);
            Assert.Equal(1, editChange1Edit2.Range.End.Line);
            Assert.Equal(24, editChange1Edit2.Range.End.Character);
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
            Assert.Equal(2, editChange.TextDocumentEdit.Edits.Count());
            var editChangeEdit1 = editChange.TextDocumentEdit.Edits.ElementAt(0);
            Assert.Equal("TestComponent", editChangeEdit1.NewText);
            Assert.Equal(1, editChangeEdit1.Range.Start.Line);
            Assert.Equal(1, editChangeEdit1.Range.Start.Character);
            Assert.Equal(1, editChangeEdit1.Range.End.Line);
            Assert.Equal(11, editChangeEdit1.Range.End.Character);
            var editChangeEdit2 = editChange.TextDocumentEdit.Edits.ElementAt(1);
            Assert.Equal("TestComponent", editChangeEdit2.NewText);
            Assert.Equal(1, editChangeEdit2.Range.Start.Line);
            Assert.Equal(14, editChangeEdit2.Range.Start.Character);
            Assert.Equal(1, editChangeEdit2.Range.End.Line);
            Assert.Equal(24, editChangeEdit2.Range.End.Character);
        }

        private static TagHelperDescriptor CreateRazorComponentTagHelperDescriptor(string assemblyName, string namespaceName, string tagName)
        {
            var fullyQualifiedName = $"{namespaceName}.{tagName}";
            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, fullyQualifiedName, assemblyName);
            builder.TagMatchingRule(rule => rule.TagName = tagName);
            builder.SetTypeName(fullyQualifiedName);
            return builder.Build();
        }

        private static TestRazorProjectItem CreateProjectItem(string text, string filePath)
        {
            return new TestRazorProjectItem(filePath, fileKind: FileKinds.Component)
            {
                Content = text
            };
        }

        private DocumentSnapshot CreateRazorDocumentSnapshot(RazorProjectEngine projectEngine, TestRazorProjectItem item, string rootNamespaceName)
        {
            var codeDocument = projectEngine.ProcessDesignTime(item);

            var namespaceNode = (NamespaceDeclarationIntermediateNode)codeDocument
                .GetDocumentIntermediateNode()
                .FindDescendantNodes<IntermediateNode>()
                .FirstOrDefault(n => n is NamespaceDeclarationIntermediateNode);
            namespaceNode.Content = rootNamespaceName;

            var sourceText = SourceText.From(new string(item.Content));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(d =>
                d.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                d.FilePath == item.FilePath &&
                d.FileKind == FileKinds.Component &&
                d.GetTextAsync() == Task.FromResult(sourceText));
            return documentSnapshot;
        }

        private RazorComponentRenameEndpoint CreateEndpoint(LanguageServerFeatureOptions languageServerFeatureOptions = null)
        {
            var tag1 = CreateRazorComponentTagHelperDescriptor("First", "First.Components", "Component1");
            var tag2 = CreateRazorComponentTagHelperDescriptor("First", "Test", "Component2");
            var tag3 = CreateRazorComponentTagHelperDescriptor("Second", "Second.Components", "Component3");
            var tag4 = CreateRazorComponentTagHelperDescriptor("Second", "Second.Components", "Component4");
            var directory1 = CreateRazorComponentTagHelperDescriptor("First", "Test.Components", "Directory1");
            var directory2 = CreateRazorComponentTagHelperDescriptor("First", "Test.Components", "Directory2");
            var tagHelperDescriptors = new[] { tag1, tag2, tag3, tag4, directory1, directory2 };
                
            var item1 = CreateProjectItem("@namespace First.Components\n@using Test\n<Component2></Component2>", "c:/First/Component1.razor");
            var item2 = CreateProjectItem("@namespace Test", "c:/First/Component2.razor");
            var item3 = CreateProjectItem("@namespace Second.Components\n<Component3></Component3>", "c:/Second/Component3.razor");
            var item4 = CreateProjectItem("@namespace Second.Components\n<Component3></Component3>\n<Component3></Component3>", "c:/Second/Component4.razor");

            var itemDirectory1 = CreateProjectItem("@namespace Test.Components\n<Directory2></Directory2>", "c:/Dir1/Directory1.razor");
            var itemDirectory2 = CreateProjectItem("@namespace Test.Components\n<Directory1></Directory1>", "c:/Dir2/Directory2.razor");

            var fileSystem = new TestRazorProjectFileSystem(new[] { item1, item2, item3, item4, itemDirectory1, itemDirectory2 });

            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder => {
                builder.AddDirective(NamespaceDirective.Directive);
                builder.AddTagHelpers(tagHelperDescriptors);
            });
            
            var component1 = CreateRazorDocumentSnapshot(projectEngine, item1, "First.Components");
            var component2 = CreateRazorDocumentSnapshot(projectEngine, item2, "Test");
            var component3 = CreateRazorDocumentSnapshot(projectEngine, item3, "Second.Components");
            var component4 = CreateRazorDocumentSnapshot(projectEngine, item4, "Second.Components");
            var directory1Component = CreateRazorDocumentSnapshot(projectEngine, itemDirectory1, "Test.Components");
            var directory2Component = CreateRazorDocumentSnapshot(projectEngine, itemDirectory2, "Test.Components");

            var firstProject = Mock.Of<ProjectSnapshot>(p =>
                p.FilePath == "c:/First/First.csproj" &&
                p.DocumentFilePaths == new[] { "c:/First/Component1.razor", "c:/First/Component2.razor", itemDirectory1.FilePath, itemDirectory2.FilePath } &&
                p.GetDocument("c:/First/Component1.razor") == component1 &&
                p.GetDocument("c:/First/Component2.razor") == component2 &&
                p.GetDocument(itemDirectory1.FilePath) == directory1Component &&
                p.GetDocument(itemDirectory2.FilePath) == directory2Component);

            var secondProject = Mock.Of<ProjectSnapshot>(p =>
                p.FilePath == "c:/Second/Second.csproj" &&
                p.DocumentFilePaths == new[] { "c:/Second/Component3.razor", "c:/Second/Component4.razor" } &&
                p.GetDocument("c:/Second/Component3.razor") == component3 &&
                p.GetDocument("c:/Second/Component4.razor") == component4);

            var projectSnapshotManager = Mock.Of<ProjectSnapshotManagerBase>(p => p.Projects == new[] { firstProject, secondProject });
            var projectSnapshotManagerAccessor = new TestProjectSnapshotManagerAccessor(projectSnapshotManager);

            var documentResolver = Mock.Of<DocumentResolver>(d =>
                d.TryResolveDocument("c:/First/Component1.razor", out component1) == true &&
                d.TryResolveDocument("c:/First/Component2.razor", out component2) == true &&
                d.TryResolveDocument("c:/Second/Component3.razor", out component3) == true &&
                d.TryResolveDocument("c:/Second/Component4.razor", out component4) == true &&
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
