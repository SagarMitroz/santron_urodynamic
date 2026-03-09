using System;

namespace SantronWinApp.Processing
{
    public static class CalibrationProfileFactory
    {
        // constants[] and offsetsCounts[] are in RAW/HW order: 0..6 as above
        public static CalibrationProfile FromLegacy(
            int[] constants,        // length >= 7 preferred; RAW order
            double[] offsetsCounts,    // length >= 7; RAW order (counts, baseline-relative)
            int uppCount,         // raw UPP constant (for puller calibration)
            double specificGravity)  // SG for VINF division
        {
            var p = new CalibrationProfile();

            // -------- defensive copies / defaults --------
            var c = (constants != null) ? (int[])constants.Clone() : new int[8];

            var off = new double[8];                        // ChannelId-aligned (0..7)
            if (offsetsCounts != null)
            {
                int n = Math.Min(7, offsetsCounts.Length);  // only 0..6 are physical
                for (int i = 0; i < n; i++) off[i] = offsetsCounts[i];
            }
            p.Offsets = off;

            // -------- SLOPES (ChannelId-aligned) --------
            // Pressure channels use constant/200.0
            // Volume/EMG channels use constant/1000.0 (fallback 1.0)
            var slopes = new double[8];
            for (int i = 0; i < 8; i++) slopes[i] = 1.0; // defaults

            // PVES (0) & PABD (1) → pressure
            slopes[(int)ChannelId.PVES] = (c.Length > 0 && c[0] != 0) ? (c[0] / 200.0) : 1.0;
            slopes[(int)ChannelId.PABD] = (c.Length > 1 && c[1] != 0) ? (c[1] / 200.0) : 1.0;

            // QVOL (2) & VINF (3) → ml
            slopes[(int)ChannelId.QVOL] = (c.Length > 2 && c[2] != 0) ? (c[2] / 1000.0) : 1.0;
            slopes[(int)ChannelId.VINF] = (c.Length > 3 && c[3] != 0) ? (c[3] / 1000.0) : 1.0;

            // EMG (4) → uV (engineering: choose 1000.0 like legacy)
            slopes[(int)ChannelId.EMG] = (c.Length > 4 && c[4] != 0) ? (c[4] / 1000.0) : 1.0;

            // FRATE_OR_UPP (5) is handled specially:
            //  - Non-UPP: remains 0 here; SignalProcessor computes Flow from QVOL
            //  - UPP:     use UppSlope below
            // Keep slopes[5] at 1.0.

            // PURA (6) → pressure (like PVES/PABD)
            //slopes[(int)ChannelId.PURA] = (c.Length > 6 && c[6] != 0) ? (c[6] / 1000.0) : 1.0;
            slopes[(int)ChannelId.PURA] = (c.Length > 6 && c[7] != 0) ? (c[7] / 200.0) : 1.0;


            // PDET (7) is derived → leave as 1.0 (unused)
            p.Slopes = slopes;

            // -------- UPP slope (used only when uppMode==true) --------
            p.UppSlope = (uppCount > 0) ? (uppCount / 300.0) : 1.0;

            // -------- Specific gravity for VINF post-scale --------
            p.SpecificGravity = (specificGravity > 0) ? specificGravity : 1.0;

            // -------- Keep constants (for flow window, etc.) --------
            var keep = new int[Math.Max(7, c.Length)];
            Array.Copy(c, keep, c.Length);
            p.Constants = keep;

            // Optional: enforce "pressure slope override" defaults if you use them elsewhere
            p.ForcePressureSlope = p.ForcePressureSlope; // leave as set by caller/defaults

            return p;
        }
    }
}
