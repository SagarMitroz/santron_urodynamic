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
using static SantronWinApp.HospitalAndDoctorInfoSetUp;

namespace SantronWinApp
{
    public partial class ScaleAndColorSteup : Form
    {
        private bool isEditMode;
        public ScaleAndColorSteup()
        {
            InitializeComponent();
            this.Load += ScaleAndColorSteup_Load;


            this.MaximizeBox = false;                
            //this.FormBorderStyle = FormBorderStyle.FixedSingle;   
        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonColor0_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {

                textPlotColor0.BackColor = colorDialog1.Color;
 
            }
        }

        private void buttonColor1_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor1.BackColor = colorDialog1.Color;
            }

        }

        private void buttonColor2_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor2.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor3_Click_1(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor3.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor4_Click_1(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor4.BackColor = colorDialog1.Color;
            }
        }
         
 
        private void buttonColor5_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor5.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor6_Click_1(object sender, EventArgs e)
        {

            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textPlotColor6.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor7_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor7.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor8_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor8.BackColor = colorDialog1.Color;
            }
        }


        private void buttonColor9_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor9.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor10_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor10.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor11_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor11.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor12_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor12.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor13_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor13.BackColor = colorDialog1.Color;
            }
        }

        private void buttonColor14_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                textPlotColor14.BackColor = colorDialog1.Color;
            }
        }


        private void buttonBgColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textBackgroundColor.BackColor = colorDialog1.Color;
            }
        }

        private void buttonBladderColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textBladderSensation.BackColor = colorDialog1.Color; 
            }
        }

        private void buttonGeneralColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textGeneralPurpose.BackColor = colorDialog1.Color;
            }

        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Apply selected color to the TextBox background
                textResponseMarkers.BackColor = colorDialog1.Color;
            }
        }

        

        private void label27_Click(object sender, EventArgs e)
        {

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void ScaleAndColorSteup_Load(object sender, EventArgs e)
        {

            //Start Code For Auto Load data
            //string folder = Path.Combine(Application.StartupPath, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.dat");
                if (files.Length > 0)
                {
                    // Load the first available setup file
                    string fileName = Path.GetFileNameWithoutExtension(files[0]);
                    LoadScaleColorData(fileName);
                }
            }
            //End Code For Auto Load data




        }

        // *************** Start For Save Data *************** //

        public class ScaleAndColorModel
        {
            public string ChannelZero { get; set; }
            public string ChannelOne { get; set; }
            public string ChannelTwo { get; set; }
            public string ChannelThree { get; set; }
            public string ChannelFour { get; set; }
            public string ChannelFive { get; set; }
            public string ChannelSix { get; set; }
            public string ChannelSeven { get; set; }
            public string ChannelEight { get; set; }
            public string ChannelNine { get; set; }
            public string ChannelTen { get; set; }
            public string ChannelEleven { get; set; }
            public string ChannelTwelve { get; set; }
            public string ChannelThirteen { get; set; }
            public string ChannelFourteen { get; set; }

            public string PlotScaleZero { get; set; }
            public string PlotScaleOne { get; set; }
            public string PlotScaleTwo { get; set; }
            public string PlotScaleThree { get; set; }
            public string PlotScaleFour { get; set; }
            public string PlotScaleFive { get; set; }
            public string PlotScaleSix { get; set; }
            public string PlotScaleSeven { get; set; }
            public string PlotScaleEight { get; set; }
            public string PlotScale { get; set; }
            public string PlotScaleNine { get; set; }
            public string PlotScaleTen { get; set; }
            public string PlotScaleEleven { get; set; }
            public string PlotScaleTwelve { get; set; }
            public string PlotScaleThirteen { get; set; }
            public string PlotScaleFourteen { get; set; }

            public string ColorZero { get; set; }
            public string ColorOne { get; set; }
            public string ColorTwo { get; set; }
            public string ColorThree { get; set; }
            public string ColorFour { get; set; }
            public string ColorFive { get; set; }
            public string ColorSix { get; set; }
            public string ColorSeven { get; set; }
            public string ColorEight { get; set; }
            public string ColorNine { get; set; }
            public string ColorTen { get; set; }
            public string ColorEleven { get; set; }
            public string ColorTwelve { get; set; }
            public string ColorThirteen { get; set; }
            public string ColorFourteen { get; set; }

            public string BackgroundColor { get; set; }
            public int? BladderSensation { get; set; }
            public int? GeneralPurose { get; set; }
            public int? ResponeMarkers { get; set; }

            public string NumberOfScreen { get; set; }
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

        private string GetScaleAndColorSetupFilePath(string ScaleColorSetup)
        {
            //string exeFolder = Application.StartupPath;
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;

            //string folder = Path.Combine(exeFolder, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "ScaleColorSetupFile" + ".dat");
        }
        public event EventHandler DataSaved;
        private void buttonOk_Click(object sender, EventArgs e)
        {
            var data = new ScaleAndColorModel
            {
                ChannelZero = textChannel0.Text,
                ChannelOne = txtChannel1.Text,
                ChannelTwo = textChannel2.Text,
                ChannelThree = textChannel3.Text,
                ChannelFour = textChannel4.Text,
                ChannelFive = textChannel5.Text,
                ChannelSix = textChannel6.Text,
                ChannelSeven = textChannel7.Text,
                ChannelEight = textChannel8.Text,
                ChannelNine = textChannel9.Text,
                ChannelTen = textChannel10.Text,
                ChannelEleven = textChannel11.Text,
                ChannelTwelve = textChannel12.Text,
                ChannelThirteen = textChannel13.Text,
                ChannelFourteen = textChannel14.Text,

                PlotScaleZero = comboPlotScale0.Text,
                PlotScaleOne = comboPlotScale1.Text,
                PlotScaleTwo = comboPlotScale2.Text,
                PlotScaleThree = comboPlotScale3.Text,
                PlotScaleFour = comboPlotScale4.Text,
                PlotScaleFive = comboPlotscale5.Text,
                PlotScaleSix = comPlotScale6.Text,
                PlotScaleSeven = comPlotScale7.Text,
                PlotScaleEight = comPlotScale8.Text,
                PlotScaleNine = comPlotScale9.Text,
                PlotScaleTen = comPlotScale10.Text,
                PlotScaleEleven = comPlotScale11.Text,
                PlotScaleTwelve = comPlotScale12.Text,
                PlotScaleThirteen = comPlotScale13.Text,
                PlotScaleFourteen = comPlotScale14.Text,

                ColorZero = ColorTranslator.ToHtml(textPlotColor0.BackColor),
                ColorOne = ColorTranslator.ToHtml(textPlotColor1.BackColor),
                ColorTwo = ColorTranslator.ToHtml(textPlotColor2.BackColor),
                ColorThree = ColorTranslator.ToHtml(textPlotColor3.BackColor),
                ColorFour = ColorTranslator.ToHtml(textPlotColor4.BackColor),
                ColorFive = ColorTranslator.ToHtml(textPlotColor5.BackColor),
                ColorSix = ColorTranslator.ToHtml(textPlotColor6.BackColor),
                ColorSeven = ColorTranslator.ToHtml(textPlotColor7.BackColor),
                ColorEight = ColorTranslator.ToHtml(textPlotColor8.BackColor),
                ColorNine = ColorTranslator.ToHtml(textPlotColor9.BackColor),
                ColorTen = ColorTranslator.ToHtml(textPlotColor10.BackColor),
                ColorEleven = ColorTranslator.ToHtml(textPlotColor11.BackColor),
                ColorTwelve = ColorTranslator.ToHtml(textPlotColor12.BackColor),
                ColorThirteen = ColorTranslator.ToHtml(textPlotColor13.BackColor),
                ColorFourteen = ColorTranslator.ToHtml(textPlotColor14.BackColor),



                BackgroundColor = ColorTranslator.ToHtml(textBackgroundColor.BackColor),
               // BladderSensation = ColorTranslator.ToHtml(textBladderSensation.BackColor.ToArgb),
               // GeneralPurose = ColorTranslator.ToHtml(textGeneralPurpose.BackColor),
               // ResponeMarkers = ColorTranslator.ToHtml(textResponseMarkers.BackColor),
                BladderSensation = textBladderSensation.BackColor.ToArgb(),
                GeneralPurose = textGeneralPurpose.BackColor.ToArgb(),
                ResponeMarkers = textResponseMarkers.BackColor.ToArgb(),

                NumberOfScreen = comboNumberOfMinScreen.Text

            };

            // Serialize to JSON
            string jsonData = System.Text.Json.JsonSerializer.Serialize(data);

            // Encrypt
            byte[] encryptedData = CryptoHelper.Encrypt(jsonData);

            string filePath = GetScaleAndColorSetupFilePath("ScaleColorSetupFile");
            File.WriteAllBytes(filePath, encryptedData);

            //MessageBox.Show(isEditMode ? "Scale And Color Setup data updated successfully." : "Scale And Color Setup data saved successfully.");

            //Show Success Massage

            //var notification = new NotificationForm(
            //    isEditMode
            //        ? "Scale And Color Setup data updated successfully."
            //        : "Scale And Color Setup data saved successfully.",
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

            DataSaved?.Invoke(this, EventArgs.Empty);

            this.Close();

        }


        private void LoadScaleColorData(string channelZero)
        {
            string filePath = GetScaleAndColorSetupFilePath(channelZero);
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    var record = System.Text.Json.JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);

                    if (record != null)
                    {
                        isEditMode = true;

                        textChannel0.Text = record.ChannelZero;
                        txtChannel1.Text = record.ChannelOne;
                        textChannel2.Text = record.ChannelTwo;
                        textChannel3.Text = record.ChannelThree;
                        textChannel4.Text = record.ChannelFour;
                        textChannel5.Text = record.ChannelFive;
                        textChannel6.Text = record.ChannelSix;
                        textChannel7.Text = record.ChannelSeven;
                        textChannel8.Text = record.ChannelEight;
                        textChannel9.Text = record.ChannelNine;
                        textChannel10.Text = record.ChannelTen;
                        textChannel11.Text = record.ChannelEleven;
                        textChannel12.Text = record.ChannelTwelve;
                        textChannel13.Text = record.ChannelThirteen;
                        textChannel14.Text = record.ChannelFourteen;


                        comboPlotScale0.Text = record.PlotScaleZero;
                        comboPlotScale1.Text = record.PlotScaleOne;
                        comboPlotScale2.Text = record.PlotScaleTwo;
                        comboPlotScale3.Text = record.PlotScaleThree;
                        comboPlotScale4.Text = record.PlotScaleFour;
                        comboPlotscale5.Text = record.PlotScaleFive;
                        comPlotScale6.Text = record.PlotScaleSix;
                        comPlotScale7.Text = record.PlotScaleSeven;
                        comPlotScale8.Text = record.PlotScaleEight;
                        comPlotScale9.Text = record.PlotScaleNine;
                        comPlotScale10.Text = record.PlotScaleTen;
                        comPlotScale11.Text = record.PlotScaleEleven;
                        comPlotScale12.Text = record.PlotScaleTwelve;
                        comPlotScale13.Text = record.PlotScaleThirteen;
                        comPlotScale14.Text = record.PlotScaleFourteen;

                        textPlotColor0.BackColor = ColorTranslator.FromHtml(record.ColorZero);
                        textPlotColor1.BackColor = ColorTranslator.FromHtml(record.ColorOne);
                        textPlotColor2.BackColor = ColorTranslator.FromHtml(record.ColorTwo);
                        textPlotColor3.BackColor = ColorTranslator.FromHtml(record.ColorThree);
                        textPlotColor4.BackColor = ColorTranslator.FromHtml(record.ColorFour);
                        textPlotColor5.BackColor = ColorTranslator.FromHtml(record.ColorFive);
                        textPlotColor6.BackColor = ColorTranslator.FromHtml(record.ColorSix);
                        textPlotColor7.BackColor = ColorTranslator.FromHtml(record.ColorSeven);
                        textPlotColor8.BackColor = ColorTranslator.FromHtml(record.ColorEight);
                        textPlotColor9.BackColor = ColorTranslator.FromHtml(record.ColorNine);
                        textPlotColor10.BackColor = ColorTranslator.FromHtml(record.ColorTen);
                        textPlotColor11.BackColor = ColorTranslator.FromHtml(record.ColorEleven);
                        textPlotColor12.BackColor = ColorTranslator.FromHtml(record.ColorTwelve);
                        textPlotColor13.BackColor = ColorTranslator.FromHtml(record.ColorThirteen);
                        textPlotColor14.BackColor = ColorTranslator.FromHtml(record.ColorFourteen);

                        textBackgroundColor.BackColor = ColorTranslator.FromHtml(record.BackgroundColor);
                        //textBladderSensation.BackColor = Color.FromArgb(record.BladderSensation);
                        //textGeneralPurpose.BackColor = Color.FromArgb(record.GeneralPurose);
                        //textResponseMarkers.BackColor = Color.FromArgb(record.ResponeMarkers);

                        textBladderSensation.BackColor = record.BladderSensation.HasValue
                        ? Color.FromArgb(record.BladderSensation.Value)
                        : Color.Magenta; // or some default color

                        textGeneralPurpose.BackColor = record.GeneralPurose.HasValue
                            ? Color.FromArgb(record.GeneralPurose.Value)
                            : Color.Magenta; // or some default color

                        textResponseMarkers.BackColor = record.ResponeMarkers.HasValue
                            ? Color.FromArgb(record.ResponeMarkers.Value)
                            : Color.Magenta; // or some default color


                        //textBladderSensation.BackColor = string.IsNullOrWhiteSpace(record.ColorZero) ? Color.Magenta : ColorTranslator.FromHtml(record.BladderSensation);

                        comboNumberOfMinScreen.Text = record.NumberOfScreen;

                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("Error loading Scale And Color Setup: " + ex.Message);
                }
            }
            else
            {
                isEditMode = false;
                //ClearFieldsExceptPatientNo();
            }
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textPlotColor0_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void comboPlotScale0_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textPlotColor3_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
