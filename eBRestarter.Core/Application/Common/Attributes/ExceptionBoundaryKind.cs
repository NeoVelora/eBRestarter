namespace eBRestarter.Core.Application.Common.Attributes;

public enum ExceptionBoundaryKind
{
    GlobalHandler,

    DegradationBoundary,

    BackgroundLoop,

    ThreadRoot,

    ProcessEntry,

    AggregationPoint,

    CleanupGuard
}
