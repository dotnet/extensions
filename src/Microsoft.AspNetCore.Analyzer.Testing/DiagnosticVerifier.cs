// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Xunit;

namespace Microsoft.AspNetCore.Analyzer.Testing
{
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract class DiagnosticVerifier
    {
        /// <summary>
        /// File name prefix used to generate Documents instances from source.
        /// </summary>
        protected static string DefaultFilePathPrefix = "Test";
        /// <summary>
        /// Project name of
        /// </summary>
        protected static string TestProjectName = "TestProject";

        /// <summary>
        /// Given classes in the form of strings, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <param name="additionalEnabledDiagnostics">Additional diagnostics to enable at Info level</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected Task<Diagnostic[]> GetDiagnosticsAsync(string[] sources, DiagnosticAnalyzer analyzer, string[] additionalEnabledDiagnostics)
        {
            return GetDiagnosticsAsync(GetDocuments(sources), analyzer, additionalEnabledDiagnostics);
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="additionalEnabledDiagnostics">Additional diagnostics to enable at Info level</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static async Task<Diagnostic[]> GetDiagnosticsAsync(Document[] documents, DiagnosticAnalyzer analyzer, string[] additionalEnabledDiagnostics)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync();

                // Enable any additional diagnostics
                var options = compilation.Options;
                if (additionalEnabledDiagnostics.Length > 0)
                {
                    options = compilation.Options
                        .WithSpecificDiagnosticOptions(
                            additionalEnabledDiagnostics.ToDictionary(s => s, s => ReportDiagnostic.Info));
                }

                var compilationWithAnalyzers = compilation
                    .WithOptions(options)
                    .WithAnalyzers(ImmutableArray.Create(analyzer));

                var diags = await compilationWithAnalyzers.GetAllDiagnosticsAsync();

                Assert.DoesNotContain(diags, d => d.Id == "AD0001");

                // Filter out non-error diagnostics not produced by our analyzer
                // We want to KEEP errors because we might have written bad code. But sometimes we leave warnings in to make the
                // test code more convenient
                diags = diags.Where(d => d.Severity == DiagnosticSeverity.Error || analyzer.SupportedDiagnostics.Any(s => s.Id.Equals(d.Id))).ToImmutableArray();

                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        foreach (var document in documents)
                        {
                            var tree = await document.GetSyntaxTreeAsync();
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <returns>An array of Documents produced from the sources.</returns>
        private Document[] GetDocuments(string[] sources)
        {
            var project = CreateProject(sources);
            var documents = project.Documents.ToArray();

            Debug.Assert(sources.Length == documents.Length);

            return documents;
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        private Project CreateProject(string[] sources)
        {
            var fileNamePrefix = DefaultFilePathPrefix;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp);

            foreach (var defaultCompileLibrary in DependencyContext.Load(GetType().Assembly).CompileLibraries)
            {
                foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(new AppLocalResolver()))
                {
                    solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(resolveReferencePath));
                }
            }

            var count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId);
        }

        // Required to resolve compilation assemblies inside unit tests
        private class AppLocalResolver : ICompilationAssemblyResolver
        {
            public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
            {
                foreach (var assembly in library.Assemblies)
                {
                    var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));
                    if (File.Exists(dll))
                    {
                        assemblies.Add(dll);
                        return true;
                    }

                    dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));
                    if (File.Exists(dll))
                    {
                        assemblies.Add(dll);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
