// MetricsComputer.cs
using System;
using System.Collections.Generic;

public sealed class MetricsComputer
{
    public double ThresholdQ = 0.5;       // ml/s
    public int MinOnSamples = 5;          // require ≥5 samples above threshold to consider flow “on”
    public int MinOffSamples = 10;

    public struct Metrics
    {
        public double Qmax;     // ml/s
        public double TQmax;    // s
        public double Vvoided;  // ml
        public double PdetAtQmax; // cmH2O
        public double FlowTime;   // s (time with Q ≥ threshold)
        public double VoidingTime;// s (from flow start to end)
        public int IdxQmax;
    }

    // t: seconds for each sample; q: flow ml/s; qvol: cumulative ml; pves/pabd: cmH2O
    public Metrics Compute(IReadOnlyList<double> t, IReadOnlyList<double> q,
                           IReadOnlyList<double> qvol,
                           IReadOnlyList<double> pves, IReadOnlyList<double> pabd)
    {
        int n = Math.Min(t.Count, Math.Min(q.Count, qvol.Count));
        Metrics m = new Metrics { Qmax = 0, TQmax = 0, Vvoided = 0, PdetAtQmax = 0, FlowTime = 0, VoidingTime = 0, IdxQmax = -1 };
        if (n == 0) return m;

        // Qmax
        for (int i = 0; i < n; i++)
        {
            if (q[i] > m.Qmax) { m.Qmax = q[i]; m.IdxQmax = i; }
        }
        if (m.IdxQmax >= 0) m.TQmax = t[m.IdxQmax];

        // Vvoided (ensure non-neg)
        m.Vvoided = Math.Max(0.0, qvol[n - 1] - qvol[0]);

        // Pdet@Qmax (at Qmax index)
        if (m.IdxQmax >= 0 && m.IdxQmax < Math.Min(pves.Count, pabd.Count))
            m.PdetAtQmax = pves[m.IdxQmax] - pabd[m.IdxQmax];

        // Flow/Voiding times (hysteresis)
        int onCount = 0, offCount = 0;
        bool flowOn = false;
        double tStart = 0, tEnd = 0;
        double flowTime = 0;

        for (int i = 0; i < n; i++)
        {
            bool above = q[i] >= ThresholdQ;
            if (above)
            {
                onCount++;
                offCount = 0;
                flowTime += (i == 0 ? 0 : (t[i] - t[i - 1]));
                if (!flowOn && onCount >= MinOnSamples)
                {
                    flowOn = true;
                    tStart = t[i - MinOnSamples + 1];
                }
            }
            else
            {
                offCount++;
                onCount = 0;
                if (flowOn && offCount >= MinOffSamples)
                {
                    flowOn = false;
                    tEnd = t[i - MinOffSamples + 1];
                }
            }
        }
        if (flowOn) tEnd = t[n - 1];

        m.FlowTime = flowTime;
        m.VoidingTime = (tEnd > tStart) ? (tEnd - tStart) : 0;

        return m;
    }
}
