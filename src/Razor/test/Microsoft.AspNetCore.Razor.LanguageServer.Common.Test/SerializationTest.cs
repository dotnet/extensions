// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class SerializationTest
    {
        public SerializationTest()
        {
            var languageVersion = RazorLanguageVersion.Experimental;
            var extensions = new RazorExtension[]
            {
                new SerializedRazorExtension("TestExtension"),
            };
            Configuration = RazorConfiguration.Create(languageVersion, "Custom", extensions);
            ProjectWorkspaceState = new ProjectWorkspaceState(new[]
            {
                TagHelperDescriptorBuilder.Create("Test", "TestAssembly").Build(),
            },
            LanguageVersion.LatestMajor);
            var converterCollection = new JsonConverterCollection();
            converterCollection.RegisterRazorConverters();
            Converters = converterCollection.ToArray();
        }

        private RazorConfiguration Configuration { get; }

        private ProjectWorkspaceState ProjectWorkspaceState { get; }

        private JsonConverter[] Converters { get; }

        [Fact]
        public void FullProjectSnapshotHandle_InvalidSerializationFormat_SerializesToNull()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
                Configuration,
                rootNamespace: "TestProject",
                ProjectWorkspaceState,
                Array.Empty<DocumentSnapshotHandle>());
            var serializedHandle = JsonConvert.SerializeObject(handle, Converters);
            var serializedJObject = JObject.Parse(serializedHandle);
            serializedJObject["SerializationFormat"] = "INVALID";
            var reserializedHandle = JsonConvert.SerializeObject(serializedJObject);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(reserializedHandle, Converters);

            // Assert
            Assert.Null(deserializedHandle);
        }

        [Fact]
        public void FullProjectSnapshotHandle_MissingSerializationFormat_SerializesToNull()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
                Configuration,
                rootNamespace: "TestProject",
                ProjectWorkspaceState,
                Array.Empty<DocumentSnapshotHandle>());
            var serializedHandle = JsonConvert.SerializeObject(handle, Converters);
            var serializedJObject = JObject.Parse(serializedHandle);
            serializedJObject.Remove("SerializationFormat");
            var reserializedHandle = JsonConvert.SerializeObject(serializedJObject);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(reserializedHandle, Converters);

            // Assert
            Assert.Null(deserializedHandle);
        }

        [Fact]
        public void FullProjectSnapshotHandle_CanRoundTrip()
        {
            // Arrange
            var legacyDocument = new DocumentSnapshotHandle("/path/to/file.cshtml", "file.cshtml", FileKinds.Legacy);
            var componentDocument = new DocumentSnapshotHandle("/path/to/otherfile.razor", "otherfile.razor", FileKinds.Component);
            var handle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
                Configuration,
                rootNamespace: "TestProject",
                ProjectWorkspaceState,
                new[] { legacyDocument, componentDocument });
            var serializedHandle = JsonConvert.SerializeObject(handle, Converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(serializedHandle, Converters);

            // Assert
            Assert.Equal(handle.FilePath, deserializedHandle.FilePath);
            Assert.Equal(handle.Configuration, deserializedHandle.Configuration);
            Assert.Equal(handle.RootNamespace, deserializedHandle.RootNamespace);
            Assert.Equal(handle.ProjectWorkspaceState, deserializedHandle.ProjectWorkspaceState);
            Assert.Collection(handle.Documents.OrderBy(doc => doc.FilePath),
                document =>
                {
                    Assert.Equal(legacyDocument.FilePath, document.FilePath);
                    Assert.Equal(legacyDocument.TargetPath, document.TargetPath);
                    Assert.Equal(legacyDocument.FileKind, document.FileKind);
                },
                document =>
                {
                    Assert.Equal(componentDocument.FilePath, document.FilePath);
                    Assert.Equal(componentDocument.TargetPath, document.TargetPath);
                    Assert.Equal(componentDocument.FileKind, document.FileKind);
                });
        }

        [Fact]
        public void ProjectSnapshot_CanKindOfRoundTrip()
        {
            // Arrange
            var projectSnapshot = TestProjectSnapshot.Create(
                "/path/to/project.csproj",
                new[]
                {
                    "/path/to/component.razor",
                    "/path/to/file.cshtml",
                },
                Configuration,
                ProjectWorkspaceState);
            var serializedHandle = JsonConvert.SerializeObject(projectSnapshot, Converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(serializedHandle, Converters);

            // Assert
            Assert.Equal(projectSnapshot.FilePath, deserializedHandle.FilePath);
            Assert.Equal(projectSnapshot.Configuration, deserializedHandle.Configuration);
            Assert.Equal(projectSnapshot.ProjectWorkspaceState, deserializedHandle.ProjectWorkspaceState);
            Assert.Collection(deserializedHandle.Documents.OrderBy(doc => doc.FilePath),
                document => Assert.Equal("/path/to/component.razor", document.FilePath),
                document => Assert.Equal("/path/to/file.cshtml", document.FilePath));
        }

        [Fact]
        public void RazorConfiguration_CanRoundTrip()
        {
            // Arrange
            var serializedConfiguration = JsonConvert.SerializeObject(Configuration, Converters);

            // Act
            var deserializedConfiguration = JsonConvert.DeserializeObject<RazorConfiguration>(serializedConfiguration, Converters);

            // Assert
            Assert.Equal(Configuration, deserializedConfiguration);
        }
    }
}
