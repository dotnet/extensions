// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    public class MsBuildTargetTest : IDisposable
    {
        private readonly string _tempDir;
        private readonly DirectoryInfo _solutionRoot;
        private readonly ITestOutputHelper _output;

        public MsBuildTargetTest(ITestOutputHelper output)
        {
            _output = output;
            _tempDir = Path.Combine(Path.GetTempPath(), "usersecretstest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            _solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
            while (_solutionRoot != null)
            {
                if (File.Exists(Path.Combine(_solutionRoot.FullName, "NuGet.config")))
                {
                    break;
                }

                _solutionRoot = _solutionRoot.Parent;
            }
        }

        [Fact]
        public void GeneratesAssemblyAttributeFile()
        {
            if (_solutionRoot == null)
            {
                Assert.True(false, "Could not identify solution root");
            }
            var target = Path.Combine(_solutionRoot.FullName, "src", "Microsoft.Extensions.Configuration.UserSecrets", "build", "netstandard1.0", "Microsoft.Extensions.Configuration.UserSecrets.targets");
            Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
            var libName = "Microsoft.Extensions.Configuration.UserSecrets.dll";
            File.Copy(Path.Combine(AppContext.BaseDirectory, libName), Path.Combine(_tempDir, libName));
            File.Copy(target, Path.Combine(_tempDir, "obj", "test.csproj.usersecretstest.targets")); // imitates how NuGet will import this target
            var testProj = Path.Combine(_tempDir, "test.csproj");
            // should represent a 'dotnet new' project
            File.WriteAllText(testProj, $@"
<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <UserSecretsId>xyz123</UserSecretsId>
        <TargetFramework>netcoreapp1.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""Program.cs""/>
        <PackageReference Include=""Microsoft.NETCore.App"" Version=""1.0.1"" />
        <Reference Include=""$(MSBuildThisFileDirectory){libName}"" />
    </ItemGroup>
</Project>
");
            _output.WriteLine($"Tempdir = {_tempDir}");
            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "public class Program { public static void Main(){}}");
            var assemblyInfoFile = Path.Combine(_tempDir, "obj/Debug/netcoreapp1.0/UserSecretsAssemblyInfo.cs");

            var restoreInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "restore",
                UseShellExecute = false,
                WorkingDirectory = _tempDir
            };
            var restore = Process.Start(restoreInfo);
            restore.WaitForExit();
            Assert.Equal(0, restore.ExitCode);

            Assert.False(File.Exists(assemblyInfoFile));

            var buildInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                UseShellExecute = false,
                WorkingDirectory = _tempDir
            };
            var build = Process.Start(buildInfo);
            build.WaitForExit();
            Assert.Equal(0, build.ExitCode);

            Assert.True(File.Exists(assemblyInfoFile));
            var contents = File.ReadAllText(assemblyInfoFile);
            Assert.Contains("[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute(\"xyz123\")]", contents);
            var lastWrite = new FileInfo(assemblyInfoFile).LastWriteTimeUtc;

            var rebuild = Process.Start(buildInfo);
            rebuild.WaitForExit();
            Assert.Equal(0, rebuild.ExitCode);
            // asserts that the target doesn't re-generate assembly file. Important for incremental build.
            Assert.Equal(lastWrite, new FileInfo(assemblyInfoFile).LastWriteTimeUtc);
        }

        public void Dispose()
        {
            try
            {
          //      Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
            }
        }
    }
}