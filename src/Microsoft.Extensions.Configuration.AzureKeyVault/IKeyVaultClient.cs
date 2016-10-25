// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Client class to perform cryptographic key operations and vault operations
    /// against the Key Vault service.
    /// Thread safety: This class is thread-safe.
    /// </summary>
    internal interface IKeyVaultClient
    {
        /// <summary>List secrets in the specified vault</summary>
        /// <param name="vault">The URL for the vault containing the secrets.</param>
        /// <returns>A response message containing a list of secrets in the vault along with a link to the next page of secrets</returns>
        Task<IPage<SecretItem>> GetSecretsAsync(string vault);

        /// <summary>Gets a secret.</summary>
        /// <param name="secretIdentifier">The URL for the secret.</param>
        /// <returns>A response message containing the secret</returns>
        Task<SecretBundle> GetSecretAsync(string secretIdentifier);

        /// <summary>List the next page of secrets</summary>
        /// <param name="nextLink">nextLink value from a previous call to GetSecrets or GetSecretsNext</param>
        /// <returns>A response message containing a list of secrets in the vault along with a link to the next page of secrets</returns>
        Task<IPage<SecretItem>> GetSecretsNextAsync(string nextLink);
    }
}