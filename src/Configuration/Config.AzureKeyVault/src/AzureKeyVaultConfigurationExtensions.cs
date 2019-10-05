// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="AzureKeyVaultConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class AzureKeyVaultConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            string clientSecret)
        {
            return configurationBuilder.AddAzureKeyVault(vault, clientId, clientSecret, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            string clientSecret,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, clientId, clientSecret, optional));
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            string clientSecret,
            IKeyVaultSecretManager manager)
        {
            return configurationBuilder.AddAzureKeyVault(vault, clientId, clientSecret, manager, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            string clientSecret,
            IKeyVaultSecretManager manager,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, clientId, clientSecret, optional)
            {
                Manager = manager
            });
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            X509Certificate2 certificate)
        {
            return configurationBuilder.AddAzureKeyVault(vault, clientId, certificate, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            X509Certificate2 certificate,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, clientId, certificate, optional));
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            X509Certificate2 certificate,
            IKeyVaultSecretManager manager)
        {
            return configurationBuilder.AddAzureKeyVault(vault, clientId, certificate, manager, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            X509Certificate2 certificate,
            IKeyVaultSecretManager manager,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, clientId, certificate, optional)
            {
                Manager = manager
            });
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault)
        {
            return configurationBuilder.AddAzureKeyVault(vault, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, optional));
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            IKeyVaultSecretManager manager)
        {
            return configurationBuilder.AddAzureKeyVault(vault, manager, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            IKeyVaultSecretManager manager,
            bool optional)
        {
            return AddAzureKeyVault(configurationBuilder, new AzureKeyVaultConfigurationOptions(vault, optional)
            {
                Manager = manager
            });
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            KeyVaultClient client,
            IKeyVaultSecretManager manager)
        {
            return configurationBuilder.AddAzureKeyVault(vault, client, manager, false);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="manager">The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.</param>
        /// <param name="optional">Whether reading from the key vault is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            KeyVaultClient client,
            IKeyVaultSecretManager manager,
            bool optional)
        {
            return configurationBuilder.Add(new AzureKeyVaultConfigurationSource(new AzureKeyVaultConfigurationOptions()
            {
                Client = client,
                Vault = vault,
                Manager = manager,
                Optional = optional
            }));
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="options">The <see cref="AzureKeyVaultConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder configurationBuilder, AzureKeyVaultConfigurationOptions options)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Client == null)
            {
                throw new ArgumentNullException(nameof(options.Client));
            }
            if (options.Vault == null)
            {
                throw new ArgumentNullException(nameof(options.Vault));
            }
            if (options.Manager == null)
            {
                throw new ArgumentNullException(nameof(options.Manager));
            }

            configurationBuilder.Add(new AzureKeyVaultConfigurationSource(options));

            return configurationBuilder;
        }
    }
}
