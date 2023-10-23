// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

namespace Microsoft.AspNetCore.Diagnostics.Latency;

public static class RequestCheckpointConstants
{
    public const string ElapsedTillHeaders = "elthdr";
    public const string ElapsedTillFinished = "eltltf";
    public const string ElapsedTillPipelineExitMiddleware = "eltexm";
    public const string ElapsedResponseProcessed = "eltrspproc";
    public const string ElapsedTillEntryMiddleware = "eltenm";
}
