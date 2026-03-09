using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantronWinApp
{
    internal static class HardwareMap
    {
        // index in raw DAQ array -> logical ChannelId slot
        // (return -1 to ignore)
        public static int MapRawToLogical(int rawIndex)
        {
            switch (rawIndex)
            {
                case 0: return (int)ChannelId.PVES;          // PVES
                case 1: return (int)ChannelId.PABD;          // PABD
                case 2: return (int)ChannelId.QVOL;          // QVOL
                case 3: return (int)ChannelId.VINF;          // VINF
                case 4: return (int)ChannelId.EMG;           // EMG
                case 5: return (int)ChannelId.FRATE_OR_UPP;  // UPP lane in UPP mode
                case 6: return (int)ChannelId.PURA;          // PURA
                default: return -1;                          // empty / unused
            }
        }

        public const int LogicalCount = 8; // PVES..PURA
    }
}
