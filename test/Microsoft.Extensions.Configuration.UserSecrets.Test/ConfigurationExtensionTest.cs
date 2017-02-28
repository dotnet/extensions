// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Test;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: UserSecretsId(ConfigurationExtensionTest.TestSecretsId)]

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{
    public class ConfigurationExtensionTest : IDisposable
    {
        public const string TestSecretsId = "d6076a6d3ab24c00b2511f10a56c68cc";

        private List<string> _tmpDirectories = new List<string>();

        private void SetSecret(string id, string key, string value)
        {
            var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(id);

            var dir = Path.GetDirectoryName(secretsFilePath);
            Directory.CreateDirectory(dir);
            _tmpDirectories.Add(dir);

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
        public void AddUserSecrets_FindsAssemblyAttribute()
        {
            var randValue = Guid.NewGuid().ToString();
            var configKey = "MyDummySetting";

            SetSecret(TestSecretsId, configKey, randValue);
            var config = new ConfigurationBuilder()
                .AddUserSecrets(typeof(ConfigurationExtensionTest).GetTypeInfo().Assembly)
                .Build();

            Assert.Equal(randValue, config[configKey]);
        }


        [Fact]
        public void AddUserSecrets_FindsAssemblyAttributeFromType()
        {
            var randValue = Guid.NewGuid().ToString();
            var configKey = "MyDummySetting";

            SetSecret(TestSecretsId, configKey, randValue);
            var config = new ConfigurationBuilder()
                .AddUserSecrets<ConfigurationExtensionTest>()
                .Build();

            Assert.Equal(randValue, config[configKey]);
        }

        [Fact]
        public void AddUserSecrets_With_SecretsId_Passed_Explicitly()
        {
            var userSecretsId = Guid.NewGuid().ToString();
            SetSecret(userSecretsId, "Facebook:AppSecret", "value1");

            var builder = new ConfigurationBuilder().AddUserSecrets(userSecretsId);
            var configuration = builder.Build();

            Assert.Equal("value1", configuration["Facebook:AppSecret"]);
        }

        [Fact]
        public void AddUserSecrets_Does_Not_Fail_On_Non_Existing_File()
        {
            var userSecretsId = Guid.NewGuid().ToString();
            var secretFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
            var builder = new ConfigurationBuilder().AddUserSecrets(userSecretsId);

            var configuration = builder.Build();
            Assert.Equal(null, configuration["Facebook:AppSecret"]);
            Assert.False(File.Exists(secretFilePath));
        }

        public void Dispose()
        {
            foreach (var dir in _tmpDirectories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to delete " + dir);
                }
            }
        }
    }
}
