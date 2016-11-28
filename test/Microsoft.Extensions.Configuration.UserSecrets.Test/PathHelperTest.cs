// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{
    public class PathHelperTest : IClassFixture<UserSecretsTestFixture>
    {
        private readonly UserSecretsTestFixture _fixture;
        private readonly ITestOutputHelper _output;

        public PathHelperTest(UserSecretsTestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void Gives_Correct_Secret_Path()
        {
            string userSecretsId;
            var projectPath = _fixture.GetTempSecretProject(out userSecretsId);
            var actualSecretPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

            var root = Environment.GetEnvironmentVariable("APPDATA") ??         // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                        Environment.GetEnvironmentVariable("HOME");             // On Mac/Linux it goes to ~/.microsoft/usersecrets/

            var expectedSecretPath = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPDATA")) ?
                Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, "secrets.json") :
                Path.Combine(root, ".microsoft", "usersecrets", userSecretsId, "secrets.json");

            Assert.Equal(expectedSecretPath, actualSecretPath);
        }

        [Fact]
        public void Throws_If_UserSecretId_Contains_Invalid_Characters()
        {
            foreach (var character in Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()))
            {
                var id = "Test" + character;
                _output.WriteLine($"Testing ID '{id}'");
                Assert.Throws<InvalidOperationException>(() => PathHelper.GetSecretsPathFromSecretsId(id));
            }
        }

        // TODO remove in 2.0
        #region LegacyApiTest

        [Fact]
        public void Throws_If_Project_Json_Not_Found()
        {
            var projectPath = _fixture.GetTempSecretProject();
            File.Delete(Path.Combine(projectPath, "project.json"));

            Assert.Throws<InvalidOperationException>(() =>
            {
#pragma warning disable CS0618
                PathHelper.GetSecretsPath(projectPath);
#pragma warning restore CS0618
            });
        }

        [Fact]
        public void Throws_If_Project_Json_Does_Not_Contain_UserSecretId()
        {
            var projectPath = _fixture.GetTempSecretProject();
            File.WriteAllText(Path.Combine(projectPath, "project.json"), "{}");

            Assert.Throws<InvalidOperationException>(() =>
            {
#pragma warning disable CS0618
                PathHelper.GetSecretsPath(projectPath);
#pragma warning restore CS0618
            });
        }

        #endregion
    }
}