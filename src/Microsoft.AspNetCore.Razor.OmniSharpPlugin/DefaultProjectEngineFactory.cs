// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(ProjectEngineFactory))]
    internal class DefaultProjectEngineFactory : ProjectEngineFactory
    {
        private readonly object InitializationLock = new object();
        private object _defaultEngineFactory;
        private MethodInfo _createMethod;

        public override RazorProjectEngine Create(
            RazorConfiguration configuration,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            lock (InitializationLock)
            {
                if (_defaultEngineFactory == null)
                {
                    var razorWorkspaces = Assembly.Load("Microsoft.CodeAnalysis.Razor.Workspaces");
                    var fallbackEngineFactoryType = razorWorkspaces.GetType("Microsoft.CodeAnalysis.Razor.FallbackProjectEngineFactory");
                    var fallbackEngineFactory = Activator.CreateInstance(fallbackEngineFactoryType);

                    var defaultEngineFactoryType = razorWorkspaces.GetType("Microsoft.CodeAnalysis.Razor.DefaultProjectSnapshotProjectEngineFactory");
                    _defaultEngineFactory = Activator.CreateInstance(defaultEngineFactoryType, fallbackEngineFactory, ProjectEngineFactories.Factories);
                    _createMethod = defaultEngineFactoryType.GetMethod("Create", new Type[] {
                        typeof(RazorConfiguration),
                        typeof(RazorProjectFileSystem),
                        typeof(Action<RazorProjectEngineBuilder>)
                    });
                }
            }

            var engine = _createMethod.Invoke(_defaultEngineFactory, new object[] { configuration, fileSystem, configure });

            return (RazorProjectEngine)engine;
        }
    }
}
