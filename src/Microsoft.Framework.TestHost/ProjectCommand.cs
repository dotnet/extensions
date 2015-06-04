// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.Runtime.CommandParsing;
using Microsoft.Framework.Runtime.Common;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.Framework.TestHost
{
    public static class ProjectCommand
    {
        public static async Task<int> Execute(
            IServiceProvider services, 
            Project project,
            string command,
            string[] args)
        {
            var environment = (IApplicationEnvironment)services.GetService(typeof(IApplicationEnvironment));
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
                entryPoint = project.EntryPoint ?? project.Name;
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
                return environment.Version;
            }
            if (string.Equals(key, "env:TargetFramework", StringComparison.OrdinalIgnoreCase))
            {
                return environment.RuntimeFramework.Identifier;
            }

            return Environment.GetEnvironmentVariable(key);
        }

        private static Task<int> ExecuteMain(IServiceProvider services, string entryPoint, string[] args)
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.Load(new AssemblyName(entryPoint));
            }
            catch (FileNotFoundException ex) when (new AssemblyName(ex.FileName).Name == entryPoint)
            {
                if (ex.InnerException is ICompilationException)
                {
                    throw ex.InnerException;
                }

                throw new InvalidOperationException(
                    $"Unable to load application or execute command '{entryPoint}'.",
                    ex.InnerException);
            }

            return EntryPointExecutor.Execute(assembly, args, services);
        }
    }
}
