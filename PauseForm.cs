using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SantronWinApp
{
    public partial class PauseForm : Form
    {
        public PauseForm()
        {
            //InitializeComponent();

            this.Size = new Size(200, 150);
            this.Text = "Recording Paused";
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                Text = "Recording Paused. Click OK to resume.",
                Location = new Point(20, 20),
                Size = new Size(160, 30)
            };

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(60, 55),
                Size = new Size(75, 23)
            };
            okButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { label, okButton });

        }

        private void PauseForm_Load(object sender, EventArgs e)
        {

        }
    }
}
