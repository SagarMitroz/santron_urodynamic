//using System;
//using NationalInstruments.DAQmx;

//namespace SantronWinApp.IO
//{
//    public interface IPumpController : IDisposable
//    {
//        void StartInfusion(int rateMlPerMin);
//        void StopInfusion();
//        void SetRate(int rateMlPerMin);
//        int CurrentRate { get; }
//        double CurrentVoltage { get; }
//        bool IsRunning { get; }
//    }

//    public sealed class PumpController : IPumpController
//    {
//        // Legacy limits from C++:
//        //   if (INFUSION_RATE <= 5) INFUSION_RATE = 5;
//        //   if (INFUSION_RATE > 100) INFUSION_RATE = 100;
//        private const int MinRate = 5;
//        private const int MaxRate = 100;

//        // AO hardware
//        private readonly string _aoChannel;
//        private readonly Task _aoTask;
//        private readonly AnalogSingleChannelWriter _aoWriter;

//        // Calibration: matches pumpslop[0] and pumpslop[1] in C++
//        // Vout = rateMlPerMin * _pumpSlope + _pumpOffset
//        private readonly double _pumpSlope;   // pumpslop[0]
//        private readonly double _pumpOffset;  // pumpslop[1]

//        public int CurrentRate { get; private set; }
//        public double CurrentVoltage { get; private set; }

//        public bool IsHardwareAvailable { get; private set; } = false;

//        //public PumpController(int pumpConst1, int pumpConst2, string aoChannel = "Dev1/ao0")
//        //{
//        //    if (pumpConst1 < 0) pumpConst1 = 0;
//        //    if (pumpConst2 < 0) pumpConst2 = 0;

//        //    _aoChannel = aoChannel;
//        //    IsHardwareAvailable = false; // Start with false

//        //    // Exact port-slop translation from C++ (see PortSlopCalculation()).
//        //    var w = pumpConst2 / 1000.0;
//        //    var y = pumpConst1 / 1000.0;
//        //    _pumpSlope = w / 100.0;
//        //    _pumpOffset = y;

//        //    try
//        //    {
//        //        // Set up a single AO task for Dev1/ao0 and keep it running.
//        //        _aoTask = new Task();
//        //        _aoTask.AOChannels.CreateVoltageChannel(
//        //            _aoChannel,
//        //            "",
//        //            0.0,
//        //            5.0,
//        //            AOVoltageUnits.Volts
//        //        );
//        //        _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
//        //        _aoTask.Start();

//        //        // ✅ FIX: Set IsHardwareAvailable to true when successful
//        //        IsHardwareAvailable = true;

//        //        // Initialize to "pump stopped" state.
//        //        CurrentRate = 0;
//        //        WriteVoltage(0.0);
//        //    }
//        //    catch (NationalInstruments.DAQmx.DaqException)
//        //    {
//        //        // ✅ DEVICE NOT CONNECTED → SWITCH TO DUMMY MODE
//        //        IsHardwareAvailable = false;
//        //        CurrentRate = 0;
//        //        // NO EXCEPTION THROWN
//        //    }
//        //}

//        public PumpController(int pumpConst1, int pumpConst2, string aoChannel = "Dev1/ao0")
//        {
//            if (pumpConst1 < 0) pumpConst1 = 0;
//            if (pumpConst2 < 0) pumpConst2 = 0;

//            _aoChannel = aoChannel;

//            // Exact port-slop translation from C++ (see PortSlopCalculation()).
//            var w = pumpConst2 / 1000.0;     // float w = pumpconstant2 / 1000;
//            var y = pumpConst1 / 1000.0;     // float y = pumpconstant1 / 1000;
//            _pumpSlope = w / 100.0;         // pumpslop[0] = w / 100;
//            _pumpOffset = y;                 // pumpslop[1] = y;

//            try
//            {

//                // Set up a single AO task for Dev1/ao0 and keep it running.
//                _aoTask = new Task();
//                _aoTask.AOChannels.CreateVoltageChannel(
//                    _aoChannel,
//                    "",
//                    0.0,
//                    5.0,
//                    AOVoltageUnits.Volts
//                );
//                _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
//                _aoTask.Start();

//                // Initialize to "pump stopped" state.
//                CurrentRate = 0;
//                WriteVoltage(0.0);
//            }
//            catch (NationalInstruments.DAQmx.DaqException)
//            {
//                // ✅ DEVICE NOT CONNECTED → SWITCH TO DUMMY MODE
//                IsHardwareAvailable = false;

//                CurrentRate = 0;   // Allow test to start
//                                   // NO EXCEPTION THROWN
//            }

//        }

//        public PumpController(double pumpSlope, double pumpOffset, string aoChannel = "Dev1/ao0")
//        {
//            _aoChannel = aoChannel;
//            _pumpSlope = pumpSlope;
//            _pumpOffset = pumpOffset;

//            _aoTask = new Task();
//            _aoTask.AOChannels.CreateVoltageChannel(
//                _aoChannel,
//                "",
//                0.0,
//                5.0,
//                AOVoltageUnits.Volts
//            );
//            _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
//            _aoTask.Start();

//            CurrentRate = 0;
//            WriteVoltage(0.0);
//        }

//        public PumpController()
//            : this(pumpConst1: 1000, pumpConst2: 1000, aoChannel: "Dev1/ao0")
//        {
//        }

//        public bool IsRunning { get; private set; } = false;


//        public void StartInfusion(int rateMlPerMin)
//        {
//            SetRate(rateMlPerMin);
//            IsRunning = true;
//        }

//        public void SetRate(int rateMlPerMin)
//        {
//            // Match C++ INFUSION_RATE limits: 5..100
//            if (rateMlPerMin < MinRate) rateMlPerMin = MinRate;
//            if (rateMlPerMin > MaxRate) rateMlPerMin = MaxRate;

//            CurrentRate = rateMlPerMin;

//            // Exact mapping: data[0] = INFUSION_RATE * pumpslop[0] + pumpslop[1]
//            var volts = (CurrentRate * _pumpSlope) + _pumpOffset;
//            if (volts < 0.0) volts = 0.0;
//            if (volts > 5.0) volts = 5.0;

//            WriteVoltage(volts);
//        }

//        public void StopInfusion()
//        {
//            CurrentRate = 0;
//            WriteVoltage(0.0);
//            IsRunning = false;
//        }


//        private void WriteVoltage(double volts)
//        {
//            try
//            {
//                // Writer / Task not available
//                if (_aoWriter == null || _aoTask == null)
//                    return;

//                // Task is invalid or device disconnected
//                if (_aoTask.IsDone)
//                    return;

//                _aoWriter.WriteSingleSample(true, volts);
//                CurrentVoltage = volts;
//            }
//            catch (DaqException)
//            {
//                // Device disconnected, task stopped, etc.
//                // Safely stop using AO until reinitialized
//                CurrentVoltage = 0;
//            }
//            catch (ObjectDisposedException)
//            {
//                // Happens when task is disposed due to device removal
//                CurrentVoltage = 0;
//            }
//            catch
//            {
//                // Ignore any other transient errors
//            }
//        }


//        public void Dispose()
//        {
//            try
//            {
//                // Put pump in safe state
//                WriteVoltage(0.0);
//            }
//            catch { }

//            try { _aoTask?.Stop(); } catch { }
//            try { _aoTask?.Dispose(); } catch { }
//        }



//        //private void WriteVoltage(double volts)
//        //{
//        //    // If hardware is not available, just simulate
//        //    if (!IsHardwareAvailable)
//        //    {
//        //        CurrentVoltage = volts;
//        //        return;
//        //    }

//        //    try
//        //    {
//        //        if (_aoWriter == null || _aoTask == null)
//        //            return;

//        //        if (_aoTask.IsDone)
//        //            return;

//        //        _aoWriter.WriteSingleSample(true, volts);
//        //        CurrentVoltage = volts;
//        //    }
//        //    catch (DaqException)
//        //    {
//        //        IsHardwareAvailable = false;  // Device disconnected
//        //        CurrentVoltage = volts;       // Still simulate
//        //    }
//        //    catch (ObjectDisposedException)
//        //    {
//        //        IsHardwareAvailable = false;  // Device disposed
//        //        CurrentVoltage = volts;       // Still simulate
//        //    }
//        //    catch
//        //    {
//        //        // Ignore any other transient errors
//        //    }
//        //}

//        //public void Dispose()
//        //{
//        //    try
//        //    {
//        //        // Put pump in safe state
//        //        WriteVoltage(0.0);
//        //    }
//        //    catch { }

//        //    if (IsHardwareAvailable)
//        //    {
//        //        try { _aoTask?.Stop(); } catch { }
//        //        try { _aoTask?.Dispose(); } catch { }
//        //    }
//        //}
//    }
//}




using System;
using NationalInstruments.DAQmx;

namespace SantronWinApp.IO
{
    public interface IPumpController : IDisposable
    {
        void StartInfusion(int rateMlPerMin);
        void StopInfusion();
        void SetRate(int rateMlPerMin);
        int CurrentRate { get; }
        double CurrentVoltage { get; }
        bool IsRunning { get; }
    }

    public sealed class PumpController : IPumpController
    {
        // Legacy limits from C++:
        //   if (INFUSION_RATE <= 5) INFUSION_RATE = 5;
        //   if (INFUSION_RATE > 100) INFUSION_RATE = 100;
        private const int MinRate = 5;
        private const int MaxRate = 100;

        // AO hardware
        private readonly string _aoChannel;
        private readonly Task _aoTask;
        private readonly AnalogSingleChannelWriter _aoWriter;

        // Calibration: matches pumpslop[0] and pumpslop[1] in C++
        // Vout = rateMlPerMin * _pumpSlope + _pumpOffset
        private readonly double _pumpSlope;   // pumpslop[0]
        private readonly double _pumpOffset;  // pumpslop[1]

        public int CurrentRate { get; private set; }
        public double CurrentVoltage { get; private set; }

        public bool IsHardwareAvailable { get; private set; } = false;
        public PumpController(int pumpConst1, int pumpConst2, string aoChannel = "Dev1/ao0")
        {
            if (pumpConst1 < 0) pumpConst1 = 0;
            if (pumpConst2 < 0) pumpConst2 = 0;

            _aoChannel = aoChannel;

            // Exact port-slop translation from C++ (see PortSlopCalculation()).
            var w = pumpConst2 / 1000.0;     // float w = pumpconstant2 / 1000;
            var y = pumpConst1 / 1000.0;     // float y = pumpconstant1 / 1000;
            _pumpSlope = w / 100.0;         // pumpslop[0] = w / 100;
            _pumpOffset = y;                 // pumpslop[1] = y;

            try
            {

                // Set up a single AO task for Dev1/ao0 and keep it running.
                _aoTask = new Task();
                _aoTask.AOChannels.CreateVoltageChannel(
                    _aoChannel,
                    "",
                    0.0,
                    5.0,
                    AOVoltageUnits.Volts
                );
                _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
                _aoTask.Start();

                // Initialize to "pump stopped" state.
                CurrentRate = 0;
                WriteVoltage(0.0);
            }
            catch (NationalInstruments.DAQmx.DaqException)
            {
                // ✅ DEVICE NOT CONNECTED → SWITCH TO DUMMY MODE
                IsHardwareAvailable = false;

                CurrentRate = 0;   // Allow test to start
                                   // NO EXCEPTION THROWN
            }

        }

        public PumpController(double pumpSlope, double pumpOffset, string aoChannel = "Dev1/ao0")
        {
            _aoChannel = aoChannel;
            _pumpSlope = pumpSlope;
            _pumpOffset = pumpOffset;

            _aoTask = new Task();
            _aoTask.AOChannels.CreateVoltageChannel(
                _aoChannel,
                "",
                0.0,
                5.0,
                AOVoltageUnits.Volts
            );
            _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
            _aoTask.Start();

            CurrentRate = 0;
            WriteVoltage(0.0);
        }

        public PumpController()
            : this(pumpConst1: 1000, pumpConst2: 1000, aoChannel: "Dev1/ao0")
        {
        }

        public bool IsRunning { get; private set; } = false;


        public void StartInfusion(int rateMlPerMin)
        {
            SetRate(rateMlPerMin);
            IsRunning = true;
        }

        public void SetRate(int rateMlPerMin)
        {
            // Match C++ INFUSION_RATE limits: 5..100
            if (rateMlPerMin < MinRate) rateMlPerMin = MinRate;
            if (rateMlPerMin > MaxRate) rateMlPerMin = MaxRate;

            CurrentRate = rateMlPerMin;

            // Exact mapping: data[0] = INFUSION_RATE * pumpslop[0] + pumpslop[1]
            var volts = (CurrentRate * _pumpSlope) + _pumpOffset;
            if (volts < 0.0) volts = 0.0;
            if (volts > 5.0) volts = 5.0;

            WriteVoltage(volts);
        }

        public void StopInfusion()
        {
            CurrentRate = 0;
            WriteVoltage(0.0);
            IsRunning = false;
        }

        //private void WriteVoltage(double volts)
        //{
        //    if (_aoWriter == null) return;

        //    try
        //    {
        //        _aoWriter.WriteSingleSample(true, volts);
        //        CurrentVoltage = volts;
        //    }
        //    catch
        //    {
        //        // Swallow transient DAQ errors in runtime; caller controls lifetime.
        //        // You can log here if needed.
        //    }
        //}

        private void WriteVoltage(double volts)
        {
            try
            {
                // Writer / Task not available
                if (_aoWriter == null || _aoTask == null)
                    return;

                // Task is invalid or device disconnected
                if (_aoTask.IsDone)
                    return;

                _aoWriter.WriteSingleSample(true, volts);
                CurrentVoltage = volts;
            }
            catch (DaqException)
            {
                // Device disconnected, task stopped, etc.
                // Safely stop using AO until reinitialized
                CurrentVoltage = 0;
            }
            catch (ObjectDisposedException)
            {
                // Happens when task is disposed due to device removal
                CurrentVoltage = 0;
            }
            catch
            {
                // Ignore any other transient errors
            }
        }


        public void Dispose()
        {
            try
            {
                // Put pump in safe state
                WriteVoltage(0.0);
            }
            catch { }

            try { _aoTask?.Stop(); } catch { }
            try { _aoTask?.Dispose(); } catch { }
        }

    }
}