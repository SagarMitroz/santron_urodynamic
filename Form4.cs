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

namespace SantronWinApp
{
    public partial class Form4 : Form
    {
        private Chart chart;
        private TableLayoutPanel chartTable;
        private FlowLayoutPanel buttonsPanel;
        private Timer updateTimer;

        // Data structures
        private readonly string[] channelNames = { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qura", "EMG" };
        private readonly Color[] channelColors = { Color.Blue, Color.Red, Color.Black, Color.Purple, Color.Orange, Color.DeepSkyBlue, Color.Green };
        private readonly double[] yMin = { 0, 0, 0, 0, 0, 0, 0 };
        private readonly double[] yMax = { 100, 100, 100, 1000, 500, 25, 2000 };
        private readonly string[] channelUnits = { "cmH2O", "cmH2O", "cmH2O", "ml", "ml", "ml/sec", "uV" };

        private readonly Chart[] charts;
        private int currentX = 0;
        private readonly int windowSize = 120;

        public Form4()
        {
            this.Text = "Multi-channel Chart (Urodynamics Style)";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.White;

            charts = new Chart[channelNames.Length];

            InitializeLayout();
            BuildCharts();
            BuildButtons();
            AddEventMarkers();
            StartSimulation();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
           
        }

        private void InitializeLayout()
        {
            // Left panel for buttons
            buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 160,
                Padding = new Padding(8),
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            this.Controls.Add(buttonsPanel);

            // Chart area - stack charts vertically
            chartTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = channelNames.Length,
                Padding = new Padding(4)
            };

            // each row gets equal height
            chartTable.RowStyles.Clear();
            for (int i = 0; i < channelNames.Length; i++)
            {
                chartTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / channelNames.Length));
            }

            this.Controls.Add(chartTable);
        }

        private void BuildCharts()
        {
            for (int i = 0; i < channelNames.Length; i++)
            {
                var ch = new Chart
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };

                var area = new ChartArea("area_" + i)
                {
                    BackColor = Color.WhiteSmoke,
                };

                // X axis grid style (dotted)
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MajorGrid.LineColor = Color.LightGray;
                area.AxisX.LabelStyle.Enabled = (i == channelNames.Length - 1); // only bottom chart shows X labels
                area.AxisX.MajorTickMark.Enabled = false;
                area.AxisX.MinorGrid.Enabled = false;
                area.AxisX.Interval = 10;

                // Y axis style for each channel
                area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisY.MajorGrid.LineColor = Color.LightGray;
                area.AxisY.LabelStyle.ForeColor = channelColors[i];
                area.AxisY.Title = channelUnits[i] + "  " + channelNames[i]; // "unit  name"
                area.AxisY.TitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
                area.AxisY.TitleForeColor = channelColors[i];
                area.AxisY.Minimum = yMin[i];
                area.AxisY.Maximum = yMax[i];

                // small margin so line doesn't touch top/bottom
                area.AxisY.IsMarginVisible = true;

                ch.ChartAreas.Add(area);

                // Series
                var s = new Series(channelNames[i])
                {
                    ChartType = SeriesChartType.Line,
                    Color = channelColors[i],
                    BorderWidth = 2,
                    ChartArea = area.Name
                };

                ch.Series.Add(s);

                // disable legend (clean look like screenshot)
                ch.Legends.Clear();

                // Add chart to layout
                charts[i] = ch;
                chartTable.Controls.Add(ch);
                chartTable.SetRow(ch, i);
            }
        }

        private void BuildButtons()
        {
            Label lbl = new Label
            {
                Text = "Channels",
                AutoSize = false,
                Width = buttonsPanel.Width - 16,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            buttonsPanel.Controls.Add(lbl);

            // Add toggle button for each channel
            for (int i = 0; i < channelNames.Length; i++)
            {
                int index = i;
                var btn = new Button
                {
                    Text = channelNames[i] + "  (ON)",
                    Tag = index,
                    Width = buttonsPanel.Width - 24,
                    Height = 30,
                    BackColor = ControlPaint.Light(channelColors[i]),
                    ForeColor = Color.Black,
                    Margin = new Padding(4)
                };
                btn.Click += (s, e) =>
                {
                    ToggleChannel(index, btn);
                };
                buttonsPanel.Controls.Add(btn);
            }

            // Show All / Hide All
            var showAll = new Button
            {
                Text = "Show All",
                Width = buttonsPanel.Width - 24,
                Height = 30,
                Margin = new Padding(4)
            };
            showAll.Click += (s, e) => SetAllChannelsVisible(true);
            buttonsPanel.Controls.Add(showAll);

            var hideAll = new Button
            {
                Text = "Hide All",
                Width = buttonsPanel.Width - 24,
                Height = 30,
                Margin = new Padding(4)
            };
            hideAll.Click += (s, e) => SetAllChannelsVisible(false);
            buttonsPanel.Controls.Add(hideAll);
        }

        private void ToggleChannel(int index, Button btn)
        {
            charts[index].Visible = !charts[index].Visible;
            btn.Text = channelNames[index] + (charts[index].Visible ? "  (ON)" : "  (OFF)");
            btn.BackColor = charts[index].Visible ? ControlPaint.Light(channelColors[index]) : ControlPaint.LightLight(Color.Gray);
        }

        private void SetAllChannelsVisible(bool visible)
        {
            // iterate buttons in the left panel to update text too
            foreach (Control c in buttonsPanel.Controls)
            {
                if (c is Button btn && btn.Tag is int idx)
                {
                    charts[idx].Visible = visible;
                    btn.Text = channelNames[idx] + (visible ? "  (ON)" : "  (OFF)");
                    btn.BackColor = visible ? ControlPaint.Light(channelColors[idx]) : ControlPaint.LightLight(Color.Gray);
                }
            }
        }

        private void AddEventMarkers()
        {
            // Example event positions and colors/labels
            var events = new (int pos, string label, Color color)[]
            {
                (10, "BC", Color.Magenta),
                (28, "Le", Color.Green),
                (33, "Le", Color.Green),
                (38, "FD", Color.Magenta),
                (62, "R2", Color.Green),
                (70, "R1", Color.Green)
            };

            foreach (var c in charts)
            {
                var area = c.ChartAreas[0];
                area.AxisX.StripLines.Clear();
                foreach (var e in events)
                {
                    var sl = new StripLine
                    {
                        IntervalOffset = e.pos,
                        BorderColor = e.color,
                        BorderWidth = 2,
                        BorderDashStyle = ChartDashStyle.Solid,
                        Text = e.label,
                        TextAlignment = StringAlignment.Near,
                        TextLineAlignment = StringAlignment.Near,
                        ForeColor = e.color,
                        BackColor = Color.Transparent
                    };
                    area.AxisX.StripLines.Add(sl);
                }
            }
        }

        private void StartSimulation()
        {
            updateTimer = new Timer { Interval = 120 }; // ms
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private readonly Random rnd = new Random();

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            currentX++;

            for (int i = 0; i < charts.Length; i++)
            {
                var chart = charts[i];

                // Always generate data but it will only be visible if chart.Visible == true
                var series = chart.Series[0];

                // Generate a waveform per channel type (sin/cos + noise), scaled to Y range
                double range = yMax[i] - yMin[i];
                double phase = (i + 1) * 0.5;
                double baseSignal = 0.5 * (1 + Math.Sin(currentX * 0.05 + phase)); // 0..1
                double noise = (rnd.NextDouble() - 0.5) * 0.05; // small noise
                double value = yMin[i] + (baseSignal + noise) * range;

                // For flow (Qura) we might want a pulse-like shape, so add occasional peaks
                if (channelNames[i] == "Qura")
                {
                    value = yMin[i] + Math.Max(0, Math.Sin(currentX * 0.15 + phase)) * range * 0.7 + rnd.NextDouble() * range * 0.05;
                }

                // Add point
                series.Points.AddXY(currentX, value);

                // Keep points count reasonable
                if (series.Points.Count > 1000)
                    series.Points.RemoveAt(0);

                // Set X window for this chart's area (scrolling)
                var area = chart.ChartAreas[0];
                area.AxisX.Minimum = Math.Max(0, currentX - windowSize);
                area.AxisX.Maximum = currentX;

                // Optionally, update X axis major grid / ticks to match the screenshot look
                area.AxisX.MajorGrid.LineColor = Color.LightGray;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            }
        }

        // Optional: stop timer when form closes
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            updateTimer?.Stop();
            base.OnFormClosed(e);
        }

        //[STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.Run(new Form4());
        //}
    }
}
