// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddRazorShims(this IServiceCollection services)
        {
            services.AddSingleton<ForegroundDispatcher, VSCodeForegroundDispatcher>();
            services.AddSingleton<ForegroundDispatcherShim, DefaultForegroundDispatcherShim>();
            services.AddSingleton<ErrorReporter, DefaultErrorReporter>();
            services.AddSingleton<ProjectSnapshotManagerShimAccessor, DefaultProjectSnapshotManagerShimAccessor>();
            services.AddSingleton<RazorConfigurationResolver, DefaultRazorConfigurationResolver>();
            services.AddSingleton<RazorSyntaxFactsService, DefaultRazorSyntaxFactsService>();
            services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();

            return services;
        }
    }
}
