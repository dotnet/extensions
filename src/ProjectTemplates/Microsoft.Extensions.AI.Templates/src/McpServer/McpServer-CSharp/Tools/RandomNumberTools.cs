using System.ComponentModel;
using ModelContextProtocol.Server;

/// <summary>
/// Sample MCP tools for demonstration purposes.
/// These tools can be invoked by MCP clients to perform various operations.
/// </summary>
internal class RandomNumberTools
{
    private readonly int _maxNumber;

    public RandomNumberTools()
    {
        // Process configuration settings from the environment variables.
        // These will be provided by the MCP client application, such as VS Code.
        // Configuration settings could be provided via dependency injection and the IOptions pattern.
        var maxNumberEnv = Environment.GetEnvironmentVariable("MAX_RANDOM_NUMBER");
        if (!int.TryParse(maxNumberEnv, out var maxNumber) || maxNumber <= 0)
        {
            throw new InvalidOperationException("Error: you must set the MAX_RANDOM_NUMBER environment variable to a positive integer.");
        }

        _maxNumber = maxNumber;
    }

    /// <summary>
    /// Returns a random number between 1 and the maximum number allowed by the tool (inclusive).
    /// </summary>
    /// <returns>A random number.</returns>
    [McpServerTool(Name = "get_random_number")]
    [Description("Returns a random number between 1 and the maximum number allowed by the tool.")]
    public int GetRandomNumber()
    {
        return Random.Shared.Next(1, _maxNumber + 1);
    }
}
