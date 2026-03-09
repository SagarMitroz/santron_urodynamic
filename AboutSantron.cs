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
    public partial class AboutSantron : Form
    {
        public AboutSantron()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }


        private void AboutSantron_Load(object sender, EventArgs e)
        {
            this.BackColor = System.Drawing.ColorTranslator.FromHtml("#D8E3F9");

            button1.BackColor = ColorTranslator.FromHtml("#25459D");
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderSize = 0;
            button1.ForeColor = Color.White;  
            button1.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            label1.ForeColor = ColorTranslator.FromHtml("#25459D");  
            label6.ForeColor = ColorTranslator.FromHtml("#25459D");
            label3.ForeColor = ColorTranslator.FromHtml("#25459D");
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label6_Click(object sender, EventArgs e)
        {
            
        }

        private void label3_Click(object sender, EventArgs e)
        {
             

        }

        private void label4_Click(object sender, EventArgs e)
        {
            
        }
    }
}
