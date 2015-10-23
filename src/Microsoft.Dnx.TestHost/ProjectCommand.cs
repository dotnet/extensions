// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime.CommandParsing;
using Microsoft.Dnx.Runtime.Common;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Dnx.TestHost
{
    public static class ProjectCommand
    {
        public static async Task<int> Execute(
            IServiceProvider services,
            Project project,
            string command,
            string[] args)
        {
            var environment = PlatformServices.Default.Application;
            var commandText = project.Commands[command];
            var replacementArgs = CommandGrammar.Process(
                commandText,
                (key) => GetVariable(environment, key),
                preserveSurroundingQuotes: false)
                .ToArray();

            var entryPoint = replacementArgs[0];
            args = replacementArgs.Skip(1).Concat(args).ToArray();

            if (string.IsNullOrEmpty(entryPoint) ||
                string.Equals(entryPoint, "run", StringComparison.Ordinal))
            {
                entryPoint = project.Name;
            }

            CallContextServiceLocator.Locator.ServiceProvider = services;
            return await ExecuteMain(services, entryPoint, args);
        }

        private static string GetVariable(IApplicationEnvironment environment, string key)
        {
            if (string.Equals(key, "env:ApplicationBasePath", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationBasePath;
            }
            if (string.Equals(key, "env:ApplicationName", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationName;
            }
            if (string.Equals(key, "env:Version", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationVersion;
            }
            if (string.Equals(key, "env:TargetFramework", StringComparison.OrdinalIgnoreCase))
            {
                return environment.RuntimeFramework.Identifier;
            }

            return Environment.GetEnvironmentVariable(key);
        }

        private static Task<int> ExecuteMain(IServiceProvider services, string entryPoint, string[] args)
        {
            var assembly = Assembly.Load(new AssemblyName(entryPoint));
            return EntryPointExecutor.Execute(assembly, args, services);
        }
    }
}
