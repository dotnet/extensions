// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Options class used by the <see cref="AzureKeyVaultConfigurationExtensions"/>.
    /// </summary>
    public class AzureKeyVaultConfigurationOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationOptions"/>.
        /// </summary>
        public AzureKeyVaultConfigurationOptions()
        {
            Manager = DefaultKeyVaultSecretManager.Instance;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationOptions"/>.
        /// </summary>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        public AzureKeyVaultConfigurationOptions(
            string vault,
            string clientId,
            X509Certificate2 certificate) : this()
        {
            KeyVaultClient.AuthenticationCallback authenticationCallback =
                (authority, resource, scope) => GetTokenFromClientCertificate(authority, resource, clientId, certificate);

            Vault = vault;
            Client = new KeyVaultClient(authenticationCallback);
        }

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationOptions"/>.
        /// </summary>
        /// <param name="vault">The Azure KeyVault uri.</param>
        public AzureKeyVaultConfigurationOptions(string vault) : this()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var authenticationCallback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);

            Vault = vault;
            Client = new KeyVaultClient(authenticationCallback);
        }

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationOptions"/>.
        /// </summary>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        public AzureKeyVaultConfigurationOptions(
            string vault,
            string clientId,
            string clientSecret) : this()
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }
            if (clientSecret == null)
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            KeyVaultClient.AuthenticationCallback authenticationCallback =
                (authority, resource, scope) => GetTokenFromClientSecret(authority, resource, clientId, clientSecret);

            Vault = vault;
            Client = new KeyVaultClient(authenticationCallback);
        }

        /// <summary>
        /// Gets or sets the <see cref="KeyVaultClient"/> to use for retrieving values.
        /// </summary>
        public KeyVaultClient Client { get; set; }

        /// <summary>
        /// Gets or sets the vault uri.
        /// </summary>
        public string Vault { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.
        /// </summary>
        public IKeyVaultSecretManager Manager { get; set; }

        /// <summary>
        /// Gets or sets the timespan to wait between attempts at polling the Azure KeyVault for changes. <see langword="null" /> to disable reloading.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }

        private static async Task<string> GetTokenFromClientCertificate(string authority, string resource, string clientId, X509Certificate2 certificate)
        {
            var authContext = new AuthenticationContext(authority);
            var result = await authContext.AcquireTokenAsync(resource, new ClientAssertionCertificate(clientId, certificate));
            return result.AccessToken;
        }

        private static async Task<string> GetTokenFromClientSecret(string authority, string resource, string clientId, string clientSecret)
        {
            var authContext = new AuthenticationContext(authority);
            var clientCred = new ClientCredential(clientId, clientSecret);
            var result = await authContext.AcquireTokenAsync(resource, clientCred);
            return result.AccessToken;
        }
    }
}
