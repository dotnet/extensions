// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.Logging.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var messagesPath = Path.Combine(Environment.CurrentDirectory, args[0]);

            var generatedFiles = LogMessageGenerator.Generate().Concat(LogScopeGenerator.Generate());

            foreach (var generatedFile in generatedFiles)
            {
                File.WriteAllText(Path.Combine(messagesPath, generatedFile.fileName), generatedFile.fileContent);
            }
        }
    }
}
