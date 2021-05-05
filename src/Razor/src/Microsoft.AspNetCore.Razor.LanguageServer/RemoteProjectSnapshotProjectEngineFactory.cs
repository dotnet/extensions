// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteProjectSnapshotProjectEngineFactory : DefaultProjectSnapshotProjectEngineFactory
    {
        public static readonly IFallbackProjectEngineFactory FallbackProjectEngineFactory = new FallbackProjectEngineFactory();

        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;

        public RemoteProjectSnapshotProjectEngineFactory(FilePathNormalizer filePathNormalizer, IOptionsMonitor<RazorLSPOptions> optionsMonitor) : 
            base(FallbackProjectEngineFactory, ProjectEngineFactories.Factories)
        {
            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _filePathNormalizer = filePathNormalizer;
            _optionsMonitor = optionsMonitor;
        }

        public override RazorProjectEngine Create(
            RazorConfiguration configuration,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            if (!(fileSystem is DefaultRazorProjectFileSystem defaultFileSystem))
            {
                Debug.Fail("Unexpected file system.");
                return null;
            }

            var remoteFileSystem = new RemoteRazorProjectFileSystem(defaultFileSystem.Root, _filePathNormalizer);
            return base.Create(configuration, remoteFileSystem, Configure);

            void Configure(RazorProjectEngineBuilder builder)
            {
                configure(builder);
                builder.Features.Add(new RemoteCodeGenerationOptionsFeature(_optionsMonitor));
            }
        }

        private class RemoteCodeGenerationOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;

            public RemoteCodeGenerationOptionsFeature(IOptionsMonitor<RazorLSPOptions> optionsMonitor)
            {
                if (optionsMonitor is null)
                {
                    throw new ArgumentNullException(nameof(optionsMonitor));
                }

                _optionsMonitor = optionsMonitor;
            }

            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                // We don't need to explicitly subscribe to options changing because this method will be run on every parse.
                options.IndentSize = _optionsMonitor.CurrentValue.TabSize;
                options.IndentWithTabs = !_optionsMonitor.CurrentValue.InsertSpaces;
            }
        }
    }
}
