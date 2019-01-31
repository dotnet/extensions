// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Azure.KeyVault;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Represents Azure KeyVault secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class AzureKeyVaultConfigurationSource : IConfigurationSource
    {
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
        /// Determines whether the source will be loaded if the Azure KeyVault changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        /// <summary>
        /// Number of milliseconds to wait inbetween each attempt at polling the Azure KeyVault for changes.
        /// </summary>
        public int ReloadPollDelay { get; set; } = 10000;

        /// <inheritdoc />
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureKeyVaultConfigurationProvider(new KeyVaultClientWrapper(Client), Vault, Manager, ReloadOnChange, ReloadPollDelay);
        }
    }
}
