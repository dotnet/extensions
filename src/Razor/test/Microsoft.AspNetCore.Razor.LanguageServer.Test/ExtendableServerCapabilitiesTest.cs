// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ExtendableServerCapabilitiesTest
    {
        [Fact]
        public void Constructor_RegistrationExtensions_Populates()
        {
            // Arrange
            var registrationExtension1 = new TestRegistrationExtension("test1");
            var registrationExtension2 = new TestRegistrationExtension("test2");
            var registrations = new IRegistrationExtension[] { registrationExtension1, registrationExtension2 };
            var baseCapability = new ServerCapabilities();

            // Act
            var extendableCapabilities = new ExtendableServerCapabilities(baseCapability, registrations);

            // Assert
            Assert.Equal(new[] { "test1", "test2" }, extendableCapabilities.CapabilityExtensions.Keys.ToArray());
        }

        [Fact]
        public void CapabilityExtensions_RoundTripsCorrectly()
        {
            // Arrange
            var registrationExtension = new TestRegistrationExtension("test1");
            var registrations = new IRegistrationExtension[] { registrationExtension };
            var baseCapability = new ServerCapabilities();
            var extendableCapabilities = new ExtendableServerCapabilities(baseCapability, registrations) {
                TextDocumentSync = new TextDocumentSync(TextDocumentSyncKind.Full),
            };

            // Act
            var serialized = JsonConvert.SerializeObject(extendableCapabilities);
            var deserialized = JsonConvert.DeserializeObject<VSCapabilities>(serialized);

            // Assert
            Assert.True(deserialized.Test1);
        }

        private class VSCapabilities : ServerCapabilities
        {
            public bool Test1 { get; set; }
        }

        private class TestRegistrationExtension : IRegistrationExtension
        {
            private readonly string _capabilityName;

            public TestRegistrationExtension(string capabilityName)
            {
                _capabilityName = capabilityName;
            }

            public RegistrationExtensionResult GetRegistration() => new RegistrationExtensionResult(_capabilityName, true);
        }
    }
}
