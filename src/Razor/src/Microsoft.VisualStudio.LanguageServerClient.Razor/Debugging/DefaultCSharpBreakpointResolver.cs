// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    // This type is temporary and will be replaced by an ExternalAccess.Razor type once available.

    [Export(typeof(CSharpBreakpointResolver))]
    internal class DefaultCSharpBreakpointResolver : CSharpBreakpointResolver
    {
        private readonly MethodInfo _tryGetBreakpointSpan;

        public DefaultCSharpBreakpointResolver()
        {
            try
            {
                var assemblyName = typeof(SyntaxTree).Assembly.GetName();
                assemblyName.Name = "Microsoft.CodeAnalysis.CSharp.Features";
                var assembly = Assembly.Load(assemblyName);
                var type = assembly.GetType("Microsoft.CodeAnalysis.CSharp.EditAndContinue.BreakpointSpans");
                _tryGetBreakpointSpan = type.GetMethod("TryGetBreakpointSpan");

                if (_tryGetBreakpointSpan == null)
                {
                    throw new InvalidOperationException(
                        "Error occured when acessing the TryGetBreakpointSpan method on Roslyn's BreakpointSpan's type. Roslyn may have changed in an unexpected way.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occured when creating an instance of Roslyn's BreakpointSpan's type. Roslyn may have changed in an unexpected way.",
                    ex);
            }
        }

        public override bool TryGetBreakpointSpan(SyntaxTree tree, int position, CancellationToken cancellationToken, out TextSpan breakpointSpan)
        {
            var parameters = new object[] { tree, position, cancellationToken, new TextSpan() };
            var result = _tryGetBreakpointSpan.Invoke(obj: null, parameters);
            var boolResult = (bool)result;

            if (boolResult)
            {
                breakpointSpan = (TextSpan)parameters[3];
                return true;
            }

            breakpointSpan = default;
            return false;
        }
    }
}
