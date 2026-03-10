using Mysqlx.Crud;
using SantronWinApp.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.Doctor;
using static SantronWinApp.Patient_Information;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SantronWinApp
{
    public partial class DocterEditForm : Form
    {
        private bool isEditMode;

        public DocterEditForm()
        {
            InitializeComponent();
        }

        private void DocterEditForm_Load(object sender, EventArgs e)
        {

        }

        private string GetDocterFilePath(string docterName)
        {
           // string exeFolder = Application.StartupPath;

           // string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");

            // Define the root for your app once
            string appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SantronWinApp"
            );

            // Define specific sub-folders
            string doctorsFolder = Path.Combine(appDataRoot, "Saved Data", "DoctorsData");
           
            // Ensure they exist
          //  Directory.CreateDirectory(doctorsFolder);

            if (!Directory.Exists(doctorsFolder))
                Directory.CreateDirectory(doctorsFolder);

            return Path.Combine(doctorsFolder, docterName + ".dat");
        }



        //Data Get For Edit In PatientList Form
        public void LoadDocterDataForEdit(string docterName)
        {
            //  string filePath = GetDocterFilePath(docterName);
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

                        txtEmail.Text = record.Email;
                        txtEmail.ForeColor = Color.Black;

                        txtDepartment.Text = record.Department;
                        txtDepartment.ForeColor = Color.Black;

                        cmbSelectTest.SelectedItem = record.Test;
                        cmbSelectTest.ForeColor = Color.Black;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading docter Data : " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
            }
        }

        private void Edit_Button_Click(object sender, EventArgs e)
        {
            //string filePath = GetDocterFilePath(txtDocterName.Text);
            string filePath = AppPathManager.GetFilePath("DoctorsData", txtDocterName.Text);
            if (!File.Exists(filePath))
            {
                MessageBox.Show("No record found to update. Please save the docter data first.");
                return;
            }

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
                Test = cmbSelectTest.SelectedItem?.ToString() ?? ""
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show("Docter data updated successfully.");

            foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            {
                if (frm is DocterList)
                {
                    frm.Close();
                    break;
                }
            }

            DocterList docterListForm = new DocterList();
            docterListForm.Show();


            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
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
