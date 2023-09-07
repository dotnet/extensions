// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Http.AutoClient;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class ParserTests
{
    private static readonly string[] _unsupportedCharactersStrings = new[] { "\\\"", "\\n", "\\r", "\\t" };
    private static readonly string[] _unsupportedCharactersHeaderValues = new[] { "\\n", "\\r" };

    [Fact]
    public void NoSymbols()
    {
        var comp = CSharpCompilation.Create(null);
        var p = new Parser(comp, (_) => { }, default);
        Assert.Empty(p.GetRestApiClasses(new List<InterfaceDeclarationSyntax>()));
    }

    [Fact]
    public async Task ApiIsClass()
    {
        var d = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public class C
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken) { return """"; }
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task NestedNamespace()
    {
        var d = await RunGenerator(@"
            namespace ParentNamespace
            {
                [AutoClient(""MyClient"")]
                public interface IClient
                {
                    [Get(""/api/users"")]
                    public Task<string> GetUsers(CancellationToken cancellationToken);
                }
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task NoAttributes()
    {
        var d = await RunGenerator(@"
            public interface IClient
            {
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task InvalidInterfaceName()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface Client
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        var d = Assert.Single(ds);
        Assert.Equal(DiagDescriptors.ErrorInterfaceName.Id, d.Id);
    }

    [Fact]
    public async Task InvalidMethodReturnType()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public string GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorInvalidReturnType.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task InvalidMethodReturnTypeNullable()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string?> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorInvalidReturnType.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task InvalidMethodReturnTypeMultipleTypeArguments()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string?, int> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorInvalidReturnType.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task GenericInterface()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient<T>
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        var d = Assert.Single(ds);
        Assert.Equal(DiagDescriptors.ErrorInterfaceIsGeneric.Id, d.Id);
    }

    [Fact]
    public async Task MissingRestApiAttribute()
    {
        var d = await RunGenerator(@"
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task NestedClass()
    {
        var ds = await RunGenerator(@"
            public class B
            {
                [AutoClient(""MyClient"")]
                public interface IClient
                {
                    [Get(""/api/users"")]
                    public Task<string> GetUsers(CancellationToken cancellationToken);
                }
            }");

        var d = Assert.Single(ds);
        Assert.Equal(DiagDescriptors.ErrorClientMustNotBeNested.Id, d.Id);
    }

    [Fact]
    public async Task MissingRestApiMethods()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.WarningRestClientWithoutRestMethods.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task MultipleApiMethodAttributes()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                [Post(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorApiMethodMoreThanOneAttribute.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task WithRequestNameAttributes()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Post(""/api/users"", RequestName = ""MyRequest"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Empty(ds);
    }

    [Fact]
    public async Task WithQueryInPath()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Post(""/api/users?query=true"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorPathWithQuery.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task GenericMethodUnsupported()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers<T>(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorMethodIsGeneric.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task StaticMethodUnsupported()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public static string GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorStaticMethod.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task MissingNamespace()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }", inNamespace: false);

        Assert.Empty(ds);
    }

    [Fact]
    public async Task InvalidMethodAttribute()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [AutoClient(""/api/users"")]
                public Task<string> GetUsers(CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorMissingMethodAttribute.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task UnsupportedBodyMethod()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers([Body] string param, CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorUnsupportedMethodBody.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task DuplicateBody()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers([Body] string param, [Body] string param2, CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorDuplicateBody.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task ParameterMissingUrl()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUser(string userId, CancellationToken cancellationToken);
            }");

        Assert.Contains(DiagDescriptors.ErrorMissingParameterUrl.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task SingleCancellationToken()
    {
        var d = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task MissingCancellationToken()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers();
            }");

        Assert.Contains(DiagDescriptors.ErrorMissingCancellationToken.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task ErrorSymbolNotImported()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(SomeSymbol symbol, CancellationToken token);
            }");

        Assert.Contains(DiagDescriptors.WarningRestClientWithoutRestMethods.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task DoubleCancellationToken()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token, CancellationToken token2);
            }");

        Assert.Contains(DiagDescriptors.ErrorDuplicateCancellationToken.Id, ds.Select(x => x.Id));
    }

    [Fact]
    public async Task RequestNameDuplicate()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);

                [Get(""/api/users"")]
                public Task<string> GetUsersAsync(CancellationToken token);
            }");

        Assert.Contains(DiagDescriptors.ErrorDuplicateRequestName.Id, ds.Select(x => x.Id));
        Assert.Contains("GetUsers", ds.First(x => x.Id == DiagDescriptors.ErrorDuplicateRequestName.Id).GetMessage());
    }

    [Fact]
    public async Task RequestNameDuplicateAttribute()
    {
        var ds = await RunGenerator(@"
            [AutoClient(""MyClient"")]
            public interface IClient
            {
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);

                [Get(""/api/users"", RequestName = ""GetUsers"")]
                public Task<string> GetSomeUsers(CancellationToken token);
            }");

        Assert.Contains(DiagDescriptors.ErrorDuplicateRequestName.Id, ds.Select(x => x.Id));
        Assert.Contains("GetUsers", ds.First(x => x.Id == DiagDescriptors.ErrorDuplicateRequestName.Id).GetMessage());
    }

    [Fact]
    public async Task InvalidHttpClientName()
    {
        foreach (var invalidChar in _unsupportedCharactersStrings)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient{invalidChar}"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidHttpClientName.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidDependencyName()
    {
        foreach (var invalidChar in _unsupportedCharactersStrings)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"", ""DependencyName{invalidChar}"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidDependencyName.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidHeaderNameType()
    {
        foreach (var invalidChar in _unsupportedCharactersStrings)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            [StaticHeader(""HeaderName{invalidChar}"", ""HeaderValue"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidHeaderName.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidHeaderNameMethod()
    {
        foreach (var invalidChar in _unsupportedCharactersStrings)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                [StaticHeader(""HeaderName{invalidChar}"", ""HeaderValue"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidHeaderName.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidHeaderValueType()
    {
        foreach (var invalidChar in _unsupportedCharactersHeaderValues)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            [StaticHeader(""HeaderName"", ""HeaderValue{invalidChar}"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidHeaderValue.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidHeaderValueMethod()
    {
        foreach (var invalidChar in _unsupportedCharactersHeaderValues)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            public interface IClient
            {{
                [Get(""/api/users"")]
                [StaticHeader(""HeaderName"", ""HeaderValue{invalidChar}"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidHeaderValue.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidPath()
    {
        foreach (var invalidChar in _unsupportedCharactersHeaderValues)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            public interface IClient
            {{
                [Get(""/api/users{invalidChar}"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidPath.Id, ds.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task InvalidRequestName()
    {
        foreach (var invalidChar in _unsupportedCharactersHeaderValues)
        {
            var ds = await RunGenerator(@$"
            [AutoClient(""MyClient"")]
            public interface IClient
            {{
                [Get(""/api/users"", RequestName = ""RequestName{invalidChar}"")]
                public Task<string> GetUsers(CancellationToken token);
            }}");

            Assert.Contains(DiagDescriptors.ErrorInvalidRequestName.Id, ds.Select(x => x.Id));
        }
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
        string code,
        bool wrap = true,
        bool inNamespace = true,
        bool includeBaseReferences = true,
        bool includeRestApi = true,
        CancellationToken cancellationToken = default)
    {
        var text = code;
        if (wrap)
        {
            var nspaceStart = "namespace Test {";
            var nspaceEnd = "}";
            if (!inNamespace)
            {
                nspaceStart = "";
                nspaceEnd = "";
            }

            text = $@"
                    {nspaceStart}
                    using Microsoft.Extensions.Http.AutoClient;
                    using System.Threading;
                    using System.Threading.Tasks;
                    {code}
                    {nspaceEnd}
                ";
        }

        Assembly[]? refs = null;
        if (includeRestApi)
        {
            refs = new[]
            {
                Assembly.GetAssembly(typeof(AutoClientAttribute))!
            };
        }

        var (d, _) = await RoslynTestUtils.RunGenerator(
            new AutoClientGenerator(),
            refs,
            new[] { text },
            includeBaseReferences: includeBaseReferences,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return d;
    }
}
