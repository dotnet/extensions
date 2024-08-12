using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Logging;

namespace Microsoft.Gen.Logging.Test;

public static class CompilationHelper
{
    public static Compilation CreateCompilation(
        string source,
        MetadataReference[]? additionalReferences = null,
        string assemblyName = "TestAssembly")
    {
        string corelib = Assembly.GetAssembly(typeof(object))!.Location;
        string runtimeDir = Path.GetDirectoryName(corelib)!;

        var refs = new List<MetadataReference>();
        refs.Add(MetadataReference.CreateFromFile(corelib));
        refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));
        refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
        refs.Add(MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(LoggerMessageAttribute).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(IEnrichmentTagCollector).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(DataClassification).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(PrivateDataAttribute).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(BigInteger).Assembly.Location));

        if (additionalReferences != null)
        {
            foreach (MetadataReference reference in additionalReferences)
            {
                refs.Add(reference);
            }
        }

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: refs.ToArray(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public static byte[] CreateAssemblyImage(Compilation compilation)
    {
        MemoryStream ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException();
        }

        return ms.ToArray();
    }
}
