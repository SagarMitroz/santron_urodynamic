// Form3.cs  — single-form, designer-free version of your Form7 with the same math & DAQ pipeline.
// Target: classic C# (no init accessors or index-from-end operators)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using CloudinaryDotNet;
using Glimpse.AspNet;
using NationalInstruments;
using NationalInstruments.DAQmx;
using SantronChart;
using SantronWinApp.IO;
using SantronWinApp.Processing;
using SantronWinApp.Test;
using Xamarin.Forms.PlatformConfiguration;


namespace SantronWinApp
{
    public partial class Form3 : Form
    {
        private TestOrchestrator _orch;
        private CalibrationProfile _profile;
        private Panel panelGraph;
        private MultiChannelLiveChart _liveChart;

        private double _lastPlotT;
        private int _calibWarmup = 10; // ignore first N samples before learning
        private int _screenMinutes = 1; // 1/2/5 typical it should be from color and setupfile        
        private int _warmupCounter = 0;
   

        // Recording state/timer
        private bool _isRecording = false;
        private bool _isPaused = false;
        private List<(double T, double[] Values)> _recorded = new List<(double, double[])>();

        private Timer _testTimer;
        private DateTime _testStartTime;        // when Start was clicked
        private TimeSpan _pausedTime = TimeSpan.Zero; // total paused duration
        private DateTime? _pauseStartedAt = null;     // when Pause was clicked
      
        private Label _lblTimer;

        // Buttons
        private Button _btnStart, _btnPause, _btnResume, _btnStop, _btnSave, _btnOpen, _btnMarker;

        public Form3()
        {
            InitializeComponent();
            Constants();            //replace this fuction with systemsetting file 
            ConfigurePanelUI();
            ConfigureChartLanes();  //replace with Colors and setup file
            LoadGraph();

        }

        private void ConfigurePanelUI()
        {
            // UI host
            panelGraph = new Panel();
            panelGraph.Width = 500;
            panelGraph.Height = 600;
            panelGraph.Padding = new Padding(0, 90, 0, 0);
            panelGraph.Dock = DockStyle.Bottom;
            Controls.Add(panelGraph);


            _liveChart = new MultiChannelLiveChart();
            _liveChart.Dock = DockStyle.Fill;
            panelGraph.Controls.Add(_liveChart);


            _btnStart = new Button { Text = "Start Test", Width = 100, Height = 30, Location = new Point(20, 20) };
            _btnStart.Click += BtnStart_Click; Controls.Add(_btnStart);

            _btnPause = new Button { Text = "Pause Test", Width = 100, Height = 30, Location = new Point(140, 20) };
            _btnPause.Click += BtnPause_Click; Controls.Add(_btnPause);

            // ✅ New Resume button
            _btnResume = new Button { Text = "Resume Test", Width = 110, Height = 30, Location = new Point(260, 20) };
            _btnResume.Click += BtnResume_Click; Controls.Add(_btnResume);

            _btnStop = new Button { Text = "Stop Test", Width = 100, Height = 30, Location = new Point(380, 20) };
            _btnStop.Click += BtnStop_Click; Controls.Add(_btnStop);

            _btnSave = new Button { Text = "Save Test", Width = 100, Height = 30, Location = new Point(500, 20) };
            _btnSave.Click += BtnSave_Click; Controls.Add(_btnSave);

            _btnOpen = new Button { Text = "Open Test", Width = 100, Height = 30, Location = new Point(620, 20) };
            _btnOpen.Click += BtnOpen_Click; Controls.Add(_btnOpen);

            _btnMarker = new Button { Text = "FS Button", Width = 100, Height = 30, Location = new Point(740, 20) };
            _btnMarker.Click += BtnMarker_Click; Controls.Add(_btnMarker);

            // Timer label (HH:MM)
            _lblTimer = new Label { Text = "00:00", Width = 80, Location = new Point(860, 25) };
            Controls.Add(_lblTimer);

            // Timer that updates every second
            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += (s, e) => UpdateTimerLabel();

            // Initialize button states
            UpdateButtons(false, false, false);  // recording, paused, canSave


        }

        private void LoadGraph()
        {
            var daq = new DaqService();                      // 0–10V → COUNTS @1kHz
            var cal = new Calibration(_profile);             // counts → engineering (legacy rules)
            var proc = new SignalProcessor(cal, 1000.0, 10.0);// decimate, PDET, Flow/UPP, EMG RMS

            _orch = new TestOrchestrator(daq, proc, new PumpController(), new SysSetupStore());
            _orch.OnDisplayFrame += OnDisplayFrame;

            // Start rendering + DAQ
            proc.SetFlowWindowFromConstant(10); 
            proc.SetEmgWindowMs(300);           // a tad calmer EMG
            //proc.SetSmoothingAlpha(0.25);
            _liveChart.SetFps(20);
            _liveChart.SetVisibleDuration(_screenMinutes * 60.0);
            _liveChart.Start();                                 // starts render timer; shows HScroll + Go Live button (built-in) :contentReference[oaicite:6]{index=6}
            try { _orch.Start("Dev1/ai0:6"); } catch { }   // safe if device missing
        }

        private void ConfigureChartLanes()
        {
            // Lanes: PVES, PABD, PDET, VINF, QVOL, FLOW/UPP, EMG
            List<MultiChannelLiveChart.Channel> lanes = new List<MultiChannelLiveChart.Channel>();
            lanes.Add(new MultiChannelLiveChart.Channel("PVES", Color.Blue, 0, 200, "cmH2O"));
            lanes.Add(new MultiChannelLiveChart.Channel("PABD", Color.Red, 0, 100, "cmH2O"));
            lanes.Add(new MultiChannelLiveChart.Channel("PDET", Color.DarkViolet, 0, 200, "cmH2O"));
            lanes.Add(new MultiChannelLiveChart.Channel("VINF", Color.SteelBlue, 0, 1000, "ml"));
            lanes.Add(new MultiChannelLiveChart.Channel("QVOL", Color.Black, 0, 500, "ml"));
            lanes.Add(new MultiChannelLiveChart.Channel("FLOW/UPP", Color.Goldenrod, 0, 25, "ml/s"));
            lanes.Add(new MultiChannelLiveChart.Channel("EMG", Color.Green, 0, 2000, "uV"));

            _liveChart.SetChannels(lanes);  // also prepares series pens, right-side scale/units, scroll, GoLive UI :contentReference[oaicite:8]{index=8}
            _liveChart.ScrollToLive();
        }

        private void Constants()
        {
            // 1) Your legacy constants → profile
            var constants = new int[7];
            // [0]=PVES  [1]=PABD  [2]=flow-smoothing denominator (keep 10 ≈ 1s) 
            // [3]=VINF   [4]=QVOL  [5]=unused  [6]=EMG
            constants[0] = 1074;   // PVES
            constants[1] = 1040;   // PABD
            constants[2] = 10;     // *** DO NOT put 250 here; 10 → stable flow window (~1s) ***
            constants[3] = 2038;   // VINF
            constants[4] = 1994;   // QVOL
            constants[5] = 0;      // unused
            constants[6] = 250;    // EMG  (=> slope = 0.25 → ~×4 compared to default)

            double sg1 = 1.0;      // Specific gravity (use your real value when available)
            int uppCount = 2084;   // UPP


            var offsetsCounts = new double[7]; // keep zeros unless you have baseline offsets (in COUNTS)
            _profile = CalibrationProfileFactory.FromLegacy(constants, offsetsCounts, uppCount, sg1);



            //// replace this function with syssetting file with following code
            //try
            //{
            //    string sysSetupPath = Path.Combine(Application.StartupPath, "SysSetup.rec");
            //    _orch.LoadProfile(sysSetupPath);
            //    _profile = _orch.Profile;

            //}
            //catch { }







        }

        private void OnDisplayFrame(SampleFrame f)
        {
            try
            {
                if (_warmupCounter < _calibWarmup)
                {
                    _warmupCounter++;
                    return; // skip plotting/learning
                }
                // Save samples if recording
                if (_isRecording && !_isPaused)
                    _recorded.Add((f.T, (double[])f.Values.Clone()));

                _lastPlotT = f.T;
                _liveChart.AppendSample((double[])f.Values.Clone(), f.T);
            }
            catch { }
        }



        private void BtnStart_Click(object sender, EventArgs e)
        {
            _recorded.Clear();
            _isRecording = true;
            _isPaused = false;
            _pauseStartedAt = null;
            _pausedTime = TimeSpan.Zero;
            _testStartTime = DateTime.Now;

            _testTimer.Start();
            UpdateTimerLabel();
            UpdateButtons(recording: true, paused: false, canSave: false);
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            if (!_isRecording || _isPaused) return;
            _isPaused = true;
            _pauseStartedAt = DateTime.Now;
            UpdateTimerLabel();
            UpdateButtons(recording: true, paused: true, canSave: _recorded.Count > 0);
        }

        private void BtnResume_Click(object sender, EventArgs e)
        {
            if (!_isRecording || !_isPaused) return;
            if (_pauseStartedAt.HasValue)
                _pausedTime += (DateTime.Now - _pauseStartedAt.Value);

            _pauseStartedAt = null;
            _isPaused = false;
            UpdateTimerLabel();
            UpdateButtons(recording: true, paused: false, canSave: _recorded.Count > 0);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (!_isRecording) return;

            
            if (_isPaused && _pauseStartedAt.HasValue)
                _pausedTime += (DateTime.Now - _pauseStartedAt.Value);

            _isRecording = false;
            _isPaused = false;
            _pauseStartedAt = null;

            _testTimer.Stop();
            UpdateTimerLabel();
            UpdateButtons(recording: false, paused: false, canSave: _recorded.Count > 0);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_recorded.Count == 0) { MessageBox.Show("Nothing to save."); return; }

            using (var dlg = new SaveFileDialog { Filter = "UroTest (*.utt)|*.utt", FileName = $"UroTest_{DateTime.Now:yyyyMMdd_HHmm}.utt" })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    using (var w = new StreamWriter(dlg.FileName))
                    {
                        // Header: Time,Ch1,Ch2,...
                        int ch = _recorded[0].Values.Length;
                        var header = new List<string> { "Time" };
                        for (int i = 1; i <= ch; i++) header.Add($"Ch{i}");
                        w.WriteLine(string.Join(",", header));

                        foreach (var (t, vals) in _recorded)
                            w.WriteLine($"{t},{string.Join(",", vals)}");
                    }
                    MessageBox.Show("Test saved successfully.");
                }
            }
        }

        //private void BtnOpen_Click(object sender, EventArgs e)
        //{
        //    using (var dlg = new OpenFileDialog { Filter = "UroTest (*.utt)|*.utt" })
        //    {
        //        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        //        // Optional: ensure live/recording is off while viewing a file
        //        if (_isRecording) BtnStop_Click(sender, e);

        //        // Clear chart and load file
        //        _liveChart.Clear();

        //        var lines = File.ReadAllLines(dlg.FileName);
        //        if (lines.Length <= 1) { MessageBox.Show("File has no data."); return; }

        //        // skip header
        //        for (int i = 1; i < lines.Length; i++)
        //        {
        //            var parts = lines[i].Split(',');
        //            if (parts.Length < 2) continue;

        //            if (!double.TryParse(parts[0], out double t)) continue;
        //            int ch = parts.Length - 1;
        //            var vals = new double[ch];
        //            for (int c = 0; c < ch; c++)
        //                double.TryParse(parts[c + 1], out vals[c]);

        //            _liveChart.AppendSample(vals, t);
        //        }

        //       // _liveChart.ScrollToLive();
        //        MessageBox.Show("Test loaded and plotted.");
        //        UpdateButtons(recording: false, paused: false, canSave: false);
        //    }
        //}

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "UroTest (*.utt)|*.utt" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var viewer = new FormTestViewer(dlg.FileName);
                viewer.Show();
            }
        }


        private void BtnMarker_Click(object sender, EventArgs e)
        {
            _liveChart.AddMarker(_lastPlotT, "FS", Color.Green);
        }

        private void UpdateTimerLabel()
        {
            if (!_isRecording)
            {
                _lblTimer.Text = "00:00";
                return;
            }

            var pauseSoFar = (_isPaused && _pauseStartedAt.HasValue)
                ? (DateTime.Now - _pauseStartedAt.Value)
                : TimeSpan.Zero;

            var elapsed = (DateTime.Now - _testStartTime) - (_pausedTime + pauseSoFar);
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            // Show HH:mm (minutes are lowercase mm)
            _lblTimer.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}";
        }

        private void UpdateButtons(bool recording, bool paused, bool canSave)
        {
            _btnStart.Enabled = !recording;
            _btnPause.Enabled = recording && !paused;
            _btnResume.Enabled = recording && paused;
            _btnStop.Enabled = recording;
            _btnSave.Enabled = canSave;  // true when you have something in _recorded
        }



    }
}
