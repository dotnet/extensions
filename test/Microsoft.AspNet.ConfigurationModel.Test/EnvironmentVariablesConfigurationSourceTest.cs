// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using Xunit;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class EnvironmentVariablesConfigurationSourceTest
    {
        [Fact]
        public void LoadKeyValuePairsFromEnvironmentDictionary()
        {
            var dic = new Hashtable()
                {
                    {"DefaultConnection:ConnectionString", "TestConnectionString"},
                    {"DefaultConnection:Provider", "SqlClient"},
                    {"Inventory:ConnectionString", "AnotherTestConnectionString"},
                    {"Inventory:Provider", "MySql"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();

            envConfigSrc.Load(dic);

            Assert.Equal(4, envConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", envConfigSrc.Data["defaultconnection:ConnectionString"]);
            Assert.Equal("SqlClient", envConfigSrc.Data["DEFAULTCONNECTION:PROVIDER"]);
            Assert.Equal("AnotherTestConnectionString", envConfigSrc.Data["Inventory:CONNECTIONSTRING"]);
            Assert.Equal("MySql", envConfigSrc.Data["Inventory:Provider"]);
        }

        [Fact]
        public void LoadKeyValuePairsFromEnvironmentDictionaryWithPrefix()
        {
            var dic = new Hashtable()
                {
                    {"DefaultConnection:ConnectionString", "TestConnectionString"},
                    {"DefaultConnection:Provider", "SqlClient"},
                    {"Inventory:ConnectionString", "AnotherTestConnectionString"},
                    {"Inventory:Provider", "MySql"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource("DefaultConnection:");

            envConfigSrc.Load(dic);

            Assert.Equal(2, envConfigSrc.Data.Count);
            Assert.Equal("TestConnectionString", envConfigSrc.Data["ConnectionString"]);
            Assert.Equal("SqlClient", envConfigSrc.Data["Provider"]);
        }

        [Fact]
        public void LoadKeyValuePairsFromAzureEnvironment()
        {
            var dic = new Hashtable()
                {
                    {"APPSETTING_AppName", "TestAppName"},
                    {"CUSTOMCONNSTR_db1", "CustomConnStr"},
                    {"SQLCONNSTR_db2", "SQLConnStr"},
                    {"MYSQLCONNSTR_db3", "MySQLConnStr"},
                    {"SQLAZURECONNSTR_db4", "SQLAzureConnStr"},
                    {"CommonEnv", "CommonEnvValue"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();

            envConfigSrc.Load(dic);

            Assert.Equal(9, envConfigSrc.Data.Count);
            Assert.Equal("TestAppName", envConfigSrc.Data["APPSETTING_AppName"]);
            Assert.False(envConfigSrc.Data.ContainsKey("AppName"));
            Assert.Equal("CustomConnStr", envConfigSrc.Data["Data:db1:ConnectionString"]);
            Assert.Equal("SQLConnStr", envConfigSrc.Data["Data:db2:ConnectionString"]);
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Data["Data:db2:ProviderName"]);
            Assert.Equal("MySQLConnStr", envConfigSrc.Data["Data:db3:ConnectionString"]);
            Assert.Equal("MySql.Data.MySqlClient", envConfigSrc.Data["Data:db3:ProviderName"]);
            Assert.Equal("SQLAzureConnStr", envConfigSrc.Data["Data:db4:ConnectionString"]);
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Data["Data:db4:ProviderName"]);
            Assert.Equal("CommonEnvValue", envConfigSrc.Data["CommonEnv"]);
        }

        [Fact]
        public void LoadKeyValuePairsFromAzureEnvironmentWithPrefix()
        {
            var dic = new Hashtable()
                {
                    {"CUSTOMCONNSTR_db1", "CustomConnStr"},
                    {"SQLCONNSTR_db2", "SQLConnStr"},
                    {"MYSQLCONNSTR_db3", "MySQLConnStr"},
                    {"SQLAZURECONNSTR_db4", "SQLAzureConnStr"},
                    {"CommonEnv", "CommonEnvValue"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource("Data:");

            envConfigSrc.Load(dic);

            Assert.Equal(7, envConfigSrc.Data.Count);
            Assert.Equal("CustomConnStr", envConfigSrc.Data["db1:ConnectionString"]);
            Assert.Equal("SQLConnStr", envConfigSrc.Data["db2:ConnectionString"]);
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Data["db2:ProviderName"]);
            Assert.Equal("MySQLConnStr", envConfigSrc.Data["db3:ConnectionString"]);
            Assert.Equal("MySql.Data.MySqlClient", envConfigSrc.Data["db3:ProviderName"]);
            Assert.Equal("SQLAzureConnStr", envConfigSrc.Data["db4:ConnectionString"]);
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Data["db4:ProviderName"]);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicatedInAzureEnvironment()
        {
            var dic = new Hashtable()
                {
                    {"Data:db2:ConnectionString", "CommonEnvValue"},
                    {"SQLCONNSTR_db2", "SQLConnStr"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();
            var expectedMsg = "An item with the same key has already been added.";

            var exception = Assert.Throws<ArgumentException>(() => envConfigSrc.Load(dic));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void HandleEmptyEnvironmentDictionaryProperly()
        {
            var dic = new Hashtable();
            var envConfigSrc = new EnvironmentVariablesConfigurationSource();

            envConfigSrc.Load(dic);

            Assert.Equal(0, envConfigSrc.Data.Count);
        }
    }
}
