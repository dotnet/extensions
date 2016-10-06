// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{

    public class ConfigurationExtensionTest
    {
        private void SetSecret(string id, string key, string value)
        {
            var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(id);

            Directory.CreateDirectory(Path.GetDirectoryName(secretsFilePath));

            var secrets = new ConfigurationBuilder()
                .AddJsonFile(secretsFilePath, optional: true)
                .Build()
                .AsEnumerable()
                .Where(i => i.Value != null)
                .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);

            secrets[key] = value;


            var contents = new JObject();
            if (secrets != null)
            {
                foreach (var secret in secrets.AsEnumerable())
                {
                    contents[secret.Key] = secret.Value;
                }
            }

            File.WriteAllText(secretsFilePath, contents.ToString(), Encoding.UTF8);
        }

        [Fact]
        public void AddUserSecrets_Does_Not_Fail_On_Non_Existing_File_Explicitly_Passed()
        {
            var builder = new ConfigurationBuilder()
                                .AddUserSecrets(userSecretsId: Guid.NewGuid().ToString());
        }

        [Fact]
        public void AddUserSecrets_Does_Not_Fail_On_Non_Existing_File()
        {
            string userSecretsId;
            var projectPath = UserSecretHelper.GetTempSecretProject(out userSecretsId);

            var builder = new ConfigurationBuilder().SetBasePath(projectPath).AddUserSecrets();

            var configuration = builder.Build();
            Assert.Equal(null, configuration["Facebook:AppSecret"]);

            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }

        [Fact]
        public void AddUserSecrets_With_An_Existing_Secret_File()
        {
            string userSecretsId;
            var projectPath = UserSecretHelper.GetTempSecretProject(out userSecretsId);

            SetSecret(userSecretsId, "Facebook:AppSecret", "value1");

            var builder = new ConfigurationBuilder().SetBasePath(projectPath).AddUserSecrets();

            var configuration = builder.Build();
            Assert.Equal("value1", configuration["Facebook:AppSecret"]);

            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }

        [Fact]
        public void AddUserSecrets_With_SecretsId_Passed_Explicitly()
        {
            string userSecretsId;
            var projectPath = UserSecretHelper.GetTempSecretProject(out userSecretsId);

            SetSecret(userSecretsId, "Facebook:AppSecret", "value1");

            var builder = new ConfigurationBuilder()
                                .AddUserSecrets(userSecretsId: userSecretsId);
            var configuration = builder.Build();

            Assert.Equal("value1", configuration["Facebook:AppSecret"]);
            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }
    }
}