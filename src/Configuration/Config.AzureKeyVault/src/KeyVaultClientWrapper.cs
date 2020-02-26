// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <inheritdoc />
    internal class KeyVaultClientWrapper : IKeyVaultClient
    {
        private readonly KeyVaultClient _keyVaultClientImplementation;

        /// <summary>
        /// Creates a new instance of <see cref="KeyVaultClientWrapper"/>.
        /// </summary>
        /// <param name="keyVaultClientImplementation">The <see cref="KeyVaultClient"/> instance to wrap.</param>
        public KeyVaultClientWrapper(KeyVaultClient keyVaultClientImplementation)
        {
            _keyVaultClientImplementation = keyVaultClientImplementation;
        }

        /// <inheritdoc />
        public Task<IPage<SecretItem>> GetSecretsAsync(string vault)
        {
            return _keyVaultClientImplementation.GetSecretsAsync(vault);
        }

        /// <inheritdoc />
        public Task<SecretBundle> GetSecretAsync(string secretIdentifier)
        {
            return _keyVaultClientImplementation.GetSecretAsync(secretIdentifier);
        }

        /// <inheritdoc />
        public Task<IPage<SecretItem>> GetSecretsNextAsync(string nextLink)
        {
            return _keyVaultClientImplementation.GetSecretsNextAsync(nextLink);
        }
    }
}