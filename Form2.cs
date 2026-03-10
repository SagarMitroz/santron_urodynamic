using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SantronWinApp.Helper;

namespace SantronWinApp
{
    public partial class Form2 : Form
    {
        private Chart liveChart;
        private Timer chartUpdateTimer;
        private Queue<ChannelData> dataQueue = new Queue<ChannelData>();
        private string encryptedFilePath = "";
        private int sampleIndex = 0;
        private bool isRecording = false;
        private bool isPaused = false;
        private ChannelSettings[] channelSettings;
        private Button pauseResumeButton;
        private Timer simulateTimer;

        public Form2()
        {
            InitializeComponent();
            channelSettings = LoadDefaultChannelSettings();
            InitializeUI();
        }


        private void InitializeUI()
        {
            var startButton = new Button { Text = "Start Test", Dock = DockStyle.Top, Height = 40 };
            var liveButton = new Button { Text = "Go Live", Dock = DockStyle.Top, Height = 40 };
            var loadButton = new Button { Text = "Load Saved Data", Dock = DockStyle.Top, Height = 40 };
            pauseResumeButton = new Button { Text = "Pause", Dock = DockStyle.Top, Height = 40 };

            startButton.Click += StartTest_Click;
            liveButton.Click += GoLive_Click;
            pauseResumeButton.Click += PauseResume_Click;
            loadButton.Click += LoadSavedData_Click;

            Controls.Add(loadButton);
            Controls.Add(pauseResumeButton);
            Controls.Add(liveButton);
            Controls.Add(startButton);
        }


        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void StartTest_Click(object sender, EventArgs e)
        {
            encryptedFilePath = Path.Combine(Application.StartupPath, "Samples", $"test_{DateTime.Now:yyyyMMdd_HHmmss}.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(encryptedFilePath));
            isRecording = true;
            isPaused = false;
            pauseResumeButton.Text = "Pause";
        }

        private void GoLive_Click(object sender, EventArgs e)
        {
            SetupLiveChart();
            chartUpdateTimer = new Timer { Interval = 100 };
            chartUpdateTimer.Tick += ChartUpdateTimer_Tick;
            chartUpdateTimer.Start();

            simulateTimer = new Timer { Interval = 100 };
            simulateTimer.Tick += (s, ev) =>
            {
                var rand = new Random();
                OnDataReceived(
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10,
                    rand.NextDouble() * 10);
            };
            simulateTimer.Start();
        }

        private void PauseResume_Click(object sender, EventArgs e)
        {
            isPaused = !isPaused;
            pauseResumeButton.Text = isPaused ? "Resume" : "Pause";
        }

        private void LoadSavedData_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Encrypted Data Files (*.dat)|*.dat";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                    {
                        while (fs.Position < fs.Length)
                        {
                            byte[] lengthBytes = new byte[4];
                            fs.Read(lengthBytes, 0, 4);
                            int length = BitConverter.ToInt32(lengthBytes, 0);

                            byte[] encryptedEntry = new byte[length];
                            fs.Read(encryptedEntry, 0, length);

                            string decrypted = CryptoHelper.Decrypt(encryptedEntry);
                            ChannelData entry = JsonSerializer.Deserialize<ChannelData>(decrypted);

                            MessageBox.Show("Loaded data:\n" + string.Join(", ", entry.Values), "Decrypted Data");
                        }
                    }
                }
            }
        }

        private void OnDataReceived(double ch0, double ch1, double ch2, double ch3, double ch4, double ch5, double ch6)
        {
            if (isPaused) return;

            var entry = new ChannelData
            {
                Timestamp = DateTime.Now,
                Values = new[] { ch0, ch1, ch2, ch3, ch4, ch5, ch6 }
            };
            dataQueue.Enqueue(entry);

            if (isRecording)
            {
                string json = JsonSerializer.Serialize(entry);
                byte[] encrypted = CryptoHelper.Encrypt(json);

                using (var fs = new FileStream(encryptedFilePath, FileMode.Append, FileAccess.Write))
                {
                    byte[] lengthPrefix = BitConverter.GetBytes(encrypted.Length);
                    fs.Write(lengthPrefix, 0, lengthPrefix.Length);
                    fs.Write(encrypted, 0, encrypted.Length);
                }
            }
        }

        private void ChartUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (dataQueue.Count == 0 || isPaused) return;

            var entry = dataQueue.Dequeue();
            sampleIndex++;

            for (int i = 0; i < 7; i++)
            {
                liveChart.Series[$"Channel{i}"].Points.AddXY(sampleIndex, entry.Values[i]);
            }

            foreach (var area in liveChart.ChartAreas)
            {
                area.AxisX.Minimum = Math.Max(0, sampleIndex - 100);
                area.AxisX.Maximum = sampleIndex;
            }
        }

        private void SetupLiveChart()
        {
            if (liveChart != null)
                Controls.Remove(liveChart);

            liveChart = new Chart { Dock = DockStyle.Fill, BorderlineWidth = 1, BorderlineColor = Color.Black };
            Controls.Add(liveChart);
            liveChart.BringToFront();

            for (int i = 0; i < 7; i++)
            {
                var area = new ChartArea($"Area{i}")
                {
                    Position = new ElementPosition(0, i * 14.2f, 100, 14.2f),
                    InnerPlotPosition = new ElementPosition(10, 10, 80, 80),
                };
                area.AxisX.ScrollBar.Enabled = true;
                area.AxisX.ScaleView.Zoomable = true;
                area.AxisY.Minimum = 0;
                area.AxisY.Maximum = 10;
                area.AxisX.LabelStyle.Enabled = (i == 6);

                liveChart.ChartAreas.Add(area);

                var series = new Series($"Channel{i}")
                {
                    ChartType = SeriesChartType.FastLine,
                    BorderWidth = 2,
                    ChartArea = area.Name,
                    Color = channelSettings[i].LineColor
                };
                liveChart.Series.Add(series);
            }
        }

        private ChannelSettings[] LoadDefaultChannelSettings()
        {
            return new[]
            {
                new ChannelSettings { Name = "Ch0", LineColor = Color.Blue },
                new ChannelSettings { Name = "Ch1", LineColor = Color.Red },
                new ChannelSettings { Name = "Ch2", LineColor = Color.Green },
                new ChannelSettings { Name = "Ch3", LineColor = Color.Black },
                new ChannelSettings { Name = "Ch4", LineColor = Color.Orange },
                new ChannelSettings { Name = "Ch5", LineColor = Color.Purple },
                new ChannelSettings { Name = "Ch6", LineColor = Color.Brown }
            };
        }
    }

    public class ChannelData
    {
        public DateTime Timestamp { get; set; }
        public double[] Values { get; set; }
    }

    
}
