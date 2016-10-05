// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.Test;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.AzureKeyVault.Test
{
    public class AzureKeyVaultConfigurationTest
    {
        private const string VaultUri = "https://vault";

        [Fact]
        public void LoadsAllSecretsFromVault()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new ListSecretsResponseMessage()
            {
                NextLink = "next",
                Value = new[] { new SecretItem { Id = secret1Id } }
            });

            client.Setup(c => c.GetSecretsNextAsync("next")).ReturnsAsync(new ListSecretsResponseMessage()
            {
                Value = new[] { new SecretItem { Id = secret2Id } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new Secret() { Value = "Value1", Id = secret1Id });
            client.Setup(c => c.GetSecretAsync(secret2Id)).ReturnsAsync(new Secret() { Value = "Value2", Id = secret2Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
            Assert.Equal(new[] { "Secret1", "Secret2" }, childKeys);
            Assert.Equal("Value1", provider.Get("Secret1"));
            Assert.Equal("Value2", provider.Get("Secret2"));
        }

        [Fact]
        public void DoesNotLoadFilteredItems()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new ListSecretsResponseMessage()
            {
                Value = new[] { new SecretItem { Id = secret1Id }, new SecretItem { Id = secret2Id } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new Secret() { Value = "Value1", Id = secret1Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new EndsWithOneKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
            Assert.Equal(new[] { "Secret1" }, childKeys);
            Assert.Equal("Value1", provider.Get("Secret1"));
        }

        [Fact]
        public void SupportsReload()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var value = "Value1";

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new ListSecretsResponseMessage()
            {
                Value = new[] { new SecretItem { Id = secret1Id } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new Secret() { Value = value, Id = id }));

            // Act & Assert
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            client.VerifyAll();
            Assert.Equal("Value1", provider.Get("Secret1"));

            value = "Value2";
            provider.Load();
            Assert.Equal("Value2", provider.Get("Secret1"));
        }

        [Fact]
        public void ReplaceDoubleMinusInKeyName()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Section--Secret1");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new ListSecretsResponseMessage()
            {
                Value = new[] { new SecretItem { Id = secret1Id } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new Secret() { Value = "Value1", Id = secret1Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            Assert.Equal("Value1", provider.Get("Section:Secret1"));
        }

        private string GetSecretId(string name) => new SecretIdentifier(VaultUri, name).Identifier;

        private class EndsWithOneKeyVaultSecretManager : DefaultKeyVaultSecretManager
        {
            public override bool Load(SecretItem secret)
            {
                return secret.Identifier.Name.EndsWith("1");
            }
        }
    }
}
