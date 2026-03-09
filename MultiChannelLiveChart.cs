using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Concurrent;
using System.Drawing.Drawing2D;

namespace SantronChart
{

    public class MultiChannelLiveChart : UserControl
    {
        #region Types
        public class Channel
        {
            public string Name;
            public Color Color;
            public double ScaleMin;
            public double ScaleMax;
            public string Unit; // e.g., "cmH2O", "uV", "ml/sec"



            public Channel(string name, Color color, double scaleMin, double scaleMax)
            {
                Name = name; Color = color; ScaleMin = scaleMin; ScaleMax = scaleMax; Unit = string.Empty;
            }
            public Channel(string name, Color color, double scaleMin, double scaleMax, string unit)
            {
                Name = name; Color = color; ScaleMin = scaleMin; ScaleMax = scaleMax; Unit = unit ?? string.Empty;
            }
        }

        private class Sample
        {
            public double T;            // seconds since Start()
            public double[] Values;     // length == Channels.Count
            public Sample(double t, double[] values) { T = t; Values = values; }
        }
        #endregion


        // ---- Markers ----
        public sealed class Marker
        {
            public double T;          // time in seconds (same domain as _samples[i].T)
            public string Label;      // optional text above the line
            public Color Color;       // line + label color
            public float Width;       // line width
            public System.Drawing.Drawing2D.DashStyle Dash;

            public Marker(double t, string label, Color color, float width,
                          System.Drawing.Drawing2D.DashStyle dash)
            { T = t; Label = label ?? ""; Color = color; Width = width; Dash = dash; }

        }
        private readonly List<Marker> _markers = new List<Marker>();

        public IReadOnlyList<Marker> Markers => _markers;



        // If true, draw using per-pixel buckets (min/max envelope + avg trace).
        // This is the closest match to the C++ "samples per pixel" approach.
        public bool UsePixelBuckets { get; set; } = true;

        // In C++ the envelope is often visible; in live mode it can look heavy.
        // Default false for live, but you can enable it.
        public bool DrawEnvelopeInLive { get; set; } = false;

        // 1 / 2 / 5 minutes per screen (default 2 like C++)
        public int MinutesPerScreen { get; private set; } = 2;

        public void SetMinutesPerScreen(int minutes)
        {
            if (minutes != 1 && minutes != 2 && minutes != 5)
                minutes = 2;
            _bgCacheDirty = true; // grid tick labels depend on time span
            MinutesPerScreen = minutes;

            // This field already exists in your control.
            _visibleDurationSec = MinutesPerScreen * 60.0;

            Invalidate();
        }



        #region Fields
        private readonly List<Channel> _channels;
        private readonly List<Sample> _samples;
        private readonly Stopwatch _sw;

        private readonly HashSet<string> _mirroredChannels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // High-speed channel data (for channels that update faster than others)
        private readonly Dictionary<int, List<(double T, double Value)>> _highSpeedData =
            new Dictionary<int, List<(double T, double Value)>>();
        private readonly HashSet<int> _highSpeedChannels = new HashSet<int>();


        private HScrollBar _hScroll;
        private bool _isUpdatingScroll = false; // prevents programmatic scroll from breaking live
        private Button _btnGoLive;
        private bool _lockLive;

        private double _visibleDurationSec;
        private double _viewStartSec;
        private double _totalDurationSec;

        private readonly Pen _gridPen;
        private readonly Pen _axisPen;
        private readonly Brush _axisTextBrush;
        private readonly Pen _hGridPen;
        private int _laneDivisions = 5;

        private readonly Dictionary<int, Pen> _seriesPens;
        private readonly Timer _renderTimer;

        private int _maxPointsPerChannelToKeep;
        private int _targetFps;

        private readonly ConcurrentQueue<Sample> _pendingSamples = new ConcurrentQueue<Sample>();
        private volatile bool _hasPendingData;

        private int[] _bucketStartIdx;
        private int[] _bucketEndIdx;

        private double[] _vMinArr, _vMaxArr, _vSumArr;
        private bool[] _hasPrevArr;
        private PointF[] _prevArr;

        private void EnsurePaintBuffers(int n)
        {
            if (_vMinArr == null || _vMinArr.Length != n)
            {
                _vMinArr = new double[n];
                _vMaxArr = new double[n];
                _vSumArr = new double[n];
                _hasPrevArr = new bool[n];
                _prevArr = new PointF[n];
            }
        }


        private void EnsureBucketBuffers(int widthPx)
        {
            if (_bucketStartIdx == null || _bucketStartIdx.Length != widthPx)
            {
                _bucketStartIdx = new int[widthPx];
                _bucketEndIdx = new int[widthPx];
            }
        }


        // ---- Cached background (grid/axes/static lane text) ----
        private Bitmap _bgCache;
        private Size _bgCacheSize = Size.Empty;
        private bool _bgCacheDirty = true;


        // Right-side layout (outside plot)
        private float _rightScaleWidth = 55f;       // space for min/max labels
        private float _rightLiveWidth = 110f;       // space for live value + unit
        private float _liveTopPadding = 25f;         // extra margin from top when clamping live Y
        private float _liveBottomPadding = 4f;      // small bottom pad
        #endregion



        #region Ctor
        public MultiChannelLiveChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.White;

            this.Resize += (s, e) => { _bgCacheDirty = true; };

            _channels = new List<Channel>();
            _samples = new List<Sample>();
            _sw = new Stopwatch();

            _hScroll = new HScrollBar();
            _hScroll.Height = 18; _hScroll.Dock = DockStyle.Bottom; _hScroll.Minimum = 0;
            _hScroll.Maximum = 1000; _hScroll.LargeChange = 100; _hScroll.SmallChange = 10; _hScroll.Value = 900;
            _hScroll.Scroll += OnScrollChanged; Controls.Add(_hScroll);

            _btnGoLive = new Button(); _btnGoLive.Text = "Go Live"; _btnGoLive.AutoSize = true;
            _btnGoLive.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; _btnGoLive.Height = 22; _btnGoLive.Width = 70;
            _btnGoLive.FlatStyle = FlatStyle.System; _btnGoLive.Click += delegate { ToggleLive(true); };
            Controls.Add(_btnGoLive);

            _gridPen = new Pen(Color.FromArgb(230, 230, 230));
            _axisPen = new Pen(Color.FromArgb(80, 80, 80));
            _axisTextBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
            _hGridPen = new Pen(Color.LightGray, 1f)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
            };

            SetStyle(ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

            UpdateStyles();
            _seriesPens = new Dictionary<int, Pen>();

            _renderTimer = new Timer();
            _targetFps = 30;
            _renderTimer.Interval = 1000 / _targetFps;
            _renderTimer.Tick += RenderTick;
            //_renderTimer.Tick += delegate { if (_lockLive) SnapViewToTail(); Invalidate(); };

            _visibleDurationSec = 10.0; _viewStartSec = 0.0; _totalDurationSec = 0.0;
            _maxPointsPerChannelToKeep = 8 * 60 * 120;
            // this.DoubleBuffered = true;
            Resize += OnResize;
        }
        #endregion

        #region Public API
        public void SetFps(int fps) { if (fps < 1) fps = 1; if (fps > 120) fps = 120; _targetFps = fps; _renderTimer.Interval = 1000 / _targetFps; }
        public void Start() { if (!_sw.IsRunning) { _sw.Reset(); _sw.Start(); ToggleLive(true); SetPointCapacityPerChannel((int)(_targetFps * _visibleDurationSec * 2)); _renderTimer.Start(); } }
        public void Stop() { _renderTimer.Stop(); _sw.Stop(); }

        // Enable mirror plotting for a specific channel (e.g., "EMG")
        public void SetMirrorMode(string channelName, bool enable)
        {
            if (enable)
                _mirroredChannels.Add(channelName);
            else
                _mirroredChannels.Remove(channelName);
        }

        public void SetChannels(List<Channel> channels)
        {
            _bgCacheDirty = true;
            _channels.Clear(); _channels.AddRange(channels); _seriesPens.Clear();
            int i; for (i = 0; i < _channels.Count; i++) { _seriesPens[i] = new Pen(_channels[i].Color, 1.5f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Miter, StartCap = System.Drawing.Drawing2D.LineCap.Flat, EndCap = System.Drawing.Drawing2D.LineCap.Flat }; }
            Invalidate();
            RaiseLayoutChanged();
        }
        public void Clear()
        {
            while (_pendingSamples.TryDequeue(out _)) { }  // flush queue
            _hasPendingData = false;

            _samples.Clear();
            _totalDurationSec = 0.0;
            _viewStartSec = 0.0;
            _lockLive = true;
            UpdateScrollFromViewport();
            Invalidate();
        }
        private void RenderTick(object sender, EventArgs e)
        {

            if (IsDisposed) return;

            var sw = Stopwatch.StartNew();
            int before = _pendingSamples.Count;

            const int MAX_BATCH = 800;   // tune 200–800
            bool added = false;

            // Drain up to MAX_BATCH quickly
            int n = 0;
            Sample last = null;

            while (n < MAX_BATCH && _pendingSamples.TryDequeue(out Sample s))
            {
                _samples.Add(s);
                last = s;
                added = true;
                n++;
            }

            // (this is what "smooth live plotting" usually does)
            if (_pendingSamples.Count > 0)
            {
                while (_pendingSamples.TryDequeue(out Sample s2))
                    last = s2;

                if (last != null)
                {
                    _samples.Add(last);
                    added = true;
                    System.Diagnostics.Debug.WriteLine($"paint tick add={added} pending={_pendingSamples.Count} samples={_samples.Count}");

                }
            }

            if (added)
            {
                _hasPendingData = false;
                TrimAndUpdateTotals();

                if (_lockLive)
                    SnapViewToTail();
            }

            if (added)
            {
                int after = _pendingSamples.Count;
                var pr = GetPlotRect();
                Invalidate(Rectangle.Ceiling(pr));

                sw.Stop();
                //   PerfTrace.EveryMs("CHART", 1000, () =>
                //     $"pendingBefore={before} pendingAfter={after} renderTick={sw.ElapsedMilliseconds}ms");

            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Prevent default background erase which causes flicker
        }
        //Start This code For Graph Test Start Left Side Commit on 28/11/2025

        public void AppendSample(double[] values, double timeSec)
        {
            if (IsDisposed) return;
            if (values == null) return;
            if (_channels.Count == 0) return;
            if (values.Length != _channels.Count) return;

            // Compute time here (can be called from any thread)
            double t = timeSec >= 0 ? timeSec : (_sw.IsRunning ? _sw.Elapsed.TotalSeconds : 0.0);

            // Clone once (caller may reuse array)
            double[] copy = (double[])values.Clone();

            // Enqueue; do NOT BeginInvoke + do NOT Invalidate here
            _pendingSamples.Enqueue(new Sample(t, copy));
            _hasPendingData = true;
        }

        //public void AppendSample(double[] values, double tSec)
        //{
        //    if (values == null) return;
        //    lock (_samples)
        //    {
        //        Sample s = new Sample(tSec, values);
        //        _pendingSamples.Enqueue(s);
        //        _hasPendingData = true;
        //    }
        //}

        // Append high-speed data for a specific channel
        public void AppendHighSpeedSample(int channelIndex, double value, double tSec)
        {
            if (channelIndex < 0 || channelIndex >= _channels.Count) return;

            lock (_highSpeedData)
            {
                if (!_highSpeedData.ContainsKey(channelIndex))
                    _highSpeedData[channelIndex] = new List<(double, double)>();

                _highSpeedData[channelIndex].Add((tSec, value));

                // Keep only recent data (last 5 minutes)
                var cutoff = tSec - 300;
                _highSpeedData[channelIndex].RemoveAll(d => d.T < cutoff);
            }

            Invalidate();
        }

        // Mark a channel as high-speed (renders from separate buffer)
        public void SetHighSpeedChannel(int channelIndex, bool enabled)
        {
            if (enabled)
                _highSpeedChannels.Add(channelIndex);
            else
                _highSpeedChannels.Remove(channelIndex);
        }


        public void AppendSample(double[] values) { AppendSample(values, -1.0); }

        /// <summary>Append a block efficiently. block: [rows, cols]</summary>
        public void AppendBlock(double[,] block, double startTimeSec, double sampleDtSec)
        {
            if (block == null) return; int rows = block.GetLength(0); int cols = block.GetLength(1);
            if (cols != _channels.Count || rows <= 0) return;
            double baseT = startTimeSec >= 0 ? startTimeSec : (_sw.IsRunning ? _sw.Elapsed.TotalSeconds : 0.0);
            int r, c; for (r = 0; r < rows; r++) { double t = baseT + r * sampleDtSec; double[] row = new double[cols]; for (c = 0; c < cols; c++) row[c] = block[r, c]; _samples.Add(new Sample(t, row)); }
            TrimAndUpdateTotals(); if (_lockLive) SnapViewToTail();
        }

        // ============================================================================
        // FIX #2B: Add new method to MultiChannelLiveChart
        // ============================================================================
        // FILE: MultiChannelLiveChart.cs
        // LOCATION: Add this method AFTER line 347 (after the existing AppendBlock method)
        // ============================================================================

        /// <summary>
        /// Append a block of samples using ACTUAL timestamps from the source data.
        /// This preserves exact timing even with gaps, pauses, or variable intervals.
        /// Use this for loading saved tests to ensure review mode matches live mode.
        /// </summary>
        /// <param name="block">Data block [rows, channels]</param>
        /// <param name="times">Actual timestamp for each row (must match row count)</param>
        public void AppendBlockWithTimes(double[,] block, double[] times)
        {
            if (block == null || times == null) return;

            int rows = block.GetLength(0);
            int cols = block.GetLength(1);

            if (cols != _channels.Count || rows <= 0) return;

            if (times.Length != rows)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"⚠️ AppendBlockWithTimes: time array length ({times.Length}) " +
                    $"doesn't match rows ({rows})");
                return;
            }

            // Add each sample with its actual timestamp from the file
            for (int r = 0; r < rows; r++)
            {
                double t = times[r];  // ✅ Use ACTUAL time from source, not recalculated
                double[] row = new double[cols];

                for (int c = 0; c < cols; c++)
                    row[c] = block[r, c];

                _samples.Add(new Sample(t, row));
            }

            TrimAndUpdateTotals();
            if (_lockLive) SnapViewToTail();

            System.Diagnostics.Debug.WriteLine(
                $"✅ AppendBlockWithTimes: Added {rows} samples, " +
                $"time range: {times[0]:F3}s to {times[rows - 1]:F3}s");
        }

        public void SetVisibleDuration(double seconds) { if (seconds < 0.5) seconds = 0.5; if (seconds > 3600) seconds = 3600; _visibleDurationSec = seconds; ClampViewStart(); UpdateScrollFromViewport(); Invalidate(); RaiseLayoutChanged(); }
        public void ScrollTo(double startTimeSec) { _viewStartSec = startTimeSec; ClampViewStart(); _lockLive = false; UpdateScrollFromViewport(); Invalidate(); }
        public void ToggleLive(bool goLive) { _lockLive = goLive; if (_lockLive) SnapViewToTail(); UpdateScrollFromViewport(); Invalidate(); }
        public void ScrollToLive() { ToggleLive(true); }
        public void SetPointCapacityPerChannel(int capacity)
        {
            if (capacity < 500) capacity = 500;
            if (capacity > 2000000) capacity = 2000000;

            _maxPointsPerChannelToKeep = capacity;

            // Removed live mode restriction - keep all data from 0 for scrollback capability
            // The capacity limit will still prevent unbounded growth

            TrimAndUpdateTotals();
        }
        #endregion




        #region Layout
        private RectangleF GetPlotRect()
        {
            int bottomArea = (_hScroll != null && _hScroll.Visible) ? _hScroll.Height : 0;

            float marginLeft = 60f; // left labels
            float rightUI = _rightScaleWidth + _rightLiveWidth + 10f + _btnGoLive.Width - 80f;
            float marginRight = rightUI;
            float marginTop = 8f;
            float marginBottom = 26f + bottomArea;

            float availW = Math.Max(10f, Width - marginLeft - marginRight);
            float availH = Math.Max(10f, Height - marginTop - marginBottom);

            float x = marginLeft;
            float w = availW;

            // Fix only for 120-sec screen: 960 px => 8 px/sec => 2 px/sample @ 4 Hz
            if (_fix120SecPlotWidth && Math.Abs(_visibleDurationSec - 120.0) < 0.001)
            {
                float desiredW = _plotWidth120SecPx;

                if (availW > desiredW)
                {
                    w = desiredW;
                    x = _centerFixedPlot ? (marginLeft + (availW - w) / 2f) : marginLeft;
                }
                else
                {
                    w = availW;
                    x = marginLeft;
                }
            }

            return new RectangleF(x, marginTop, w, availH);
        }


        //private RectangleF GetPlotRect()
        //{
        //    int bottomArea = (_hScroll != null && _hScroll.Visible) ? _hScroll.Height : 0;
        //    float marginLeft = 60f;         // left labels
        //    float rightUI = _rightScaleWidth + _rightLiveWidth + 10f + _btnGoLive.Width - 80f;
        //    float marginRight = rightUI;    // reserve space for right scale + live text + button
        //    float marginTop = 8f; float marginBottom = 26f + bottomArea;
        //    return new RectangleF(marginLeft, marginTop, Math.Max(10, Width - marginLeft - marginRight), Math.Max(10, Height - marginTop - marginBottom));
        //}

        // 25/01/2026 change by pratik
        // --- Fixed plot width for 120-sec window ---
        private bool _fix120SecPlotWidth = true;
        private float _plotWidth120SecPx = 960f;   // 120 sec * 8 px/sec  => 2 px/sample @ 4 samples/sec
        private bool _centerFixedPlot = false;


        private void OnResize(object sender, EventArgs e)
        {
            int padding = 6; _btnGoLive.Top = Height - (_hScroll.Height + _btnGoLive.Height + padding); _btnGoLive.Left = Width - (_btnGoLive.Width + padding); Invalidate();
            RaiseLayoutChanged();
        }
        #endregion

        #region Scrolling helpers
        private void OnScrollChanged(object sender, ScrollEventArgs e)
        {
            if (_isUpdatingScroll) return;                      // ignore internal updates
            if (_hScroll.Maximum <= _hScroll.LargeChange) return;

            double frac = (double)_hScroll.Value / (double)(_hScroll.Maximum - _hScroll.LargeChange);
            double start = GetMinTime();
            double end = GetMaxTime();
            double span = Math.Max(0.0, end - start - _visibleDurationSec);

            _viewStartSec = start + frac * span;
            _lockLive = false;                                  // user scrolled -> exit live
            Invalidate();
        }

        private void UpdateScrollFromViewport()
        {
            double start = GetMinTime();
            double end = GetMaxTime();
            double span = Math.Max(0.0, end - start - _visibleDurationSec);

            if (span <= 0.0)
            {
                if (_hScroll != null)
                {
                    _hScroll.Enabled = false;
                    try { _hScroll.Value = 0; } catch { }
                }
                return;
            }

            if (_hScroll == null) return;

            _hScroll.Enabled = true;
            int max = 1000;
            _hScroll.Maximum = max;
            _hScroll.LargeChange = 100;

            double frac = (_viewStartSec - start) / span;
            if (frac < 0.0) frac = 0.0;
            if (frac > 1.0) frac = 1.0;

            int val = (int)Math.Round(frac * (max - _hScroll.LargeChange));
            if (val < _hScroll.Minimum) val = _hScroll.Minimum;
            if (val > _hScroll.Maximum - _hScroll.LargeChange) val = _hScroll.Maximum - _hScroll.LargeChange;

            _isUpdatingScroll = true;             // <— prevent OnScrollChanged from firing logic
            try { _hScroll.Value = val; } catch { }
            _isUpdatingScroll = false;
        }

        private void SnapViewToTail() { double minT = GetMinTime(), maxT = GetMaxTime(); _viewStartSec = Math.Max(minT, maxT - _visibleDurationSec); UpdateScrollFromViewport(); }
        private void ClampViewStart() { double minT = GetMinTime(), maxT = GetMaxTime(); if (_viewStartSec < minT) _viewStartSec = minT; if (_viewStartSec > maxT - _visibleDurationSec) _viewStartSec = Math.Max(minT, maxT - _visibleDurationSec); }
        private double GetMinTime() { return _samples.Count == 0 ? 0.0 : _samples[0].T; }
        private double GetMaxTime() { return _samples.Count == 0 ? 0.0 : _samples[_samples.Count - 1].T; }
        #endregion


        //AddMarker Lines Design Code
        //Start This code For Graph Test Start Left Side Commit on 28/11/2025 old Code


        //private void DrawMarkers(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        //{
        //    if (_markers.Count == 0) return;

        //    // Current X-axis window
        //    double startT = _viewStartSec;
        //    double endT = _viewStartSec + _visibleDurationSec;

        //    // Draw once across full plot height (all lanes)
        //    foreach (var m in _markers)
        //    {
        //        if (m.T < startT || m.T > endT) continue;

        //        float x = (float)(plot.Left + (m.T - startT) / _visibleDurationSec * plot.Width);

        //        using (var p = new Pen(m.Color, m.Width) { DashStyle = m.Dash, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
        //        {
        //            g.DrawLine(p, x, plot.Top, x, plot.Bottom);
        //        }

        //        if (!string.IsNullOrEmpty(m.Label))
        //        {
        //            using (var f = new Font("Segoe UI", Math.Max(8f, Font.Size - 1f), FontStyle.Bold))
        //            using (var b = new SolidBrush(m.Color))
        //            {
        //                // draw label slightly inside the plot, top aligned, centered on the line
        //                string text = m.Label;
        //                SizeF sz = g.MeasureString(text, f);

        //                //Start code for show label in left side "FS, FD, DC, BC etc"
        //                float tx = x + 4f;
        //                float ty = plot.Top + 2f;
        //                if (tx + sz.Width > plot.Left)
        //                    tx = x - sz.Width - 4f;
        //                //End code for show label in left side "FS, FD, DC, BC etc"

        //                g.DrawString(text, f, b, tx, ty);
        //            }
        //        }
        //    }
        //}


        private void DrawMarkers(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            if (_markers.Count == 0) return;

            // Current X-axis window
            double startT = _viewStartSec;
            double endT = _viewStartSec + _visibleDurationSec;

            // Draw once across full plot height (all lanes)
            foreach (var m in _markers)
            {
                if (m.T < startT || m.T > endT) continue;

                float x = (float)(plot.Left + (m.T - startT) / _visibleDurationSec * plot.Width);

                using (var p = new Pen(m.Color, m.Width) { DashStyle = m.Dash, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                {
                    g.DrawLine(p, x, plot.Top, x, plot.Bottom);
                }

                if (!string.IsNullOrEmpty(m.Label))
                {
                    using (var f = new Font("Segoe UI", Math.Max(8f, Font.Size - 1f), FontStyle.Bold))
                    using (var b = new SolidBrush(m.Color))
                    {
                        // draw label slightly inside the plot, top aligned, centered on the line
                        string text = m.Label;
                        SizeF sz = g.MeasureString(text, f);

                        //Start code for show label in left side "FS, FD, DC, BC etc"
                        float tx = x + 4f;
                        float ty = plot.Top + 2f;
                        if (tx + sz.Width > plot.Left)
                            tx = x - sz.Width - 4f;
                        //End code for show label in left side "FS, FD, DC, BC etc"

                        g.DrawString(text, f, b, tx, ty);
                    }
                }
            }
        }

        //End This code For Graph Test Start Left Side Commit on 28/11/2025 old Code


        private void EnsureBackgroundCache(RectangleF plot, float laneHeight, float laneGap)
        {
            if (Width <= 2 || Height <= 2) return;

            var size = new Size(Width, Height);

            if (_bgCache == null || _bgCacheSize != size || _bgCacheDirty)
            {
                _bgCache?.Dispose();
                _bgCache = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                _bgCacheSize = size;
                _bgCacheDirty = false;

                using (var g = Graphics.FromImage(_bgCache))
                {
                    // background
                    g.Clear(BackColor);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;


                }
            }
        }




        #region Rendering
        protected override void OnPaint(PaintEventArgs e)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                base.OnPaint(e);

                RectangleF plot = GetPlotRect();
                if (plot.Width < 30 || plot.Height < 30)
                {
                    DrawCenteredText(e.Graphics, "Resize window", Font, Brushes.Gray, ClientRectangle);
                    return;
                }

                int n = _channels.Count;
                if (n == 0)
                {
                    DrawCenteredText(e.Graphics, "No channels configured", Font, Brushes.Gray, ClientRectangle);
                    return;
                }

                float laneGap = 6f;
                float laneHeight = (plot.Height - laneGap * (n - 1)) / n;
                if (laneHeight < 12f) laneHeight = 12f;

                // =========================
                // 1) Background cache (fast fill)
                // =========================
                EnsureBackgroundCache(plot, laneHeight, laneGap);

                if (_bgCache != null)
                    e.Graphics.DrawImageUnscaled(_bgCache, 0, 0);
                else
                    e.Graphics.Clear(BackColor);

                // =========================
                // 2) STATIC-ish overlays (grid, axes, lane advisory/alerts/markers)
                // =========================
                // Keep these FAST
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                DrawGridAndAxes(e.Graphics, plot, laneHeight, laneGap);
                DrawLaneAdvisories(e.Graphics, plot, laneHeight, laneGap);
                DrawLaneAlerts(e.Graphics, plot, laneHeight, laneGap);
                DrawMarkers(e.Graphics, plot, laneHeight, laneGap);

                // =========================
                // 3) Signals (traces)
                // =========================
                // AntiAlias only for traces
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                if (_samples.Count >= 2)
                {
                    double startT = _viewStartSec;
                    double endT = _viewStartSec + _visibleDurationSec;

                    int i0 = FindFirstIndexAtOrAfter(startT);
                    int i1 = FindLastIndexAtOrBefore(endT);

                    if (i0 < 0) i0 = 0;
                    if (i1 < 0) i1 = 0;
                    if (i1 < i0) i1 = i0;

                    if (i1 - i0 > 0)
                    {
                        int widthPx = Math.Max(1, (int)plot.Width);
                        int span = i1 - i0 + 1;

                        // Use buckets for live OR if span is much larger than pixels
                        bool useBuckets = UsePixelBuckets && (_lockLive || span > widthPx * 2);

                        if (!useBuckets)
                        {
                            int step = Math.Max(1, span / widthPx);

                            for (int c = 0; c < n; c++)
                            {
                                Pen pen = _seriesPens[c];
                                List<PointF> pts = new List<PointF>(widthPx + 2);

                                for (int i = i0; i <= i1; i += step)
                                {
                                    Sample s = _samples[i];
                                    double t = s.T;
                                    double v = s.Values[c];

                                    float x = (float)(plot.Left + (t - startT) / _visibleDurationSec * plot.Width);
                                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                                    float y = MapValueToLaneY(v, _channels[c], laneTop, laneHeight);

                                    pts.Add(new PointF(x, y));
                                }

                                if (pts.Count >= 2)
                                    e.Graphics.DrawLines(pen, pts.ToArray());
                            }
                        }
                        else
                        {
                            // ========= Bucket rendering =========
                            EnsureBucketBuffers(widthPx);
                            EnsurePaintBuffers(n);

                            double secPerPixel = _visibleDurationSec / widthPx;
                            int sampleCount = _samples.Count;

                            int j = i0;
                            int curStart = j;

                            for (int pxCol = 0; pxCol < widthPx; pxCol++)
                            {
                                double t1 = startT + (pxCol + 1) * secPerPixel;
                                while (j < sampleCount && _samples[j].T <= t1) j++;

                                _bucketStartIdx[pxCol] = curStart;
                                _bucketEndIdx[pxCol] = j - 1;
                                curStart = j;
                            }

                            for (int c = 0; c < n; c++) _hasPrevArr[c] = false;

                            bool drawEnvelope = (!_lockLive) || DrawEnvelopeInLive;

                            for (int pxCol = 0; pxCol < widthPx; pxCol++)
                            {
                                int j0b = _bucketStartIdx[pxCol];
                                int j1b = _bucketEndIdx[pxCol];

                                if (j1b < j0b || j0b < 0 || j1b < 0 || j0b >= sampleCount || j1b >= sampleCount)
                                {
                                    // IMPORTANT: break trace so next valid column doesn't connect diagonally
                                    for (int c = 0; c < n; c++) _hasPrevArr[c] = false;
                                    continue;
                                }

                                for (int c = 0; c < n; c++)
                                {
                                    _vMinArr[c] = double.PositiveInfinity;
                                    _vMaxArr[c] = double.NegativeInfinity;
                                    _vSumArr[c] = 0.0;
                                }

                                int cnt = 0;
                                for (int k = j0b; k <= j1b; k++)
                                {
                                    double[] row = _samples[k].Values;
                                    if (row == null) continue;

                                    for (int c = 0; c < n; c++)
                                    {
                                        double v = row[c];
                                        if (v < _vMinArr[c]) _vMinArr[c] = v;
                                        if (v > _vMaxArr[c]) _vMaxArr[c] = v;
                                        _vSumArr[c] += v;
                                    }
                                    cnt++;
                                }

                                if (cnt <= 0)
                                {
                                    // IMPORTANT: break trace so next valid column doesn't connect diagonally
                                    for (int c = 0; c < n; c++) _hasPrevArr[c] = false;
                                    continue;
                                }

                                float x = (float)Math.Round(plot.Left + pxCol) + 0.5f;

                                for (int c = 0; c < n; c++)
                                {
                                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                                    Pen pen = _seriesPens[c];

                                    if (drawEnvelope)
                                    {
                                        float yMin = MapValueToLaneY(_vMinArr[c], _channels[c], laneTop, laneHeight);
                                        float yMax = MapValueToLaneY(_vMaxArr[c], _channels[c], laneTop, laneHeight);
                                        e.Graphics.DrawLine(pen, x, yMin, x, yMax);
                                    }

                                    //double vTrace = _samples[j1b].Values[c]; // stable trace
                                    //float yTrace = MapValueToLaneY(vTrace, _channels[c], laneTop, laneHeight);
                                    //PointF cur = new PointF(x, yTrace);

                                    //if (_hasPrevArr[c])
                                    //    e.Graphics.DrawLine(pen, _prevArr[c], cur);

                                    //_prevArr[c] = cur;
                                    //_hasPrevArr[c] = true;

                                    double vTrace;

                                    // Check if this is a high-speed channel - use high-speed buffer instead
                                    if (_highSpeedChannels.Contains(c))
                                    {
                                        // Calculate time for this pixel column
                                        double tStart = _viewStartSec;
                                        double tEnd = _viewStartSec + _visibleDurationSec;
                                        double timeAtPixel = tStart + (pxCol / plot.Width) * (tEnd - tStart);

                                        // Find value from high-speed buffer at this time
                                        vTrace = GetHighSpeedValue(c, timeAtPixel);
                                        if (double.IsNaN(vTrace))
                                            vTrace = _samples[j1b].Values[c]; // Fallback to regular sample
                                    }
                                    else
                                    {
                                        vTrace = _samples[j1b].Values[c]; // stable trace
                                    }

                                    // Check if this channel should be mirrored
                                    bool shouldMirror = _mirroredChannels.Contains(_channels[c].Name);
                                    if (shouldMirror)
                                    {
                                        // Mirror plotting: draw both positive and negative
                                        double absValue = Math.Abs(vTrace);

                                        float yPos = MapValueToLaneY(absValue, _channels[c], laneTop, laneHeight);
                                        float yNeg = MapValueToLaneY(-absValue, _channels[c], laneTop, laneHeight);

                                        // Validate
                                        if (float.IsNaN(yPos) || float.IsInfinity(yPos) || Math.Abs(yPos) > 100000 ||
                                            float.IsNaN(yNeg) || float.IsInfinity(yNeg) || Math.Abs(yNeg) > 100000)
                                        {
                                            _hasPrevArr[c] = false;
                                            continue;
                                        }

                                        PointF curPos = new PointF(x, yPos);
                                        PointF curNeg = new PointF(x, yNeg);

                                        if (_hasPrevArr[c] && !float.IsNaN(_prevArr[c].Y))
                                        {
                                            // Previous point was stored as POSITIVE
                                            // Calculate previous NEGATIVE by mirroring around lane center
                                            float laneCenter = laneTop + laneHeight / 2f;
                                            float prevPosDistFromCenter = _prevArr[c].Y - laneCenter;
                                            float prevNegY = laneCenter - prevPosDistFromCenter; // Mirror it

                                            PointF prevNeg = new PointF(_prevArr[c].X, prevNegY);

                                            // Draw positive trace
                                            // e.Graphics.DrawLine(pen, _prevArr[c], curPos);
                                            // Draw negative mirror trace
                                            // e.Graphics.DrawLine(pen, prevNeg, curNeg);

                                            // 🔹 NEW: Draw vertical connector between current positive and negative
                                            if (pxCol % 2 == 0)
                                            {
                                                e.Graphics.DrawLine(pen, curPos, curNeg);
                                            }
                                        }

                                        _prevArr[c] = curPos;
                                        _hasPrevArr[c] = true;
                                    }

                                    else
                                    {
                                        // Normal single-line plotting
                                        float yTrace = MapValueToLaneY(vTrace, _channels[c], laneTop, laneHeight);

                                        if (float.IsNaN(yTrace) || float.IsInfinity(yTrace) || Math.Abs(yTrace) > 100000)
                                        {
                                            _hasPrevArr[c] = false;
                                            continue;
                                        }

                                        PointF cur = new PointF(x, yTrace);

                                        if (_hasPrevArr[c] && !float.IsNaN(_prevArr[c].Y) && !float.IsInfinity(_prevArr[c].Y))
                                            e.Graphics.DrawLine(pen, _prevArr[c], cur);

                                        _prevArr[c] = cur;
                                        _hasPrevArr[c] = true;
                                    }

                                }
                            }
                        }
                    }
                }

                // =========================
                // 4) UI overlays that you were missing
                // =========================
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                // Full width lane separators (right side area too)
                DrawFullWidthLaneSeparators(e.Graphics, plot, laneHeight, laneGap);

                // Left-side channel labels
                using (Font f = new Font(Font.FontFamily, Math.Max(9f, Font.Size - 1f), FontStyle.Bold))
                {
                    for (int c = 0; c < n; c++)
                    {
                        float laneTop = plot.Top + c * (laneHeight + laneGap);
                        RectangleF r = new RectangleF(0, laneTop, plot.Left - 15, laneHeight);
                        TextRenderer.DrawText(e.Graphics, _channels[c].Name, f,
                            Rectangle.Round(r),
                            _channels[c].Color,
                            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                }

                // Right-side min/max + live value
                DrawRightScalesAndLive(e.Graphics, plot, laneHeight, laneGap);

                // Bottom X labels
                DrawXAxisLabels(e.Graphics, plot);


                // Live red line
                if (_lockLive)
                {
                    using (Pen p = new Pen(Color.FromArgb(230, 80, 60)))
                    {
                        float x = plot.Right - 1;
                        e.Graphics.DrawLine(p, x, plot.Top, x, plot.Bottom);
                    }
                }

                // Hover crosshair + markers (keep your existing logic)
                Point clientPos = PointToClient(System.Windows.Forms.Cursor.Position);
                if (plot.Contains(clientPos) && !_lockLive)
                {
                    using (Pen crossPen = new Pen(Color.FromArgb(120, Color.Gray), 1f))
                    {
                        e.Graphics.DrawLine(crossPen, clientPos.X, plot.Top, clientPos.X, plot.Bottom);
                    }

                    if (_samples.Count > 1)
                    {
                        double tHover = PixelToTime(clientPos.X);
                        int nearestIdx = FindNearestSampleIndex(tHover);

                        if (nearestIdx >= 0 && nearestIdx < _samples.Count)
                        {
                            Sample s = _samples[nearestIdx];
                            for (int c = 0; c < _channels.Count; c++)
                            {
                                if (s.Values == null || c >= s.Values.Length) continue;
                                double val = s.Values[c];
                                float laneTop = plot.Top + c * (laneHeight + laneGap);
                                float y = MapValueToLaneY(val, _channels[c], laneTop, laneHeight);

                                using (Brush b = new SolidBrush(_channels[c].Color))
                                    e.Graphics.FillEllipse(b, clientPos.X - 3, y - 3, 6, 6);
                            }
                        }
                    }
                }

                if (_isHovering && !double.IsNaN(_hoverTimeSec))
                {
                    TimeSpan ts = TimeSpan.FromSeconds(_hoverTimeSec);
                    string timeText = (ts.TotalHours >= 1) ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");

                    using (Font timeFont = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                    using (Brush textBrush = new SolidBrush(Color.Black))
                    {
                        SizeF textSize = e.Graphics.MeasureString(timeText, timeFont);
                        float rightEdge = this.ClientSize.Width;
                        float x = rightEdge - textSize.Width - 8f;
                        float y = plot.Top + 4f;

                        e.Graphics.FillRectangle(bgBrush, x - 4, y - 2, textSize.Width + 8, textSize.Height + 4);
                        e.Graphics.DrawString(timeText, timeFont, textBrush, x, y);
                    }
                }
            }
            finally
            {
                sw.Stop();
                // PerfTrace.EveryMs("PAINT", 1000, () => $"OnPaint={sw.ElapsedMilliseconds}ms size={Width}x{Height} pending={_pendingSamples.Count}");
            }
        }

        private double GetHighSpeedValue(int channelIndex, double tSec)
        {
            lock (_highSpeedData)
            {
                if (!_highSpeedData.ContainsKey(channelIndex))
                    return double.NaN;

                var data = _highSpeedData[channelIndex];
                if (data.Count == 0)
                    return double.NaN;

                // Find the two samples surrounding this time
                var before = data.Where(d => d.T <= tSec).OrderByDescending(d => d.T).FirstOrDefault();
                var after = data.Where(d => d.T > tSec).OrderBy(d => d.T).FirstOrDefault();

                // If we have exact match, return it
                var exact = data.FirstOrDefault(d => Math.Abs(d.T - tSec) < 0.001);
                if (exact.Value != 0 || exact.T != 0)
                    return exact.Value;

                // If we have surrounding samples, interpolate
                if (before.Value != 0 && after.Value != 0)
                {
                    double dt = after.T - before.T;
                    if (dt > 0 && dt < 0.1) // Only interpolate if samples are close
                    {
                        double ratio = (tSec - before.T) / dt;
                        return before.Value + (after.Value - before.Value) * ratio;
                    }
                }

                // Just use closest if within tolerance
                var closest = data.OrderBy(d => Math.Abs(d.T - tSec)).FirstOrDefault();
                if (Math.Abs(closest.T - tSec) < 0.05)
                    return closest.Value;

                return double.NaN;
            }
        }


        private void DrawFullWidthLaneSeparators(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            int n = _channels.Count;
            if (n <= 0) return;

            // draw across the entire control width (slightly inset to avoid edge clipping)
            float fullLeftX = 2f;
            float fullRightX = this.ClientSize.Width - 2f;

            using (Pen sepPen = new Pen(Color.FromArgb(130, 160, 160, 160), 1f))
            {
                for (int c = 0; c < n; c++)
                {
                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                    float laneBottom = laneTop + laneHeight;

                    // top border for the first lane
                    if (c == 0)
                    {
                        float yTop = (float)Math.Round(laneTop) + 0.5f;
                        g.DrawLine(sepPen, fullLeftX, yTop, fullRightX, yTop);
                    }

                    // bottom border for every lane
                    float yBottom = (float)Math.Round(laneBottom) + 0.5f;
                    g.DrawLine(sepPen, fullLeftX, yBottom, fullRightX, yBottom);
                }
            }
        }


        private void DrawLaneAlerts(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            if (_laneAlerts.Count == 0) return;

            using (Font msgFont = new Font("Segoe UI", Math.Max(10f, Font.Size + 1f), FontStyle.Bold))
            {
                // format for perfect centering
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                foreach (KeyValuePair<int, LaneAlert> kv in _laneAlerts)
                {
                    int c = kv.Key;
                    if (c < 0 || c >= _channels.Count) continue;

                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                    RectangleF r = new RectangleF(plot.Left, laneTop, plot.Width, laneHeight);

                    // translucent lane overlay (light red)
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(60, kv.Value.Back)))
                        g.FillRectangle(b, r);

                    // centered warning text
                    string text = string.IsNullOrEmpty(kv.Value.Message) ? "Sensor Error" : kv.Value.Message;

                    // dark-red-ish readable text
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(220, 60, 0, 0)))
                    {
                        // draw a subtle shadow for readability
                        RectangleF rShadow = new RectangleF(r.X + 1, r.Y + 1, r.Width, r.Height);
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                            g.DrawString("⚠ " + text, msgFont, sb, rShadow, sf);

                        g.DrawString("⚠ " + text, msgFont, tb, r, sf);
                    }
                }
            }
        }


        private void DrawLaneAdvisories(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            if (_laneAdvisories.Count == 0) return;

            using (Font msgFont = new Font("Segoe UI", Math.Max(9f, Font.Size), FontStyle.Bold))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                foreach (KeyValuePair<int, LaneAdvisory> kv in _laneAdvisories)
                {
                    int c = kv.Key;
                    if (c < 0 || c >= _channels.Count) continue;

                    // 🔑 skip advisories if this lane currently has an alert
                    if (_laneAlerts.ContainsKey(c)) continue;

                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                    RectangleF r = new RectangleF(plot.Left, laneTop, plot.Width, laneHeight);

                    using (SolidBrush b = new SolidBrush(Color.FromArgb(60, kv.Value.Back)))
                        g.FillRectangle(b, r);

                    string text = string.IsNullOrEmpty(kv.Value.Message) ? "Near scale limit" : kv.Value.Message;
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(220, 80, 60, 0))) // brownish for readability
                    {
                        RectangleF rShadow = new RectangleF(r.X + 1, r.Y + 1, r.Width, r.Height);
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                            g.DrawString("ℹ " + text, msgFont, sb, rShadow, sf);

                        g.DrawString("ℹ " + text, msgFont, tb, r, sf);
                    }


                }
            }
        }



        private void DrawGridAndAxes(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            g.DrawRectangle(_axisPen, plot.Left, plot.Top, plot.Width, plot.Height);
            int c; for (c = 0; c < _channels.Count; c++)
            {
                float laneTop = plot.Top + c * (laneHeight + laneGap); float laneBottom = laneTop + laneHeight;
                // lane separator
                g.DrawLine(_axisPen, plot.Left, laneBottom, plot.Right, laneBottom);
                // nominal zero line if within scale
                if (_channels[c].ScaleMin < 0 && _channels[c].ScaleMax > 0)
                { float y0 = MapValueToLaneY(0.0, _channels[c], laneTop, laneHeight); g.DrawLine(_gridPen, plot.Left, y0, plot.Right, y0); }


                if (_laneDivisions > 1)
                {
                    float step = laneHeight / _laneDivisions;
                    for (int d = 1; d < _laneDivisions; d++)
                    {
                        float y1 = laneTop + d * step;
                        y1 = (float)Math.Round(y1) + 0.5f;
                        g.DrawLine(_hGridPen, plot.Left, y1, plot.Right, y1);
                    }
                }

            }

        }


        //Show Device Status Connected & Disconnected and Saved Graph Mouse Houver Value show in Graph change on 12/11/2025
        private void DrawRightScalesAndLive(Graphics g, RectangleF plot, float laneHeight, float laneGap)
        {
            float scaleLeft = plot.Right + 6f;                  // start of min/max area
            float scaleRight = scaleLeft + _rightScaleWidth;    // end of min/max area
            float liveLeft = scaleRight + 6f;                   // start of name/value/unit
            float rightMaxX = this.ClientSize.Width - 4f;       // rightmost line

            using (Font tickFont = new Font("Segoe UI", Math.Max(9.5f, Font.Size - 2f), FontStyle.Bold))
            using (Font nameFont = new Font("Segoe UI", Math.Max(9f, Font.Size + 0.5f), FontStyle.Bold))
            using (Font valueFont = new Font("Segoe UI", Math.Max(14.5f, Font.Size + 2.5f), FontStyle.Bold))
            using (Font unitFont = new Font("Segoe UI", Math.Max(9f, Font.Size + 1f), FontStyle.Regular))
            using (Pen sepPen = new Pen(Color.FromArgb(130, 160, 160, 160), 1f))
            {
                StringFormat nearTop = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                StringFormat nearBot = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

                for (int c = 0; c < _channels.Count; c++)
                {
                    Channel ch = _channels[c];

                    float laneTop = plot.Top + c * (laneHeight + laneGap);
                    float laneBottom = laneTop + laneHeight;
                    float laneMid = laneTop + laneHeight * 0.5f;

                    // ---- horizontal grid lines ----
                    //if (_laneDivisions > 1)
                    //{
                    //    float step = laneHeight / _laneDivisions;
                    //    for (int d = 1; d < _laneDivisions; d++)
                    //    {
                    //        float y1 = laneTop + d * step;
                    //        y1 = (float)Math.Round(y1) + 0.5f;
                    //        g.DrawLine(_hGridPen, plot.Left, y1, plot.Right, y1);
                    //    }
                    //}

                    // thin right border
                    g.DrawLine(_axisPen, plot.Right, laneTop, plot.Right, laneBottom);

                    // ---- min/max text ----
                    string maxTxt = FormatTick(ch.ScaleMax);
                    g.DrawString(maxTxt, tickFont, _axisTextBrush, scaleLeft + 2f, laneTop, nearTop);

                    string minTxt = FormatTick(ch.ScaleMin);
                    SizeF szMin = g.MeasureString(minTxt, tickFont);
                    g.DrawString(minTxt, tickFont, _axisTextBrush, scaleLeft + 2f, laneBottom - szMin.Height, nearBot);

                    // ---- stacked: Name / Value / Unit ----
                    string nameText = string.IsNullOrEmpty(ch.Name) ? "" : ch.Name;
                    string unitText = string.IsNullOrEmpty(ch.Unit) ? "" : ch.Unit;

                    double displayValue = double.NaN;

                    // ✅ Use hover value if mouse is hovering
                    if (_isHovering && !double.IsNaN(_hoverTimeSec))
                    {
                        int idx = FindNearestSampleIndex(_hoverTimeSec);
                        if (idx >= 0 && idx < _samples.Count)
                        {
                            var s = _samples[idx];
                            if (s.Values != null && c < s.Values.Length)
                                displayValue = s.Values[c];
                        }
                    }
                    else
                    {
                        // otherwise, show latest (live) value
                        TryGetLatestValue(c, out displayValue);
                    }

                    // --- Special handling for Device Status ---
                    string valueText = "";
                    if (string.Equals(ch.Name, "Device Status", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!double.IsNaN(displayValue))
                        {
                            if (displayValue == 1)
                                valueText = "Connected";
                            else if (displayValue == 0)
                                valueText = "Disconnected";
                            else
                                valueText = FormatTick(displayValue);
                        }

                        if (string.IsNullOrEmpty(nameText)) nameText = ch.Name ?? "Device";
                        if (string.IsNullOrEmpty(unitText)) unitText = "";
                    }
                    else
                    {
                        if (!double.IsNaN(displayValue))
                            valueText = FormatTick(displayValue);
                    }

                    // measure
                    SizeF hName = (nameText.Length > 0) ? g.MeasureString(nameText, nameFont) : SizeF.Empty;
                    SizeF hValue = (valueText.Length > 0) ? g.MeasureString(valueText, valueFont) : SizeF.Empty;
                    SizeF hUnit = (unitText.Length > 0) ? g.MeasureString(unitText, unitFont) : SizeF.Empty;

                    float gapNV = (nameText.Length > 0 && valueText.Length > 0) ? 2f : 0f;
                    float gapVU = (valueText.Length > 0 && unitText.Length > 0) ? 2f : 0f;
                    float totalH = hName.Height + gapNV + hValue.Height + gapVU + hUnit.Height;
                    if (totalH <= 0f) totalH = 0f;

                    float yStart = laneMid - totalH * 0.5f;
                    if (yStart < laneTop + 1f) yStart = laneTop + 1f;
                    float y = yStart;

                    // draw name
                    if (nameText.Length > 0)
                    {
                        using (Brush nameBrush = new SolidBrush(ch.Color))
                            g.DrawString(nameText, nameFont, nameBrush, liveLeft, y, nearTop);
                        y += hName.Height + gapNV;
                    }

                    // draw value (bold)
                    if (valueText.Length > 0)
                    {
                        using (Brush valBrush = new SolidBrush(ch.Color))
                            g.DrawString(valueText, valueFont, valBrush, liveLeft, y, nearTop);
                        y += hValue.Height + gapVU;
                    }

                    // draw unit
                    if (unitText.Length > 0)
                    {
                        g.DrawString(unitText, unitFont, _axisTextBrush, liveLeft, y, nearTop);
                    }

                    // ---- lane separator line ----
                    if (c < _channels.Count - 1)
                    {
                        float ySep = (float)Math.Round(laneBottom) + 0.5f;
                        g.DrawLine(sepPen, plot.Left, ySep, rightMaxX, ySep);
                    }
                }

                //This Code For Remove Timer show on Graph in Mouse Hover "t = 75.00s"

                // 🕓 Optional: show current hover time
                //if (_isHovering && !double.IsNaN(_hoverTimeSec))
                //{
                //    using (Font f = new Font("Segoe UI", 9f, FontStyle.Italic))
                //    using (Brush b = new SolidBrush(Color.DimGray))
                //    {
                //        string tLabel = $"t = {_hoverTimeSec:F2}s";
                //        SizeF sz = g.MeasureString(tLabel, f);
                //        g.DrawString(tLabel, f, b, plot.Right - sz.Width, plot.Top + 4);
                //    }
                //}
            }
        }



        // =================== emg ble ==============

        /// <summary>
        /// Draws a zero reference line for symmetric channels (e.g., BLE EMG with -2000 to +2000 range)
        /// </summary>
        private void DrawZeroReferenceLine(Graphics g, int laneIdx, RectangleF laneRect, Channel ch)
        {
            // Only draw zero line if channel scale includes zero (symmetric scale)
            if (ch.ScaleMin >= 0 || ch.ScaleMax <= 0) return; // Skip if not symmetric

            // Calculate Y position of zero value
            float yZero = laneRect.Top + laneRect.Height * (float)((ch.ScaleMax - 0) / (ch.ScaleMax - ch.ScaleMin));

            // Draw zero line
            using (Pen zeroPen = new Pen(Color.FromArgb(100, Color.Gray), 1.5f))
            {
                zeroPen.DashStyle = DashStyle.Dash;
                g.DrawLine(zeroPen, laneRect.Left, yZero, laneRect.Right, yZero);
            }

            // Optional: Draw "0" label
            using (Font labelFont = new Font("Segoe UI", 7f))
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(150, Color.Gray)))
            {
                string zeroLabel = "0";
                SizeF labelSize = g.MeasureString(zeroLabel, labelFont);
                g.DrawString(zeroLabel, labelFont, labelBrush,
                    laneRect.Left + 2, yZero - labelSize.Height / 2);
            }
        }

        //============================================

        public Rectangle GetScaleMaxOverlayBounds(int laneIndex)
        {
            RectangleF plot = GetPlotRect();
            int n = _channels.Count;
            if (laneIndex < 0 || laneIndex >= n) return Rectangle.Empty;

            float laneGap = 6f;
            float laneHeight = (plot.Height - laneGap * (n - 1)) / Math.Max(1, n);
            if (laneHeight < 12f) laneHeight = 12f;

            float laneTop = plot.Top + laneIndex * (laneHeight + laneGap);
            float scaleLeft = plot.Right + 6f; // same as DrawRightScalesAndLive
            int x = (int)scaleLeft;
            int y = (int)laneTop;
            int w = (int)_rightScaleWidth;     // same field you use for max/min label width
            int h = (int)Math.Min(22f, laneHeight);
            return new Rectangle(x, y, w, h);
        }



        private string FormatTick(double value)
        {
            double abs = Math.Abs(value);
            if (abs >= 1000) return value.ToString("0");
            if (abs >= 100) return value.ToString("0");
            if (abs >= 10) return value.ToString("0");
            if (abs >= 1) return value.ToString("0");
            return value.ToString("0");
        }
        private string FormatLive(double value, string unit)
        {
            string s; double abs = Math.Abs(value);
            if (abs >= 1000) s = value.ToString("0");
            else if (abs >= 100) s = value.ToString("0");
            else if (abs >= 10) s = value.ToString("0.0");
            else if (abs >= 1) s = value.ToString("0.00");
            else s = value.ToString("0.000");
            return string.IsNullOrEmpty(unit) ? s : (s + " " + unit);
        }


        /// <summary>
        /// Add a vertical marker at time 'tSec' in seconds. Optional label shown at top of plot.
        /// </summary>
        public void AddMarker(double tSec, string label = "", Color? color = null,
                              float width = 0.7f, //0.4f for event marker line width //
                              System.Drawing.Drawing2D.DashStyle dash = System.Drawing.Drawing2D.DashStyle.Solid)
        {
            if (double.IsNaN(tSec) || double.IsInfinity(tSec)) return;
            _markers.Add(new Marker(tSec, label ?? "", color ?? Color.Black, width, dash));
            Invalidate();
        }

        //Start Code For Add Mark Selected Location Work Only Saved Test on 15/10/2025
        public event Action<double> ChartClicked;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            double tSec = PixelToTime(e.X);
            ChartClicked?.Invoke(tSec);
        }

        //Start This code For Graph Test Start Left Side Commit on 28/11/2025 old Code

        public double PixelToTime(int x)
        {
            if (_visibleDurationSec <= 0) return _viewStartSec;

            RectangleF plot = GetPlotRect();

            double fraction = (x - plot.Left) / plot.Width;
            fraction = Math.Max(0, Math.Min(1, fraction));

            return _viewStartSec + fraction * _visibleDurationSec;
        }

        //End This code For Graph Test Start Left Side Commit on 28/11/2025 old Code

        public void RemoveMarkerByLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return;

            // Remove all markers with the same label
            _markers.RemoveAll(m => m.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

            Invalidate();
        }

        public IReadOnlyList<string> GetChannelNames() => _channels.Select(c => c.Name).ToList();

        /// <summary>Remove all markers.</summary>
        public void ClearMarkers()
        {
            _markers.Clear();
            Invalidate();
        }

        public void RemoveMarker(string label)
        {
            var marker = _markers.FirstOrDefault(m => m.Label == label);
            if (marker != null)
            {
                _markers.Remove(marker);
                Invalidate();
            }
        }

        /// <summary>Remove the most recent marker (if any).</summary>
        public bool RemoveLastMarker()
        {
            if (_markers.Count == 0) return false;
            _markers.RemoveAt(_markers.Count - 1);
            Invalidate();
            return true;
        }
        private void DrawXAxisLabels(Graphics g, RectangleF plot)
        {
            using (Font f = new Font(Font.FontFamily, Math.Max(7.5f, Font.Size - 2f), FontStyle.Regular))
            {
                int ticks = 10; int i; for (i = 0; i <= ticks; i++)
                {
                    float x = plot.Left + (float)i / (float)ticks * plot.Width; g.DrawLine(_axisPen, x, plot.Bottom, x, plot.Bottom + 4);
                    double t = _viewStartSec + (double)i / (double)ticks * _visibleDurationSec; string label = t.ToString("0.0") + "s";
                    SizeF sz = g.MeasureString(label, f); g.DrawString(label, f, _axisTextBrush, x - sz.Width / 2, plot.Bottom + 4);
                }
            }
        }

        //private float MapValueToLaneY(double v, Channel ch, float laneTop, float laneHeight)
        //{
        //    double min = ch.ScaleMin, max = ch.ScaleMax; if (Math.Abs(max - min) < 1e-12) max = min + 1.0;
        //    double norm = (v - min) / (max - min); return laneTop + (float)((1.0 - norm) * laneHeight);
        //}

        private float MapValueToLaneY(double v, Channel ch, float laneTop, float laneHeight)
        {
            double min = ch.ScaleMin, max = ch.ScaleMax;
            if (Math.Abs(max - min) < 1e-12) max = min + 1.0;

            // Guard against NaN/Infinity
            if (double.IsNaN(v) || double.IsInfinity(v))
                return laneTop + laneHeight / 2;

            // Don't clamp - allow values to overflow beyond lane boundaries
            double norm = (v - min) / (max - min);
            return laneTop + (float)((1.0 - norm) * laneHeight);
        }

        private bool TryGetLatestValue(int channelIndex, out double value)
        {
            value = 0.0; if (_samples.Count == 0) return false; if (channelIndex < 0 || channelIndex >= _channels.Count) return false;
            Sample s = _samples[_samples.Count - 1]; if (s.Values == null || s.Values.Length <= channelIndex) return false; value = s.Values[channelIndex]; return true;
        }


        /// <summary>
        /// Remove all samples with time T in [t0, t1]. After removal the sample times
        /// are renormalized so the first sample starts at 0.0 (if any samples remain).
        /// </summary>
        public void RemoveSamplesBetween(double t0, double t1)
        {
            if (double.IsNaN(t0) || double.IsNaN(t1)) return;
            if (t1 < t0) { double tmp = t0; t0 = t1; t1 = tmp; }

            if (_samples.Count == 0) return;

            // Remove samples whose T is between t0 and t1 (inclusive)
            int write = 0;
            for (int read = 0; read < _samples.Count; read++)
            {
                var s = _samples[read];
                if (s.T < t0 || s.T > t1)
                {
                    if (write != read) _samples[write] = s;
                    write++;
                }
            }

            if (write == _samples.Count)
            {
                // nothing removed
                return;
            }

            // truncate list to kept items
            if (write == 0)
            {
                _samples.Clear();
            }
            else
            {
                _samples.RemoveRange(write, _samples.Count - write);
            }

            // If we removed the earliest samples, or to keep behaviour consistent,
            // renormalize times so the first sample starts at zero.
            if (_samples.Count > 0)
            {
                double t0new = _samples[0].T;
                if (Math.Abs(t0new) > 1e-12)
                {
                    for (int i = 0; i < _samples.Count; i++)
                        _samples[i].T -= t0new;
                }
            }

            // update totals, scroll and redraw
            if (_samples.Count >= 2) _totalDurationSec = _samples[_samples.Count - 1].T - _samples[0].T;
            else _totalDurationSec = 0.0;
            ClampViewStart();
            UpdateScrollFromViewport();
            Invalidate();
        }

        private int FindFirstIndexAtOrAfter(double t)
        { int lo = 0, hi = _samples.Count - 1, ans = _samples.Count - 1; while (lo <= hi) { int mid = (lo + hi) >> 1; if (_samples[mid].T >= t) { ans = mid; hi = mid - 1; } else lo = mid + 1; } return ans; }
        private int FindLastIndexAtOrBefore(double t)
        { int lo = 0, hi = _samples.Count - 1, ans = 0; while (lo <= hi) { int mid = (lo + hi) >> 1; if (_samples[mid].T <= t) { ans = mid; lo = mid + 1; } else hi = mid - 1; } return ans; }

        void TrimAndUpdateTotals()
        {
            if (_samples.Count == 0)
            {
                _totalDurationSec = 0.0;
                return;
            }

            // ✅ Keep ALL data from time 0 to allow scrolling back to the beginning
            // Only trim based on max sample count to prevent unbounded memory growth

            // Only trim if we exceed the maximum capacity
            if (_samples.Count > _maxPointsPerChannelToKeep)
            {
                int removeCount = _samples.Count - _maxPointsPerChannelToKeep;
                if (removeCount > 0 && removeCount < _samples.Count - 2)
                {
                    _samples.RemoveRange(0, removeCount);
                }
            }

            if (_samples.Count >= 2)
                _totalDurationSec = _samples[_samples.Count - 1].T - _samples[0].T;
            else
                _totalDurationSec = 0.0;
        }


        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        { using (StringFormat sf = new StringFormat()) { sf.Alignment = StringAlignment.Center; sf.LineAlignment = StringAlignment.Center; g.DrawString(text, font, brush, rect, sf); } }


        #endregion

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MultiChannelLiveChart
            // 
            this.Name = "MultiChannelLiveChart";
            this.Load += new System.EventHandler(this.MultiChannelLiveChart_Load);
            this.ResumeLayout(false);

        }

        // --- Alerts (per-lane) 6-10-2025 ---
        private sealed class LaneAlert
        {
            public string Message;
            public Color Back;
            public LaneAlert(string message, Color back) { Message = message; Back = back; }
        }
        private readonly Dictionary<int, LaneAlert> _laneAlerts = new Dictionary<int, LaneAlert>();

        // --- Advisories (per-lane, non-blocking) ---
        private sealed class LaneAdvisory
        {
            public string Message;
            public Color Back;
            public LaneAdvisory(string message, Color back) { Message = message; Back = back; }
        }
        private readonly Dictionary<int, LaneAdvisory> _laneAdvisories = new Dictionary<int, LaneAdvisory>();

        private int IndexOfChannelByName(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return -1;
            for (int i = 0; i < _channels.Count; i++)
                if (string.Equals(_channels[i].Name, channelName, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
        public void SetLaneAlert(string channelName, string message)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetLaneAlert(channelName, message)));
                return;
            }

            int idx = IndexOfChannelByName(channelName);
            if (idx < 0) return;

            _laneAlerts[idx] = new LaneAlert(message ?? "", Color.FromArgb(255, 255, 200, 200));

            // remove advisory on same lane (not alert)
            if (_laneAdvisories.ContainsKey(idx))
                _laneAdvisories.Remove(idx);

            Invalidate();
        }

        public void ClearLaneAlert(string channelName)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ClearLaneAlert(channelName)));
                return;
            }

            int idx = IndexOfChannelByName(channelName);
            if (idx < 0) return;

            _laneAlerts.Remove(idx);
            Invalidate();
        }

        public void ClearAllAlerts()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ClearAllAlerts));
                return;
            }

            if (_laneAlerts.Count == 0) return;
            _laneAlerts.Clear();
            Invalidate();
        }

        public void SetLaneAdvisory(string channelName, string message)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetLaneAdvisory(channelName, message)));
                return;
            }

            int idx = IndexOfChannelByName(channelName);
            if (idx < 0) return;

            // if alert exists, advisory should not override (optional but consistent with your DrawLaneAdvisories)
            if (_laneAlerts.ContainsKey(idx)) return;

            _laneAdvisories[idx] = new LaneAdvisory(message ?? "", Color.FromArgb(255, 255, 240, 200));
            Invalidate();
        }

        public void ClearLaneAdvisory(string channelName)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ClearLaneAdvisory(channelName)));
                return;
            }

            int idx = IndexOfChannelByName(channelName);
            if (idx < 0) return;

            _laneAdvisories.Remove(idx);
            Invalidate();
        }

        public void ClearAllAdvisories()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(ClearAllAdvisories));
                return;
            }

            if (_laneAdvisories.Count == 0) return;
            _laneAdvisories.Clear();
            Invalidate();
        }

        public bool HasAnyAlerts()
        {
            return _laneAlerts != null && _laneAlerts.Count > 0;
        }

        public bool SetChannelScaleByName(string channelName, double min, double max)
        {
            if (string.IsNullOrEmpty(channelName)) return false;
            int idx = -1;
            for (int i = 0; i < _channels.Count; i++)
                if (string.Equals(_channels[i].Name, channelName, StringComparison.OrdinalIgnoreCase))
                { idx = i; break; }
            if (idx < 0) return false;

            _channels[idx].ScaleMin = min;
            _channels[idx].ScaleMax = max;
            Invalidate();
            return true;
        }

        public double GetChannelScaleMaxByName(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return 0;
            for (int i = 0; i < _channels.Count; i++)
                if (string.Equals(_channels[i].Name, channelName, StringComparison.OrdinalIgnoreCase))
                    return _channels[i].ScaleMax;
            return 0;
        }

        // === Public helpers so MainForm can place overlays ===
        public int GetChannelCount()
        {
            return _channels.Count;
        }

        public string GetChannelNameAt(int index)
        {
            if (index < 0 || index >= _channels.Count) return null;
            return _channels[index].Name;
        }

        public RectangleF GetLaneRect(int laneIndex)
        {
            RectangleF plot = GetPlotRect();
            int n = _channels.Count;
            if (laneIndex < 0 || laneIndex >= n) return RectangleF.Empty;

            float laneGap = 6f;
            float laneHeight = (plot.Height - laneGap * (n - 1)) / n;
            if (laneHeight < 12f) laneHeight = 12f;

            float top = plot.Top + laneIndex * (laneHeight + laneGap);
            return new RectangleF(plot.Left, top, plot.Width, laneHeight);
        }

        // Fire when layout changes so overlays can reposition
        public event EventHandler ChartLayoutChanged;
        private void RaiseLayoutChanged()
        {
            if (ChartLayoutChanged != null) ChartLayoutChanged(this, EventArgs.Empty);
        }

        private void MultiChannelLiveChart_Load(object sender, EventArgs e)
        {

        }


        public sealed class ExportSample
        {
            public double T;          // seconds
            public double[] Values;   // channel values
            public ExportSample(double t, double[] v) { T = t; Values = v; }
        }

        private bool _enableHover = true;

        /// <summary>
        /// Enables or disables mouse hover tracking (e.g., for live vs playback).
        /// </summary>
        public bool EnableHover
        {
            get => _enableHover;
            set
            {
                _enableHover = value;
                if (!_enableHover)
                {
                    _isHovering = false;
                    _hoverTimeSec = double.NaN;
                }
                Invalidate();
            }
        }



        private bool _isHovering = false;
        private double _hoverTimeSec = double.NaN;


        private readonly ToolTip _hoverTip = new ToolTip();
        private double _lastHoverT = double.NaN;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_sw != null && _sw.IsRunning)
                return;

            if (!_enableHover)
                return; // 🚫 completely disable hover

            RectangleF plot = GetPlotRect();
            if (!plot.Contains(e.Location))
            {
                _isHovering = false;
                _hoverTimeSec = double.NaN;
                Invalidate();
                return;
            }

            if (_samples.Count == 0)
                return;

            _isHovering = true;
            //_hoverTimeSec = PixelToTime(e.X);
            //Invalidate();
            double newHoverT = PixelToTime(e.X);

            // repaint ONLY if hover moved meaningfully (~30 ms)
            if (double.IsNaN(_hoverTimeSec) || Math.Abs(newHoverT - _hoverTimeSec) > 0.03)
            {
                _hoverTimeSec = newHoverT;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_sw != null && _sw.IsRunning)
                return;

            if (!_enableHover)
                return;

            _isHovering = false;
            _hoverTimeSec = double.NaN;
            Invalidate();
        }


        private int FindNearestSampleIndex(double tSec)
        {
            if (_samples == null || _samples.Count == 0) return -1;

            int lo = 0, hi = _samples.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                double midT = _samples[mid].T;
                if (midT < tSec)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            if (lo == 0) return 0;
            if (lo >= _samples.Count) return _samples.Count - 1;

            double t1 = _samples[lo].T;
            double t0 = _samples[lo - 1].T;
            return (Math.Abs(t1 - tSec) < Math.Abs(tSec - t0)) ? lo : lo - 1;
        }

        // 🔑 Call ONLY when starting a NEW test
        public void StartNew()
        {
            _renderTimer.Stop();
            _sw.Reset();          // reset ONLY once
            _sw.Start();
            ToggleLive(true);
            //  _renderTimer.Start();
        }

        // ⏸ Pause due to device disconnect
        public void Pause()
        {
            _renderTimer.Stop();
            _sw.Stop();           // DO NOT reset
        }

        // ▶ Resume after reconnect
        public void Resume()
        {
            if (!_sw.IsRunning)
                _sw.Start();      // continue SAME time

            if (!_renderTimer.Enabled)
                _renderTimer.Start();
        }




        // Start Code for If Click on M button so remove this marker this clicked previously remove first added marker chnage on 14-02-2026
        public void RemoveMarker(double time, string label, double tolerance = 0.001)
        {
            var marker = _markers.FirstOrDefault(m =>
                Math.Abs(m.T - time) < tolerance && m.Label == label);

            if (marker != null)
            {
                _markers.Remove(marker);
                Invalidate();
            }
        }

        // Add this method to remove a marker at a specific index
        public void RemoveMarkerAt(int index)
        {
            if (index >= 0 && index < _markers.Count)
            {
                _markers.RemoveAt(index);
                Invalidate();
            }
        }

        // Add this method to remove the closest marker to a given time
        public bool RemoveClosestMarker(double time, double maxDistance = 0.2)
        {
            if (_markers.Count == 0)
                return false;

            // Find the marker with the closest time
            var closest = _markers
                .Select((m, idx) => new { Marker = m, Index = idx, Distance = Math.Abs(m.T - time) })
                .Where(x => x.Distance < maxDistance)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (closest != null)
            {
                _markers.RemoveAt(closest.Index);
                Invalidate();
                return true;
            }

            return false;
        }
        // End Code for If Click on M button so remove this marker this clicked previously remove first added marker chnage on 14-02-2026

    }
}


