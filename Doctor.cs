using Glimpse.Core.ClientScript;
using Mysqlx.Crud;
using SantronWinApp.Helper;
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

namespace SantronWinApp
{
    public partial class Doctor : Form
    {
        private bool isEditMode;

        public Doctor()
        {
            InitializeComponent();
        }

       
        private void Doctor_Load(object sender, EventArgs e)
        {
            txtDocterName.Focus();

            //Start Code For Click On Screen Out Side Screen Not Hode
            this.ShowInTaskbar = false;
            this.TopMost = true;
            //End Code For Click On Screen Out Side Screen Not Hode

            txtDocterName.TextChanged += (s, b) => this.Invalidate();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }


        public class DocterViewModel
        {
            public string DocterName { get; set; }
            public string Designation { get; set; }
            public string Qualification { get; set; }
            public string Gender { get; set; }
            public string MobileNo { get; set; }
            public string Email { get; set; }
            public string Department { get; set; }
            public string Test { get; set; }
        }

     

        private string GetDocterFilePath(string docterNames)
        {
            //string exeFolder = Application.StartupPath;

            //// Append your desired subfolder
            //string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");


            //// Ensure folder exists
            //if (!Directory.Exists(folder))
            //    Directory.CreateDirectory(folder);

            //// Make final file path
            //return Path.Combine(folder, docterNames + ".dat");

            // Define the root for your app once
           
                // CHANGE 1: Use 'MyDocuments' instead of 'ApplicationData'
                string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // This creates: C:\Users\YourName\Documents\SantronWinApp
                string appRootFolder = Path.Combine(baseFolder, "SantronWinApp");

                // This creates: C:\Users\YourName\Documents\SantronWinApp\Saved Data\DoctorsData
                string doctorsFolder = Path.Combine(appRootFolder, "System", "DoctorsData");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(doctorsFolder))
                {
                    Directory.CreateDirectory(doctorsFolder);
                }

                // Sanitize the filename (remove bad characters like / \ : *)
                string cleanName = docterNames;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    cleanName = cleanName.Replace(c, '_');
                }

                // Return the full path
                return Path.Combine(doctorsFolder, cleanName + ".dat");
            
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string gender = "";
            if (radioButton1.Checked) gender = "Male";
            else if (radioButton2.Checked) gender = "Female";
            else if (radioButton3.Checked) gender = "Other";

            var record = new DocterViewModel
            {
                DocterName = txtDocterName.Text,
                Designation = txtSpecialization.Text,
                Qualification = txtQualification.Text,
                Gender = gender,
                MobileNo = txtMobileNo.Text,
                Email = txtEmail.Text,
                Department = txtDepartment.Text,
                Test = cmbSelectTest.SelectedItem?.ToString() ?? "",
            };

            // Serialize to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);

            // Encrypt
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            // Save to file
            //string filePath = GetDocterFilePath(record.DocterName);

            string filePath = AppPathManager.GetFilePath("DoctorsData", record.DocterName);
            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show(isEditMode ? "Docter data updated successfully." : "Docter data saved successfully.");

            foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            {
                if (frm is DocterList)
                {
                    frm.Close();
                    break;
                }
            }

            // Reopen MainForm
            DocterList docterListForm = new DocterList();
            docterListForm.Show();

            this.Close();

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void LoadDocterData(string docterName)
        {
            //string filePath = GetDocterFilePath(docterName);
            string filePath = AppPathManager.GetFilePath("DoctorsData", docterName);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<DocterViewModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtDocterName.Text = record.DocterName;
                        txtSpecialization.Text = record.Designation;
                        txtSpecialization.ForeColor = Color.Black;

                        txtQualification.Text = record.Qualification;
                        txtQualification.ForeColor = Color.Black;

                        radioButton1.Checked = record.Gender == "Male";
                        radioButton2.Checked = record.Gender == "Female";
                        radioButton3.Checked = record.Gender == "Other";

                        txtMobileNo.Text = record.MobileNo;
                        txtMobileNo.ForeColor = Color.Black;

                        txtMobileNo.Text = record.MobileNo;
                        txtMobileNo.ForeColor = Color.Black;

                        txtEmail.Text = record.Email;
                        txtEmail.ForeColor = Color.Black;

                        txtDepartment.Text = record.Designation;
                        txtDepartment.ForeColor = Color.Black;

                       

                        cmbSelectTest.SelectedItem = record.Test;
                        cmbSelectTest.ForeColor = Color.Black;

                      
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
            //txtDocterName.Clear();
            txtDocterName.ForeColor = Color.Black;

            txtSpecialization.Clear();
            txtSpecialization.ForeColor = Color.Black;

            radioButton1.Checked = radioButton2.Checked = radioButton3.Checked = false;

            txtQualification.Clear();
            txtQualification.ForeColor = Color.Black;

            txtMobileNo.Clear();
            txtMobileNo.ForeColor = Color.Black;

            txtEmail.Clear();
            txtEmail.ForeColor = Color.Black;

            txtDepartment.Clear();
            txtDepartment.ForeColor = Color.Black;

            cmbSelectTest.SelectedIndex = -1;
            cmbSelectTest.ForeColor = Color.Black;

        }

        private void txtDocterName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                LoadDocterData(txtDocterName.Text.Trim());
            }
        }

        private void txtDocterName_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtDocterName.Text))
            {
                LoadDocterData(txtDocterName.Text.Trim());
            }
        }

        private void txtMobileNo_TextChanged(object sender, EventArgs e)
        {
            // Store current cursor position
            int cursorPosition = txtMobileNo.SelectionStart;

            // Remove any non-digit characters
            string text = txtMobileNo.Text;
            string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

            // Limit to 3 digits
            if (digitsOnly.Length > 10)
            {
                digitsOnly = digitsOnly.Substring(0, 10);
            }

            // Update text only if it's different
            if (txtMobileNo.Text != digitsOnly)
            {
                txtMobileNo.Text = digitsOnly;

                // Adjust cursor position
                if (cursorPosition > txtMobileNo.Text.Length)
                {
                    cursorPosition = txtMobileNo.Text.Length;
                }
                txtMobileNo.SelectionStart = cursorPosition;
            }
        }
    }
}
