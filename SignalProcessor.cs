using NationalInstruments.Restricted;
using SantronWinApp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using static SantronWinApp.SystemSetup;

namespace SantronWinApp
{
    public interface ISignalProcessor
    {
        event Action<SampleFrame> OnDecimated;
        void SetFlowWindowFromConstant(int constant2);
        void PushRaw(SampleFrame rawCounts);
        double DisplayRateHz { get; }
        void ResetAllChannels();
        void ResetForLiveGraphExceptPressureChannels();
        void ZeroNonPressureChannels(double[] rawNow, params int[] pressureIdx);
        void ZeroChannel(int rawIndex, double currentRawCount);
        void ZeroNow();
        void SetBaselineSamples(int samples);
        void SetEmgWindowMs(int windowMs);
        void SetSmoothingAlpha(double a);
        void ForceChannelZero(int channelIndex, double rawValue);
    }

    public sealed class SignalProcessor : ISignalProcessor
    {
        public event Action<SampleFrame> OnDecimated;

        private readonly ICalibration _cal;
        private readonly double _srHz;
        private readonly double _displayHz;
        private int _decimN;

        public double DisplayRateHz => _displayHz;

        private TestMode _mode;
        private bool UppMode => _mode == TestMode.UPP;

        // ---- Baseline (raw counts zero) ----
        private readonly double[] _baselineSum = new double[8];
        private readonly double[] _baseline = new double[8];
        private int _baselineCount;
        private int _baselineTarget;
        private bool _baselineReady;

        // ---- Post-zero skip frames ----
        private int _postZeroSkipFrames;
        private int _postZeroFrameCounter;

        // ---- EMG RMS ----
        private EmgRmsFilter _emg;
        private int _emgWindowMs = 300;

        // ---- Median filter for PVES/PABD ----
        private const int MEDIAN_WINDOW = 5;
        private readonly Queue<double> _medPVES = new Queue<double>(MEDIAN_WINDOW);
        private readonly Queue<double> _medPABD = new Queue<double>(MEDIAN_WINDOW);

        // ---- Decimation ----
        private readonly double[] _sumEng = new double[8];
        private int _decimCount;

        // ---- EMA smoothing ----
        private readonly double[] _ema = new double[8];
        private bool _emaInit;
        private double _emaAlpha = 0.08;

        // ---- Timebase ----
        private double _tSeconds;
        private double _lastRawT;
        private bool _hasLastRawT;

        // ---- Reusable buffers (zero allocation) ----
        private readonly double[] _countsZeroed = new double[8];
        private readonly double[] _eng = new double[8];
        private readonly double[] _yDec = new double[8];
        private readonly double[] _ySmooth = new double[8];
        private readonly double[] _displayed = new double[8];

        // ---- Spike detection ----
        private readonly double[] _prevDec = new double[8];
        private bool _hasPrevDec;
        private readonly int[] _spikeCount = new int[8];

        // ---- Flow computation (4Hz method) ----
        private readonly Queue<double> _qvolWindow = new Queue<double>(9);
        private int _qvolSampleIndex;
        private double _flowEma;
        private bool _flowEmaInit;

        // ---- QVOL smoothing ----
        private double _qvolEma;
        private bool _qvolEmaInit;

        // ---- VINF smoothing ----
        private double _vinfEma;
        private bool _vinfEmaInit;

        // ---- Performance monitoring ----
        private long _rawIn;
        private long _decOut;
        private long _lastDecTick;
        private long _rawInLast;
        private long _decOutLast;
        private long _lastRateTick;

        // ---- Reflection-based optimization ----
        private delegate void CountsToEngIntoDelegate(object cal, double[] counts, double[] dstEng, bool uppMode);
        private CountsToEngIntoDelegate _countsToEngInto;
        private bool _countsToEngIntoResolved;

        // ---- Flow window (unused but kept for interface compatibility) ----
        private int _flowWindowFrames = 1;


        //Start Code for Get System Setup Data 16-02-2025 for Set Dynamic Data
        public SystemSetup.SystemSetupModel GetSystemSetupData(string pvesId = null)
        {
            try
            {
                // Get the folder path
                string folder = AppPathManager.GetFolderPath("SystemSetup");

                if (!Directory.Exists(folder))
                    return null;

                // If specific PVES ID is provided, try to load that file
                if (!string.IsNullOrEmpty(pvesId))
                {
                    string specificFile = Path.Combine(folder, pvesId + ".dat");
                    if (File.Exists(specificFile))
                    {
                        byte[] encrypted = File.ReadAllBytes(specificFile);
                        string json = SystemSetup.CryptoHelper.Decrypt(encrypted);
                        return JsonSerializer.Deserialize<SystemSetup.SystemSetupModel>(json);
                    }
                }

                // Otherwise load the first .dat file found
                string[] files = Directory.GetFiles(folder, "*.dat");
                if (files.Length == 0)
                    return null;

                string filePath = files[0];
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string jsonData = SystemSetup.CryptoHelper.Decrypt(encryptedData);

                return JsonSerializer.Deserialize<SystemSetup.SystemSetupModel>(jsonData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading system setup: {ex.Message}");
                return null;
            }
        }

        //End Code for Get System Setup Data 16-02-2025 for Set Dynamic Data

        private double _defaultSampleRate;
        private double _defaultPointsPerMin;
        public SignalProcessor(ICalibration cal, double sampleRateHz = 400.0, double displayHz = 8.0)
        {
            //Start Code For Dynamic Data get
            var setupData = GetSystemSetupData();

            if (setupData != null && !string.IsNullOrEmpty(setupData.Rate) &&
               double.TryParse(setupData.Rate, out double rate) && rate > 0)
            {
                _defaultSampleRate = rate;
            }
            else
            {
                _defaultSampleRate = 400.0; // Default fallback
            }
            //End Code For Dynamic Data get

            _cal = cal;
            //_srHz = sampleRateHz <= 0 ? 400.0 : sampleRateHz;
            _srHz = sampleRateHz <= 0 ? _defaultSampleRate : sampleRateHz;
            _displayHz = displayHz <= 0 ? 8.0 : displayHz;
            _decimN = Math.Max(1, (int)Math.Round(_srHz / _displayHz));
            _baselineTarget = Math.Max(1, (int)Math.Round(_srHz));
            _postZeroSkipFrames = Math.Max(1, (int)Math.Round(DisplayRateHz * 0.25));
            _emg = new EmgRmsFilter(_emgWindowMs, _srHz);
            _lastRateTick = Stopwatch.GetTimestamp();
            _lastDecTick = Stopwatch.GetTimestamp();
        }

        //This Code for Dynamic data get
        public void SetFlowWindowFromConstant(int constant2)
        {
            var setupData = GetSystemSetupData();

            if (setupData != null && !string.IsNullOrEmpty(setupData.Point) &&
               double.TryParse(setupData.Constant2, out double pointsmin) && pointsmin > 0)
            {
                _defaultPointsPerMin = pointsmin;
            }
            else
            {
                _defaultPointsPerMin = 250; // Default fallback
            }

            //int ms = constant2 > 0 ? constant2 : 250;
            int ms = (int)(constant2 > 0 ? constant2 : _defaultPointsPerMin);

            _flowWindowFrames = Math.Max(1, (int)Math.Round(DisplayRateHz * ms / 1000.0));
        }

        //This Code for Static Data Get

        //public void SetFlowWindowFromConstant(int constant2)
        //{
        //    int ms = constant2 > 0 ? constant2 : 250;
        //    _flowWindowFrames = Math.Max(1, (int)Math.Round(DisplayRateHz * ms / 1000.0));
        //}

        public void SetEmgWindowMs(int windowMs)
        {
            _emgWindowMs = Math.Max(10, Math.Min(2000, windowMs));
            _emg.SetWindow(_emgWindowMs, _srHz);
        }

        public void SetMode(TestMode mode) => _mode = mode;

        public void SetBaselineSamples(int samples)
        {
            _baselineTarget = Math.Max(10, Math.Min((int)(_srHz * 5), samples));
        }

        public void SetSmoothingAlpha(double a) => _emaAlpha = Math.Max(0.0, Math.Min(1.0, a));

        public void ZeroChannel(int rawIndex, double currentRawCount)
        {
            if (rawIndex < 0 || rawIndex > 6) return;
            _baseline[rawIndex] = currentRawCount;
            _baselineReady = true;
        }

        public void ZeroNow()
        {
            Array.Clear(_baselineSum, 0, 8);
            Array.Clear(_baseline, 0, 8);
            _baselineCount = 0;
            _baselineReady = false;
            _postZeroFrameCounter = 0;
            _hasPrevDec = false;
            _flowEmaInit = false;
            _flowEma = 0.0;
            _qvolEmaInit = false;
            _qvolEma = 0.0;
            _vinfEmaInit = false;
            _vinfEma = 0.0;
            _hasLastRawT = false;
            _qvolWindow.Clear();
            _qvolSampleIndex = 0;
            Array.Clear(_prevDec, 0, 8);
        }

        public void ResetAllChannels()
        {
            Array.Clear(_baselineSum, 0, 8);
            Array.Clear(_baseline, 0, 8);
            _baselineCount = 0;
            _baselineReady = false;
            _postZeroFrameCounter = 0;

            _medPVES.Clear();
            _medPABD.Clear();

            _emg = new EmgRmsFilter(_emgWindowMs, _srHz);

            Array.Clear(_sumEng, 0, 8);
            Array.Clear(_ema, 0, 8);
            _emaInit = false;
            _decimCount = 0;
            _tSeconds = 0.0;

            _hasPrevDec = false;
            Array.Clear(_prevDec, 0, 8);

            _flowEmaInit = false;
            _flowEma = 0.0;
            _qvolEmaInit = false;
            _qvolEma = 0.0;
            _vinfEmaInit = false;
            _vinfEma = 0.0;

            _hasLastRawT = false;
            _qvolWindow.Clear();
            _qvolSampleIndex = 0;
        }

        public void ResetForLiveGraphExceptPressureChannels()
        {
            _postZeroFrameCounter = 0;

            _medPVES.Clear();
            _medPABD.Clear();

            _emg = new EmgRmsFilter(_emgWindowMs, _srHz);

            Array.Clear(_sumEng, 0, 8);
            Array.Clear(_ema, 0, 8);
            _emaInit = false;
            _decimCount = 0;
            _tSeconds = 0.0;

            _hasPrevDec = false;
            Array.Clear(_prevDec, 0, 8);

            _flowEmaInit = false;
            _flowEma = 0.0;
            _qvolEmaInit = false;
            _qvolEma = 0.0;
            _vinfEmaInit = false;
            _vinfEma = 0.0;

            _hasLastRawT = false;
            _qvolWindow.Clear();
            _qvolSampleIndex = 0;
        }

        public void ZeroNonPressureChannels(double[] rawNow, params int[] pressureIdx)
        {
            var pressureSet = new HashSet<int>(pressureIdx ?? Array.Empty<int>());
            for (int ch = 0; ch < 7; ch++)
            {
                if (!pressureSet.Contains(ch))
                    _baseline[ch] = rawNow[ch];
            }
        }

        public void ForceChannelZero(int channelIndex, double rawValue)
        {
            _baseline[channelIndex] = rawValue;
            _ema[channelIndex] = 0;
            _sumEng[channelIndex] = 0;
            _emaInit = false;
        }

        private void EmitZeroFrame(double timestamp)
        {
            var zeroFrame = new double[8];
            Array.Clear(zeroFrame, 0, 8);

            // Use synthetic time for zero frames to ensure consistent spacing
            _tSeconds += 1.0 / _displayHz;

            Interlocked.Increment(ref _decOut);
            OnDecimated?.Invoke(new SampleFrame(_tSeconds, zeroFrame));
        }

        // ---- Helper methods ----

        private static double EmaAlphaFor(int ch)
        {
            switch ((ChannelId)ch)
            {
                case ChannelId.PVES:
                case ChannelId.PABD:
                case ChannelId.PDET: return 0.60;
                case ChannelId.EMG: return 0.25;
                case ChannelId.QVOL: return 0.30;
                case ChannelId.FRATE_OR_UPP: return 0.40;
                default: return 0.30;
            }
        }

        private static double NoiseThreshold(int ch)
        {
            switch ((ChannelId)ch)
            {
                case ChannelId.PVES:
                case ChannelId.PABD:
                case ChannelId.PDET: return 20.0;
                case ChannelId.EMG: return 8.0;
                case ChannelId.QVOL: return 3.0;
                case ChannelId.FRATE_OR_UPP: return 5.0;
                default: return 20.0;
            }
        }

        private static double SpikeLimitPerFrame(int ch)
        {
            switch ((ChannelId)ch)
            {
                case ChannelId.PVES:
                case ChannelId.PABD:
                case ChannelId.PDET:
                case ChannelId.PURA: return 2.5;
                case ChannelId.VINF:
                case ChannelId.QVOL: return 5.0;
                case ChannelId.EMG: return 50.0;
                default: return 5.0;
            }
        }

        private static double MedianPush(Queue<double> q, double x)
        {
            if (q.Count == MEDIAN_WINDOW) q.Dequeue();
            q.Enqueue(x);
            var a = q.ToArray();
            Array.Sort(a);
            return a[a.Length / 2];
        }

        private void CountsToEngReuse(double[] countsZeroed, bool uppMode, double[] dstEng)
        {
            if (!_countsToEngIntoResolved)
            {
                _countsToEngIntoResolved = true;
                try
                {
                    var mi = _cal.GetType().GetMethod("CountsToEngInto",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(double[]), typeof(double[]), typeof(bool) },
                        null);

                    if (mi != null)
                    {
                        _countsToEngInto = (cal, c, dst, upp) => mi.Invoke(cal, new object[] { c, dst, upp });
                    }
                }
                catch { _countsToEngInto = null; }
            }

            if (_countsToEngInto != null)
            {
                _countsToEngInto(_cal, countsZeroed, dstEng, uppMode);
                return;
            }

            var tmp = _cal.CountsToEng(countsZeroed, uppMode);
            int n = Math.Min(8, tmp?.Length ?? 0);
            Array.Clear(dstEng, 0, 8);
            for (int i = 0; i < n; i++) dstEng[i] = tmp[i];
        }

        public void PushRaw(SampleFrame rawCounts)
        {
            Interlocked.Increment(ref _rawIn);

            // ---- Update timestamp tracking ----
            if (rawCounts.Values != null && rawCounts.Values.Length > 0)
            {
                _lastRawT = rawCounts.T;
                _hasLastRawT = true;
            }
            else
            {
                // Null or empty data - skip processing but don't break timing
                return;
            }

            // ---- Learn baseline ----
            if (!_baselineReady)
            {
                var v = rawCounts.Values;
                int n = Math.Min(8, v.Length);
                for (int i = 0; i < n; i++) _baselineSum[i] += v[i];
                _baselineCount++;

                if (_baselineCount >= _baselineTarget)
                {
                    for (int i = 0; i < n; i++) _baseline[i] = _baselineSum[i] / _baselineCount;
                    _baselineReady = true;

                    // Reset decimation state for clean transition
                    _decimCount = 0;
                    Array.Clear(_sumEng, 0, 8);
                }

                // Continue decimation counting even during baseline collection
                _decimCount++;
                if (_decimCount >= _decimN)
                {
                    // Emit zero frame at proper decimation rate to avoid blank plotting
                    _decimCount = 0;
                    Array.Clear(_sumEng, 0, 8); // Keep sumEng synchronized
                    EmitZeroFrame(rawCounts.T);
                }
                return;
            }

            // ---- Subtract baseline ----
            var src = rawCounts.Values;
            int nz = Math.Min(8, src.Length);
            for (int i = 0; i < nz; i++) _countsZeroed[i] = src[i] - _baseline[i];

            // ---- Convert to engineering units ----
            bool uppMode = UppMode;
            CountsToEngReuse(_countsZeroed, uppMode, _eng);

            // ---- Uroflow: sanitize QVOL ----
            if (!uppMode)
            {
                int iQv = (int)ChannelId.QVOL;
                double qvol = _eng[iQv];
                if (double.IsNaN(qvol) || double.IsInfinity(qvol) || qvol < 0) qvol = 0.0;
                _eng[iQv] = qvol;
                _eng[(int)ChannelId.FRATE_OR_UPP] = 0.0;
            }

            // ---- EMG RMS ----
            _eng[(int)ChannelId.EMG] = _emg.Push(_eng[(int)ChannelId.EMG]);

            // ---- Median filter DISABLED for ultra-raw data ----
            // _eng[(int)ChannelId.PVES] = MedianPush(_medPVES, _eng[(int)ChannelId.PVES]);
            // _eng[(int)ChannelId.PABD] = MedianPush(_medPABD, _eng[(int)ChannelId.PABD]);

            // ---- Accumulate for decimation ----
            for (int i = 0; i < 8; i++) _sumEng[i] += _eng[i];
            _decimCount++;
            if (_decimCount < _decimN) return;

            // ---- Decimate ----
            for (int i = 0; i < 8; i++) _yDec[i] = _sumEng[i] / _decimCount;

            // ---- Sanitize NaN/Infinity ----
            for (int i = 0; i < 8; i++)
            {
                double v = _yDec[i];
                if (double.IsNaN(v) || double.IsInfinity(v))
                    v = _hasPrevDec ? _prevDec[i] : 0.0;
                _yDec[i] = v;
            }

            if (!_hasPrevDec) _hasPrevDec = true;
            Array.Copy(_yDec, _prevDec, 8);
            Array.Clear(_sumEng, 0, 8);
            _decimCount = 0;

            // ---- NO SMOOTHING - Use decimated values directly ----
            // Copy _yDec to _ySmooth (we'll still use _ySmooth for consistency)
            Array.Copy(_yDec, _ySmooth, 8);

            // ---- Calculate PDET from the values we'll actually display ----
            // This ensures PDET = PVES - PABD mathematically
            double pves = _ySmooth[(int)ChannelId.PVES];
            double p_abd_or_pirp = _ySmooth[(int)ChannelId.PABD];
            double pura = _ySmooth[(int)ChannelId.PURA];

            double pdetCalculated;
            if (_mode == TestMode.UPP)
                pdetCalculated = pura - pves;
            else // Whitaker or standard
                pdetCalculated = pves - p_abd_or_pirp;

            // ---- OPTIONAL: Rounding adjustment for integer display consistency ----
            // If UI displays as integers, this ensures PDET matches rounded PVES - PABD
            // Comment out this section if displaying with decimals (recommended)
            double pvesRounded = Math.Round(pves);
            double pabdRounded = Math.Round(p_abd_or_pirp);
            double pdetRounded = Math.Round(pdetCalculated);
            double expectedFromRounded = pvesRounded - pabdRounded;

            // Only adjust if off by exactly 1 due to rounding
            if (Math.Abs(pdetRounded - expectedFromRounded) == 1.0)
            {
                // Adjust to match visual expectation
                _ySmooth[(int)ChannelId.PDET] = expectedFromRounded;
            }
            else
            {
                // Use actual calculated value
                _ySmooth[(int)ChannelId.PDET] = pdetCalculated;
            }
            // ---- End rounding adjustment ----

            // ---- VINF - use direct value with small deadband ----
            const double VINF_DEADBAND = 0.20;
            int iVinf = (int)ChannelId.VINF;
            double vinfVal = _ySmooth[iVinf];
            _ySmooth[iVinf] = Math.Abs(vinfVal) < VINF_DEADBAND ? 0.0 : vinfVal;

            // ---- QVOL sanitize and smooth ----
            int iQv2 = (int)ChannelId.QVOL;
            double qv = _ySmooth[iQv2];
            if (double.IsNaN(qv) || double.IsInfinity(qv)) qv = 0.0;
            qv = Math.Max(0.0, qv);

            // Apply light EMA smoothing to QVOL to reduce fluctuations
            const double QVOL_ALPHA = 0.25;  // Responsive but smooth
            if (!_qvolEmaInit)
            {
                _qvolEma = qv;
                _qvolEmaInit = true;
            }
            else
            {
                _qvolEma += QVOL_ALPHA * (qv - _qvolEma);
            }

            _ySmooth[iQv2] = _qvolEma;

            // ---- Flow computation (4Hz method) with EMA smoothing ----
            if (!UppMode)
            {
                int iFr = (int)ChannelId.FRATE_OR_UPP;
                double qvolNow = _ySmooth[iQv2];

                _qvolWindow.Enqueue(qvolNow);
                while (_qvolWindow.Count > 9) _qvolWindow.Dequeue();

                double flowRaw = 0.0;
                if (_qvolSampleIndex >= 9 && _qvolWindow.Count == 9)
                {
                    double qvolOldest = _qvolWindow.Peek();
                    double deltaV = qvolNow - qvolOldest;

                    const double VOL_EPS = 0.05;
                    if (Math.Abs(deltaV) > VOL_EPS) flowRaw = deltaV;
                }

                if (double.IsNaN(flowRaw) || double.IsInfinity(flowRaw) || flowRaw < 0.0)
                    flowRaw = 0.0;

                // Apply EMA smoothing to flow - lighter than before but reduces noise
                const double FLOW_ALPHA_UP = 0.20;    // Faster response when increasing
                const double FLOW_ALPHA_DOWN = 0.35;  // Faster response when decreasing

                if (!_flowEmaInit)
                {
                    _flowEma = flowRaw;
                    _flowEmaInit = true;
                }
                else
                {
                    double alpha = flowRaw < _flowEma ? FLOW_ALPHA_DOWN : FLOW_ALPHA_UP;
                    _flowEma += alpha * (flowRaw - _flowEma);
                    if (_flowEma < 0.05) _flowEma = 0.0;
                }

                const double FLOW_DEADBAND = 1.0;
                _ySmooth[iFr] = _flowEma < FLOW_DEADBAND ? 0.0 : _flowEma;

                _qvolSampleIndex++;
            }

            // ---- No deadband on PDET - use exact calculated values ----
            // PDET should always equal PVES - PABD exactly

            // ---- Skip frames after zero (emit zeros to maintain timing) ----
            if (_postZeroFrameCounter < _postZeroSkipFrames)
            {
                _postZeroFrameCounter++;

                // Emit zero frame with synthetic timestamp to avoid gaps
                var zeroFrame = new double[8];
                Array.Clear(zeroFrame, 0, 8);

                _tSeconds += 1.0 / _displayHz;

                Interlocked.Increment(ref _decOut);
                OnDecimated?.Invoke(new SampleFrame(_tSeconds, zeroFrame));
                return;
            }

            // ---- Emit output ----
            Array.Copy(_ySmooth, _displayed, 8);
            var yOut = new double[8];
            Array.Copy(_displayed, yOut, 8);

            // Use synthetic time for consistent spacing (prevents gaps from timestamp irregularities)
            _tSeconds += 1.0 / _displayHz;

            var tickNow = Stopwatch.GetTimestamp();
            double emitMs = (tickNow - _lastDecTick) * 1000.0 / Stopwatch.Frequency;
            _lastDecTick = tickNow;

            Interlocked.Increment(ref _decOut);



            OnDecimated?.Invoke(new SampleFrame(_tSeconds, yOut));
        }
    }
}