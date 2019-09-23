// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class ProjectEngineFactories
    {
        public static readonly Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[] Factories =
            new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[]
            {
                // Razor based configurations
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new DefaultProjectEngineFactory(),
                    new ExportCustomProjectEngineFactoryAttribute("Default") { SupportsSerialization = true }),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_1_0(),
                    new ExportCustomProjectEngineFactoryAttribute("MVC-1.0") { SupportsSerialization = true }),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_1_1(),
                    new ExportCustomProjectEngineFactoryAttribute("MVC-1.1") { SupportsSerialization = true }),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_2_0(),
                    new ExportCustomProjectEngineFactoryAttribute("MVC-2.0") { SupportsSerialization = true }),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_2_1(),
                    new ExportCustomProjectEngineFactoryAttribute("MVC-2.1") { SupportsSerialization = true }),
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_3_0(),
                    new ExportCustomProjectEngineFactoryAttribute("MVC-3.0") { SupportsSerialization = true }),

                // Unsupported (Legacy/System.Web.Razor)
                new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                    () => new ProjectEngineFactory_Unsupported(),
                    new ExportCustomProjectEngineFactoryAttribute(UnsupportedRazorConfiguration.Instance.ConfigurationName) { SupportsSerialization = true }),
            };
    }
}
