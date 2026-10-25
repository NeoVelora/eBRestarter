using System;

namespace eBRestarter.Desktop.WinUI3.ObjectArchetypes.Messages;

public sealed record ComputerRestartDateChangedMessage(DateTime? NewDate);
