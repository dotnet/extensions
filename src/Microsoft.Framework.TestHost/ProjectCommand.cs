// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using System.ComponentModel.Design;

namespace Microsoft.Framework.TestHost
{
    public class ProjectCommand
    {
        public static async Task<int> Execute(
            IServiceProvider services, 
            Project project,
            string[] args)
        {
            var oldEnvironment = (IApplicationEnvironment)services.GetService(typeof(IApplicationEnvironment));

            var environment = new ApplicationEnvironment(
                project, 
                oldEnvironment.RuntimeFramework, 
                oldEnvironment.Configuration);

            var newServices = new ServiceContainer(services);
            newServices.AddService(typeof(IApplicationEnvironment), environment);

            var applicationHost = new Microsoft.Framework.ApplicationHost.Program(
                (IAssemblyLoaderContainer)services.GetService(typeof(IAssemblyLoaderContainer)),
                environment,
                newServices);

            return await applicationHost.Main(args);
        }
    }
}