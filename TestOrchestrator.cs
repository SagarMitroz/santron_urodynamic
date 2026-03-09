//using SantronWinApp.IO;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace SantronWinApp
//{
//    public sealed class TestOrchestrator : IDisposable
//    {
//        public readonly IDaqService Daq;
//        public readonly ISignalProcessor Proc;
//        public readonly IPumpController Pump;
//        public readonly ISysSetupStore Store;
//        public CalibrationProfile Profile { get; private set; }

//        public event Action<SampleFrame> OnDisplayFrame; // decimated 10 Hz

//        public TestOrchestrator(IDaqService daq, ISignalProcessor proc, IPumpController pump, ISysSetupStore store)
//        {
//            Daq = daq; Proc = proc; Pump = pump; Store = store;
//            Daq.OnRawSample += Proc.PushRaw;
//            Proc.OnDecimated += f => OnDisplayFrame?.Invoke(f);
//        }

//        public void LoadProfile(string sysSetupPath)
//        {
//            Profile = Store.Load(sysSetupPath) ?? new CalibrationProfile();
//            Proc.SetFlowWindowFromConstant(Profile.Constants[6]);
//        }

//        public void Start(string ai = "Dev1/ai0:6") => Daq.Start(ai);
//        public void Stop() => Daq.Stop();

//        public void Dispose()
//        {
//            try { Stop(); } catch { }
//            try { Pump?.Dispose(); } catch { }
//        }

//        //Code For Channel Value Set Zero

//        public void StartProcessing(string ai = "Dev1/ai0:6")
//        {
//            try
//            {
//                // Attach event again
//                Daq.OnRawSample -= Proc.PushRaw;
//                Daq.OnRawSample += Proc.PushRaw;
//                // Start DAQ
//                Daq.Start(ai);
//            }
//            catch { }
//        }
//        public void StopProcessing()
//        {
//            try
//            {
//                Daq.OnRawSample -= Proc.PushRaw;   
//            }
//            catch { }

//            try { Daq.Stop(); } catch { }
//        }

//    }
//}

using SantronWinApp.IO;
using SantronWinApp;
using System;
public sealed class TestOrchestrator : IDisposable
{
    public readonly IDaqService Daq;
    public readonly ISignalProcessor Proc;
    public readonly IPumpController Pump;
    public readonly ISysSetupStore Store;
    public CalibrationProfile Profile { get; private set; }

    public event Action<SampleFrame> OnDisplayFrame; // decimated 10 Hz


    public TestOrchestrator(IDaqService daq, ISignalProcessor proc, IPumpController pump, ISysSetupStore store)
    {
        Daq = daq; Proc = proc; Pump = pump; Store = store;

        // Subscribe once
        Daq.OnRawSample += Proc.PushRaw;
        Proc.OnDecimated += f => OnDisplayFrame?.Invoke(f);
    }

    public void Start(string ai = "Dev1/ai0:6") => Daq.Start(ai);
    public void Stop() => Daq.Stop();

    // Remove StartProcessing/StopProcessing OR make them aliases:
    public void StartProcessing(string ai = "Dev1/ai0:6") => Start(ai);
    public void StopProcessing() => Stop();

    public void Dispose()
    {
        try { Stop(); } catch { }
        try { Pump?.Dispose(); } catch { }
    }
}
