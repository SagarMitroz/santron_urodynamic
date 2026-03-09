using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.Doctor;
using static SantronWinApp.HospitalAndDoctorInfoSetUp;
using static SantronWinApp.PatientWithTestForm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SantronWinApp
{
    public partial class PatientList : Form
    {
        private string currentSearchText = "";

        public PatientList()
        {
            InitializeComponent();

            //this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromHandle(this.Handle).WorkingArea;
        }

        private void PatientList_Load(object sender, EventArgs e)
        {
            //txtSearch.Focus();

            LoadAllPatients();

            DocterDropdown.Items.Clear();
            DocterDropdown.Items.AddRange(GetAllDoctorNames().ToArray());

            TestDropdown.Items.Clear();
            TestDropdown.Items.AddRange(GetAllTestNames().ToArray());

            GenderDropdown.Items.Clear();
            GenderDropdown.Items.AddRange(GetAllGenders().ToArray());


            AgeDropdown.Items.Add("1 to 10");
            AgeDropdown.Items.Add("11 to 25");
            AgeDropdown.Items.Add("26 to 35");
            AgeDropdown.Items.Add("36 to 50");
            AgeDropdown.Items.Add("51 to 70");
            AgeDropdown.Items.Add("71 to 100");

            this.ActiveControl = txtSearch;
        }

        public void RefreshData()
        {
            LoadAllPatients();   // <-- call your load method
        }


        private List<PatientRecord> allPatients = new List<PatientRecord>();
        //Start this code Show Only Saved Graph Test Records only 14/11/2025
        private void LoadAllPatients()
        {
            try
            {
                //string baseFolder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
                string baseFolder = AppPathManager.GetFolderPath("PatientsData");
                if (!Directory.Exists(baseFolder))
                    return;

                allPatients.Clear();

                foreach (string datPath in Directory.GetFiles(baseFolder, "*.dat", SearchOption.TopDirectoryOnly))
                {
                    PatientRecord record = null;
                    try
                    {
                        string json = CryptoHelper.Decrypt(File.ReadAllBytes(datPath));
                        record = JsonSerializer.Deserialize<PatientRecord>(json);
                    }
                    catch
                    {
                        continue;
                    }

                    if (record == null)
                        continue;

                    string patientNo = (record.PatientNo ?? "").Trim();
                    string patientName = (record.PatientName ?? "").Trim();
                    string testName = (record.Test ?? "").Trim();

                    if (patientNo == "" || patientName == "" || testName == "")
                        continue;

                    string safeName = MakeSafeFolderName(patientName);

                    // Patient folder
                    string patientFolder = Path.Combine(baseFolder, $"{patientNo}_{safeName}");

                    // Test folder
                    string testFolder = Path.Combine(patientFolder, "Tests", testName);

                    bool hasSavedTest = false;

                    if (Directory.Exists(testFolder))
                    {
                        foreach (string uttFile in Directory.GetFiles(testFolder, "*.utt"))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(uttFile);

                            // Filename pattern: TestName_Id_PatientNo_yyyyMMdd_HHmm
                            string[] parts = fileName.Split('_');

                            if (parts.Length < 4)
                                continue;

                            string fTest = parts[0];
                            string fId = parts[1];
                            string fPatNo = parts[2];

                            // MATCH EXACT TEST, PATIENTNO, ID
                            if (fTest.Equals(testName, StringComparison.OrdinalIgnoreCase) &&
                                fPatNo.Equals(patientNo, StringComparison.OrdinalIgnoreCase) &&
                                fId.Equals(record.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                hasSavedTest = true;
                                break;
                            }
                        }
                    }

                    if (hasSavedTest)
                    {
                        allPatients.Add(record);
                    }
                }

                // APPLY FILTERS
                ApplyFilters();

                // DISPLAY LIST
                var displayList = allPatients
                     .OrderByDescending(p => p.Id)
                    .Select(p => new
                {
                    MainId = p.Id,
                    ID = p.PatientNo,
                    PatientName = p.PatientName,
                    Age = p.Age,
                    Gender = p.Gender,
                    Weight = p.Weight,
                    MobileNo = p.MobileNo,
                    Address = p.Address,
                    ReferredBy = p.ReferredBy,
                    Symptoms = p.Symptoms,
                    Test = p.Test,
                    Date = p.Date
                }).ToList();

                dataGridView1.DataSource = displayList;

                // Hide MainId column and rename ID column to "Patient No"
                if (dataGridView1.Columns.Contains("MainId"))
                {
                    dataGridView1.Columns["MainId"].Visible = false; // Hide MainId column
                }

                if (dataGridView1.Columns.Contains("ID"))
                {
                    dataGridView1.Columns["ID"].HeaderText = "PatientNo"; // Rename ID column
                }

                // SR NUMBER
                if (!dataGridView1.Columns.Contains("SrNo"))
                {
                    DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn
                    {
                        Name = "SrNo",
                        HeaderText = "SrNo",
                        ReadOnly = true
                    };
                    dataGridView1.Columns.Insert(0, srNoCol);
                }

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;

                // BASIC UI SETTINGS
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
                dataGridView1.EnableHeadersVisualStyles = false;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.RowHeadersVisible = false;

                SafeSetColWidth("SrNo", 50);
                SafeSetColWidth("MainId", 0);
                SafeSetColWidth("ID", 90);
                SafeSetColWidth("PatientName", 150);
                SafeSetColWidth("Age", 50);
                SafeSetColWidth("Gender", 70);
                SafeSetColWidth("Weight", 70);
                SafeSetColWidth("MobileNo", 95);
                SafeSetColWidth("Address", 120);
                SafeSetColWidth("ReferredBy", 100);
                SafeSetColWidth("Symptoms", 100);
                SafeSetColWidth("Test", 170);
                SafeSetColWidth("Date", 90);

                //if (!dataGridView1.Columns.Contains("Report"))
                //{
                //    dataGridView1.Columns.Add(new DataGridViewButtonColumn
                //    {
                //        HeaderText = "Report",
                //        Text = "View",
                //        UseColumnTextForButtonValue = true
                //    });
                //}

                //if (!dataGridView1.Columns.Contains("Action"))
                //{
                //    dataGridView1.Columns.Add(new DataGridViewButtonColumn
                //    {
                //        HeaderText = "Action",
                //        Text = "Edit",
                //        UseColumnTextForButtonValue = true
                //    });
                //}

                if (!dataGridView1.Columns.Contains("ReportBtn"))
                {
                    DataGridViewButtonColumn reportBtn = new DataGridViewButtonColumn
                    {
                        Name = "ReportBtn",
                        HeaderText = "Report",
                        Text = "View",
                        UseColumnTextForButtonValue = true
                    };
                    dataGridView1.Columns.Add(reportBtn);
                }

                if (!dataGridView1.Columns.Contains("ActionBtn"))
                {
                    DataGridViewButtonColumn actionBtn = new DataGridViewButtonColumn
                    {
                        Name = "ActionBtn",
                        HeaderText = "Action",
                        Text = "Edit",
                        UseColumnTextForButtonValue = true
                    };
                    dataGridView1.Columns.Add(actionBtn);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient list: " + ex.Message);
            }
        }


        
        
        // Helper: creates a filesystem-safe folder name (replaces invalid chars with underscore)
        private string MakeSafeFolderName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (invalid.Contains(c))
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            // trim spaces at ends
            return sb.ToString().Trim();
        }

        // Safe column width setter
        private void SafeSetColWidth(string colName, int width)
        {
            if (dataGridView1.Columns.Contains(colName))
            {
                try { dataGridView1.Columns[colName].Width = width; }
                catch { }
            }
        }

        //End this code Show Only Saved Graph Test Records only 14/11/2025

        //Start this code Show all records means Saved test and live test

        //private void LoadAllPatients()
        //{
        //    try
        //    {
        //        string baseFolder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
        //        if (!Directory.Exists(baseFolder))
        //        {
        //            return;
        //        }

        //        allPatients.Clear();



        //        foreach (string filePath in Directory.GetFiles(baseFolder, "*.dat", SearchOption.TopDirectoryOnly))
        //        {
        //            try
        //            {
        //                byte[] encryptedData = File.ReadAllBytes(filePath);
        //                string jsonData = CryptoHelper.Decrypt(encryptedData);
        //                var record = JsonSerializer.Deserialize<PatientRecord>(jsonData);

        //                if (record == null) continue;

        //                string patientNo = (record.PatientNo ?? "").Trim();
        //                string patientName = (record.PatientName ?? "").Trim();
        //                string testName = (record.Test ?? "").Trim();

        //                if (string.IsNullOrWhiteSpace(patientNo) || string.IsNullOrWhiteSpace(testName))
        //                    continue;

        //                string safeName = MakeSafeFolderName(patientName);

        //                string testFolderPath = Path.Combine(
        //                    baseFolder,
        //                    $"{patientNo}_{safeName}",
        //                    "Tests",
        //                    testName
        //                );

        //                bool hasSavedTest = false;

        //                if (Directory.Exists(testFolderPath))
        //                {
        //                    if (Directory.EnumerateFiles(testFolderPath).Any())
        //                        hasSavedTest = true;
        //                }

        //                if (hasSavedTest)
        //                {
        //                    allPatients.Add(record);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show($"Error reading file {Path.GetFileName(filePath)}: {ex.Message}");
        //            }
        //        }


        //        ApplyFilters();

        //        var displayList = allPatients.Select(p => new
        //        {
        //            MainId = p.Id,
        //            ID = p.PatientNo,
        //            PatientName = p.PatientName,
        //            Age = p.Age,
        //            Gender = p.Gender,
        //            Weight = p.Weight,
        //            MobileNo = p.MobileNo,
        //            Address = p.Address,
        //            ReferredBy = p.ReferredBy,
        //            Symptoms = p.Symptoms,
        //            Test = p.Test,
        //            Date = p.Date
        //        }).ToList();

        //        dataGridView1.DataSource = displayList;

        //        if (!dataGridView1.Columns.Contains("SrNo"))
        //        {
        //            DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn
        //            {
        //                Name = "SrNo",
        //                HeaderText = "SrNo",
        //                ReadOnly = true
        //            };
        //            dataGridView1.Columns.Insert(0, srNoCol);
        //        }

        //        for (int i = 0; i < dataGridView1.Rows.Count; i++)
        //            dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;

        //        dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        //        dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
        //        dataGridView1.EnableHeadersVisualStyles = false;
        //        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        //        dataGridView1.RowHeadersVisible = false;

        //        SafeSetColWidth("SrNo", 50);
        //        SafeSetColWidth("MainId", 50);
        //        SafeSetColWidth("ID", 50);
        //        SafeSetColWidth("PatientName", 150);
        //        SafeSetColWidth("Age", 50);
        //        SafeSetColWidth("Gender", 70);
        //        SafeSetColWidth("Weight", 70);
        //        SafeSetColWidth("MobileNo", 110);
        //        SafeSetColWidth("Address", 130);
        //        SafeSetColWidth("ReferredBy", 120);
        //        SafeSetColWidth("Symptoms", 100);
        //        SafeSetColWidth("Test", 170);
        //        SafeSetColWidth("Date", 90);

        //        if (!dataGridView1.Columns.Contains("Report"))
        //        {
        //            DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn
        //            {
        //                HeaderText = "Report",
        //                Text = "View",
        //                UseColumnTextForButtonValue = true
        //            };
        //            dataGridView1.Columns.Add(btnCol);
        //        }

        //        if (!dataGridView1.Columns.Contains("Action"))
        //        {
        //            DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn
        //            {
        //                HeaderText = "Action",
        //                Text = "Edit",
        //                UseColumnTextForButtonValue = true
        //            };
        //            dataGridView1.Columns.Add(btnCol);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error loading patient list: " + ex.Message);
        //    }
        //}


        private void ApplyFilters()
        {
            var filtered = allPatients.AsEnumerable();

           

            if (!string.IsNullOrEmpty(currentSearchText))
            {
                filtered = filtered.Where(p =>
                    (!string.IsNullOrEmpty(p.PatientName) &&
                     p.PatientName.IndexOf(currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)

                    || (!string.IsNullOrEmpty(p.MobileNo) &&
                        p.MobileNo.IndexOf(currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)

                    || (!string.IsNullOrEmpty(p.Address) &&
                        p.Address.IndexOf(currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)

                    || (!string.IsNullOrEmpty(p.PatientNo) &&
                        p.PatientNo.IndexOf(currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)

                    || (!string.IsNullOrEmpty(p.Symptoms) &&
                        p.Symptoms.IndexOf(currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }


            // Doctor filter
            if (DocterDropdown.SelectedItem != null && DocterDropdown.SelectedItem.ToString() != "")
            {
                string selectedDoctor = DocterDropdown.SelectedItem.ToString();
                filtered = filtered.Where(p => p.ReferredBy == selectedDoctor);
            }

            // Test filter
            if (TestDropdown.SelectedItem != null && TestDropdown.SelectedItem.ToString() != "")
            {
                string selectedTest = TestDropdown.SelectedItem.ToString();
                filtered = filtered.Where(p => p.Test == selectedTest);
            }

            // Gender filter
            if (GenderDropdown.SelectedItem != null && GenderDropdown.SelectedItem.ToString() != "")
            {
                string selectedGender = GenderDropdown.SelectedItem.ToString();
                filtered = filtered.Where(p => p.Gender == selectedGender);
            }

            // Age group filter
            if (AgeDropdown.SelectedItem != null && AgeDropdown.SelectedItem.ToString() != "")
            {
                string selectedGroup = AgeDropdown.SelectedItem.ToString();
                string[] parts = selectedGroup.Split(new string[] { " to " }, StringSplitOptions.None);

                if (parts.Length == 2 && int.TryParse(parts[0], out int minAge) && int.TryParse(parts[1], out int maxAge))
                {
                    filtered = filtered.Where(p => int.TryParse(p.Age, out int age) && age >= minAge && age <= maxAge);
                }
            }

            // Bind to DataGridView
            dataGridView1.DataSource = filtered.Select(p => new
            {
                MainId = p.Id,
                ID = p.PatientNo,
                PatientName = p.PatientName,
                Age = p.Age,
                Gender = p.Gender,
                Weight = p.Weight,
                MobileNo = p.MobileNo,
                Address = p.Address,
                ReferredBy = p.ReferredBy,
                Symptoms = p.Symptoms,
                Test = p.Test,
                Date = p.Date
            }).ToList();

            // Hide MainId column and rename ID column to "Patient No"
            if (dataGridView1.Columns.Contains("MainId"))
            {
                dataGridView1.Columns["MainId"].Visible = false;
            }

            if (dataGridView1.Columns.Contains("ID"))
            {
                dataGridView1.Columns["ID"].HeaderText = "PatientNo";
            }

            // Re-apply SrNo column
            if (!dataGridView1.Columns.Contains("SrNo"))
            {
                DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn();
                srNoCol.Name = "SrNo";
                srNoCol.HeaderText = "SrNo";
                srNoCol.ReadOnly = true;
                dataGridView1.Columns.Insert(0, srNoCol);
            }

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;
            }
        }


        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddNewPatient_Click(object sender, EventArgs e)
        {
            //var patientForm = new Patient_Information();
            //ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);

            var patientForm = new PatientWithTestForm();
            ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            txtSearch.Focus();

            currentSearchText = txtSearch.Text.Trim();
            ApplyFilters();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


       

        private string GetPatientFilePath(string id, string patientNo)
        {
          //  string folder = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            string folder = AppPathManager.GetFolderPath("PatientsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, $"{id}_{patientNo}.dat");
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string columnName = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            // 🟢 Edit button ("Action")
            if (columnName == "Action")
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells["MainId"].Value?.ToString();
                string patientNo = dataGridView1.Rows[e.RowIndex].Cells["ID"].Value?.ToString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(patientNo))
                {
                    MessageBox.Show("Invalid record: missing ID or Patient No.");
                    return;
                }

                var patientForm = new PatientEditForm();
                patientForm.LoadPatientDataForEdit(id, patientNo);
                ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
            }

            // 🔵 View button ("Report")
            else if (columnName == "Report")
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells["MainId"].Value?.ToString();
                string patientNo = dataGridView1.Rows[e.RowIndex].Cells["ID"].Value?.ToString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(patientNo))
                    return;

                try
                {
                    string filePath = GetPatientFilePath(id, patientNo);
                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show("Patient data file not found.");
                        return;
                    }

                    // Decrypt and deserialize record
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);
                    var record = JsonSerializer.Deserialize<PatientRecord>(jsonData);

                    if (record == null)
                    {
                        MessageBox.Show("Failed to read patient data.");
                        return;
                    }

                    // ✅ Trigger Graph/Video Test on MainForm
                    var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainForm != null)
                    {
                        if (!string.IsNullOrEmpty(record.Test) && record.Test.EndsWith("+ Video", StringComparison.OrdinalIgnoreCase))
                            mainForm.TriggerStartTest(record);
                        else
                            mainForm.TriggerGraphOnly(record);
                    }
                    else
                    {
                        MessageBox.Show("Main form not found. Please open the MainForm first.");
                    }

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while opening graph test: " + ex.Message);
                }
            }
        }




        private void DocterDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (DocterDropdown.SelectedItem != null)
            //{
            //    string selectedDoctorName = DocterDropdown.SelectedItem.ToString();

            //}

            ApplyFilters();
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

        private void TestDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (TestDropdown.SelectedItem != null)
            //{
            //    string selectedTestName = TestDropdown.SelectedItem.ToString();

            //}

            ApplyFilters();
        }

        private string GetTestFolder()
        {
            string exeFolder = Application.StartupPath;
           // string folder = Path.Combine(exeFolder, "Saved Data", "PatientsData");
            string folder = AppPathManager.GetFolderPath("PatientsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        private List<string> GetAllTestNames()
        {
            string folderPath = GetTestFolder();
            List<string> testNames = new List<string>();

            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);

                        var record = System.Text.Json.JsonSerializer.Deserialize<PatientRecord>(jsonData);

                        if (record != null && !string.IsNullOrEmpty(record.Test))
                        {
                            testNames.Add(record.Test);
                        }
                    }
                    catch { /* Ignore corrupt files */ }
                }
            }

            return testNames.Distinct().ToList();
        }

        private void GenderDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (GenderDropdown.SelectedItem != null)
            //{
            //    string selectedGender = GenderDropdown.SelectedItem.ToString();

            //}

            ApplyFilters();
        }

        private List<string> GetAllGenders()
        {
            string folderPath = GetTestFolder();
            List<string> gendersList = new List<string>();

            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(file);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);

                        var record = System.Text.Json.JsonSerializer.Deserialize<PatientRecord>(jsonData);

                        if (record != null && !string.IsNullOrEmpty(record.Gender))
                        {
                            gendersList.Add(record.Gender);
                        }
                    }
                    catch { /* Ignore corrupt files */ }
                }
            }
            return gendersList.Distinct().ToList();
        }

        private void AgeDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (AgeDropdown.SelectedItem != null)
            //{
            //    string selectedGroup = AgeDropdown.SelectedItem.ToString();
            //    //MessageBox.Show("You selected: " + selectedGroup);
            //}

            ApplyFilters();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            DocterDropdown.SelectedIndex = -1;
            TestDropdown.SelectedIndex = -1;
            GenderDropdown.SelectedIndex = -1;
            AgeDropdown.SelectedIndex = -1;

            // Reset search box
            txtSearch.Text = "";
            currentSearchText = "";

            // Show all patients again
            ApplyFilters();

            txtSearch.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        //private void btnExportExcel_Click(object sender, EventArgs e)
        //{
        //    if (dataGridView1.Rows.Count == 0)
        //    {
        //        MessageBox.Show("No data available to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        return;
        //    }

        //    try
        //    {
        //        // Open "Save As" dialog
        //        using (SaveFileDialog sfd = new SaveFileDialog())
        //        {
        //            sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
        //            sfd.FileName = "PatientData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

        //            if (sfd.ShowDialog() == DialogResult.OK)
        //            {
        //                using (XLWorkbook workbook = new XLWorkbook())
        //                {
        //                    var worksheet = workbook.Worksheets.Add("Patients");

        //                    // Add headers
        //                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
        //                    {
        //                        worksheet.Cell(1, i + 1).Value = dataGridView1.Columns[i].HeaderText;
        //                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        //                    }

        //                    // Add rows
        //                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
        //                    {
        //                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
        //                        {
        //                            object value = dataGridView1.Rows[i].Cells[j].Value;
        //                            worksheet.Cell(i + 2, j + 1).Value = value?.ToString() ?? "";
        //                        }
        //                    }

        //                    // Auto adjust column widths
        //                    worksheet.Columns().AdjustToContents();

        //                    // Save the file
        //                    workbook.SaveAs(sfd.FileName);
        //                }

        //                MessageBox.Show("Excel file exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error exporting data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    sfd.FileName = "PatientData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var ws = workbook.Worksheets.Add("Patient Data");
                            int row = 1;
                            int col = 1;

                            // 🟢 Heading
                            ws.Cell(row, col).Value = "Patients List Report";
                            ws.Range(row, col, row, 12).Merge(); 
                            ws.Cell(row, col).Style.Font.Bold = true;
                            ws.Cell(row, col).Style.Font.FontSize = 16;
                            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Row(row).Height = 25;
                            row += 1;

                            // 🟢 Custom Header Row
                            ws.Cell(row, 1).Value = "Sr No";
                            ws.Cell(row, 2).Value = "Patient Id";
                            ws.Cell(row, 3).Value = "Patient Name";
                            ws.Cell(row, 4).Value = "Age";
                            ws.Cell(row, 5).Value = "Gender";
                            ws.Cell(row, 6).Value = "Weight";
                            ws.Cell(row, 7).Value = "Mobile No.";
                            ws.Cell(row, 8).Value = "Address";
                            ws.Cell(row, 9).Value = "Referred By";
                            ws.Cell(row, 10).Value = "Symptoms";
                            ws.Cell(row, 11).Value = "Test";
                            ws.Cell(row, 12).Value = "Date";

                            // Header style
                            for (int i = 1; i <= 12; i++)
                            {
                                ws.Cell(row, i).Style.Font.Bold = true;
                                ws.Cell(row, i).Style.Fill.BackgroundColor = XLColor.LightGray;
                                ws.Cell(row, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                ws.Cell(row, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }

                            // 🟢 Add Data Rows
                            int srNo = 1;
                            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                            {
                                var gridRow = dataGridView1.Rows[i];
                                if (gridRow.IsNewRow) continue;

                                int dataRow = row + 1 + i;
                                int c = 1;

                                ws.Cell(dataRow, c++).Value = srNo++;
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["ID"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["PatientName"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Age"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Gender"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Weight"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["MobileNo"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Address"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["ReferredBy"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Symptoms"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Test"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Date"].Value?.ToString();
                            }

                            // 🟢 Auto-fit columns
                            ws.Columns().AdjustToContents();

                            workbook.SaveAs(sfd.FileName);
                        }

                        //MessageBox.Show("Excel file exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        var notification = new NotificationForm("Excel file exported successfully!", 2000); // 2 sec
                        notification.Show();

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
