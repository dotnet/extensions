// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Analyzer.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Internal.AspNetCore.Analyzers.Tests
{
    public class PubternabilityAnalyzerTests : DiagnosticVerifier
    {

        private const string InternalDefinitions = @"
namespace A.Internal.Namespace
{
   public class C {}
   public delegate C CD ();
   public class CAAttribute: System.Attribute {}

   public class Program
   {
       public static void Main() {}
   }
}";
        public PubternabilityAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task PublicExposureOfPubternalTypeProducesPUB0001(string member)
        {
            var code = $@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}";
            var diagnostic = Assert.Single(await GetDiagnosticFromNamespaceDeclaration(code));
            Assert.Equal("PUB0001", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(PublicTypeDefinitions))]
        public async Task PublicExposureOfPubternalTypeProducesInTypeDefinitionPUB0001(string member)
        {
            var code = $@"
namespace A
{{
    {member}
}}";
            var diagnostic = Assert.Single(await GetDiagnosticFromNamespaceDeclaration(code));
            Assert.Equal("PUB0001", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task PrivateUsageOfPubternalTypeDoesNotProduce(string member)
        {
            var code = $@"
namespace A
{{
    internal class T
    {{
        {member}
    }}
}}";
            var diagnostics = await GetDiagnosticFromNamespaceDeclaration(code);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [MemberData(nameof(PrivateMemberDefinitions))]
        public async Task PrivateUsageOfPubternalTypeDoesNotProduceInPublicClasses(string member)
        {
            var code = $@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}";
            var diagnostics = await GetDiagnosticFromNamespaceDeclaration(code);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [MemberData(nameof(PrivateMemberDefinitions))]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task DefinitionOfPubternalCrossAssemblyProducesPUB0002(string member)
        {
            var code = $@"
using A.Internal.Namespace;
namespace A
{{
    internal class T
    {{
        {member}
    }}
}}";

            var diagnostic = Assert.Single(await GetDiagnosticWithProjectReference(code));
            Assert.Equal("PUB0002", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(TypeUsages))]
        public async Task UsageOfPubternalCrossAssemblyProducesPUB0002(string usage)
        {
            var code = $@"
using A.Internal.Namespace;
namespace A
{{
    public class T
    {{
        private void M()
        {{
            {usage}
        }}
    }}
}}";

            var diagnostic = Assert.Single(await GetDiagnosticWithProjectReference(code));
            Assert.Equal("PUB0002", diagnostic.Id);
        }

        public static IEnumerable<object[]> PublicMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "public", "protected");

        public static IEnumerable<object[]> PublicTypeDefinitions =>
            ApplyModifiers(TypeDefinitions, "public");

        public static IEnumerable<object[]> PrivateMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "private", "internal");

        public static IEnumerable<object[]> TypeUsages =>
            ApplyModifiers(TypeUsageStrings, string.Empty);

        public static string[] MemberDefinitions => new []
        {
            "C c;",
            "T(C c) {}",
            "T([CA]int c) {}",
            "CD c { get; }",
            "event CD c;",
            "delegate C WOW();"
        };

        public static string[] TypeDefinitions => new []
        {
            "delegate C WOW();",
            "class T: I<C> { } interface I<T> {}",
            "class T: C {}"
        };

        public static string[] TypeUsageStrings => new []
        {
            "var c = new C();",
            "CD d = () => null;",
            "var t = typeof(CAAttribute);"
        };

        private static IEnumerable<object[]> ApplyModifiers(string[] code, params string[] mods)
        {
            foreach (var mod in mods)
            {
                foreach (var s in code)
                {
                    yield return new object[] { mod + " " + s };
                }
            }
        }

        private Task<Diagnostic[]> GetDiagnosticFromNamespaceDeclaration(string namespaceDefinition)
        {
            var code = "using A.Internal.Namespace;" + InternalDefinitions + namespaceDefinition;
            return GetDiagnostics(code);
        }

        private Task<Diagnostic[]> GetDiagnosticWithProjectReference(string code)
        {
            var libraray = CreateProject(InternalDefinitions);

            var mainProject = CreateProject(code).AddProjectReference(new ProjectReference(libraray.Id));

            return GetDiagnosticsAsync(mainProject.Documents.ToArray(), new PubternalityAnalyzer(), new [] { "PUB0002" });
        }

        private Task<Diagnostic[]> GetDiagnostics(string code)
        {
            return GetDiagnosticsAsync(new[] { code }, new PubternalityAnalyzer(), new [] { "PUB0002" });
        }
    }
}
