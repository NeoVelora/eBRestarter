using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public readonly record struct RestarterCycleProgress(
    RestartTaskState State,
    int SecondsRemaining,
    string StatusMessage
);

