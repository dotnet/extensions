// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultDocumentDivergenceCheckerTest : WorkspaceTestBase
    {
        public DefaultDocumentDivergenceCheckerTest()
        {
            ProjectSnapshotManager = new TestProjectSnapshotManager(Workspace);
            HostProject = new HostProject("C:/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            ProjectSnapshotManager.ProjectAdded(HostProject);
            var cshtmlDocument = new HostDocument("C:/path/to/file1.cshtml", "file1.cshtml", FileKinds.Legacy);
            ProjectSnapshotManager.DocumentAdded(HostProject, cshtmlDocument, new EmptyTextLoader(cshtmlDocument.FilePath));
            var componentDocument = new HostDocument("C:/path/to/file2.razor", "file2.razor", FileKinds.Component);
            ProjectSnapshotManager.DocumentAdded(HostProject, componentDocument, new EmptyTextLoader(componentDocument.FilePath));
            var importDocument = new HostDocument("C:/path/to/_Imports.razor", "_Imports.razor", FileKinds.ComponentImport);
            ProjectSnapshotManager.DocumentAdded(HostProject, importDocument, new EmptyTextLoader(importDocument.FilePath));

            var project = ProjectSnapshotManager.GetLoadedProject(HostProject.FilePath);
            CSHTMLDocument = project.GetDocument(cshtmlDocument.FilePath);
            ComponentDocument = project.GetDocument(componentDocument.FilePath);
            ImportDocument = project.GetDocument(importDocument.FilePath);
        }

        private TestProjectSnapshotManager ProjectSnapshotManager { get; }

        private HostProject HostProject { get; }

        private DocumentSnapshot CSHTMLDocument { get; }

        private DocumentSnapshot ComponentDocument { get; }

        private DocumentSnapshot ImportDocument { get; }

        [Fact]
        public void PossibleDivergence_NonComponent_ReturnsTrue()
        {
            // Arrange
            var old = CSHTMLDocument;
            var @new = UpdateDocument(CSHTMLDocument.FilePath, SourceText.From(string.Empty));
            var impactChecker = new DefaultDocumentDivergenceChecker();

            // Act
            var result = impactChecker.PossibleDivergence(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PossibleDivergence_Import_ReturnsTrue()
        {
            // Arrange
            var old = ImportDocument;
            var @new = UpdateDocument(ImportDocument.FilePath, SourceText.From(string.Empty));
            var impactChecker = new DefaultDocumentDivergenceChecker();

            // Act
            var result = impactChecker.PossibleDivergence(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_Component_UnretrievableNewSourceText_ReturnsTrue()
        {
            // Arrange
            var old = ComponentDocument;
            var @new = UpdateDocument(ComponentDocument.FilePath, SourceText.From(string.Empty));

            // Prime the documents source text
            await old.GetTextAsync();

            var impactChecker = new DefaultDocumentDivergenceChecker();

            // Act
            var result = impactChecker.PossibleDivergence(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_Component_NoDirectives_ReturnsFalse()
        {
            // Arrange
            var old = ComponentDocument;
            var @new = UpdateDocument(ComponentDocument.FilePath, SourceText.From(string.Empty));

            // Prime the documents source texts
            await old.GetTextAsync();
            await @new.GetTextAsync();

            var impactChecker = new DefaultDocumentDivergenceChecker();

            // Act
            var result = impactChecker.PossibleDivergence(old, @new);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PossibleDivergence_Component_IdenticalProperties_ReturnsFalse()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    private int _field;

    [Parameter]
    public int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_SomeSameProperties_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }

    public string SomeProperty2 { get; set; }
}";
            var @new =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }

    [Parameter]
    public int SomeProperty2 { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_Count_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_Identifier_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Parameter]
    public int AnotherProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_Type_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Parameter]
    public in SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_ModifierCount_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    private int SomeProperty { get; set; }
}";
            var @new = 
@"@code {
    [Parameter]
    private protected int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_ModifierType_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Parameter]
    private int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_AttributeListCount_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Bind]
    [Parameter]
    public int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_AttributeCount_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Parameter, Bind]
    public int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_PropertyDifference_DifferentAttributes_ReturnsTrue()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
}";
            var @new =
@"@code {
    [Bind]
    public int SomeProperty { get; set; }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task PossibleDivergence_Complex_ReturnsFalse()
        {
            // Arrange
            var old =
@"@code {
    [Parameter]
    public int SomeProperty { get; set; }
    public string Something { get; }
}";
            var @new =
@"@code {
    private int _field;

    [Parameter]
    public int SomeProperty { get; set; }

    public void SetField(int value)
    {
        _field = value;
    }

    public string Something { get; }

    private class NestedClass
    {
        public bool Checked { get; set; }
    }
}";

            // Act
            var result = await GetDivergenceAsync(old, @new);

            // Assert
            Assert.False(result);
        }

        private async Task<bool> GetDivergenceAsync(string before, string after)
        {
            var old = UpdateDocument(ComponentDocument.FilePath, SourceText.From(before));
            var @new = UpdateDocument(ComponentDocument.FilePath, SourceText.From(after));

            // Prime the documents source texts
            await @new.GetTextAsync();

            var impactChecker = new DefaultDocumentDivergenceChecker();
            var result = impactChecker.PossibleDivergence(old, @new);

            return result;
        }

        private DocumentSnapshot UpdateDocument(string filePath, SourceText sourceText)
        {
            ProjectSnapshotManager.DocumentChanged(HostProject.FilePath, filePath, sourceText);
            var project = ProjectSnapshotManager.GetLoadedProject(HostProject.FilePath);
            var document = project.GetDocument(filePath);
            return document;
        }
    }
}
