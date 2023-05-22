// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Shared.Collections;
using Xunit;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

internal static class RoslynTestUtils
{
#if ROSLYN_4_0_OR_GREATER
    internal const string RoslynVersion = "4.0";
#else
    internal const string RoslynVersion = "3.8";
#endif

#if DEBUG
    internal const string BuildType = "Debug";
#else
    internal const string BuildType = "Release";
#endif

    /// <summary>
    /// Creates a canonical Roslyn project for testing.
    /// </summary>
    /// <param name="references">Assembly references to include in the project.</param>
    /// <param name="includeBaseReferences">Whether to include references to the BCL assemblies.</param>
    public static Project CreateTestProject(IEnumerable<Assembly>? references, bool includeBaseReferences = true)
    {
        return CreateTestProject(references, Empty.Enumerable<string>(), includeBaseReferences);
    }

    /// <summary>
    /// Creates a canonical Roslyn project for testing.
    /// </summary>
    /// <param name="references">Assembly references to include in the project.</param>
    /// <param name="preprocessorSymbols">Preprocessor symbols to run compilation with.</param>
    /// <param name="includeBaseReferences">Whether to include references to the BCL assemblies.</param>
    public static Project CreateTestProject(IEnumerable<Assembly>? references, IEnumerable<string> preprocessorSymbols, bool includeBaseReferences = true)
    {
        var corelib = Assembly.GetAssembly(typeof(object))!.Location;
        var runtimeDir = Path.GetDirectoryName(corelib)!;

        var refs = new List<MetadataReference>();
        if (includeBaseReferences)
        {
            refs.Add(MetadataReference.CreateFromFile(corelib));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
        }

        if (references != null)
        {
            foreach (var r in references)
            {
                refs.Add(MetadataReference.CreateFromFile(r.Location));
            }
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        return new AdhocWorkspace()
                    .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                    .AddProject("Test", "test.dll", "C#")
                        .WithMetadataReferences(refs)
                        .WithParseOptions(new CSharpParseOptions().WithPreprocessorSymbols(preprocessorSymbols))
                        .WithCompilationOptions(
                            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    public static void CommitChanges(this Project proj)
    {
        Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));
    }

    public static Project WithDocument(this Project proj, string name, string text)
    {
        return proj.AddDocument(name, text).Project;
    }

    public static Document FindDocument(this Project proj, string name)
    {
        foreach (var doc in proj.Documents)
        {
            if (doc.Name == name)
            {
                return doc;
            }
        }

        throw new FileNotFoundException(name);
    }

    /// <summary>
    /// Looks for /*N+*/ and /*-N*/ markers in a string and creates a TextSpan containing the enclosed text.
    /// </summary>
    public static TextSpan? MakeTextSpan(this string text, int spanNum)
    {
        var seq = $"/*{spanNum}+*/";
        int start = text.IndexOf(seq, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += seq.Length;

        int end = text.IndexOf($"/*-{spanNum}*/", StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        return new TextSpan(start, end - start);
    }

    public static void AssertDiagnostic(this string text, int spanNum, DiagnosticDescriptor expected, Diagnostic actual)
    {
        var expectedSpan = text.MakeTextSpan(spanNum);
        if (expectedSpan != null)
        {
            Assert.True(expected.Id == actual.Id,
                $"Span {spanNum} doesn't match: expected {expected.Id} but got {actual}");

            Assert.True(expectedSpan.Equals(actual.Location.SourceSpan),
                $"Span {spanNum} doesn't match: expected {expectedSpan} but got {actual.Location.SourceSpan}");
        }
        else
        {
            Assert.True(false, $"Unexpected diagnostics {actual}");
        }
    }

    public static void AssertDiagnostics(this string text, DiagnosticDescriptor expected, IEnumerable<Diagnostic> actual)
    {
        int spanNum = 0;
        foreach (var d in actual)
        {
            TextSpan? expectedSpan = Location.None.SourceSpan;
            if (d.Location != Location.None)
            {
                expectedSpan = text.MakeTextSpan(spanNum);
                if (expectedSpan == null)
                {
                    Assert.True(false, $"No span detected for diagnostic #{spanNum}, {d}");
                }
            }

            Assert.True(expected.Id == d.Id,
                $"Span {spanNum} doesn't match: expected {expected.Id} but got {d}");

            Assert.True(expectedSpan.Equals(d.Location.SourceSpan),
                $"Span {spanNum} doesn't match: expected {expectedSpan} but got {d.Location.SourceSpan}");

            if (expectedSpan != Location.None.SourceSpan)
            {
                spanNum++;
            }
        }

        if (text.MakeTextSpan(spanNum) != null)
        {
            Assert.True(false, $"Diagnostic {spanNum} was not detected");
        }
    }

    public static IReadOnlyList<Diagnostic> FilterDiagnostics(this IEnumerable<Diagnostic> diagnostics, params DiagnosticDescriptor[] filter)
    {
        var filtered = new List<Diagnostic>();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            foreach (var f in filter)
            {
                if (diagnostic.Id.Equals(f.Id, StringComparison.Ordinal))
                {
                    filtered.Add(diagnostic);
                    break;
                }
            }
        }

        return filtered;
    }

    public static IReadOnlyList<Diagnostic> FilterOutDiagnostics(this IEnumerable<Diagnostic> diagnostics, params DiagnosticDescriptor[] filter)
    {
        var filtered = new List<Diagnostic>();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            bool keep = true;
            foreach (var f in filter)
            {
                if (diagnostic.Id.Equals(f.Id, StringComparison.Ordinal))
                {
                    keep = false;
                    break;
                }
            }

            if (keep)
            {
                filtered.Add(diagnostic);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        ISourceGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        AnalyzerConfigOptionsProvider? optionsProvider = null,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
        return RunGenerator(generator, references, sources, Empty.Enumerable<string>(), optionsProvider, includeBaseReferences, cancellationToken);
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static async Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        ISourceGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        IEnumerable<string> preprocessorSymbols,
        AnalyzerConfigOptionsProvider? optionsProvider = null,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
        var proj = CreateTestProject(references, preprocessorSymbols, includeBaseReferences);

        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        proj.CommitChanges();
        var comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

        var cgd = CSharpGeneratorDriver.Create(new[] { generator }, optionsProvider: optionsProvider);
        var gd = cgd.RunGenerators(comp!, cancellationToken);

        var r = gd.GetRunResult();
        return (Sort(r.Results[0].Diagnostics), r.Results[0].GeneratedSources);
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        IIncrementalGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        AnalyzerConfigOptionsProvider? optionsProvider = null,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
        return RunGenerator(generator, references, sources, Empty.Enumerable<string>(), optionsProvider, includeBaseReferences, cancellationToken);
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static async Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        IIncrementalGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        IEnumerable<string> preprocessorSymbols,
        AnalyzerConfigOptionsProvider? optionsProvider = null,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
#if ROSLYN_4_0_OR_GREATER
        preprocessorSymbols = preprocessorSymbols.Append("ROSLYN_4_0_OR_GREATER");
#endif

        var proj = CreateTestProject(references, preprocessorSymbols, includeBaseReferences);

        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        proj.CommitChanges();
        var comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

        CSharpParseOptions options = CSharpParseOptions.Default;
        CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: options);

        var gd = cgd.RunGeneratorsAndUpdateCompilation(comp!, out var newComp, out var newDiags, cancellationToken);
        var r = gd.GetRunResult();

        if (r.Results[0].Diagnostics.Length == 0)
        {
            if (newDiags.Length > 0)
            {
                throw new InvalidDataException("Was unable to fully compile assembly with generated code");
            }
        }

        return (Sort(r.Results[0].Diagnostics), r.Results[0].GeneratedSources);
    }

    [Generator]
    private sealed class Generator : ISourceGenerator
    {
        private readonly ISyntaxReceiver _receiver;

        public Generator(ISyntaxReceiver receiver)
        {
            _receiver = receiver;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Method intentionally left empty.
        }

        public void Initialize(GeneratorInitializationContext context) =>
            context.RegisterForSyntaxNotifications(() => _receiver);
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static async Task<Compilation> RunSyntaxContextReceiver(
        ISyntaxReceiver receiver,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        bool includeBaseReferences = true)
    {
        var proj = CreateTestProject(references, includeBaseReferences);
        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        proj.CommitChanges();
        var comp = await proj.GetCompilationAsync().ConfigureAwait(false);
        _ = CSharpGeneratorDriver.Create(new Generator(receiver)).RunGenerators(comp!);
        return comp!;
    }

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static async Task<TParserOutput?> RunParser<TReceiver, TParserOutput>(
        TReceiver receiver,
        Func<TReceiver, Compilation, TParserOutput> parser,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        bool includeBaseReferences = true)
        where TReceiver : ISyntaxReceiver
    {
        var comp = await RunSyntaxContextReceiver(receiver, references, sources, includeBaseReferences);
        return parser(receiver, comp);
    }

    /// <summary>
    /// Runs a Roslyn analyzer over a set of source files.
    /// </summary>
    public static async Task<IReadOnlyList<Diagnostic>> RunAnalyzer(
        DiagnosticAnalyzer analyzer,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources)
    {
        var proj = CreateTestProject(references);

        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        proj.CommitChanges();

        var analyzers = ImmutableArray.Create(analyzer);

        var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
        var diags = await comp!.WithAnalyzers(analyzers).GetAllDiagnosticsAsync().ConfigureAwait(false);
        return Sort(diags);
    }

    private static IReadOnlyList<Diagnostic> Sort(ImmutableArray<Diagnostic> diags)
    {
        return diags.Sort((x, y) =>
        {
            if (x.Location.SourceSpan.Start < y.Location.SourceSpan.Start)
            {
                return -1;
            }
            else if (x.Location.SourceSpan.Start > y.Location.SourceSpan.Start)
            {
                return 1;
            }

            return 0;
        });
    }

    /// <summary>
    /// Runs a Roslyn analyzer and fixer.
    /// </summary>
    public static async Task<IReadOnlyList<string>> RunAnalyzerAndFixer(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider fixer,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        IEnumerable<string>? sourceNames = null,
        string? defaultNamespace = null,
        string? extraFile = null)
    {
        var proj = CreateTestProject(references);

        var count = 0;
        if (sourceNames != null)
        {
            var l = sourceNames.ToList();
            foreach (var s in sources)
            {
                proj = proj.WithDocument(l[count++], s);
            }
        }
        else
        {
            foreach (var s in sources)
            {
                proj = proj.WithDocument($"src-{count++}.cs", s);
            }
        }

        if (defaultNamespace != null)
        {
            proj = proj.WithDefaultNamespace(defaultNamespace);
        }

        proj.CommitChanges();

        var analyzers = ImmutableArray.Create(analyzer);

        while (true)
        {
            var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
            var diags = await comp!.WithAnalyzers(analyzers).GetAllDiagnosticsAsync().ConfigureAwait(false);
            if (diags.IsEmpty)
            {
                // no more diagnostics reported by the analyzers
                break;
            }

            var actions = new List<CodeAction>();
            foreach (var d in diags)
            {
                var doc = proj.GetDocument(d.Location.SourceTree);

                var context = new CodeFixContext(doc!, d, (action, _) => actions.Add(action), CancellationToken.None);
                await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
            }

            if (actions.Count == 0)
            {
                // nothing to fix
                break;
            }

            var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            var changedProj = solution.GetProject(proj.Id);
            if (changedProj != proj)
            {
                proj = await RecreateProjectDocumentsAsync(changedProj!).ConfigureAwait(false);
            }
        }

        var results = new List<string>();

        if (sourceNames != null)
        {
            var l = sourceNames.ToList();
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument(l[i]).GetTextAsync().ConfigureAwait(false);
                results.Add(Regex.Replace(s.ToString(), "\r\n", "\n", RegexOptions.IgnoreCase));
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument($"src-{i}.cs").GetTextAsync().ConfigureAwait(false);
                results.Add(Regex.Replace(s.ToString(), "\r\n", "\n", RegexOptions.IgnoreCase));
            }
        }

        if (extraFile != null)
        {
            var s = await proj.FindDocument(extraFile).GetTextAsync().ConfigureAwait(false);
            results.Add(Regex.Replace(s.ToString(), "\r\n", "\n", RegexOptions.IgnoreCase));
        }

        return results;
    }

    private static async Task<Project> RecreateProjectDocumentsAsync(Project project)
    {
        foreach (var documentId in project.DocumentIds)
        {
            var document = project.GetDocument(documentId);
            document = await RecreateDocumentAsync(document!).ConfigureAwait(false);
            project = document.Project;
        }

        return project;
    }

    private static async Task<Document> RecreateDocumentAsync(Document document)
    {
        var newText = await document.GetTextAsync().ConfigureAwait(false);
        return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
    }
}
