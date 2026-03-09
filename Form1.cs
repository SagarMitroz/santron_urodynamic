using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using NationalInstruments;

namespace SantronWinApp
{
    public partial class Form1 : Form
    {
        private NationalInstruments.DAQmx.Task analogTask;
        private AnalogMultiChannelReader analogReader;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;

        private const int NumChannels = 7;
        private List<Chart> charts = new List<Chart>();

        public Form1()
        {
            InitializeComponent();
            InitializeDynamicCharts();
            InitializeDAQ();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private class ChannelSettings
        {
            public string Name { get; set; }
            public double YMin { get; set; }
            public double YMax { get; set; }
            public Color LineColor { get; set; }
        }

        private readonly ChannelSettings[] channelSettings = new[]
        {
            new ChannelSettings { Name = "PVES", YMin = 0, YMax = 5, LineColor = Color.Blue },
            new ChannelSettings { Name = "PABD", YMin = 0, YMax = 5, LineColor = Color.Red },
            new ChannelSettings { Name = "QVOL", YMin = 0, YMax = 5, LineColor = Color.Black },
            new ChannelSettings { Name = "VINF", YMin = 0, YMax = 5, LineColor = Color.SkyBlue },
            new ChannelSettings { Name = "EMG", YMin = 0, YMax = 5, LineColor = Color.Orange },
            new ChannelSettings { Name = "UPP", YMin = 0, YMax = 5, LineColor = Color.Yellow },
            new ChannelSettings { Name = "PURA", YMin = 0, YMax = 5, LineColor = Color.Green }
        };

        private void InitializeDynamicCharts()
        {
            var table = new TableLayoutPanel
            {
                RowCount = NumChannels,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                AutoScroll = true,
            };

            for (int i = 0; i < NumChannels; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / NumChannels));

                var chart = new Chart
                {
                    Dock = DockStyle.Fill,
                    BorderlineWidth = 1,
                    //BorderlineColor = System.Drawing.Color.Black,
                    BorderlineColor = Color.Black,
                };

                var area = new ChartArea();
                area.AxisX.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.Minimum = channelSettings[i].YMin;
                area.AxisY.Maximum = channelSettings[i].YMax;
                chart.ChartAreas.Add(area);

                var legend = new Legend($"Legend{i}");
                chart.Legends.Add(legend);

                var series = new Series($"ai{i}")
                {
                    ChartType = SeriesChartType.Spline,
                    BorderWidth = 2,
                    Color = channelSettings[i].LineColor,
                    Legend = legend.Name,
                    LegendText = $"{channelSettings[i].Name}: 0.00 V",
                    MarkerStyle = MarkerStyle.Square,
                    MarkerSize = 4,
                    MarkerColor = channelSettings[i].LineColor
                };

                chart.Series.Add(series);

                charts.Add(chart);
                table.Controls.Add(chart, 0, i);
            }

            this.Controls.Add(table);
        }

        private void InitializeDAQ()
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

        private void AnalogInCallback(IAsyncResult ar)
        {
            if (analogTask == null || analogTask.IsDone)
                return;

            data = analogReader.EndReadWaveform(ar);

            this.Invoke((MethodInvoker)(() =>
            {
                for (int ch = 0; ch < data.Length; ch++)
                {
                    var chart = charts[ch];
                    var series = chart.Series[0];
                    series.Points.Clear();

                    int sampleCount = Math.Min(100, data[ch].Samples.Count);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        double sample = Convert.ToDouble(data[ch].Samples[i].Value);
                        int index = series.Points.AddY(sample);

                        // Label only the last point
                        if (i == sampleCount - 1)
                        {
                            series.Points[index].Label = sample.ToString("0.00") + " V";
                        }
                    }

                    // Update legend with current value
                    double lastVal = data[ch].Samples[sampleCount - 1].Value;
                    series.LegendText = $"{channelSettings[ch].Name}: {lastVal:F2} V";
                }
            }));

            analogReader.BeginReadWaveform(100, analogCallback, analogTask);
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
    }
}
