// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare.Razor.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public class SerializationTest
    {
        [Fact]
        public void ProjectSnapshotHandleProxy_RoundTripsProperly()
        {
            // Arrange
            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build(),
                TagHelperDescriptorBuilder.Create("TestTagHelper2", "TestAssembly2").Build(),
            };
            var projectWorkspaceState = new ProjectWorkspaceState(tagHelpers, default);
            var expectedConfiguration = RazorConfiguration.Default;
            var expectedRootNamespace = "project";
            var handle = new ProjectSnapshotHandleProxy(new Uri("vsls://some/path/project.csproj"), RazorConfiguration.Default, expectedRootNamespace, projectWorkspaceState);
            var converterCollection = new JsonConverterCollection();
            converterCollection.RegisterRazorLiveShareConverters();
            var converters = converterCollection.ToArray();
            var serializedHandle = JsonConvert.SerializeObject(handle, converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<ProjectSnapshotHandleProxy>(serializedHandle, converters);

            // Assert
            Assert.Equal("vsls://some/path/project.csproj", deserializedHandle.FilePath.ToString());
            Assert.Equal(projectWorkspaceState, deserializedHandle.ProjectWorkspaceState);
            Assert.Equal(expectedConfiguration.ConfigurationName, deserializedHandle.Configuration.ConfigurationName);
            Assert.Equal(expectedConfiguration.Extensions.Count, deserializedHandle.Configuration.Extensions.Count);
            Assert.Equal(expectedConfiguration.LanguageVersion, deserializedHandle.Configuration.LanguageVersion);
            Assert.Equal(expectedRootNamespace, deserializedHandle.RootNamespace);
        }
    }
}
