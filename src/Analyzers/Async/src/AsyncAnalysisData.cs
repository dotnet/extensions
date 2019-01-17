// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Analyzers
{
    public class AsyncAnalysisData
    {
        private readonly Dictionary<string, bool> _methods;

        public AsyncAnalysisData()
        {
            _methods = new Dictionary<string, bool>();

            using (var reader = new StreamReader(GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Microsoft.Dotnet.Analyzers.Async.AsyncAnalyzer.csv")))
            {
                while (!reader.EndOfStream)
                {
                    _methods[reader.ReadLine()] = true;
                }
            }
        }

        public bool Contains(string type, string member)
        {
            return _methods.ContainsKey(type + "." + member);
        }
    }
}
