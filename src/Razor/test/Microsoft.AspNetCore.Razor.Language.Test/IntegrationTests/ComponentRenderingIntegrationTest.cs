// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentRenderingIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_Simple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent/>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 1, 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithParameters()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : ComponentBase
    {
        [Parameter] int IntProperty { get; set; }
        [Parameter] bool BoolProperty { get; set; }
        [Parameter] string StringProperty { get; set; }
        [Parameter] SomeType ObjectProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent 
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 5, 0),
                frame => AssertFrame.Attribute(frame, "IntProperty", 123, 1),
                frame => AssertFrame.Attribute(frame, "BoolProperty", true, 2),
                frame => AssertFrame.Attribute(frame, "StringProperty", "My string", 3),
                frame =>
                {
                    AssertFrame.Attribute(frame, "ObjectProperty", 4);
                    Assert.Equal("Test.SomeType", frame.AttributeValue.GetType().FullName);
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_TriesToSetNonParamter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        public int IntProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent  IntProperty=""123"" />");

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => GetRenderTree(component));

            // Assert
            Assert.Equal(
                "Object of type 'Test.MyComponent' has a property matching the name 'IntProperty', " +
                    "but it does not have [ParameterAttribute] or [CascadingParameterAttribute] applied.",
                ex.Message);
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string StringProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent StringProperty=""@(42.ToString())"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "StringProperty", "42", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithNonPropertyAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent some-attribute=""foo"" another-attribute=""@(42.ToString())"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "some-attribute", "foo", 1),
                frame => AssertFrame.Attribute(frame, "another-attribute", "42", 2));
        }


        [Theory(Skip = "Not ready yet.")]
        [InlineData("e => Increment(e)")]
        [InlineData("(e) => Increment(e)")]
        [InlineData("@(e => Increment(e))")]
        [InlineData("@(e => { Increment(e); })")]
        [InlineData("Increment")]
        [InlineData("@Increment")]
        [InlineData("@(Increment)")]
        public void Render_ChildComponent_WithEventHandler(string expression)
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        Action<UIMouseEventArgs> OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent($@"
@addTagHelper *, TestAssembly
<MyComponent OnClick=""{expression}""/>

@functions {{
    private int counter;
    private void Increment(UIMouseEventArgs e) {{
        counter++;
    }}
}}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "OnClick", 1);

                    // The handler will have been assigned to a lambda
                    var handler = Assert.IsType<Action<UIMouseEventArgs>>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithExplicitEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        Action<UIEventArgs> OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent OnClick=""@Increment""/>

@functions {
    private int counter;
    private void Increment(UIEventArgs e) {
        counter++;
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "OnClick", 1);

                    // The handler will have been assigned to a lambda
                    var handler = Assert.IsType<Action<UIEventArgs>>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                    Assert.Equal("Increment", handler.Method.Name);
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithMinimizedBoolAttribute()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        bool BoolProperty { get; set; }
    }
}"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent BoolProperty />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "BoolProperty", true, 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text @(""Hello"")</some-child></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "MyAttr", "abc", 1),
                frame => AssertFrame.Attribute(frame, "ChildContent", 2));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[2].AttributeValue);
            Assert.Collection(
                childFrames,
                frame => AssertFrame.Text(frame, "Some text", 3),
                frame => AssertFrame.Element(frame, "some-child", 4, 4),
                frame => AssertFrame.Attribute(frame, "a", "1", 5),
                frame => AssertFrame.Text(frame, "Nested text ", 6),
                frame => AssertFrame.Text(frame, "Hello", 7));
        }

        [Fact(Skip = "Not ready yet.")]
        public void Render_ChildComponent_Nested()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent><MyComponent>Some text</MyComponent></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: outer component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "ChildContent", 1));

            // Assert: first level of ChildContent is correct
            // Note that we don't really need the sequence numbers to continue on from the
            // sequence numbers at the parent level. All that really matters is that they are
            // correct relative to each other (i.e., incrementing) within the nesting level.
            // As an implementation detail, it happens that they do follow on from the parent
            // level, but we could change that part of the implementation if we wanted.
            var innerFrames = GetFrames((RenderFragment)frames[1].AttributeValue).ToArray();
            Assert.Collection(
                innerFrames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3));

            // Assert: second level of ChildContent is correct
            Assert.Collection(
                GetFrames((RenderFragment)innerFrames[1].AttributeValue),
                frame => AssertFrame.Text(frame, "Some text", 4));
        }

        [Fact(Skip = "Not ready yet.")] // https://github.com/aspnet/Blazor/issues/773
        public void Regression_773()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] private string Title { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@page ""/""

<SurveyPrompt Title=""<div>Test!</div>"" />
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.SurveyPrompt", 2, 0),
                frame => AssertFrame.Attribute(frame, "Title", "<div>Test!</div>", 1));
        }


        [Fact(Skip = "Not ready yet.")]
        public void Regression_784()
        {
            // Arrange

            // Act
            var component = CompileToComponent(@"
<p onmouseover=""@OnComponentHover"" style=""background: @ParentBgColor;"" />
@functions {
    public string ParentBgColor { get; set; } = ""#FFFFFF"";

    public void OnComponentHover(UIMouseEventArgs e)
    {
    }
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "p", 3, 0),
                frame => AssertFrame.Attribute(frame, "onmouseover", 1),
                frame => AssertFrame.Attribute(frame, "style", "background: #FFFFFF;", 2));
        }

        // Text nodes decode HTML entities
        [Fact(Skip = "Not ready yet.")]
        public void Render_Component_HtmlEncoded()
        {
            // Arrange
            var component = CompileToComponent(@"&lt;span&gt;Hi&lt/span&gt;");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Text(frame, "<span>Hi</span>"));
        }

        // Integration test for HTML block rewriting
        [Fact(Skip = "Not ready yet.")]
        public void Render_HtmlBlock_Integration()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly

<html>
  <head><meta><meta></head>
  <body>
    <MyComponent>
      <div><span></span><span></span></div>
      <div>@(""hi"")</div>
      <div><span></span><span></span></div>
      <div></div>
      <div>@(""hi"")</div>
      <div></div>
  </MyComponent>
  </body>
</html>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "html", 9, 0),
                frame => AssertFrame.Whitespace(frame, 1),
                frame => AssertFrame.Markup(frame, "<head><meta><meta></head>\n  ", 2),
                frame => AssertFrame.Element(frame, "body", 5, 3),
                frame => AssertFrame.Whitespace(frame, 4),
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 5),
                frame => AssertFrame.Attribute(frame, "ChildContent", 6),
                frame => AssertFrame.Whitespace(frame, 16),
                frame => AssertFrame.Whitespace(frame, 17));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[6].AttributeValue);
            Assert.Collection(
                childFrames,
                frame => AssertFrame.Whitespace(frame, 7),
                frame => AssertFrame.Markup(frame, "<div><span></span><span></span></div>\n      ", 8),
                frame => AssertFrame.Element(frame, "div", 2, 9),
                frame => AssertFrame.Text(frame, "hi", 10),
                frame => AssertFrame.Whitespace(frame, 11),
                frame => AssertFrame.Markup(frame, "<div><span></span><span></span></div>\n      <div></div>\n      ", 12),
                frame => AssertFrame.Element(frame, "div", 2, 13),
                frame => AssertFrame.Text(frame, "hi", 14),
                frame => AssertFrame.Markup(frame, "\n      <div></div>\n  ", 15));
        }

        [Fact(Skip = "Not ready yet.")]
        public void RazorTemplate_CanBeUsedFromComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Test
{
    public class Repeater : ComponentBase
    {
        [Parameter] int Count { get; set; }
        [Parameter] RenderFragment<string> Template { get; set; }
        [Parameter] string Value { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            for (var i = 0; i < Count; i++)
            {
                builder.AddContent(i, Template, Value);
            }
        }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper ""*, TestAssembly""
@{ RenderFragment<string> template = (context) => @<div>@context.ToLower()</div>; }
<Repeater Count=3 Value=""Hello, World!"" Template=""template"" />
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.Repeater", 4, 2),
                frame => AssertFrame.Attribute(frame, "Count", typeof(int), 3),
                frame => AssertFrame.Attribute(frame, "Value", typeof(string), 4),
                frame => AssertFrame.Attribute(frame, "Template", typeof(RenderFragment<string>), 5),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }

                [Fact(Skip = "Not ready yet.")]
        public void SupportsPlainText()
        {
            // Arrange/Act
            var component = CompileToComponent("Some plain text");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Some plain text", 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsCSharpExpressions()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
                @(""Hello"")
                @((object)null)
                @(123)
                @(new object())
            ");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello", 0),
                frame => AssertFrame.Whitespace(frame, 1),
                frame => AssertFrame.Whitespace(frame, 2), // @((object)null)
                frame => AssertFrame.Whitespace(frame, 3),
                frame => AssertFrame.Text(frame, "123", 4),
                frame => AssertFrame.Whitespace(frame, 5),
                frame => AssertFrame.Text(frame, new object().ToString(), 6));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsCSharpFunctionsBlock()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
                @foreach(var item in items) {
                    @item
                }
                @functions {
                    string[] items = new[] { ""First"", ""Second"", ""Third"" };
                }
            ");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "First", 0),
                frame => AssertFrame.Text(frame, "Second", 0),
                frame => AssertFrame.Text(frame, "Third", 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsElementsWithDynamicContent()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello @(\"there\")</myelem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "myelem", 3, 0),
                frame => AssertFrame.Text(frame, "Hello ", 1),
                frame => AssertFrame.Text(frame, "there", 2));
        }

        [Fact(Skip = "Temporarily disable compiling markup frames in 0.5.1")]
        public void SupportsElementsAsStaticBlock()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello</myelem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Markup(frame, "<myelem>Hello</myelem>", 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void CreatesSeparateMarkupFrameForEachTopLevelStaticElement()
        {
            // The JavaScript-side rendering code does not rely on this behavior. It supports
            // inserting markup frames with arbitrary markup (e.g., multiple top-level elements
            // or none). This test exists only as an observation of the current behavior rather
            // than a promise that we never want to change it.

            // Arrange/Act
            var component = CompileToComponent(
                "<root>@(\"Hi\") <child1>a</child1> <child2><another>b</another></child2> </root>");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "root", 5, 0),
                frame => AssertFrame.Text(frame, "Hi", 1),
                frame => AssertFrame.Text(frame, " ", 2),
                frame => AssertFrame.Markup(frame, "<child1>a</child1> ", 3),
                frame => AssertFrame.Markup(frame, "<child2><another>b</another></child2> ", 4));
        }

        [Fact(Skip = "Not ready yet.")]
        public void RendersMarkupStringAsMarkupFrame()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var someMarkup = new MarkupString(\"<div>Hello</div>\"); }"
                + "<p>@someMarkup</p>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "p", 2, 0),
                frame => AssertFrame.Markup(frame, "<div>Hello</div>", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsSelfClosingElementsWithDynamicContent()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <myelem myattr=@(\"val\") />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Element(frame, "myelem", 2, 1),
                frame => AssertFrame.Attribute(frame, "myattr", "val", 2));
        }

        [Fact(Skip = "Temporarily disable compiling markup frames in 0.5.1")]
        public void SupportsSelfClosingElementsAsStaticBlock()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <input attr='123' />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Markup(frame, "<input attr=\"123\">", 1));
        }

        [Fact(Skip = "Temporarily disable compiling markup frames in 0.5.1")]
        public void SupportsVoidHtmlElements()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <img>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Markup(frame, "<img>", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsComments()
        {
            // Arrange/Act
            var component = CompileToComponent("Start<!-- My comment -->End");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Markup(frame, "StartEnd", 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithLiteralValues()
        {
            // Arrange/Act
            var component = CompileToComponent("<elem attrib-one=\"Value 1\" a2='v2'>@(\"Hello\")</elem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 4, 0),
                frame => AssertFrame.Attribute(frame, "attrib-one", "Value 1", 1),
                frame => AssertFrame.Attribute(frame, "a2", "v2", 2),
                frame => AssertFrame.Text(frame, "Hello", 3));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem attr=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "My string", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithNonStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = 123; }"
                + "<elem attr=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "123", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithInterpolatedStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; var myNum=123; }"
                + "<elem attr=\"Hello, @myValue.ToUpperInvariant()    with number @(myNum*2)!\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "Hello, WORLD    with number 246!", 1));
        }

        // This test exercises the case where two IntermediateTokens are part of the same expression.
        // In these case they are split by a comment.
        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithInterpolatedStringExpressionValues_SplitByComment()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; var myNum=123; }"
                + "<elem attr=\"Hello, @myValue.ToUpperInvariant()    with number @(myN@* Blazor is Blawesome! *@um*2)!\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "Hello, WORLD    with number 246!", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithInterpolatedTernaryExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; }"
                + "<elem attr=\"Hello, @(true ? myValue : \"nothing\")!\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "Hello, world!", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsHyphenedAttributesWithCSharpExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem abc-def=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "abc-def", "My string", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsDataDashAttributes()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@{ 
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@myValue"" />");

            // Assert
            Assert.Collection(
                GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 3, 0),
                frame => AssertFrame.Attribute(frame, "data-abc", "Literal value", 1),
                frame => AssertFrame.Attribute(frame, "data-def", "Expression value", 2));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsAttributesWithEventHandlerValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<elem attr=@MyHandleEvent />
                @functions {
                    public bool HandlerWasCalled { get; set; } = false;

                    void MyHandleEvent(Microsoft.AspNetCore.Components.UIEventArgs eventArgs)
                    {
                        HandlerWasCalled = true;
                    }
                }");
            var handlerWasCalledProperty = component.GetType().GetProperty("HandlerWasCalled");

            // Assert
            Assert.False((bool)handlerWasCalledProperty.GetValue(component));
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame =>
                {
                    Assert.Equal(RenderTreeFrameType.Attribute, frame.FrameType);
                    Assert.Equal(1, frame.Sequence);
                    Assert.NotNull(frame.AttributeValue);

                    ((Action<UIEventArgs>)frame.AttributeValue)(null);
                    Assert.True((bool)handlerWasCalledProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsUsingStatements()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"@using System.Collections.Generic
                @(typeof(List<string>).FullName)");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, typeof(List<string>).FullName, 0));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForTextboxes()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyValue"" />
                @functions {
                    public string MyValue { get; set; } = ""Initial value"";
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Modified value"
                    });
                    Assert.Equal("Modified value", myValueProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForTextareas()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<textarea bind=""MyValue"" ></textarea>
                @functions {
                    public string MyValue { get; set; } = ""Initial value"";
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "textarea", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Modified value"
                    });
                    Assert.Equal("Modified value", myValueProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForDateValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyDate"" />
                @functions {
                    public DateTime MyDate { get; set; } = new DateTime(2018, 3, 4, 1, 2, 3);
                }");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4, 1, 2, 3).ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    var newDateValue = new DateTime(2018, 3, 5, 4, 5, 6);
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = newDateValue.ToString()
                    });
                    Assert.Equal(newDateValue, myDateProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForDateValuesWithFormatString()
        {
            // Arrange/Act
            var testDateFormat = "ddd yyyy-MM-dd";
            var component = CompileToComponent(
                $@"<input bind=""@MyDate"" format-value=""{testDateFormat}"" />
                @functions {{
                    public DateTime MyDate {{ get; set; }} = new DateTime(2018, 3, 4);
                }}");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4).ToString(testDateFormat), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = new DateTime(2018, 3, 5).ToString(testDateFormat)
                    });
                    Assert.Equal(new DateTime(2018, 3, 5), myDateProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")] // In this case, onclick is just a normal HTML attribute
        public void SupportsEventHandlerWithString()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""function(){console.log('hello');};"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame => AssertFrame.Attribute(frame, "onclick", "function(){console.log('hello');};", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsEventHandlerWithLambda()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""@(x => Clicked = true)"" />
@functions {
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    var func = Assert.IsType<Action<UIMouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));

                    func(new UIMouseEventArgs());
                    Assert.True((bool)clicked.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsEventHandlerWithMethodGroup()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""@OnClick"" />
@functions {
    public void OnClick(UIMouseEventArgs e) { Clicked = true; }
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    var func = Assert.IsType<Action<UIMouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));

                    func(new UIMouseEventArgs());
                    Assert.True((bool)clicked.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForBoolValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyValue"" />
                @functions {
                    public bool MyValue { get; set; } = true;
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", true, 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = false
                    });
                    Assert.False((bool)myValueProperty.GetValue(component));
                });
        }

        [Fact(Skip = "Not ready yet.")]
        public void SupportsTwoWayBindingForEnumValues()
        {
            // Arrange/Act
            var myEnumType = FullTypeName<MyEnum>();
            var component = CompileToComponent(
                $@"<input bind=""MyValue"" />
                @functions {{
                    public {myEnumType} MyValue {{ get; set; }} = {myEnumType}.{nameof(MyEnum.FirstValue)};
                }}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", MyEnum.FirstValue.ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = MyEnum.SecondValue.ToString()
                    });
                    Assert.Equal(MyEnum.SecondValue, (MyEnum)myValueProperty.GetValue(component));
                });
        }

        public enum MyEnum { FirstValue, SecondValue }

        [Fact(Skip = "Not ready yet.")]
        public void RazorTemplate_NonGeneric_CanBeUsedFromRazorCode()
        {
            // Arrange
            var component = CompileToComponent(@"
@{ RenderFragment template = @<div>@(""Hello, World!"".ToLower())</div>; }
@for (var i = 0; i < 3; i++)
{
    @template;
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void RazorTemplate_Generic_CanBeUsedFromRazorCode()
        {
            // Arrange
            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLower()</div>; }
@for (var i = 0; i < 3; i++)
{
    @template(""Hello, World!"");
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }

        [Fact(Skip = "Not ready yet.")]
        public void RazorTemplate_NonGeneric_CanBeUsedFromMethod()
        {
            // Arrange
            var component = CompileToComponent(@"
@(Repeat(@<div>@(""Hello, World!"".ToLower())</div>, 3))

@functions {
    RenderFragment Repeat(RenderFragment template, int count)
    {
        return (b) =>
        {
            for (var i = 0; i < count; i++)
            {
                b.AddContent(i, template);
            }
        };
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            //
            // The sequence numbers start at 1 here because there is an AddContent(0, Repeat(....) call
            // that precedes the definition of the lambda. Sequence numbers for the lambda are allocated
            // from the same logical sequence as the surrounding code.
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2));
        }

        [Fact(Skip = "Not ready yet.")]
        public void RazorTemplate_Generic_CanBeUsedFromMethod()
        {
            // Arrange
            var component = CompileToComponent(@"
@(Repeat((context) => @<div>@context.ToLower()</div>, ""Hello, World!"", 3))

@functions {
    RenderFragment Repeat<T>(RenderFragment<T> template, T value, int count)
    {
        return (b) =>
        {
            for (var i = 0; i < count; i++)
            {
                b.AddContent(i, template, value);
            }
        };
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            //
            // The sequence numbers start at 1 here because there is an AddContent(0, Repeat(....) call
            // that precedes the definition of the lambda. Sequence numbers for the lambda are allocated
            // from the same logical sequence as the surrounding code.
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2));
        }
    }
}
