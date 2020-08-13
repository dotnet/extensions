// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultProjectConfigurationFilePathStoreTest
    {
        [Fact]
        public void Set_InvokesChanged()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            var configurationFilePath = "C:/project/obj/project.razor.json";
            var called = false;
            store.Changed += (sender, args) =>
            {
                called = true;
                Assert.Equal(projectFilePath, args.ProjectFilePath);
                Assert.Equal(configurationFilePath, args.ConfigurationFilePath);
            };

            // Act
            store.Set(projectFilePath, configurationFilePath);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Set_SameConfigurationFilePath_DoesNotInvokeChanged()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            var configurationFilePath = "C:/project/obj/project.razor.json";
            store.Set(projectFilePath, configurationFilePath);
            var called = false;
            store.Changed += (sender, args) =>
            {
                called = true;
            };

            // Act
            store.Set(projectFilePath, configurationFilePath);

            // Assert
            Assert.False(called);
        }

        [Fact]
        public void Set_AllowsTryGet()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            var expectedConfigurationFilePath = "C:/project/obj/project.razor.json";
            store.Set(projectFilePath, expectedConfigurationFilePath);

            // Act
            var result = store.TryGet(projectFilePath, out var configurationFilePath);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
        }

        [Fact]
        public void Set_OverridesPrevious()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            var expectedConfigurationFilePath = "C:/project/obj/project.razor.json";

            // Act
            store.Set(projectFilePath, "C:/other/obj/project.razor.json");
            store.Set(projectFilePath, expectedConfigurationFilePath);

            // Assert
            var result = store.TryGet(projectFilePath, out var configurationFilePath);
            Assert.True(result);
            Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
        }

        [Fact]
        public void GetMappings_NotMutable()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();

            // Act
            var mappings = store.GetMappings();
            store.Set("C:/project.csproj", "C:/project/obj/project.razor.json");

            // Assert
            Assert.Empty(mappings);
        }

        [Fact]
        public void GetMappings_ReturnsAllSetMappings()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var expectedMappings = new Dictionary<string, string>()
            {
                ["C:/project1.csproj"] = "C:/project1/obj/project.razor.json",
                ["C:/project2.csproj"] = "C:/project2/obj/project.razor.json"
            };
            foreach (var mapping in expectedMappings)
            {
                store.Set(mapping.Key, mapping.Value);
            }

            // Act
            var mappings = store.GetMappings();

            // Assert
            Assert.Equal(expectedMappings, mappings);
        }

        [Fact]
        public void Remove_InvokesChanged()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            store.Set(projectFilePath, "C:/project/obj/project.razor.json");
            var called = false;
            store.Changed += (sender, args) =>
            {
                called = true;
                Assert.Equal(projectFilePath, args.ProjectFilePath);
                Assert.Null(args.ConfigurationFilePath);
            };

            // Act
            store.Remove(projectFilePath);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Remove_UntrackedProject_DoesNotInvokeChanged()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var called = false;
            store.Changed += (sender, args) =>
            {
                called = true;
            };

            // Act
            store.Remove("C:/project.csproj");

            // Assert
            Assert.False(called);
        }

        [Fact]
        public void Remove_RemovesGettability()
        {
            // Arrange
            var store = new DefaultProjectConfigurationFilePathStore();
            var projectFilePath = "C:/project.csproj";
            store.Set(projectFilePath, "C:/project/obj/project.razor.json");

            // Act
            store.Remove(projectFilePath);
            var result = store.TryGet(projectFilePath, out var configurationFilePath);

            // Assert
            Assert.False(result);
        }
    }
}
