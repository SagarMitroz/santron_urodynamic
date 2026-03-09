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
    public partial class splash : Form
    {
        Timer splashTimer;
        public splash()
        {
            InitializeComponent();
        }

      
        private void splash_Load(object sender, EventArgs e)
        {
            //Form3 graphForm = new Form3();
            //graphForm.Show();

            splashTimer = new Timer();
            splashTimer.Interval = 2000; // 5000 ms = 5 seconds
            splashTimer.Tick += SplashTimer_Tick;
            splashTimer.Start();
        }

        private void SplashTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer so it doesn't trigger again
            splashTimer.Stop();

            //Form3 graphForm = new Form3();
            //graphForm.Show();

            // Show MainForm
            MainForm graphForm = new MainForm();
            graphForm.Show();

            // Close splash screen
            this.Hide();
        }
    }
}
