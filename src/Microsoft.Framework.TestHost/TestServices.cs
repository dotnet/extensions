// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.TestAdapter;
using Microsoft.Framework.TestHost.TestAdapter;

namespace Microsoft.Framework.TestHost
{
    internal static class TestServices
    {
        public static ServiceProvider CreateTestServices(
            IServiceProvider applicationServices,
            Project project,
            ReportingChannel channel)
        {
            var services = new ServiceProvider(applicationServices);

            var libraryManager = applicationServices.GetRequiredService<ILibraryManager>();
            var export = libraryManager.GetLibraryExport(project.Name);

            var projectReference = export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .Where(r => r.Name == project.Name)
                .FirstOrDefault();

            services.Add(
                typeof(ISourceInformationProvider),
                new SourceInformationProvider(projectReference, new NullLogger()));

            services.Add(typeof(ITestDiscoverySink), new TestDiscoverySink(channel));
            services.Add(typeof(ITestExecutionSink), new TestExecutionSink(channel));

            return services;
        }

        private class NullLogger : ILogger
        {
            public IDisposable BeginScope(object state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
            }
        }
    }
}