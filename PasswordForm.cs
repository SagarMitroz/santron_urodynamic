using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SantronWinApp
{

    public partial class PasswordForm : Form
    {

        private const string StaticPassword = "12345";

        // 🔴 Add this event to notify when SystemSetup saves data
        public event EventHandler SystemSetupSaved;

        public PasswordForm()
        {
            InitializeComponent();
            //this.Shown += PasswordForm_Shown;
        }

        //private void PasswordForm_Shown(object sender, EventArgs e)
        //{
        //    textBox1.Focus();
        //    textBox1.SelectionStart = textBox1.TextLength; 
        //}


        private void PasswordForm_Load(object sender, EventArgs e)
        {
            textBox1.PasswordChar = '*';
            //BeginInvoke(new Action(() =>
            //{
            //    this.ActiveControl = textBox1;
            //    textBox1.Focus();
            //    textBox1.Select();
            //}));
            textBox1.Focus();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == StaticPassword)
            {
               
                //SystemSetup setupForm = new SystemSetup();
                //setupForm.Show();

                var setupForm = new SystemSetup();

                // 🔴 Subscribe to the DataSaved event
                setupForm.DataSaved += (s, ev) =>
                {
                    // When SystemSetup saves data, raise our own event
                    SystemSetupSaved?.Invoke(this, EventArgs.Empty);
                };

                ScreenDimOverlay.ShowDialogWithDim(setupForm, alpha: 150);

                this.Close(); 
            }
            else
            {
                // Password is incorrect
                MessageBox.Show("Incorrect password. Please try again.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Clear();
                textBox1.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
           if(e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button1.PerformClick();
            }
        }
    }
}