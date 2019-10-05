// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <inheritdoc />
    internal class KeyVaultClientWrapper : IKeyVaultClient
    {
        private sealed class EmptyPage : IPage<SecretItem>
        {
            public static readonly EmptyPage Instance = new EmptyPage();

            public IEnumerator<SecretItem> GetEnumerator()
            {
                return Enumerable.Empty<SecretItem>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public string NextPageLink { get; } = null;
        }

        private readonly KeyVaultClient _keyVaultClientImplementation;
        private readonly bool _optional;

        /// <summary>
        /// Creates a new instance of <see cref="KeyVaultClientWrapper"/>.
        /// </summary>
        /// <param name="keyVaultClientImplementation">The <see cref="KeyVaultClient"/> instance to wrap.</param>
        /// <param name="optional">Whether GetSecretsAsync returns an empty page instead of throwing when retrieval fails.</param>
        public KeyVaultClientWrapper(KeyVaultClient keyVaultClientImplementation, bool optional)
        {
            _keyVaultClientImplementation = keyVaultClientImplementation;
            _optional = optional;
        }

        /// <inheritdoc />
        public async Task<IPage<SecretItem>> GetSecretsAsync(string vault)
        {
            try
            {
                return await _keyVaultClientImplementation.GetSecretsAsync(vault);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                //Host unknown or not reachable.
                if (!_optional)
                {
                    throw;
                }
            }
            catch (Azure.Services.AppAuthentication.AzureServiceTokenProviderException)
            {
                //Not logged into Azure (locally) or no managed identity configured (Azure)
                if (!_optional)
                {
                    throw;
                }
            }
            catch (KeyVaultErrorException)
            {
                //No permission to read secret list from key vault.
                if (!_optional)
                {
                    throw;
                }
            }

            return EmptyPage.Instance;
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
