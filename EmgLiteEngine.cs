using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using SantronChart;
using static SantronWinApp.EmgLiteForm;

namespace SantronWinApp
{
    public sealed class EmgLiteEngine : IDisposable
    {
        private readonly IEmgSampleSource _src;
        private readonly MultiChannelLiveChart _chart;
        private readonly Decimator _decimator;
        private readonly EmgSignalProcessor _proc;
        private readonly System.Windows.Forms.Timer _statusTimer;
        public event Action<double> OnEmgPoint;
        private bool _mirrorToggle = false;

        private bool _paused;
        private long _sampleCount;
        private long _lastDebugOutput = 0;

        public EmgLiteEngine(
          IEmgSampleSource src,
          MultiChannelLiveChart chart,
          double chartUpdateHz,
          int rmsWindowMs,
          int smoothingPercent,
          EmgOutputMode outputMode =EmgOutputMode.RmsEnvelope)
        {
            _src = src ?? throw new ArgumentNullException(nameof(src));
          //   _chart = chart ?? throw new ArgumentNullException(nameof(chart));

            chartUpdateHz = Math.Max(1, Math.Min(chartUpdateHz, 500));

            _decimator = new Decimator(_src.SampleRateHz, chartUpdateHz);
            _proc = new EmgSignalProcessor(_src.SampleRateHz, rmsWindowMs, smoothingPercent, outputMode);
            _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };

            Initialize();
        }

        private void Initialize()
        {
            _src.OnSample += OnSample;

            // Status update timer
            _statusTimer.Tick += (sender, e) =>
            {
                Debug.WriteLine($"Engine Status: Samples={_sampleCount}, Paused={_paused}");
            };
            _statusTimer.Start();

            Debug.WriteLine($"Engine initialized: Source SR={_src.SampleRateHz}");
        }

        public void Start() => _src.Start();
        public void Stop() => _src.Stop();
        public void SetPaused(bool paused) => _paused = paused;

        private void OnSample(double raw)
        {
            double y = _proc.Process(raw);

            if (_paused) return;

            if (_decimator.Push(y, out double yPlot))
            {
                OnEmgPoint?.Invoke(yPlot);
            }
        }



        //private void OnSample(double raw)
        //{
        //    Interlocked.Increment(ref _sampleCount);

        //    // EMG processing with muscle filter and 50Hz notch
        //    double y = _proc.Process(raw);

        //    if (_paused) return;

        //    if (_decimator.Push(y, out double yPlot))
        //    {
        //        try
        //        {
        //            if (_chart.IsDisposed) return;

        //            // Clamp the value before sending to chart
        //            yPlot = ClampChartValue(yPlot);

        //            // Debug output every 100 samples
        //            if (_sampleCount - _lastDebugOutput > 100)
        //            {
        //                Debug.WriteLine($"Chart value: {yPlot:F2}");
        //                _lastDebugOutput = _sampleCount;
        //            }

        //            if (_chart.InvokeRequired)
        //            {
        //                _chart.BeginInvoke(new Action(() =>
        //                {
        //                    try
        //                    {
        //                        if (_chart.IsDisposed) return;
        //                        _chart.AppendSample(new[] { -yPlot });
        //                        _chart.AppendSample(new[] { yPlot });

        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Debug.WriteLine($"Chart invoke error: {ex.Message}");
        //                    }
        //                }));
        //            }
        //            else
        //            {
        //                try
        //                {
        //                    _chart.AppendSample(new[] { -yPlot });
        //                    _chart.AppendSample(new[] { yPlot });

        //                }
        //                catch (Exception ex)
        //                {
        //                    Debug.WriteLine($"Chart append error: {ex.Message}");
        //                }
        //            }
        //        }
        //        catch (ObjectDisposedException) { /* ignore */ }
        //        catch (InvalidOperationException ex)
        //        {
        //            Debug.WriteLine($"Invalid operation: {ex.Message}");
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Unexpected error in OnSample: {ex.Message}");
        //        }
        //    }
        //}

        private double ClampChartValue(double value)
        {
            // Clamp to reasonable range for EMG
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            // EMG signals typically range from -5000 to 5000 uV
            if (value < -10000) return -10000;
            if (value > 10000) return 10000;

            return value;
        }

        public void Dispose()
        {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();

            if (_src != null)
            {
                _src.OnSample -= OnSample;
                _src.Dispose();
            }

            Debug.WriteLine("Engine disposed");
        }
    }
}