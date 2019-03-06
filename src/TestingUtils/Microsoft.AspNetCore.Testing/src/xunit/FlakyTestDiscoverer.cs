using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    public class FlakyTestDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            if (traitAttribute is ReflectionAttributeInfo attribute && attribute.Attribute is FlakyAttribute flakyAttribute)
            {
                return GetTraitsCore(flakyAttribute);
            }
            else
            {
                throw new InvalidOperationException("The 'Flaky' attribute is only supported via reflection.");
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetTraitsCore(FlakyAttribute attribute)
        {
            if(attribute.OnAzDO)
            {
                yield return new KeyValuePair<string, string>("Flaky:AzDO", "true");
            }

            foreach(var queue in attribute.FlakyQueues)
            {
                yield return new KeyValuePair<string, string>($"Flaky:Helix:{queue}", "true");
            }
        }
    }
}
