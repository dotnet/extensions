// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.Dnx.TestHost
{
    public class ProjectTest
    {
        private const string projectName = "Sample.Tests";
        private const string ProjectJsonFileName = "project.json";
        private readonly string _testProjectPath;

        public ProjectTest()
        {
            var libraryManager = DnxPlatformServices.Default.LibraryManager;

            // Example: "C:\github\testing\test\Sample.Tests"
            _testProjectPath = Path.GetDirectoryName(libraryManager.GetLibrary(projectName).Path);
        }

        [Fact]
        public void GetsProjectInfo_FromProjectDirectoryPath()
        {
            // Arrange
            var projectDirPath = _testProjectPath;

            // Act & Assert
            Project project;
            Assert.True(Project.TryGetProject(projectDirPath, out project));
            Assert.NotNull(project);
            Assert.Equal(projectName, project.Name);
            Assert.NotNull(project.Commands);
            Assert.Equal(1, project.Commands.Count);
            string val;
            Assert.True(project.Commands.TryGetValue("test", out val));
            Assert.Equal("xunit.runner.aspnet", val);
        }

        [Fact]
        public void GetsProjectInfo_FromPathHavingProjectJsonFile()
        {
            // Arrange
            var projectJsonPath = Path.Combine(_testProjectPath, ProjectJsonFileName);

            // Act & Assert
            Project project;
            Assert.True(Project.TryGetProject(projectJsonPath, out project));
            Assert.NotNull(project);
            Assert.Equal(projectName, project.Name);
            Assert.NotNull(project.Commands);
            Assert.Equal(1, project.Commands.Count);
            string val;
            Assert.True(project.Commands.TryGetValue("test", out val));
            Assert.Equal("xunit.runner.aspnet", val);
        }

        [Fact]
        public void GetsProjectInfo_FromRelativeProjectDirectoryPath()
        {
            // Arrange
            var projectDirPath = string.Join(Path.DirectorySeparatorChar.ToString(), new[] { "..", projectName });

            // Act & Assert
            Project project;
            Assert.True(Project.TryGetProject(projectDirPath, out project));
            Assert.NotNull(project);
            Assert.Equal(projectName, project.Name);
            Assert.NotNull(project.Commands);
            Assert.Equal(1, project.Commands.Count);
            string val;
            Assert.True(project.Commands.TryGetValue("test", out val));
            Assert.Equal("xunit.runner.aspnet", val);
        }

        [Fact]
        public void GetsProjectInfo_FromRelativePathHavingProjectJsonFile()
        {
            // Arrange
            var projectDirPath = string.Join(
                Path.DirectorySeparatorChar.ToString(), new[] { "..", projectName, ProjectJsonFileName });

            // Act & Assert
            Project project;
            Assert.True(Project.TryGetProject(projectDirPath, out project));
            Assert.NotNull(project);
            Assert.Equal(projectName, project.Name);
            Assert.NotNull(project.Commands);
            Assert.Equal(1, project.Commands.Count);
            string val;
            Assert.True(project.Commands.TryGetValue("test", out val));
            Assert.Equal("xunit.runner.aspnet", val);
        }

        [Fact]
        public void ThrowsExceptionOnInvalidJsonContent()
        {
            // Arrange
            var projectJsonPath = Path.Combine(_testProjectPath, ProjectJsonFileName);
            var projectJsonContentStream = new MemoryStream(Encoding.UTF8.GetBytes("not a valid json content"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                Project.GetProject(projectJsonContentStream, projectJsonPath, projectName));
            Assert.Equal(
                $"The JSON file '{projectJsonPath}' can't be deserialized to a JSON object.", exception.Message);
        }

        [Fact]
        public void CommandsSetIsEmptyByDefault()
        {
            // Arrange
            var projectContent = @"{}";
            var projectJsonPath = Path.Combine(_testProjectPath, "project.json");
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(projectContent));

            // Act
            var project = Project.GetProject(memoryStream, projectJsonPath, projectName);

            // Assert
            Assert.NotNull(project.Commands);
            Assert.Equal(0, project.Commands.Count);
        }

        [Fact]
        public void CommandsSetIsSet()
        {
            // Arrange
            var projectContent = @"
{
    ""commands"": {
        ""cmd1"": ""cmd1value"",
        ""cmd2"": ""cmd2value"",
        ""cmd3"": ""cmd3value""
    }
}";
            var projectJsonPath = Path.Combine(_testProjectPath, "project.json");
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(projectContent));

            // Act
            var project = Project.GetProject(memoryStream, projectJsonPath, projectName);

            // Assert
            Assert.NotNull(project.Commands);
            Assert.Equal(3, project.Commands.Count);
        }

        public static TheoryData<string> InputPathWithoutProjectJsonData
        {
            get
            {
                var separator = Path.DirectorySeparatorChar.ToString();

                return new TheoryData<string>()
                {
                    // ex: samples\todo
                    string.Join(separator, new[] { "samples", "todo" }),

                    // ex: samples\todo\
                    string.Join(separator, new[] { "samples", "todo" }) + separator,

                    // ex: ..\samples\todo
                    string.Join(separator, new[] { "..", "samples", "todo" }),

                    // ex: ..\samples\todo\
                    string.Join(separator, new[] { "..", "samples", "todo" }) + separator
                };
            }
        }

        [Theory]
        [MemberData(nameof(InputPathWithoutProjectJsonData))]
        public void InputPathWithoutProjectJson_ReturnsFullPathWithProjectJson(string inputPath)
        {
            // Arrange & Act
            string projectName;
            var projectJsonPath = Project.GetProjectJsonPath(inputPath, out projectName);

            // Assert
            Assert.Equal(Path.GetFullPath(Path.Combine(inputPath, "project.json")), projectJsonPath);
            Assert.Equal("todo", projectName);
        }

        public static TheoryData<string> RelativeInputPathWithProjectJsonData
        {
            get
            {
                var separator = Path.DirectorySeparatorChar.ToString();

                return new TheoryData<string>()
                {
                    // ex: samples\todo\project.json
                    string.Join(separator, new[] { "samples", "todo", "project.json" }),

                    // ex: ..\samples\todo\project.json
                    string.Join(separator, new[] { "..", "samples", "todo", "project.json" }),
                };
            }
        }

        [Theory]
        [MemberData(nameof(RelativeInputPathWithProjectJsonData))]
        public void RelativeInputPathWithProjectJson_ReturnsFullPathWithProjectJson(string inputPath)
        {
            // Arrange & Act
            string projectName;
            var projectJsonPath = Project.GetProjectJsonPath(inputPath, out projectName);

            // Assert
            Assert.Equal(Path.GetFullPath(inputPath), projectJsonPath, ignoreCase: true);
            Assert.Equal("todo", projectName);
        }
    }
}
