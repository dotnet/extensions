// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.DotNet.Analyzers.Async
{
    public class AsyncAnalysisData
    {
        private readonly HashSet<string> _methods;

        public AsyncAnalysisData()
        {
            _methods = new HashSet<string>(StringComparer.Ordinal);

            using (var reader = new StreamReader(GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Microsoft.DotNet.Analyzers.Async.AsyncAnalyzer.csv")))
            {
                while (!reader.EndOfStream)
                {
                    _methods.Add(reader.ReadLine());
                }
            }
        }

        public bool Contains(string type, string member)
        {
            return _methods.Contains(type + "." + member);
        }
    }
}
