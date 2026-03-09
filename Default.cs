using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SantronWinApp
{
    public partial class Default : Form
    {
        private Timer updateTimer;
        private Random rand = new Random();

        public Default()
        {
            InitializeComponent(); // ✅ This creates chart1 and other controls
        }

        private void Default_Load(object sender, EventArgs e)
        {
            chart1.Visible = true; // 👈 Now it will actually exist and show

            updateTimer = new Timer();
            updateTimer.Interval = 2000; // 2 seconds
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            int value = rand.Next(20, 60);

            var series = chart1.Series["MachineData"];
            if (series.Points.Count > 20)
                series.Points.RemoveAt(0);

            series.Points.AddY(value);
        }
    }
}
