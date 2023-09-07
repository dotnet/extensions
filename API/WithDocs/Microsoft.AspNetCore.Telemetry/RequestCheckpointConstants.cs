// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Project constants.
/// </summary>
public static class RequestCheckpointConstants
{
    /// <summary>
    /// The time elapsed before the response headers have been sent to the client.
    /// </summary>
    public const string ElapsedTillHeaders = "elthdr";

    /// <summary>
    /// The time elapsed before the response has finished being sent to the client.
    /// </summary>
    public const string ElapsedTillFinished = "eltltf";

    /// <summary>
    /// The time elapsed before hitting the <see cref="T:Microsoft.AspNetCore.Telemetry.CapturePipelineExitMiddleware" /> middleware.
    /// </summary>
    public const string ElapsedTillPipelineExitMiddleware = "eltexm";

    /// <summary>
    /// The time elapsed before the response back to middleware pipeline.
    /// </summary>
    public const string ElapsedResponseProcessed = "eltrspproc";

    /// <summary>
    /// The time elapsed before hitting the first middleware.
    /// </summary>
    public const string ElapsedTillEntryMiddleware = "eltenm";
}
