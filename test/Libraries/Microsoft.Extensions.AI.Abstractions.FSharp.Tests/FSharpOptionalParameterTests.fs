// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.FSharpTests

open System.Text.Json
open Microsoft.Extensions.AI
open Xunit

/// F# type containing methods with F# optional parameter syntax.
type FSharpToolMethods() =
    /// Method with an F# optional parameter using the ?param syntax.
    /// In IL this compiles to a parameter of type FSharpOption&lt;int&gt; with [Optional] and [OptionalArgument] attributes.
    static member AddWithOptional(a: int, ?b: int) : int =
        let bFinal = defaultArg b 42
        a + bFinal

    /// Method with multiple optional parameters.
    static member MultipleOptionals(a: int, ?b: int, ?c: string) : string =
        let bFinal = defaultArg b 0
        let cFinal = defaultArg c "default"
        $"{a + bFinal}: {cFinal}"

module FSharpOptionalParameterTests =

    let private createFunc name =
        let m = typeof<FSharpToolMethods>.GetMethod(name)
        AIFunctionFactory.Create(m, target = null, name = null, description = null)

    [<Fact>]
    let ``F# optional parameter is not marked as required in schema`` () =
        let func = createFunc "AddWithOptional"
        let schemaText = func.JsonSchema.ToString()
        let doc = JsonDocument.Parse(schemaText)
        let root = doc.RootElement

        // Schema should have properties for both 'a' and 'b'
        let props = root.GetProperty("properties")
        let mutable aProp = Unchecked.defaultof<JsonElement>
        let mutable bProp = Unchecked.defaultof<JsonElement>
        Assert.True(props.TryGetProperty("a", &aProp), "Expected 'a' in properties")
        Assert.True(props.TryGetProperty("b", &bProp), "Expected 'b' in properties")

        // Only 'a' should be in the required array
        let mutable requiredProp = Unchecked.defaultof<JsonElement>
        Assert.True(root.TryGetProperty("required", &requiredProp), "Expected a 'required' property in schema")

        let requiredItems = [| for i in 0 .. requiredProp.GetArrayLength() - 1 -> string requiredProp[i] |]
        Assert.Contains("a", requiredItems)
        Assert.DoesNotContain("b", requiredItems)

    [<Fact>]
    let ``F# optional parameter schema includes default null`` () =
        let func = createFunc "AddWithOptional"
        let schemaText = func.JsonSchema.ToString()

        // The optional parameter 'b' should have a "default": null entry
        let doc = JsonDocument.Parse(schemaText)
        let bProp = doc.RootElement.GetProperty("properties").GetProperty("b")
        let mutable defaultProp = Unchecked.defaultof<JsonElement>
        Assert.True(bProp.TryGetProperty("default", &defaultProp), "Expected 'b' parameter to have a 'default' property")
        Assert.Equal(JsonValueKind.Null, defaultProp.ValueKind)

    [<Fact>]
    let ``F# optional parameter can be omitted when invoking`` () = task {
        let func = createFunc "AddWithOptional"

        // Invoke with only 'a', omitting optional 'b'
        let args = AIFunctionArguments(dict [ ("a", box 5) ])
        let! result = func.InvokeAsync(args)

        // b defaults to 42 in the F# method, so result should be 5 + 42 = 47
        let resultElement = result :?> JsonElement
        Assert.Equal(47, resultElement.GetInt32())
    }

    [<Fact>]
    let ``F# optional parameter can be provided when invoking`` () = task {
        let func = createFunc "AddWithOptional"

        // Invoke with both 'a' and 'b'
        let args = AIFunctionArguments(dict [ ("a", box 5); ("b", box 10) ])
        let! result = func.InvokeAsync(args)

        let resultElement = result :?> JsonElement
        Assert.Equal(15, resultElement.GetInt32())
    }

    [<Fact>]
    let ``Multiple F# optional parameters can all be omitted`` () = task {
        let func = createFunc "MultipleOptionals"

        // Invoke with only required 'a'
        let args = AIFunctionArguments(dict [ ("a", box 10) ])
        let! result = func.InvokeAsync(args)

        let resultElement = result :?> JsonElement
        Assert.Equal("10: default", resultElement.GetString())
    }

    [<Fact>]
    let ``Multiple F# optional parameters schema only requires non-optional`` () =
        let func = createFunc "MultipleOptionals"
        let schemaText = func.JsonSchema.ToString()
        let doc = JsonDocument.Parse(schemaText)
        let root = doc.RootElement

        let mutable requiredProp = Unchecked.defaultof<JsonElement>
        Assert.True(root.TryGetProperty("required", &requiredProp), "Expected a 'required' property in schema")

        let requiredItems = [| for i in 0 .. requiredProp.GetArrayLength() - 1 -> string requiredProp[i] |]
        Assert.Contains("a", requiredItems)
        Assert.DoesNotContain("b", requiredItems)
        Assert.DoesNotContain("c", requiredItems)
