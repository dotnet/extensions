// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Internal;
using Newtonsoft.Json.Linq;

namespace Microsoft.Framework.ConfigurationModel.UserSecrets
{
    public class PathHelper
    {
        private const string Secrets_File_Name = "secrets.json";

        public static string GetSecretsPath([NotNull]string projectPath)
        {
            var projectFilePath = Path.Combine(projectPath, "project.json");

            if (!File.Exists(projectFilePath))
            {
                throw new InvalidOperationException(
                    string.Format(Resources.Error_Missing_Project_Json, projectFilePath));
            }

            var obj = JObject.Parse(File.ReadAllText(projectFilePath));
            var userSecretsId = obj.Value<string>("userSecretsId");

            if (string.IsNullOrEmpty(userSecretsId))
            {
                throw new InvalidOperationException(
                    string.Format(Resources.Error_Missing_UserSecretId_In_Project_Json, projectFilePath));
            }

            return GetSecretsPathFromSecretsId(userSecretsId);
        }

        public static string GetSecretsPathFromSecretsId([NotNull]string userSecretsId)
        {
            var badCharIndex = userSecretsId.IndexOfAny(Path.GetInvalidPathChars());
            if (badCharIndex != -1)
            {
                throw new InvalidOperationException(
                    string.Format(
                        Resources.Error_Invalid_Character_In_UserSecrets_Id,
                        userSecretsId[badCharIndex],
                        badCharIndex));
            }

            var root = Environment.GetEnvironmentVariable("APPDATA") ??         // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                        Environment.GetEnvironmentVariable("HOME");             // On Mac/Linux it goes to ~/.microsoft/usersecrets/

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPDATA")))
            {
                return Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, Secrets_File_Name);
            }
            else
            {
                return Path.Combine(root, ".microsoft", "usersecrets", userSecretsId, Secrets_File_Name);
            }
        }
    }
}