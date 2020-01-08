// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets
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
        /// <param name="vaultUri">Azure KeyVault uri.</param>
        /// <param name="credential">The <see cref="TokenCredential"/> to use for authentication, like <see cref="DefaultAzureCredential"/>.</param>
        public AzureKeyVaultConfigurationOptions(
            Uri vaultUri,
            TokenCredential credential) : this()
        {
            Client = new SecretClient(vaultUri, credential);
        }

        /// <summary>
        /// Gets or sets the <see cref="SecretClient"/> to use for retrieving values.
        /// </summary>
        public SecretClient Client { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.
        /// </summary>
        public IKeyVaultSecretManager Manager { get; set; }

        /// <summary>
        /// Gets or sets the timespan to wait between attempts at polling the Azure KeyVault for changes. <code>null</code> to disable reloading.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }
    }
}
