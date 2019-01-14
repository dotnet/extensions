// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// An AzureKeyVault based <see cref="ConfigurationProvider"/>.
    /// </summary>
    internal class AzureKeyVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly IKeyVaultClient _client;
        private readonly string _vault;
        private readonly IKeyVaultSecretManager _manager;

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationProvider"/>.
        /// </summary>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager"></param>
        public AzureKeyVaultConfigurationProvider(IKeyVaultClient client, string vault, IKeyVaultSecretManager manager)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (vault == null)
            {
                throw new ArgumentNullException(nameof(vault));
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            _client = client;
            _vault = vault;
            _manager = manager;
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var secrets = await _client.GetSecretsAsync(_vault).ConfigureAwait(false);
            do
            {
                var tasks = new List<Task<KeyValuePair<string, string>>>();
                foreach (var secretItem in secrets)
                {
                    if (!_manager.Load(secretItem) || secretItem.Attributes?.Enabled != true)
                    {
                        continue;
                    }
                    tasks.Add(LoadSecretValue(secretItem));
                }

                await Task.WhenAll(tasks);

                foreach (var task in tasks)
                {
                    data.Add(task.Result.Key, task.Result.Value);
                }

                secrets = secrets.NextPageLink != null ?
                    await _client.GetSecretsNextAsync(secrets.NextPageLink).ConfigureAwait(false) :
                    null;
            } while (secrets != null);

            Data = data;
        }

        private async Task<KeyValuePair<string, string>> LoadSecretValue(SecretItem secretItem)
        {
            var value = await _client.GetSecretAsync(secretItem.Id).ConfigureAwait(false);
            var key = _manager.GetKey(value);
            return new KeyValuePair<string, string>(key, value.Value);
        }
    }
}
