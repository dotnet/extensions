// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Helper;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel.Test
{
    public class ConfigurationHelperTest
    {
        [Fact]
        public void ResolveFilePath()
        {
            var testFile = Path.GetTempFileName();
            var testDir = Path.GetDirectoryName(testFile);
            var testFileName = Path.GetFileName(testFile);
            var mockSourceRoot = new MockConfigurationSourceRoot { BasePath = testDir };

            var actualPath = ConfigurationHelper.ResolveConfigurationFilePath(mockSourceRoot, testFileName);

            Assert.Equal(testFile, actualPath);
        }

        [Fact]
        public void ThrowWhenBasePathIsNull()
        {
            var testFile = "config.j";
            var mockSourceRoot = new MockConfigurationSourceRoot();
            var expectErrorMessage = Resources.FormatError_MissingBasePath(
                testFile,
                typeof(IConfigurationSourceRoot).Name,
                nameof(mockSourceRoot.BasePath));

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                ConfigurationHelper.ResolveConfigurationFilePath(mockSourceRoot, testFile);
            });

            Assert.Equal(expectErrorMessage, exception.Message);
        }

        [Fact]
        public void NotThrowWhenFileDoesNotExists()
        {
            var testFile = Path.GetTempFileName();
            var testDir = Path.GetDirectoryName(testFile);
            var testFileName = Path.GetFileName(testFile);
            var mockSourceRoot = new MockConfigurationSourceRoot { BasePath = testDir };

            File.Delete(testFile);

            var path = ConfigurationHelper.ResolveConfigurationFilePath(mockSourceRoot, testFileName);

            Assert.Equal(testFile, path);
        }

        private class MockConfigurationSourceRoot : IConfigurationSourceRoot
        {
            public string this[string key]
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public string BasePath
            {
                get; set;
            }

            public IEnumerable<IConfigurationSource> Sources
            {
                get { throw new NotImplementedException(); }
            }

            public IConfigurationSourceRoot Add(IConfigurationSource configurationSource)
            {
                throw new NotImplementedException();
            }

            public string Get(string key)
            {
                throw new NotImplementedException();
            }

            public IConfiguration GetConfigurationSection(string key)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections(string key)
            {
                throw new NotImplementedException();
            }

            public void Reload()
            {
                throw new NotImplementedException();
            }

            public void Set(string key, string value)
            {
                throw new NotImplementedException();
            }

            public bool TryGet(string key, out string value)
            {
                throw new NotImplementedException();
            }
        }
    }
}