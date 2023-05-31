// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace ApiChief.Processing;

/// <summary>
/// A visitor that removes non-public entities from the AST.
/// </summary>
internal sealed class PublicFilterVisitor : CSharpOutputVisitor
{
    public PublicFilterVisitor()
        : base(StringWriter.Null, FormattingOptionsFactory.CreateEmpty())
    {
    }

    private static bool RemoveNonPublicEntity(EntityDeclaration entityDeclaration)
    {
        if (entityDeclaration.HasModifier(Modifiers.Public)
            || (entityDeclaration.HasModifier(Modifiers.Protected) && !entityDeclaration.HasModifier(Modifiers.Private)))
        {
            return false;
        }

        RemoveEntity(entityDeclaration);
        return true;
    }

    private static void RemoveEntity(EntityDeclaration entityDeclaration)
    {
        var node = entityDeclaration.PrevSibling;

        while (node is Attribute attribute)
        {
            node = node.PrevSibling;
            attribute.Remove();
        }

        while (node is Comment comment)
        {
            node = node.PrevSibling;

            if (comment.CommentType == CommentType.Documentation)
            {
                comment.Remove();
            }
        }

        entityDeclaration.Remove();
    }

    public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
    {
        if (!RemoveNonPublicEntity(typeDeclaration))
        {
            base.VisitTypeDeclaration(typeDeclaration);

            if (typeDeclaration != typeDeclaration.Parent?.LastChild)
            {
                NewLine();
            }
        }
    }

    public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
    {
        var type = (TypeDeclaration)methodDeclaration.Parent!;
        if (type.ClassType == ClassType.Interface || !RemoveNonPublicEntity(methodDeclaration))
        {
            foreach (var attribute in methodDeclaration.Attributes)
            {
                var s = attribute.ToString();
#pragma warning disable S1067
                if (s.Contains("SkipLocalsInit")
                    || s.Contains("ExcludeFromCodeCoverage")
                    || s.Contains("DebuggerStepThrough")
                    || s.Contains("SuppressMessage")
                    || s.Contains("DynamicDependency")
                    || s.Contains("AsyncStateMachine"))
#pragma warning restore S1067
                {
                    attribute.Remove();
                }
            }

            base.VisitMethodDeclaration(methodDeclaration);
        }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
    {
        if (!RemoveNonPublicEntity(constructorDeclaration))
        {
            base.VisitConstructorDeclaration(constructorDeclaration);
        }
    }

    public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
    {
        if (!RemoveNonPublicEntity(fieldDeclaration))
        {
            base.VisitFieldDeclaration(fieldDeclaration);
        }
    }

    public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
    {
        var type = (TypeDeclaration)eventDeclaration.Parent!;
        if (type.ClassType == ClassType.Interface || !RemoveNonPublicEntity(eventDeclaration))
        {
            base.VisitEventDeclaration(eventDeclaration);
        }
    }

    public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
    {
        var type = (TypeDeclaration)propertyDeclaration.Parent!;
        if (type.ClassType == ClassType.Interface || !RemoveNonPublicEntity(propertyDeclaration))
        {
            base.VisitPropertyDeclaration(propertyDeclaration);
        }
    }

    public override void VisitAccessor(Accessor accessor)
    {
        var type = (TypeDeclaration)accessor.Parent!.Parent!;

        if (accessor.Attributes != null)
        {
            foreach (var attributeSection in accessor.Attributes)
            {
                foreach (var attribute in attributeSection.Attributes)
                {
                    var s = attribute.ToString();
                    if (s == "CompilerGenerated")
                    {
                        attribute.Remove();
                        break;
                    }
                }

                if (attributeSection.Attributes.Count == 0)
                {
                    attributeSection.Remove();
                    break;
                }
            }
        }

        if (type.ClassType == ClassType.Interface || accessor.Modifiers == Modifiers.None)
        {
            base.VisitAccessor(accessor);
        }
        else
        {
            RemoveEntity(accessor);
        }
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
    {
        base.VisitNamespaceDeclaration(namespaceDeclaration);

        var count = namespaceDeclaration.Children.Count();

        if (count == 1)
        {
            namespaceDeclaration.Remove();
        }
    }
}
