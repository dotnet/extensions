// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    public class MsBuildTargetTest : IDisposable
    {
        private const string SkipReason = "Not safe to run on CI. MSBuild and SDK not available yet.";
        private readonly string _tempDir;
        private readonly DirectoryInfo _solutionRoot;

        public MsBuildTargetTest()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "usersecrettest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            _solutionRoot = new DirectoryInfo(AppContext.BaseDirectory);
            while (_solutionRoot != null)
            {
                if (File.Exists(Path.Combine(_solutionRoot.FullName, "global.json")))
                {
                    break;
                }

                _solutionRoot = _solutionRoot.Parent;
            }
        }

        [Fact(Skip = SkipReason)]
        public void GeneratesAssemblyAttributeFile()
        {
            if (_solutionRoot == null)
            {
                Assert.True(false, "Could not identify solution root");
            }
            var target = Path.Combine(_solutionRoot.FullName, "src", "Microsoft.Extensions.Configuration.UserSecrets", "build", "netstandard1.0", "Microsoft.Extensions.Configuration.UserSecrets.targets");
            var testProj = Path.Combine(_tempDir, "test.csproj");
            // should represent a 'dotnet new' project
            File.WriteAllText(testProj, @"
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />
    <PropertyGroup>
        <OutputType>Exe></OutputType>
        <UserSecretsId>xyz123</UserSecretsId>
        <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""Program.cs""/>
        <PackageReference Include=""Microsoft.NETCore.App"">
            <Version>1.0.1</Version>
        </PackageReference>
        <PackageReference Include=""Microsoft.NET.Sdk"">
            <Version>1.0.0-*</Version>
            <PrivateAssets>All</PrivateAssets>
        </PackageReference>
    </ItemGroup>
    <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
    <ImportGroup Condition=""'$(TargetFramework)'!=''"">
        <Import Project=""$(TestTarget)"" Condition=""'$(TestTarget)' != ''""/>
    </ImportGroup>
</Project>
");
            File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "public class Program { public static void Main(){}}");
            var assemblyInfoFile = Path.Combine(_tempDir, "obj/Debug/netcoreapp1.0/UserSecretsAssemblyInfo.cs");

            var restoreInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore3 \"{testProj}\" -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json /nologo /v:m",
                UseShellExecute = false
            };
            var restore = Process.Start(restoreInfo);
            restore.WaitForExit();
            Assert.Equal(0, restore.ExitCode);

            Assert.False(File.Exists(assemblyInfoFile));

            // TODO actually build a project
            var buildInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"msbuild \"{testProj}\" /nologo /v:m \"/p:TestTarget={target}\" /p:TargetFramework=netcoreapp1.0 /t:GenerateUserSecretsAttribute",
                UseShellExecute = false
            };
            var build = Process.Start(buildInfo);
            build.WaitForExit();
            Assert.Equal(0, build.ExitCode);

            Assert.True(File.Exists(assemblyInfoFile));
            var contents = File.ReadAllText(assemblyInfoFile);
            Assert.Contains("[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute(\"xyz123\")]", contents);
            var lastWrite = new FileInfo(assemblyInfoFile).LastWriteTimeUtc;

            build = Process.Start(buildInfo);
            build.WaitForExit();
            Assert.Equal(0, build.ExitCode);
            // assert that the target doesn't re-generate assembly file. Important for incremental build.
            Assert.Equal(lastWrite, new FileInfo(assemblyInfoFile).LastWriteTimeUtc);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
            }
        }
    }
}