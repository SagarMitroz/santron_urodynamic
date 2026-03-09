using System;

namespace SantronWinApp // change to your namespace
{
    public interface IEmgSampleSource : IDisposable
    {
        event Action<double> OnSample;
        double SampleRateHz { get; }
        void Start();
        void Stop();
    }
}
