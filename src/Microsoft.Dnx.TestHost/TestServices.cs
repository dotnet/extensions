// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Dnx.Testing.Abstractions;
using Microsoft.Dnx.TestHost.TestAdapter;
using Microsoft.Dnx.Runtime.Common.DependencyInjection;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Compilation;

namespace Microsoft.Dnx.TestHost
{
    internal static class TestServices
    {
        public static ServiceProvider CreateTestServices(
            IServiceProvider applicationServices,
            Project project,
            ReportingChannel channel)
        {
            var services = new ServiceProvider(applicationServices);

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new TestHostLoggerProvider(channel));
            services.Add(typeof(ILoggerFactory), loggerFactory);

            var libraryExporter = applicationServices.GetRequiredService<ILibraryExporter>();
            var export = libraryExporter.GetExport(project.Name);

            var projectReference = export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .Where(r => r.Name == project.Name)
                .FirstOrDefault();

            services.Add(
                typeof(ISourceInformationProvider),
                new SourceInformationProvider(projectReference, loggerFactory.CreateLogger<SourceInformationProvider>()));

            services.Add(typeof(ITestDiscoverySink), new TestDiscoverySink(channel));
            services.Add(typeof(ITestExecutionSink), new TestExecutionSink(channel));

            return services;
        }
    }
}