// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFilterTest
    {
        [Fact]
        public void FiltersMessages_OnDefaultLogLevel_BeforeSendingTo_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Default", LogLevel.Warning }
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);

            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");

            // Act
            logger1.LogCritical("critical event");
            logger1.LogDebug("debug event");
            logger1.LogInformation("information event");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("Microsoft.Foo"));
                var logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal("critical event", logEventWrite.State?.ToString());
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
            }
        }

        [Fact]
        public void FiltersMessages_BeforeSendingTo_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                        { "Microsoft", LogLevel.Warning },
                        { "System", LogLevel.Warning },
                        { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);
            var microsoftAssemblyLogger = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");
            var systemAssemblyLogger = loggerFactoryFromHost.CreateLogger("System.Foo");
            var myappAssemblyLogger = loggerFactoryFromHost.CreateLogger("SampleApp.Program");

            // Act
            microsoftAssemblyLogger.LogCritical("critical event");
            microsoftAssemblyLogger.LogDebug("debug event");
            microsoftAssemblyLogger.LogInformation("information event");
            systemAssemblyLogger.LogCritical("critical event");
            systemAssemblyLogger.LogDebug("debug event");
            systemAssemblyLogger.LogInformation("information event");
            myappAssemblyLogger.LogCritical("critical event");
            myappAssemblyLogger.LogDebug("debug event");
            myappAssemblyLogger.LogInformation("information event");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("Microsoft"));
                var logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
                Assert.Equal("critical event", logEventWrite.State?.ToString());

                logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("System"));
                logEventWrite = Assert.Single(logEventWrites);
                Assert.Equal(LogLevel.Critical, logEventWrite.LogLevel);
                Assert.Equal("critical event", logEventWrite.State?.ToString());

                logEventWrites = sink.Writes.Where(wc => wc.LoggerName.StartsWith("SampleApp.Program"));
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Critical));
                Assert.Equal("critical event", logEventWrite.State?.ToString());
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Debug));
                Assert.Equal("debug event", logEventWrite.State?.ToString());
                logEventWrite = Assert.Single(logEventWrites.Where(wc => wc.LogLevel == LogLevel.Information));
                Assert.Equal("information event", logEventWrite.State?.ToString());
            }
        }

        [Fact]
        public void BeginScope_CreatesScopesOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);

            var microsoftAssemblyLogger = loggerFactoryFromHost.CreateLogger("Microsoft.foo");
            var systemAssemblyLogger = loggerFactoryFromHost.CreateLogger("System.foo");
            var myappAssemblyLogger = loggerFactoryFromHost.CreateLogger("SampleApp.Program");

            // Act
            var disposable1 = systemAssemblyLogger.BeginScope("Scope1");
            var disposable2 = microsoftAssemblyLogger.BeginScope("Scope2");
            var disposable3 = myappAssemblyLogger.BeginScope("Scope3");

            // Assert
            foreach (var sink in new[] { loggerProvider1.Sink, loggerProvider2.Sink })
            {
                var scopeContexts = sink.Scopes;
                Assert.Equal(3, scopeContexts.Count);

                Assert.Equal("Scope1", scopeContexts[0].Scope?.ToString());
                Assert.NotNull(disposable1);

                Assert.Equal("Scope2", scopeContexts[1].Scope?.ToString());
                Assert.NotNull(disposable2);

                Assert.Equal("Scope3", scopeContexts[2].Scope?.ToString());
                Assert.NotNull(disposable3);
            }
        }

        [Fact]
        public void DisposeOnFilterLoggerFactory_DoesNotCallDisposeOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);

            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.foo");

            // Act
            filterLoggerFactory.Dispose();

            // Assert
            Assert.False(loggerProvider1.DisposeCalled);
            Assert.False(loggerProvider2.DisposeCalled);
        }

        [Fact]
        public void DisposeOnLoggerFactory_CallsDisposeOn_AllRegisteredLoggerProviders()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);

            // Imagine this to be the default logger factory that is provided by the host and is
            // present in DI.
            var loggerFactoryFromHost = new LoggerFactory();

            // Imagine this to be the user code which adds the wrapped logger providers.
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "SampleApp", LogLevel.Debug },
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);

            var logger1 = loggerFactoryFromHost.CreateLogger("Microsoft.foo");

            // Act
            loggerFactoryFromHost.Dispose();

            // Assert
            Assert.True(loggerProvider1.DisposeCalled);
            Assert.True(loggerProvider2.DisposeCalled);
        }

        [Fact]
        public void CanFilterMessagesAtProviderLevel_AfterFilterLoggerFactory_HasFilteredMessages()
        {
            // Arrange
            var loggerProvider1 = new TestLoggerProvider(new TestSink(), filter: level => level == LogLevel.Critical);
            var loggerProvider2 = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var loggerFactoryFromHost = new LoggerFactory();
            var filterLoggerFactory = loggerFactoryFromHost
                .WithFilter(new FilterLoggerSettings()
                {
                    { "Default", LogLevel.Warning }
                });
            filterLoggerFactory.AddProvider(loggerProvider1);
            filterLoggerFactory.AddProvider(loggerProvider2);

            var logger = loggerFactoryFromHost.CreateLogger("Microsoft.Foo");

            // Act
            logger.LogCritical("critical event");
            logger.LogWarning("warning event");
            logger.LogTrace("trace event");

            // Assert
            // This provider filters the messages further to only log 'critical' messages
            var sink1 = loggerProvider1.Sink;
            var logEventWrites = sink1.Writes.Where(wc => wc.LoggerName.Equals("Microsoft.Foo")).ToList();
            Assert.Equal(1, logEventWrites.Count);
            Assert.Equal("critical event", logEventWrites[0].State?.ToString());
            Assert.Equal(LogLevel.Critical, logEventWrites[0].LogLevel);

            var sink2 = loggerProvider2.Sink;
            logEventWrites = sink2.Writes.Where(wc => wc.LoggerName.Equals("Microsoft.Foo")).ToList();
            Assert.Equal(2, logEventWrites.Count);
            Assert.Equal("critical event", logEventWrites[0].State?.ToString());
            Assert.Equal(LogLevel.Critical, logEventWrites[0].LogLevel);
            Assert.Equal("warning event", logEventWrites[1].State?.ToString());
            Assert.Equal(LogLevel.Warning, logEventWrites[1].LogLevel);
        }

        [Fact]
        public void ChangingConfigReloadsDefaultFilter()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Information""
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Trace""
    }
  }
}";
            config.Reload();

            // Act
            logger.LogTrace("Message");

            // Assert
            writes = loggerProvider.Sink.Writes;
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void CanFilterOnNamedProviders()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""CustomName"": {
      ""LogLevel"": {
        ""Microsoft"": ""Information""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("CustomName", loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(0, writes.Count);
        }

        [Fact]
        public void PreferCustomProviderNameOverFullNameForFiltering()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""CustomName"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    },
    ""Microsoft.Extensions.Logging.Testing.TestLogger"": {
      ""LogLevel"": {
        ""Microsoft"": ""Critical""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("CustomName", loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void PreferFullNameOverShortNameForFiltering()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""TestLogger"": {
      ""LogLevel"": {
        ""Microsoft"": ""Critical""
      }
    },
    ""Microsoft.Extensions.Logging.Testing.TestLogger"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void PreferShortNameOverDefaultForFiltering()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Default"": {
      ""LogLevel"": {
        ""Microsoft"": ""Critical""
      }
    },
    ""TestLogger"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void CanHaveMultipleProvidersOfSameTypeWithDifferentNames()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Custom1"": {
      ""LogLevel"": {
        ""Microsoft"": ""Critical""
      }
    },
    ""Custom2"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Custom1", loggerProvider);
            factory.AddProvider("Custom2", loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(1, writes.Count);

            json =
@"{
  ""Logging"": {
    ""Custom1"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    },
    ""Custom2"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    }
  }
}";
            config.Reload();

            // Act
            logger.LogTrace("Message");

            // Assert
            writes = loggerProvider.Sink.Writes;
            Assert.Equal(3, writes.Count);
        }

        [Fact]
        public void DefaultCategoryNameIsUsedIfNoneMatch()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Name"": {
      ""LogLevel"": {
        ""Default"": ""Information"",
        ""Microsoft"": ""Warning""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            // No config value for 'None' so should use 'Default'
            logger = factory.CreateLogger("None");

            // Act
            logger.LogTrace("Message");

            // Assert
            Assert.Equal(0, writes.Count);

            // Act
            logger.LogInformation("Message");

            // Assert
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void SupportLegacyTopLevelLogLevelConfig()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Critical""
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(loggerProvider);

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(0, writes.Count);
        }

        [Fact]
        public void AddFilterForMatchingProviderFilters()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter((name, cat, level) =>
            {
                if (string.Equals("Name", name))
                {
                    if (string.Equals("Test", cat))
                    {
                        return level >= LogLevel.Information;
                    }
                }

                return true;
            });

            var logger = factory.CreateLogger("Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(1, writes.Count);

            logger.LogTrace("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterForNonMatchingProviderDoesNotFilter()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(provider);
            factory.AddFilter((name, cat, level) =>
            {
                if (string.Equals("None", name))
                {
                    return level >= LogLevel.Error;
                }

                return true;
            });

            var logger = factory.CreateLogger("Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithDictionaryFiltersDifferentCategories()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", new Dictionary<string, LogLevel>
            {
                { "Test", LogLevel.Warning },
                { "Microsoft", LogLevel.Information }
            });

            var logger = factory.CreateLogger("Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger = factory.CreateLogger("Microsoft");
            logger.LogInformation("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterIsAdditive()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider(provider);
            factory.AddFilter((name, cat, level) => level >= LogLevel.Warning);
            factory.AddFilter((name, cat, level) => string.Equals(cat, "NotTest"));

            var logger = factory.CreateLogger("Test");

            logger.LogWarning("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger = factory.CreateLogger("NotTest");

            logger.LogInformation("Message");

            Assert.Equal(0, writes.Count);

            logger.LogError("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterIsAdditiveWithConfigurationFilter()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Name"": {
      ""LogLevel"": {
        ""Test"": ""Error""
      }
    }
  }
}";
            var config = CreateConfiguration(() => json);
            var factory = new LoggerFactory(config.GetSection("Logging"));
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", loggerProvider);
            factory.AddFilter((name, cat, level) => level < LogLevel.Critical);

            var logger = factory.CreateLogger("Test");

            logger.LogCritical("Message");

            var writes = loggerProvider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(0, writes.Count);

            logger.LogError("Message");
            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithDictionarySplitsCategoryNameByDots()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", new Dictionary<string, LogLevel>
            {
                { "Sample", LogLevel.Warning }
            });

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithProviderNameCategoryNameAndFilterFuncFiltersCorrectly()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", "Sample", l => l >= LogLevel.Warning);

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithProviderNameCategoryNameAndMinLevelFiltersCorrectly()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", "Sample", LogLevel.Warning);

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithProviderNameAndCategoryFilterFuncFiltersCorrectly()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", (c, l) => l >= LogLevel.Warning);

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(1, writes.Count);
        }

        [Fact]
        public void AddFilterWithProviderNameAndFilterFuncFiltersCorrectly()
        {
            var factory = new LoggerFactory();
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            factory.AddProvider("Name", provider);
            factory.AddFilter("Name", l => l >= LogLevel.Warning);

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Equal(0, writes.Count);

            logger.LogWarning("Message");

            Assert.Equal(1, writes.Count);
        }

        internal ConfigurationRoot CreateConfiguration(Func<string> getJson)
        {
            var provider = new TestConfiguration(new JsonConfigurationSource { Optional = true }, getJson);
            return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
        }

        private class TestConfiguration : JsonConfigurationProvider
        {
            private Func<string> _json;
            public TestConfiguration(JsonConfigurationSource source, Func<string> json)
                : base(source)
            {
                _json = json;
            }

            public override void Load()
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(_json());
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                Load(stream);
            }
        }
    }
}
