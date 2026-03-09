using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SantronWinApp
{
    public partial class NotificationForm : Form
    {
        Timer timer = new Timer();
        private Button btnOk;
        //public NotificationForm(string message, int durationMs = 1000)
        //{
        //    InitializeComponent();
        //    this.FormBorderStyle = FormBorderStyle.None;
        //    this.StartPosition = FormStartPosition.CenterScreen;
        //    this.BackColor = Color.FromArgb(50, 205, 50); // Sweet green
        //    this.Opacity = 0.95;
        //    this.Width = 300;
        //    this.Height = 100;

        //    Label lbl = new Label();
        //    lbl.Text = message;
        //    lbl.ForeColor = Color.White;
        //    lbl.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        //    lbl.AutoSize = false;
        //    lbl.TextAlign = ContentAlignment.MiddleCenter;
        //    lbl.Dock = DockStyle.Fill;
        //    this.Controls.Add(lbl);

        //    // Auto-close timer
        //    timer.Interval = durationMs;
        //    timer.Tick += (s, e) => { this.Close(); };
        //    timer.Start();
        //}

        public NotificationForm(string message, int durationMs = 4000)
        {
            InitializeComponent();

            // 🔹 Basic Window Settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 350;
            this.Height = 150;
            this.Opacity = 0.97;
            this.BackColor = Color.MediumSpringGreen;
            this.ShowInTaskbar = false;

            // 🔹 Rounded Corners
            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, this.Width, this.Height, 25, 25)
            );

            // 🔹 Drop shadow effect
            this.Paint += (s, e) =>
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(-10, -10, this.Width + 20, this.Height + 20);
                    using (PathGradientBrush brush = new PathGradientBrush(path))
                    {
                        brush.CenterColor = Color.FromArgb(50, Color.Black);
                        brush.SurroundColors = new[] { Color.Transparent };
                        e.Graphics.FillRectangle(brush, this.ClientRectangle);
                    }
                }
            };

            // 🔹 Gradient Background
            this.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(245, 246, 250),
                    Color.FromArgb(200, 200, 200),
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            };

            // 🔹 Message Label
            Label lbl = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lbl);

            // 🔹 OK Button
            btnOk = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 35),
                Location = new Point((this.Width - 80) / 2, 95),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => this.Close();
            this.Controls.Add(btnOk);

            // 🔹 Auto-close timer
            timer.Interval = durationMs;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        // For rounded corners
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private void NotificationForm_Load(object sender, EventArgs e)
        {

        }
    }
}
