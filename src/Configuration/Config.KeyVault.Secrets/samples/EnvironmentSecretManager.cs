
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.KeyVault.Secrets;

namespace ConsoleApplication
{
    public class EnvironmentSecretManager : DefaultKeyVaultSecretManager
    {
        private readonly string _environmentPrefix;

        public EnvironmentSecretManager(string environment)
        {
            _environmentPrefix = environment + "-";
        }

        public override bool Load(SecretProperties secret)
        {
            return HasEnvironmentPrefix(secret.Name);
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            var keyName = base.GetKey(secret);

            return HasEnvironmentPrefix(keyName)
                ? keyName.Substring(_environmentPrefix.Length)
                : keyName;
        }

        private bool HasEnvironmentPrefix(string name)
        {
            return name.StartsWith(_environmentPrefix);
        }
    }
}
