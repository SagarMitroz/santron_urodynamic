using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NationalInstruments;
using System.Collections.Generic;
using NationalInstruments.DAQmx;
using CloudinaryDotNet;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Security.Cryptography;
using System.Text.Json;


namespace SantronWinApp
{

    public class ChannelSettings
    {
        public string Name { get; set; }
        public double YMin { get; set; }
        public double XMin { get; set; }
        public double YMax { get; set; }
        public double XMax { get; set; }
        public Color LineColor { get; set; }

        public string UnitName { get; set; } // NEW
    }

    public class ScaleAndColorModel
    {
        public string ChannelZero { get; set; }
        public string ChannelOne { get; set; }
        public string ChannelTwo { get; set; }
        public string ChannelThree { get; set; }
        public string ChannelFour { get; set; }
        public string ChannelFive { get; set; }
        public string ChannelSix { get; set; }
        public string ChannelSeven { get; set; }
        public string ChannelEight { get; set; }
        public string ChannelNine { get; set; }
        public string ChannelTen { get; set; }
        public string ChannelEleven { get; set; }
        public string ChannelTwelve { get; set; }
        public string ChannelThirteen { get; set; }
        public string ChannelFourteen { get; set; }

        public string PlotScaleZero { get; set; }
        public string PlotScaleOne { get; set; }
        public string PlotScaleTwo { get; set; }
        public string PlotScaleThree { get; set; }
        public string PlotScaleFour { get; set; }
        public string PlotScaleFive { get; set; }
        public string PlotScaleSix { get; set; }
        public string PlotScaleSeven { get; set; }
        public string PlotScaleEight { get; set; }
        public string PlotScale { get; set; }
        public string PlotScaleNine { get; set; }
        public string PlotScaleTen { get; set; }
        public string PlotScaleEleven { get; set; }
        public string PlotScaleTwelve { get; set; }
        public string PlotScaleThirteen { get; set; }
        public string PlotScaleFourteen { get; set; }

        public string ColorZero { get; set; }
        public string ColorOne { get; set; }
        public string ColorTwo { get; set; }
        public string ColorThree { get; set; }
        public string ColorFour { get; set; }
        public string ColorFive { get; set; }
        public string ColorSix { get; set; }
        public string ColorSeven { get; set; }
        public string ColorEight { get; set; }
        public string ColorNine { get; set; }
        public string ColorTen { get; set; }
        public string ColorEleven { get; set; }
        public string ColorTwelve { get; set; }
        public string ColorThirteen { get; set; }
        public string ColorFourteen { get; set; }

        public string BackgroundColor { get; set; }
        public int? BladderSensation { get; set; }
        public int? GeneralPurose { get; set; }
        public int? ResponeMarkers { get; set; }

        public string NumberOfScreen { get; set; }
    }
    public static class CryptoHelper
    {
        private static readonly string keyString = "MySuperSecretKey123";
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("MyInitVector12345".PadRight(16).Substring(0, 16));

        public static byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] cipherData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (MemoryStream ms = new MemoryStream(cipherData))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }

    public partial class MasterForm : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;



        //Start Code For Graph
        private NationalInstruments.DAQmx.Task analogTask;
        private AnalogMultiChannelReader analogReader;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;

        private const int NumChannels = 7;
        private List<Chart> charts = new List<Chart>();
        //End Code For Graph

        private MenuStrip menuStrip1;
        private ToolStrip toolStrip1;
        private Panel mainContentPanel;
        private Panel mainContentPanelGraphOnly;
        private Panel mainContentPanelPatient;
        private PictureBox graphPictureBox;
        private Panel rightSidebarPanel;
        private Panel rightSidebarPanelForPatient;

        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private Timer updateTimer;
        private Random rand = new Random();

        private Chart liveChart;
        private Button buttonGoLive;
        private readonly ChannelSettings[] channelSettings;


        



        public MasterForm()
        {
            InitializeComponent();
            channelSettings = LoadChannelSettings();

            //Start Code For Graph
            //CreateMainContent();
            InitializeDAQ();
            //InitializeDataStorage();
            //End Code For Graph

            this.WindowState = FormWindowState.Maximized;
            this.Text = "Santron PC based Urodynamics USB, India Visit us at www.santronmeditronic.com, email:santronmeditronic@gmail.com";
        }


        private Panel patientCardsPanel;
        private void MasterForm_Load(object sender, EventArgs e)
        {
            HideMainContent();
            CreateMenuStrip();
            CreateToolStrip();
            //CreateMainContent();
            menuStrip1.Renderer = new CustomMenuRenderer();

            CreateMainContentForPatient();



        }

        private void ShowMainContent()
        {
            if (mainContentPanel == null || rightSidebarPanel == null)
            {
                CreateMainContent();
                CreateRightSidebar();
            }
            else
            {
                mainContentPanel.Visible = true;
                rightSidebarPanel.Visible = true;
            }
        }

        private void HideMainContent()
        {
            if (mainContentPanel != null)
                mainContentPanel.Visible = false;

            if (mainContentPanelGraphOnly != null)
                mainContentPanelGraphOnly.Visible = false;

            if (rightSidebarPanel != null)
                rightSidebarPanel.Visible = false;
        }

        private void btnStartTest_Click(object sender, EventArgs e)
        {
            if (mainContentPanelPatient != null && this.Controls.Contains(mainContentPanelPatient))
            {
                this.Controls.Remove(mainContentPanelPatient);
                mainContentPanelPatient.Dispose();
                mainContentPanelPatient = null;
            }


            CreateMainContent();
            mainContentPanel.Visible = true;
        }

        private void btnGraphOnly_Click(object sender, EventArgs e)
        {
            if (mainContentPanelPatient != null && this.Controls.Contains(mainContentPanelPatient))
            {
                this.Controls.Remove(mainContentPanelPatient);
                mainContentPanelPatient.Dispose();
                mainContentPanelPatient = null;
            }


            CreateMainContentOnlyGraph();
            mainContentPanelGraphOnly.Visible = true;
        }

        private void btnShowPatients_Click(object sender, EventArgs e)
        {
            HideMainContent(); 
            CreateMainContentForPatient(); 
        }

        private void btnForm3_Click(object sender, EventArgs e)
        {
            Form3 graphForm = new Form3();
            graphForm.Show();
        }

        //Start Graph Code
        private string GetScaleAndColorSetupFilePath(string channelZero)
        {
            string exeFolder = Application.StartupPath;
            //string folder = Path.Combine(exeFolder, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, channelZero + ".dat");
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

       

        private void AnalogInCallback(IAsyncResult ar)
        {
            if (analogTask == null || analogTask.IsDone)
                return;

            data = analogReader.EndReadWaveform(ar);

            this.Invoke((MethodInvoker)(() =>
            {
                for (int i = 0; i < channelSettings.Length; i++)
                {
                    UpdateChart(i, channelSettings[i].Name);
                }
            }));

            analogReader.BeginReadWaveform(100, analogCallback, analogTask);
        }


        //Start Code For Graph
        private class ChannelSettings
        {
            public string Name { get; set; }
            public double YMin { get; set; }
            public double XMin { get; set; }
            public double YMax { get; set; }
            public double XMax { get; set; }
            public Color LineColor { get; set; }
        }

        private void InitializeDAQ()
        {
            try
            {
                analogTask = new NationalInstruments.DAQmx.Task();
                analogTask.AIChannels.CreateVoltageChannel(
                    "Dev1/ai0:6", "", AITerminalConfiguration.Rse,
                    -10, 10, AIVoltageUnits.Volts
                );

                analogTask.Timing.ConfigureSampleClock(
                    "",
                    1000,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    1000
                );

                analogReader = new AnalogMultiChannelReader(analogTask.Stream);
                analogCallback = new AsyncCallback(AnalogInCallback);
                analogReader.SynchronizeCallbacks = true;

                analogReader.BeginReadWaveform(100, analogCallback, analogTask);
            }
            catch (DaqException ex)
            {
                // Show message to user
                MessageBox.Show("Machine is not connected. Please check the connection.\n\n" + ex.Message,
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Optionally disable UI that needs the device
                //DisableDAQControls();
            }


        }


       


       

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (analogTask != null)
            {
                analogTask.Stop();
                analogTask.Dispose();
            }
        }


        //private void CreateMainContent()
        //{
        //    mainContentPanel = new Panel();
        //    mainContentPanel.Dock = DockStyle.Fill;
        //    mainContentPanel.BackColor = Color.FromArgb(245, 245, 245);

        //    // Create Right Sidebar first
        //    CreateRightSidebar();

        //    // Parent split container with 2 columns
        //    var contentSplit = new TableLayoutPanel
        //    {
        //        Dock = DockStyle.Fill,
        //        ColumnCount = 2,
        //        RowCount = 1,
        //    };
        //    contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        //    contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

        //    // GRAPH PANEL
        //    var graphContainer = new Panel
        //    {
        //        Dock = DockStyle.Fill,
        //        Padding = new Padding(0, 112, 0, 0),
        //        BackColor = Color.FromArgb(245, 245, 245),
        //        AutoScroll = true
        //    };


        //    // Start Code For Four Buttons
        //    //var toolStrip = new ToolStrip
        //    //{
        //    //    Dock = DockStyle.Top,
        //    //    GripStyle = ToolStripGripStyle.Hidden,
        //    //    BackColor = Color.FromArgb(245, 245, 245),
        //    //    Padding = new Padding(1),
        //    //    AutoSize = false,
        //    //    Height = 30,
        //    //};

        //    //int buttonSpacing = 1;

        //    //ToolStripButton CreateButton(string iconChar, Color backColor, Color foreColor, Size size, EventHandler onClick)
        //    //{
        //    //    var btn = new ToolStripButton
        //    //    {
        //    //        Text = iconChar,
        //    //        Font = new Font("Segoe MDL2 Assets", 12F),
        //    //        AutoSize = false,
        //    //        Size = size,
        //    //        BackColor = backColor,
        //    //        ForeColor = foreColor,
        //    //        DisplayStyle = ToolStripItemDisplayStyle.Text,
        //    //        TextAlign = ContentAlignment.MiddleCenter,
        //    //        Margin = new Padding(2, 0, 2, 0)
        //    //    };
        //    //    if (onClick != null)
        //    //        btn.Click += onClick;

        //    //    return btn;
        //    //}


        //    //toolStrip.Items.Add(CreateButton("\uE160", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 20), ButtonForPatient));
        //    //toolStrip.Items.Add(CreateButton("\uE8B7", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 20), OpenMenuItem_Click));
        //    //toolStrip.Items.Add(CreateButton("\uE749", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 20), null));
        //    //toolStrip.Items.Add(CreateButton("\uE721", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 20), null));
        //    //End Code For Four Buttons



        //    // Add graph table layout to graphContainer
        //    var graphTable = new TableLayoutPanel
        //    {
        //        RowCount = NumChannels,
        //        ColumnCount = 1,
        //        Dock = DockStyle.Fill,
        //        AutoScroll = true
        //    };

        //    for (int i = 0; i < NumChannels; i++)
        //    {
        //        graphTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / NumChannels));

        //        var chart = new Chart
        //        {
        //            Dock = DockStyle.Fill,
        //            BorderlineWidth = 1,
        //            BorderlineColor = Color.Black,
        //        };

        //        var area = new ChartArea();
        //        area.AxisX.MajorGrid.LineColor = Color.LightGray;
        //        area.AxisY.MajorGrid.LineColor = Color.LightGray;
        //        area.AxisY.Minimum = channelSettings[i].YMin;
        //        area.AxisY.Maximum = channelSettings[i].YMax;
        //        chart.ChartAreas.Add(area);

        //        var legend = new Legend($"Legend{i}");
        //        chart.Legends.Add(legend);

        //        var series = new Series($"ai{i}")
        //        {
        //            ChartType = SeriesChartType.Spline,
        //            BorderWidth = 2,
        //            Color = channelSettings[i].LineColor,
        //            Legend = legend.Name,
        //            LegendText = $"{channelSettings[i].Name}: 0.00 V",
        //            MarkerStyle = MarkerStyle.Square,
        //            MarkerSize = 4,
        //            MarkerColor = channelSettings[i].LineColor
        //        };

        //        chart.Series.Add(series);
        //        charts.Add(chart);
        //        graphTable.Controls.Add(chart, 0, i);
        //    }

        //    graphContainer.Controls.Add(graphTable);
        //    //graphContainer.Controls.Add(toolStrip);

        //    // Add both panels to split
        //    contentSplit.Controls.Add(graphContainer, 0, 0);
        //    contentSplit.Controls.Add(rightSidebarPanel, 1, 0);

        //    mainContentPanel.Controls.Add(contentSplit);

        //    // Add main panel to form
        //    this.Controls.Add(mainContentPanel);
        //}



        //private void UpdateChart(int channelIndex, string channelName)
        //{
        //    if (channelIndex >= data.Length)
        //        return;

        //    var chart = charts[channelIndex];
        //    var series = chart.Series[0];
        //    series.Points.Clear();

        //    int sampleCount = Math.Min(100, data[channelIndex].Samples.Count);
        //    for (int i = 0; i < sampleCount; i++)
        //    {
        //        double sample = Convert.ToDouble(data[channelIndex].Samples[i].Value);
        //        int index = series.Points.AddY(sample);

        //        // Label only the last point
        //        if (i == sampleCount - 1)
        //        {
        //            series.Points[index].Label = sample.ToString("0.00") + " V";
        //        }
        //    }

        //    double lastVal = data[channelIndex].Samples[sampleCount - 1].Value;
        //    series.LegendText = $"{channelName}: {lastVal:F2} V";
        //}


        private void CreateMainContent()
        {
            mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(0, 120, 0, 0),
            };

            CreateRightSidebar();

            var contentSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));

            // Chart container
            var graphContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                //Padding = new Padding(10),
                BackColor = Color.White
            };

            // Give initial minimum size
            var sharedChart = new Chart
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(300, 300),
                BorderlineWidth = 1,
                BorderlineColor = Color.Black
            };

            var sharedArea = new ChartArea("SharedArea");
            sharedArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            sharedArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            sharedArea.AxisY.Minimum = channelSettings.Min(c => c.YMin);
            sharedArea.AxisY.Maximum = channelSettings.Max(c => c.YMax);
            sharedChart.ChartAreas.Add(sharedArea);

            for (int i = 0; i < channelSettings.Length; i++)
            {
                var series = new Series($"ai{i}")
                {
                    ChartType = SeriesChartType.Spline,
                    BorderWidth = 2,
                    Color = channelSettings[i].LineColor,
                    ChartArea = "SharedArea",
                    LegendText = $"{channelSettings[i].Name}: 0.00 V",
                    MarkerStyle = MarkerStyle.Square,
                    MarkerSize = 4,
                    MarkerColor = channelSettings[i].LineColor
                };
                sharedChart.Series.Add(series);
            }

            sharedChart.Legends.Add(new Legend());

            charts.Clear();
            charts.Add(sharedChart);

            graphContainer.Controls.Add(sharedChart);
            contentSplit.Controls.Add(graphContainer, 0, 0);
            contentSplit.Controls.Add(rightSidebarPanel, 1, 0);
            contentSplit.ColumnStyles[1].Width = 410;
            mainContentPanel.Controls.Add(contentSplit);
            this.Controls.Add(mainContentPanel);
        }




        private void CreateMainContentOnlyGraph()
        {
            mainContentPanelGraphOnly = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(5, 120, 0, 5),
            };

            var contentSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1350F));
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));

            // Chart container
            var graphContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                //Padding = new Padding(10),
                BackColor = Color.White
            };

            // Give initial minimum size
            var sharedChart = new Chart
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(300, 300),
                BorderlineWidth = 1,
                BorderlineColor = Color.Black
            };

            var sharedArea = new ChartArea("SharedArea");
            sharedArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            sharedArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            sharedArea.AxisY.Minimum = channelSettings.Min(c => c.YMin);
            sharedArea.AxisY.Maximum = channelSettings.Max(c => c.YMax);
            sharedChart.ChartAreas.Add(sharedArea);

            for (int i = 0; i < channelSettings.Length; i++)
            {
                var series = new Series($"ai{i}")
                {
                    ChartType = SeriesChartType.Spline,
                    BorderWidth = 2,
                    Color = channelSettings[i].LineColor,
                    ChartArea = "SharedArea",
                    LegendText = $"{channelSettings[i].Name}: 0.00 V",
                    MarkerStyle = MarkerStyle.Square,
                    MarkerSize = 4,
                    MarkerColor = channelSettings[i].LineColor
                };
                sharedChart.Series.Add(series);
            }

            sharedChart.Legends.Add(new Legend());

            charts.Clear();
            charts.Add(sharedChart);

            graphContainer.Controls.Add(sharedChart);
            contentSplit.Controls.Add(graphContainer, 0, 0);
            mainContentPanelGraphOnly.Controls.Add(contentSplit);
            this.Controls.Add(mainContentPanelGraphOnly);
        }

        private void UpdateChart(int channelIndex, string channelName)
        {
            if (channelIndex >= data.Length || charts.Count == 0)
                return;

            var chart = charts[0]; // Single shared chart
            var series = chart.Series[channelIndex];
            series.Points.Clear();

            int sampleCount = Math.Min(100, data[channelIndex].Samples.Count);
            for (int i = 0; i < sampleCount; i++)
            {
                double sample = Convert.ToDouble(data[channelIndex].Samples[i].Value);
                int index = series.Points.AddY(sample);

                if (i == sampleCount - 1)
                {
                    series.Points[index].Label = sample.ToString("0.00") + " V";
                }
            }

            double lastVal = data[channelIndex].Samples[sampleCount - 1].Value;
            series.LegendText = $"{channelName}: {lastVal:F2} V";
        }


        //End Code For Graph




        private void ButtonForPatient(object sender, EventArgs e)
        {
            Patient_Information patientForm = new Patient_Information();
            patientForm.Show();
        }





        //Start Code For Camera
        private PictureBox cameraPreview;
        private Button captureButton;
        private FlowLayoutPanel capturedImagesPanel;
        private Image dummyImage;
        private Button toggleCameraButton;
        private bool isCameraOn = false;


        private void CreateRightSidebar()
        {
            // Right Sidebar Panel
            //rightSidebarPanel = new Panel();
            //rightSidebarPanel.Width = 450;
            //rightSidebarPanel.Dock = DockStyle.Fill;
            //rightSidebarPanel.BackColor = Color.FromArgb(253, 254, 2);
            //rightSidebarPanel.Padding = new Padding(15);
            //this.Controls.Add(rightSidebarPanel);

            rightSidebarPanel = new Panel
            {
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15)
            };

            // Camera Preview
            cameraPreview = new PictureBox();
            cameraPreview.Size = new Size(404, 270);
            cameraPreview.BackColor = Color.Black;
            cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
            cameraPreview.Location = new Point(0, 0);
            rightSidebarPanel.Controls.Add(cameraPreview);

            // Camera ON/OFF Button
            toggleCameraButton = new Button();
            toggleCameraButton.Text = "🎥 Start Camera";
            toggleCameraButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            toggleCameraButton.BackColor = Color.DarkGreen;
            toggleCameraButton.ForeColor = Color.White;
            toggleCameraButton.FlatStyle = FlatStyle.Flat;
            toggleCameraButton.Size = new Size(200, 40);
            toggleCameraButton.Location = new Point(5, 273);
            toggleCameraButton.Click += ToggleCameraButton_Click;
            rightSidebarPanel.Controls.Add(toggleCameraButton);

            // Capture Button
            captureButton = new Button();
            captureButton.Text = "📸 Capture";
            captureButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            captureButton.BackColor = Color.FromArgb(0, 73, 165);
            captureButton.ForeColor = Color.White;
            captureButton.FlatStyle = FlatStyle.Flat;
            captureButton.Size = new Size(200, 40);
            captureButton.Location = new Point(200, 273);
            captureButton.Click += CaptureButton_Click;
            captureButton.Enabled = false;
            rightSidebarPanel.Controls.Add(captureButton);

            // Thumbnails panel
            capturedImagesPanel = new FlowLayoutPanel();
            capturedImagesPanel.Location = new Point(0, 313);
            capturedImagesPanel.Size = new Size(404, 300);
            capturedImagesPanel.AutoScroll = true;
            capturedImagesPanel.WrapContents = true;
            capturedImagesPanel.FlowDirection = FlowDirection.TopDown;
            rightSidebarPanel.Controls.Add(capturedImagesPanel);
        }


        private void ToggleCameraButton_Click(object sender, EventArgs e)
        {
            if (!isCameraOn)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += (s, args) =>
                    {
                        Bitmap frame = (Bitmap)args.Frame.Clone();
                        cameraPreview.Invoke(new Action(() =>
                        {
                            cameraPreview.Image?.Dispose();
                            cameraPreview.Image = frame;
                        }));
                    };
                    videoSource.Start();
                    isCameraOn = true;
                    toggleCameraButton.Text = "⛔ Stop Camera";
                    toggleCameraButton.BackColor = Color.Maroon;
                    captureButton.Enabled = true;
                }
                else
                {
                    MessageBox.Show("No camera detected.");
                }
            }
            else
            {
                StopCamera();
            }
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            if (cameraPreview.Image != null)
            {
                // Clone image
                Image captured = (Image)cameraPreview.Image.Clone();

                // Create thumbnail
                PictureBox thumbnail = new PictureBox();
                thumbnail.Image = captured;
                thumbnail.Size = new Size(80, 80);
                thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
                thumbnail.Margin = new Padding(5);
                thumbnail.BorderStyle = BorderStyle.FixedSingle;

                capturedImagesPanel.Controls.Add(thumbnail);
            }
        }

        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }
            isCameraOn = false;
            cameraPreview.Image = null;
            toggleCameraButton.Text = "🎥 Start Camera";
            toggleCameraButton.BackColor = Color.DarkGreen;
            captureButton.Enabled = false;
        }
        //End Code For Camera


        //private void CreateRightSidebar()
        //{
        //    // Right Sidebar Panel
        //    rightSidebarPanel = new Panel();
        //    rightSidebarPanel.Width = 320;
        //    rightSidebarPanel.Dock = DockStyle.Right;
        //    rightSidebarPanel.BackColor = Color.FromArgb(248, 249, 250);
        //    rightSidebarPanel.Padding = new Padding(15);

        //    dummyImage = new Bitmap(290, 220);
        //    using (Graphics g = Graphics.FromImage(dummyImage))
        //    {
        //        g.Clear(Color.LightGray);
        //        g.DrawString("Camera Placeholder", new Font("Segoe UI", 14), Brushes.Black, new PointF(10, 90));
        //    }

        //    //dummyImage = Image.FromFile(@"D:\Mitroz 2025 All Projects\Santron Project\Windows App\SantronWinApp\Image\man.png");
        //    string imagePath = Path.Combine(Application.StartupPath, "Images", "man.png");
        //    dummyImage = Image.FromFile(imagePath);

        //    // Camera Preview
        //    cameraPreview = new PictureBox();
        //    cameraPreview.Size = new Size(290, 220);
        //    cameraPreview.BackColor = Color.Black;
        //    cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
        //    cameraPreview.Location = new Point(15, 105);
        //    cameraPreview.Image = dummyImage;
        //    // Later, set camera frame to this

        //    // Capture Button
        //    captureButton = new Button();
        //    captureButton.Text = "📸 Capture";
        //    captureButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        //    captureButton.BackColor = Color.FromArgb(0, 73, 165);
        //    captureButton.ForeColor = Color.White;
        //    captureButton.FlatStyle = FlatStyle.Flat;
        //    captureButton.FlatAppearance.BorderSize = 0;
        //    captureButton.Size = new Size(290, 45);
        //    captureButton.Location = new Point(15, 340);
        //    captureButton.Cursor = Cursors.Hand;
        //    captureButton.Click += CaptureButton_Click;

        //    // Scrollable Thumbnails Panel
        //    capturedImagesPanel = new FlowLayoutPanel();
        //    capturedImagesPanel.Location = new Point(15, 400);
        //    capturedImagesPanel.Size = new Size(290, rightSidebarPanel.Height - 300);
        //    capturedImagesPanel.BackColor = Color.White;
        //    capturedImagesPanel.AutoScroll = true;
        //    capturedImagesPanel.WrapContents = true;
        //    capturedImagesPanel.FlowDirection = FlowDirection.TopDown;
        //    capturedImagesPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        //    // Handle form resize to keep thumbnail panel sized correctly
        //    this.Resize += (sender, e) =>
        //    {
        //        if (capturedImagesPanel != null && rightSidebarPanel != null)
        //        {
        //            capturedImagesPanel.Size = new Size(290, rightSidebarPanel.Height - 300);
        //        }
        //    };

        //    // Add Controls to Sidebar
        //    rightSidebarPanel.Controls.Add(cameraPreview);
        //    rightSidebarPanel.Controls.Add(captureButton);
        //    rightSidebarPanel.Controls.Add(capturedImagesPanel);
        //}


        //private void CaptureButton_Click(object sender, EventArgs e)
        //{



        //    if (dummyImage != null)
        //    {
        //        // Clone the dummy image
        //        Image captured = (Image)dummyImage.Clone();

        //        // Create a thumbnail PictureBox
        //        PictureBox thumbnail = new PictureBox();
        //        thumbnail.Image = captured;
        //        thumbnail.Size = new Size(80, 80);
        //        thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        //        thumbnail.Margin = new Padding(5);
        //        thumbnail.BorderStyle = BorderStyle.FixedSingle;

        //        // Add to the FlowLayoutPanel
        //        capturedImagesPanel.Controls.Add(thumbnail);
        //    }
        //}









        //private void CreateRightSidebar()
        //{
        //    // Right Sidebar Panel
        //    rightSidebarPanel = new Panel();
        //    rightSidebarPanel.Width = 320;
        //    rightSidebarPanel.Dock = DockStyle.Right;
        //    rightSidebarPanel.BackColor = Color.FromArgb(248, 249, 250);
        //    rightSidebarPanel.Padding = new Padding(15);

        //    // Camera Preview
        //    cameraPreview = new PictureBox();
        //    cameraPreview.Size = new Size(290, 220);
        //    cameraPreview.BackColor = Color.Black;
        //    cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
        //    cameraPreview.Location = new Point(15, 105);
        //    cameraPreview.Image = null;

        //    // Initialize camera
        //    videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        //    if (videoDevices.Count > 0)
        //    {
        //        videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
        //        videoSource.NewFrame += (s, e) =>
        //        {
        //            Bitmap frame = (Bitmap)e.Frame.Clone();
        //            cameraPreview.Invoke(new Action(() =>
        //            {
        //                cameraPreview.Image?.Dispose();
        //                cameraPreview.Image = frame;
        //            }));
        //        };
        //        videoSource.Start();
        //    }

        //    // Capture Button
        //    captureButton = new Button();
        //    captureButton.Text = "📸 Capture";
        //    captureButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        //    captureButton.BackColor = Color.FromArgb(0, 73, 165);
        //    captureButton.ForeColor = Color.White;
        //    captureButton.FlatStyle = FlatStyle.Flat;
        //    captureButton.FlatAppearance.BorderSize = 0;
        //    captureButton.Size = new Size(290, 45);
        //    captureButton.Location = new Point(15, 340);
        //    captureButton.Cursor = Cursors.Hand;
        //    captureButton.Click += CaptureButton_Click;

        //    // Scrollable Thumbnails Panel
        //    capturedImagesPanel = new FlowLayoutPanel();
        //    capturedImagesPanel.Location = new Point(15, 400);
        //    capturedImagesPanel.Size = new Size(290, rightSidebarPanel.Height - 300);
        //    capturedImagesPanel.BackColor = Color.White;
        //    capturedImagesPanel.AutoScroll = true;
        //    capturedImagesPanel.WrapContents = true;
        //    capturedImagesPanel.FlowDirection = FlowDirection.TopDown;
        //    capturedImagesPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        //    this.Resize += (sender, e) =>
        //    {
        //        if (capturedImagesPanel != null && rightSidebarPanel != null)
        //        {
        //            capturedImagesPanel.Size = new Size(290, rightSidebarPanel.Height - 300);
        //        }
        //    };

        //    rightSidebarPanel.Controls.Add(cameraPreview);
        //    rightSidebarPanel.Controls.Add(captureButton);
        //    rightSidebarPanel.Controls.Add(capturedImagesPanel);
        //}

        //private void CaptureButton_Click(object sender, EventArgs e)
        //{
        //    if (cameraPreview.Image != null)
        //    {
        //        // Clone the current frame
        //        Image captured = (Image)cameraPreview.Image.Clone();

        //        // Create thumbnail
        //        PictureBox thumbnail = new PictureBox();
        //        thumbnail.Image = captured;
        //        thumbnail.Size = new Size(80, 80);
        //        thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        //        thumbnail.Margin = new Padding(5);
        //        thumbnail.BorderStyle = BorderStyle.FixedSingle;

        //        // Add to panel
        //        capturedImagesPanel.Controls.Add(thumbnail);
        //    }
        //}


        //Start Code For Patient

        private void CreateMainContentForPatient()
        {
            // Create main content panel
            mainContentPanelPatient = new Panel();
            mainContentPanelPatient.Dock = DockStyle.Fill;
            mainContentPanelPatient.BackColor = Color.FromArgb(245, 245, 245);
            mainContentPanelPatient.Padding = new Padding(0, 120, 0, 0);

            // Create graph container
            Panel graphContainer = new Panel();
            graphContainer.Dock = DockStyle.Fill;
            graphContainer.Padding = new Padding(10);
            graphContainer.BackColor = Color.FromArgb(245, 245, 245);

            // Create black placeholder box instead of image
            graphPictureBox = new PictureBox();
            graphPictureBox.Dock = DockStyle.Fill;
            graphPictureBox.BackColor = Color.White; // show only black space
            graphPictureBox.BorderStyle = BorderStyle.FixedSingle;
            graphPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Add the black box
            graphContainer.Controls.Add(graphPictureBox);

            // Create sidebar (patient section)
            CreatePatientRightSidebar();

            // Add both panels to main content
            mainContentPanelPatient.Controls.Add(graphContainer);
            mainContentPanelPatient.Controls.Add(rightSidebarPanelForPatient);

            // Add main content to form
            this.Controls.Add(mainContentPanelPatient);
        }


        private void CreatePatientRightSidebar()
        {
            // ✅ Right sidebar panel with full height
            rightSidebarPanelForPatient = new Panel();
            rightSidebarPanelForPatient.Width = 320; // ✅ Slightly wider for better spacing
            rightSidebarPanelForPatient.Dock = DockStyle.Right;
            rightSidebarPanelForPatient.BackColor = Color.FromArgb(248, 249, 250);
            rightSidebarPanelForPatient.Padding = new Padding(15, 15, 15, 15);

            // ✅ Search Box Container with search button
            Panel searchContainer = new Panel();
            searchContainer.Size = new Size(290, 40);
            searchContainer.Location = new Point(15, 15);
            searchContainer.BackColor = Color.White;
            searchContainer.BorderStyle = BorderStyle.FixedSingle;

            TextBox searchBox = new TextBox();
            searchBox.Text = "Search here";
            searchBox.ForeColor = Color.Gray;
            searchBox.Font = new Font("Segoe UI", 10F);
            searchBox.BorderStyle = BorderStyle.None;
            searchBox.Size = new Size(240, 25);
            searchBox.Location = new Point(10, 8);
            searchBox.BackColor = Color.White;

            // ✅ Search Icon Button
            Button searchButton = new Button();
            searchButton.Text = "🔍";
            searchButton.Font = new Font("Segoe UI", 12F);
            searchButton.Size = new Size(35, 25);
            searchButton.Location = new Point(250, 7);
            searchButton.FlatStyle = FlatStyle.Flat;
            searchButton.FlatAppearance.BorderSize = 0;
            searchButton.BackColor = Color.FromArgb(0, 73, 165);
            searchButton.ForeColor = Color.White;
            searchButton.Cursor = Cursors.Hand;

            searchContainer.Controls.Add(searchBox);
            searchContainer.Controls.Add(searchButton);

            // Search box placeholder behavior
            searchBox.GotFocus += (s, e) =>
            {
                if (searchBox.Text == "Search here")
                {
                    searchBox.Text = "";
                    searchBox.ForeColor = Color.Black;
                }
            };
            searchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    searchBox.Text = "Search here";
                    searchBox.ForeColor = Color.Gray;
                }
            };

            // ✅ Dr. Name Dropdown
            ComboBox drNameCombo = new ComboBox();
            drNameCombo.Text = "Dr. Name";
            drNameCombo.Font = new Font("Segoe UI", 10F);
            drNameCombo.Height = 35;
            drNameCombo.Width = 290;
            drNameCombo.Location = new Point(15, 70);
            drNameCombo.BackColor = Color.White;
            drNameCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            drNameCombo.Items.AddRange(new string[] { "Dr. Smith", "Dr. Johnson", "Dr. Williams", "Dr. Brown" });

            // ✅ Today's Appointments Label
            Label appointmentsLabel = new Label();
            appointmentsLabel.Text = "Today's Appointments";
            appointmentsLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            appointmentsLabel.ForeColor = Color.FromArgb(33, 37, 41);
            appointmentsLabel.Location = new Point(15, 120);
            appointmentsLabel.AutoSize = true;

            // ✅ Scrollable Panel for Patient Cards
            Panel scrollablePanel = new Panel();
            scrollablePanel.Location = new Point(15, 150);
            scrollablePanel.Size = new Size(290, 300); // ✅ Fixed height for scrolling
            scrollablePanel.AutoScroll = true;
            scrollablePanel.BackColor = Color.Transparent;

            // ✅ Patient Card 1
            Panel patientCard1 = CreatePatientCard("John Doe", "ID: P001 • 45 years", "+1234567890", "Dr. Smith", "Male", "Cystometry + Uroflow + EMG", false);
            patientCard1.Location = new Point(0, 0);
            patientCard1.Click += (s, e) => btnGraphOnly_Click(s, e);
            


            // ✅ Patient Card 2 (Selected/Highlighted)
            Panel patientCard2 = CreatePatientCard("John Doe", "ID: P001 • 45 years", "+1234567890", "Dr. Smith", "Male", "UPP + Video", true);
            patientCard2.Location = new Point(0, 85);
            patientCard2.Click += (s, e) => btnStartTest_Click(s, e);

            scrollablePanel.Controls.Add(patientCard1);
            scrollablePanel.Controls.Add(patientCard2);

            // ✅ Add New Patient Button - ANCHORED TO BOTTOM
            Button addPatientBtn = new Button();
            addPatientBtn.Text = "+ Add New Patient";
            addPatientBtn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            addPatientBtn.BackColor = Color.FromArgb(0, 73, 165);
            addPatientBtn.ForeColor = Color.White;
            addPatientBtn.FlatStyle = FlatStyle.Flat;
            addPatientBtn.FlatAppearance.BorderSize = 0;
            addPatientBtn.Size = new Size(290, 45);
            addPatientBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            addPatientBtn.Location = new Point(15, rightSidebarPanelForPatient.Height - 70); // ✅ Position at bottom
            addPatientBtn.Cursor = Cursors.Hand;

            // ✅ Handle form resize to keep button at bottom
            this.Resize += (sender, e) =>
            {
                if (addPatientBtn != null && rightSidebarPanelForPatient != null)
                {
                    addPatientBtn.Location = new Point(15, rightSidebarPanelForPatient.Height - 70);
                }
            };

            addPatientBtn.Click += (s, e) =>
            {
                Patient_Information patientForm = new Patient_Information();
                patientForm.Show();
            };

            // Add all controls to sidebar
            rightSidebarPanelForPatient.Controls.Add(searchContainer);
            rightSidebarPanelForPatient.Controls.Add(drNameCombo);
            rightSidebarPanelForPatient.Controls.Add(appointmentsLabel);
            rightSidebarPanelForPatient.Controls.Add(scrollablePanel);
            rightSidebarPanelForPatient.Controls.Add(addPatientBtn); // ✅ Added last to stay on top
        }

        //private Panel CreatePatientCard(string name, string details, string phone, string doctor, string gender, bool isSelected)
        //{
        //    // ✅ Enhanced patient card with better styling
        //    Panel card = new Panel();
        //    card.Size = new Size(290, 80); // ✅ Slightly taller for better spacing
        //    card.BackColor = isSelected ? Color.FromArgb(230, 240, 255) : Color.White;
        //    card.BorderStyle = BorderStyle.FixedSingle;
        //    card.Padding = new Padding(12, 10, 12, 10);
        //    card.Margin = new Padding(0, 0, 0, 5);

        //    // ✅ Patient icon with better styling
        //    //Label iconLabel = new Label();
        //    //iconLabel.Text = "👤";
        //    //iconLabel.Font = new Font("Segoe UI", 18F);
        //    //iconLabel.Location = new Point(12, 12);
        //    //iconLabel.Size = new Size(30, 30);
        //    //iconLabel.ForeColor = Color.FromArgb(100, 100, 100);

        //    // ✅ Patient name
        //    Label nameLabel = new Label();
        //    nameLabel.Text = name;
        //    nameLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        //    nameLabel.ForeColor = Color.FromArgb(0, 73, 165);
        //    nameLabel.Location = new Point(10, 8);
        //    nameLabel.AutoSize = true;

        //    // ✅ Gender badge with better styling
        //    Label genderLabel = new Label();
        //    genderLabel.Text = gender;
        //    genderLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        //    genderLabel.BackColor = Color.FromArgb(230, 230, 230);
        //    genderLabel.ForeColor = Color.FromArgb(80, 80, 80);
        //    genderLabel.TextAlign = ContentAlignment.MiddleCenter;
        //    genderLabel.Size = new Size(45, 18);
        //    genderLabel.Location = new Point(230, 8);

        //    string[] detailsParts = details.Split('•'); // Assuming format: "ID: P001 • 45 years"
        //    string idPart = detailsParts.Length > 0 ? detailsParts[0].Trim() : "";
        //    string agePart = detailsParts.Length > 1 ? detailsParts[1].Trim() : "";

        //    Label idLabel = new Label();
        //    idLabel.Text = idPart;
        //    idLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        //    idLabel.ForeColor = Color.Black;
        //    idLabel.Location = new Point(10, 28);
        //    idLabel.AutoSize = true;

        //    // ✅ Patient details
        //    Label detailsLabel = new Label();
        //    detailsLabel.Text = details;
        //    detailsLabel.Font = new Font("Segoe UI", 9F);
        //    detailsLabel.ForeColor = Color.FromArgb(120, 120, 120);
        //    detailsLabel.Location = new Point(50, 30);
        //    detailsLabel.AutoSize = true;

        //    // ✅ Phone and doctor info
        //    Label infoLabel = new Label();
        //    infoLabel.Text = $"Phone: {phone}\nDoctor: {doctor}";
        //    infoLabel.Font = new Font("Segoe UI", 8F);
        //    infoLabel.ForeColor = Color.FromArgb(120, 120, 120);
        //    infoLabel.Location = new Point(50, 45);
        //    infoLabel.AutoSize = true;

        //    // ✅ IMPROVED Action buttons with professional styling
        //    Button viewBtn = new Button();
        //    viewBtn.Text = "👁";
        //    viewBtn.Font = new Font("Segoe UI", 12F);
        //    viewBtn.Size = new Size(28, 25);
        //    viewBtn.Location = new Point(230, 45);
        //    viewBtn.FlatStyle = FlatStyle.Flat;
        //    viewBtn.FlatAppearance.BorderSize = 1;
        //    viewBtn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        //    viewBtn.BackColor = Color.White;
        //    viewBtn.ForeColor = Color.FromArgb(80, 80, 80);
        //    viewBtn.Cursor = Cursors.Hand;

        //    // ✅ Hover effect for view button
        //    viewBtn.MouseEnter += (s, e) =>
        //    {
        //        viewBtn.BackColor = Color.FromArgb(240, 240, 240);
        //    };
        //    viewBtn.MouseLeave += (s, e) =>
        //    {
        //        viewBtn.BackColor = Color.White;
        //    };

        //    Button editBtn = new Button();
        //    editBtn.Text = "✏";
        //    editBtn.Font = new Font("Segoe UI", 11F);
        //    editBtn.Size = new Size(28, 25);
        //    editBtn.Location = new Point(260, 45);
        //    editBtn.FlatStyle = FlatStyle.Flat;
        //    editBtn.FlatAppearance.BorderSize = 1;
        //    editBtn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        //    editBtn.BackColor = Color.White;
        //    editBtn.ForeColor = Color.FromArgb(80, 80, 80);
        //    editBtn.Cursor = Cursors.Hand;

        //    // ✅ Hover effect for edit button
        //    editBtn.MouseEnter += (s, e) =>
        //    {
        //        editBtn.BackColor = Color.FromArgb(240, 240, 240);
        //    };
        //    editBtn.MouseLeave += (s, e) =>
        //    {
        //        editBtn.BackColor = Color.White;
        //    };

        //    // Add all controls to card
        //    //card.Controls.Add(iconLabel);
        //    card.Controls.Add(nameLabel);
        //    card.Controls.Add(idLabel);
        //    card.Controls.Add(genderLabel);
        //    card.Controls.Add(detailsLabel);
        //    card.Controls.Add(infoLabel);
        //    card.Controls.Add(viewBtn);
        //    card.Controls.Add(editBtn);

        //    return card;
        //}

        private Panel CreatePatientCard(string name, string details, string phone, string doctor, string gender, string symptoms, bool isSelected)
        {
            // ✅ Enhanced patient card with better styling
            Panel card = new Panel();
            card.Size = new Size(290, 80);
            card.BackColor = isSelected ? Color.FromArgb(230, 240, 255) : Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Padding = new Padding(12, 10, 12, 10);
            card.Margin = new Padding(0, 0, 0, 5);

            // ✅ Patient name
            Label nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            nameLabel.ForeColor = Color.FromArgb(0, 73, 165);
            nameLabel.Location = new Point(10, 8);
            nameLabel.AutoSize = true;

            // ✅ Gender badge
            Label genderLabel = new Label();
            genderLabel.Text = gender;
            genderLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            genderLabel.BackColor = Color.FromArgb(230, 230, 230);
            genderLabel.ForeColor = Color.FromArgb(80, 80, 80);
            genderLabel.TextAlign = ContentAlignment.MiddleCenter;
            genderLabel.Size = new Size(45, 18);
            genderLabel.Location = new Point(230, 8);

            // ✅ Separate ID label (bold)
            string[] detailsParts = details.Split('•'); // Assuming format: "ID: P001 • 45 years"
            string idPart = detailsParts.Length > 0 ? detailsParts[0].Trim() : "";
            string agePart = detailsParts.Length > 1 ? detailsParts[1].Trim() : "";

            Label idLabel = new Label();
            idLabel.Text = idPart;
            idLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            idLabel.ForeColor = Color.Black;
            idLabel.Location = new Point(10, 28);
            idLabel.AutoSize = true;

            // ✅ Age label (normal)
            Label ageLabel = new Label();
            ageLabel.Text = agePart;
            ageLabel.Font = new Font("Segoe UI", 9F);
            ageLabel.ForeColor = Color.FromArgb(120, 120, 120);
            ageLabel.Location = new Point(10, 45);
            ageLabel.AutoSize = true;

            // ✅ Phone and doctor info
            Label infoLabel = new Label();
            infoLabel.Text = $"Phone: {phone}\nDoctor: {doctor}";
            infoLabel.Font = new Font("Segoe UI", 8F);
            infoLabel.ForeColor = Color.FromArgb(120, 120, 120);
            infoLabel.Location = new Point(110, 28);
            infoLabel.AutoSize = true;

            // ✅ Symptoms line
            Label symptomsLabel = new Label();
            symptomsLabel.Text = $"Test : {symptoms}";
            symptomsLabel.Font = new Font("Segoe UI", 9F);
            symptomsLabel.ForeColor = Color.FromArgb(200, 80, 80);
            symptomsLabel.Location = new Point(10, 60);
            symptomsLabel.AutoSize = true;

            // ✅ View button
            Button viewBtn = new Button();
            viewBtn.Text = "👁";
            viewBtn.Font = new Font("Segoe UI", 12F);
            viewBtn.Size = new Size(28, 25);
            viewBtn.Location = new Point(230, 45);
            viewBtn.FlatStyle = FlatStyle.Flat;
            viewBtn.FlatAppearance.BorderSize = 1;
            viewBtn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            viewBtn.BackColor = Color.White;
            viewBtn.ForeColor = Color.FromArgb(80, 80, 80);
            viewBtn.Cursor = Cursors.Hand;
            viewBtn.MouseEnter += (s, e) => viewBtn.BackColor = Color.FromArgb(240, 240, 240);
            viewBtn.MouseLeave += (s, e) => viewBtn.BackColor = Color.White;

            // ✅ Edit button
            Button editBtn = new Button();
            editBtn.Text = "✏";
            editBtn.Font = new Font("Segoe UI", 11F);
            editBtn.Size = new Size(28, 25);
            editBtn.Location = new Point(260, 45);
            editBtn.FlatStyle = FlatStyle.Flat;
            editBtn.FlatAppearance.BorderSize = 1;
            editBtn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            editBtn.BackColor = Color.White;
            editBtn.ForeColor = Color.FromArgb(80, 80, 80);
            editBtn.Cursor = Cursors.Hand;
            editBtn.MouseEnter += (s, e) => editBtn.BackColor = Color.FromArgb(240, 240, 240);
            editBtn.MouseLeave += (s, e) => editBtn.BackColor = Color.White;

            // ✅ Add all controls
            card.Controls.Add(nameLabel);
            card.Controls.Add(genderLabel);
            card.Controls.Add(idLabel);
            card.Controls.Add(ageLabel);
            card.Controls.Add(infoLabel);
            card.Controls.Add(symptomsLabel);
            card.Controls.Add(viewBtn);
            card.Controls.Add(editBtn);

            return card;
        }


        //Start Code For Patient


        // All your existing menu and toolbar methods remain the same...
        private void CreateMenuStrip()
        {
            menuStrip1 = new MenuStrip();
            menuStrip1.BackColor = Color.FromArgb(0, 73, 165);
            menuStrip1.ForeColor = Color.White;
            menuStrip1.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            menuStrip1.AutoSize = false;
            menuStrip1.Height = 32;
            menuStrip1.Dock = DockStyle.Top;
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Renderer = new CustomMenuRenderer();

            //string imagePath = Path.Combine(Application.StartupPath, "Images", "SantronLogo.png");
            string imagePath = AppPathManager.GetFilePath( "Images", "SantronLogo.png");
            Image logoImage = Image.FromFile(imagePath);


            try
            {
                using (WebClient client = new WebClient())
                {
                    using (Stream stream = client.OpenRead(imagePath))
                    {
                        logoImage = Image.FromStream(stream);
                    }
                }

                Image resizedLogo = new Bitmap(logoImage, new Size(100, 23));

                ToolStripLabel logoLabel = new ToolStripLabel
                {
                    Image = resizedLogo,
                    ImageScaling = ToolStripItemImageScaling.None,
                    Margin = new Padding(10, 2, 20, 2),
                    Padding = new Padding(2, 1, 2, 0),
                    IsLink = true
                };

                logoLabel.Click += (s, e) =>
                {
                    btnShowPatients_Click(s, e);
                };

                menuStrip1.Items.Add(logoLabel);
            }
            catch
            {
                ToolStripLabel logoText = new ToolStripLabel
                {
                    Text = "Santron Meditronic",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    Margin = new Padding(15, 5, 25, 5),
                    IsLink = true
                };

                logoText.Click += (s, e) =>
                {
                    btnShowPatients_Click(s, e);
                };

                menuStrip1.Items.Add(logoText);
            }





            string[] menuItems = { "File", "View", "Setup", "Markers", "Infusion", "Test Recording", "Windows", "Help" };
            for (int i = 0; i < menuItems.Length; i++)
            {
                ToolStripMenuItem menuItem = CreateMenuItem(menuItems[i]);
                menuItem.Margin = new Padding(10, 2, 10, 2);
                menuItem.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

                switch (menuItems[i])
                {
                    case "File": AddFileMenuItems(menuItem); break;
                    case "View": AddViewMenuItems(menuItem); break;
                    case "Setup": AddSetupMenuItems(menuItem); break;
                    case "Markers": AddMarkersMenuItems(menuItem); break;
                    case "Infusion": AddInfusionMenuItems(menuItem); break;
                    case "Test Recording": AddTestRecordingMenuItems(menuItem); break;
                    case "Windows": AddWindowsMenuItems(menuItem); break;
                    case "Help": AddHelpMenuItems(menuItem); break;
                }

                menuStrip1.Items.Add(menuItem);
            }
            ToolStripLabel deviceStatus = new ToolStripLabel
            {
                Text = "● Device Connected",
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0, 3, 15, 3),
                Alignment = ToolStripItemAlignment.Right
            };

            deviceStatus.Paint += (sender, e) =>
            {
                Rectangle rect = new Rectangle(0, 0, deviceStatus.Width, deviceStatus.Height);

                // Draw rounded background
                using (var path = GetRoundedRectanglePath(rect, 8))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(Pens.LightGray, path);
                }

                // Prepare text parts
                string dot = "●";
                string text = "Device Connected";

                // Measure the dot width
                SizeF dotSize = e.Graphics.MeasureString(dot, deviceStatus.Font);

                // Vertically center Y
                float y = (deviceStatus.Height - dotSize.Height) / 2;

                // Draw green dot
                e.Graphics.DrawString(dot, deviceStatus.Font, Brushes.Green, new PointF(10, y));

                // Draw remaining text in black
                e.Graphics.DrawString(text, deviceStatus.Font, Brushes.Black, new PointF(10 + dotSize.Width, y));
            };

            menuStrip1.Items.Add(deviceStatus);


            ToolStripButton notificationBtn = new ToolStripButton
            {
                Text = "🔔",
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.White,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(5, 0, 20, 0),
                Alignment = ToolStripItemAlignment.Right
            };
            menuStrip1.Items.Add(notificationBtn);

            this.Controls.Add(menuStrip1);
            this.MainMenuStrip = menuStrip1;
        }


        //private void AddToolStripSeparator(int height = 35)
        //{
        //    ToolStripSeparator sep = new ToolStripSeparator();
        //    sep.AutoSize = false;
        //    sep.Width = 2; // thin vertical line
        //    sep.Height = height;
        //    sep.Margin = new Padding(1, 0, 1, 0);
        //    toolStrip1.Items.Add(sep);
        //}




        //private void AddToolStripSeparator(int height = 30, int thickness = 3, Color? lineColor = null)
        //{
        //    var sep = new ToolStripSeparator
        //    {
        //        AutoSize = false,
        //        Width = thickness, // boldness
        //        Height = height,
        //        Margin = new Padding(1, 0, 1, 0)
        //    };

        //    // Custom painting for bold line
        //    sep.Paint += (s, e) =>
        //    {
        //        Color colorToUse = lineColor ?? Color.Gray;
        //        using (var pen = new Pen(colorToUse, thickness))
        //        {
        //            int x = sep.Width / 2; // center the line
        //            e.Graphics.DrawLine(pen, x, 0, x, sep.Height);
        //        }
        //    };

        //    toolStrip1.Items.Add(sep);
        //}

        private void AddToolStripSeparator(ToolStrip targetToolStrip, int height = 35)
        {
            ToolStripSeparator sep = new ToolStripSeparator();
            sep.AutoSize = false;
            sep.Width = 4;
            sep.Height = height;
            sep.Margin = new Padding(1, 0, 1, 0);

            sep.Paint += (s, e) =>
            {
                e.Graphics.Clear(targetToolStrip.BackColor);
                using (Pen p = new Pen(Color.Gray, 2))
                {
                    int x = sep.Width / 2;
                    e.Graphics.DrawLine(p, x, 2, x, sep.Height - 2);
                }
            };

            targetToolStrip.Items.Add(sep);
        }

        private void CreateToolStrip()
        {
            toolStrip1 = new ToolStrip();
            toolStrip1.Dock = DockStyle.None;
            toolStrip1.Location = new Point(0, 33);
            toolStrip1.Size = new Size(this.Width, 45);
            toolStrip1.BackColor = Color.FromArgb(240, 240, 240);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Padding = new Padding(5, 5, 5, 5);
            toolStrip1.RenderMode = ToolStripRenderMode.System;
            toolStrip1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            int buttonSpacing = 3;

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("FS", Color.FromArgb(253, 254, 2), Color.Black, new Size(34, 35), btnForm3_Click, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("FD", Color.FromArgb(253, 254, 2), Color.Black, new Size(34, 35), btnShowPatients_Click, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("ND", Color.FromArgb(253, 254, 2), Color.Black, new Size(40, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("SD", Color.FromArgb(253, 254, 2), Color.Black, new Size(35, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("BC", Color.FromArgb(253, 254, 2), Color.Black, new Size(35, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("Start\nTest", Color.FromArgb(122, 4, 2), Color.White, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Pause\nTest", Color.FromArgb(122, 4, 2), Color.White, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Stop\nTest", Color.FromArgb(122, 4, 2), Color.White, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Save\nTest", Color.FromArgb(253, 254, 2), Color.Black, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("Pump\nstart", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Speed\n▲", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Speed\n▼", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Pump\nStop", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("GP", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("LE", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("CU", Color.FromArgb(220, 220, 220), Color.Black, new Size(35, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("UDC", Color.FromArgb(220, 220, 220), Color.Black, new Size(50, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("RF", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));

            AddToolStripButton("0", Color.FromArgb(4, 254, 1), Color.Black, new Size(33, 35), ButtonForTwo, 1, new Font("Segoe UI", 18F, FontStyle.Bold));
            AddToolStripButton("Pves\n0", Color.FromArgb(4, 254, 1), Color.Black, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("Pabd\n0", Color.FromArgb(4, 254, 1), Color.Black, new Size(40, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("FLOW\nVol 0", Color.FromArgb(4, 254, 1), Color.Black, new Size(50, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("R1", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("R2", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("M", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("CM", Color.FromArgb(220, 220, 220), Color.Black, new Size(40, 35), Button_CM_Click, 1, new Font("Segoe UI", 15F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("S1", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));
            AddToolStripButton("S2", Color.FromArgb(220, 220, 220), Color.Black, new Size(33, 35), null, 1, new Font("Segoe UI", 15F, FontStyle.Bold));

            AddToolStripSeparator(toolStrip1);
            AddToolStripButton("START\nARM", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("STOP\nARM", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("ARM\nDIR", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 1, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripButton("ARM\nSPEED", Color.FromArgb(5, 253, 251), Color.Black, new Size(45, 35), null, 0, new Font("Segoe UI", 9F, FontStyle.Bold));
            AddToolStripSeparator(toolStrip1);

            this.Controls.Add(toolStrip1);

            this.Resize += (sender, e) =>
            {
                if (toolStrip1 != null)
                {
                    toolStrip1.Width = this.Width;
                }
            };

            //Start Code For 4 shortcuts  Button 
            ToolStrip toolStrip2 = new ToolStrip
            {
                Dock = DockStyle.None,
                Location = new Point(2, toolStrip1.Bottom + 2),
                Size = new Size(this.Width, 35),
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(1),
                AutoSize = false,
                Height = 35,
                CanOverflow = false
            };

            int iconButtonSpacing = 1;

            ToolStripButton CreateIconButton(string iconChar, Color backColor, Color foreColor, Size size, EventHandler onClick)
            {
                var btn = new ToolStripButton
                {
                    Text = iconChar,
                    Font = new Font("Segoe MDL2 Assets", 12F),
                    AutoSize = false,
                    Size = size,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(2, 0, 2, 0)
                };
                if (onClick != null)
                    btn.Click += onClick;

                return btn;
            }

            // Add icon buttons to the second row
            AddToolStripSeparator(toolStrip2, 25);
            toolStrip2.Items.Add(CreateIconButton("\uE160", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 25), ButtonForPatient));
            toolStrip2.Items.Add(CreateIconButton("\uE8B7", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 25), OpenMenuItem_Click));
            toolStrip2.Items.Add(CreateIconButton("\uE749", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 25), null));
            toolStrip2.Items.Add(CreateIconButton("\uE721", Color.FromArgb(220, 220, 220), Color.Black, new Size(30, 25), null));
            AddToolStripSeparator(toolStrip2, 25);
            toolStrip2.Items.Add(CreateIconButton("",Color.FromArgb(220, 220, 220), Color.Black, new Size(2, 2), null));

            // Insert toolStrip2 below toolStrip1
            this.Controls.Add(toolStrip2);

            this.Controls.SetChildIndex(toolStrip2, 0);
            //End Code For 4 shortcuts Button 
        }

        //private void AddToolStripButton(string text, Color backColor, Color foreColor, Size size, EventHandler clickHandler, int rightMargin)
        //{
        //    ToolStripButton button = new ToolStripButton();
        //    button.Text = text;
        //    button.BackColor = backColor;
        //    button.ForeColor = foreColor;
        //    button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        //    button.Size = size;
        //    button.AutoSize = false;
        //    button.TextAlign = ContentAlignment.MiddleCenter;
        //    button.Margin = new Padding(0, 0, rightMargin, 2);
        //    button.DisplayStyle = ToolStripItemDisplayStyle.Text;

        //    if (clickHandler != null)
        //    {
        //        button.Click += clickHandler;
        //    }

        //    button.Paint += (sender, e) =>
        //    {
        //        Rectangle rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
        //        ControlPaint.DrawBorder3D(e.Graphics, rect, Border3DStyle.Raised);

        //        using (Pen shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0)))
        //        {
        //            e.Graphics.DrawLine(shadowPen, 1, rect.Height - 2, rect.Width - 2, rect.Height - 2);
        //            e.Graphics.DrawLine(shadowPen, rect.Width - 2, 1, rect.Width - 2, rect.Height - 2);
        //        }
        //    };

        //    toolStrip1.Items.Add(button);
        //}

        private void AddToolStripButton(string text, Color backColor, Color foreColor, Size size,EventHandler clickHandler, int rightMargin, Font customFont)
        {
            ToolStripButton button = new ToolStripButton
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = customFont ?? new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = size,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, rightMargin, 0), // bottom margin reduced
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };

            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }

            //Code For Button Shado show border type in buttons
            //button.Paint += (sender, e) =>
            //{
            //    Rectangle rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            //    ControlPaint.DrawBorder3D(e.Graphics, rect, Border3DStyle.Raised);

            //    using (Pen shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0)))
            //    {
            //        e.Graphics.DrawLine(shadowPen, 1, rect.Height - 2, rect.Width - 2, rect.Height - 2);
            //        e.Graphics.DrawLine(shadowPen, rect.Width - 2, 1, rect.Width - 2, rect.Height - 2);
            //    }
            //};

            toolStrip1.Items.Add(button);
        }


        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private ToolStripMenuItem CreateMenuItem(string text)
        {
            return new ToolStripMenuItem(text)
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
        }

        private void Button_CM_Click(object sender, EventArgs e)
        {
            ReportComments reportsForm = new ReportComments();
            reportsForm.Show();
        }

        private void ButtonForTwo(object sender, EventArgs e)
        {
            Form2 twoForm = new Form2();
            twoForm.Show();
        }


        // All existing menu methods remain the same...
        private void AddFileMenuItems(ToolStripMenuItem fileMenu)
        {
            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("New");
            newMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newMenuItem.Click += (s, args) =>
            {
                PatientList patientForm = new PatientList();
                patientForm.Show();
            };

            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open..");
            openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openMenuItem.Click += OpenMenuItem_Click;

            ToolStripMenuItem printSetupMenuItem = new ToolStripMenuItem("Print Setup...");
            printSetupMenuItem.Click += PrintSetupMenuItem_Click;

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, args) => Application.Exit();

            fileMenu.DropDownItems.Add(newMenuItem);
            fileMenu.DropDownItems.Add(openMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(printSetupMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);
        }

        private void AddViewMenuItems(ToolStripMenuItem viewMenu)
        {
            ToolStripMenuItem toolBarMenuItem = new ToolStripMenuItem("Toolbar") { CheckOnClick = true, Checked = true };
            ToolStripMenuItem statusBarMenuItem = new ToolStripMenuItem("Status Bar") { CheckOnClick = true, Checked = true };

            viewMenu.DropDownItems.Add(toolBarMenuItem);
            viewMenu.DropDownItems.Add(statusBarMenuItem);
        }

        private void AddSetupMenuItems(ToolStripMenuItem setupMenu)
        {
            ToolStripMenuItem systemMenuItem = new ToolStripMenuItem("System");
            systemMenuItem.Click += (s, args) =>
            {
                PasswordForm systemForm = new PasswordForm();
                systemForm.Show();
            };

            ToolStripMenuItem hospitalMenuItem = new ToolStripMenuItem("Hospital / Doctor Information");
            hospitalMenuItem.Click += (s, args) =>
            {
                HospitalAndDoctorInfoSetUp hospitalForm = new HospitalAndDoctorInfoSetUp();
                hospitalForm.Show();
            };

            ToolStripMenuItem pressureMenuItem = new ToolStripMenuItem("Pressure Flow Study");
            pressureMenuItem.Click += (s, args) =>
            {
                ScaleAndColorSteup pressureStudyForm = new ScaleAndColorSteup();
                pressureStudyForm.Show();
            };

            ToolStripMenuItem docterMenuItem = new ToolStripMenuItem("Docters");
            docterMenuItem.Click += (s, args) =>
            {
                DocterList dicterForm = new DocterList();
                dicterForm.Show();
            };

            ToolStripMenuItem symptomsMenuItem = new ToolStripMenuItem("Symptoms");
            symptomsMenuItem.Click += (s, args) =>
            {
                SymptomsList symptomsForm = new SymptomsList();
                symptomsForm.Show();
            };

            setupMenu.DropDownItems.Add(systemMenuItem);
            setupMenu.DropDownItems.Add(hospitalMenuItem);
            setupMenu.DropDownItems.Add(pressureMenuItem);

            setupMenu.DropDownItems.Add(docterMenuItem);
            setupMenu.DropDownItems.Add(symptomsMenuItem);
        }

        private void AddMarkersMenuItems(ToolStripMenuItem markersMenu)
        {
            ToolStripMenuItem bladderItem = new ToolStripMenuItem("Bladder Sensation");
            bladderItem.DropDownItems.Add("First Sensation (FS)");
            bladderItem.DropDownItems.Add("First Desire (FD)");
            bladderItem.DropDownItems.Add("Normal Desire (ND)");
            bladderItem.DropDownItems.Add("Strong Desire (SD)");
            bladderItem.DropDownItems.Add("Bladder Capacity (BC)");
            bladderItem.DropDownItems.Add("End Filling Phase");
            bladderItem.DropDownItems.Add("Start of Voiding");

            ToolStripMenuItem generalItem = new ToolStripMenuItem("General");
            generalItem.DropDownItems.Add("Leak (L)");
            generalItem.DropDownItems.Add("Cough (C)");
            generalItem.DropDownItems.Add("Laugh (La)");
            generalItem.DropDownItems.Add("Artifact (X)");
            generalItem.DropDownItems.Add("General Purpose");
            generalItem.DropDownItems.Add("Standing");
            generalItem.DropDownItems.Add("Supine");
            generalItem.DropDownItems.Add("Sitting");

            markersMenu.DropDownItems.Add(bladderItem);
            markersMenu.DropDownItems.Add(generalItem);
        }

        private void AddInfusionMenuItems(ToolStripMenuItem infusionMenu)
        {
            infusionMenu.DropDownItems.Add("Start Infusion (STI)");
            infusionMenu.DropDownItems.Add("Stop Infusion (STP)");
            infusionMenu.DropDownItems.Add(new ToolStripSeparator());
            infusionMenu.DropDownItems.Add("Increase Rate");
            infusionMenu.DropDownItems.Add("Decrease Rate");
        }

        private void AddTestRecordingMenuItems(ToolStripMenuItem testRecordingMenu)
        {
            testRecordingMenu.DropDownItems.Add("Start");
            testRecordingMenu.DropDownItems.Add("Pause");
            testRecordingMenu.DropDownItems.Add("Stop");
        }

        private void AddWindowsMenuItems(ToolStripMenuItem windowsMenu)
        {
            windowsMenu.DropDownItems.Add("New Window");
            windowsMenu.DropDownItems.Add("Cascade");
            windowsMenu.DropDownItems.Add("Tile");
            windowsMenu.DropDownItems.Add("Arrange Icons");
            windowsMenu.DropDownItems.Add(new ToolStripSeparator());
            windowsMenu.DropDownItems.Add("Current Open");
        }

        private void AddHelpMenuItems(ToolStripMenuItem helpMenu)
        {
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About Santron UroDynamics...");
            aboutMenuItem.Click += (s, args) =>
            {
                SantronVersion aboutForm = new SantronVersion();
                aboutForm.Show();
            };

            ToolStripMenuItem helpMenuItem = new ToolStripMenuItem("Help Topics");
            helpMenuItem.Enabled = false;
            helpMenuItem.Click += (s, args) => MessageBox.Show("Read Only", "Help");

            helpMenu.DropDownItems.Add(aboutMenuItem);
            helpMenu.DropDownItems.Add(helpMenuItem);
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open File";
                openFileDialog.InitialDirectory = @"D:\";
                openFileDialog.Filter =
                    "All Supported Files (.xlsx;.xls;.pdf;.png;.jpg;.jpeg;.bmp;.txt;.docx)|.xlsx;.xls;.pdf;.png;.jpg;.jpeg;.bmp;.txt;.docx|" +
                    "Excel Files (.xlsx;.xls)|.xlsx;.xls|" +
                    "PDF Files (.pdf)|.pdf|" +
                    "Image Files (.png;.jpg;.jpeg;.bmp)|.png;.jpg;.jpeg;.bmp|" +
                    "Text Files (.txt)|.txt|" +
                    "Word Files (.docx)|.docx|" +
                    "All Files (.)|.";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFile = openFileDialog.FileName;
                    FilePreviewForm previewForm = new FilePreviewForm(selectedFile);
                    previewForm.Show();
                }
            }
        }

        private void PrintSetupMenuItem_Click(object sender, EventArgs e)
        {
            using (var pageSetup = new PageSetupDialog())
            {
                pageSetup.PageSettings = new PageSettings();
                pageSetup.PrinterSettings = new PrinterSettings();

                if (pageSetup.ShowDialog(this) == DialogResult.OK)
                {
                    // Handle page setup changes
                }
            }
        }

       

        public class CustomMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

                // Check if the item is in a DropDown (submenu)
                bool isInDropDown = e.ToolStrip is ToolStripDropDownMenu;

                if (e.Item.Selected)
                {
                    if (isInDropDown)
                    {
                        // Hover background for submenu items
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, 150, 255)), rect);
                    }
                    else
                    {
                        // Hover background for main menu items
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
                else
                {
                    if (!isInDropDown)
                    {
                        // Non-hover main menu background fix (prevent white flash)
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is MenuStrip)
                {
                    // Blue for top-level MenuStrip
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), e.AffectedBounds);
                }
                else
                {
                    // White for dropdown submenus
                    e.Graphics.FillRectangle(Brushes.White, e.AffectedBounds);
                }
            }
        }

        private void ButtonGoLive_Click(object sender, EventArgs e)
        {
            buttonGoLive.BackColor = Color.Green;
            buttonGoLive.Text = "Live";

            liveChart.Visible = true;

            if (updateTimer == null)
            {
                updateTimer = new Timer();
                updateTimer.Interval = 1000; // 1 second
                updateTimer.Tick += UpdateLiveChart;
                updateTimer.Start();
            }
        }


        private void UpdateLiveChart(object sender, EventArgs e)
        {
            foreach (Series s in liveChart.Series)
            {
                if (s.Points.Count > 50)
                    s.Points.RemoveAt(0);

                s.Points.AddY(rand.Next(20, 80)); // Simulated data
            }
        }




    }


}