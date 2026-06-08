namespace MCPLocalServer.FSharp.Tools
open System
open System.ComponentModel
open System.Runtime.InteropServices
open ModelContextProtocol.Server

/// <summary>
/// Sample MCP tools for demonstration purposes.
/// These tools can be invoked by MCP clients to perform various operations.
/// </summary>
type RandomNumberTools() =

    [<McpServerTool>]
    [<Description("Generates a random number between the specified minimum and maximum values.")>]
    member _.GetRandomNumber
        (
            [<Optional; DefaultParameterValue(0)>]
            [<Description("Minimum value (inclusive)")>] min : int,
            [<Optional; DefaultParameterValue(100)>]
            [<Description("Maximum value (exclusive)")>] max : int
        ) =

        Random.Shared.Next(min, max)
