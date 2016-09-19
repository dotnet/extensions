// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    public class PathHelper
    {
        internal const string Secrets_File_Name = "secrets.json";
        internal const string Config_File_Name = "project.json";

        public static string GetSecretsPath(IFileProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var fileInfo = provider.GetFileInfo(Config_File_Name);
            if (fileInfo == null || !fileInfo.Exists || string.IsNullOrEmpty(fileInfo.PhysicalPath))
            {
                throw new InvalidOperationException(
                    string.Format(Resources.Error_Missing_Project_Json, provider.GetFileInfo("/")?.PhysicalPath ?? "unknown"));
            }

            using (var stream = fileInfo.CreateReadStream())
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var obj = JObject.Load(jsonReader);

                var userSecretsId = obj.Value<string>("userSecretsId");

                if (string.IsNullOrEmpty(userSecretsId))
                {
                    throw new InvalidOperationException(
                        string.Format(Resources.Error_Missing_UserSecretId_In_Project_Json, fileInfo.Name));
                }

                return GetSecretsPathFromSecretsId(userSecretsId);
            }
        }

        public static string GetSecretsPath(string projectPath)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            using (var provider = new PhysicalFileProvider(projectPath))
            {
                return GetSecretsPath(provider);
            }
        }

        public static string GetSecretsPathFromSecretsId(string userSecretsId)
        {
            if (userSecretsId == null)
            {
                throw new ArgumentNullException(nameof(userSecretsId));
            }

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