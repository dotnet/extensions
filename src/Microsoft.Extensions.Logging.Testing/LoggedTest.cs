// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public abstract class LoggedTest
    {
        private ILoggerFactory _loggerFactory;

        // Obsolete but keeping for back compat
        public LoggedTest(ITestOutputHelper output = null)
        {
            TestOutputHelper = output;
        }

        // Internal for testing
        internal string TestMethodTestName { get; set; }

        public ILogger Logger { get; set; }

        public ILoggerFactory LoggerFactory
        {
            get
            {
                return _loggerFactory;
            }
            set
            {
                _loggerFactory = value;
                AddTestLogging = services => services.AddSingleton(_loggerFactory);
            }
        }

        public ITestOutputHelper TestOutputHelper { get; set; }

        public ITestSink TestSink { get; set; }

        public Action<IServiceCollection> AddTestLogging { get; private set; } = services => { };

        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) => StartLog(out loggerFactory, LogLevel.Information, testName);

        public IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return AssemblyTestLog.ForAssembly(GetType().GetTypeInfo().Assembly).StartTestLog(TestOutputHelper, GetType().FullName, out loggerFactory, minLogLevel, testName);
        }
    }
}
