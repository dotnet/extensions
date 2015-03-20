// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Framework.ConfigurationModel.UserSecrets;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.Framework.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds the user secrets configuration source.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IConfigurationSourceRoot AddUserSecrets([NotNull]this IConfigurationSourceRoot configuration)
        {
            var appEnv = (IApplicationEnvironment)CallContextServiceLocator.Locator.ServiceProvider.GetService(typeof(IApplicationEnvironment));
            var secretPath = PathHelper.GetSecretsPath(appEnv.ApplicationBasePath);

            if (!File.Exists(secretPath))
            {
                // TODO: Use the optional config add after that's available?.
                return configuration;
            }

            return configuration.AddJsonFile(secretPath);
        }

        /// <summary>
        /// Adds the user secrets configuration source with specified secrets id.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IConfigurationSourceRoot AddUserSecrets([NotNull]this IConfigurationSourceRoot configuration, [NotNull]string userSecretsId)
        {
            var secretPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

            if (!File.Exists(secretPath))
            {
                // TODO: Use the optional config add after that's available?.
                return configuration;
            }

            return configuration.AddJsonFile(secretPath);
        }
    }
}