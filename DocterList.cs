using ClosedXML.Excel;
using SantronWinApp.Helper;
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
using static SantronWinApp.Patient_Information;

namespace SantronWinApp
{
    public partial class DocterList : Form
    {
        public DocterList()
        {
            InitializeComponent();

            //this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromHandle(this.Handle).WorkingArea;
        }


        private void DocterList_Load(object sender, EventArgs e)
        {
            LoadAllDocters();
        }


        private List<DocterViewModel> allDocters = new List<DocterViewModel>();

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            ApplySearch(searchText);
        }

        private void ApplySearch(string searchText)
        {
            var filtered = allDocters
                .Where(p =>
                    (p.DocterName ?? "").ToLower().Contains(searchText) ||
                    (p.Designation ?? "").ToLower().Contains(searchText) ||
                    (p.Qualification ?? "").ToLower().Contains(searchText) ||
                    (p.Gender ?? "").ToLower().Contains(searchText) ||
                    (p.MobileNo ?? "").ToLower().Contains(searchText) ||
                    (p.Email ?? "").ToLower().Contains(searchText) ||
                    (p.Department ?? "").ToLower().Contains(searchText) ||
                    (p.Test ?? "").ToLower().Contains(searchText)
                ).ToList();

            dataGridView1.DataSource = filtered.Select((p, index) => new
            {
                //SrNo = index + 1,
                DoctorName = p.DocterName,
                Designation = p.Designation,
                Qualification = p.Qualification,
                Gender = p.Gender,
                MobileNo = p.MobileNo,
                Email = p.Email,
                Department = p.Department,
                Type = p.Test
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

        private void LoadAllDocters()
        {
            try
            {
                // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "DoctorsData");
                string folderPath = AppPathManager.GetFolderPath("DoctorsData");
                if (!Directory.Exists(folderPath))
                {
                    //MessageBox.Show("No docters data found.");
                    return;
                }

                allDocters.Clear();

                //List<DocterViewModel> docters = new List<DocterViewModel>();

                foreach (string filePath in Directory.GetFiles(folderPath, "*.dat"))
                {
                    try
                    {
                        byte[] encryptedData = File.ReadAllBytes(filePath);
                        string jsonData = CryptoHelper.Decrypt(encryptedData);
                        var record = JsonSerializer.Deserialize<DocterViewModel>(jsonData);

                        if (record != null)
                            allDocters.Add(record);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }



                // Show only selected columns with custom header names
                dataGridView1.DataSource = allDocters.Select(p => new
                {
                    DoctorName = p.DocterName,
                    Designation = p.Designation,
                    Qualification = p.Qualification,
                    Gender = p.Gender,
                    MobileNo = p.MobileNo,
                    Email = p.Email,
                    Department = p.Department,
                    Type = p.Test,
                }).ToList();

                // Add SrNo column at index 0
                if (!dataGridView1.Columns.Contains("SrNo"))
                {
                    DataGridViewTextBoxColumn srNoCol = new DataGridViewTextBoxColumn();
                    srNoCol.Name = "SrNo";
                    srNoCol.HeaderText = "SrNo";
                    //srNoCol.Width = 20;
                    srNoCol.ReadOnly = true;
                    dataGridView1.Columns.Insert(0, srNoCol);
                }

                // Fill SrNo column values
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    dataGridView1.Rows[i].Cells["SrNo"].Value = i + 1;
                }

                // Style header
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
                dataGridView1.EnableHeadersVisualStyles = false;

                // Optional: Adjust grid columns for better view
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dataGridView1.RowHeadersVisible = false;

                dataGridView1.Columns["SrNo"].Width = 50;
                dataGridView1.Columns["DoctorName"].Width = 170;
                dataGridView1.Columns["Designation"].Width = 120;
                dataGridView1.Columns["Qualification"].Width = 150;
                dataGridView1.Columns["Gender"].Width = 70;
                dataGridView1.Columns["MobileNo"].Width = 90;
                dataGridView1.Columns["Email"].Width = 140;
                dataGridView1.Columns["Department"].Width = 110;
                dataGridView1.Columns["Type"].Width = 80;

               

                // Add Action Button Column
                if (!dataGridView1.Columns.Contains("Action"))
                {
                    DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
                    btnCol.HeaderText = "Action";
                    btnCol.Text = "Edit";
                    btnCol.UseColumnTextForButtonValue = true;
                    //btnCol.Width = 50;
                    dataGridView1.Columns.Add(btnCol);
                }

                 ApplySearch("");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient list: " + ex.Message);
            }
        }



        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridView1.Columns[e.ColumnIndex].HeaderText == "Action")
            {
                string docterName = dataGridView1.Rows[e.RowIndex].Cells["DoctorName"].Value.ToString();

                //DocterEditForm form = new DocterEditForm();
                //form.LoadDocterDataForEdit(docterName);
                //form.ShowDialog();

                var docterForm = new DocterEditForm();
                docterForm.LoadDocterDataForEdit(docterName);
                ScreenDimOverlay.ShowDialogWithDim(docterForm, alpha: 150);
            }
        }



        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AddNewDocter_Click(object sender, EventArgs e)
        {

            var docterForm = new Doctor();
            ScreenDimOverlayOne.ShowWithDimOne(docterForm, alpha: 150);

        }

        public static class ScreenDimOverlayOne
        {
            public static void ShowWithDimOne(Form childForm, int alpha = 150)
            {
                Form overlay = new Form
                {
                    BackColor = Color.Black,
                    Opacity = alpha / 255.0,
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    WindowState = FormWindowState.Maximized,
                    TopMost = true
                };

                overlay.Click += (s, e) => childForm.Activate();

                overlay.Show();

                childForm.FormClosed += (s, e) => overlay.Close();

                childForm.Show();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

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
                    sfd.FileName = "DoctersData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var ws = workbook.Worksheets.Add("Docters Data");
                            int row = 1;
                            int col = 1;

                            // 🟢 Heading
                            ws.Cell(row, col).Value = "Docters List Report";
                            ws.Range(row, col, row, 9).Merge();
                            ws.Cell(row, col).Style.Font.Bold = true;
                            ws.Cell(row, col).Style.Font.FontSize = 16;
                            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Row(row).Height = 25;
                            row += 1;

                            // 🟢 Custom Header Row
                            ws.Cell(row, 1).Value = "Sr No";
                            ws.Cell(row, 2).Value = "Doctor Name";
                            ws.Cell(row, 3).Value = "Designation";
                            ws.Cell(row, 4).Value = "Qualification";
                            ws.Cell(row, 5).Value = "Gender";
                            ws.Cell(row, 6).Value = "Mobile No.";
                            ws.Cell(row, 7).Value = "Email";
                            ws.Cell(row, 8).Value = "Department";
                            ws.Cell(row, 9).Value = "Type";

                            // Header style
                            for (int i = 1; i <= 9; i++)
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
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["DoctorName"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Designation"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Qualification"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Gender"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["MobileNo"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Email"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Department"].Value?.ToString();
                                ws.Cell(dataRow, c++).Value = gridRow.Cells["Type"].Value?.ToString();
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




        //public static class ScreenDimOverlay
        //{
        //    public static void ShowWithDim(Form form, int alpha = 150)
        //    {
        //        Form overlay = new Form();
        //        overlay.StartPosition = FormStartPosition.Manual;
        //        overlay.FormBorderStyle = FormBorderStyle.None;
        //        overlay.Opacity = alpha / 255.0;  
        //        overlay.BackColor = Color.Black;
        //        overlay.ShowInTaskbar = false;
        //        overlay.WindowState = FormWindowState.Maximized;
        //        overlay.TopMost = true;

        //        // Show overlay first
        //        overlay.Show();

        //        // When your Doctor form closes, remove overlay
        //        form.FormClosed += (s, e) => overlay.Close();

        //        // Show the doctor form (non-modal)
        //        form.Show();
        //    }
        //}



    }
}
