// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    [Export(typeof(CSharpProximityExpressionResolver))]
    internal class DefaultCSharpProximityExpressionResolver : CSharpProximityExpressionResolver
    {
        private readonly MethodInfo _getProximityExpressionAsync;

        public DefaultCSharpProximityExpressionResolver()
        {
            try
            {
                var assemblyName = typeof(SyntaxTree).Assembly.GetName();
                assemblyName.Name = "Microsoft.CodeAnalysis.CSharp.Features";
                var assembly = Assembly.Load(assemblyName);
                var type = assembly.GetType("Microsoft.CodeAnalysis.CSharp.Debugging.CSharpProximityExpressionsService");
                _getProximityExpressionAsync = type.GetMethod(
                    "Do",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(SyntaxTree), typeof(int), typeof(CancellationToken) },
                    modifiers: Array.Empty<ParameterModifier>());

                if (_getProximityExpressionAsync == null)
                {
                    throw new InvalidOperationException(
                        "Error occured when acessing the Do method on Roslyn's CSharpProximityExpressionsService's type. Roslyn may have changed in an unexpected way.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occured when creating an instance of Roslyn's CSharpProximityExpressionsService's type. Roslyn may have changed in an unexpected way.",
                    ex);
            }
        }

        public override IReadOnlyList<string> GetProximityExpressions(SyntaxTree syntaxTree, int absoluteIndex, CancellationToken cancellationToken)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var parameters = new object[] { syntaxTree, absoluteIndex, cancellationToken };
            var result = _getProximityExpressionAsync.Invoke(obj: null, parameters);
            var expressions = (IReadOnlyList<string>)result;

            return expressions;
        }
    }
}
