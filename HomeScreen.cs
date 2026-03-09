using SantronWinApp.IO;
using SantronWinApp.Processing;
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
using static SantronWinApp.SystemSetup;

namespace SantronWinApp
{
    public partial class HomeScreen : Form
    {
        //private const int InfusionRateMin = 5;
        //private const int InfusionRateMax = 100;

        //private int _infusionRate;

        //private TestOrchestrator _orch;
        //private SystemSetupModel _SystemSetup;

        //private int ClampInfusionRate(int value)
        //{
        //    if (value < InfusionRateMin) return InfusionRateMin;
        //    if (value > InfusionRateMax) return InfusionRateMax;
        //    return value;
        //}

        //private void ApplyInfusionRateToPump()
        //{
        //    if (_orch?.Pump == null) return;
        //    _orch.Pump.SetRate(_infusionRate);
        //}


        public HomeScreen()
        {
            InitializeComponent();
        }



        private void HomeScreen_Load(object sender, EventArgs e)
        {
            //_lblInfusion = this.lblInfusion;
            //LoadGraph();
            //UpdateInfusionLabel();
        }

       

        //private void HomeScreen_Load(object sender, EventArgs e)
        //{
        //    int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        //    int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        //    bool isLowResolution =
        //        (screenWidth == 1024 && screenHeight == 768) ||
        //        (screenWidth == 1280 && screenHeight == 600);

        //    if (isLowResolution)
        //    {
        //        // ✅ Hide only these buttons
        //        button20.Visible = false;
        //        button21.Visible = false;
        //        button22.Visible = false;
        //        button23.Visible = false;
        //        button24.Visible = false;
        //        button25.Visible = false;
        //        button26.Visible = false;
        //        button27.Visible = false;
        //        button28.Visible = false;
        //    }
        //    else
        //    {
        //        // ✅ Show again on bigger screens
        //        button20.Visible = true;
        //        button21.Visible = true;
        //        button22.Visible = true;
        //        button23.Visible = true;
        //        button24.Visible = true;
        //        button25.Visible = true;
        //        button26.Visible = true;
        //        button27.Visible = true;
        //        button28.Visible = true;
        //    }
        //}


        public void HomeDesignPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void BtnAddNew_Click(object sender, EventArgs e)
        {
            var patientWithTestForm = new PatientWithTestForm();
            ScreenDimOverlay.ShowDialogWithDim(patientWithTestForm, alpha: 150);
        }

        private void BtnList_Click(object sender, EventArgs e)
        {
            var patientList = new PatientList();
            ScreenDimOverlay.ShowDialogWithDim(patientList, alpha: 150);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
           
        }

        private void BtnPatinetDatabase_Click(object sender, EventArgs e)
        {
            var patientDatabase = new PatientHistory();
            ScreenDimOverlay.ShowDialogWithDim(patientDatabase, alpha: 150);
        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {

        }


        //private void LoadSystemSetup()
        //{
        //    try
        //    {
        //        string folder = Path.Combine(Application.StartupPath, "Saved Data", "SystemSetup");

        //        if (!Directory.Exists(folder))
        //            return;

        //        string[] files = Directory.GetFiles(folder, "*.dat");

        //        if (files.Length == 0)
        //            return;

        //        string filePath = files[0];

        //        byte[] encrypted = File.ReadAllBytes(filePath);
        //        string json = CryptoHelper.Decrypt(encrypted);

        //        _SystemSetup = JsonSerializer.Deserialize<SystemSetupModel>(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Load error: " + ex.Message);
        //    }
        //}

        //private void LoadGraph()
        //{
        //    LoadSystemSetup();

        //    int pumpConst1 = 1500;
        //    int pumpConst2 = 2000;
        //    int defaultInfusion = 20;

        //    if (_SystemSetup != null)
        //    {
        //        pumpConst1 = Convert.ToInt32(_SystemSetup.Constant1);
        //        pumpConst2 = Convert.ToInt32(_SystemSetup.Constant2);
        //        defaultInfusion = Convert.ToInt32(_SystemSetup.DefualInfusion);
        //    }

        //    _infusionRate = ClampInfusionRate(defaultInfusion);

        //    var daq = new DaqService();
        //    var cal = new Calibration(_profile);
        //    var proc = new SignalProcessor(cal, 1000.0, 25.0);

        //    IPumpController pump = new PumpController(pumpConst1, pumpConst2);

        //    _orch = new TestOrchestrator(daq, proc, pump, new SysSetupStore());
        //}

        //private Label _lblInfusion;
        //private CalibrationProfile _profile;

        //private void UpdateInfusionLabel()
        //{
        //    if (_lblInfusion == null) return;

        //    _lblInfusion.Text = _orch?.Pump?.IsRunning == true
        //        ? $"Rate: {_infusionRate} ml/min"
        //        : "Rate: 0 ml/min";
        //}



        //private void PumpStart()
        //{
        //    if (_orch?.Pump == null) return;

        //    _infusionRate = ClampInfusionRate(_infusionRate);
        //    _orch.Pump.StartInfusion(_infusionRate);

        //    UpdateInfusionLabel();
        //}

        //private void PumpSpeedUp()
        //{
        //    if (_orch?.Pump == null) return;

        //    _infusionRate = ClampInfusionRate(_infusionRate + 1);
        //    ApplyInfusionRateToPump();

        //    UpdateInfusionLabel();
        //}

        //private void PumpSpeedDown()
        //{
        //    if (_orch?.Pump == null) return;

        //    _infusionRate = ClampInfusionRate(_infusionRate - 1);
        //    ApplyInfusionRateToPump();

        //    UpdateInfusionLabel();
        //}

        //private void PumpStop()
        //{
        //    if (_orch?.Pump == null) return;

        //    _orch.Pump.StopInfusion();

        //    if (lblInfusion != null)
        //        _lblInfusion.Text = "Rate: 0 ml/min";
        //}


        private void btnPumpStart_Click(object sender, EventArgs e)
        {
            //PumpStart();
        }

        private void btnSpeedUp_Click(object sender, EventArgs e)
        {
            //PumpSpeedUp();
        }

        private void btnSpeedDown_Click(object sender, EventArgs e)
        {
            //PumpSpeedDown();
        }

        private void btnPumpStop_Click(object sender, EventArgs e)
        {
            //PumpStop();
        }

        private void btnArmStart_Click(object sender, EventArgs e)
        {

        }
    }
}
