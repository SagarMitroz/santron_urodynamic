using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using SantronChart;
using SantronWinApp.Helper;
using SantronWinApp.Properties;

namespace SantronWinApp
{
    public class FormTestViewer : Form
    {
        private readonly MultiChannelLiveChart _chart;
        private readonly Label _lblDuration;
        private TestChannelManager _testMgr;
        private readonly ChannelSettings[] channelSettings;


        // keep a zoom state so +/- is smooth
        private double _visibleWindowSec = 60.0;  // initial window (forces scrollbar on long tests)
        private double _tMin = 0.0, _tMax = 0.0;  // loaded time range

        public FormTestViewer(string filePath)
        {
            Text = $"Test Viewer - {Path.GetFileName(filePath)}";
            Width = 1100;
            Height = 720;

         

            // ==== Top bar (title + duration + toolbar) ====
            var top = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(248, 249, 250) };
            Controls.Add(top);

            var lblTitle = new Label
            {
                Text = "Test Duration:",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(12, 10)
            };
            top.Controls.Add(lblTitle);

            _lblDuration = new Label
            {
                Text = "00:00",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(lblTitle.Right + 8, 10)
            };
            top.Controls.Add(_lblDuration);

            // toolbar buttons
            int x = 260;
            Button btnStart = MkBtn("⏮ Start", x, 6, (s, e) => _chart.ScrollTo(_tMin)); x += 90;
            Button btnEnd = MkBtn("⏭ End", x, 6, (s, e) => { _chart.ScrollTo(Math.Max(_tMin, _tMax - _visibleWindowSec)); }); x += 80;
            Button btnZOut = MkBtn("− Zoom", x, 6, (s, e) => Zoom(1.25)); x += 80;
            Button btnZIn = MkBtn("+ Zoom", x, 6, (s, e) => Zoom(0.8)); x += 80;
            Button btnFit10 = MkBtn("Fit 10s", x, 6, (s, e) => { _visibleWindowSec = 10.0; _chart.SetVisibleDuration(_visibleWindowSec); }); x += 80;

            top.Controls.AddRange(new Control[] { btnStart, btnEnd, btnZOut, btnZIn, btnFit10 });

            // ==== Chart ====
            _chart = new MultiChannelLiveChart { Dock = DockStyle.Fill };
            Controls.Add(_chart);
            _chart.BringToFront();

            ConfigureChartLanes();
            var setup = LoadScaleAndColorModel("DefaultSetup");
            _testMgr = new TestChannelManager(setup);
            channelSettings = LoadChannelSettings();

            // render loop + initial visible window ensures scrollbar on long files
            _chart.SetFps(30);
            _chart.SetVisibleDuration(_visibleWindowSec);
            _chart.Start();

            LoadFile(filePath);
            // Show from the beginning by default (not “Go Live”)
            _chart.ScrollTo(_tMin);
        }
        private ChannelSettings[] LoadChannelSettings()
        {
            // Default channel zero name to load the settings file
            string defaultChannelZero = "PVES";
            string filePath = GetScaleAndColorSetupFilePath(defaultChannelZero);
            ScaleAndColorModel record = null;

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);
                    record = JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading Scale And Color Setup: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Parse plot scale strings (e.g., "0-6" to YMin=0, YMax=6)
            double ParseYMin(string scale) => double.Parse(scale.Split('-')[0]);
            double ParseYMax(string scale) => double.Parse(scale.Split('-')[1]);

            // If no file or error, fall back to default settings
            if (record == null)
            {
                return new[]
                {
                    new ChannelSettings { Name = "PVES", YMin = 0, YMax = 6, LineColor = Color.Blue },
                    new ChannelSettings { Name = "PABD", YMin = 0, YMax = 8, LineColor = Color.Red },
                    new ChannelSettings { Name = "QVOL", YMin = 0, YMax = 10, LineColor = Color.Black },
                    new ChannelSettings { Name = "VINF", YMin = 0, YMax = 15, LineColor = Color.SkyBlue },
                    new ChannelSettings { Name = "EMG", YMin = 0.1, YMax = 6, LineColor = Color.Orange },
                    new ChannelSettings { Name = "UPP", YMin = 0.1, YMax = 7, LineColor = Color.Yellow },
                    new ChannelSettings { Name = "PURA", YMin = 0, YMax = 6, LineColor = Color.Green }
                };
            }

            return new[]
            {
                new ChannelSettings
                {
                    Name = record.ChannelZero ?? "PVES",
                    YMin = ParseYMin(record.PlotScaleZero ?? "0-6"),
                    YMax = ParseYMax(record.PlotScaleZero ?? "0-6"),
                    LineColor = ColorTranslator.FromHtml(record.ColorZero ?? "#0000FF")
                },
                new ChannelSettings
                {
                    Name = record.ChannelOne ?? "PABD",
                    YMin = ParseYMin(record.PlotScaleOne ?? "0-8"),
                    YMax = ParseYMax(record.PlotScaleOne ?? "0-8"),
                    LineColor = ColorTranslator.FromHtml(record.ColorOne ?? "#FF0000")
                },
                new ChannelSettings
                {
                    Name = record.ChannelTwo ?? "QVOL",
                    YMin = ParseYMin(record.PlotScaleTwo ?? "0-10"),
                    YMax = ParseYMax(record.PlotScaleTwo ?? "0-10"),
                    LineColor = ColorTranslator.FromHtml(record.ColorTwo ?? "#000000")
                },
                new ChannelSettings
                {
                    Name = record.ChannelThree ?? "VINF",
                    YMin = ParseYMin(record.PlotScaleThree ?? "0-15"),
                    YMax = ParseYMax(record.PlotScaleThree ?? "0-15"),
                    LineColor = ColorTranslator.FromHtml(record.ColorThree ?? "#87CEEB")
                },
                new ChannelSettings
                {
                    Name = record.ChannelFour ?? "EMG",
                    YMin = ParseYMin(record.PlotScaleFour ?? "0.1-6"),
                    YMax = ParseYMax(record.PlotScaleFour ?? "0.1-6"),
                    LineColor = ColorTranslator.FromHtml(record.ColorFour ?? "#FFA500")
                },
                new ChannelSettings
                {
                    Name = record.ChannelFive ?? "UPP",
                    YMin = ParseYMin(record.PlotScaleFive ?? "0.1-7"),
                    YMax = ParseYMax(record.PlotScaleFive ?? "0.1-7"),
                    LineColor = ColorTranslator.FromHtml(record.ColorFive ?? "#FFFF00")
                },
                new ChannelSettings
                {
                    Name = record.ChannelSix ?? "PURA",
                    YMin = ParseYMin(record.PlotScaleSix ?? "0-6"),
                    YMax = ParseYMax(record.PlotScaleSix ?? "0-6"),
                    LineColor = ColorTranslator.FromHtml(record.ColorSix ?? "#008000")
                }
            };


        }

        private string GetScaleAndColorSetupFilePath(string channelZero)
        {
            string exeFolder = Application.StartupPath;
            //string folder = Path.Combine(exeFolder, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, channelZero + ".dat");
        }
        private ScaleAndColorModel LoadScaleAndColorModel(string setupName)
        {
            //string folder = Path.Combine(Application.StartupPath, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");

            // make sure the folder exists
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, setupName + ".dat");

            // if requested file not found → try latest available
            if (!File.Exists(filePath))
            {
                var files = Directory.GetFiles(folder, "*.dat")
                                     .OrderByDescending(f => File.GetLastWriteTime(f))
                                     .ToList();

                if (files.Count == 0)
                    return null; // no saved setups at all

                filePath = files[0]; // pick the newest
            }

            try
            {
                // read and decrypt
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string jsonData = CryptoHelper.Decrypt(encryptedData);

                if (string.IsNullOrWhiteSpace(jsonData))
                    return null;

                // deserialize into your model
                var model = System.Text.Json.JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);

                return model;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Scale And Color Setup: " + ex.Message);
                return null;
            }
        }


        private Button MkBtn(string text, int x, int y, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9F),
                Height = 26,
                Width = 72,
                Location = new Point(x, y)
            };
            b.Click += onClick;
            return b;
        }

        private void ConfigureChartLanes()
        {
            var lanes = new[]
            {
                new MultiChannelLiveChart.Channel("PVES", Color.Blue,       0, 200, "cmH2O"),
                new MultiChannelLiveChart.Channel("PABD", Color.Red,        0, 100, "cmH2O"),
                new MultiChannelLiveChart.Channel("PDET", Color.DarkViolet, 0, 200, "cmH2O"),
                new MultiChannelLiveChart.Channel("VINF", Color.SteelBlue,  0, 1000, "ml"),
                new MultiChannelLiveChart.Channel("QVOL", Color.Black,      0, 500, "ml"),
                new MultiChannelLiveChart.Channel("FLOW/UPP", Color.Goldenrod, 0, 50, "ml/s"),
                new MultiChannelLiveChart.Channel("EMG",  Color.Green,      0, 2000, "uV")
            }.ToList();

            _chart.SetChannels(lanes);
        }

        private void LoadFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1) return;

            



            bool first = true;
            double tLast = 0;

            // CSV header: Time,Ch1,Ch2,...
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = lines[i].Split(',');
                if (parts.Length < 2) continue;

                if (!double.TryParse(parts[0], out double t)) continue;
                var vals = parts.Skip(1).Select(p => double.TryParse(p, out var v) ? v : 0.0).ToArray();

                if (first) { _tMin = t; first = false; }
                tLast = t;

                _chart.AppendSample(vals, t);
            }

            _tMax = tLast;

            var total = TimeSpan.FromSeconds(Math.Max(0.0, _tMax - _tMin));
            _lblDuration.Text = $"{(int)total.TotalHours:00}:{total.Minutes:00}";  // HH:mm
        }

        private void Zoom(double factor)
        {
            // factor >1 => zoom out; <1 => zoom in
            _visibleWindowSec = Math.Max(1.0, Math.Min(3600.0, _visibleWindowSec * factor));
            _chart.SetVisibleDuration(_visibleWindowSec);
        }
    }
}
