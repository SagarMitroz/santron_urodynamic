namespace SantronWinApp
{
    partial class Default
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        /// 
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;



        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Default
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "Default";
            this.Text = "Default";
            this.Load += new System.EventHandler(this.Default_Load);

            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series = new System.Windows.Forms.DataVisualization.Charting.Series();

            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();

            // Chart area
            chartArea.Name = "MainArea";
            this.chart1.ChartAreas.Add(chartArea);

            // Chart series
            series.ChartArea = "MainArea";
            series.Name = "MachineData";
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            this.chart1.Series.Add(series);

            // Chart layout
            this.chart1.Location = new System.Drawing.Point(40, 40);
            this.chart1.Size = new System.Drawing.Size(700, 300);
            this.chart1.Name = "chart1";
            this.chart1.Visible = false; // hide by default

            this.Controls.Add(this.chart1);

            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
        }

        #endregion
    }
}