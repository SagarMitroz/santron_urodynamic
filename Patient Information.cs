using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using static SantronWinApp.Doctor;
using static SantronWinApp.Symptoms;
using static SantronWinApp.HospitalAndDoctorInfoSetUp;




namespace SantronWinApp
{
    public partial class Patient_Information : Form
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;

        bool isEditMode = false;


        public Patient_Information()
        {
            InitializeComponent();

            //Start Code For Click On Screen Out Side Screen Not Hide
            this.ShowInTaskbar = false;
            this.TopMost = true;
            //End Code For Click On Screen Out Side Screen Not Hide
        }



        private void SetPlaceholder(System.Windows.Forms.TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }


        private void Patient_Information_Load(object sender, EventArgs e)
        {
            txtPatientIdN.Focus();

            //txtPatientIdN.BorderStyle = BorderStyle.None;
            //txtPatientName.BorderStyle = BorderStyle.None;
            //txtAge.BorderStyle = BorderStyle.None;
            //txtMobileNo.BorderStyle = BorderStyle.None;
            //txtAddress.BorderStyle = BorderStyle.None;
            //txtReferredBy.BorderStyle = BorderStyle.None;
            //txtSymptoms.BorderStyle = BorderStyle.None;



            //Start Code For Auto Load data
            //string folder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");

            //if (Directory.Exists(folder))
            //{
            //    string[] files = Directory.GetFiles(folder, "*.dat");
            //    if (files.Length > 0)
            //    {
            //        // Load the first available setup file
            //        string fileName = Path.GetFileNameWithoutExtension(files[0]);
            //        LoadPatientDataForEdit(fileName);
            //    }
            //}
            //End Code For Auto Load data



            //this.Paint += Form1_Paint;

            txtPatientIdN.TextChanged += (s, b) => this.Invalidate();

            // Set default values
            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;

            //SetPlaceholder(txtPatientName, "Enter patient name");
            //SetPlaceholder(txtAge, "Enter age");
            //SetPlaceholder(txtMobileNo, "Enter number");
            //SetPlaceholder(txtWeight, "Enter weight");
            //SetPlaceholder(txtAddress, "Enter address");
            


            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(GetAllDoctorNames().ToArray());

            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(GetAllSymptomNames().ToArray());

        }

        //private void Form1_Paint(object sender, PaintEventArgs e)
        //{
        //    Graphics g = e.Graphics;

        //    int borderThickness = 1;
        //    Color borderColor = ColorTranslator.FromHtml("#25459D"); //#86adef

        //    using (Pen pen = new Pen(borderColor, borderThickness))
        //    {
        //        // Controls to draw borders around (TextBox, ComboBox, DateTimePicker)
        //        Control[] borderedControls = new Control[]
        //        {
        //            txtPatientIdN, txtPatientName, txtAge, txtMobileNo, txtAddress, txtReferredBy, txtSymptoms,
        //            cmbSelectTest, dtpDate, dtpTime
        //        };

        //        foreach (Control ctrl in borderedControls)
        //        {
        //            Rectangle rect = new Rectangle(
        //                ctrl.Left - borderThickness,
        //                ctrl.Top - borderThickness,
        //                ctrl.Width + borderThickness * 2,
        //                ctrl.Height + borderThickness * 2);

        //            g.DrawRectangle(pen, rect);
        //        }
        //    }
        //}

        

        //This Code for Only TextBox show Border

        //private void Form1_Paint(object sender, PaintEventArgs e)
        //{
        //    Graphics g = e.Graphics;

        //    int borderThickness = 1;
        //    Color borderColor = ColorTranslator.FromHtml("#25459D");

        //    using (Pen pen = new Pen(borderColor, borderThickness))
        //    {
        //        // List of TextBoxes to draw borders around
        //        TextBox[] textBoxes = new TextBox[] { txtPatientIdN, txtPatientName, txtAge, txtMobileNo, txtAddress, txtReferredBy, txtSymptoms }; // Add your textboxes here

        //        foreach (TextBox tb in textBoxes)
        //        {
        //            Rectangle rect = new Rectangle(
        //                tb.Left - borderThickness,
        //                tb.Top - borderThickness,
        //                tb.Width + borderThickness * 2,
        //                tb.Height + borderThickness * 2);

        //            g.DrawRectangle(pen, rect);
        //        }
        //    }
        //}


        private void button3_Click(object sender, EventArgs e)
        {
            // Cancel button clicked
            this.Close();
        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void dtpDate_ValueChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void txtAddress_TextChanged(object sender, EventArgs e)
        {

        }


       


        private void ClearFieldsExceptPatientNo()
        {
            //txtPatientName.Clear();
            txtPatientName.ForeColor = Color.Black;

            //txtAge.Clear();
            txtAge.ForeColor = Color.Black;

            radioButton1.Checked = radioButton2.Checked = radioButton3.Checked = false;

            //txtWeight.Clear();
            txtWeight.ForeColor = Color.Black;

            //txtMobileNo.Clear();
            txtMobileNo.ForeColor = Color.Black;

            //txtAddress.Clear();
            txtAddress.ForeColor = Color.Black;

            txtReferredBy.Clear();
            txtReferredBy.ForeColor = Color.Black;

            txtSymptoms.Clear();
            txtSymptoms.ForeColor = Color.Black;

            cmbSelectTest.SelectedIndex = -1;
            cmbSelectTest.ForeColor = Color.Black;

            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;
 
        }



        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbSelectTest_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void txtPatientIdN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                LoadPatientData(txtPatientIdN.Text.Trim());
            }
        }

        private void txtPatientIdN_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPatientIdN.Text))
            {
                LoadPatientData(txtPatientIdN.Text.Trim());
            }
        }

        private void txtPatientName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtAge_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtWeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtMobileNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtReferredBy_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtSymptoms_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }



        public class PatientRecord
        {
            public string PatientNo { get; set; }
            public string PatientName { get; set; }
            public string Age { get; set; }
            public string Gender { get; set; }
            public string Weight { get; set; }
            public string MobileNo { get; set; }
            public string Address { get; set; }
            public string ReferredBy { get; set; }
            public string Symptoms { get; set; }
            public string TechnicianName { get; set; }
            public string Test { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public object Id { get; internal set; }
        }


        public static class CryptoHelper
        {
            private static readonly string keyString = "MySuperSecretKey123"; 
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


        private string GetPatientFilePath(string patientNo)
        {
            string exeFolder = Application.StartupPath;

          //  string folder = Path.Combine(exeFolder, "Saved Data", "PatientsData");
            string folder = AppPathManager.GetFolderPath("PatientsData");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, patientNo + ".dat");
        }

        


        //This code Work To Save & Update Same button Commit on 30/08/2025 by Bhushan

        //private void btnOk_Click(object sender, EventArgs e)
        //{
        //    string gender = "";
        //    if (radioButton1.Checked) gender = "Male";
        //    else if (radioButton2.Checked) gender = "Female";
        //    else if (radioButton3.Checked) gender = "Other";

        //    string date = dtpDate.Value.ToString("yyyy-MM-dd");
        //    string time = dtpTime.Value.ToString("HH:mm:ss");

        //    // Create PatientRecord object
        //    var record = new PatientRecord
        //    {
        //        PatientNo = txtPatientIdN.Text,
        //        PatientName = txtPatientName.Text,
        //        Age = txtAge.Text,
        //        Gender = gender,
        //        Weight = txtWeight.Text,
        //        MobileNo = txtMobileNo.Text,
        //        Address = txtAddress.Text,
        //        ReferredBy = comboBox1.SelectedItem?.ToString() ?? "",
        //        Symptoms = comboBox2.SelectedItem?.ToString() ?? "",
        //        Test = cmbSelectTest.SelectedItem?.ToString() ?? "",
        //        Date = date,
        //        Time = time
        //    };

        //    string jsonData = System.Text.Json.JsonSerializer.Serialize(record);

        //    byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

        //    string filePath = GetPatientFilePath(record.PatientNo);
        //    File.WriteAllBytes(filePath, encryptedData);

        //    MessageBox.Show(isEditMode ? "Patient data updated successfully." : "Patient data saved successfully.");
        //    this.Close();
        //}

        //Code Save Date
        private void btnOk_Click(object sender, EventArgs e)
        {
            string filePath = GetPatientFilePath(txtPatientIdN.Text);

            if (File.Exists(filePath))
            {
                MessageBox.Show("This data is already saved. If you want to update, click on Edit button.");
                return;
            }

            string gender = "";
            if (radioButton1.Checked) gender = "Male";
            else if (radioButton2.Checked) gender = "Female";
            else if (radioButton3.Checked) gender = "Other";

            string date = dtpDate.Value.ToString("yyyy-MM-dd");
            string time = dtpTime.Value.ToString("HH:mm:ss");

            var record = new PatientRecord
            {
                PatientNo = txtPatientIdN.Text,
                PatientName = txtPatientName.Text,
                Age = txtAge.Text,
                Gender = gender,
                Weight = txtWeight.Text,
                MobileNo = txtMobileNo.Text,
                Address = txtAddress.Text,
                ReferredBy = comboBox1.SelectedItem?.ToString() ?? "",
                Symptoms = comboBox2.SelectedItem?.ToString() ?? "",
                Test = cmbSelectTest.SelectedItem?.ToString() ?? "",
                Date = date,
                Time = time
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show("Patient data saved successfully.");

            // ✅ Get the already open MainForm
            //var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();

            //if (checkBox1.Checked)
            //{
            //    var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            //    if (patientListForm != null)
            //    {
            //        patientListForm.Close();
            //    }

            //    // ✅ Trigger graph/video test on existing MainForm
            //    if (mainForm != null)
            //    {
            //        if (!string.IsNullOrEmpty(record.Test) && record.Test.EndsWith("+ Video", StringComparison.OrdinalIgnoreCase))
            //            mainForm.TriggerStartTest(record);
            //        else
            //            mainForm.TriggerGraphOnly(record);
            //    }
            //}
            //else
            //{
            //    // Close existing MainForm (if any)
            //    if (mainForm != null) mainForm.Close();

            //    // Reopen MainForm
            //    MainForm newMainForm = new MainForm();
            //    newMainForm.Show();

            //    // Refresh PatientList if open
            //    var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            //    if (patientListForm != null)
            //    {
            //        patientListForm.Close();
            //        PatientList newPatientList = new PatientList();
            //        newPatientList.Show();
            //    }
            //}


            foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            {
                if (frm is MainForm)
                {
                    frm.Close();
                    break;
                }
            }

            // Reopen MainForm
            MainForm mainForm1 = new MainForm();
            mainForm1.Show();

            // If PatientList is open, close and reopen it
            var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            if (patientListForm != null) // means it's open
            {
                patientListForm.Close();

                PatientList newPatientList = new PatientList();
                newPatientList.Show();
            }

            this.Close();
        }


        //Code Update Data
        private void Edit_Button_Click(object sender, EventArgs e)
        {
            string filePath = GetPatientFilePath(txtPatientIdN.Text);

            if (!File.Exists(filePath))
            {
                MessageBox.Show("No record found to update. Please save the patient first.");
                return;
            }

            string gender = "";
            if (radioButton1.Checked) gender = "Male";
            else if (radioButton2.Checked) gender = "Female";
            else if (radioButton3.Checked) gender = "Other";

            string date = dtpDate.Value.ToString("yyyy-MM-dd");
            string time = dtpTime.Value.ToString("HH:mm:ss");

            var record = new PatientRecord
            {
                PatientNo = txtPatientIdN.Text,
                PatientName = txtPatientName.Text,
                Age = txtAge.Text,
                Gender = gender,
                Weight = txtWeight.Text,
                MobileNo = txtMobileNo.Text,
                Address = txtAddress.Text,
                ReferredBy = comboBox1.SelectedItem?.ToString() ?? "",
                Symptoms = comboBox2.SelectedItem?.ToString() ?? "",
                Test = cmbSelectTest.SelectedItem?.ToString() ?? "",
                Date = date,
                Time = time
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            File.WriteAllBytes(filePath, encryptedData);

            MessageBox.Show("Patient data updated successfully.");

            foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
            {
                if (frm is MainForm)
                {
                    frm.Close();
                    break;
                }
            }

            // Reopen MainForm
            MainForm mainForm = new MainForm();
            mainForm.Show();


            // If PatientList is open, close and reopen it
            var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            if (patientListForm != null) // means it's open
            {
                patientListForm.Close();

                PatientList newPatientList = new PatientList();
                newPatientList.Show();
            }

            this.Close();
        }

        public void LoadPatientData(string patientNo)
        {
            string filePath = GetPatientFilePath(patientNo);
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

                        txtPatientIdN.Text = record.PatientNo;
                        txtPatientName.Text = record.PatientName;
                        txtPatientName.ForeColor = Color.Black;

                        txtAge.Text = record.Age;
                        txtAge.ForeColor = Color.Black;

                        radioButton1.Checked = record.Gender == "Male";
                        radioButton2.Checked = record.Gender == "Female";
                        radioButton3.Checked = record.Gender == "Other";

                        txtWeight.Text = record.Weight;
                        txtWeight.ForeColor = Color.Black;

                        txtMobileNo.Text = record.MobileNo;
                        txtMobileNo.ForeColor = Color.Black;

                        txtAddress.Text = record.Address;
                        txtAddress.ForeColor = Color.Black;


                        if (comboBox1.Items.Contains(record.ReferredBy))
                            comboBox1.SelectedItem = record.ReferredBy;
                        else
                            comboBox1.Text = record.ReferredBy; 

                        if (comboBox2.Items.Contains(record.Symptoms))
                            comboBox2.SelectedItem = record.Symptoms;
                        else
                            comboBox2.Text = record.Symptoms;


                        cmbSelectTest.SelectedItem = record.Test;
                        cmbSelectTest.ForeColor = Color.Black;

                        if (DateTime.TryParse(record.Date, out DateTime dateValue))
                            dtpDate.Value = dateValue;
                        else
                            dtpDate.Value = DateTime.Now;

                        if (TimeSpan.TryParse(record.Time, out TimeSpan timeValue))
                            dtpTime.Value = DateTime.Today.Add(timeValue);
                        else
                            dtpTime.Value = DateTime.Now; 
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
                ClearFieldsExceptPatientNo();
            }
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


        //                if (comboBox1.Items.Contains(record.ReferredBy))
        //                    comboBox1.SelectedItem = record.ReferredBy;
        //                else
        //                    comboBox1.Text = record.ReferredBy;

        //                if (comboBox2.Items.Contains(record.Symptoms))
        //                    comboBox2.SelectedItem = record.Symptoms;
        //                else
        //                    comboBox2.Text = record.Symptoms;


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

        private void txtPatientIdN_TextChanged(object sender, EventArgs e)
        {
            //char[] invalidChars = { '.', ',', '/', '\'', ':', '"', ';' };

            //if (txtPatientIdN.Text.IndexOfAny(invalidChars) >= 0)
            //{
            //    MessageBox.Show("Please do not use special characters like . , / ' : \" ; in Patient ID",
            //                    "Invalid Input",
            //                    MessageBoxButtons.OK,
            //                    MessageBoxIcon.Warning);

            //    txtPatientIdN.Text = new string(txtPatientIdN.Text
            //                        .Where(ch => !invalidChars.Contains(ch)).ToArray());

            //    txtPatientIdN.SelectionStart = txtPatientIdN.Text.Length;
            //}


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

        private void dtpTime_ValueChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtPatientName_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void txtReferredBy_TextChanged(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
           
        }

       

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

       

        private void label8_Click(object sender, EventArgs e)
        {

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
          //  string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");
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

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void txtSymptoms_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label12_Click_1(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label36_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void txtWeight_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label11_Click_1(object sender, EventArgs e)
        {
           
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

            if (validText.Length > 2)
                validText = validText.Substring(0, 2);

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
            if (char.IsDigit(e.KeyChar) && txtAge.Text.Length < 2)
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
    }
}