using NationalInstruments.DAQmx;
using System;
using System.Diagnostics;
using System.Threading;

namespace SantronWinApp
{
    public interface IDaqService : IDisposable
    {
        /// <summary>
        /// RAW frames in COUNTS (0..4095-ish). Frequency = SampleRateHz.
        /// In finite-burst mode we raise SamplesPerTick frames quickly every TickSec.
        /// </summary>
        event Action<SampleFrame> OnRawSample;

        double SampleRateHz { get; }

        /// <param name="physicalChannels">Example: "Dev1/ai0:6"</param>
        void Start(string physicalChannels);

        void Stop();
    }

    /// <summary>
    /// Finite-burst DAQ:
    /// - Every TickSec (0.25s) we acquire SamplesPerTick samples/channel (100).
    /// - We raise OnRawSample for each sample, so existing SignalProcessor logic
    ///   (EMG RMS, flow, etc.) still works.
    /// - Plotting stays smooth because SignalProcessor decimates to 4 Hz.
    /// </summary>
    public sealed class DaqService : IDaqService
    {
        public event Action<SampleFrame> OnRawSample;

        // ======== REQUIRED BY YOUR NEW APPROACH ========
        public const double TickSec = 0.25;       // 250 ms
        public const int SamplesPerTick = 50;    // raw samples/channel per burst

        // 100 samples in 0.25s => 400 Hz
        public double SampleRateHz => SamplesPerTick / TickSec; // 400 Hz

        // Convert 0..10V -> 0..4095 counts (legacy scaling)
        private const double COUNTS_PER_10V = 4095.0;

        private readonly object _lock = new object();
        private Timer _timer;
        private volatile bool _running;
        private volatile int _inTick; // re-entrancy guard

        private string _channels;
        private double _t;

        private NationalInstruments.DAQmx.Task _task;
        private AnalogMultiChannelReader _reader;
        private int _channelCount;

        private Thread _worker;
        private CancellationTokenSource _cts;
        private DateTime _daqStartUtc;

        public void Start(string physicalChannels)
        {
            if (string.IsNullOrWhiteSpace(physicalChannels))
                throw new ArgumentException("physicalChannels is required, e.g. \"Dev1/ai0:6\"");

            lock (_lock)
            {
                StopInternal();

                _channels = physicalChannels;
                _t = 0.0;
                _running = true;
                _daqStartUtc = DateTime.UtcNow;

                _task = new NationalInstruments.DAQmx.Task();
                _task.AIChannels.CreateVoltageChannel(
                    physicalChannelName: _channels,
                    nameToAssignChannel: "",
                    terminalConfiguration: AITerminalConfiguration.Rse,
                    minimumValue: 0,
                    maximumValue: 10,
                    units: AIVoltageUnits.Volts
                );

                // ✅ Continuous mode: start once, read blocks of 100 forever
                _task.Timing.ConfigureSampleClock(
                    signalSource: "",
                    rate: SampleRateHz, // 400 Hz
                    activeEdge: SampleClockActiveEdge.Rising,
                    sampleMode: SampleQuantityMode.ContinuousSamples,
                    samplesPerChannel: SamplesPerTick * 10 // buffer hint (e.g., 1000)
                );

                // Optional but recommended: bigger input buffer to avoid driver stalls
                try
                {
                    _task.Stream.Buffer.InputBufferSize = SamplesPerTick * 50; // 5000 samples/channel
                }
                catch { /* some NI configs may not allow */ }

                _task.Control(TaskAction.Verify);

                _reader = new AnalogMultiChannelReader(_task.Stream);
                _channelCount = _task.AIChannels.Count;

                // ✅ Start the hardware task ONCE
                _task.Start();

                _cts = new CancellationTokenSource();

                // ✅ Dedicated read loop (no Timer drift)
                _worker = new Thread(() => WorkerLoop(_cts.Token))
                {
                    IsBackground = true,
                    Name = "DAQ Worker"
                };
                _worker.Start();
            }
        }

        private void WorkerLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    NationalInstruments.DAQmx.Task task;
                    AnalogMultiChannelReader reader;

                    lock (_lock)
                    {
                        if (!_running) break;
                        task = _task;
                        reader = _reader;
                    }

                    if (task == null || reader == null) break;

                    // This blocks until SamplesPerTick are available -> ~250ms naturally at 400 Hz
                    var swTotal = Stopwatch.StartNew();
                    long t0 = Stopwatch.GetTimestamp();

                    double[,] v = reader.ReadMultiSample(SamplesPerTick);

                    long tRead = Stopwatch.GetTimestamp();
                    swTotal.Stop();

                    int channels = v.GetLength(0);
                    int samples = v.GetLength(1);

                    double[] row = new double[channels];

                    for (int s = 0; s < samples; s++)
                    {
                        for (int c = 0; c < channels; c++)
                            row[c] = v[c, s] * (COUNTS_PER_10V / 10.0);

                        _t += 1.0 / SampleRateHz;
                        OnRawSample?.Invoke(new SampleFrame(_t, row));
                        PerfTrace.EveryMs("TBASE", 1000, () =>
                        {
                            // _t is your DAQ-derived seconds (incremented by 1/SampleRateHz)
                            double wall = (DateTime.UtcNow - _daqStartUtc).TotalSeconds;
                            return $"wall={wall:F2}s daqT={_t:F2}s wall-daq={wall - _t:F2}s";
                        });
                    }

                    double msRead = (tRead - t0) * 1000.0 / Stopwatch.Frequency;

                    // log once per second
                    PerfTrace.EveryMs("DAQ", 1000, () =>
                        $"CONT read={msRead:F1}ms total={swTotal.ElapsedMilliseconds}ms burst={SamplesPerTick} sr={SampleRateHz}Hz");
                }
            }
            catch (Exception ex)
            {
                PerfTrace.Log("DAQ", "WorkerLoop exception: " + ex.Message);
            }
        }


        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            _running = false;

            try { _cts?.Cancel(); } catch { }
            try { _worker?.Join(500); } catch { }

            _cts = null;
            _worker = null;

            _channels = null;
            _t = 0.0;

            try { _task?.Stop(); } catch { }
            try { _task?.Dispose(); } catch { }

            _task = null;
            _reader = null;
            _channelCount = 0;
        }



        private void StopInternalold()
        {
            _running = false;

            try { _timer?.Dispose(); } catch { }
            _timer = null;

            _channels = null;
            _t = 0.0;

            try { _task?.Dispose(); } catch { }
            _task = null;
            _reader = null;
            _channelCount = 0;

        }

        private void TickOneShot()
        {
            if (!_running) return;
            if (Interlocked.Exchange(ref _inTick, 1) == 1) return;

           
            var started = DateTime.UtcNow;

            try
            {
                AcquireOneBurst_ReuseTask();
            }
            catch
            {
                // optional logging
            }
            finally
            {
                Interlocked.Exchange(ref _inTick, 0);

                // re-arm timer to keep ~0.25s cadence (best effort)
                if (_running)
                {
                    var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
                    var due = Math.Max(1, (int)Math.Round(TickSec * 1000.0 - elapsed));
                    
                    //System.Diagnostics.Debug.WriteLine($"DAQ tick ms={elapsed:0.0}");
                    try { _timer?.Change(due, Timeout.Infinite); } catch { }
                }
            }
        }

        private void AcquireOneBurst_ReuseTask()
        {
            NationalInstruments.DAQmx.Task task;
            AnalogMultiChannelReader reader;

            lock (_lock)
            {
                if (!_running) return;
                task = _task;
                reader = _reader;
            }

            if (task == null || reader == null) return;

            var swTotal = Stopwatch.StartNew();
            long t0 = Stopwatch.GetTimestamp();



            task.Start();

            long tStart = Stopwatch.GetTimestamp();

            double[,] v = reader.ReadMultiSample(SamplesPerTick);

            long tRead = Stopwatch.GetTimestamp();
            task.Stop();

            long tStop = Stopwatch.GetTimestamp();

            int channels = v.GetLength(0);
            int samples = v.GetLength(1);

            // ✅ reuse one row buffer (big GC reduction)
            double[] row = new double[channels];

            for (int s = 0; s < samples; s++)
            {
                for (int c = 0; c < channels; c++)
                    row[c] = v[c, s] * (COUNTS_PER_10V / 10.0);

                _t += 1.0 / SampleRateHz;

                // IMPORTANT: OK because your pipeline only uses values immediately (Proc.PushRaw)
                OnRawSample?.Invoke(new SampleFrame(_t, row));
            }

            swTotal.Stop();          

            double msStart = (tStart - t0) * 1000.0 / Stopwatch.Frequency;
            double msRead = (tRead - tStart) * 1000.0 / Stopwatch.Frequency;
            double msStop = (tStop - tRead) * 1000.0 / Stopwatch.Frequency;

            PerfTrace.EveryMs("DAQ", 1000, () =>
                $"burst={SamplesPerTick} sr={SampleRateHz}Hz start={msStart:F1}ms read={msRead:F1}ms stop={msStop:F1}ms total={swTotal.ElapsedMilliseconds}ms");

        }

        public void Dispose()
        {
            Stop();
        }
    }
}
