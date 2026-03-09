using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;


namespace SantronWinApp
{
    public partial class FilePreviewForm : Form
    {
        private Chart chart1;

        public FilePreviewForm(string filePath)
        {
            InitializeComponent();

            chart1 = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            this.Controls.Add(chart1);

            var chartArea = new ChartArea("MainArea");
            chart1.ChartAreas.Add(chartArea);
            chart1.Legends.Add(new Legend("Legend"));

            // 1️⃣ Try reading as CSV
            if (Path.GetExtension(filePath).ToLower() == ".csv")
            {
                LoadCsvFile(filePath);
            }
            else
            {
                // 2️⃣ Try reading as UFF binary
                LoadUffBinaryFile(filePath);
            }
        
        }

        private void FilePreviewForm_Load(object sender, EventArgs e)
        {

        }

        private void LoadCsvFile(string filePath)
        {
            var time = new List<double>();
            var Pves = new List<double>();
            var Pabd = new List<double>();
            var Pdet = new List<double>();

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');

                if (parts.Length < 4) continue;

                if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double t) &&
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double pves) &&
                    double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double pabd) &&
                    double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double pdet))
                {
                    time.Add(t);
                    Pves.Add(pves);
                    Pabd.Add(pabd);
                    Pdet.Add(pdet);
                }
            }

            if (time.Count == 0)
            {
                MessageBox.Show("No valid data found in this CSV file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PlotGraph(time, Pves, Pabd, Pdet);
        }


        private void PlotGraph(List<double> time, List<double> Pves, List<double> Pabd, List<double> Pdet)
        {
            chart1.Series.Clear();

            var seriesPves = new Series("Pves")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Blue,
                BorderWidth = 2
            };

            var seriesPabd = new Series("Pabd")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 2
            };

            var seriesPdet = new Series("Pdet")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Black,
                BorderWidth = 2
            };

            for (int i = 0; i < time.Count; i++)
            {
                seriesPves.Points.AddXY(time[i], Pves[i]);
                seriesPabd.Points.AddXY(time[i], Pabd[i]);
                seriesPdet.Points.AddXY(time[i], Pdet[i]);
            }

            chart1.Series.Add(seriesPves);
            chart1.Series.Add(seriesPabd);
            chart1.Series.Add(seriesPdet);

            chart1.ChartAreas[0].AxisX.Title = "Time (s)";
            chart1.ChartAreas[0].AxisY.Title = "Pressure (cmH2O)";
        }

        private void LoadUffBinaryFile(string filePath)
        {
            var time = new List<double>();
            var Pves = new List<double>();
            var Pabd = new List<double>();
            var Pdet = new List<double>();

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    long totalBytes = fs.Length;
                    const int recordSize = 4 * 4;

                    while (fs.Position + recordSize <= totalBytes)
                    {
                        float t = br.ReadSingle();
                        float pves = br.ReadSingle();
                        float pabd = br.ReadSingle();
                        float pdet = br.ReadSingle();

                        if (float.IsNaN(t) || float.IsInfinity(t) ||
                            float.IsNaN(pves) || float.IsInfinity(pves) ||
                            float.IsNaN(pabd) || float.IsInfinity(pabd) ||
                            float.IsNaN(pdet) || float.IsInfinity(pdet))
                        {
                            continue;
                        }

                        if (t < 0 || t > 10000 || Math.Abs(pves) > 500 || Math.Abs(pabd) > 500 || Math.Abs(pdet) > 500)
                        {
                            continue;
                        }

                        time.Add(t);
                        Pves.Add(pves);
                        Pabd.Add(pabd);
                        Pdet.Add(pdet);
                    }
                }

                if (time.Count == 0)
                {
                    MessageBox.Show("No valid binary records found in this UFF file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                PlotGraph(time, Pves, Pabd, Pdet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read UFF file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
