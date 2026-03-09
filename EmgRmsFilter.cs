using System.Collections.Generic;

namespace SantronWinApp.Processing
{
    // Full-wave rectify + moving RMS over a fixed sample window
    internal sealed class EmgRmsFilter
    {
        private readonly Queue<double> _sqWin = new Queue<double>();
        private int _winSamples;

        public EmgRmsFilter(int windowMs, double sampleRateHz)
        {
            SetWindow(windowMs, sampleRateHz);
        }

        public void SetWindow(int windowMs, double sampleRateHz)
        {
            _winSamples = System.Math.Max(1, (int)System.Math.Round(windowMs * sampleRateHz / 1000.0));
            _sqWin.Clear();
        }

        public double Push(double emgEng) // emg in engineering units (e.g., µV)
        {
            double rect = System.Math.Abs(emgEng);
            double sq = rect * rect;

            _sqWin.Enqueue(sq);
            if (_sqWin.Count > _winSamples) _sqWin.Dequeue();

            double sum = 0.0;
            foreach (var v in _sqWin) sum += v;
            double meanSq = sum / _sqWin.Count;
            return System.Math.Sqrt(meanSq);
        }

        public void Reset() { _sqWin.Clear(); }
    }
}
