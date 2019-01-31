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
        private readonly bool _reloadOnChange;
        private readonly int _reloadPollDelay;
        private readonly IKeyVaultClient _client;
        private readonly string _vault;
        private readonly IKeyVaultSecretManager _manager;
        private Dictionary<string, SecretAttributes> _loadedSecrets;

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationProvider"/>.
        /// </summary>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager"></param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the Azure KeyVault changes.</param>
        /// <param name="reloadPollDelay">Number of milliseconds to wait inbetween each attempt at polling the Azure KeyVault for changes.</param>
        public AzureKeyVaultConfigurationProvider(IKeyVaultClient client, string vault, IKeyVaultSecretManager manager, bool reloadOnChange = false, int reloadPollDelay = 10000)
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

            if(reloadOnChange && reloadPollDelay <= 0)
            {
                throw new ArgumentException("{0} must be greater than 0", nameof(reloadPollDelay));
            }

            _reloadOnChange = reloadOnChange;
            _reloadPollDelay = reloadPollDelay;
            _client = client;
            _vault = vault;
            _manager = manager;
            _loadedSecrets = new Dictionary<string, SecretAttributes>();
        }
        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task PollForSecretChangesAsync(int reloadPollDelay)
        {
            while (true)
            {
                await Task.Delay(reloadPollDelay);
                var secretsHaveChanged = await ShouldReloadAsync();

                // if the secret list has changed, reload.
                if(secretsHaveChanged)
                {
                    Load();
                }
            }
        }

        private async Task LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _loadedSecrets.Clear();

            var secrets = await _client.GetSecretsAsync(_vault).ConfigureAwait(false);
            var tasks = new List<Task<SecretBundle>>(secrets.Count());

            do
            {
                tasks.Clear();

                foreach (var secretItem in secrets)
                {
                    if (_manager.Load(secretItem) && secretItem.Attributes?.Enabled == true)
                    {
                        tasks.Add(_client.GetSecretAsync(secretItem.Id));
                        _loadedSecrets.Add(secretItem.Identifier.Name, secretItem.Attributes);
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var task in tasks)
                {
                    data.Add(_manager.GetKey(task.Result), task.Result.Value);
                }

                secrets = secrets.NextPageLink != null ?
                    await _client.GetSecretsNextAsync(secrets.NextPageLink).ConfigureAwait(false) :
                    null;
            } while (secrets != null);

            Data = data;
            OnReload();

            if (_reloadOnChange)
            {
                var task = PollForSecretChangesAsync(_reloadPollDelay);
            }
        }

        private async Task<bool> ShouldReloadAsync()
        {
            var secrets = await _client.GetSecretsAsync(_vault).ConfigureAwait(false);
            if (secrets.Count() != _loadedSecrets.Count())
            {
                return true;
            }

            foreach (var secret in secrets)
            {
                if (!_loadedSecrets.ContainsKey(secret.Identifier.Name))
                {
                    return true;
                }

                long? currentSecretLastUpdateTick = secret.Attributes.Updated.Value.Ticks;
                long? loadedSecretLastUpdateTick = _loadedSecrets[secret.Identifier.Name].Updated.Value.Ticks;

                if(currentSecretLastUpdateTick != null && loadedSecretLastUpdateTick != null)
                {
                    bool isKeyUpdateTimeDifferent = (loadedSecretLastUpdateTick != currentSecretLastUpdateTick);

                    if (isKeyUpdateTimeDifferent)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
