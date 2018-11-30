// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Rest.Azure;
using Moq;

namespace Microsoft.Extensions.Configuration.AzureKeyVault.Test
{
    public class ConfigurationProviderKeyVaultTest : ConfigurationProviderTestBase
    {
        protected override (IConfigurationProvider Provider, System.Action Initializer) LoadThroughProvider(
            TestSection testConfig)
        {
            var values = new List<KeyValuePair<string, string>>();
            SectionToValues(testConfig, "", values);

            return (FromKeyVault(values), () => {});
        }

        private IConfigurationProvider FromKeyVault(IEnumerable<KeyValuePair<string, string>> data)
        {
            const string vaultUri = "https://vault";

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var rawData = data.ToList();
            var nextPageLink = vaultUri;

            for (var i = 0; i < rawData.Count; i++)
            {
                var currentPageLink = nextPageLink;
                nextPageLink = i < (rawData.Count - 1) ? $"next{i}" : null;

                var secretId = new SecretIdentifier(vaultUri, rawData[i].Key).Identifier;

                var pageMock = new PageMock
                {
                    NextPageLink = nextPageLink,
                    Value = new[] { new SecretItem { Id = secretId, Attributes = new SecretAttributes { Enabled = true } } }
                };

                if (i == 0)
                {
                    client.Setup(c => c.GetSecretsAsync(currentPageLink)).ReturnsAsync(pageMock);
                }
                else
                {
                    client.Setup(c => c.GetSecretsNextAsync(currentPageLink)).ReturnsAsync(pageMock);
                }

                client.Setup(c => c.GetSecretAsync(secretId)).ReturnsAsync(new SecretBundle() { Value = rawData[i].Value, Id = secretId });
            }

            return new AzureKeyVaultConfigurationProvider(
                    client.Object,
                    vaultUri,
                    new DefaultKeyVaultSecretManager());
        }

        private class PageMock : IPage<SecretItem>
        {
            public IEnumerable<SecretItem> Value { get; set; }
            public IEnumerator<SecretItem> GetEnumerator() => Value.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public string NextPageLink { get; set; }
        }
    }
}
