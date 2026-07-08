using eBRestarter.Desktop.WinUI3.ObjectArchetypes.UIOptionDTO;
using System.Collections.ObjectModel;

namespace eBRestarter.Desktop.WinUI3.Models.Constants
{
    public static class ComputerRestartConstants
    {
        public static readonly ReadOnlyCollection<ComputerRestartOption> Options = new(
        [
            new("Computer nicht neustarten", 0),
            new("Computer jeden Tag neustarten", 1),
            new("Computer alle 3 Tage neustarten", 3),
            new("Computer alle 7 Tage neustarten", 7),
            new("Computer alle 14 Tage neustarten", 14)
        ]);
    }
}
