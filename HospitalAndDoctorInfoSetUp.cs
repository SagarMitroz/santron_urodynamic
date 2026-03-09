using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SantronWinApp.SystemSetup;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SantronWinApp
{
    public partial class HospitalAndDoctorInfoSetUp : Form
    {
        private bool isEditMode;
        //private string hospitalName;
        private Image logoImage = null;
        private Image letterHeadImage = null;
        public HospitalAndDoctorInfoSetUp()
        {
            InitializeComponent();
            this.Load += HospitalAndDoctorInfoSetUp_Load;

            //this.hospitalName = hospitalName;

            this.MaximizeBox = false;                
            //this.FormBorderStyle = FormBorderStyle.FixedSingle;  
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        // ✅ Placeholder helper method
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

        private void textAddress1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textAddress2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void HospitalAndDoctorInfoSetUp_Load(object sender, EventArgs e)
        {
            radioButton1.Checked = true;

            _isUpdating = true;
            DefaultH.Checked = true;
            _isUpdating = false;

            //LoadHospitalDocterData(hospitalName);


            //Start Code For Auto Load data
            //string folder = Path.Combine(Application.StartupPath, "Saved Data", "HospitalAndDocter");
            string folder = AppPathManager.GetFolderPath("HospitalAndDocter");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.dat");
                if (files.Length > 0)
                {
                    // Load the first available setup file
                    string fileName = Path.GetFileNameWithoutExtension(files[0]);
                    LoadHospitalDocterData(fileName);
                }
            }
            //End Code For Auto Load data



            //this.BackColor = ColorTranslator.FromHtml("#D8E3F9");

            // Style the button buttonColor0
            //buttonCancel.FlatStyle = FlatStyle.Flat;
            //buttonCancel.BackColor = ColorTranslator.FromHtml("#FFFFFF");
            //buttonCancel.ForeColor = Color.Black;
            //buttonCancel.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#FFFFFF");
            //buttonCancel.FlatAppearance.BorderSize = 1;  // or 0 if you want no border
            //buttonCancel.Font = new Font("Segoe UI", 8, FontStyle.Bold);


            // Style the button buttonColor0
            //btnOk.FlatStyle = FlatStyle.Flat;
            //btnOk.BackColor = ColorTranslator.FromHtml("#25459D");
            //btnOk.ForeColor = Color.White;
            //btnOk.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#25459D");
            //btnOk.FlatAppearance.BorderSize = 1;  // or 0 if you want no border
            //btnOk.Font = new Font("Segoe UI", 8, FontStyle.Bold);


            // ✅ Set placeholder text for your fields

            //SetPlaceholder(textAddress, "Enter address line");
            //SetPlaceholder(textEmail, "Enter email id ");
            //SetPlaceholder(textPhone, "Enter phone number");  
                                                              
            //SetPlaceholder(textPost, "Enter Post");
            //SetPlaceholder(textDrName, "Enter dr name");
            //SetPlaceholder(textDegree, "Enter degree");  
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtHopitalName_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextAddress_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnEdit_Click(object sender, EventArgs e)
        {

        }





        // *************** Start For Save Data **************** //

        public class HospitalAndDocterModel
        {
            public string HospitalName { get; set; }
            public string Address { get; set; }
            public string AddressTwo { get; set; }
            public string PhoneNo { get; set; }
            public string Email { get; set; }
            public string DocterName { get; set; }
            public string Degree { get; set; }
            public string Post { get; set; }
            public bool Nomograms { get; set; }
            public bool PrintFlowNomograms { get; set; }
            public bool PrintImpression { get; set; }
            public bool HeadOne { get; set; }
            public bool HeadTwo { get; set; }
            public bool HeadThree { get; set; }
            public bool HeadFour { get; set; }
            public bool HeadFive { get; set; }
            public bool CompanyName { get; set; }
            public string PrintForm { get; set; }
            public string Logo { get; set; }
            public byte[] LogoImage { get; set; }
            public byte[] LetterHeadImage { get; set; }  // Add this for letterhead image

            public bool DefaultHeader { get; set; }
            public bool LetterHead { get; set; }
            public bool HospitalLogo { get; set; }

            public bool TextAndLogo { get; set; }
        }

        public static class CryptoHelper
        {
            private static readonly string keyString = "MySuperSecretKey123"; // Must be 16, 24, or 32 bytes
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

        private string GetHospitalAndDocterFilePath(string hospitalName)
        {
            string exeFolder = Application.StartupPath;

            string folder = Path.Combine(exeFolder, "Saved Data", "HospitalAndDocter");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, hospitalName + ".dat");
        }

        private byte[] ImageToByteArray(Image img)
        {
            if (img == null) return null;
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png); // Save as PNG
                return ms.ToArray();
            }
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            string logo = "";
            if (radioButton1.Checked) logo = "UploadLogo";
            else if (radioButton2.Checked) logo = "UploadLetterHead";


            var data = new HospitalAndDocterModel
            {
                HospitalName = txtHopitalName.Text,
                Address = textAddress.Text,
                AddressTwo = txtAddress2.Text,
                PhoneNo = textPhone.Text,
                Email = textEmail.Text,
                DocterName = textDrName.Text,
                Degree = textDegree.Text,
                Post = textPost.Text,
                Nomograms = checkNomograms.Checked,
                PrintFlowNomograms = checkPrintFlowNomograms.Checked,
                PrintImpression = checkPrintImpression.Checked,
                //HeadOne = checkHead1.Checked,
                //HeadTwo = checkHead2.Checked,   
                //HeadThree = checkHead3.Checked,
                //HeadFour = checkHead4.Checked,
                //HeadFive = checkHead5.Checked,
                HeadOne = checkBoxDropdown1.IsChecked(0),
                HeadThree = checkBoxDropdown1.IsChecked(1),
                HeadFour = checkBoxDropdown1.IsChecked(2),
                HeadFive = checkBoxDropdown1.IsChecked(3),
                CompanyName = checkCompanyName.Checked,

                //DefaultHeader = DefaultH.Checked,
                //LetterHead = LetterH.Checked,
                //HospitalLogo = HospitalLogo.Checked,
                //TextAndLogo = TextAndLogo.Checked,
                DefaultHeader = checkBoxDropdown2.IsChecked(0),
                LetterHead = checkBoxDropdown2.IsChecked(1),
                HospitalLogo = checkBoxDropdown2.IsChecked(2),
                TextAndLogo = checkBoxDropdown2.IsChecked(3),

                //PrintForm = comboSelect.Text,
                PrintForm = txtPrintFont.Text,
                Logo = logo,
                LogoImage = ImageToByteArray(logoImage),
                LetterHeadImage = ImageToByteArray(letterHeadImage)
            };

            // Serialize to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);

            // Encrypt
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            // string filePath = GetHospitalAndDocterFilePath(data.HospitalName);
            //string filePath = AppPathManager.GetFilePath("HospitalAndDocter", data.HospitalName);
            string filePath = AppPathManager.GetFilePath("HospitalAndDocter", "HospitalAndDocterFile");

            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show(isEditMode ? "Hospital And Docter data updated successfully." : "Hospital And Docter data saved successfully.");
            //this.Close();

            // 🟢 Show success Massage (auto closes in 2 seconds)
            //var notification = new NotificationForm(
            //    isEditMode
            //        ? "Hospital And Doctor data updated successfully."
            //        : "Hospital And Doctor data saved successfully.",
            //    1000 
            //);
            //notification.TopMost = true;
            //notification.Show();

            //// 🟢 Close the current form after a short delay
            //Task.Delay(1000).ContinueWith(_ =>
            //{
            //    if (this.InvokeRequired)
            //        this.Invoke(new Action(() => this.Close()));
            //    else
            //        this.Close();
            //});

            this.Close();
        }


        private Image ByteArrayToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return Image.FromStream(ms);
            }
        }

        private void LoadHospitalDocterData(string hospitalName)
        {
            //string filePath = GetHospitalAndDocterFilePath(hospitalName);
            string filePath = AppPathManager.GetFilePath("HospitalAndDocter", hospitalName);

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<HospitalAndDocterModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        txtHopitalName.Text = record.HospitalName;
                        textAddress.Text = record.Address;
                        txtAddress2.Text = record.AddressTwo;
                        textPhone.Text = record.PhoneNo;
                        textEmail.Text = record.Email;
                        textDrName.Text = record.DocterName;
                        textDegree.Text = record.Degree;
                        textPost.Text = record.Post;
                        //comboSelect.Text = record.PrintForm;
                        txtPrintFont.Text = record.PrintForm;

                        //Code For CheckBox
                        checkNomograms.Checked = record.Nomograms;
                        checkPrintFlowNomograms.Checked = record.PrintFlowNomograms;
                        checkPrintImpression.Checked = record.PrintImpression;
                        //checkHead1.Checked = record.HeadOne;
                        //checkHead2.Checked = record.HeadTwo;
                        //checkHead3.Checked = record.HeadThree;
                        //checkHead4.Checked = record.HeadFour;
                        //checkHead5.Checked = record.HeadFive;
                        checkBoxDropdown1.SetChecked(0, record.HeadOne);
                        checkBoxDropdown1.SetChecked(1, record.HeadThree);
                        checkBoxDropdown1.SetChecked(2, record.HeadFour);
                        checkBoxDropdown1.SetChecked(3, record.HeadFive);

                        checkCompanyName.Checked = record.CompanyName;

                        //DefaultH.Checked = record.DefaultHeader;
                        //LetterH.Checked = record.LetterHead;
                        //HospitalLogo.Checked = record.HospitalLogo;
                        //TextAndLogo.Checked = record.TextAndLogo;
                        checkBoxDropdown2.SetChecked(0, record.DefaultHeader);
                        checkBoxDropdown2.SetChecked(1, record.LetterHead);
                        checkBoxDropdown2.SetChecked(2, record.HospitalLogo);
                        checkBoxDropdown2.SetChecked(3, record.TextAndLogo);

                        // Load both images
                        if (record.LogoImage != null)
                        {
                            logoImage = ByteArrayToImage(record.LogoImage);
                        }

                        if (record.LetterHeadImage != null)
                        {
                            letterHeadImage = ByteArrayToImage(record.LetterHeadImage);
                        }

                        radioButton1.Checked = record.Logo == "UploadLogo";
                        radioButton2.Checked = record.Logo == "UploadLetterHead";

                        //if (record.LogoImage != null)
                        //{
                        //    logoImage = ByteArrayToImage(record.LogoImage);
                        //    logoupload.Invalidate();  // refresh panel to show image
                        //}
                        //else
                        //{
                        //    logoImage = null; // no saved image
                        //    logoupload.Invalidate();
                        //}
                        UpdateDisplayedImage();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading Hospital And Docter: " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
                //ClearFieldsExceptPatientNo();
            }
        }


        // Add this helper method to update the displayed image
        private void UpdateDisplayedImage()
        {
            if (radioButton1.Checked && logoImage != null)
            {
                logoupload.Invalidate();  // Will show logoImage in Paint event
            }
            else if (radioButton2.Checked && letterHeadImage != null)
            {
                logoupload.Invalidate();  // Will show letterHeadImage in Paint event
            }
            else
            {
                logoImage = null; // Clear for Paint event to show "No Logo" text
                logoupload.Invalidate();
            }
        }
        private void txtHopitalName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                LoadHospitalDocterData(txtHopitalName.Text.Trim());
            }
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void existButton_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        //private void logoupload_Paint(object sender, PaintEventArgs e)
        //{
        //    if (logoImage != null)
        //    {
        //        // Stretch the image to fill the panel area
        //        e.Graphics.DrawImage(logoImage, logoupload.ClientRectangle);
        //    }
        //    else
        //    {
        //        // Optional: show "No Image" text
        //        using (Font font = new Font("Arial", 10))
        //        using (Brush brush = new SolidBrush(Color.Gray))
        //        {
        //            string text = "No Logo";
        //            SizeF textSize = e.Graphics.MeasureString(text, font);
        //            PointF location = new PointF(
        //                (logoupload.Width - textSize.Width) / 2,
        //                (logoupload.Height - textSize.Height) / 2);
        //            e.Graphics.DrawString(text, font, brush, location);
        //        }
        //    }
        //}

        private void logoupload_Paint(object sender, PaintEventArgs e)
        {
            Image imageToShow = null;

            if (radioButton1.Checked)
            {
                imageToShow = logoImage;
            }
            else if (radioButton2.Checked)
            {
                imageToShow = letterHeadImage;
            }

            if (imageToShow != null)
            {
                // Stretch the image to fill the panel area
                e.Graphics.DrawImage(imageToShow, logoupload.ClientRectangle);
            }
            else
            {
                // Optional: show "No Image" text
                using (Font font = new Font("Arial", 10))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    string text = radioButton1.Checked ? "No Logo" : "No LetterHead";
                    SizeF textSize = e.Graphics.MeasureString(text, font);
                    PointF location = new PointF(
                        (logoupload.Width - textSize.Width) / 2,
                        (logoupload.Height - textSize.Height) / 2);
                    e.Graphics.DrawString(text, font, brush, location);
                }
            }
        }



        //private void logoButton_Click(object sender, EventArgs e)
        //{

        //    if (radioButton1.Checked)
        //    {
        //        OpenFileDialog openFileDialog = new OpenFileDialog();
        //        openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png"; // Allow only JPG/PNG

        //        if (openFileDialog.ShowDialog() == DialogResult.OK)
        //        {
        //            FileInfo fileInfo = new FileInfo(openFileDialog.FileName);

        //            // Validate file size < 1 MB
        //            if (fileInfo.Length > 1024 * 1024)
        //            {
        //                MessageBox.Show("File size must be less than 1 MB.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                return;
        //            }

        //            // Load image
        //            logoImage = Image.FromFile(openFileDialog.FileName);
        //            logoupload.Invalidate();
        //        }
        //    }
        //    else if (radioButton2.Checked)
        //    {
        //        MessageBox.Show("Logo upload is disabled in this mode.", "Read Only", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }


        //}

        private void logoButton_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png"; // Allow only JPG/PNG

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);

                    // Validate file size < 1 MB
                    if (fileInfo.Length > 1024 * 1024)
                    {
                        MessageBox.Show("File size must be less than 1 MB.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Load and save logo image
                    if (logoImage != null)
                    {
                        logoImage.Dispose();
                    }
                    logoImage = Image.FromFile(openFileDialog.FileName);
                    logoupload.Invalidate();
                }
            }
            else if (radioButton2.Checked)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);

                    // Validate file size < 1 MB
                    if (fileInfo.Length > 1024 * 1024)
                    {
                        MessageBox.Show("File size must be less than 1 MB.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Load and save letterhead image
                    if (letterHeadImage != null)
                    {
                        letterHeadImage.Dispose();
                    }
                    letterHeadImage = Image.FromFile(openFileDialog.FileName);
                    logoupload.Invalidate();
                }
            }
        }

        private void label36_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            //logoButton.Enabled = radioButton1.Checked;
            logoButton.Enabled = true; // Enable button for both modes

            if (radioButton1.Checked)
            {
                // Switch to logo mode - show logo image if exists
                UpdateDisplayedImage();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton2.Checked)
            //{
            //    logoImage = null;
            //    logoupload.Invalidate(); 
            //}

            logoButton.Enabled = true; // Enable button for both modes

            if (radioButton2.Checked)
            {
                // Switch to letterhead mode - show letterhead image if exists
                UpdateDisplayedImage();
            }
        }

        private void DefaultH_CheckedChanged(object sender, EventArgs e)
        {
            if (DefaultH.Checked)
                SetOnly(DefaultH);
            else
                EnsureDefaultIfNone();
        }

        private void LetterH_CheckedChanged(object sender, EventArgs e)
        {
            if (LetterH.Checked)
                SetOnly(LetterH);
            else
                EnsureDefaultIfNone();
        }

        private void HospitalLogo_CheckedChanged(object sender, EventArgs e)
        {
            if (HospitalLogo.Checked)
                SetOnly(HospitalLogo);
            else
                EnsureDefaultIfNone();
        }

        private bool _isUpdating = false;

        private void SetOnly(CheckBox selected)
        {
            if (_isUpdating) return;

            _isUpdating = true;

            DefaultH.Checked = false;
            LetterH.Checked = false;
            HospitalLogo.Checked = false;
            TextAndLogo.Checked = false;

            selected.Checked = true;

            _isUpdating = false;
        }

        private void EnsureDefaultIfNone()
        {
            if (_isUpdating) return;

            if (!DefaultH.Checked && !LetterH.Checked && !HospitalLogo.Checked && !TextAndLogo.Checked)
            {
                _isUpdating = true;
                DefaultH.Checked = true;
                _isUpdating = false;
            }
        }

        private void TextAndLogo_CheckedChanged(object sender, EventArgs e)
        {
            if (TextAndLogo.Checked)
                SetOnly(TextAndLogo);
            else
                EnsureDefaultIfNone();
        }

        private void checkBoxDropdown1_Load(object sender, EventArgs e)
        {
            // Add checkbox items
            checkBoxDropdown1.AddItems(
                "Print Letter Head Page 1",
                "Print Letter Head Page 3",
                "Print Letter Head Page 4",
                "Print Letter Head Page 5"
            );
        }



        private void checkBoxDropdown2_Load(object sender, EventArgs e)
        {
            // 🔒 Only one option allowed
            checkBoxDropdown2.SingleSelection = true; // ✅ works now

            checkBoxDropdown2.AddItems(
                "Only Text Header",
                "Letter Head Image",
                "Only Hospital Logo",
                "Text + Hospital Logo"
            );

        }
    }
}
