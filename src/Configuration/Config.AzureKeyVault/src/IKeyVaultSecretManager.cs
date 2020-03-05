// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.KeyVault.Models;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// The <see cref="IKeyVaultSecretManager"/> instance used to control secret loading.
    /// </summary>
    public interface IKeyVaultSecretManager
    {
        /// <summary>
        /// Checks if <see cref="SecretItem"/> value should be retrieved.
        /// </summary>
        /// <param name="secret">The <see cref="SecretItem"/> instance.</param>
        /// <returns><see langword="true" /> if secrets value should be loaded, otherwise <see langword="false" />.</returns>
        bool Load(SecretItem secret);

        /// <summary>
        /// Maps secret to a configuration key.
        /// </summary>
        /// <param name="secret">The <see cref="SecretBundle"/> instance.</param>
        /// <returns>Configuration key name to store secret value.</returns>
        string GetKey(SecretBundle secret);
    }
}
