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
using static SantronWinApp.Symptoms;

namespace SantronWinApp
{
    public partial class SymptomsEditForm : Form
    {
        private bool isEditMode;

        public SymptomsEditForm()
        {
            InitializeComponent();
        }

        private void SymptomsEditForm_Load(object sender, EventArgs e)
        {

        }

        private string GetSymptomsFilePath(string symptomNames)
        {
            string exeFolder = Application.StartupPath;

           // string folder = Path.Combine(exeFolder, "Saved Data", "SymtomsData");
            string folder = AppPathManager.GetFolderPath("SymtomsData");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, symptomNames + ".dat");
        }

        public void LoadSymptomsDataForEdit(string symptomNames)
        {
            string filePath = GetSymptomsFilePath(symptomNames);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<SymptomsViewModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtSymptom.Text = record.SymptomsName;
                        txtSymCode.Text = record.SymptomsCode;
                        txtSymCode.ForeColor = Color.Black;

                        CategoryDropdown.SelectedItem = record.Category;
                        CategoryDropdown.ForeColor = Color.Black;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading symptoms Data : " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
            }
        }

        private void Edit_Button_Click(object sender, EventArgs e)
        {
            string filePath = GetSymptomsFilePath(txtSymptom.Text);

            if (!File.Exists(filePath))
            {
                MessageBox.Show("No record found to update. Please save the symptoms data first.");
                return;
            }


            var record = new SymptomsViewModel
            {
                SymptomsName = txtSymptom.Text,
                SymptomsCode = txtSymCode.Text,
                Category = CategoryDropdown.SelectedItem?.ToString() ?? ""
            };

            string jsonData = System.Text.Json.JsonSerializer.Serialize(record);
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show("Symptoms data updated successfully.");

            //Start This code For auto close SymptomsList Form and auto Open
            foreach (Form frm in Application.OpenForms.OfType<SymptomsList>().ToList())
            {
                frm.Close();
            }

            SymptomsList newList = new SymptomsList();
            newList.Show();
            //End This code For auto close SymptomsList Form and auto Open

            this.Close();

        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
