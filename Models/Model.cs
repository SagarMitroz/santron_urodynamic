using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantronWinApp
{

    public enum ChannelId
    {
        PVES = 0,           // raw 0
        PABD = 1,           // raw 1 (PIRP in Whitaker UI)
        QVOL = 2,           // raw 2
        VINF = 3,           // raw 3
        EMG = 4,           // raw 4
        FRATE_OR_UPP = 5,   // raw 5 (UPP pressure in UPP; otherwise Flow lives here after processing)
        PURA = 6,           // raw 6
        PDET = 7            // DERIVED slot (PDET/PCLO/PRPG) — computed in SignalProcessor
    }

    public sealed class CalibrationProfile
    {
        public double[] Offsets { get; set; } = new double[8];
        public double[] Slopes { get; set; } = new double[8];
        public int[] Constants { get; set; } = new int[8];
        public bool ForcePressureSlope { get; set; } = false;
        public double ForcedPVESSlope { get; set; } = 4.15;
        public double ForcedPABDSlope { get; set; } = 4.15;
        public double SpecificGravity { get; set; } = 1.0;
        public double UppSlope { get; set; } = 1.0;
    }

    public readonly struct SampleFrame
    {
        public SampleFrame(double t, double[] values) { T = t; Values = values; }
        public double T { get; }        // seconds
        public double[] Values { get; }        // length = 8   <-- update this comment
    }
}
