// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
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
            var expectedConfiguration = RazorConfiguration.Default;
            var handle = new ProjectSnapshotHandleProxy(new Uri("vsls://some/path/project.csproj"), tagHelpers, RazorConfiguration.Default);
            var converterCollection = new JsonConverterCollection();
            converterCollection.RegisterRazorLiveShareConverters();
            var converters = converterCollection.ToArray();
            var serializedHandle = JsonConvert.SerializeObject(handle, converters);

            // Act
            var deserializedHandle = JsonConvert.DeserializeObject<ProjectSnapshotHandleProxy>(serializedHandle, converters);

            // Assert
            Assert.Equal("vsls://some/path/project.csproj", deserializedHandle.FilePath.ToString());
            Assert.Equal(tagHelpers, deserializedHandle.TagHelpers);
            Assert.Equal(expectedConfiguration.ConfigurationName, deserializedHandle.Configuration.ConfigurationName);
            Assert.Equal(expectedConfiguration.Extensions.Count, deserializedHandle.Configuration.Extensions.Count);
            Assert.Equal(expectedConfiguration.LanguageVersion, deserializedHandle.Configuration.LanguageVersion);
        }
    }
}
