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
using static SantronWinApp.Doctor;

namespace SantronWinApp
{
    public partial class Symptoms : Form
    {
        private bool isEditMode;

        public Symptoms()
        {
            InitializeComponent();
        }

        private void Symptoms_Load(object sender, EventArgs e)
        {
            //Start Code For Click On Screen Out Side Screen Not Hode
            this.ShowInTaskbar = false;
            this.TopMost = true;
            //End Code For Click On Screen Out Side Screen Not Hode

            txtSymptom.TextChanged += (s, b) => this.Invalidate();
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtMobile_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmbSelectTest_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtPatientId_TextChanged(object sender, EventArgs e)
        {

        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void txtSymCode_TextChanged(object sender, EventArgs e)
        {

        }

       

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


        public class SymptomsViewModel
        {
            public string SymptomsName { get; set; }
            public string SymptomsCode { get; set; }
            public string Category { get; set; }
           
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

        private string GetSymptomsFilePath(string symptomNames)
        {
            string exeFolder = Application.StartupPath;

            // Append your desired subfolder
           // string folder = Path.Combine(exeFolder, "Saved Data", "SymtomsData");
            string folder = AppPathManager.GetFolderPath("SymtomsData");
            // Ensure folder exists
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Make final file path
            return Path.Combine(folder, symptomNames + ".dat");
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var record = new SymptomsViewModel
            {
                SymptomsName = txtSymptom.Text,
                SymptomsCode = txtSymCode.Text,
                Category = CategoryDropdown.SelectedItem?.ToString() ?? "",
            };

            // Serialize to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);

            // Encrypt
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            // Save to file
            string filePath = GetSymptomsFilePath(record.SymptomsName);
            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show(isEditMode ? "Symptoms data updated successfully." : "Symptoms data saved successfully.");

            foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            {
                if (frm is SymptomsList)
                {
                    frm.Close();
                    break;
                }
            }

            // Reopen MainForm
            SymptomsList symptomsListForm = new SymptomsList();
            symptomsListForm.Show();

            this.Close();
        }

        private void LoadSymptomsData(string symptomName)
        {
            string filePath = GetSymptomsFilePath(symptomName);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<SymptomsViewModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtSymptom.Text = record.SymptomsName;
                        txtSymCode.Text = record.SymptomsCode;
                        txtSymCode.ForeColor = Color.Black;

                        CategoryDropdown.Text = record.Category;
                        CategoryDropdown.ForeColor = Color.Black;
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
                ClearFieldsExceptDocterName();
            }
        }

        private void ClearFieldsExceptDocterName()
        {
            //txtSymptom.Clear();
            txtSymptom.ForeColor = Color.Black;

            txtSymCode.Clear();
            txtSymCode.ForeColor = Color.Black;

            CategoryDropdown.SelectedIndex = -1;
            CategoryDropdown.ForeColor = Color.Black;

        }

        private void txtSymptom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                LoadSymptomsData(txtSymptom.Text.Trim());
            }
        }

        private void txtSymptom_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSymptom.Text))
            {
                LoadSymptomsData(txtSymptom.Text.Trim());
            }
        }
    }
}
