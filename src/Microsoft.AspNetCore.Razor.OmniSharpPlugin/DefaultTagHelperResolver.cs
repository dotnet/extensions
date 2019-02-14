// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(TagHelperResolver))]
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        private readonly object _tagHelperResolver;
        private readonly MethodInfo _getTagHelpersAsyncMethod;

        public DefaultTagHelperResolver()
        {
            var razorLSCommon = Assembly.Load("Microsoft.AspNetCore.Razor.LanguageServer.Common");
            var pluginTagHelperResolverType = razorLSCommon.GetType("Microsoft.AspNetCore.Razor.LanguageServer.Common.PluginTagHelperResolver");
            _tagHelperResolver = Activator.CreateInstance(pluginTagHelperResolverType);
            _getTagHelpersAsyncMethod = pluginTagHelperResolverType.GetMethod("GetTagHelpersAsync", new Type[] {
                typeof(Project),
                typeof(RazorProjectEngine),
                typeof(CancellationToken)
            });
        }

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(
            Project project,
            RazorProjectEngine engine,
            CancellationToken cancellationToken)
        {
            var tagHelpersTask = (Task<IReadOnlyList<TagHelperDescriptor>>)_getTagHelpersAsyncMethod.Invoke(_tagHelperResolver, new object[] { project, engine, cancellationToken });
            return tagHelpersTask;
        }
    }
}
