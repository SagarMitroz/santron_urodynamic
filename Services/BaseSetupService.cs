using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using SantronWinApp.Helper;

namespace SantronWinApp.Services
{
    public abstract class BaseSetupService<T> where T : class
    {
        protected abstract string FolderName { get; }
        protected abstract string DefaultSetupName { get; }

        protected virtual string FixJson(string json)
        {
            return json;
        }

        protected abstract T GetDefaultSetup();

        private string GetFilePath(string setupName)
        {
            string folder = AppPathManager.GetFolderPath(FolderName);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, setupName + ".dat");
        }

        public T Load(string setupName = null)
        {
            try
            {
                setupName = DefaultSetupName;

                string filePath = GetFilePath(setupName);

                if (!File.Exists(filePath))
                    return GetDefaultSetup();

                byte[] encrypted = File.ReadAllBytes(filePath);

                string json = CryptoHelper.Decrypt(encrypted);

                json = FixJson(json);

                var model = JsonSerializer.Deserialize<T>(json);

                return model ?? GetDefaultSetup();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Configuration load error\nUsing default configuration.\n\n" + ex.Message,
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return GetDefaultSetup();
            }
        }

        public void Save(T model, string setupName = null)
        {
            try
            {
                setupName = DefaultSetupName;

                string filePath = GetFilePath(setupName);

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
                    "Configuration save error\n\n" + ex.Message,
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}