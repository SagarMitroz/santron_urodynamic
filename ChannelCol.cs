using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantronWinApp
{
    public enum ChannelCol
    {
        PVES = (int)ChannelId.PVES,                 // 0
        PABD = (int)ChannelId.PABD,                 // 1
        VINF = (int)ChannelId.VINF,                 // 3
        QVOL = (int)ChannelId.QVOL,                 // 2
        FRATE_OR_UPP = (int)ChannelId.FRATE_OR_UPP, // 5
        EMG = (int)ChannelId.EMG,                  // 4
        PURA = (int)ChannelId.PURA,                 // 6
        PDET_or_PCLO_or_PRPG = (int)ChannelId.PDET  // 7  (derived)
    }

    public enum TestMode
    {
        Cystometry,
        PressureFlow,
        PressureFlow_EMG,
        PressureFlow_Video,
        PressureFlow_EMG_Video,
        UPP,
        Whitaker,
        Uroflowmetry,
        Uroflowmetry_EMG,
        Biofeedback,
        AnalManometry,
        CystoUroflowEMG // “full” default
    }
}
