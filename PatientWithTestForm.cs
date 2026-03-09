using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.Doctor;
using static SantronWinApp.Symptoms;

namespace SantronWinApp
{
    public partial class PatientWithTestForm : Form
    {
        private bool isEditMode;
        private string _patientId;
        public int PatientIdNumber { get; set; }

        //Start This code For Click on Patient List Card in MainForm Screen So get this Id Against Patinet Data on 03/11/2025
        public string PatientId
        {
            get => _patientId;
            set
            {
                _patientId = value;
                if (!string.IsNullOrEmpty(_patientId))
                    LoadPatientData();
            }
        }


        private void LoadPatientData()
        {
           // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            //string folderPath = AppPathManager.GetFolderPath("PatientsData");
            string folderPath = AppPathManager.GetFolderPath("Samples");
            // ✅ Find file by exact unique Id and PatientId
            string searchPattern = $"{PatientIdNumber}_{PatientId}.dat";
            string filePath = Path.Combine(folderPath, searchPattern);

            if (!File.Exists(filePath))
            {
                //MessageBox.Show($"Patient data not found for ID {PatientIdNumber}.","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                string json = CryptoHelper.Decrypt(bytes);
                var record = JsonSerializer.Deserialize<PatientRecord>(json);

                if (record == null)
                {
                    MessageBox.Show("Failed to parse patient record.");
                    return;
                }

                // ✅ Fill form controls
                txtId.Text = record.Id.ToString();
                txtPatientIdN.Text = record.PatientNo;
                txtPatientName.Text = record.PatientName;
                txtWeight.Text = record.Weight;
                txtAddress.Text = record.Address;
                txtAge.Text = record.Age;
                txtMobileNo.Text = record.MobileNo;
                cmbSelectTest.Text = record.Test;

                // Gender
                radioButton1.Checked = record.Gender == "Male";
                radioButton2.Checked = record.Gender == "Female";
                radioButton3.Checked = record.Gender == "Other";

                if (comboBox1.Items.Contains(record.ReferredBy))
                    comboBox1.SelectedItem = record.ReferredBy;
                else
                    comboBox1.Text = record.ReferredBy;

                if (comboBox2.Items.Contains(record.Symptoms))
                    comboBox2.SelectedItem = record.Symptoms;
                else
                    comboBox2.Text = record.Symptoms;

                if (DateTime.TryParse(record.Date, out DateTime parsedDate))
                    dtpDate.Value = parsedDate;

                if (TimeSpan.TryParse(record.Time, out TimeSpan parsedTime))
                    dtpTime.Value = DateTime.Today.Add(parsedTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load patient data: " + ex.Message);
            }
        }
       
        //End This code For Click on Patient List Card in MainForm Screen So get this Id Against Patinet Data on 03/11/2025

        public PatientWithTestForm()
        {
            InitializeComponent();

            //Start Code For Click On Screen Out Side Screen Not Hide
            this.ShowInTaskbar = false;
            this.TopMost = true;
            //End Code For Click On Screen Out Side Screen Not Hide
        }

        private void PatientWithTestForm_Load(object sender, EventArgs e)
        {
            txtPatientIdN.Focus();

            txtPatientIdN.TextChanged += (s, b) => this.Invalidate();

            // Set default values
            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(GetAllDoctorNames().ToArray());

            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(GetAllSymptomNames().ToArray());

            //For Get MainForm PatientList Card click Data show
            if (!string.IsNullOrEmpty(PatientId))
            {
                LoadPatientData(PatientIdNumber.ToString(), PatientId);
            }
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

            txtTechnicianName.ForeColor = Color.Black;

            txtReferredBy.Clear();
            txtReferredBy.ForeColor = Color.Black;

            txtSymptoms.Clear();
            txtSymptoms.ForeColor = Color.Black;

            cmbSelectTest.SelectedIndex = -1;
            cmbSelectTest.ForeColor = Color.Black;

            dtpDate.Value = DateTime.Now;
            dtpTime.Value = DateTime.Now;

        }

        public class PatientRecord
        {
            public int Id { get; set; }
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

           // string folder = Path.Combine(exeFolder, "Saved Data", "PatientsData");
            //string folder = AppPathManager.GetFolderPath("PatientsData");
            string folder = AppPathManager.GetFolderPath("Samples");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, patientNo + ".dat");
        }

        //private void btnOk_Click(object sender, EventArgs e)
        //{
        //    string filePath = GetPatientFilePath(txtPatientIdN.Text);

        //    if (File.Exists(filePath))
        //    {
        //        MessageBox.Show("This data is already saved. If you want to update, click on Edit button.");
        //        return;
        //    }

        //    string gender = "";
        //    if (radioButton1.Checked) gender = "Male";
        //    else if (radioButton2.Checked) gender = "Female";
        //    else if (radioButton3.Checked) gender = "Other";

        //    string date = dtpDate.Value.ToString("yyyy-MM-dd");
        //    string time = dtpTime.Value.ToString("HH:mm:ss");

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

        //    File.WriteAllBytes(filePath, encryptedData);

        //    //MessageBox.Show("Patient data saved successfully.");

        //    var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();

        //    // ✅ Trigger graph/video test on existing MainForm
        //    if (mainForm != null)
        //    {
        //        if (!string.IsNullOrEmpty(record.Test) && record.Test.EndsWith("+ Video", StringComparison.OrdinalIgnoreCase))
        //            mainForm.TriggerStartTest(record);
        //        else
        //            mainForm.TriggerGraphOnly(record);
        //    }


        //    this.Close();
        //}


        private void btnOk_Click(object sender, EventArgs e)
        {
            string patientId = txtPatientIdN.Text.Trim();
            if (string.IsNullOrWhiteSpace(patientId))
            {
                MessageBox.Show("Please enter Patient ID.");
                return;
            }

            // ✅ Test validation (ADD HERE)
            if (cmbSelectTest.SelectedItem == null ||
                string.IsNullOrWhiteSpace(cmbSelectTest.SelectedItem.ToString()))
            {
                MessageBox.Show(
                    "Please select the Test first and then save record.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                cmbSelectTest.Focus();
                return;
            }

           // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            //string folderPath = AppPathManager.GetFolderPath("PatientsData");
            string folderPath = AppPathManager.GetFolderPath("Samples");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // ✅ Get next auto-increment ID 
            int nextId = 1;
            var existingFiles = Directory.GetFiles(folderPath, "*.dat");
            if (existingFiles.Length > 0)
            {
                var lastRecordIds = new List<int>();
                foreach (var file in existingFiles)
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string json = CryptoHelper.Decrypt(encryptedData);
                        var existing = JsonSerializer.Deserialize<PatientRecord>(json);
                        if (existing != null)
                            lastRecordIds.Add(existing.Id);
                    }
                    catch
                    {
                        // Ignore any corrupt or unreadable files
                    }
                }
                if (lastRecordIds.Count > 0)
                    nextId = lastRecordIds.Max() + 1;
            }

            txtId.Text = nextId.ToString();

            string filePath = Path.Combine(folderPath, $"{nextId}_{patientId}.dat");

            string gender = "";
            if (radioButton1.Checked) gender = "Male";
            else if (radioButton2.Checked) gender = "Female";
            else if (radioButton3.Checked) gender = "Other";

            string date = dtpDate.Value.ToString("yyyy-MM-dd");
            string time = dtpTime.Value.ToString("HH:mm:ss");

            var record = new PatientRecord
            {
                Id = nextId,
                PatientNo = patientId,
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
            byte[] encryptedDataToSave = CryptoHelper.Encrypt(jsonData);
            File.WriteAllBytes(filePath, encryptedDataToSave);

            this.Close();

            // ✅ Close PatientList form if open
            var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
            
            if (patientListForm != null)
            {
                patientListForm.Close();
            }

          

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm != null)
            {
                if (!string.IsNullOrEmpty(record.Test) && record.Test.EndsWith("+ Video", StringComparison.OrdinalIgnoreCase))
                    mainForm.TriggerStartTestLive(record);
                else
                    mainForm.TriggerGraphOnlyLive(record);
            }


            //MessageBox.Show("Patient data saved successfully.");

        }



        //Start this code for Update Records Only 
        //private void Edit_Button_Click(object sender, EventArgs e)
        //{
        //    string filePath = GetPatientFilePath(txtPatientIdN.Text);

        //    if (!File.Exists(filePath))
        //    {
        //        MessageBox.Show("No record found to update. Please save the patient first.");
        //        return;
        //    }

        //    string gender = "";
        //    if (radioButton1.Checked) gender = "Male";
        //    else if (radioButton2.Checked) gender = "Female";
        //    else if (radioButton3.Checked) gender = "Other";

        //    string date = dtpDate.Value.ToString("yyyy-MM-dd");
        //    string time = dtpTime.Value.ToString("HH:mm:ss");

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

        //    File.WriteAllBytes(filePath, encryptedData);

        //    MessageBox.Show("Patient data updated successfully.");

        //    foreach (Form frm in Application.OpenForms.OfType<Form>().ToList())
        //    {
        //        if (frm is MainForm)
        //        {
        //            frm.Close();
        //            break;
        //        }
        //    }

        //    // Reopen MainForm
        //    MainForm mainForm = new MainForm();
        //    mainForm.Show();


        //    // If PatientList is open, close and reopen it
        //    var patientListForm = Application.OpenForms.OfType<PatientList>().FirstOrDefault();
        //    if (patientListForm != null) // means it's open
        //    {
        //        patientListForm.Close();

        //        PatientList newPatientList = new PatientList();
        //        newPatientList.Show();
        //    }

        //    this.Close();
        //}


        //Start this code for Insert New Record & Update Existing Record for same button add on 06/11/2025
        private void Edit_Button_Click(object sender, EventArgs e)
        {
            try
            {
                string patientId = txtPatientIdN.Text.Trim();
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    MessageBox.Show("Please enter Patient ID.");
                    return;
                }

                //string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
                //string folderPath = AppPathManager.GetFolderPath("PatientsData");
                string folderPath = AppPathManager.GetFolderPath("Samples");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Get all existing files
                var existingFiles = Directory.GetFiles(folderPath, "*.dat");
                PatientRecord existingRecord = null;
                string existingFilePath = null;

                // 🔍 Try to find an existing record with same PatientNo
                foreach (var file in existingFiles)
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string json = CryptoHelper.Decrypt(encryptedData);
                        var record = JsonSerializer.Deserialize<PatientRecord>(json);

                        if (record != null && record.PatientNo == patientId)
                        {
                            existingRecord = record;
                            existingFilePath = file;
                            break;
                        }
                    }
                    catch
                    {
                        // Ignore unreadable or corrupt files
                    }
                }

                // 🆕 or 🔄 Get next auto-increment ID
                int recordId;
                string filePath;

                if (existingRecord != null)
                {
                    // ✅ Existing record → update same file
                    recordId = existingRecord.Id;
                    filePath = existingFilePath;
                }
                else
                {
                    // ✅ New record → assign new ID
                    int nextId = 1;
                    var ids = new List<int>();
                    foreach (var file in existingFiles)
                    {
                        try
                        {
                            byte[] encryptedData = File.ReadAllBytes(file);
                            string json = CryptoHelper.Decrypt(encryptedData);
                            var rec = JsonSerializer.Deserialize<PatientRecord>(json);
                            if (rec != null)
                                ids.Add(rec.Id);
                        }
                        catch
                        {
                            // ignore invalid files
                        }
                    }
                    if (ids.Count > 0)
                        nextId = ids.Max() + 1;

                    recordId = nextId;
                    filePath = Path.Combine(folderPath, $"{recordId}_{patientId}.dat");
                }

                txtId.Text = recordId.ToString();

                // 🧩 Collect form data
                string gender = "";
                if (radioButton1.Checked) gender = "Male";
                else if (radioButton2.Checked) gender = "Female";
                else if (radioButton3.Checked) gender = "Other";

                string date = dtpDate.Value.ToString("yyyy-MM-dd");
                string time = dtpTime.Value.ToString("HH:mm:ss");

                var recordToSave = new PatientRecord
                {
                    Id = recordId,
                    PatientNo = patientId,
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

                // 🔐 Encrypt & Save
                string jsonData = System.Text.Json.JsonSerializer.Serialize(recordToSave);
                byte[] encryptedDataToSave = CryptoHelper.Encrypt(jsonData);
                File.WriteAllBytes(filePath, encryptedDataToSave);

                // ✅ Message
                //if (existingRecord != null)
                //    MessageBox.Show("Patient record updated successfully.");
                //else
                //    MessageBox.Show("New patient record saved successfully.");

                PatientList openPatientList = Application.OpenForms
                .OfType<PatientList>()
                .FirstOrDefault();

                // 2️⃣ Close current Edit Form
                this.Close();

                // 3️⃣ If PatientList is open → refresh it
                if (openPatientList != null)
                {
                    openPatientList.RefreshData(); // <-- Add this method for refresh
                    openPatientList.BringToFront();
                }

                
                //this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving or updating patient record:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

           
        }



        //public void LoadPatientData(int id, string patientNo)
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
        //            MessageBox.Show("Error loading patient: " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        isEditMode = false;
        //        ClearFieldsExceptPatientNo();
        //    }
        //}

        public void LoadPatientData(string id, string patientNo)
        {
            //string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            string folderPath = AppPathManager.GetFolderPath("Samples");
            string filePath = Path.Combine(folderPath, $"{id}_{patientNo}.dat");

            if (!File.Exists(filePath))
            {
                isEditMode = false;
                ClearFieldsExceptPatientNo();
                //MessageBox.Show($"Patient data not found for ID: {id} and PatientNo: {patientNo}","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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

                    txtTechnicianName.Text = record.TechnicianName;
                    txtTechnicianName.ForeColor = Color.Black;

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

                    //This code get the Saved Date & Time comment on 03-03-2026

                    //if (DateTime.TryParse(record.Date, out DateTime dateValue))
                    //    dtpDate.Value = dateValue;
                    //else
                    //    dtpDate.Value = DateTime.Now;

                    //if (TimeSpan.TryParse(record.Time, out TimeSpan timeValue))
                    //    dtpTime.Value = DateTime.Today.Add(timeValue);
                    //else
                    //    dtpTime.Value = DateTime.Now;

                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient: " + ex.Message);
            }
        }


        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem != null)
            {
                string selectedSymptomsName = comboBox2.SelectedItem.ToString();
                //MessageBox.Show($"Selected Doctor: {selectedDoctorName}");
            }
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
           // string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");
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

        private void txtPatientIdN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                //LoadPatientData(txtId.Text.Trim(), txtPatientIdN.Text.Trim());

                string patientNo = txtPatientIdN.Text.Trim();

                if (string.IsNullOrWhiteSpace(txtPatientName.Text))
                {
                    string latestId = GetLatestIdForPatient(patientNo);
                    txtId.Text = latestId;
                }

                LoadPatientData(txtId.Text.Trim(), patientNo);

            }
        }

        private void txtPatientIdN_Leave(object sender, EventArgs e)
        {
            string patientNo = txtPatientIdN.Text.Trim();

            if (!string.IsNullOrWhiteSpace(patientNo))
            {
                if (string.IsNullOrWhiteSpace(txtPatientName.Text))
                {
                    string latestId = GetLatestIdForPatient(patientNo);
                    txtId.Text = latestId;
                }

                LoadPatientData(txtId.Text.Trim(), patientNo);
            }
        }



        private string GetLatestIdForPatient(string patientNo)
        {
           // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            string folderPath = AppPathManager.GetFolderPath("Samples");
            if (!Directory.Exists(folderPath))
                return "1"; // default if no folder

            // pattern like  ID_PATIENTNO.dat
            var files = Directory.GetFiles(folderPath, $"*_{patientNo}.dat");

            if (files.Length == 0)
                return "1"; // no records yet for this patient

            var ids = files
                .Select(f => Path.GetFileNameWithoutExtension(f).Split('_').FirstOrDefault())
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            if (ids.Count == 0)
                return "1";

            // Return the latest id (max one)
            return ids.Max().ToString();
        }



      

        private void txtPatientName_KeyDown(object sender, KeyEventArgs e)
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

        private void txtAddress_KeyDown(object sender, KeyEventArgs e)
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

        private void txtMobileNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void cmbSelectTest_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void dtpDate_ValueChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
             this.Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

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

        private void txtPatientIdN_TextChanged(object sender, EventArgs e)
        {
            txtPatientIdN.Text = System.Text.RegularExpressions.Regex
           .Replace(txtPatientIdN.Text, @"[^a-zA-Z0-9]", "");

            txtPatientIdN.SelectionStart = txtPatientIdN.Text.Length;

            //This code for if Enter any Special Character so show the massage and clear the textbox

            //string validText = new string(txtPatientIdN.Text
            //    .Where(ch => char.IsLetterOrDigit(ch))
            //    .ToArray());

            //if (txtPatientIdN.Text != validText)
            //{
            //    MessageBox.Show("Only letters and numbers are allowed in Patient ID",
            //                    "Invalid Input",
            //                    MessageBoxButtons.OK,
            //                    MessageBoxIcon.Warning);

            //    txtPatientIdN.Text = validText;

            //    txtPatientIdN.SelectionStart = txtPatientIdN.Text.Length;
            //}
        }

        private void txtAge_TextChanged(object sender, EventArgs e)
        {
            // Store current cursor position
            int cursorPosition = txtAge.SelectionStart;

            // Remove any non-digit characters
            string text = txtAge.Text;
            string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

            // Limit to 3 digits
            if (digitsOnly.Length > 3)
            {
                digitsOnly = digitsOnly.Substring(0, 3);
            }

            // Update text only if it's different
            if (txtAge.Text != digitsOnly)
            {
                txtAge.Text = digitsOnly;

                // Adjust cursor position
                if (cursorPosition > txtAge.Text.Length)
                {
                    cursorPosition = txtAge.Text.Length;
                }
                txtAge.SelectionStart = cursorPosition;
            }
        }

        private void txtWeight_TextChanged(object sender, EventArgs e)
        {
            // Store current cursor position
            int cursorPosition = txtWeight.SelectionStart;

            // Remove any non-digit characters
            string text = txtWeight.Text;
            string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

            // Limit to 3 digits
            if (digitsOnly.Length > 3)
            {
                digitsOnly = digitsOnly.Substring(0, 3);
            }

            // Update text only if it's different
            if (txtWeight.Text != digitsOnly)
            {
                txtWeight.Text = digitsOnly;

                // Adjust cursor position
                if (cursorPosition > txtWeight.Text.Length)
                {
                    cursorPosition = txtWeight.Text.Length;
                }
                txtWeight.SelectionStart = cursorPosition;
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
