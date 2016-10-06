// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{
    public class UserSecretsTestFixture : IDisposable
    {
        public const string TestSecretsId = "d6076a6d3ab24c00b2511f10a56c68cc";

        private Stack<Action> _disposables = new Stack<Action>();

        public UserSecretsTestFixture()
        {
            _disposables.Push(() => TryDelete(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(TestSecretsId))));
        }

        public void Dispose()
        {
            while (_disposables.Count > 0)
            {
                _disposables.Pop().Invoke();
            }
        }

        internal string GetTempSecretProject()
        {
            string userSecretsId;
            return GetTempSecretProject(out userSecretsId);
        }

        internal string GetTempSecretProject(out string userSecretsId)
        {
            var projectPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "usersecretstest", Guid.NewGuid().ToString()));
            userSecretsId = Guid.NewGuid().ToString();
            File.WriteAllText(
                Path.Combine(projectPath.FullName, "project.json"),
                JsonConvert.SerializeObject(new { userSecretsId }));

            var id = userSecretsId;
            _disposables.Push(() => TryDelete(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(id))));
            _disposables.Push(() => TryDelete(projectPath.FullName));

            return projectPath.FullName;
        }

        private static void TryDelete(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception)
            {
                // Ignore failures.
                Console.WriteLine("Failed to delete " + directory);
            }
        }
    }
}