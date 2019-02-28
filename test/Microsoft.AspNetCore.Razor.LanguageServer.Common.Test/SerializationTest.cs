// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;
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
            });
            var converterCollection = new JsonConverterCollection();
            converterCollection.RegisterRazorConverters();
            Converters = converterCollection.ToArray();
        }

        private RazorConfiguration Configuration { get; }

        private ProjectWorkspaceState ProjectWorkspaceState { get; }

        private JsonConverter[] Converters { get; }

        [Fact]
        public void FullProjectSnapshotHandle_CanRoundTrip()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle("/path/to/project.csproj", Configuration, ProjectWorkspaceState);
            var serializedHandle = JsonConvert.SerializeObject(handle, Converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(serializedHandle, Converters);

            // Assert
            Assert.Equal(handle.FilePath, deserializedHandle.FilePath);
            Assert.Equal(handle.Configuration, deserializedHandle.Configuration);
            Assert.Equal(handle.ProjectWorkspaceState, deserializedHandle.ProjectWorkspaceState);
        }

        [Fact]
        public void ProjectSnapshot_CanKindOfRoundTrip()
        {
            // Arrange
            var projectSnapshot = TestProjectSnapshot.Create("/path/to/project.csproj", Array.Empty<string>(), Configuration, ProjectWorkspaceState);
            var serializedHandle = JsonConvert.SerializeObject(projectSnapshot, Converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<FullProjectSnapshotHandle>(serializedHandle, Converters);

            // Assert
            Assert.Equal(projectSnapshot.FilePath, deserializedHandle.FilePath);
            Assert.Equal(projectSnapshot.Configuration, deserializedHandle.Configuration);
            Assert.Equal(projectSnapshot.ProjectWorkspaceState, deserializedHandle.ProjectWorkspaceState);
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
