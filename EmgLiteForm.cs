using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SantronChart;
using static SantronWinApp.EmgLiteForm;

namespace SantronWinApp
{
    public sealed class EmgLiteForm : Form
    {
        private readonly MultiChannelLiveChart _chart;
        private readonly Panel _settingsPanel;
        private readonly Label _recordingTimeLabel;
        private readonly System.Windows.Forms.Timer _recordingTimer;
        private readonly TableLayoutPanel _mainContainer;

        private EmgLiteEngine _engine;
        private EmgBleSource _bleSource;

        // State tracking
        private bool _isRecording = false;
        private DateTime _recordingStartTime;

        public EmgLiteForm()
        {
            // KEEP the title bar and standard buttons
            FormBorderStyle = FormBorderStyle.Sizable;
            // Start maximized - covers all screen except taskbar
            WindowState = FormWindowState.Maximized;

            // Optional: Show in taskbar (default is true, but good to ensure)
            ShowInTaskbar = true;
            // Title shown in title bar and taskbar
            Text = "EMG Lite - Live Monitoring";

            // Background
            BackColor = Color.White;


            // Rest of your initialization
            _recordingTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _mainContainer = CreateMainContainer();
            _chart = CreateChart();
            InitializeComponents();

            Load += (_, __) => InitChartAndEngine();
            FormClosing += (_, __) => SafeDisposeEngine();
        }

        private MultiChannelLiveChart CreateChart()
        {
            return new MultiChannelLiveChart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
        }




        private TableLayoutPanel CreateMainContainer()
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
        }

        private void InitializeComponents()
        {
            // Configure main container
            _mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            _mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            // Top panel for chart
            var chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5)
            };
            chartPanel.Controls.Add(_chart);

            // Bottom panel for controls and status

            // Add to main container
            _mainContainer.Controls.Add(chartPanel, 0, 0);

            Controls.Add(_mainContainer);
        }




        private void InitChartAndEngine()
        {
            try
            {
                // Setup chart for 1 channel with more reasonable range
                // EMG signals typically range from 0-5000 uV after RMS smoothing
                var channels = new List<MultiChannelLiveChart.Channel>
                {
                    new MultiChannelLiveChart.Channel("EMG Signal", Color.Red, -1000, 1000, "uV")
                };
                _chart.SetChannels(channels);
                _chart.SetVisibleDuration(10);

                // Start chart internal timing/render
                _chart.Start();

                // BLE Source configuration - Automatic connection
                _bleSource = new EmgBleSource
                {
                    DeviceNameContains = "NPG-",
                    PlotChannelIndex = 0, // Fixed to channel 0 (muscle)
                    SendStartCommand = true
                };

                _bleSource.OnConnectionStateChanged += state =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        Text = "EMG Lite - " + state;

                        // Update UI based on connection state
                        if (state.Contains("STREAMING"))
                        {
                        }
                        else if (state.Contains("CONNECTED"))
                        {
                            // Auto-start when connected
                            _engine?.Start();
                        }
                        else if (state.Contains("DISCONNECTED") || state.Contains("STOPPED"))
                        {
                        }
                    }));
                };

                _engine = new EmgLiteEngine(
                    _bleSource,
                    _chart,
                    chartUpdateHz: 50,
                    rmsWindowMs: 100,
                    smoothingPercent: 15
                );

                // Auto-start connection
                _bleSource.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing chart/engine: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void SafeDisposeEngine()
        {
            try
            {
                _engine?.Dispose();
                _bleSource?.Dispose();
                _recordingTimer?.Stop();
                _recordingTimer?.Dispose();
            }
            catch { /* ignore */ }
            _engine = null;
            _bleSource = null;
        }



        private void EmgLiteForm_Load(object sender, EventArgs e)
        {

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // EmgLiteForm
            // 
            this.ClientSize = new System.Drawing.Size(1924, 963);
            this.Name = "EmgLiteForm";
            this.Load += new System.EventHandler(this.EmgLiteForm_Load_1);
            this.ResumeLayout(false);

        }
        public enum EmgOutputMode
        {
            RmsEnvelope,     // current behavior (always >= 0)
            SignedFiltered   // new behavior (can be negative)
        }

        private void EmgLiteForm_Load_1(object sender, EventArgs e)
        {

        }
    }

    // Enhanced EMG Signal Processor with Fixed Muscle Filter and 50Hz Notch
   
}