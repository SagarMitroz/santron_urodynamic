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
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.Doctor;
using static SantronWinApp.Patient_Information;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SantronWinApp
{
    public partial class ReportComments : Form
    {
        
        private string _mainId;
        private string _patientNo;
        private string _testName;

        FontStyle currentStyle = FontStyle.Regular;

        private FontStyle currentStylePatient = FontStyle.Regular;
        private FontStyle currentStyleResult = FontStyle.Regular;
        private bool isEditMode;
        public ReportComments()
        {
            InitializeComponent();

            this.MaximizeBox = false;                 
            //this.FormBorderStyle = FormBorderStyle.FixedSingle;   
        }

       

        private void ReportComments_Load(object sender, EventArgs e)
        {

            cmbReferredBy.Items.Clear();
            cmbReferredBy.Items.AddRange(GetAllDoctorNames().ToArray());

            //Start Code For Auto Load data For Edit
           // string folder = Path.Combine(Application.StartupPath, "Saved Data", "ReportComment");
            string folder = AppPathManager.GetFolderPath("ReportComment");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.dat");
                if (files.Length > 0)
                {
                    // Load the first available setup file
                    string fileName = Path.GetFileNameWithoutExtension(files[0]);
                    //LoadReportCommentData(fileName);
                    LoadReportCommentData();
                }
            }
            //End Code For Auto Load data For Edit



            foreach (FontFamily font in FontFamily.Families)
            {
                ComboFont.Items.Add(font.Name);
            }
            ComboFont.SelectedItem = "Arial"; // default
            comboSize.Items.AddRange(new object[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 });
            comboSize.SelectedItem = 12;


            foreach (FontFamily font in FontFamily.Families)
            {
                comboresultfontstyle.Items.Add(font.Name);
            }
            comboresultfontstyle.SelectedItem = "Arial";
            comboresultfontsize.Items.AddRange(new object[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 });
            comboresultfontsize.SelectedItem = 12;
        }

        private void ToggleFontStyle(RichTextBox box, FontStyle styleToToggle, ref FontStyle currentStyle, System.Windows.Forms.Button button)
        {
            if ((currentStyle & styleToToggle) == styleToToggle)
            {
                currentStyle &= ~styleToToggle;
                button.BackColor = SystemColors.Control; // Not active
            }
            else
            {
                currentStyle |= styleToToggle;
                button.BackColor = Color.LightBlue; // Active
            }

            if (box.SelectionFont != null)
            {
                string fontName = box.SelectionFont.FontFamily.Name;
                float fontSize = box.SelectionFont.Size;

                box.SelectionFont = new Font(fontName, fontSize, currentStyle);
            }
        }

        private void ApplyFontStyle()
        {
            if (ComboFont.SelectedItem == null || comboSize.SelectedItem == null)
                return;

            string fontName = ComboFont.SelectedItem.ToString();
            float fontSize = float.Parse(comboSize.SelectedItem.ToString());



            richTextPatient.SelectionFont = new Font(fontName, fontSize, currentStyle);

        }

        private void ApplyFontStyle1()
        {
            if (comboresultfontstyle.SelectedItem == null || comboresultfontsize.SelectedItem == null)
                return;

            string fontName = comboresultfontstyle.SelectedItem.ToString();
            float fontSize = float.Parse(comboresultfontsize.SelectedItem.ToString());




            richTextResult.SelectionFont = new Font(fontName, fontSize, currentStyle);
        }

       


        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }
        private void richTextPatient_TextChanged(object sender, EventArgs e)
        {
            //int remaining = 500 - richTextPatient.Text.Length;
            //label40.Text = $"({remaining}/500)";

            int maxLength = 500;

            if (richTextPatient.Text.Length > maxLength)
            {
                MessageBox.Show("Maximum 500 characters allowed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                richTextPatient.Text = richTextPatient.Text.Substring(0, maxLength);
                richTextPatient.SelectionStart = richTextPatient.Text.Length;
            }

            int remaining = maxLength - richTextPatient.Text.Length;
            label40.Text = $"({remaining}/500)";

        }
        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            //int remaining = 500 - richTextResult.Text.Length;
            //label41.Text = $"({remaining}/500)";

            int maxLength = 500;

            if (richTextResult.Text.Length > maxLength)
            {
                MessageBox.Show("Maximum 500 characters allowed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                richTextResult.Text = richTextResult.Text.Substring(0, maxLength);
                richTextResult.SelectionStart = richTextResult.Text.Length;
            }

            int remaining = maxLength - richTextResult.Text.Length;
            label41.Text = $"({remaining}/500)";
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

       

        private void textPdetiso_TextChanged(object sender, EventArgs e)
        {

        }

       

        private void comboSphincter_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

       

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnBold_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextPatient, FontStyle.Bold, ref currentStylePatient, btnBold);
        }

        private void btnItalic_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextPatient, FontStyle.Italic, ref currentStylePatient, btnItalic);
        }

        private void btnUnderLine_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextPatient, FontStyle.Underline, ref currentStylePatient, btnUnderLine);
        }

        private void comboresultfontstyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFontStyle1();
        }

        private void comboresultfontsize_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFontStyle1();
        }

        private void btnBold2_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextResult, FontStyle.Bold, ref currentStyleResult, btnBold2);
        }

        private void btnitalic2_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextResult, FontStyle.Italic, ref currentStyleResult, btnitalic2);
        }

        private void btnunderline2_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(richTextResult, FontStyle.Underline, ref currentStyleResult, btnunderline2);
        }

        private void ComboFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFontStyle1();
        }

        private void comboSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFontStyle();
        }

       

        private void comboSensations_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboAnalTone_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void comboVoidningPressure_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void comboDetrusor_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboVoidedVolume_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public class ReportCommentViewModel
        {
            // 🔑 Patient identity
            public string MainId { get; set; }
            public string PatientNo { get; set; }
            public string TestName { get; set; }

            public string TestPosition { get; set; } //Dropdown
            public string CatheterType { get; set; } //Dropdown
            public string InfusionRate { get; set; }

            public string Sensations { get; set; } //Dropdown
            public string AnalTone { get; set; } //Dropdown
            public string ClitoraBulboca { get; set; } //Dropdown
            public string ValuntaryContraction { get; set; } //Dropdown

            public string FlowRate { get; set; } //Dropdown
            public string VoidedVolume { get; set; } //Dropdown
            public string PostVoid { get; set; }
            public string BladderCapacity { get; set; } //Dropdown
            public string Proprioception { get; set; } //Dropdown
            public string Complaince { get; set; } //Dropdown
            public string PDET { get; set; } 
            public string VoidingPressure { get; set; } //Dropdown
            public string Detrusor { get; set; } //Dropdown
            public string PdetVoid { get; set; } 
            public string PdetLeak { get; set; } 

            public string ThereIsOne { get; set; } //Dropdown
            public string CoughStress { get; set; } //Dropdown
            public string ThereIsTwo { get; set; } //Dropdown
            public string Sphincter { get; set; } //Dropdown

            public string PatientHistory { get; set; } //TestArea
            public string ResultConclusion { get; set; } //TestArea

            public string UrethralClosure { get; set; } //Dropdown
            public string ReportBy { get; set; } 
        }

       
        public void SetPatientContext(string mainId, string patientNo, string testName)
        {
            _mainId = mainId;
            _patientNo = patientNo;
            _testName = testName;

            LoadReportCommentData();
        }
        private string GetReportCommentFilePath()
        {
            string folder1 = Path.Combine(
                Application.StartupPath,
                "Saved Data",
                "ReportComment",
                $"{_patientNo}_{_mainId}"
            );
            string specificfolder = Path.Combine("ReportComment", $"{_patientNo}_{_mainId}");
            string folder = AppPathManager.GetFolderPath(specificfolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, $"{_testName}.dat");
        }

        //private string GetReportCommentFilePath(string testPosition)
        //{
        //    string exeFolder = Application.StartupPath;

        //    string folder = Path.Combine(exeFolder, "Saved Data", "ReportComment");

        //    if (!Directory.Exists(folder))
        //        Directory.CreateDirectory(folder);

        //    return Path.Combine(folder, testPosition + ".dat");
        //}

        //private void buttonok_Click(object sender, EventArgs e)
        //{

        //    // Create PatientRecord object
        //    var record = new ReportCommentViewModel
        //    {
        //        MainId = _mainId,
        //        PatientNo = _patientNo,
        //        TestName = _testName,

        //        TestPosition = comboTestPosition.SelectedItem?.ToString() ?? "",
        //        CatheterType = comboCatherType.SelectedItem?.ToString() ?? "",
        //        InfusionRate = textInfusionRate.Text,

        //        //Sensations = comboSensations.SelectedItem?.ToString() ?? "",
        //        Sensations = string.IsNullOrWhiteSpace(comboSensations.Text) ? "" : comboSensations.Text.Trim(),

        //        AnalTone = comboAnalTone.SelectedItem?.ToString() ?? "",
        //        ClitoraBulboca = comboClitoral.SelectedItem?.ToString() ?? "",
        //        ValuntaryContraction = comboValuntaryContraction.SelectedItem?.ToString() ?? "",

        //        FlowRate = comboFlowrate.SelectedItem?.ToString() ?? "",
        //        VoidedVolume = comboVoidedVolume.SelectedItem?.ToString() ?? "",
        //        PostVoid = textPostVoid.Text,
        //        BladderCapacity = comboBladderCapacity.SelectedItem?.ToString() ?? "",
        //        Proprioception = comboProprioceptions.SelectedItem?.ToString() ?? "",
        //        Complaince = comboComplaince.SelectedItem?.ToString() ?? "",
        //        PDET = textPdetiso.Text,
        //        VoidingPressure = comboVoidningPressure.SelectedItem?.ToString() ?? "",
        //        Detrusor = comboDetrusor.SelectedItem?.ToString() ?? "",
        //        PdetVoid = textPdetVoid.Text,
        //        PdetLeak = textPdetLeak.Text,

        //        ThereIsOne = comboThereis1.SelectedItem?.ToString() ?? "",
        //        CoughStress = comboCoughStressTest.SelectedItem?.ToString() ?? "",
        //        ThereIsTwo = comboThereIs2.SelectedItem?.ToString() ?? "",
        //        Sphincter = comboSphincter.SelectedItem?.ToString() ?? "",

        //        PatientHistory = richTextPatient.Text,
        //        ResultConclusion = richTextResult.Text,

        //        UrethralClosure = comboUrethralClosure.SelectedItem?.ToString() ?? "",
        //        //ReportBy = textReportedBy.Text,
        //        ReportBy = cmbReferredBy.SelectedItem?.ToString() ?? "",
        //    };

        //    string jsonData = System.Text.Json.JsonSerializer.Serialize(record);

        //    byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

        //    //string filePath = GetReportCommentFilePath(record.TestPosition);
        //    //File.WriteAllBytes(filePath, encryptedData);

        //    File.WriteAllBytes(GetReportCommentFilePath(), encryptedData);


        //    //MessageBox.Show(isEditMode ? "Report & Comments data updated successfully." : "Report & Comments data saved successfully.");

        //    // 🟢 Show success notification (auto closes in 2 seconds)
        //    var notification = new NotificationForm(
        //        isEditMode
        //            ? "Report & Comments data updated successfully."
        //            : "Report & Comments data saved successfully.",
        //        1200 // 2 seconds
        //    );
        //    notification.TopMost = true;
        //    notification.Show();

        //    // 🟢 Close the current form after a short delay
        //    Task.Delay(1200).ContinueWith(_ =>
        //    {
        //        if (this.InvokeRequired)
        //            this.Invoke(new Action(() => this.Close()));
        //        else
        //            this.Close();
        //    });
        //}


        private void buttonok_Click(object sender, EventArgs e)
        {

            // Create PatientRecord object
            var record = new ReportCommentViewModel
            {
                MainId = _mainId,
                PatientNo = _patientNo,
                TestName = _testName,

                //TestPosition = comboTestPosition.SelectedItem?.ToString() ?? "",
                TestPosition = string.IsNullOrWhiteSpace(comboTestPosition.Text) ? "" : comboTestPosition.Text.Trim(),

                //CatheterType = comboCatherType.SelectedItem?.ToString() ?? "",
                CatheterType = string.IsNullOrWhiteSpace(comboCatherType.Text) ? "" : comboCatherType.Text.Trim(),

                InfusionRate = textInfusionRate.Text,

                //Sensations = comboSensations.SelectedItem?.ToString() ?? "",
                Sensations = string.IsNullOrWhiteSpace(comboSensations.Text) ? "" : comboSensations.Text.Trim(),

                //AnalTone = comboAnalTone.SelectedItem?.ToString() ?? "",
                AnalTone = string.IsNullOrWhiteSpace(comboAnalTone.Text) ? "" : comboAnalTone.Text.Trim(),

                //ClitoraBulboca = comboClitoral.SelectedItem?.ToString() ?? "",
                ClitoraBulboca = string.IsNullOrWhiteSpace(comboClitoral.Text) ? "" : comboClitoral.Text.Trim(),

                //ValuntaryContraction = comboValuntaryContraction.SelectedItem?.ToString() ?? "",
                ValuntaryContraction = string.IsNullOrWhiteSpace(comboValuntaryContraction.Text) ? "" : comboValuntaryContraction.Text.Trim(),

                //FlowRate = comboFlowrate.SelectedItem?.ToString() ?? "",
                FlowRate = string.IsNullOrWhiteSpace(comboFlowrate.Text) ? "" : comboFlowrate.Text.Trim(),

                //VoidedVolume = comboVoidedVolume.SelectedItem?.ToString() ?? "",
                VoidedVolume = string.IsNullOrWhiteSpace(comboVoidedVolume.Text) ? "" : comboVoidedVolume.Text.Trim(),

                PostVoid = textPostVoid.Text,

                //BladderCapacity = comboBladderCapacity.SelectedItem?.ToString() ?? "",
                BladderCapacity = string.IsNullOrWhiteSpace(comboBladderCapacity.Text) ? "" : comboBladderCapacity.Text.Trim(),

                //Proprioception = comboProprioceptions.SelectedItem?.ToString() ?? "",
                Proprioception = string.IsNullOrWhiteSpace(comboProprioceptions.Text) ? "" : comboProprioceptions.Text.Trim(),

                //Complaince = comboComplaince.SelectedItem?.ToString() ?? "",
                Complaince = string.IsNullOrWhiteSpace(comboComplaince.Text) ? "" : comboComplaince.Text.Trim(),

                PDET = textPdetiso.Text,

                //VoidingPressure = comboVoidningPressure.SelectedItem?.ToString() ?? "",
                VoidingPressure = string.IsNullOrWhiteSpace(comboVoidningPressure.Text) ? "" : comboVoidningPressure.Text.Trim(),

                //Detrusor = comboDetrusor.SelectedItem?.ToString() ?? "",
                Detrusor = string.IsNullOrWhiteSpace(comboDetrusor.Text) ? "" : comboDetrusor.Text.Trim(),

                PdetVoid = textPdetVoid.Text,
                PdetLeak = textPdetLeak.Text,

                //ThereIsOne = comboThereis1.SelectedItem?.ToString() ?? "",
                ThereIsOne = string.IsNullOrWhiteSpace(comboThereis1.Text) ? "" : comboThereis1.Text.Trim(),

                //CoughStress = comboCoughStressTest.SelectedItem?.ToString() ?? "",
                CoughStress = string.IsNullOrWhiteSpace(comboCoughStressTest.Text) ? "" : comboCoughStressTest.Text.Trim(),

                //ThereIsTwo = comboThereIs2.SelectedItem?.ToString() ?? "",
                ThereIsTwo = string.IsNullOrWhiteSpace(comboThereIs2.Text) ? "" : comboThereIs2.Text.Trim(),

                //Sphincter = comboSphincter.SelectedItem?.ToString() ?? "",
                Sphincter = string.IsNullOrWhiteSpace(comboSphincter.Text) ? "" : comboSphincter.Text.Trim(),

                PatientHistory = richTextPatient.Text,
                ResultConclusion = richTextResult.Text,

                //UrethralClosure = comboUrethralClosure.SelectedItem?.ToString() ?? "",
                UrethralClosure = string.IsNullOrWhiteSpace(comboUrethralClosure.Text) ? "" : comboUrethralClosure.Text.Trim(),

                //ReportBy = textReportedBy.Text,
                //ReportBy = cmbReferredBy.SelectedItem?.ToString() ?? "",
                ReportBy = string.IsNullOrWhiteSpace(cmbReferredBy.Text) ? "" : cmbReferredBy.Text.Trim(),
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);

            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            //string filePath = GetReportCommentFilePath(record.TestPosition);
            //File.WriteAllBytes(filePath, encryptedData);

            File.WriteAllBytes(GetReportCommentFilePath(), encryptedData);


            //MessageBox.Show(isEditMode ? "Report & Comments data updated successfully." : "Report & Comments data saved successfully.");

            // 🟢 Show success Massage (auto closes in 2 seconds)
            //var notification = new NotificationForm(
            //    isEditMode
            //        ? "Report & Comments data updated successfully."
            //        : "Report & Comments data saved successfully.",
            //    1200 // 2 seconds
            //);
            //notification.TopMost = true;
            //notification.Show();

            //Task.Delay(1200).ContinueWith(_ =>
            //{
            //    if (this.InvokeRequired)
            //        this.Invoke(new Action(() => this.Close()));
            //    else
            //        this.Close();
            //});

            this.Close();
        }

        private void LoadReportCommentData()
        {
            string path = GetReportCommentFilePath();
            if (!File.Exists(path))
            {
                isEditMode = false;
                return;
            }

            try
            {
                byte[] encrypted = File.ReadAllBytes(path);
                string json = CryptoHelper.Decrypt(encrypted);

                var record = JsonSerializer.Deserialize<ReportCommentViewModel>(json);
                if (record == null) return;

                isEditMode = true;

                comboTestPosition.Text = record.TestPosition;
                comboCatherType.Text = record.CatheterType;
                textInfusionRate.Text = record.InfusionRate;

                comboSensations.Text = record.Sensations;
                comboAnalTone.Text = record.AnalTone;
                comboClitoral.Text = record.ClitoraBulboca;
                comboValuntaryContraction.Text = record.ValuntaryContraction;

                comboFlowrate.Text = record.FlowRate;
                comboVoidedVolume.Text = record.VoidedVolume;
                textPostVoid.Text = record.PostVoid;
                comboBladderCapacity.Text = record.BladderCapacity;
                comboProprioceptions.Text = record.Proprioception;
                comboComplaince.Text = record.Complaince;
                textPdetiso.Text = record.PDET;
                comboVoidningPressure.Text = record.VoidingPressure;
                comboDetrusor.Text = record.Detrusor;
                textPdetVoid.Text = record.PdetVoid;
                textPdetLeak.Text = record.PdetLeak;

                comboThereis1.Text = record.ThereIsOne;
                comboCoughStressTest.Text = record.CoughStress;
                comboThereIs2.Text = record.ThereIsTwo;
                comboSphincter.Text = record.Sphincter;

                richTextPatient.Text = record.PatientHistory;
                richTextResult.Text = record.ResultConclusion;

                comboUrethralClosure.Text = record.UrethralClosure;
                //textReportedBy.Text = record.ReportBy;

                if (cmbReferredBy.Items.Contains(record.ReportBy))
                    cmbReferredBy.SelectedItem = record.ReportBy;
                else
                    cmbReferredBy.Text = record.ReportBy;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading report: " + ex.Message);
            }
        }




        private void cmbReferredBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbReferredBy.SelectedItem != null)
            {
                string selectedDoctorName = cmbReferredBy.SelectedItem.ToString();
                //MessageBox.Show($"Selected Doctor: {selectedDoctorName}");
            }
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

        //public void LoadReportCommentData(string testPosition)
        //{
        //    string filePath = GetReportCommentFilePath(testPosition);
        //    if (File.Exists(filePath))
        //    {
        //        try
        //        {
        //            byte[] encryptedData = File.ReadAllBytes(filePath);
        //            string jsonData = CryptoHelper.Decrypt(encryptedData);

        //            var record = System.Text.Json.JsonSerializer.Deserialize<ReportCommentViewModel>(jsonData);

        //            if (record != null)
        //            {
        //                isEditMode = true;

        //                comboTestPosition.SelectedItem = record.TestPosition;
        //                comboCatherType.SelectedItem = record.CatheterType;
        //                textInfusionRate.Text = record.InfusionRate;

        //                comboSensations.SelectedItem = record.Sensations;
        //                comboAnalTone.SelectedItem = record.AnalTone;
        //                comboClitoral.SelectedItem = record.ClitoraBulboca;
        //                comboValuntaryContraction.SelectedItem = record.ValuntaryContraction;

        //                comboFlowrate.SelectedItem = record.FlowRate;
        //                comboVoidedVolume.SelectedItem = record.VoidedVolume;
        //                textPostVoid.Text = record.PostVoid;
        //                comboBladderCapacity.SelectedItem = record.BladderCapacity;
        //                comboProprioceptions.SelectedItem = record.Proprioception;
        //                comboComplaince.SelectedItem = record.Complaince;
        //                textPdetiso.Text = record.PDET;
        //                comboVoidningPressure.SelectedItem = record.VoidingPressure;
        //                comboDetrusor.SelectedItem = record.Detrusor;
        //                textPdetVoid.Text = record.PdetVoid;
        //                textPdetLeak.Text = record.PdetLeak;

        //                comboThereis1.SelectedItem = record.ThereIsOne;
        //                comboCoughStressTest.SelectedItem = record.CoughStress;
        //                comboThereIs2.SelectedItem = record.ThereIsTwo;
        //                comboSphincter.SelectedItem = record.Sphincter;

        //                richTextPatient.Text = record.PatientHistory;
        //                richTextResult.Text = record.ResultConclusion;

        //                comboUrethralClosure.SelectedItem = record.UrethralClosure;
        //                textReportedBy.Text = record.ReportBy;


        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Error loading Report & Comments Data : " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        isEditMode = false;

        //    }
        //}

    }
}
