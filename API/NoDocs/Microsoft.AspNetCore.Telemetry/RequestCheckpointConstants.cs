// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

namespace Microsoft.AspNetCore.Telemetry;

public static class RequestCheckpointConstants
{
    public const string ElapsedTillHeaders = "elthdr";
    public const string ElapsedTillFinished = "eltltf";
    public const string ElapsedTillPipelineExitMiddleware = "eltexm";
    public const string ElapsedResponseProcessed = "eltrspproc";
    public const string ElapsedTillEntryMiddleware = "eltenm";
}
