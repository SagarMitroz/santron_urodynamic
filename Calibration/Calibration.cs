namespace SantronWinApp.Processing
{
    public interface ICalibration
    {
        double[] CountsToEng(double[] counts, bool uppMode = false);

        // NEW: non-allocating path
        void CountsToEngInto(double[] counts, double[] dstEng, bool uppMode = false);
    }

    public sealed class Calibration : ICalibration
    {
        private readonly CalibrationProfile _p;
        public Calibration(CalibrationProfile p) { _p = p; }

        public double[] CountsToEng(double[] c, bool uppMode = false)
        {
            var y = new double[8];
            CountsToEngInto(c, y, uppMode);
            return y;
        }

        public void CountsToEngInto(double[] c, double[] y, bool uppMode = false)
        {
            if (y == null || y.Length < 8) return;

            // clear (only needed if caller reuses buffer)
           // Array.Clear(y, 0, 8);

            if (c == null || c.Length < 7) return;

            for (int raw = 0; raw < 7; raw++)
            {
                int dst = raw;

                double slope;
                if (dst == (int)ChannelId.FRATE_OR_UPP && uppMode)
                    slope = (_p.UppSlope != 0.0) ? _p.UppSlope : 1.0;
                else if (_p.ForcePressureSlope && (dst == (int)ChannelId.PVES || dst == (int)ChannelId.PABD))
                    slope = (dst == (int)ChannelId.PVES) ? _p.ForcedPVESSlope : _p.ForcedPABDSlope;
                else
                    slope = (dst < _p.Slopes.Length && _p.Slopes[dst] != 0.0) ? _p.Slopes[dst] : 1.0;

                double off = (dst == (int)ChannelId.EMG) ? 0.0
                           : (dst < _p.Offsets.Length ? _p.Offsets[dst] : 0.0);

                if (dst == (int)ChannelId.FRATE_OR_UPP && !uppMode) { y[dst] = 0.0; continue; }

                double denom = (slope == 0.0) ? 1.0 : slope;
                double val = (c[raw] - off) / denom;

                if (dst == (int)ChannelId.VINF)
                    val = -val;

                y[dst] = val;
            }

            y[(int)ChannelId.PDET] = 0.0;
        }
    }






}
