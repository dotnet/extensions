// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    public static class FileProviderExtensions
    {
        public static IConfigurationRoot ReloadOnChanged(this IConfigurationRoot config, string filename)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }
#if NET451
            var basePath = AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") as string ??
                AppDomain.CurrentDomain.BaseDirectory ?? 
                string.Empty;
#else
            var basePath = AppContext.BaseDirectory ?? string.Empty;
#endif
            return ReloadOnChanged(config, basePath, filename);
        }

        public static IConfigurationRoot ReloadOnChanged(
            this IConfigurationRoot config,
            string basePath,
            string filename)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var fileProvider = new PhysicalFileProvider(basePath);
            return ReloadOnChanged(config, fileProvider, filename);
        }

        public static IConfigurationRoot ReloadOnChanged(
            this IConfigurationRoot config,
            IFileProvider fileProvider,
            string filename)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            Action<object> callback = null;
            callback = _ =>
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the file changes after we take the token, then we'll process the update immediately upon
                // registering the callback.
                var token = fileProvider.Watch(filename);
                config.Reload();
                token.RegisterChangeCallback(callback, null);
            };

            fileProvider.Watch(filename).RegisterChangeCallback(callback, null);
            return config;
        }
    }
}
