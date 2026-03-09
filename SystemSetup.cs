using Mysqlx.Crud;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.Patient_Information;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace SantronWinApp
{
    public partial class SystemSetup : Form
    {
        private bool isEditMode;
        private string pves;

        public SystemSetup()
        {
            InitializeComponent();
            this.Load += SystemSetup_Load;

            this.pves = pves;

            this.MaximizeBox = false;                 
            //this.FormBorderStyle = FormBorderStyle.FixedSingle;   
        }

        private void textPves_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

       
        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }
        
        private void SystemSetup_Load(object sender, EventArgs e)
        {
            LoadSystemSetupData(pves);


            //Start Code For Auto Load data
          //  string folder = Path.Combine(Application.StartupPath, "Saved Data", "SystemSetup");
            string folder = AppPathManager.GetFolderPath("SystemSetup");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.dat");
                if (files.Length > 0)
                {
                    // Load the first available setup file
                    string fileName = Path.GetFileNameWithoutExtension(files[0]);
                    LoadSystemSetupData(fileName);
                }
            }
            //End Code For Auto Load data

            //this.BackColor = ColorTranslator.FromHtml("#D8E3F9");

            // Style the button buttonColor0
            //buttonCancel.FlatStyle = FlatStyle.Flat;
            //buttonCancel.BackColor = ColorTranslator.FromHtml("#FFFFFF");
            //buttonCancel.ForeColor = Color.Black;
            //buttonCancel.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#FFFFFF");
            //buttonCancel.FlatAppearance.BorderSize = 1;  // or 0 if you want no border
            //buttonCancel.Font = new Font("Segoe UI", 8, FontStyle.Bold);


            // Style the button buttonColor0
            //buttonOk.FlatStyle = FlatStyle.Flat;
            //buttonOk.BackColor = ColorTranslator.FromHtml("#25459D");
            //buttonOk.ForeColor = Color.White;
            //buttonOk.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#25459D");
            //buttonOk.FlatAppearance.BorderSize = 1;  // or 0 if you want no border
            //buttonOk.Font = new Font("Segoe UI", 8, FontStyle.Bold);

        }

        private void textEmg_TextChanged(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // Create RichTextBox
            RichTextBox headingBox = new RichTextBox();
            headingBox.Text = "System Setup";
            headingBox.ReadOnly = true; // Prevent user editing
            headingBox.BorderStyle = BorderStyle.None; // Optional: No border
            headingBox.BackColor = Color.White;
            headingBox.ForeColor = Color.Black;
            headingBox.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            headingBox.Height = 50;
            headingBox.Dock = DockStyle.Top;

            // Center align the text
            headingBox.SelectAll(); // Select all text before applying alignment
            headingBox.SelectionAlignment = HorizontalAlignment.Center;

            // Add to form
            this.Controls.Add(headingBox);
        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public class SystemSetupModel
        {
            public string Pves { get; set; }
            public string Pabd { get; set; }
            public string Pura { get; set; }
            public string Point { get; set; }
            public string Vinf { get; set; }
            public string Flow { get; set; }
            public string Rate { get; set; }
            public string EMG { get; set; }
            public string ChangeBottle { get; set; }
            public string P1 { get; set; }
            public string P2 { get; set; }
            public string P3 { get; set; }
            public string JF { get; set; }
            public string UPP { get; set; }
            public string UPPH { get; set; }
            public string UPPL { get; set; }
            public string U1 { get; set; }
            public string SpGravity { get; set; }
            public string DefualInfusion { get; set; }
            public string Constant1 { get; set; }
            public string Constant2 { get; set; }
            public bool PreTest { get; set; }
            public bool PVR { get; set; }
            public bool Comments { get; set; }
        }

        public static class CryptoHelper
        {
            private static readonly string keyString = "MySuperSecretKey123"; // Must be 16, 24, or 32 bytes
            private static readonly byte[] Key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
            private static readonly byte[] IV = Encoding.UTF8.GetBytes("MyInitVector12345".PadRight(16).Substring(0, 16));

            public static byte[] Encrypt(string plainText)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                        sw.Close();
                        return ms.ToArray();
                    }
                }
            }

            public static string Decrypt(byte[] cipherData)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    using (MemoryStream ms = new MemoryStream(cipherData))
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        private string GetSystemSteupFilePath(string pves)
        {
            string exeFolder = Application.StartupPath;

           // string folder = Path.Combine(exeFolder, "Saved Data", "SystemSetup");
            string folder = AppPathManager.GetFolderPath("SystemSetup");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "SystemSetupFile" + ".dat");
        }

        public event EventHandler DataSaved;

        private void buttonOk_Click(object sender, EventArgs e)
        {
            var data = new SystemSetupModel
            {
                Pves = txtPves.Text,
                Pabd = txtPabd.Text,
                Pura = txtPura.Text,
                Point = txtPoints.Text,
                Vinf = txtVinf.Text,
                Flow = txtFlow.Text,
                Rate = txtRate.Text,
                EMG = txtEmg.Text,
                ChangeBottle = txtChangeBottel.Text,
                P1 = txtP1.Text,
                P2 = txtP2.Text,
                P3 = txtP3.Text,
                JF = txtJf.Text,
                UPP = txtUPP.Text,
                UPPH = txtUppH.Text,
                UPPL = txtUppL.Text,
                U1 = txtU1.Text,
                SpGravity = txtSPGravity.Text,
                DefualInfusion = txtDefualtInfusion.Text,
                Constant1 = txtConstant1.Text,
                Constant2 = txtConstant2.Text,
                PreTest = checkPretestVoding.Checked,
                PVR = checkPVR.Checked,
                Comments = checkComments.Checked,
            };

            // Serialize to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);

            // Encrypt
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            //string filePath = GetSystemSteupFilePath(data.Pves);
            string filePath = GetSystemSteupFilePath("SystemSetupFile");
            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show(isEditMode ? "System Setup data updated successfully." : "System Setup data saved successfully.");

            //Show Success Massage

            //var notification = new NotificationForm(
            //    isEditMode
            //        ? "System Setup data updated successfully."
            //        : "System Setup data saved successfully.",
            //    1000 
            //);
            //notification.TopMost = true;
            //notification.Show();

            //// 🟢 Close the current form after a short delay
            //Task.Delay(1000).ContinueWith(_ =>
            //{
            //    if (this.InvokeRequired)
            //        this.Invoke(new Action(() => this.Close()));
            //    else
            //        this.Close();
            //});


            // 🔴 Trigger the DataSaved event
            DataSaved?.Invoke(this, EventArgs.Empty);

            this.Close();
        }


        private void LoadSystemSetupData(string pves)
        {
            string filePath = GetSystemSteupFilePath(pves);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<SystemSetupModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtPves.Text = record.Pves;
                        txtPabd.Text = record.Pabd;
                        txtPura.Text = record.Pura;
                        txtPoints.Text = record.Point;
                        txtVinf.Text = record.Vinf;
                        txtFlow.Text = record.Flow;
                        txtRate.Text = record.Rate;
                        txtEmg.Text = record.EMG;
                        txtChangeBottel.Text = record.ChangeBottle;
                        txtP1.Text = record.P1;
                        txtP2.Text = record.P2;
                        txtP3.Text = record.P3;
                        txtJf.Text = record.JF;
                        txtUPP.Text = record.UPP;
                        txtUppH.Text = record.UPPH;
                        txtUppL.Text = record.UPPL;
                        txtU1.Text = record.U1;
                        txtSPGravity.Text = record.SpGravity;
                        txtDefualtInfusion.Text = record.DefualInfusion;
                        txtConstant1.Text = record.Constant1;
                        txtConstant2.Text = record.Constant2;
                        //Code For CheckBox
                        checkPretestVoding.Checked = record.PreTest;
                        checkPVR.Checked = record.PVR;
                        checkComments.Checked = record.Comments;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading patient: " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
                //ClearFieldsExceptPatientNo();
            }
        }

        private void txtPves_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                LoadSystemSetupData(txtPves.Text.Trim());
            }
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
