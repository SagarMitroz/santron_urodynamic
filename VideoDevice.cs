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

namespace SantronWinApp
{
    public partial class VideoDevice : Form
    {
        public VideoDevice()
        {
            InitializeComponent();
        }

        private void existButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string GetPatientFilePath(string videoDevice)
        {
            string exeFolder = Application.StartupPath;

            //string folder = Path.Combine(exeFolder, "Saved Data", "VideoDevice");
            string folder = AppPathManager.GetFolderPath("VideoDevice");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, videoDevice + ".dat");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbSelectDevice.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a device first!");
                return;
            }

            string selectedDevice = cmbSelectDevice.SelectedItem.ToString();
            string filePath = GetPatientFilePath("SelectedDevice");

            File.WriteAllText(filePath, selectedDevice);

            //MessageBox.Show("Video Device Saved Successfully!");

            this.Close();
        }

        private void cmbSelectDevice_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void VideoDevice_Load(object sender, EventArgs e)
        {
            LoadSavedVideoDevice();
        }

        private void LoadSavedVideoDevice()
        {
            string filePath = GetPatientFilePath("SelectedDevice");

            if (!File.Exists(filePath))
                return;

            string savedDevice = File.ReadAllText(filePath);

            if (cmbSelectDevice.Items.Contains(savedDevice))
            {
                cmbSelectDevice.SelectedItem = savedDevice;
            }
        }

    }
}
