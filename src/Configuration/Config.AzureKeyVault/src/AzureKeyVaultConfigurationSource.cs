// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Represents Azure KeyVault secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    internal class AzureKeyVaultConfigurationSource : IConfigurationSource
    {
        private readonly AzureKeyVaultConfigurationOptions _options;

        public AzureKeyVaultConfigurationSource(AzureKeyVaultConfigurationOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureKeyVaultConfigurationProvider(new KeyVaultClientWrapper(_options.Client), _options.Vault, _options.Manager, _options.ReloadInterval);
        }
    }
}
