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
using static SantronWinApp.DocterList;
using static SantronWinApp.Doctor;
using static SantronWinApp.Symptoms;

namespace SantronWinApp
{
    public partial class SymptomsList : Form
    {
        public SymptomsList()
        {
            InitializeComponent();
        }
        private void SymptomsList_Load(object sender, EventArgs e)
        {
            LoadAllSymptoms();

            //this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromHandle(this.Handle).WorkingArea;
        }

        private List<SymptomsViewModel> allSymptoms = new List<SymptomsViewModel>();

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            ApplySearch(searchText);
        }


        private void ApplySearch(string searchText)
        {
            var filtered = allSymptoms
                .Where(p =>
                    (p.SymptomsName ?? "").ToLower().Contains(searchText) ||
                    (p.SymptomsCode ?? "").ToLower().Contains(searchText) ||
                    (p.Category ?? "").ToLower().Contains(searchText)
                ).ToList();

            dataGridView1.DataSource = filtered.Select((p, index) => new
            {
                Symptoms = p.SymptomsName,
                Code = p.SymptomsCode,
                Category = p.Category
            }).ToList();

            if (!dataGridView1.Columns.Contains("SrNo"))
            {
                DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn();
                srNoCol.Name = "SrNo";
                srNoCol.HeaderText = "SrNo";
                srNoCol.ReadOnly = true;
                dataGridView1.Columns.Insert(0, srNoCol);
            }

            // Fill SrNo values
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;
            }

            // Style header again if needed
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.EnableHeadersVisualStyles = false;

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;
        }

        private void LoadAllSymptoms()
        {
            try
            {
               // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "SymtomsData");
                string folderPath = AppPathManager.GetFolderPath("SymtomsData");
                if (!Directory.Exists(folderPath))
                {
                    //MessageBox.Show("No symptoms data found.");
                    return;
                }

                allSymptoms.Clear();

                foreach (string filePath in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(filePath);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);
                        var record = JsonSerializer.Deserialize<SymptomsViewModel>(jsonData);

                        if (record != null)
                            allSymptoms.Add(record);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }


                dataGridView1.DataSource = allSymptoms.Select(p => new
                {
                    Symptoms = p.SymptomsName,
                    Code = p.SymptomsCode,
                    Category = p.Category,
                }).ToList();

                if (!dataGridView1.Columns.Contains("SrNo"))
                {
                    DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn();
                    srNoCol.Name = "SrNo";
                    srNoCol.HeaderText = "SrNo";
                    //srNoCol.Width = 20;
                    srNoCol.ReadOnly = true;
                    dataGridView1.Columns.Insert(0, srNoCol);
                }

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;
                }

                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
                dataGridView1.EnableHeadersVisualStyles = false;

                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dataGridView1.RowHeadersVisible = false;

                dataGridView1.Columns["SrNo"].Width = 70;
                dataGridView1.Columns["Symptoms"].Width = 250;
                dataGridView1.Columns["Code"].Width = 200;
                dataGridView1.Columns["Category"].Width = 400;
               

                // Add Action Button Column
                if (!dataGridView1.Columns.Contains("Action"))
                {
                    DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
                    btnCol.HeaderText = "Action";
                    btnCol.Text = "Edit";
                    btnCol.UseColumnTextForButtonValue = true;
                    dataGridView1.Columns.Add(btnCol);
                }

                ApplySearch("");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading symptoms list: " + ex.Message);
            }
        }

       
        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddNewSymptoms_Click(object sender, EventArgs e)
        {

            var symptomsForm = new Symptoms();
            ScreenDimOverlayOne.ShowWithDimOne(symptomsForm, alpha: 150);

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridView1.Columns[e.ColumnIndex].HeaderText == "Action")
            {
                string symptomsName = dataGridView1.Rows[e.RowIndex].Cells["Symptoms"].Value.ToString();

                //SymptomsEditForm form = new SymptomsEditForm();
                //form.LoadSymptomsDataForEdit(symptomsName);
                //form.ShowDialog();

                var symptomsForm = new SymptomsEditForm();
                symptomsForm.LoadSymptomsDataForEdit(symptomsName);
                ScreenDimOverlay.ShowDialogWithDim(symptomsForm, alpha: 150);
            }
        }

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
                    sfd.FileName = "SymptomsData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var ws = workbook.Worksheets.Add("Symptoms Data");
                            int row = 1;
                            int col = 1;

                            // 🟢 Heading
                            ws.Cell(row, col).Value = "Symptoms List";
                            ws.Range(row, col, row, 4).Merge();
                            ws.Cell(row, col).Style.Font.Bold = true;
                            ws.Cell(row, col).Style.Font.FontSize = 16;
                            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Row(row).Height = 25;
                            row += 1;

                            // 🟢 Custom Header Row
                            ws.Cell(row, 1).Value = "Sr No";
                            ws.Cell(row, 2).Value = "Symptoms";
                            ws.Cell(row, 3).Value = "Symptoms Code";
                            ws.Cell(row, 4).Value = "Category";

                            // Header style
                            for (int i = 1; i <= 4; i++)
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
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Symptoms"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Code"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Category"].Value?.ToString();
                            }
                            // 🟢 Auto-fit columns
                            ws.Columns().AdjustToContents();

                            workbook.SaveAs(sfd.FileName);
                        }

                        //MessageBox.Show("Excel file exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        var notification = new NotificationForm("Excel file exported successfully!", 4000); // 2 sec
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
