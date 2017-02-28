// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Configuration extensions for adding user secrets configuration source.
    /// </summary>
    public static class UserSecretsConfigurationExtensions
    {
        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. Searches the assembly that contains type <typeparamref name="T"/>
        /// for an instance of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <typeparam name="T">The type from the assembly to search for an instance of <see cref="UserSecretsIdAttribute"/>.</typeparam>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets<T>(this IConfigurationBuilder configuration)
            where T : class
            => configuration.AddUserSecrets(typeof(T).GetTypeInfo().Assembly);

        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source. This searches <paramref name="assembly"/> for an instance
        /// of <see cref="UserSecretsIdAttribute"/>, which specifies a user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="assembly">The assembly with the <see cref="UserSecretsIdAttribute" />.</param>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, Assembly assembly)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var attribute = assembly.GetCustomAttribute<UserSecretsIdAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException(Resources.FormatError_Missing_UserSecretsIdAttribute(assembly.GetName().Name));
            }

            return AddUserSecrets(configuration, attribute.UserSecretsId);
        }

        /// <summary>
        /// <para>
        /// Adds the user secrets configuration source with specified user secrets ID.
        /// </para>
        /// <para>
        /// A user secrets ID is unique value used to store and identify a collection of secret configuration values.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
        /// <param name="userSecretsId">The user secrets ID.</param>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddUserSecrets(this IConfigurationBuilder configuration, string userSecretsId)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (userSecretsId == null)
            {
                throw new ArgumentNullException(nameof(userSecretsId));
            }

            return AddSecretsFile(configuration, PathHelper.GetSecretsPathFromSecretsId(userSecretsId));
        }

        private static IConfigurationBuilder AddSecretsFile(IConfigurationBuilder configuration, string secretPath)
        {
            var directoryPath = Path.GetDirectoryName(secretPath);
            var fileProvider = Directory.Exists(directoryPath)
                ? new PhysicalFileProvider(directoryPath)
                : null;
            return configuration.AddJsonFile(fileProvider, PathHelper.SecretsFileName, optional: true, reloadOnChange: false);
        }
    }
}
