using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SantronWinApp.Services
{
    public static class ScaleAndColorService
    {
        private const string DefaultSetupName = "DefaultSetup";

        public static ScaleAndColorModel Load(string setupName = DefaultSetupName)
        {
            try
            {
                string filePath = GetSetupFilePath(setupName);

                if (!File.Exists(filePath))
                    return GetDefaultSetup();

                byte[] encryptedData = File.ReadAllBytes(filePath);

                string jsonData = CryptoHelper.Decrypt(encryptedData);

                jsonData = FixJsonForIntFields(jsonData);

                var model = JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);

                return model ?? GetDefaultSetup();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error loading Scale & Color setup.\nUsing default configuration.\n\n" + ex.Message,
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return GetDefaultSetup();
            }
        }

        public static void Save(ScaleAndColorModel model, string setupName = DefaultSetupName)
        {
            try
            {
                string filePath = GetSetupFilePath(setupName);

                string json = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                byte[] encrypted = CryptoHelper.Encrypt(json);

                File.WriteAllBytes(filePath, encrypted);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error saving Scale & Color setup.\n\n" + ex.Message,
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string GetSetupFilePath(string setupName)
        {
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, setupName + ".dat");
        }

        private static string FixJsonForIntFields(string json)
        {
            // keep your existing logic here
            return json;
        }

        private static ScaleAndColorModel GetDefaultSetup()
        {
            return new ScaleAndColorModel
            {
                ChannelZero = "Pves",
                ChannelOne = "Pabd",
                ChannelTwo = "Pdet",
                ChannelThree = "Pura",
                ChannelFour = "Pclo",
                ChannelFive = "Vinf",
                ChannelSix = "Qvol",
                ChannelSeven = "Qura",
                ChannelEight = "EMG",
                ChannelNine = "Prpg",
                ChannelTen = "Pirp",
                ChannelEleven = "Lura",
                ChannelTwelve = "Pa1",
                ChannelThirteen = "Pa2",
                ChannelFourteen = "Pa3",

                PlotScaleZero = "100",
                PlotScaleOne = "100",
                PlotScaleTwo = "100",
                PlotScaleThree = "100",
                PlotScaleFour = "100",
                PlotScaleFive = "1000",
                PlotScaleSix = "500",
                PlotScaleSeven = "50",
                PlotScaleEight = "2000",
                PlotScaleNine = "100",
                PlotScaleTen = "100",
                PlotScaleEleven = "30cms",
                PlotScaleTwelve = "100",
                PlotScaleThirteen = "100",
                PlotScaleFourteen = "100",

                ColorZero = "#0000FF",
                ColorOne = "#FF0000",
                ColorTwo = "#000000",
                ColorThree = "#FFD700",
                ColorFour = "#A9A9A9",
                ColorFive = "#800080",
                ColorSix = "#FFA500",
                ColorSeven = "#ADD8E6",
                ColorEight = "#008000",
                ColorNine = "#000000",
                ColorTen = "#FF0000",
                ColorEleven = "#800080",
                ColorTwelve = "#0000FF",
                ColorThirteen = "#FF0000",
                ColorFourteen = "#FFD700",

                BackgroundColor = "#FFFFFF",
                BladderSensation = 500000,
                GeneralPurose = 500000,
                ResponeMarkers = 500000,
                NumberOfScreen = "2"
            };
        }
    }
}
