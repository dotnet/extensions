// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

internal static class RoslynTestUtils
{
    /// <summary>
    /// Creates a canonical Roslyn project for testing.
    /// </summary>
    /// <param name="references">Assembly references to include in the project.</param>
    /// <param name="includeBaseReferences">Whether to include references to the BCL assemblies.</param>
    public static Project CreateTestProject(IEnumerable<Assembly>? references, bool includeBaseReferences = true,
        string? testAssemblyName = null)
    {
        const string TestAssemblyName = "test.dll";

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
                         .AddProject("Test", testAssemblyName ?? TestAssemblyName, "C#")
                         .WithMetadataReferences(refs)
                         .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                         .WithNullableContextOptions(NullableContextOptions.Enable));
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
    public static TextSpan MakeTextSpan(this string text, int spanNum)
    {
        var seq = $"/*{spanNum}+*/";
        int start = text.IndexOf(seq, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spanNum));
        }

        start += seq.Length;

        int end = text.IndexOf($"/*-{spanNum}*/", StringComparison.Ordinal);
        if (end < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spanNum));
        }

        return new TextSpan(start, end - start);
    }

    /// <summary>
    /// Counts the number of /*N+*/ and /*-N*/ markers in a string.
    /// </summary>
    public static int CountSpans(this string text)
    {
        int index = 0;
        while (true)
        {
            var seq = $"/*{index}+*/";
            int start = text.IndexOf(seq, StringComparison.Ordinal);
            if (start < 0)
            {
                return index;
            }

            start += seq.Length;

            int end = text.IndexOf($"/*-{index}*/", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidDataException($"Missing end marker for span {index}");
            }

            index++;
        }
    }

    public static void AssertDiagnostic(this string text, int spanNum, DiagnosticDescriptor expected, Diagnostic actual)
    {
        try
        {
            var expectedSpan = text.MakeTextSpan(spanNum);
            Assert.True(expected.Id == actual.Id,
                $"Span {spanNum} doesn't match: expected {expected.Id} but got {actual}");
            Assert.True(expectedSpan.Equals(actual.Location.SourceSpan),
                $"Span {spanNum} doesn't match: expected {expectedSpan} but got {actual.Location.SourceSpan}");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.Fail($"Unexpected warning {actual}");
        }
    }

    public static IList<Diagnostic> FilterDiagnostics(this IEnumerable<Diagnostic> diagnostics, params DiagnosticDescriptor[] filter)
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

    /// <summary>
    /// Runs a Roslyn generator over a set of source files.
    /// </summary>
    public static async Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        ISourceGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        AnalyzerConfigOptionsProvider? optionsProvider = null,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
        var proj = CreateTestProject(references, includeBaseReferences);

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
    public static async Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        IIncrementalGenerator generator,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        bool includeBaseReferences = true,
        CancellationToken cancellationToken = default)
    {
        var proj = CreateTestProject(references, includeBaseReferences);

        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        proj.CommitChanges();
        var comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

        // workaround https://github.com/dotnet/roslyn/pull/55866. We can remove "LangVersion=Preview" when we get a Roslyn build with that change.
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: options);

        var gd = cgd.RunGenerators(comp!, cancellationToken);

        var r = gd.GetRunResult();
        return (Sort(r.Results[0].Diagnostics), r.Results[0].GeneratedSources);
    }

    /// <summary>
    /// Runs a Roslyn analyzer over a set of source files.
    /// </summary>
    public static async Task<IReadOnlyList<Diagnostic>> RunAnalyzer(
        DiagnosticAnalyzer analyzer,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        bool asExecutable = false,
        AnalyzerOptions? options = null,
        string? testAssemblyName = null)
    {
        var proj = CreateTestProject(references, testAssemblyName: testAssemblyName);

        var count = 0;
        foreach (var s in sources)
        {
            proj = proj.WithDocument($"src-{count++}.cs", s);
        }

        if (asExecutable)
        {
            proj = proj.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        }

        proj.CommitChanges();

        var analyzers = ImmutableArray.Create(analyzer);

        var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
        var diags = await comp!.WithAnalyzers(analyzers, options).GetAllDiagnosticsAsync().ConfigureAwait(false);

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
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Hey, that's life")]
    public static async Task<IReadOnlyList<string>> RunAnalyzerAndFixer(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider fixer,
        IEnumerable<Assembly>? references,
        IEnumerable<string> sources,
        IEnumerable<string>? sourceNames = null,
        string? defaultNamespace = null,
        string? extraFile = null,
        bool asExecutable = false,
        string? testAssemblyName = null,
        AnalyzerOptions? analyzerOptions = null)
    {
        var proj = CreateTestProject(references, testAssemblyName: testAssemblyName);

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

        if (asExecutable)
        {
            proj = proj.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        }

        if (defaultNamespace != null)
        {
            proj = proj.WithDefaultNamespace(defaultNamespace);
        }

        proj.CommitChanges();

        var analyzers = ImmutableArray.Create(analyzer);
        int numberOfActionsInPreviousIteration = 0;
        while (true)
        {
            var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
            var diags = await comp!.WithAnalyzers(analyzers, analyzerOptions).GetAllDiagnosticsAsync().ConfigureAwait(false);

            if (diags.IsEmpty)
            {
                // no more diagnostics reported by the analyzers
                break;
            }

            var actions = new List<CodeAction>();
            foreach (var d in diags)
            {
                // apply CodeFix action only if diagnostic is fixable by the fixer
                if (fixer.FixableDiagnosticIds.Contains(d.Id))
                {
                    var doc = proj.GetDocument(d.Location.SourceTree);

                    var context = new CodeFixContext(doc!, d, (action, _) => actions.Add(action), CancellationToken.None);
                    await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                }
            }

            if (actions.Count == 0 || numberOfActionsInPreviousIteration == actions.Count)
            {
                // nothing to fix or expected fix was not applied
                break;
            }

            var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            var changedProj = solution.GetProject(proj.Id);
            if (changedProj != proj)
            {
                proj = await RecreateProjectDocumentsAsync(changedProj!).ConfigureAwait(false);
            }

            numberOfActionsInPreviousIteration = actions.Count;
        }

        var results = new List<string>();

        if (sourceNames != null)
        {
            var l = sourceNames.ToList();
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument(l[i]).GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument($"src-{i}.cs").GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }
        }

        if (extraFile != null)
        {
            var s = await proj.FindDocument(extraFile).GetTextAsync().ConfigureAwait(false);
            results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
        }

        return results;
    }

    /// <summary>
    /// Runs a Roslyn analyzer and FixAll code action.
    /// </summary>
    public static async Task<(IReadOnlyList<string> results, string title)> RunAnalyzerAndFixAllCodeAction(
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

        // set up FixAllProvider and corresponding FixAll code action
        var diagsProvider = new TestDiagnosticProvider(proj, ImmutableArray.Create(analyzer), fixer);
        var context = new FixAllContext(
            project: proj,
            codeFixProvider: fixer,
            scope: FixAllScope.Project,
            codeActionEquivalenceKey: fixer.GetType().FullName!,
            diagnosticIds: fixer.FixableDiagnosticIds,
            fixAllDiagnosticProvider: diagsProvider,
            cancellationToken: CancellationToken.None);

        var fixAllProvider = fixer.GetFixAllProvider();
        var fixAllCodeAction = await fixAllProvider!.GetFixAsync(context);
        var title = fixAllCodeAction!.Title;

        // apply fixAllCodeAction and process changed project
        var operations = await fixAllCodeAction!.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var changedProj = solution.GetProject(proj.Id);
        if (changedProj != proj)
        {
            proj = await RecreateProjectDocumentsAsync(changedProj!).ConfigureAwait(false);
        }

        var results = new List<string>();

        if (sourceNames != null)
        {
            var l = sourceNames.ToList();
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument(l[i]).GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var s = await proj.FindDocument($"src-{i}.cs").GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }
        }

        if (extraFile != null)
        {
            var s = await proj.FindDocument(extraFile).GetTextAsync().ConfigureAwait(false);
            results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
        }

        return (results, title);
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

    internal class TestDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public TestDiagnosticProvider(
            Project project,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            CodeFixProvider fixer)
        {
            _analyzers = analyzers;
            _fixer = fixer;
            _project = project;
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return await GetProjectDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var diagnostics = await GetProjectDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => d.Location.SourceTree!.FilePath.EndsWith(document.Name));
        }

        public override async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            if (_project != project)
            {
                _project = await RecreateProjectDocumentsAsync(project!).ConfigureAwait(false);
            }

            var comp = await _project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var diags = await comp!.WithAnalyzers(_analyzers, cancellationToken: cancellationToken)
                                   .GetAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

            return diags.Where(d => _fixer.FixableDiagnosticIds.Contains(d.Id));
        }

        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly CodeFixProvider _fixer;
        private Project _project;
    }
}
