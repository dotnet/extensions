// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;

namespace SecretManager.Tests
{
    public class UserSecretHelper
    {
        internal static string GetTempSecretProject()
        {
            string userSecretsId;
            return GetTempSecretProject(out userSecretsId);
        }

        internal static string GetTempSecretProject(out string userSecretsId)
        {
            var projectPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            userSecretsId = Guid.NewGuid().ToString();
            File.WriteAllText(
                Path.Combine(projectPath.FullName, "project.json"),
                string.Format("{{\"userSecretsId\": {0}}}", JsonConvert.ToString(userSecretsId)));
            return projectPath.FullName;
        }

        internal static void SetTempSecretInProject(string projectPath, string userSecretsId)
        {
            File.WriteAllText(
                Path.Combine(projectPath, "project.json"),
                string.Format("{{\"userSecretsId\": {0}}}", JsonConvert.ToString(userSecretsId)));
        }

        internal static void DeleteTempSecretProject(string projectPath)
        {
            try
            {
                Directory.Delete(projectPath, true);
            }
            catch (Exception)
            {
                // Ignore failures.
            }
        }
    }
}