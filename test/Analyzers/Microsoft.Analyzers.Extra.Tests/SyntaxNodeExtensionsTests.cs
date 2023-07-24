// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class SyntaxNodeExtensionsTests
{
    [Fact]
    public void CheckFindNodeInTreeUpToSpecifiedParentByMethodNameReturnsNullWhenNodeIsNotfound()
    {
        var node = SyntaxFactory.VariableDeclarator("v");
        var emptyList = new List<string>();

        Assert.Null(node.FindNodeInTreeUpToSpecifiedParentByMethodName(new Mock<SemanticModel>().Object, emptyList, new List<Type>()));
    }

    [Fact]
    public void FindNodeInTreeUpToSpecifiedParentByMethodName_WhenNodeFounded()
    {
        string codeStr = @"
	         public static class Extensions {
                public static string AddA(this string a)
                {
                    return a + ""a"";
                }
             }

	         public class MyClass {
			    int Method1() { return 0; }
			    void Method2()
			    {
				    string a = ""ab"";
                    a.AddA();
			    }
		     }";

        SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(codeStr);

        var compilation = CSharpCompilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);
        var methodInvocSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        var parentToFind = "Extensions.AddA(string)";
        Assert.Equal("a.AddA()", methodInvocSyntax?.FindNodeInTreeUpToSpecifiedParentByMethodName(model, new[] { parentToFind }, Array.Empty<Type>())?.ToString());
    }

    [Fact]
    public void FindNodeInTreeUpToSpecifiedParentByMethodName_WhenStopped()
    {
        string codeStr = @"
	         public static class Extensions {
                public static string AddA(this string a)
                {
                    return a + ""a"";
                }
             }

	         public class MyClass {
			    int Method1() { return 0; }
			    void Method2()
			    {
				    string a = ""ab"";
                    a.AddA().AddA();
			    }
		     }";

        SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(codeStr);

        var compilation = CSharpCompilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);
        var methodInvocSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();

        var typesToStopTraversing = new HashSet<Type>
        {
            typeof(ExpressionStatementSyntax),
        };

        Assert.Null(methodInvocSyntax?.FindNodeInTreeUpToSpecifiedParentByMethodName(model, Array.Empty<string>(), typesToStopTraversing));
    }

    [Fact]
    public void CheckGetFirstAncestorOfSyntaxKindReturnsNullWhenNodeIsNotfound()
    {
        var node = SyntaxFactory.VariableDeclarator("v");

        Assert.Null(node.GetFirstAncestorOfSyntaxKind(SyntaxKind.EqualsValueClause));
    }

    [Fact]
    public void GetExpressionNameReturnsNullWhenExpressionIsNotMemberAccessOrMemeberBinding()
    {
        var node = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("CheckGetFirstAncestorOfSyntaxKindReturnsNullWhenNodeIsNotfound"));

        Assert.Null(node.GetExpressionName());
    }

    [Theory]
    [InlineData("expectedName", true)]
    [InlineData("anotherNameExpected", false)]
    public void IdentifierNameEqualsReturnsResultWhenNodeTypeIsIdentifierNameSyntax(string expectedName, bool result)
    {
        var node = SyntaxFactory.IdentifierName("expectedName");

        Assert.Equal(result, node.IdentifierNameEquals(expectedName));
    }

    [Fact]
    public void IdentifierNameEqualsReturnsFalseWhenNodeTypeIsNotIdentifierNameSyntax()
    {
        Assert.False(((LiteralExpressionSyntax)null!).IdentifierNameEquals("a"));
    }

    [Fact]
    public void NodeHasSpecifiedMethodReturnsFalseWhenMethodSymbolIsNull()
    {
        string codeStr = @"
                using System;

                public class MyClassClass
                {
                    virtual public void MyMehtod()
                    {
                        Console.WriteLine(""Hello from MyClass"");
                    }
                }
            ";
        SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(codeStr);

        var compilation = CSharpCompilation.Create(
                            "MyCompilation",
                            syntaxTrees: new[] { tree },
                            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);
        var methodInvocSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        Assert.False(methodInvocSyntax!.NodeHasSpecifiedMethod(model, new HashSet<string>()));
    }

    [Fact]
    public void NodeHasSpecifiedMethodContainsReducedForm()
    {
        string codeStr = @"
             public static class Extensions {
                public static string AddA(this string a)
                {
                    return a + ""a"";
                }
             }

	         public class MyClass {
			    int Method1() { return 0; }
			    void Method2()
			    {
				    string a = ""ab"";
                    a.AddA();
			    }
		     }";

        SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(codeStr);
        var compilation = CSharpCompilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);
        var methodInvocSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        Assert.True(methodInvocSyntax!.NodeHasSpecifiedMethod(model, new HashSet<string> { "Extensions.AddA(string)" }));
    }

    [Fact]
    public void GetExpressionName_WithMemberAccessExpression()
    {
        var console = SyntaxFactory.IdentifierName("Console");
        var writeline = SyntaxFactory.IdentifierName("WriteLine");
        var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, console, writeline);
        var expression = SyntaxFactory.InvocationExpression(memberaccess, SyntaxFactory.ArgumentList());

        Assert.Equal(writeline.ToString(), expression?.GetExpressionName()?.ToString());
    }

    [Fact]
    public void GetExpressionName_WithMemberBindingExpression()
    {
        var b = SyntaxFactory.IdentifierName("b");
        var memberbind = SyntaxFactory.MemberBindingExpression(SyntaxFactory.ParseToken("."), b);
        var expression = SyntaxFactory.InvocationExpression(memberbind);

        Assert.Equal(b.ToString(), expression?.GetExpressionName()?.ToString());
    }
}
