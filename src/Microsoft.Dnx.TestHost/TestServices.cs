// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime.Common.DependencyInjection;
using Microsoft.Dnx.TestHost.TestAdapter;
using Microsoft.Dnx.Testing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Dnx.TestHost
{
    internal static class TestServices
    {
        public static ServiceProvider CreateTestServices(
            Project project,
            ReportingChannel channel)
        {
            var services = new ServiceProvider();

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new TestHostLoggerProvider(channel));
            services.Add(typeof(ILoggerFactory), loggerFactory);
            services.Add(typeof(IApplicationEnvironment), PlatformServices.Default.Application);
            services.Add(typeof(ILibraryManager), PlatformServices.Default.LibraryManager);

            var libraryExporter = CompilationServices.Default.LibraryExporter;
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