using DocumentFormat.OpenXml.Wordprocessing;
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
using static SantronWinApp.Symptoms;

namespace SantronWinApp
{
    public partial class PatientEditForm : Form
    {
        private bool isEditMode;

        public PatientEditForm()
        {
            InitializeComponent();
        }



        //private void PatientEditForm_Load(object sender, EventArgs e)
        //{
        //    //comboBox1.Items.Clear();
        //    comboBox1.Items.AddRange(GetAllDoctorNames().ToArray());

        //    //comboBox2.Items.Clear();
        //    comboBox2.Items.AddRange(GetAllSymptomNames().ToArray());

        //    //Start Code For Auto Load data

        //    if (!isEditMode)
        //    {
        //        string folder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");

        //        if (Directory.Exists(folder))
        //        {
        //            string[] files = Directory.GetFiles(folder, "*.dat");
        //            if (files.Length > 0)
        //            {
        //                string fileName = Path.GetFileNameWithoutExtension(files[0]);
        //                LoadPatientDataForEdit(fileName);
        //            }
        //        }
        //    }

        //    //End Code For Auto Load data


        //}

        //private void PatientEditForm_Load(object sender, EventArgs e)
        //{
        //    comboBox1.Items.AddRange(GetAllDoctorNames().ToArray());
        //    comboBox2.Items.AddRange(GetAllSymptomNames().ToArray());

        //    // Auto-load first record
        //    if (!isEditMode)
        //    {
        //        string folder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");


        //        if (Directory.Exists(folder))
        //        {
        //            string[] files = Directory.GetFiles(folder, "*.dat");
        //            if (files.Length > 0)
        //            {
        //                string fileName = Path.GetFileNameWithoutExtension(files[0]);
        //                string[] parts = fileName.Split('_');

        //                if (parts.Length == 2)
        //                {
        //                    string id = parts[0];
        //                    string patientNo = parts[1];
        //                    LoadPatientDataForEdit(id, patientNo);
        //                }
        //            }
        //        }
        //    }
        //}

        private void PatientEditForm_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();

            comboBox1.Items.AddRange(GetAllDoctorNames().ToArray());
            comboBox2.Items.AddRange(GetAllSymptomNames().ToArray());

            // ✅ Always load patient data if edit mode is true
            if (isEditMode)
            {
                LoadPatientDataForEdit(txtId.Text, txtPatientIdN.Text);
            }
            else
            {
                // Auto-load first record for new form
                //string folder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
                string folder = AppPathManager.GetFolderPath("DoctorsData");
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "*.dat");
                    if (files.Length > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(files[0]);
                        string[] parts = fileName.Split('_');
                        if (parts.Length == 2)
                        {
                            LoadPatientDataForEdit(parts[0], parts[1]);
                        }
                    }
                }
            }
        }



        private string GetPatientFilePath(string id, string patientNo)
        {
            string exeFolder = Application.StartupPath;
            //  string folder = Path.Combine(exeFolder, "Saved Data", "PatientsData");
            string folder = AppPathManager.GetFolderPath("Samples");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // File name format: Id_PatientId.dat
            return Path.Combine(folder, $"{id}_{patientNo}.dat");
        }




        //Data Get For Edit In PatientList Form
        //public void LoadPatientDataForEdit(string patientNo)
        //{
        //    string filePath = GetPatientFilePath(patientNo);
        //    if (File.Exists(filePath))
        //    {
        //        try
        //        {
        //            byte[] encryptedData = File.ReadAllBytes(filePath);
        //            string jsonData = CryptoHelper.Decrypt(encryptedData);

        //            var record = System.Text.Json.JsonSerializer.Deserialize<PatientRecord>(jsonData);

        //            if (record != null)
        //            {
        //                isEditMode = true;

        //                txtPatientIdN.Text = record.PatientNo;
        //                txtPatientName.Text = record.PatientName;
        //                txtPatientName.ForeColor = Color.Black;

        //                txtAge.Text = record.Age;
        //                txtAge.ForeColor = Color.Black;

        //                radioButton1.Checked = record.Gender == "Male";
        //                radioButton2.Checked = record.Gender == "Female";
        //                radioButton3.Checked = record.Gender == "Other";

        //                txtWeight.Text = record.Weight;
        //                txtWeight.ForeColor = Color.Black;

        //                txtMobileNo.Text = record.MobileNo;
        //                txtMobileNo.ForeColor = Color.Black;

        //                txtAddress.Text = record.Address;
        //                txtAddress.ForeColor = Color.Black;


        //                //if (comboBox1.Items.Contains(record.ReferredBy))
        //                //    comboBox1.SelectedItem = record.ReferredBy;
        //                //else
        //                //    comboBox1.Text = record.ReferredBy;

        //                //if (comboBox2.Items.Contains(record.Symptoms))
        //                //    comboBox2.SelectedItem = record.Symptoms;
        //                //else
        //                //    comboBox2.Text = record.Symptoms;

        //                comboBox1.SelectedItem = record.ReferredBy;
        //                comboBox1.ForeColor = Color.Black;

        //                comboBox2.SelectedItem = record.Symptoms;
        //                comboBox2.ForeColor = Color.Black;

        //                cmbSelectTest.SelectedItem = record.Test;
        //                cmbSelectTest.ForeColor = Color.Black;

        //                if (DateTime.TryParse(record.Date, out DateTime dateValue))
        //                    dtpDate.Value = dateValue;
        //                else
        //                    dtpDate.Value = DateTime.Now;

        //                if (TimeSpan.TryParse(record.Time, out TimeSpan timeValue))
        //                    dtpTime.Value = DateTime.Today.Add(timeValue);
        //                else
        //                    dtpTime.Value = DateTime.Now;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Error loading Patient Data : " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        isEditMode = false;
        //        //ClearFieldsExceptPatientNo();
        //    }
        //}

        public void LoadPatientDataForEdit(string id, string patientNo)
        {
            string filePath = GetPatientFilePath(id, patientNo);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);
                    var record = System.Text.Json.JsonSerializer.Deserialize<PatientRecord>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtId.Text = id;
                        txtPatientIdN.Text = record.PatientNo;
                        txtPatientName.Text = record.PatientName;
                        txtAge.Text = record.Age;
                        txtWeight.Text = record.Weight;
                        txtMobileNo.Text = record.MobileNo;
                        txtAddress.Text = record.Address;
                        txtTechnicianName.Text = record.TechnicianName;

                        // Gender
                        radioButton1.Checked = record.Gender == "Male";
                        radioButton2.Checked = record.Gender == "Female";
                        radioButton3.Checked = record.Gender == "Other";

                        // ReferredBy & Symptoms
                        if (comboBox1.Items.Contains(record.ReferredBy))
                            comboBox1.SelectedItem = record.ReferredBy;
                        else
                            comboBox1.Text = record.ReferredBy;

                        if (comboBox2.Items.Contains(record.Symptoms))
                            comboBox2.SelectedItem = record.Symptoms;
                        else
                            comboBox2.Text = record.Symptoms;

                        // Test
                        cmbSelectTest.SelectedItem = record.Test;

                        // Date & Time
                        if (DateTime.TryParse(record.Date, out DateTime dateValue))
                            dtpDate.Value = dateValue;
                        if (TimeSpan.TryParse(record.Time, out TimeSpan timeValue))
                            dtpTime.Value = DateTime.Today.Add(timeValue);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading Patient Data: " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
            }
        }


        

        private void Edit_Button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtId.Text) || string.IsNullOrWhiteSpace(txtPatientIdN.Text))
            {
                MessageBox.Show("Missing ID or Patient ID. Cannot save.");
                return;
            }

            string id = txtId.Text.Trim();
            string patientNo = txtPatientIdN.Text.Trim();
            string filePath = GetPatientFilePath(id, patientNo);

            if (!File.Exists(filePath))
            {
                MessageBox.Show("No record found to update. Please save the patient first.");
                return;
            }

            string gender = radioButton1.Checked ? "Male" :
                            radioButton2.Checked ? "Female" :
                            radioButton3.Checked ? "Other" : "";

            string date = dtpDate.Value.ToString("yyyy-MM-dd");
            string time = dtpTime.Value.ToString("HH:mm:ss");

            var record = new PatientRecord
            {
                Id = int.Parse(id),
                PatientNo = patientNo,
                PatientName = txtPatientName.Text,
                Age = txtAge.Text,
                Gender = gender,
                Weight = txtWeight.Text,
                MobileNo = txtMobileNo.Text,
                Address = txtAddress.Text,
                TechnicianName = txtTechnicianName.Text,
                ReferredBy = comboBox1.SelectedItem?.ToString() ?? "",
                Symptoms = comboBox2.SelectedItem?.ToString() ?? "",
                Test = cmbSelectTest.SelectedItem?.ToString() ?? "",
                Date = date,
                Time = time
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);
            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show("Patient data updated successfully!");

            // Refresh forms if open
            //foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            //{
            //    if (frm is MainForm)
            //    {
            //        frm.Close();
            //        break;
            //    }
            //}

            //MainForm mainForm = new MainForm();
            //mainForm.Show();

            var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            if (patientListForm != null)
            {
                patientListForm.RefreshData();
                new PatientList().BringToFront();
            }

            var patientHistoryForm = Application.OpenForms.OfType<PatientHistory>().FirstOrDefault();
            if (patientHistoryForm != null)
            {
                patientHistoryForm.RefreshData();
                new PatientHistory().BringToFront();
            }

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

        

        private string GetSymptomsFolder()
        {
            string exeFolder = Application.StartupPath;
           // string folder = Path.Combine(exeFolder, "Saved Data", "SymtomsData");
            string folder = AppPathManager.GetFolderPath("SymtomsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem != null)
            {
                string selectedSymptomsName = comboBox2.SelectedItem.ToString();
                //MessageBox.Show($"Selected Doctor: {selectedDoctorName}");
            }
        }

        private List<string> GetAllSymptomNames()
        {
            string folderPath = GetSymptomsFolder();
            List<string> symptomNames = new List<string>();

            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);

                        var record = System.Text.Json.JsonSerializer.Deserialize<SymptomsViewModel>(jsonData);

                        if (record != null && !string.IsNullOrEmpty(record.SymptomsName))
                        {
                            symptomNames.Add(record.SymptomsName);
                        }
                    }
                    catch { /* Ignore corrupt files */ }
                }
            }

            return symptomNames;
        }



        private string GetDoctorsFolder()
        {
            string exeFolder = Application.StartupPath;
            //string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");
            string folder = AppPathManager.GetFolderPath("DoctorsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedDoctorName = comboBox1.SelectedItem.ToString();
                //MessageBox.Show($"Selected Doctor: {selectedDoctorName}");
            }
        }

        private List<string> GetAllDoctorNames()
        {
            string folderPath = GetDoctorsFolder();
            List<string> doctorNames = new List<string>();

            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);

                        var record = System.Text.Json.JsonSerializer.Deserialize<DocterViewModel>(jsonData);

                        if (record != null && !string.IsNullOrEmpty(record.DocterName))
                        {
                            doctorNames.Add(record.DocterName);
                        }
                    }
                    catch { /* Ignore corrupt files */ }
                }
            }

            return doctorNames;
        }

        private void txtPatientIdN_TextChanged(object sender, EventArgs e)
        {
            string validText = new string(txtPatientIdN.Text
                        .Where(ch => char.IsLetterOrDigit(ch))
                        .ToArray());

            // If invalid characters were removed
            if (txtPatientIdN.Text != validText)
            {
                MessageBox.Show("Only letters and numbers are allowed in Patient ID",
                                "Invalid Input",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtPatientIdN.Text = validText;

                // Keep cursor at the end
                txtPatientIdN.SelectionStart = txtPatientIdN.Text.Length;
            }
        }

        private void txtWeight_TextChanged(object sender, EventArgs e)
        {
            string validText = "";
            foreach (char c in txtWeight.Text)
            {
                if (char.IsDigit(c))
                    validText += c;
            }

            if (validText.Length > 3)
                validText = validText.Substring(0, 3);

            if (txtWeight.Text != validText)
            {
                int selectionStart = txtWeight.SelectionStart - (txtWeight.Text.Length - validText.Length);
                txtWeight.Text = validText;
                txtWeight.SelectionStart = Math.Max(0, selectionStart);
            }
        }

        private void txtWeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            // Allow digits
            if (char.IsDigit(e.KeyChar) && txtWeight.Text.Length < 3)
                return;

            e.Handled = true;
        }

        private void txtAge_TextChanged(object sender, EventArgs e)
        {
            string validText = "";
            foreach (char c in txtAge.Text)
            {
                if (char.IsDigit(c))
                    validText += c;
            }

            if (validText.Length > 3)
                validText = validText.Substring(0, 3);

            if (txtAge.Text != validText)
            {
                int selectionStart = txtAge.SelectionStart - (txtAge.Text.Length - validText.Length);
                txtAge.Text = validText;
                txtAge.SelectionStart = Math.Max(0, selectionStart);
            }
        }

        private void txtAge_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            // Allow digits
            if (char.IsDigit(e.KeyChar) && txtAge.Text.Length < 3)
                return;

            e.Handled = true;
        }

        private void txtMobileNo_TextChanged(object sender, EventArgs e)
        {
            string validText = "";
            foreach (char c in txtMobileNo.Text)
            {
                if (char.IsDigit(c))
                    validText += c;
            }

            if (validText.Length > 10)
                validText = validText.Substring(0, 10);

            if (txtMobileNo.Text != validText)
            {
                int selectionStart = txtMobileNo.SelectionStart - (txtMobileNo.Text.Length - validText.Length);
                txtMobileNo.Text = validText;
                txtMobileNo.SelectionStart = Math.Max(0, selectionStart);
            }
        }

        private void txtMobileNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            // Allow digits
            if (char.IsDigit(e.KeyChar) && txtMobileNo.Text.Length < 10)
                return;

            e.Handled = true;
        }

        private void radioButton1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                radioButton1.Checked = true;   // select radio button
                //txtAddress.Focus();             // move focus to textbox
                e.Handled = true;
                e.SuppressKeyPress = true;     // prevent beep
            }
        }

        private void radioButton2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                radioButton2.Checked = true;
                txtAddress.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void radioButton3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                radioButton3.Checked = true;
                txtAddress.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void radioButton2_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                e.IsInputKey = true;
            }
        }

        private void radioButton3_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                e.IsInputKey = true;
            }
        }
    }
}
