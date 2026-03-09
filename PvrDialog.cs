// PvrDialog.cs (simple modal)
using System;
using System.Windows.Forms;
using NationalInstruments.DAQmx;

public sealed class PvrDialog : Form
{
    private Label lbl;
    public double ResidualMl { get; private set; }

    public PvrDialog()
    {
        Text = "Measure PVR";
        Width = 300; Height = 140;
        lbl = new Label { Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = new System.Drawing.Font("Segoe UI", 12) };
        Controls.Add(lbl);
    }

    // Measure channel ‘ai2’ for 500 ms, average, then convert using (rawCounts - offset)/slope
    public bool Measure(string aiChan = "Dev1/ai2", int ms = 500, double offsetCounts = 0, double slope = 1.0)
    {
        try
        {
            using (var t = new Task("PVR_AI"))
            {
                t.AIChannels.CreateVoltageChannel(aiChan, "", AITerminalConfiguration.Rse, -10, 10, AIVoltageUnits.Volts);
                t.Timing.ConfigureSampleClock("", 1000, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, Math.Max(1, ms));
                var reader = new AnalogSingleChannelReader(t.Stream);
                double[] v = reader.ReadMultiSample(Math.Max(1, ms));
                double sumCounts = 0;
                for (int i = 0; i < v.Length; i++)
                    sumCounts += v[i] * 4095.0 / 10.0;

                double avgCounts = sumCounts / v.Length;
                double ml = (avgCounts - offsetCounts) / (Math.Abs(slope) < 1e-9 ? 1.0 : slope);
                ResidualMl = ml < 0 ? 0 : ml;

                lbl.Text = "Residual: " + ResidualMl.ToString("0.0") + " ml";
                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("PVR measure failed:\n" + ex.Message, "PVR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
