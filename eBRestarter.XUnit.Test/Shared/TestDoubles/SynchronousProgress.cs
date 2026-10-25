using System;

namespace eBRestarter.XUnit.Test.Shared.TestDoubles;

internal sealed class SynchronousProgress<T>(Action<T> onReport) : IProgress<T>
{
    private readonly Action<T> _onReport = onReport ?? throw new ArgumentNullException(nameof(onReport));

    public void Report(T value) => _onReport(value);
}
