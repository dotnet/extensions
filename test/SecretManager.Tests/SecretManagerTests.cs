// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.ConfigurationModel.UserSecrets;
using Xunit;

namespace SecretManager.Tests
{
    public class SecretManagerTests
    {
        [Fact]
        public void SetSecret_With_ProjectPath_As_CommandLine_Parameter()
        {
            SetSecrets(fromCurrentDirectory: false);
        }

        [Fact]
        public void SetSecret_From_CurrentDirectory()
        {
            var backUpCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                SetSecrets(fromCurrentDirectory: true);
            }
            catch (Exception)
            {
                Directory.SetCurrentDirectory(backUpCurrentDirectory);
                throw;
            }
        }

        private void SetSecrets(bool fromCurrentDirectory)
        {
            var secrets = new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>("Facebook:AppId", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>(@"key-@\/.~123!#$%^&*())-+==", @"key-@\/.~123!#$%^&*())-+=="),
                            new KeyValuePair<string, string>("key2", string.Empty)
                        };

            var projectPath = UserSecretHelper.GetTempSecretProject();
            if (fromCurrentDirectory)
            {
                Directory.SetCurrentDirectory(projectPath);         // Point current directory to the project.json directory.
            }

            var logger = new TestLogger();
            var secretManager = new Program() { Logger = logger };

            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.Main(parameters);
            }

            Assert.Equal(4, logger.Messages.Count);

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    logger.Messages);
            }

            logger.Messages.Clear();
            var args = fromCurrentDirectory ?
                new string[] { "list" } : new string[] { "list", "-p", projectPath };
            secretManager.Main(args);
            Assert.Equal(4, logger.Messages.Count);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    logger.Messages);
            }

            // Remove secrets.
            logger.Messages.Clear();
            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "remove", secret.Key } :
                    new string[] { "remove", secret.Key, "-p", projectPath };
                secretManager.Main(parameters);
            }

            // Verify secrets are removed.
            logger.Messages.Clear();
            args = fromCurrentDirectory ?
                new string[] { "list" } : new string[] { "list", "-p", projectPath };
            secretManager.Main(args);
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, logger.Messages);

            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }

        [Fact]
        public void SetSecret_Update_Existing_Secret()
        {
            var projectPath = UserSecretHelper.GetTempSecretProject();
            var logger = new TestLogger();
            var secretManager = new Program() { Logger = logger };

            secretManager.Main(new string[] { "set", "secret1", "value1", "-p", projectPath });
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", logger.Messages);
            secretManager.Main(new string[] { "set", "secret1", "value2", "-p", projectPath });
            Assert.Equal(2, logger.Messages.Count);
            Assert.Contains("Successfully saved secret1 = value2 to the secret store.", logger.Messages);

            logger.Messages.Clear();

            secretManager.Main(new string[] { "list", "-p", projectPath });
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains("secret1 = value2", logger.Messages);

            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }

        [Fact]
        public void SetSecret_With_Verbose_Flag()
        {
            var projectPath = UserSecretHelper.GetTempSecretProject();
            var logger = new TestLogger(verbose: true);
            var secretManager = new Program() { Logger = logger };

            secretManager.Main(new string[] { "-v", "set", "secret1", "value1", "-p", projectPath });
            Assert.Equal(3, logger.Messages.Count);
            Assert.Contains(string.Format("Project file path {0}.", projectPath), logger.Messages);
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPath(projectPath)), logger.Messages);
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", logger.Messages);
            logger.Messages.Clear();

            secretManager.Main(new string[] { "-v", "list", "-p", projectPath });
            Assert.Equal(3, logger.Messages.Count);
            Assert.Contains(string.Format("Project file path {0}.", projectPath), logger.Messages);
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPath(projectPath)), logger.Messages);
            Assert.Contains("secret1 = value1", logger.Messages);

            UserSecretHelper.DeleteTempSecretProject(projectPath);
        }

        [Fact]
        public void Remove_Non_Existing_Secret()
        {
            var projectPath = UserSecretHelper.GetTempSecretProject();
            var logger = new TestLogger();
            var secretManager = new Program() { Logger = logger };
            secretManager.Main(new string[] { "remove", "secret1", "-p", projectPath });
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains("Cannot find 'secret1' in the secret store.", logger.Messages);
        }

        [Fact]
        public void List_Empty_Secrets_File()
        {
            var projectPath = UserSecretHelper.GetTempSecretProject();
            var logger = new TestLogger();
            var secretManager = new Program() { Logger = logger };
            secretManager.Main(new string[] { "list", "-p", projectPath });
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, logger.Messages);
        }

        [Fact]
        public void Clear_All_Secrets_With_ProjectPath_As_Parameter()
        {
            Clear_Secrets(fromCurrentDirectory: false);
        }

        [Fact]
        public void Clear_All_Secrets_From_Current_Directory()
        {
            Clear_Secrets(fromCurrentDirectory: true);
        }

        private void Clear_Secrets(bool fromCurrentDirectory)
        {
            var projectPath = UserSecretHelper.GetTempSecretProject();
            if (fromCurrentDirectory)
            {
                Directory.SetCurrentDirectory(projectPath);         // Point current directory to the project.json directory.
            }

            var logger = new TestLogger();
            var secretManager = new Program() { Logger = logger };

            var secrets = new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>("Facebook:AppId", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>(@"key-@\/.~123!#$%^&*())-+==", @"key-@\/.~123!#$%^&*())-+=="),
                            new KeyValuePair<string, string>("key2", string.Empty)
                        };

            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.Main(parameters);
            }

            Assert.Equal(4, logger.Messages.Count);

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    logger.Messages);
            }

            // Verify secrets are persisted.
            logger.Messages.Clear();
            var args = fromCurrentDirectory ?
                new string[] { "list" } :
                new string[] { "list", "-p", projectPath };
            secretManager.Main(args);
            Assert.Equal(4, logger.Messages.Count);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    logger.Messages);
            }

            // Clear secrets.
            logger.Messages.Clear();
            args = fromCurrentDirectory ? new string[] { "clear" } : new string[] { "clear", "-p", projectPath };
            secretManager.Main(args);
            Assert.Equal(0, logger.Messages.Count);

            args = fromCurrentDirectory ? new string[] { "list" } : new string[] { "list", "-p", projectPath };
            secretManager.Main(args);
            Assert.Equal(1, logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, logger.Messages);
        }
    }
}