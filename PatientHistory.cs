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
using static SantronWinApp.PatientWithTestForm;

namespace SantronWinApp
{
    public partial class PatientHistory : Form
    {
        private string currentSearchText = "";

        public PatientHistory()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromHandle(this.Handle).WorkingArea;
        }

        // ─────────────────────────────────────────────────────────────
        // FORM LOAD
        // ─────────────────────────────────────────────────────────────
        private void PatientHistory_Load(object sender, EventArgs e)
        {
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
            LoadAllPatients();
        }

        // ─────────────────────────────────────────────────────────────
        // DATA
        // ─────────────────────────────────────────────────────────────
        private List<PatientRecord> allPatients = new List<PatientRecord>();

        private void LoadAllPatients()
        {
            try
            {
                string baseFolder = AppPathManager.GetFolderPath("PatientsData");
                if (!Directory.Exists(baseFolder))
                    return;

                allPatients.Clear();

                foreach (string filePath in Directory.GetFiles(baseFolder, "*.dat", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(filePath);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);

                        var record = JsonSerializer.Deserialize<PatientRecord>(jsonData);
                        if (record == null) continue;

                        string patientNo = (record.PatientNo ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(patientNo)) continue;

                        allPatients.Add(record);
                    }
                    catch { }
                }

                // De-duplicate by PatientNo — keep latest date entry
                allPatients = allPatients
                    .GroupBy(p => (p.PatientNo ?? "").Trim())
                    .Select(g =>
                        g.OrderByDescending(x =>
                            DateTime.TryParse(x.Date, out DateTime d) ? d : DateTime.MinValue
                        ).First()
                    )
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient list: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // APPLY FILTERS
        // ─────────────────────────────────────────────────────────────
        private void ApplyFilters()
        {
            var filtered = allPatients.AsEnumerable();

            // Search box
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
                filtered = filtered.Where(p => p.ReferredBy == DocterDropdown.SelectedItem.ToString());

            // Test filter
            if (TestDropdown.SelectedItem != null && TestDropdown.SelectedItem.ToString() != "")
                filtered = filtered.Where(p => p.Test == TestDropdown.SelectedItem.ToString());

            // Gender filter
            if (GenderDropdown.SelectedItem != null && GenderDropdown.SelectedItem.ToString() != "")
                filtered = filtered.Where(p => p.Gender == GenderDropdown.SelectedItem.ToString());

            // Age group filter
            if (AgeDropdown.SelectedItem != null && AgeDropdown.SelectedItem.ToString() != "")
            {
                string[] parts = AgeDropdown.SelectedItem.ToString()
                    .Split(new string[] { " to " }, StringSplitOptions.None);
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int minAge) &&
                    int.TryParse(parts[1], out int maxAge))
                {
                    filtered = filtered.Where(p =>
                        int.TryParse(p.Age, out int age) && age >= minAge && age <= maxAge);
                }
            }

            // ── Step 1: Disable AutoSize BEFORE binding ──────────────
            // This is critical — must be set before DataSource changes
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // ── Step 2: Bind DataSource ──────────────────────────────
            dataGridView1.DataSource = filtered
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    MainId = p.Id,
                    PatientId = p.PatientNo,
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

            // ── Step 3: SrNo column ──────────────────────────────────
            if (!dataGridView1.Columns.Contains("SrNo"))
            {
                dataGridView1.Columns.Insert(0, new DataGridViewTextBoxColumn
                {
                    Name = "SrNo",
                    HeaderText = "Sr No",
                    ReadOnly = true
                });
            }

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;

            // ── Step 4: Rename headers ───────────────────────────────
            SetHeaderText("PatientId", "Patient No");
            SetHeaderText("PatientName", "Patient Name");
            SetHeaderText("Age", "Age");
            SetHeaderText("Gender", "Gender");
            SetHeaderText("Weight", "Weight");
            SetHeaderText("MobileNo", "Mobile No");
            SetHeaderText("Address", "Address");
            SetHeaderText("ReferredBy", "Referred By");
            SetHeaderText("Symptoms", "Symptoms");
            SetHeaderText("Date", "Date");

            // ── Step 5: Hide columns ─────────────────────────────────
            SafeHideCol("MainId");   // used in Edit/Report click
            SafeHideCol("Test");     // used in Test filter

            // ── Step 6: Action button column ─────────────────────────
            if (!dataGridView1.Columns.Contains("ActionBtn"))
            {
                dataGridView1.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = "ActionBtn",
                    HeaderText = "Action",
                    Text = "Edit",
                    UseColumnTextForButtonValue = true
                });
            }

            // ── Step 7: Apply grid styling ───────────────────────────
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.RowHeadersVisible = false;

            // ── Step 8: Set FIXED column widths LAST ─────────────────
            // Must be after everything else — this is what locks the widths
            dataGridView1.Columns["SrNo"].Width = 80;
            dataGridView1.Columns["PatientId"].Width = 80;
            dataGridView1.Columns["PatientName"].Width = 230;
            dataGridView1.Columns["Age"].Width = 50;
            dataGridView1.Columns["Gender"].Width = 80;
            dataGridView1.Columns["Weight"].Width = 70;
            dataGridView1.Columns["MobileNo"].Width = 120;
            dataGridView1.Columns["Address"].Width = 170;
            dataGridView1.Columns["ReferredBy"].Width = 130;
            dataGridView1.Columns["Symptoms"].Width = 120;
            dataGridView1.Columns["Date"].Width = 100;
            //dataGridView1.Columns["ActionBtn"].Width = 80;
            dataGridView1.Columns["ActionBtn"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        // ─────────────────────────────────────────────────────────────
        // CELL CLICK
        // ─────────────────────────────────────────────────────────────
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string columnName = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            if (columnName == "Action")
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells["MainId"].Value?.ToString();
                string patientNo = dataGridView1.Rows[e.RowIndex].Cells["PatientId"].Value?.ToString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(patientNo))
                {
                    MessageBox.Show("Invalid record: missing ID or Patient No.");
                    return;
                }

                var patientForm = new PatientEditForm();
                patientForm.LoadPatientDataForEdit(id, patientNo);
                ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
            }
            else if (columnName == "Report")
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells["MainId"].Value?.ToString();
                string patientNo = dataGridView1.Rows[e.RowIndex].Cells["PatientId"].Value?.ToString();

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

                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);
                    var record = JsonSerializer.Deserialize<PatientRecord>(jsonData);

                    if (record == null)
                    {
                        MessageBox.Show("Failed to read patient data.");
                        return;
                    }

                    var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainForm != null)
                    {
                        if (!string.IsNullOrEmpty(record.Test) &&
                            record.Test.EndsWith("+ Video", StringComparison.OrdinalIgnoreCase))
                            mainForm.TriggerStartTest(record);
                        else
                            mainForm.TriggerGraphOnly(record);
                    }
                    else
                    {
                        MessageBox.Show("Main form not found.");
                    }

                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while opening graph test: " + ex.Message);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────
        private string GetPatientFilePath(string id, string patientNo)
        {
            string folder = AppPathManager.GetFolderPath("PatientsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"{id}_{patientNo}.dat");
        }

        private void SetHeaderText(string colName, string headerText)
        {
            if (dataGridView1.Columns.Contains(colName))
                dataGridView1.Columns[colName].HeaderText = headerText;
        }

        private void SafeHideCol(string colName)
        {
            if (dataGridView1.Columns.Contains(colName))
                dataGridView1.Columns[colName].Visible = false;
        }

        // ─────────────────────────────────────────────────────────────
        // SEARCH / FILTER EVENTS
        // ─────────────────────────────────────────────────────────────
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            txtSearch.Focus();
            currentSearchText = txtSearch.Text.Trim();
            ApplyFilters();
        }

        private void DocterDropdown_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilters();
        private void TestDropdown_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilters();
        private void GenderDropdown_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilters();
        private void AgeDropdown_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilters();

        private void btnReset_Click(object sender, EventArgs e)
        {
            DocterDropdown.SelectedIndex = -1;
            TestDropdown.SelectedIndex = -1;
            GenderDropdown.SelectedIndex = -1;
            AgeDropdown.SelectedIndex = -1;
            txtSearch.Text = "";
            currentSearchText = "";
            ApplyFilters();
            txtSearch.Focus();
        }

        // ─────────────────────────────────────────────────────────────
        // DROPDOWN DATA LOADERS
        // ─────────────────────────────────────────────────────────────
        private string GetDoctorsFolder()
        {
            string folder = AppPathManager.GetFolderPath("DoctorsData");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private List<string> GetAllDoctorNames()
        {
            var names = new List<string>();
            foreach (var file in Directory.GetFiles(GetDoctorsFolder(), "*.dat"))
            {
                try
                {
                    string json = CryptoHelper.Decrypt(File.ReadAllBytes(file));
                    var rec = JsonSerializer.Deserialize<DocterViewModel>(json);
                    if (rec != null && !string.IsNullOrEmpty(rec.DocterName))
                        names.Add(rec.DocterName);
                }
                catch { }
            }
            return names;
        }

        private string GetTestFolder()
        {
            string folder = AppPathManager.GetFolderPath("PatientsData");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private List<string> GetAllTestNames()
        {
            var names = new List<string>();
            foreach (var file in Directory.GetFiles(GetTestFolder(), "*.dat"))
            {
                try
                {
                    string json = CryptoHelper.Decrypt(File.ReadAllBytes(file));
                    var rec = JsonSerializer.Deserialize<PatientRecord>(json);
                    if (rec != null && !string.IsNullOrEmpty(rec.Test))
                        names.Add(rec.Test);
                }
                catch { }
            }
            return names.Distinct().ToList();
        }

        private List<string> GetAllGenders()
        {
            var list = new List<string>();
            foreach (var file in Directory.GetFiles(GetTestFolder(), "*.dat"))
            {
                try
                {
                    string json = CryptoHelper.Decrypt(File.ReadAllBytes(file));
                    var rec = JsonSerializer.Deserialize<PatientRecord>(json);
                    if (rec != null && !string.IsNullOrEmpty(rec.Gender))
                        list.Add(rec.Gender);
                }
                catch { }
            }
            return list.Distinct().ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // EXCEL EXPORT
        // ─────────────────────────────────────────────────────────────
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    sfd.FileName = "PatientHistory_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var ws = workbook.Worksheets.Add("Patient History");
                            int row = 1;

                            ws.Cell(row, 1).Value = "Patient History Report";
                            ws.Range(row, 1, row, 11).Merge();
                            ws.Cell(row, 1).Style.Font.Bold = true;
                            ws.Cell(row, 1).Style.Font.FontSize = 16;
                            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Row(row).Height = 25;
                            row++;

                            string[] headers = {
                                "Sr No", "Patient Id", "Patient Name", "Age",
                                "Gender", "Weight", "Mobile No.", "Address",
                                "Referred By", "Symptoms", "Date"
                            };
                            for (int i = 0; i < headers.Length; i++)
                            {
                                ws.Cell(row, i + 1).Value = headers[i];
                                ws.Cell(row, i + 1).Style.Font.Bold = true;
                                ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                                ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                ws.Cell(row, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }

                            int srNo = 1;
                            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                            {
                                var gridRow = dataGridView1.Rows[i];
                                if (gridRow.IsNewRow) continue;

                                int dataRow = row + srNo;
                                int c = 1;
                                ws.Cell(dataRow, c++).Value = srNo++;
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["PatientId"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["PatientName"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Age"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Gender"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Weight"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["MobileNo"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Address"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["ReferredBy"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Symptoms"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Date"].Value?.ToString();
                            }

                            ws.Columns().AdjustToContents();
                            workbook.SaveAs(sfd.FileName);
                        }

                        var notification = new NotificationForm("Excel file exported successfully!", 2000);
                        notification.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // OTHER BUTTONS
        // ─────────────────────────────────────────────────────────────
        private void AddNewPatient_Click(object sender, EventArgs e)
        {
            var patientForm = new PatientWithTestForm();
            ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // reserved
        }
    }
}