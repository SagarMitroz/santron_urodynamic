using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AForge.Video;
using AForge.Video.DirectShow;
using CloudinaryDotNet;
using NationalInstruments;
using NationalInstruments.DAQmx;
using SantronChart;
using SantronReports;
using SantronWinApp.IO;
using SantronWinApp.Processing;
using static MarkerService;
using static SantronReports.LegacyUroReportPrinter;
using static SantronWinApp.DocterList;
using static SantronWinApp.Doctor;
using static SantronWinApp.EmgLiteForm;
using static SantronWinApp.HospitalAndDoctorInfoSetUp;
using static SantronWinApp.PatientWithTestForm;
using static SantronWinApp.ReportComments;
using static SantronWinApp.SystemSetup;
using Task = System.Threading.Tasks.Task;

namespace SantronWinApp
{

    public partial class MainForm : Form
    {

        //======= EMG ============
        private EmgBleSource _emgSrc;

        private Queue<double> _emgHistory = new Queue<double>();
        private const int EMG_SMOOTH_SAMPLES = 3;
        private EmgLiteEngine _emgEngine;
        private double _lastEmgUv = 0;   // last EMG plotted value (RMS uV)
        private volatile bool _emgStreaming = false;
        private Action<double> _emgPointHandler;
        private bool _emgEnabled = false;
        private readonly Queue<double> _emgCircularBuffer = new Queue<double>();
        private const int EMG_BUFFER_SIZE = 1000; // Store 2 seconds at 500Hz
        private double _lastProcessedEmgTime = 0;
        private const double EMG_SAMPLE_INTERVAL = 0.002; // 500Hz = 0.002 seconds
        private NotchFilter50Hz _emgNotchFilter;
        private readonly Queue<double> _emgRawBuffer = new Queue<double>();
        private double _filteredEmgValue = 0;

        // BLE EMG Source Toggle
        private const bool _useBleEmg = true; // Set to true to use Bluetooth EMG instead of DAQ
        //======= EMG ============

        private string _currentMainId;
        private string _currentPatientNo;
        private string _currentTestName;


        private double _timeOffsetAfterPause = 0;

        // ---------- AUTO STOP (NO FLOW FOR 30 SEC) ----------
        private bool _monitorNoFlow = false;
        private DateTime _lastMovementTime = DateTime.MinValue;
        private System.Windows.Forms.Timer _noFlowTimer;
        private const int NO_FLOW_TIMEOUT_SECONDS = 30;
        private double _lastVolumeValue = 0;
        private const double FLOW_THRESHOLD = 0.5; // ignore tiny noise



        private const int InfusionRateMin = 5;
        private const int InfusionRateMax = 100;
        private int _infusionRate = 20;

        private SignalProcessor _signalProcessor;

        private ArmController _arm;
        private int ClampInfusionRate(int value)
        {
            if (value < InfusionRateMin) return InfusionRateMin;
            if (value > InfusionRateMax) return InfusionRateMax;
            return value;
        }

        private void ApplyInfusionRateToPump()
        {
            if (_orch?.Pump == null) return;
            _orch.Pump.SetRate((int)_infusionRate);
        }

        //private PumpController _pump;
        //private int _currentSpeed = 0;   // 0–100



        private int _currentPage = 1;
        private LegacyUroReportPrinter _printer;
        private PrintDocument _doc;

        private System.Windows.Forms.ComboBox comboDevices;



        //  private enum TestKind { Default, Uroflowmetry, UroflowmetryEMG, Cystometry, CystoUroflowEMG, UPP }
        private ComboBox _cbTest;
        private int[] _activeIndices = Array.Empty<int>();   // which columns to plot in current view
                                                             // column layout in your pipeline/files:
                                                             //private const int PVES = 0, PABD = 1, PDET = 2, VINF = 3, QVOL = 4, FLOW_UPP = 5, EMG = 6;




        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        OverlayForm _overlay;



        private MenuStrip menuStrip1;
        private ToolStrip toolStrip1;
        private Panel mainContentPanel;
        private Panel mainContentPanelGraphOnly;
        private Panel mainContentPanelPatient;
        private PictureBox graphPictureBox;
        private Panel rightSidebarPanel;
        private Panel rightSidebarPanelForPatient;

        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private Timer updateTimer;
        private Random rand = new Random();

        private Chart liveChart;
        private Button buttonGoLive;
        private readonly ChannelSettings[] channelSettings;



        // --- Layout roots ---
        private MenuStrip _menu;
        private Panel _root;
        private Panel _right;

        // ========== UI ==========


        private TestChannelManager _testMgr;
        private TestOrchestrator _orch;
        private CalibrationProfile _profile;
        private Panel panelGraph;
        private MultiChannelLiveChart _liveChart;

        // private double _lastPlotT;
        private double _lastPlotT = double.NegativeInfinity;

        private int _calibWarmup = 10;
        private int _screenMinutes;
        private int _warmupCounter = 0;


        private bool _orchSubscribed;
        private bool _daqSubscribed;

        private readonly object _rawLock = new object();
        private double[] _lastRawCounts;


        // Recording state/timer
        private bool _isRecording = false;
        private bool _isPaused = false;
        //private List<(double T, double[] Values)> _recorded = new List<(double, double[])>();

        private Timer _testTimer;
        private DateTime _testStartTime;
        private TimeSpan _pausedTime = TimeSpan.Zero;
        private DateTime? _pauseStartedAt = null;


        private double _lastPlottedT = 0.0;     // last absolute time we plotted (seconds)
        private double _resumeOffset = 0.0;     // added to incoming frame.T after reconnect
        private bool _resumeArmed = false;   // set true on disconnect, applied on first new frame

        // ---- VINF continuation across bottle-change pause ----
        private double _vinfResumeOffset = 0.0;  // engineering-unit offset added to VINF after resume
        private double _lastVinfEngValue = 0.0;   // last displayed VINF eng value before pause
        private long _frameCounter = 0;
        private double _plotRateHz;
        // Heartbeat from OnDisplayFrame
        private DateTime _lastFrameAt = DateTime.MinValue;
        private DateTime _orchStartedAt = DateTime.MinValue;

        private double _firstTOut = double.NaN;

        //  private double[] _lastRawCounts = null;

        // Reconnect re-entrancy guard
        private bool _isReconnecting = false;

        // Device status
        private ToolStripLabel _deviceStatusPill;
        private System.Windows.Forms.Timer _devicePollTimer;
        private enum DeviceState { Disconnected, Connected }
        private DeviceState _deviceState = DeviceState.Disconnected;

        // Plot / DAQ
        private bool _wasAcquiringBeforeDisconnect = false;

        // Probe control
        private bool _probeBusy = false;
        private DateTime _lastHwSeenAt = DateTime.MinValue;

        // Adjust to your device. If you use full 7 channels use "Dev1/ai0:6"; for quick probe use single chan.
        private const string AI_CONFIG = "Dev1/ai0:6"; // lightweight probe to avoid heavy start



        // --- UI decoupling to prevent 1-2 sec lag ---
        private System.Windows.Forms.Timer _uiSlowWorkTimer;
        private volatile double[] _uiLastVals;
        private double _uiLastT;
        private int _uiWorkPending; // 0/1 gate
        private double _wallStartSec;
        private bool _wallStartSet;
        private DateTime _firstFrameUtc = DateTime.MinValue;


        // fields (top of MainForm)
        private DeviceWatcher _watcher;


        PatientRecord currentTest;
        private string currenttest;

        private DateTime _uiStartUtc = DateTime.MinValue;
        private double _uiFirstT = double.NaN;
        //-------- 06-10-2025 save and open test
        // Playback / recording control
        private bool _isPlaybackMode = false;
        private bool _acceptLiveFrames = true;   // gate live frames when in playback
                                                 // private bool _isRecording = false;

        private double _resumeShiftSeconds = 0.0;
        private double _timeBaseShiftSeconds = 0.0;
        private bool _timeBaseArmed = false;

        // Replace tuples with a simple class
        public class SampleRecord
        {
            public double T;
            public double[] Values;

            public SampleRecord(double t, double[] values)
            {
                T = t;
                Values = values;
            }

            public double TimeSec { get; internal set; }
        }
        private readonly List<SampleRecord> _recorded = new List<SampleRecord>();
        private TestChannelManager.TestDefinition _currentTestDef = null;

        // Manage event subscription cleanly
        // private bool _orchSubscribed = false;

        private PatientRecord _currentPatient = null;  // who is selected now



        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEUP = 2;
        private const int SB_LINEDOWN = 3;
        private const int WM_HSCROLL = 0x0114;

        private const int SB_TOP = 6;
        private const int SB_LEFT = 6;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);


        // --- System-check thresholds (demo defaults; load from SysSetup if you have it) ---
        //private int P0 = 120;   // pressure low threshold for Pves
        //private int P1 = 120;   // pressure low threshold for Pabd/Pirp
        //private int P6 = 120;   // pressure low threshold for Pura
        //private int JF = 3500;  // uroflow Jar Full threshold
        //private int ChangeBottle = 600; // infusion bottle missing/change threshold
        //private int U1 = 100;   // UPP not connected
        //private int UH = 1800;  // UPP arm pulled high threshold

        //// Fixed bounds from the spec (counts domain)
        //private const int RAW_MIN_SMALL = 10;
        //private const int RAW_MIN_20 = 20;
        //private const int RAW_MIN_25 = 25;
        //private const int RAW_PRESSURE_FAULT = 2050;
        //private const int RAW_FAULT_3900 = 3900;
        //private const int RAW_EMG_FAULT = 16000;

        // Thresholds per Error Document (verify system-setup may override)
        private const double RAW_MIN_20 = 20.0;
        private const double RAW_MIN_SMALL = 10.0;   // doc: "greater than 10"
        private const double RAW_MIN_25 = 25.0;
        private const double RAW_PRESSURE_FAULT = 2050.0; // doc: >2050 => faulty
        private const double RAW_FAULT_3900 = 3900.0;
        private const double RAW_EMG_FAULT = 16000.0; // doc: >16000 => EMG faulty

        // Other system-setup variables — make sure these are populated from settings
        //private double P0 = 50.0; 
        //private double P1 = 50.0;
        //private double P6 = 50.0;
        //private double JF = 3000.0;         
        //private double ChangeBottle = 500.0; 
        //private double U1 = 10.0;          
        //private double UH = 2000.0;         

        private double P0 = 50.0;
        private double P1;
        private double P6 = 50.0;
        private double JF;
        private double ChangeBottle;
        private double U1;
        private double UH;

        //Default Value for id data not save so that time use static value change on 20/12/2025
        private const double DEFAULT_P1 = 75;
        private const double DEFAULT_CHANGE_BOTTLE = 1200;
        private const double DEFAULT_JF = 2700;
        private const double DEFAULT_U1 = 0;
        private const double DEFAULT_UH = 1800;

        // ENGINEERING-UNIT thresholds (tune as you like)
        private const double PRESSURE_CONN_CM = 0.5;   // |P| < 0.5 cmH2O → likely not connected
        private const double PRESSURE_FLUSH_CM = 3.0;  // 0.5..3 cmH2O → not flushed / extension not parked
        private const double PRESSURE_MAX_CM = 300.0;  // beyond scale → faulty

        private const double QVOL_JAR_FULL_ML = 1000.0; // your jar capacity; advisory/error as you prefer
                                                        // VINF is infused volume (ml). Sitting at ~0 before pump starts is NORMAL; do not treat as error.

        // Keep latest full frame values for checks (not only active lanes)
        private double[] _lastFrameRaw = null;
        private bool _haltPlottingDueToAlert = false;  // true => skip AppendSample and recording


        //This Code Start for Graph Test DropDown Valuse show
        //Refe for Line No.1328 Method **private void BuildScaleMaxOverlays()** and 
        //Second Refe Line No.2487   Method **private void ConfigureChartLanes(ScaleAndColorModel record)**
        private static readonly Dictionary<string, double[]> _scaleOptions = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Pressures (cm H2O)
            { "Pves", new double[] { 50, 100, 200 } },
            { "Pabd", new double[] { 50, 100, 200 } },
            { "Pirp", new double[] { 50, 100, 200 } },
            { "Pura", new double[] { 50, 100, 200 } },

            // Calculated pressures (cm H2O)
            { "Pdet", new double[] { 50, 100, 200 } },
            { "Pclo", new double[] { 50, 100, 200 } },
            { "Prpg", new double[] { 50, 100, 200 } },

            // Flow rate (ml/sec)
            { "Qrate", new double[] { 25, 50 } },

            // Volumes (ml)
            { "Qvol", new double[] { 500, 1000 } },
            { "Vinf", new double[] { 500, 1000 } },

            { "Qura", new double[] { 25, 50 } },

            // EMG (PDF shows 0–5000)
           // { "EMG", new double[] { 5000, 1000, 2000 } }
            { "EMG", _useBleEmg ? new double[] { 2000, 500, 1000 } : new double[] { 5000, 1000, 2000 } }
        };

        // Simple unit map for nicer labels in the dropdown
        private static readonly Dictionary<string, string> _unitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Pves", "cm H2O" }, { "Pabd", "cm H2O" }, { "Pirp", "cm H2O" }, { "Pura", "cm H2O" },
            { "Pdet", "cm H2O" }, { "Pclo", "cm H2O" }, { "Prpg", "cm H2O" },
            { "Qrate", "ml/sec" }, { "Qvol", "ml" }, { "Vinf", "ml" },
            { "Qura", "ml/sec" },
            { "EMG", "uV" }
        };
        private readonly Dictionary<string, ComboBox> _scaleCombos = new Dictionary<string, ComboBox>(StringComparer.OrdinalIgnoreCase);

        // Advisory when signal hugs scale max: threshold & hold-time
        private const double SCALE_ADVISORY_FRAC = 0.95;   // 95% of scale
        private const double SCALE_ADVISORY_HOLD_S = 1.0;  // sustain for >= 2s
        private const int V = 1;

        // Track when each lane first exceeded the threshold (key: lane name)
        private readonly Dictionary<string, double> _advisoryStartT =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // overlay width controls
        private int _scaleComboMinWidth = 30;   // minimum visible width
        private int _scaleComboExtraWidth = 28;  // add on top of chart's right-band width

        private int BladderColor;
        private int GPColor;
        private int ResponseColor;


        //public MainForm()
        //{
        //    InitializeComponent();
        //    Constants();

        //    //EnsureGraphReady();
        //    var setup = LoadScaleAndColorModel("DefaultSetup");
        //    _testMgr = new TestChannelManager(setup);
        //    channelSettings = LoadChannelSettings();

        //    this.WindowState = FormWindowState.Maximized;
        //    this.Text = "Santron PC based Urodynamics USB, India Visit us at www.santronmeditronic.com, email:santronmeditronic@gmail.com";


        //    this.AutoScaleMode = AutoScaleMode.Dpi;
        //    this.AutoSize = false;
        //    this.MinimumSize = new Size(900, 600);
        //}


        private IPumpController _pump;
        public MainForm()
        {
            InitializeComponent();
            Constants();
            _emgNotchFilter = new NotchFilter50Hz();
            SetupBatteryIndication();
            //_pump = new PumpController("Dev1/port0/line0", "Dev1/ao0");
            _arm = new ArmController("Dev1/port0");

            _pump = new PumpController(1500, 2000);
            _infusionRate = 20;

            _lastPopupState = _deviceState;

            // Disable popups until the form is actually shown
            this.Shown += MainForm_Shown;

            label36.Visible = false;

            comboDevices.Visible = false;
            LoadVideoDevices();

            // Ensure setup is never null
            ScaleAndColorModel setup = null;

            try
            {
                setup = LoadScaleAndColorModel("DefaultSetup");

                // Optional: log what was loaded
                if (setup == null)
                    Console.WriteLine("⚠️ Setup file not found or invalid. Using default setup.");
            }
            catch (Exception ex)
            {
                // You can also log to a file here if you want
                MessageBox.Show("Error loading setup file: " + ex.Message + "\nUsing default setup.");
            }

            // ✅ Fallback if LoadScaleAndColorModel returned null
            if (setup == null)
            {
                setup = new ScaleAndColorModel
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
                    //ChannelEight = "Ch8",
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
                    //PlotScaleEight = "1",

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
                    //ColorEight = "#008080",

                    BackgroundColor = "#FFFFFF",
                    BladderSensation = 500000,
                    GeneralPurose = 500000,
                    ResponeMarkers = 500000,
                    NumberOfScreen = "2"
                };
            }


            _uiSlowWorkTimer = new System.Windows.Forms.Timer();
            _uiSlowWorkTimer.Interval = 200; // 5 Hz
            _uiSlowWorkTimer.Tick += (s, e) =>
            {
                // Only run if we have a fresh frame pending
                if (System.Threading.Interlocked.Exchange(ref _uiWorkPending, 0) == 0)
                    return;

                var vals = _uiLastVals;
                var t = _uiLastT;

                if (vals == null) return;

                // These were making UI lag when called from OnDisplayFrame
                RunLiveSystemChecks_RAW(_lastRawCounts);
                UpdateScaleAdvisories(vals, t);
            };
            _uiSlowWorkTimer.Start();


            // ✅ Now safe — setup is guaranteed not null
            _testMgr = new TestChannelManager(setup);
            channelSettings = LoadChannelSettings();

            // Window setup
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Santron PC based Urodynamics USB, India Visit us at www.santronmeditronic.com, email:santronmeditronic@gmail.com";

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoSize = false;
            this.MinimumSize = new Size(900, 600);
        }

        private string _selectedPatientNo;
        private string _selectedPatientId;

        //Start This Code For PatientForm To Open Test on 08/10/2025
        public void TriggerStartTest(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();
            // simulate clicking the StartTest button
            //btnStartTest_Click(this, EventArgs.Empty);
            btnReviewModeCameraTest_Click(this, EventArgs.Empty);

            // optionally open the test record view
            OpenTestViewForRecord(record); //OpenLiveTestOnly
        }

        public void TriggerStartTestHistory(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();
            // simulate clicking the StartTest button
            //btnStartTest_Click(this, EventArgs.Empty);
            btnReviewModeCameraTest_Click(this, EventArgs.Empty);

            // optionally open the test record view
            OpenTestViewForRecord(record); //OpenLiveTestOnly
        }
        public void TriggerGraphOnlyHistory(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();
            // simulate clicking the GraphOnly button
            btnGraphOnly_Click(this, EventArgs.Empty);

            // optionally open the test record view
            OpenTestViewForRecord(record);
        }

        public void TriggerGraphOnly(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();
            // simulate clicking the GraphOnly button
            btnGraphOnly_Click(this, EventArgs.Empty);

            // optionally open the test record view
            OpenTestViewForRecord(record);
        }

        //This Code Save Patient And Open Live Test
        public void TriggerStartTestLive(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();

            currenttest = record.Test;

            // simulate clicking the StartTest button
            btnStartTest_Click(this, EventArgs.Empty);

            EnterDemoMode();

            // optionally open the test record view
            OpenLiveTestOnly(record); //
        }

        public void TriggerGraphOnlyLive(PatientRecord record)
        {
            _currentMainId = record.Id.ToString();
            _currentPatientNo = record.PatientNo;
            _currentTestName = record.Test;

            _selectedPatientNo = record.PatientNo;
            _selectedPatientId = record.Id.ToString();

            currenttest = record.Test;

            btnGraphOnly_Click(this, EventArgs.Empty);

            EnterDemoMode();

            OpenLiveTestOnly(record);
        }


        //End This Code For PatientForm To Open Test on 08/10/2025



        private void LoadHomeScreenPanel()
        {
            // Create object of HomeScreen
            HomeScreen home = new HomeScreen();

            // Run initialization so panel exists
            home.TopLevel = false;
            home.FormBorderStyle = FormBorderStyle.None;
            home.Show();

            // Take the HomeDesignPanel from HomeScreen
            Panel homePanel = home.HomeDesignPanel;

            // Remove panel from HomeScreen
            homePanel.Parent = null;

            // Fill in MainForm panel
            homePanel.Dock = DockStyle.Fill;

            // Add panel to MainForm
            mainContainerPanel.Controls.Clear();
            mainContainerPanel.Controls.Add(homePanel);
        }

        //private void LoadHomeDesignDirect()
        //{
        //    // Load HomeScreen
        //    HomeScreen home = new HomeScreen();

        //    // Initialize the form so panel is created
        //    home.TopLevel = false;
        //    home.FormBorderStyle = FormBorderStyle.None;
        //    home.Show();

        //    // Take the HomeDesignPanel
        //    Panel panel = home.HomeDesignPanel;

        //    // Detach from HomeScreen
        //    panel.Parent = null;

        //    // Dock to entire MainForm
        //    panel.Dock = DockStyle.Fill;

        //    // Add directly to MainForm
        //    this.Controls.Clear();
        //    this.Controls.Add(panel);
        //}


        private Panel patientCardsPanel;
        private ToolTip toolTip1 = new ToolTip();
        private void MainForm_Load(object sender, EventArgs e)
        {
            //Start code for AUTO-STOP Measn Auto Savedtest only for UROFLOW tests 12/12/2025
            _noFlowTimer = new System.Windows.Forms.Timer();
            _noFlowTimer.Interval = 1000;   // check every 1 sec
            _noFlowTimer.Tick += NoFlowTimer_Tick;
            //End code for AUTO-STOP Measn Auto Savedtest only for UROFLOW tests 12/12/2025

            HideMainContent();
            CreateMenuStrip();
            menuStrip1.Renderer = new CustomMenuRenderer();

            _screenMinutes = LoadScreenMinutes();

            CreateMainContentForPatient();

            LoadHomeScreenPanel();

            panel8.Visible = false;

            //foreach (ToolStripItem item in menuStrip1.Items)
            //{
            //    if (item.Name == "settingsButton")
            //    {
            //        item.Enabled = false;
            //        item.ForeColor = Color.Gray;
            //        break;
            //    }
            //}

            EnableMenuStripItems();

            //Pane 1 Buttons
            toolTip1.SetToolTip(btnTestStart1, "Test Start");
            toolTip1.SetToolTip(btnTestPause1, "Test Pause");
            toolTip1.SetToolTip(btnTestStop1, "Test Stop");
            toolTip1.SetToolTip(btnTestSave1, "Test Save");
            toolTip1.SetToolTip(btnR1, "Artifact R1");
            toolTip1.SetToolTip(btnR2, "Artifact R2");
            toolTip1.SetToolTip(btnM, "Artifact M");
            toolTip1.SetToolTip(btnCM, "Reports & Comments");
            toolTip1.SetToolTip(btnGP, "Artifact GP");
            toolTip1.SetToolTip(btnRF, "Artifact RF");
            toolTip1.SetToolTip(btnS1, "Select Graph S1");
            toolTip1.SetToolTip(btnS2, "Select Graph S2");

            //Panel 2 Buttons
            toolTip1.SetToolTip(btnFS, "First Sensation");
            toolTip1.SetToolTip(btnFD, "First Desire");
            toolTip1.SetToolTip(btnND, "Normal Desire");
            toolTip1.SetToolTip(btnSD, "Strong Desire");
            toolTip1.SetToolTip(btnBC, "Bladder Capacity");

            toolTip1.SetToolTip(btnTestStart2, "Test Start");
            toolTip1.SetToolTip(btnTestPause2, "Test Pause");
            toolTip1.SetToolTip(btnTestStop2, "Test Stop");
            toolTip1.SetToolTip(btnTestSave2, "Test Save");

            toolTip1.SetToolTip(btnPumpStart, "Pump Start");
            toolTip1.SetToolTip(btnPumpSpUp, "Speed Up");
            toolTip1.SetToolTip(btnPumpSpDown, "Speed Down");
            toolTip1.SetToolTip(btnPumpStop, "Pump Stop");

            toolTip1.SetToolTip(btn0, "Channel Zero");
            toolTip1.SetToolTip(btnPves, "Channel PVES");
            toolTip1.SetToolTip(btnPabd, "Channel PABD");
            toolTip1.SetToolTip(btnPura, "Channel PURA");

            toolTip1.SetToolTip(btnEventGP, "Event Button GP");
            toolTip1.SetToolTip(btnEventLE, "Event Button LE");
            toolTip1.SetToolTip(btnEventCU, "Event Button CU");
            toolTip1.SetToolTip(btnEventUDC, "Event Button UDC");
            toolTip1.SetToolTip(btnEventRF, "Event Button RF");

            toolTip1.SetToolTip(btnGraphS1, "Select Graph S1");
            toolTip1.SetToolTip(btnGraphS2, "Select Graph S2");

            toolTip1.SetToolTip(btnArtifactR1, "Artifact Marking R1");
            toolTip1.SetToolTip(btnArtifactR2, "Artifact Marking R2");
            toolTip1.SetToolTip(btnArtifactM, "Artifact Marking M");
            toolTip1.SetToolTip(btnCM, "Reports & Comments");


            //Panel #
            toolTip1.SetToolTip(btnTestStart3, "Test Start");
            toolTip1.SetToolTip(btnTestPause3, "Test Pause");
            toolTip1.SetToolTip(btnTestStop3, "Test Stop");
            toolTip1.SetToolTip(btnTestSave3, "Test Save");

            toolTip1.SetToolTip(btnChannel0, "Channel Zero");
            toolTip1.SetToolTip(btnChannelPves, "Channel PVES");
            toolTip1.SetToolTip(btnChannelPirp, "Channel PIRP");

            toolTip1.SetToolTip(btnPumpStart3, "Pump Start");
            toolTip1.SetToolTip(btnPumpSpUp3, "Speed Up");
            toolTip1.SetToolTip(btnPumpSpDown3, "Speed Down");
            toolTip1.SetToolTip(btnPumpStop3, "Pump Stop");

            toolTip1.SetToolTip(btnEventGP3, "Event Button GP");
            toolTip1.SetToolTip(btnEventLE3, "Event Button LE");
            toolTip1.SetToolTip(btnEventCU3, "Event Button CU");
            toolTip1.SetToolTip(btnEventUDC3, "Event Button UDC");
            toolTip1.SetToolTip(btnEventRF3, "Event Button RF");

            toolTip1.SetToolTip(btnUppStart, "UPP Control Start");
            toolTip1.SetToolTip(btnUppStop, "Event Button Stop");
            toolTip1.SetToolTip(btnUppDIR, "Event Button DIR");
            toolTip1.SetToolTip(btnUppSpeed, "Event Button Speed");

            toolTip1.SetToolTip(btnMarkingR1, "Artifact R1");
            toolTip1.SetToolTip(btnMarkingR2, "Artifact R2");
            toolTip1.SetToolTip(btnMarkingM, "Artifact M");
            toolTip1.SetToolTip(btnCMP4, "Reports & Comments");

            toolTip1.SetToolTip(btnSelectGraphS1, "Select Graph S1");
            toolTip1.SetToolTip(btnSelectGraphS2, "Select Graph S2");



            _watcher = new DeviceWatcher();

            // 2) Provide a real hardware probe (NI-DAQ style via your DaqService)
            _watcher.SetProbe(new DeviceWatcher.DeviceProbe(ProbeDaqOnce));

            // 3) Subscribe to events
            _watcher.Connected += new EventHandler(Watcher_Connected);
            _watcher.Disconnected += new EventHandler(Watcher_Disconnected);
            _watcher.StateChanged += new EventHandler(Watcher_StateChanged);

            // 4) Start it
            _watcher.Start(Owner);

        }

        private void HideMainContent()
        {
            if (mainContentPanel != null)
                mainContentPanel.Visible = false;

            if (mainContentPanelGraphOnly != null)
                mainContentPanelGraphOnly.Visible = false;

            if (rightSidebarPanel != null)
                rightSidebarPanel.Visible = false;
        }

        private void btnStartTest_Click(object sender, EventArgs e)
        {
            if (mainContentPanelPatient != null && this.Controls.Contains(mainContentPanelPatient))
            {
                this.Controls.Remove(mainContentPanelPatient);
                mainContentPanelPatient.Dispose();
                mainContentPanelPatient = null;
            }


            CreateMainContent();
            //CreateMainContentStopCamera();
            mainContentPanel.Visible = true;
        }

        //Code For Review Mode Camera Test Stop Camera 16-01-2026
        private void btnReviewModeCameraTest_Click(object sender, EventArgs e)
        {
            if (mainContentPanelPatient != null && this.Controls.Contains(mainContentPanelPatient))
            {
                this.Controls.Remove(mainContentPanelPatient);
                mainContentPanelPatient.Dispose();
                mainContentPanelPatient = null;
            }


            //CreateMainContent();
            ReviewModeStopCamera();
            mainContentPanel.Visible = true;
        }

        private void btnGraphOnly_Click(object sender, EventArgs e)
        {
            if (mainContentPanelPatient != null && this.Controls.Contains(mainContentPanelPatient))
            {
                this.Controls.Remove(mainContentPanelPatient);
                mainContentPanelPatient.Dispose();
                mainContentPanelPatient = null;
            }


            CreateMainContentOnlyGraph();
            mainContentPanelGraphOnly.Visible = true;
        }

        //private void ForceChartZero()
        //{
        //    if (_liveChart == null)
        //        return;

        //    double[] zero = new double[_liveChart.GetChannelCount()];
        //    double t = 0;

        //    // push 10 zero frames to flush old values
        //    for (int i = 0; i < 10; i++)
        //    {
        //        t += 0.1;
        //        _liveChart.AppendSample(zero, t);
        //    }

        //    _liveChart.Invalidate();
        //}



        //For Logo Buttun
        private void btnShowPatients_Click(object sender, EventArgs e)
        {
            if (_orch != null)
                _orch.Dispose();

            //ForceChartZero();
            ResetTimeForNewTest();
            _markerCounter = 0;

            //ClearArtifactSelections();

            StopCamera();
            HideMainContent();
            CreateMainContentForPatient();
            EnableMenuStripItems();
            PumpStop();

            _isLiveTestRunning = false;

            ClearSavedImagesForCurrentTest();
            if (_capturedImages != null)
                _capturedImages.Clear();

            if (capturedImagesPanel != null)
                capturedImagesPanel.Controls.Clear();

            mainContainerPanel.Visible = true;
            label36.Visible = false;

            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
            panel6.Visible = false;
            panel7.Visible = false;
            //panel8.Visible = true;

            _isRecording = false;
            _isPaused = false;
            _pauseStartedAt = null;
            _pausedTime = TimeSpan.Zero;

            _testTimer.Stop();
            UpdateTimerLabel();     // reset label back to 00:00

            UpdateButtons(recording: false, paused: false, canSave: true);

            try { if (_orch != null) _orch.Stop(); } catch { }
            DetachOrch();
        }


        private void DisableMenuStripItems()
        {
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                // disable these specific items
                if (item.Name == "patientButton" ||
                    item.Name == "menuButton" ||
                    item.Name == "settingsButton" ||
                    item.Name == "infoButton" ||
                    item.Name == "FileMenu" ||
                    item.Name == "SetupMenu" ||
                    item.Name == "HelpMenu" ||
                    item.Name == "PumpArmMenu" ||
                    item.Name == "⛯Menu")
                {
                    item.Enabled = false;
                    item.ForeColor = Color.Gray; // visual feedback (optional)
                }

                // ✅ settingsButton MUST stay ENABLED
                //if (item.Name == "settingsButton")
                //{
                //    item.Enabled = true;
                //    item.ForeColor = Color.White;
                //}
                //else if (item.Name == "infoButton")
                //{
                //    item.Enabled = true;
                //    item.ForeColor = Color.White;
                //}
            }
        }

        //Code For Show Review Mode Test time show buttons
        private void ShowPrintButtons()
        {
            //foreach (ToolStripItem item in menuStrip1.Items)
            //{
            //    // disable these specific items
            //    if (item.Name == "patientButton" ||
            //        item.Name == "menuButton" ||
            //        //item.Name == "settingsButton" ||
            //        //item.Name == "infoButton" ||
            //        item.Name == "FileMenu" ||
            //        item.Name == "SetupMenu" ||
            //        item.Name == "HelpMenu" ||
            //        item.Name == "PumpArmMenu" ||
            //        item.Name == "⛯Menu")
            //    {
            //        item.Enabled = false;
            //        item.ForeColor = Color.Gray; // visual feedback (optional)
            //    }

            //    // ✅ settingsButton MUST stay ENABLED
            //    if (item.Name == "settingsButton")
            //    {
            //        item.Enabled = true;
            //        item.ForeColor = Color.White;
            //    }
            //    else if (item.Name == "infoButton")
            //    {
            //        item.Enabled = true;
            //        item.ForeColor = Color.White;
            //    }
            //}

            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Name == "patientButton" ||
                    item.Name == "menuButton" ||
                    item.Name == "settingsButton" ||
                    item.Name == "infoButton" ||
                    item.Name == "FileMenu" ||
                    item.Name == "SetupMenu" ||
                    item.Name == "HelpMenu" ||
                    item.Name == "PumpArmMenu" ||
                    item.Name == "⛯Menu")
                {
                    item.Enabled = true;
                    item.ForeColor = Color.White;
                }

                // ✅ PumpArm button MUST be DISABLED now
                if (item.Name == "PumpArmMenu")
                {
                    item.Enabled = false;
                    item.ForeColor = Color.Gray;
                }
            }
        }

        private void EnableMenuStripItems()
        {
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Name == "patientButton" ||
                    item.Name == "menuButton" ||
                    //item.Name == "settingsButton" ||
                    //item.Name == "infoButton" ||
                    item.Name == "FileMenu" ||
                    item.Name == "SetupMenu" ||
                    item.Name == "HelpMenu" ||
                    item.Name == "PumpArmMenu" ||
                    item.Name == "⛯Menu")
                {
                    item.Enabled = true;
                    item.ForeColor = Color.White;
                }

                // ✅ settingsButton MUST be DISABLED now
                if (item.Name == "settingsButton")
                {
                    item.Enabled = false;
                    item.ForeColor = Color.Gray;
                }
                else if (item.Name == "infoButton")
                {
                    item.Enabled = false;
                    item.ForeColor = Color.Gray;
                }
            }
        }

        //Show All MenuStrip SavedTest or Review Mode
        private void ShowAllMenuStripItems()
        {
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                if (item.Name == "patientButton" ||
                    item.Name == "menuButton" ||
                    item.Name == "settingsButton" ||
                    item.Name == "infoButton" ||
                    item.Name == "FileMenu" ||
                    item.Name == "SetupMenu" ||
                    item.Name == "HelpMenu" ||
                    item.Name == "PumpArmMenu" ||
                    item.Name == "⛯Menu")
                {
                    item.Enabled = true;
                    item.ForeColor = Color.White;
                }

                // ✅ PumpArm button MUST be DISABLED now
                if (item.Name == "PumpArmMenu")
                {
                    item.Enabled = false;
                    item.ForeColor = Color.Gray;
                }
            }
        }

        private void btnForm3_Click(object sender, EventArgs e)
        {
            Form3 graphForm = new Form3();
            graphForm.Show();
        }


        private int LoadScreenMinutes()
        {
            try
            {
                //string folder = Path.Combine(Application.StartupPath, "Saved Data", "ScaleAndColorSetup");
                string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");
                if (!Directory.Exists(folder))
                    return 2;

                string[] files = Directory.GetFiles(folder, "*.dat");

                if (files.Length == 0)
                    return 2;

                // load the first or latest file
                string filePath = files.Last();

                byte[] encrypted = File.ReadAllBytes(filePath);
                string decrypted = CryptoHelper.Decrypt(encrypted);

                ScaleAndColorModel data =
                    System.Text.Json.JsonSerializer.Deserialize<ScaleAndColorModel>(decrypted);

                if (data == null || string.IsNullOrWhiteSpace(data.NumberOfScreen))
                    return 2;

                if (int.TryParse(data.NumberOfScreen, out int minutes))
                    return minutes;

                return 2;
            }
            catch
            {
                return 2;
            }
        }

        //private SystemSetupModel GetSystemSetData()
        //{
        //    try
        //    {
        //        string folder = Path.Combine(Application.StartupPath, "Saved Data", "SystemSetup");

        //        if (!Directory.Exists(folder))
        //            return null;

        //        string[] files = Directory.GetFiles(folder, "*.dat");

        //        if (files.Length == 0)
        //            return null;

        //        string filePath = files[0];

        //        byte[] encrypted = File.ReadAllBytes(filePath);
        //        string json = CryptoHelper.Decrypt(encrypted);

        //        return JsonSerializer.Deserialize<SystemSetupModel>(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Load error: " + ex.Message);
        //        return null;
        //    }
        //}

        private SystemSetupModel _SystemSetup;

        private void LoadSystemSetup()
        {
            try
            {
                // string folder = Path.Combine(Application.StartupPath, "Saved Data", "SystemSetup");
                string folder = AppPathManager.GetFolderPath("SystemSetup");

                if (!Directory.Exists(folder))
                    return;

                string[] files = Directory.GetFiles(folder, "*.dat");

                if (files.Length == 0)
                    return;

                string filePath = files[0];

                byte[] encrypted = File.ReadAllBytes(filePath);
                string json = CryptoHelper.Decrypt(encrypted);

                _SystemSetup = JsonSerializer.Deserialize<SystemSetupModel>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load error: " + ex.Message);
            }
        }

        //Start Code For Graph 09/09/2025

        private void LoadGraph()
        {
            LoadSystemSetup();

            int pumpConst1 = 1500;
            int pumpConst2 = 2000;
            int defaultInfusion = 20;

            // If SystemSetup loaded → use dynamic values
            if (_SystemSetup != null)
            {
                pumpConst1 = Convert.ToInt32(_SystemSetup.Constant1);
                pumpConst2 = Convert.ToInt32(_SystemSetup.Constant2);
                defaultInfusion = Convert.ToInt32(_SystemSetup.DefualInfusion);
            }

            _infusionRate = ClampInfusionRate(defaultInfusion);

            var daq = new DaqService();
            var cal = new Calibration(_profile);

            // ✅ IMPORTANT: match your finite burst design
            // DaqService.SampleRateHz = 100 samples / 0.25 sec = 400 Hz
            // DisplayHz = 4 samples/sec (one plotted sample per 0.25 sec)
            var proc = new SignalProcessor(cal, daq.SampleRateHz, 8.0);

            _signalProcessor = proc; // if you have this field (you use it in OnDisplayFrame)
            IPumpController pump = new PumpController(pumpConst1, pumpConst2);
            _orch = new TestOrchestrator(daq, proc, pump, new SysSetupStore());
            AttachOrch();  // This subscribes to BOTH OnDisplayFrame AND OnRawSample

            _activemode = GetModeFromTestName(currenttest);
            _signalProcessor.SetMode(_activemode);
            _orchStartedAt = DateTime.UtcNow;

            _wallStartSec = 0;
            _wallStartSet = false;
            try { _orch.Start(); } catch { }


            if (_useBleEmg)
                StartEmg();


            proc.SetFlowWindowFromConstant(10);
            proc.SetEmgWindowMs(300);
            proc.SetSmoothingAlpha(0.25);

            // ✅ Rendering: 4Hz data doesn’t need 20fps; 10fps is plenty and reduces paint churn
            _liveChart.SetFps(30);

            // 120 sec window (as you said)
            _liveChart.SetVisibleDuration(120.0);

            _liveChart.Start();
        }

        //private void LoadGraph()
        //{
        //    LoadSystemSetup();

        //    int pumpConst1 = 1500;
        //    int pumpConst2 = 2000;
        //    int defaultInfusion = 20;

        //    // If SystemSetup loaded → use dynamic values
        //    if (_SystemSetup != null)
        //    {
        //        pumpConst1 = Convert.ToInt32(_SystemSetup.Constant1);
        //        pumpConst2 = Convert.ToInt32(_SystemSetup.Constant2);
        //        defaultInfusion = Convert.ToInt32(_SystemSetup.DefualInfusion);
        //    }

        //    _infusionRate = ClampInfusionRate(defaultInfusion);

        //    bool isResume = _resumeArmed;


        //    DetachOrch();
        //    // Ensure orchestrator exists and is wired to chart

        //        var daq = new DaqService();
        //        var cal = new Calibration(_profile);
        //        if (_signalProcessor == null || !isResume)
        //        _signalProcessor = new SignalProcessor(cal, sampleRateHz: 400.0, displayHz: 4.0);
        //    IPumpController pump = new PumpController(pumpConst1, pumpConst2);
        //        _orch = new TestOrchestrator(daq, _signalProcessor, pump, new SysSetupStore());


        //    AttachOrch();
        //    try { _orch.Start(); } catch { }

        //    if (_currentTestDef == null && _testMgr != null)
        //        _currentTestDef = _testMgr.GetDefinition(currenttest);

        //    _activemode = GetModeFromTestName(currenttest);
        //    _signalProcessor.SetMode(_activemode);

        //    _signalProcessor.SetFlowWindowFromConstant(_profile.Constants[6]);

        //    _signalProcessor.SetEmgWindowMs(300);
        //    _signalProcessor.SetSmoothingAlpha(0.08);

        //    if (!isResume)
        //        _signalProcessor.ZeroNow();

        //    _liveChart.SetMinutesPerScreen(_screenMinutes);
        //    _liveChart.UsePixelBuckets = true;
        //    _liveChart.DrawEnvelopeInLive = false; // you can set true if you want the min/max envelope in live view

        //    // _liveChart.SetFps(30);
        //    _liveChart.SetVisibleDuration(_screenMinutes * 60.0); //60
        //    _liveChart.Start();
        //}




        // Start Code For Show the Dianamic Constants Values on 28/10/2025
        private string GetSystemSetupFolder()
        {
            string exeFolder = Application.StartupPath;
            // string folder = Path.Combine(exeFolder, "Saved Data", "SystemSetup");
            string folder = AppPathManager.GetFolderPath("SystemSetup");
            return folder;
        }

        private string FindMostRecentDatFile()
        {
            string folder = GetSystemSetupFolder();

            if (!Directory.Exists(folder))
                return null;

            var datFiles = Directory.GetFiles(folder, "*.dat", SearchOption.TopDirectoryOnly);
            if (datFiles == null || datFiles.Length == 0)
                return null;

            return datFiles.OrderByDescending(f => File.GetLastWriteTimeUtc(f)).FirstOrDefault();
        }

        private bool TryLoadSystemSetupFromFile(string filePath, out SystemSetupModel model, out string failReason)
        {
            model = null;
            failReason = null;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string jsonData = CryptoHelper.Decrypt(encryptedData);

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    failReason = "Decrypted payload is empty.";
                    return false;
                }

                var record = System.Text.Json.JsonSerializer.Deserialize<SystemSetupModel>(jsonData);
                if (record == null)
                {
                    failReason = "Deserialization returned null.";
                    return false;
                }

                model = record;
                return true;
            }
            catch (Exception ex)
            {
                failReason = ex.Message;
                return false;
            }
        }

        //Start code 26-02-2026

        // Add this field at the class level
        private SystemSetupModel _systemSetupModel;
        public void ReloadSystemSetupAndConstants()
        {
            try
            {
                // Call the Constants() method to reload everything
                Constants();

                System.Diagnostics.Debug.WriteLine("System Setup and Constants reloaded successfully");

                // If there's an active signal processor, update its calibration
                if (_signalProcessor != null && _profile != null)
                {
                    var cal = new Calibration(_profile);
                    // If SignalProcessor has an UpdateCalibration method
                    // _signalProcessor.UpdateCalibration(cal);

                    // If not, you might need to restart processing
                    if (_orch != null)
                    {
                        _orch.StopProcessing();
                        _orch.StartProcessing();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading SystemSetup: {ex.Message}");
            }
        }
        private void Constants()
        {
            // Defaults (your originals)
            var defaultConstants = new int[] { 1030, 1030, 1994, 2038, 250, 0, 250, 1030 };
            double sg1 = 1.0;
            int uppCount = 2084;
            var offsetsCounts = new double[8];
            var constants = new int[8];

            string folder = GetSystemSetupFolder();
            string usedFile = null;
            SystemSetupModel setup = null;

            string candidate = FindMostRecentDatFile();

            if (candidate != null)
            {
                if (TryLoadSystemSetupFromFile(candidate, out SystemSetupModel m, out string reason))
                {
                    setup = m;
                    usedFile = candidate;
                }
                else
                {
                    var allFiles = Directory.GetFiles(folder, "*.dat", SearchOption.TopDirectoryOnly)
                                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                            .ToArray();

                    foreach (var f in allFiles)
                    {
                        if (f == candidate) continue;
                        if (TryLoadSystemSetupFromFile(f, out SystemSetupModel mm, out string r))
                        {
                            setup = mm;
                            usedFile = f;
                            break;
                        }
                    }
                }
            }

            if (setup != null)
            {
                constants[0] = int.TryParse(setup.Pves, out int c0) ? c0 : defaultConstants[0];
                constants[1] = int.TryParse(setup.Pabd, out int c1) ? c1 : defaultConstants[1];
                constants[2] = int.TryParse(setup.Flow, out int c2) ? c2 : defaultConstants[2];
                constants[3] = int.TryParse(setup.Vinf, out int c3) ? c3 : defaultConstants[3];
                constants[4] = int.TryParse(setup.EMG, out int c4) ? c4 : defaultConstants[4];
                constants[5] = int.TryParse(setup.UPP, out int c5) ? c5 : defaultConstants[5];
                constants[6] = int.TryParse(setup.Rate, out int c6) ? c6 : defaultConstants[6];
                constants[7] = int.TryParse(setup.Pura, out int c7) ? c7 : defaultConstants[7];
                //constants[6] = int.TryParse(setup.EMG, out int c6) ? c6 : defaultConstants[6];

                sg1 = double.TryParse(setup.SpGravity, out double sgParsed) ? sgParsed : sg1;
                uppCount = int.TryParse(setup.UPP, out int uppParsed) ? uppParsed : uppCount;

                //New
                if (double.TryParse(setup.P1, out double p1))
                    P1 = p1;

                if (double.TryParse(setup.ChangeBottle, out double cb))
                    ChangeBottle = cb;

                if (double.TryParse(setup.JF, out double jf))
                    JF = jf;

                if (double.TryParse(setup.U1, out double u1))
                    U1 = u1;

                if (double.TryParse(setup.UPPH, out double uh))
                    UH = uh;



            }
            else
            {
                Array.Copy(defaultConstants, constants, 8);

                P1 = DEFAULT_P1;
                ChangeBottle = DEFAULT_CHANGE_BOTTLE;
                JF = DEFAULT_JF;
                U1 = DEFAULT_U1;
                UH = DEFAULT_UH;

                //MessageBox.Show($"No valid SystemSetup .dat file found in:\n{folder}\nUsing default constants.", "SystemSetup Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //if (usedFile != null)
            //{
            //    MessageBox.Show($"Loaded system setup from:\n{usedFile}", "SystemSetup Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}

            _profile = CalibrationProfileFactory.FromLegacy(constants, offsetsCounts, uppCount, sg1);
        }




        //Commit on 17/12/2025
        //private void Constants()
        //{
        //    // Defaults (your originals)
        //    var defaultConstants = new int[] { 1030, 1030, 1994, 2038, 250, 0, 250 };
        //    double sg1 = 1.0;
        //    int uppCount = 2084;
        //    var offsetsCounts = new double[7];
        //    var constants = new int[7];

        //    string folder = GetSystemSetupFolder();
        //    string usedFile = null;
        //    SystemSetupModel setup = null;

        //    string candidate = FindMostRecentDatFile();

        //    if (candidate != null)
        //    {
        //        if (TryLoadSystemSetupFromFile(candidate, out SystemSetupModel m, out string reason))
        //        {
        //            setup = m;
        //            usedFile = candidate;
        //        }
        //        else
        //        {
        //            var allFiles = Directory.GetFiles(folder, "*.dat", SearchOption.TopDirectoryOnly)
        //                                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
        //                                    .ToArray();

        //            foreach (var f in allFiles)
        //            {
        //                if (f == candidate) continue; 
        //                if (TryLoadSystemSetupFromFile(f, out SystemSetupModel mm, out string r))
        //                {
        //                    setup = mm;
        //                    usedFile = f;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    if (setup != null)
        //    {
        //        constants[0] = int.TryParse(setup.Pves, out int c0) ? c0 : defaultConstants[0];
        //        constants[1] = int.TryParse(setup.Pabd, out int c1) ? c1 : defaultConstants[1];
        //        constants[2] = int.TryParse(setup.Flow, out int c2) ? c2 : defaultConstants[2];
        //        constants[3] = int.TryParse(setup.Vinf, out int c3) ? c3 : defaultConstants[3];
        //        constants[4] = int.TryParse(setup.EMG, out int c4) ? c4 : defaultConstants[4];
        //        constants[5] = int.TryParse(setup.UPP, out int c5) ? c5 : defaultConstants[5];
        //        constants[6] = int.TryParse(setup.Rate, out int c6) ? c6 : defaultConstants[6];
        //        //constants[6] = int.TryParse(setup.EMG, out int c6) ? c6 : defaultConstants[6];

        //        sg1 = double.TryParse(setup.SpGravity, out double sgParsed) ? sgParsed : sg1;
        //        uppCount = int.TryParse(setup.UPP, out int uppParsed) ? uppParsed : uppCount;

        //    }
        //    else
        //    {
        //        Array.Copy(defaultConstants, constants, 7);
        //        //MessageBox.Show($"No valid SystemSetup .dat file found in:\n{folder}\nUsing default constants.", "SystemSetup Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }

        //    //if (usedFile != null)
        //    //{
        //    //    MessageBox.Show($"Loaded system setup from:\n{usedFile}", "SystemSetup Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    //}

        //    _profile = CalibrationProfileFactory.FromLegacy(constants, offsetsCounts, uppCount, sg1);
        //}
        // Start Code For Show the Dianamic Constants Values on 28/10/2025



        // private double _lastPlotT = double.NegativeInfinity;
        private double _lastChartEmitT = double.NegativeInfinity;
        private double[] _valsBuf;
        // ===== CPP tick sampling for computations/report =====
        private const double CPP_TICK_SEC = 0.25; // constant[2]=250ms => 4Hz
        private double _lastRecordedTickT = double.NegativeInfinity;

        //Bhushan 03-03-2026
        private double _lastVinfDisplayed = 0.0;
        private void OnDisplayFrame(SampleFrame f)
        {
            if (!_acceptLiveFrames) return;
            if (_isPlaybackMode) return;
            if (_isPaused) return;
            if (_activeIndices == null || _activeIndices.Length == 0) return;
            if (f.Values == null) return;

            try
            {
                int len = _activeIndices.Length;

                if (_valsBuf == null || _valsBuf.Length != len)
                    _valsBuf = new double[len];

                double[] vals = _valsBuf;

                for (int i = 0; i < len; i++)
                {
                    int idx = _activeIndices[i];

                    // Do NOT kill the whole frame if one index is wrong.
                    // Use NaN so chart / checks can ignore it safely.
                    if (idx < 0 || idx >= f.Values.Length)
                    {
                        vals[i] = double.NaN;
                        continue;
                    }

                    vals[i] = f.Values[idx];
                }

                _plotRateHz = _signalProcessor.DisplayRateHz;

                double tOut = f.T;

                if (_firstFrameUtc == DateTime.MinValue)
                {
                    _firstFrameUtc = DateTime.UtcNow;
                    _firstTOut = tOut;
                    //  PerfTrace.Log("LAG", $"Init: firstTOut={_firstTOut:F2}s");
                }

                double wall = (DateTime.UtcNow - _firstFrameUtc).TotalSeconds;
                double tRel = tOut - _firstTOut;               // time progressed in data stream
                double lag = wall - tRel;                      // +ve means UI behind data

                //PerfTrace.EveryMs("LAG", 1000, () => $"wall={wall:F2}s tRel={tRel:F2}s lag={lag:F2}s (tOut={tOut:F2}s)");


                if (_resumeArmed)
                {
                    _resumeShiftSeconds = _resumeOffset - tOut;
                    _resumeArmed = false;

                    // Important: prevent gating after time-shift
                    _lastChartEmitT = double.NegativeInfinity;
                }



                tOut += _resumeShiftSeconds;

                // If requested, re-zero X-axis time on the very next frame.
                // Fixes: demo mode/new test continues time from previous session.
                if (_timeBaseArmed)
                {
                    _timeBaseShiftSeconds = -tOut;     // make first plotted sample start at 0.00 sec
                    _timeBaseArmed = false;

                    // reset clamps/gates because time jumped
                    _lastPlotT = double.NegativeInfinity;
                    _lastChartEmitT = double.NegativeInfinity;
                    _lastRecordedTickT = double.NegativeInfinity;
                }

                // Apply session time shift so chart X-axis starts from 0
                tOut += _timeBaseShiftSeconds;

                if (tOut <= _lastPlotT)
                    tOut = _lastPlotT + (1.0 / Math.Max(1.0, _plotRateHz));

                _lastPlotT = tOut;

                // Feed chart at the processor display rate (your new design: 4 Hz).
                // Using a small tolerance to avoid double-emits due to clock adjustments.
                double chartDt = 1.0 / Math.Max(1.0, _plotRateHz); // e.g. 0.25 sec at 4 Hz
                double minEmitDt = chartDt * 0.80;                 // 20% tolerance

                if (tOut - _lastChartEmitT < minEmitDt)
                    return;

                _lastChartEmitT = tOut;


                // Recording moved below VINF offset block



                //Bhushan 03-03-2026 Start

                double correctedT = tOut - _timeOffsetAfterPause;

                // ---- VINF continuation: apply offset so plot doesn't reset to 0 after bottle change ----
                //if (_vinfResumeOffset != 0.0 && len > 3)
                //{
                //    vals[3] += _vinfResumeOffset;
                //}
                // Always track last displayed VINF for next pause
                if (len > 3) _lastVinfEngValue = vals[3];

                if (len > 3 && _vinfResumeOffset != 0.0)
                {
                    if (_vinfWaitingForFirstFrame)
                    {
                        _vinfResumeOffset = _vinfResumeOffset - vals[3];
                        _vinfWaitingForFirstFrame = false;
                    }
                    vals[3] += _vinfResumeOffset;
                }


                //// ----------------------- 07/03/2026 change pratik --------
                if (len > 3)
                    _lastVinfDisplayed = vals[3];

                //Bhushan 03-03-2026 END

                // Record AFTER offset applied — saved data matches display
                if (_isRecording)
                {
                    double[] copy = new double[len];
                    Array.Copy(vals, copy, len);
                    _recorded.Add(new SampleRecord(tOut, copy));
                }

                // 🔹 Inject BLE EMG value into DAQ frame before sending to chart
                if (_useBleEmg)
                {
                    var channelNames = _liveChart.GetChannelNames();
                    for (int i = 0; i < channelNames.Count && i < vals.Length; i++)
                    {
                        if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                        {
                            vals[i] = _filteredEmgValue;
                            break;
                        }
                    }
                }

                else
                {
                    // Debug: show DAQ EMG value
                    var channelNames = _liveChart.GetChannelNames();
                    for (int i = 0; i < channelNames.Count && i < vals.Length; i++)
                    {
                        if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DAQ] EMG value from DAQ: {vals[i]:F1}");
                            break;
                        }
                    }
                }

                _liveChart.AppendSample(vals, correctedT);
                /// ------------------------------------------------------------------

                //if (len > 3)
                //    _lastVinfDisplayed = vals[3];

                ////Bhushan 03-03-2026 END

                //// Record AFTER offset applied — saved data matches display
                //if (_isRecording)
                //{
                //    double[] copy = new double[len];
                //    Array.Copy(vals, copy, len);
                //    _recorded.Add(new SampleRecord(tOut, copy));
                //}

                //// 🔹 FIX: When using BLE EMG, inject the latest BLE value into DAQ frame
                //if (_useBleEmg)
                //{
                //    var channelNames = _liveChart.GetChannelNames();
                //    for (int i = 0; i < channelNames.Count && i < vals.Length; i++)
                //    {
                //        if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                //        {
                //            // Use the latest filtered EMG value from BLE
                //            double bleEmgValue = _filteredEmgValue; // This should already exist in your code

                //            // Mirror effect: send positive value
                //            vals[i] = Math.Abs(bleEmgValue);
                //            break;
                //        }
                //    }
                //}

                //// Send single merged sample with DAQ + BLE data
                //_liveChart.AppendSample(vals, correctedT);

                //// If BLE EMG, send mirrored samples with tiny time offset
                //System.Diagnostics.Debug.WriteLine($"[MIRROR CHECK] _useBleEmg={_useBleEmg}");

                //if (_useBleEmg)
                //{
                //    var channelNames = _liveChart.GetChannelNames();
                //    int emgIdx = -1;

                //    System.Diagnostics.Debug.WriteLine($"[MIRROR] Searching for EMG in {channelNames.Count} channels");

                //    for (int i = 0; i < channelNames.Count; i++)
                //    {
                //        System.Diagnostics.Debug.WriteLine($"[MIRROR] Channel[{i}] = '{channelNames[i]}'");
                //        if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                //        {
                //            emgIdx = i;
                //            System.Diagnostics.Debug.WriteLine($"[MIRROR] Found EMG at index {emgIdx}");
                //            break;
                //        }
                //    }

                //    if (emgIdx >= 0)
                //    {
                //        double emgValue = _filteredEmgValue;
                //        System.Diagnostics.Debug.WriteLine($"[MIRROR] EMG value={emgValue:F1}, abs={Math.Abs(emgValue):F1}");

                //        // Send negative mirror first
                //        double[] valsNeg = (double[])vals.Clone();
                //        valsNeg[emgIdx] = -Math.Abs(emgValue);

                //        System.Diagnostics.Debug.WriteLine($"[MIRROR NEG] Sending: [{string.Join(", ", valsNeg.Select(v => v.ToString("F1")))}] at t={correctedT:F3}");
                //        _liveChart.AppendSample(valsNeg, correctedT);

                //        // Send positive mirror with tiny offset (0.001 sec)
                //        double[] valsPos = (double[])vals.Clone();
                //        valsPos[emgIdx] = Math.Abs(emgValue);

                //        System.Diagnostics.Debug.WriteLine($"[MIRROR POS] Sending: [{string.Join(", ", valsPos.Select(v => v.ToString("F1")))}] at t={correctedT + 0.001:F3}");
                //        _liveChart.AppendSample(valsPos, correctedT + 0.001);
                //    }
                //    else
                //    {
                //        System.Diagnostics.Debug.WriteLine($"[MIRROR ERROR] EMG channel not found! Sending normal sample");
                //        _liveChart.AppendSample(vals, correctedT);
                //    }
                //}
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine($"[MIRROR] BLE disabled, sending normal DAQ sample");
                //    // Normal DAQ mode - single sample
                //    _liveChart.AppendSample(vals, correctedT);
                //}



                //// - ----------------------- change end ---------------------------

                // Defer heavy work to UI timer (prevents 1–2 sec lag)
                _uiLastVals = (double[])vals.Clone();
                _uiLastT = tOut;
                System.Threading.Interlocked.Exchange(ref _uiWorkPending, 1);


                if (_uiStartUtc == DateTime.MinValue)
                {
                    _uiStartUtc = DateTime.UtcNow;
                    _uiFirstT = f.T;
                }

                wall = (DateTime.UtcNow - _uiStartUtc).TotalSeconds;
                tRel = f.T - _uiFirstT;
                // PerfTrace.EveryMs("UILAG", 1000, () => $"wall={wall:F2}s uiRel={tRel:F2}s wall-ui={wall - tRel:F2}s");


                //PerfTrace.EveryMs("UI", 1000, () =>
                //{
                //    lag = (DateTime.UtcNow - _orchStartedAt).TotalSeconds - tOut;
                //    return $"tOut={tOut:F2}s lagVsWall={lag:F2}s";
                //});

                if (_monitorNoFlow && _isRecording)
                {
                    double qvol = f.Values[2];
                    if (Math.Abs(qvol - _lastVolumeValue) > FLOW_THRESHOLD)
                    {
                        _lastMovementTime = DateTime.Now;
                        _lastVolumeValue = qvol;
                    }
                }
            }
            catch
            {
            }
        }

        //private void OnDisplayFrame(SampleFrame f)
        //{
        //    if (!_acceptLiveFrames) return;
        //    if (_isPlaybackMode) return;
        //    if (_isPaused) return;
        //    if (_activeIndices == null || _activeIndices.Length == 0) return;
        //    if (f.Values == null) return;

        //    try
        //    {
        //        int len = _activeIndices.Length;

        //        if (_valsBuf == null || _valsBuf.Length != len)
        //            _valsBuf = new double[len];

        //        double[] vals = _valsBuf;

        //        for (int i = 0; i < len; i++)
        //        {
        //            int idx = _activeIndices[i];

        //            // Do NOT kill the whole frame if one index is wrong.
        //            // Use NaN so chart / checks can ignore it safely.
        //            if (idx < 0 || idx >= f.Values.Length)
        //            {
        //                vals[i] = double.NaN;
        //                continue;
        //            }

        //            vals[i] = f.Values[idx];
        //        }

        //        _plotRateHz = _signalProcessor.DisplayRateHz;

        //        double tOut = f.T;

        //        if (_firstFrameUtc == DateTime.MinValue)
        //        {
        //            _firstFrameUtc = DateTime.UtcNow;
        //            _firstTOut = tOut;
        //            PerfTrace.Log("LAG", $"Init: firstTOut={_firstTOut:F2}s");
        //        }

        //        double wall = (DateTime.UtcNow - _firstFrameUtc).TotalSeconds;
        //        double tRel = tOut - _firstTOut;               // time progressed in data stream
        //        double lag = wall - tRel;                      // +ve means UI behind data

        //        PerfTrace.EveryMs("LAG", 1000, () => $"wall={wall:F2}s tRel={tRel:F2}s lag={lag:F2}s (tOut={tOut:F2}s)");


        //        if (_resumeArmed)
        //        {
        //            _resumeShiftSeconds = _resumeOffset - tOut;
        //            _resumeArmed = false;

        //            // Important: prevent gating after time-shift
        //            _lastChartEmitT = double.NegativeInfinity;
        //        }



        //        tOut += _resumeShiftSeconds;

        //        // If requested, re-zero X-axis time on the very next frame.
        //        // Fixes: demo mode/new test continues time from previous session.
        //        if (_timeBaseArmed)
        //        {
        //            _timeBaseShiftSeconds = -tOut;     // make first plotted sample start at 0.00 sec
        //            _timeBaseArmed = false;

        //            // reset clamps/gates because time jumped
        //            _lastPlotT = double.NegativeInfinity;
        //            _lastChartEmitT = double.NegativeInfinity;
        //            _lastRecordedTickT = double.NegativeInfinity;
        //        }

        //        // Apply session time shift so chart X-axis starts from 0
        //        tOut += _timeBaseShiftSeconds;

        //        if (tOut <= _lastPlotT)
        //            tOut = _lastPlotT + (1.0 / Math.Max(1.0, _plotRateHz));

        //        _lastPlotT = tOut;

        //        // Feed chart at the processor display rate (your new design: 4 Hz).
        //        // Using a small tolerance to avoid double-emits due to clock adjustments.
        //        double chartDt = 1.0 / Math.Max(1.0, _plotRateHz); // e.g. 0.25 sec at 4 Hz
        //        double minEmitDt = chartDt * 0.80;                 // 20% tolerance

        //        if (tOut - _lastChartEmitT < minEmitDt)
        //            return;

        //        _lastChartEmitT = tOut;


        //        if (_isRecording)
        //        {
        //            // Record EVERY frame that arrives from the processor
        //            // This ensures review mode matches live mode exactly
        //            double[] copy = new double[len];
        //            Array.Copy(vals, copy, len);
        //            _recorded.Add(new SampleRecord(tOut, copy));
        //        }



        //        //Bhushan 03-03-2026 Start

        //        double correctedT = tOut - _timeOffsetAfterPause;   

        //        // ---- VINF continuation: apply offset so plot doesn't reset to 0 after bottle change ----
        //        //if (_vinfResumeOffset != 0.0 && len > 3)
        //        //{
        //        //    vals[3] += _vinfResumeOffset;
        //        //}
        //        // Always track last displayed VINF for next pause
        //        if (len > 3) _lastVinfEngValue = vals[3];

        //        if (len > 3 && _vinfResumeOffset != 0.0)
        //        {
        //            if (_vinfWaitingForFirstFrame)
        //            {
        //                _vinfResumeOffset = _vinfResumeOffset - vals[3];
        //                _vinfWaitingForFirstFrame = false;
        //            }
        //            vals[3] += _vinfResumeOffset;
        //        }

        //        if (len > 3)
        //            _lastVinfDisplayed = vals[3];

        //        //Bhushan 03-03-2026 END

        //        _liveChart.AppendSample(vals, correctedT);



        //        // Defer heavy work to UI timer (prevents 1–2 sec lag)
        //        _uiLastVals = (double[])vals.Clone();
        //        _uiLastT = tOut;
        //        System.Threading.Interlocked.Exchange(ref _uiWorkPending, 1);


        //        if (_uiStartUtc == DateTime.MinValue)
        //        {
        //            _uiStartUtc = DateTime.UtcNow;
        //            _uiFirstT = f.T;
        //        }

        //        wall = (DateTime.UtcNow - _uiStartUtc).TotalSeconds;
        //        tRel = f.T - _uiFirstT;
        //        PerfTrace.EveryMs("UILAG", 1000, () => $"wall={wall:F2}s uiRel={tRel:F2}s wall-ui={wall - tRel:F2}s");


        //        PerfTrace.EveryMs("UI", 1000, () =>
        //        {
        //            lag = (DateTime.UtcNow - _orchStartedAt).TotalSeconds - tOut;
        //            return $"tOut={tOut:F2}s lagVsWall={lag:F2}s";
        //        });

        //        if (_monitorNoFlow && _isRecording)
        //        {
        //            double qvol = f.Values[2];
        //            if (Math.Abs(qvol - _lastVolumeValue) > FLOW_THRESHOLD)
        //            {
        //                _lastMovementTime = DateTime.Now;
        //                _lastVolumeValue = qvol;
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //}

        // x-axis time to 0
        private void ArmTimeZero()
        {
            // Make the very next incoming frame become t = 0 on X-axis
            _timeBaseShiftSeconds = 0.0;
            _timeBaseArmed = true;

            // Reset debug/lag trackers (keeps internals consistent)
            _firstFrameUtc = DateTime.MinValue;
            _uiStartUtc = DateTime.MinValue;
            _firstTOut = double.NaN;
            _uiFirstT = double.NaN;

            // Reset plot clamps + gates (important because time will jump)
            _lastPlotT = double.NegativeInfinity;
            _lastChartEmitT = double.NegativeInfinity;
            _lastRecordedTickT = double.NegativeInfinity;
        }



        // test time reset to zero
        private void ResetTimeForNewTest()
        {
            // Stop any UI timer updates
            try { _testTimer.Stop(); } catch { }

            // Reset recording/pause state
            _isRecording = false;
            _isPaused = false;
            _pauseStartedAt = null;
            _pausedTime = TimeSpan.Zero;

            // Reset the baseline start time (so next Record starts from 00:00 cleanly)
            _testStartTime = DateTime.Now;

            // Reset reconnect helpers
            _resumeShiftSeconds = 0.0;
            _resumeOffset = 0.0;
            _resumeArmed = false;
            _vinfResumeOffset = 0.0;   // clear VINF continuation on new test
            _lastVinfEngValue = 0.0;
            //Bhushan 03-03-2026 Start
            _lastVinfDisplayed = 0.0;        // ← add
            _vinfWaitingForFirstFrame = false; // ← add
            //Bhushan 03-03-2026 END

            // Reset + arm live X-axis timebase to start from 0 for the next session
            ArmTimeZero();

            // Reset the label to 00:00
            UpdateTimerLabel();
        }



        //Bhushan Comment On 05-01-2026
        //private void OnDisplayFrame(SampleFrame f)
        //{
        //    // --- fast exits ---
        //    if (!_acceptLiveFrames) return;
        //    if (_isPlaybackMode) return;
        //    if (_isPaused) return;
        //    if (_activeIndices == null || _activeIndices.Length == 0) return;

        //    try
        //    {
        //        // --- extract active channel values ---
        //        int len = _activeIndices.Length;
        //        double[] vals = new double[len];

        //        for (int i = 0; i < len; i++)
        //        {
        //            int idx = _activeIndices[i];
        //            vals[i] = f.Values[idx];
        //        }

        //        // --- establish UNIFORM plotting clock ---
        //        if (_resumeArmed)
        //        {
        //            _lastPlottedT = _resumeOffset;
        //            _resumeArmed = false;
        //        }

        //        _plotRateHz = _signalProcessor.DisplayRateHz;

        //        // in OnDisplayFrame
        //        _lastPlottedT += 1.0 / _plotRateHz;
        //        double tOut = _lastPlottedT;
        //       // _lastPlottedT = 0.0;
        //       // double tOut = f.T;
        //        // --- enforce monotonic time (critical) ---
        //        //if (tOut <= _lastPlotT)
        //        //    return;

        //        _lastPlotT = tOut;

        //        // --- recording ---
        //        if (_isRecording)
        //        {
        //            double[] copy = new double[len];
        //            Array.Copy(vals, copy, len);
        //            _recorded.Add(new SampleRecord(tOut, copy));
        //        }

        //        // --- plot FIRST (never block plotting) ---
        //        _liveChart.AppendSample(vals, tOut);


        //        if ((_frameCounter++ & 3) == 0)
        //        {
        //            try
        //            {
        //                RunLiveSystemChecks_RAW(_lastRawCounts);
        //                UpdateScaleAdvisories(vals, tOut);
        //            }
        //            catch
        //            {
        //                // swallow or log
        //            }
        //        }

        //        // --- AUTO-STOP logic (unchanged, safe) ---
        //        if (_monitorNoFlow && _isRecording)
        //        {
        //            double qvol = f.Values[2]; // QVOL channel

        //            if (Math.Abs(qvol - _lastVolumeValue) > FLOW_THRESHOLD)
        //            {
        //                _lastMovementTime = DateTime.Now;
        //                _lastVolumeValue = qvol;
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        // swallow or log as needed
        //    }
        //}



        //Bhushan 03-03-2026 Start 
        private bool _autoPausedByInfusionAlert = false;
        private bool _autoPauseInProgress = false;
        private double _vinfNewBottleStartEng = double.NaN;
        private bool _vinfWaitingForFirstFrame = false;
        private void PauseAndResumeWithPopup()
        {
            if (_autoPauseInProgress) return;
            _autoPauseInProgress = true;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            _resumeOffset = _lastPlotT;
            _resumeArmed = true;

            _testTimer.Stop();

            _pumpWasRunningBeforePause = _isPumpRunning;  // ← save state BEFORE stopping
            if (_isPumpRunning)
            {
                PumpStop();
            }

            // ⚠ Do NOT call StopProcessing yet — DAQ must stay alive so
            // _lastRawCounts keeps updating while we wait for bottle change.
            _arm?.StopArm();

            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            //double vinfAtPause = _lastVinfEngValue;
            double vinfAtPause = _lastVinfDisplayed;

            ShowBottleChangeDialog();   // blocks until bottle replaced & OK clicked

            _vinfResumeOffset = vinfAtPause;       // target: continue from here
            _vinfWaitingForFirstFrame = true;

            // NOW stop+restart processing (resets signal pipeline cleanly)
            _orch?.StopProcessing();
            _orch?.StartProcessing();

            _autoPausedByInfusionAlert = true;   // ← keep locked out briefly
            var rearmTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            rearmTimer.Tick += (s, e) =>
            {
                rearmTimer.Stop();
                rearmTimer.Dispose();
                _autoPausedByInfusionAlert = false;  // re-arm after 3s once new bottle settled
            };
            rearmTimer.Start();

            //_vinfResumeOffset = vinfAtPause;

            _isPaused = false;

            if (_pauseStartedAt.HasValue)
            {
                _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                _pauseStartedAt = null;
            }

            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Black;

            _testTimer.Start();
            UpdateTimerLabel();

            if (_pumpWasRunningBeforePause)
                PumpStart();

            _autoPauseInProgress = false;
        }

        private double _vinfNewBottleStartRaw = 0.0;

        private void ShowBottleChangeDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Recording Paused";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ControlBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new System.Drawing.Size(420, 180);
                dlg.TopMost = true;

                var lblMain = new Label
                {
                    Text = "Change Infusion Bottle",
                    Font = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Width = 400,
                    Height = 40,
                    Left = 10,
                    Top = 18
                };

                var lblStatus = new Label
                {
                    Text = "Waiting for new bottle...",
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Width = 400,
                    Height = 28,
                    Left = 10,
                    Top = 65,
                    ForeColor = Color.DarkOrange
                };

                var btnOk = new Button
                {
                    Text = "OK — Resume Recording",
                    Width = 210,
                    Height = 36,
                    Left = 100,
                    Top = 100,
                    Enabled = false
                };
                // btnOk.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };

                btnOk.Enabled = true; // always enabled

                btnOk.Click += (s, e) =>
                {
                    double vinfRaw;
                    lock (_rawLock)
                    {
                        if (_lastRawCounts == null || _lastRawCounts.Length <= 3) return;
                        vinfRaw = _lastRawCounts[3];
                    }
                    bool inDangerZone = vinfRaw > RAW_MIN_25 && vinfRaw < ChangeBottle;

                    if (inDangerZone)
                    {
                        lblStatus.Text = "Please change the bottle before resuming.";
                        lblStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        _vinfNewBottleStartRaw = vinfRaw;
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                };

                dlg.Controls.Add(lblMain);
                dlg.Controls.Add(lblStatus);
                dlg.Controls.Add(btnOk);

                var pollTimer = new System.Windows.Forms.Timer { Interval = 500 };
                pollTimer.Tick += (s, e) =>
                {
                    try
                    {
                        double vinfRaw;
                        lock (_rawLock)
                        {
                            if (_lastRawCounts == null || _lastRawCounts.Length <= 3) return;
                            vinfRaw = _lastRawCounts[3];
                        }
                        bool inDangerZone = vinfRaw > RAW_MIN_25 && vinfRaw < ChangeBottle;
                        //lblStatus.Text = $"Sensor: {vinfRaw:F0}  (need ≥ {ChangeBottle:F0} for new bottle)";
                        //btnOk.Enabled = !inDangerZone;

                        lblStatus.Text = inDangerZone
                       ? "Please change the infusion bottle to resume."
                       : "Bottle changed — click OK to resume.";

                        lblStatus.ForeColor = inDangerZone ? Color.DarkOrange : Color.DarkGreen;
                    }
                    catch { }
                };

                pollTimer.Start();
                dlg.FormClosed += (s, e) => pollTimer.Stop();
                dlg.ShowDialog(this);
                pollTimer.Stop();
            }
        }
        //Bhushan 03-03-2026 END


        //Start Error Code For Bhusahn Change on 05/12/2025
        private void RunLiveSystemChecks_RAW(double[] raw)
        {
            if (_liveChart == null) return;
            if (_autoPauseInProgress) return;


            // clear previous alerts
            _liveChart.ClearAllAlerts();

            // keep a copy for later (useful for UI/reporting)
            if (raw == null || raw.Length == 0)
            {
                _lastFrameRaw = null;
                _haltPlottingDueToAlert = false;
                return;
            }
            _lastFrameRaw = (double[])raw.Clone();

            // Which NI channels must be OK for this test?
            int[] toCheck = GetInputChannelsToCheckForTest(currenttest);
            bool hasAlerts = false;

            // helper local to set alerts and mark that we have alerts
            void SetAlertAndMark(string laneName, string message)
            {
                if (string.IsNullOrEmpty(laneName)) return;
                _liveChart.SetLaneAlert(laneName, message);
                hasAlerts = true;
            }

            // helper to map channel index -> visible lane name (skip hidden)
            string LaneNameOrEmpty(int ch)
            {
                return LaneNameForChannelIndex(ch, currenttest) ?? string.Empty;
            }

            // Iterate requested channels and apply rules (counts-domain thresholds from spec)
            for (int i = 0; i < toCheck.Length; i++)
            {
                int ch = toCheck[i];
                if (ch < 0 || ch >= raw.Length) continue;
                double v = raw[ch];
                string laneName = LaneNameOrEmpty(ch);
                if (string.IsNullOrEmpty(laneName)) continue;

                switch (ch)
                {
                    // Channel 0: Pves (pressure)
                    case 0:
                        if (v < RAW_MIN_20)
                        {
                            //SetAlertAndMark(laneName, "Pves sensor not connected");

                            // Check if test is running to determine which message to show
                            if (IsTestRunning())
                            {
                                // In live mode, negative pressure could indicate catheter slip
                                SetAlertAndMark(laneName, "Check for Catheter slip");
                            }
                            else
                            {
                                // In demo mode, show sensor not connected
                                SetAlertAndMark(laneName, "Pves sensor not connected");
                            }

                        }
                        else if (v > RAW_PRESSURE_FAULT)
                        {
                            SetAlertAndMark(laneName, "Pves sensor faulty");
                        }
                        else if (v > RAW_MIN_SMALL && v < P0)
                        {
                            SetAlertAndMark(laneName, "Pves not flushed / extension not on holder");
                        }
                        break;

                    // Channel 1: Pabd or Pirp
                    case 1:
                        if (IsWhitaker(currenttest))
                        {
                            if (v < RAW_MIN_20)
                            {
                                SetAlertAndMark(laneName, "Pirp sensor not connected");
                            }
                            else if (v > RAW_PRESSURE_FAULT)
                            {
                                SetAlertAndMark(laneName, "Pirp sensor faulty");
                            }
                            else if (v > RAW_MIN_SMALL && v < P1)
                            {
                                SetAlertAndMark(laneName, "Pirp not flushed / extension not on holder");
                            }
                        }
                        else
                        {
                            if (v < RAW_MIN_20)
                            {
                                //SetAlertAndMark(laneName, "Pabd sensor not connected");

                                // Check if test is running to determine which message to show
                                if (IsTestRunning())
                                {
                                    // In live mode, negative pressure could indicate catheter slip
                                    SetAlertAndMark(laneName, "Check Rectal catheter slip or Balloon leaking");
                                }
                                else
                                {
                                    // In demo mode, show sensor not connected
                                    SetAlertAndMark(laneName, "Pabd sensor not connected");
                                }

                            }
                            else if (v > RAW_PRESSURE_FAULT)
                            {
                                SetAlertAndMark(laneName, "Pabd sensor faulty");
                            }
                            else if (v > RAW_MIN_SMALL && v < P1)
                            {
                                SetAlertAndMark(laneName, "Pabd not flushed / extension not on holder");
                            }
                        }
                        break;

                    // Channel 2: Qvol (uroflow)
                    case 2:
                        if (v < RAW_MIN_25)
                        {
                            SetAlertAndMark(laneName, "Connect uroflow sensor cable (or cable broken)");
                        }
                        else if (v >= JF && v < RAW_FAULT_3900)
                        {
                            //SetAlertAndMark(laneName, "Uroflow jar full—empty jar");
                            // Check if test is running to determine which message to show
                            if (IsTestRunning())
                            {
                                // In live mode, negative pressure could indicate catheter slip
                                //SetAlertAndMark(laneName, "Uroflow jar full—empty jar");
                            }
                            else
                            {
                                // In demo mode, show sensor not connected
                                SetAlertAndMark(laneName, "Uroflow jar full—empty jar");
                            }
                        }
                        else if (v >= RAW_FAULT_3900)
                        {
                            SetAlertAndMark(laneName, "Uroflow sensor faulty");
                        }
                        break;

                    // Channel 3: Vinf (infusion)
                    case 3:
                        if (v < RAW_MIN_25)
                        {
                            SetAlertAndMark(laneName, "Infusion sensor not connected");
                        }
                        else if (v > 100 && v < ChangeBottle)
                        {
                            SetAlertAndMark(laneName, "Change infusion bottle / bottle missing");

                            // 🔴 AUTO PAUSE + RESUME
                            //if (IsTestRunning() && !_autoPausedByInfusionAlert)
                            //{
                            //    _autoPausedByInfusionAlert = true;

                            //    // Pause + Resume on UI thread
                            //    BeginInvoke(new Action(() =>
                            //    {
                            //        PauseAndResumeWithPopup();
                            //    }));
                            //}


                            if (_isRecording && !_autoPausedByInfusionAlert)
                            {
                                _autoPausedByInfusionAlert = true;

                                BeginInvoke(new Action(() =>
                                {
                                    PauseAndResumeWithPopup();
                                }));
                            }
                        }
                        else if (v > RAW_FAULT_3900)
                        {
                            SetAlertAndMark(laneName, "Infusion sensor faulty / Large NS bottle");
                        }
                        else
                        {
                            // ✅ Condition back to normal → allow future auto-pause
                            if (!_autoPauseInProgress)
                                _autoPausedByInfusionAlert = false;
                        }
                        break;

                    // Channel 4: EMG
                    case 4:
                        if (v < RAW_MIN_25)
                        {
                            SetAlertAndMark(laneName, "EMG not connected");
                        }
                        else if (v > RAW_EMG_FAULT)
                        {
                            SetAlertAndMark(laneName, "EMG sensor faulty");
                        }
                        break;

                    //Start this code for UPP test "UPP ARM pulled fully—reverse it" this error show in Pura Channel wrong work for UPP Test Comment on 25-02-2026
                    // Channel 5: UPP pulled length (displayed value)
                    //case 5:
                    //    // map alert to a visible lane (prefer channel 6's lane if it exists)
                    //    string uppLane = !string.IsNullOrEmpty(LaneNameForChannelIndex(6, currenttest)) ? LaneNameForChannelIndex(6, currenttest) : laneName;
                    //    if (v < U1)
                    //    {
                    //        SetAlertAndMark(uppLane, "UPP module not connected");
                    //    }
                    //    else if (v > RAW_FAULT_3900)
                    //    {
                    //        SetAlertAndMark(uppLane, "UPP module faulty");
                    //    }
                    //    else if (v > UH && v < RAW_PRESSURE_FAULT)
                    //    {
                    //        SetAlertAndMark(uppLane, "UPP ARM pulled fully—reverse it");
                    //    }
                    //    break;
                    //END this code for UPP test "UPP ARM pulled fully—reverse it" this error show in Pura Channel wrong work for UPP Test Comment on 25-02-2026


                    //Start this code for UPP test "UPP ARM pulled fully—reverse it" this error show in Lura Channel add on 25-02-2026

                    // Channel 5: UPP pulled length (displayed value)
                    case 5:
                        string uppLane = laneName;

                        // Check if Lura is visible in current test
                        if (LaneIsShown("Lura"))
                        {
                            uppLane = "Lura";
                        }
                        else
                        {
                            // Fallback to channel 6's lane if Lura not visible
                            string channel6Lane = LaneNameForChannelIndex(6, currenttest);
                            if (!string.IsNullOrEmpty(channel6Lane))
                                uppLane = channel6Lane;
                        }

                        if (v < U1)
                        {
                            SetAlertAndMark(uppLane, "UPP module not connected");
                        }
                        else if (v > RAW_FAULT_3900)
                        {
                            SetAlertAndMark(uppLane, "UPP module faulty");
                        }
                        else if (v > UH && v < RAW_PRESSURE_FAULT)
                        {
                            SetAlertAndMark(uppLane, "UPP ARM pulled fully—reverse it");
                        }
                        break;
                    //END this code for UPP test "UPP ARM pulled fully—reverse it" this error show in Lura Channel add on 25-02-2026

                    // Channel 6: Pura (pressure)
                    case 6:
                        if (v < RAW_MIN_20)
                        {
                            SetAlertAndMark(laneName, "Pura sensor not connected");
                        }
                        else if (v > RAW_PRESSURE_FAULT)
                        {
                            SetAlertAndMark(laneName, "Pura sensor faulty");
                        }
                        else if (v > RAW_MIN_SMALL && v < P6)
                        {
                            SetAlertAndMark(laneName, "Pura not flushed / extension not on holder");
                        }
                        break;

                    default:
                        // other channels: no checks by default (add if you want)
                        break;
                }
            }

            //if (IsTestRunning()) 
            //{
            //    double negPressureThresholdCm = -10.0;

            //    // Channel 0 (Pves) negative check
            //    if (0 < raw.Length)
            //    {
            //        double pvesEng = ConvertCountsToPressureIfAvailable(raw[0], 0); // helper below will fall back
            //        if (pvesEng < negPressureThresholdCm)
            //        {
            //            SetAlertAndMark(LaneNameOrEmpty(0), "Check for Catheter slip (Pves showing large negative)");
            //        }
            //    }

            //    // Channel 1 (Pabd) negative check
            //    if (1 < raw.Length)
            //    {
            //        double pabdEng = ConvertCountsToPressureIfAvailable(raw[1], 1);
            //        if (pabdEng < negPressureThresholdCm)
            //        {
            //            SetAlertAndMark(LaneNameOrEmpty(1), "Check Rectal catheter slip or Balloon leaking (Pabd negative)");
            //        }
            //    }

            //    // Channel 6 (Pura) negative check (for UPP test / urethral slip)
            //    if (6 < raw.Length)
            //    {
            //        double puraEng = ConvertCountsToPressureIfAvailable(raw[6], 6);
            //        if (puraEng < negPressureThresholdCm)
            //        {
            //            SetAlertAndMark(LaneNameOrEmpty(6), "Check Urethral catheter slip (Pura negative)");
            //        }
            //    }
            //}

            if (IsTestRunning())
            {
                double negPressureThresholdCm = -10.0;

                // Channel 0 (Pves) negative check
                if (raw.Length > 0)
                {
                    double pvesEng = ConvertCountsToPressureIfAvailable(raw[0], 0);
                    if (pvesEng < negPressureThresholdCm)
                    {
                        SetAlertAndMark(LaneNameOrEmpty(0), "Check for Catheter slip (Pves showing large negative)");
                    }
                }

                // Channel 1 (Pabd) negative check
                if (raw.Length > 1)
                {
                    double pabdEng = ConvertCountsToPressureIfAvailable(raw[1], 1);
                    if (pabdEng < negPressureThresholdCm)
                    {
                        SetAlertAndMark(LaneNameOrEmpty(1), "Check Rectal catheter slip or Balloon leaking (Pabd negative)");
                    }
                }

                // Channel 6 (Pura) negative check
                if (raw.Length > 6)
                {
                    double puraEng = ConvertCountsToPressureIfAvailable(raw[6], 6);
                    if (puraEng < negPressureThresholdCm)
                    {
                        SetAlertAndMark(LaneNameOrEmpty(6), "Check Urethral catheter slip (Pura negative)");
                    }
                }

            }


            // update halt plotting flag so AppendSample/recording can skip when an alert exists
            _haltPlottingDueToAlert = hasAlerts;
        }


        // ---------- Helpers used above ----------

        // If you already have a counts->engineering-unit converter, call it.
        // This helper will try to call your converter if present, otherwise returns the raw value
        // (useful if your raw[] already carries engineering units).
        private double ConvertCountsToPressureIfAvailable(double counts, int channel)
        {
            // If your project has a conversion function, use it here. Example:
            // return CountsToCmH2O(counts, channel);
            //
            // Otherwise assume 'counts' is already in engineering units (cmH2O) and return it.
            // Adjust this function to your real conversion routine.
            try
            {
                if (this.GetType().GetMethod("CountsToCmH2O", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) != null)
                {
                    // dynamic invocation to avoid compile error if method doesn't exist in this class
                    var mi = this.GetType().GetMethod("CountsToCmH2O", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (mi != null)
                    {
                        object result = mi.Invoke(this, new object[] { counts, channel });
                        if (result is double d) return d;
                    }
                }
            }
            catch
            {
                // ignore and fall back
            }
            // fallback - no conversion available
            return counts;
        }

        // Replace this with your actual test-running flag (name used in your code).
        private bool IsTestRunning()
        {
            // example: if you have a flag named _isRecording or _isTestActive use that instead
            if (this.GetType().GetField("_isRecording", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) != null)
            {
                var fld = this.GetType().GetField("_isRecording", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (fld != null && fld.GetValue(this) is bool b) return b;
            }

            // fallback default: false
            return false;
        }
        //End Error Code For Bhusahn Change on 05/12/2025

        private bool ValidatePreTestConditions()
        {
            // System.Diagnostics.Debug.WriteLine($"ValidatePreTestConditions called at {DateTime.Now:HH:mm:ss.fff}");
            // System.Diagnostics.Debug.WriteLine($"_lastRawCounts is null: {_lastRawCounts == null}");
            // System.Diagnostics.Debug.WriteLine($"_lastRawCounts Length: {_lastRawCounts?.Length ?? 0}");
            // No hardware snapshot yet
            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
            {
                // Try to get data by waiting briefly
                int attempts = 0;
                while ((_lastRawCounts == null || _lastRawCounts.Length == 0) && attempts < 10)
                {
                    System.Threading.Thread.Sleep(100);
                    Application.DoEvents(); // Let events fire
                    attempts++;
                }

                // Still no data?
                if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                {
                    MessageBox.Show(
                        "No live data is available from the hardware yet.\nPlease wait a few seconds and ensure the device is connected.",
                        "Cannot Start Test",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }

            int[] channels = GetInputChannelsToCheckForTest(currenttest);
            if (channels == null || channels.Length == 0)
                return true; // nothing special to validate

            foreach (int ch in channels)
            {
                if (ch < 0 || ch >= _lastRawCounts.Length)
                    continue;

                double v = _lastRawCounts[ch];

                switch (ch)
                {
                    // 0: PVES
                    case 0:
                        if (v < RAW_MIN_20)
                        {
                            MessageBox.Show("Pves sensor not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        if (v < P0) // not flushed
                        {
                            MessageBox.Show("Pves not flushed properly. Please flush before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;

                    // 1: PABD / PIRP
                    case 1:
                        if (v < RAW_MIN_20)
                        {
                            MessageBox.Show("Pabd sensor not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        if (v < P1)
                        {
                            MessageBox.Show("Pabd not flushed properly. Please flush before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;

                    // 6: PURA
                    case 6:
                        if (v < RAW_MIN_20)
                        {
                            MessageBox.Show("Pura sensor not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        if (v < P6)
                        {
                            MessageBox.Show("Pura not flushed properly. Please flush before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;

                    // 2: QVOL (uroflow)
                    case 2:
                        if (v < RAW_MIN_25)
                        {
                            MessageBox.Show("Uroflow sensor not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;

                    // 3: VINF (infusion)
                    case 3:
                        if (v < RAW_MIN_25)
                        {
                            MessageBox.Show("Infusion sensor not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;

                    // 4: EMG
                    //case 4:
                    //    if (v < RAW_MIN_25)
                    //    {
                    //        MessageBox.Show("EMG not connected. Please connect EMG before starting the test.",
                    //            "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //        return false;
                    //    }
                    //    break;

                    // 5: UPP LENGTH
                    case 5:
                        if (v < U1)
                        {
                            MessageBox.Show("UPP module not connected. Please connect before starting the test.",
                                "Cannot Start Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        break;
                }
            }

            return true;
        }


        //Start Error Code For Pratik Sir comment on 05/12/2025

        //private void RunLiveSystemChecks_RAW(double[] raw)
        //{
        //    if (_liveChart == null) return;
        //    _liveChart.ClearAllAlerts();

        //    if (raw == null || raw.Length == 0) return;

        //    // Which NI channels must be OK for this test?
        //    int[] toCheck = GetInputChannelsToCheckForTest(currenttest);
        //    bool hasAlerts = false;

        //    for (int i = 0; i < toCheck.Length; i++)
        //    {
        //        int ch = toCheck[i];
        //        if (ch < 0 || ch >= raw.Length) continue;

        //        double v = raw[ch];
        //        string laneName = LaneNameForChannelIndex(ch, currenttest);
        //        if (string.IsNullOrEmpty(laneName)) continue; // if not displayed, skip

        //        // Per-channel rules (counts-domain as per spec)
        //        switch (ch)
        //        {
        //            // 0: Pves (pressure)
        //            case 0:
        //                if (v < RAW_MIN_20) { _liveChart.SetLaneAlert(laneName, "Pves sensor not connected"); }
        //                else if (v > RAW_PRESSURE_FAULT) { _liveChart.SetLaneAlert(laneName, "Pves sensor faulty"); }
        //                else if (v > RAW_MIN_SMALL && v < P0) { _liveChart.SetLaneAlert(laneName, "Pves not flushed / extension not on holder"); }
        //                break;

        //            // 1: Pabd or Pirp (pressure)
        //            case 1:
        //                if (IsWhitaker(currenttest))
        //                {
        //                    if (v < RAW_MIN_20) { _liveChart.SetLaneAlert(laneName, "Pirp sensor not connected"); }
        //                    else if (v > RAW_PRESSURE_FAULT) { _liveChart.SetLaneAlert(laneName, "Pirp sensor faulty"); }
        //                    else if (v > RAW_MIN_SMALL && v < P1) { _liveChart.SetLaneAlert(laneName, "Pirp not flushed / extension not on holder"); }
        //                }
        //                else
        //                {
        //                    if (v < RAW_MIN_20) { _liveChart.SetLaneAlert(laneName, "Pabd sensor not connected"); }
        //                    else if (v > RAW_PRESSURE_FAULT) { _liveChart.SetLaneAlert(laneName, "Pabd sensor faulty"); }
        //                    else if (v > RAW_MIN_SMALL && v < P1) { _liveChart.SetLaneAlert(laneName, "Pabd not flushed / extension not on holder"); }
        //                }
        //                break;

        //            // 2: Qvol (uroflow)
        //            case 2:
        //                if (v < RAW_MIN_25) { _liveChart.SetLaneAlert(laneName, "Connect uroflow sensor cable (or cable broken)"); }
        //                else if (v >= JF && v < RAW_FAULT_3900) { _liveChart.SetLaneAlert(laneName, "Uroflow jar full—empty jar"); }
        //                else if (v >= RAW_FAULT_3900) { _liveChart.SetLaneAlert(laneName, "Uroflow sensor faulty"); }
        //                break;

        //            // 3: Vinf (infusion)
        //            case 3:
        //                if (v < RAW_MIN_25) { _liveChart.SetLaneAlert(laneName, "Infusion sensor not connected"); }
        //                else if (v > 100 && v < ChangeBottle) { _liveChart.SetLaneAlert(laneName, "Change infusion bottle / bottle missing"); }
        //                else if (v > RAW_FAULT_3900) { _liveChart.SetLaneAlert(laneName, "Infusion sensor faulty / Large NS bottle"); }
        //                break;

        //            // 4: EMG
        //            case 4:
        //                if (v < RAW_MIN_25) { _liveChart.SetLaneAlert(laneName, "EMG not connected"); }
        //                else if (v > RAW_EMG_FAULT) { _liveChart.SetLaneAlert(laneName, "EMG sensor faulty"); }
        //                break;

        //            // 5: UPP pulled length (displayed value; still alert on UPP-related lane)
        //            case 5:
        //                // Map alert to a visible lane (usually Pura/Pclo area or a generic UPP lane label if you use one)
        //                string uppLane = !string.IsNullOrEmpty(LaneNameForChannelIndex(6, currenttest)) ? LaneNameForChannelIndex(6, currenttest) : laneName;
        //                if (v < U1) { _liveChart.SetLaneAlert(uppLane, "UPP module not connected"); }
        //                else if (v > RAW_FAULT_3900) { _liveChart.SetLaneAlert(uppLane, "UPP module faulty"); }
        //                else if (v > UH && v < RAW_PRESSURE_FAULT) { _liveChart.SetLaneAlert(uppLane, "UPP ARM pulled fully—reverse it"); }
        //                break;

        //            // 6: Pura (pressure)
        //            case 6:
        //                if (v < RAW_MIN_20) { _liveChart.SetLaneAlert(laneName, "Pura sensor not connected"); }
        //                else if (v > RAW_PRESSURE_FAULT) { _liveChart.SetLaneAlert(laneName, "Pura sensor faulty"); }
        //                else if (v > RAW_MIN_SMALL && v < P6) { _liveChart.SetLaneAlert(laneName, "Pura not flushed / extension not on holder"); }
        //                break;
        //        }
        //    }
        //   //  _haltPlottingDueToAlert = hasAlerts;
        //}
        //End Error Code For Pratik Sir comment on 05/12/2025

        private bool LaneIsShown(string laneName)
        {
            if (_currentTestDef == null || _currentTestDef.Lanes == null) return false;
            for (int i = 0; i < _currentTestDef.Lanes.Count; i++)
                if (string.Equals(_currentTestDef.Lanes[i].Name, laneName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void RunLiveSystemChecks(double[] eng)
        {
            if (_liveChart == null) return;

            // Clear all previous alerts each frame; advisories are cleared per-lane when an alert is set
            _liveChart.ClearAllAlerts();

            // No data -> no alerts and no halt
            if (eng == null || eng.Length == 0)
            {
                _haltPlottingDueToAlert = false;
                return;
            }

            bool hasAlerts = false;
            int len = eng.Length;

            // ===== PVES (index 0) =====
            if (len > 0 && LaneIsShown("Pves"))
            {
                double v = eng[0];
                if (Math.Abs(v) < PRESSURE_CONN_CM)
                {
                    _liveChart.SetLaneAlert("Pves", "Pves sensor not connected");
                    _liveChart.ClearLaneAdvisory("Pves"); if (_advisoryStartT.ContainsKey("Pves")) _advisoryStartT.Remove("Pves");
                    hasAlerts = true;
                }
                else if (v > PRESSURE_MAX_CM)
                {
                    _liveChart.SetLaneAlert("Pves", "Pves sensor faulty");
                    _liveChart.ClearLaneAdvisory("Pves"); if (_advisoryStartT.ContainsKey("Pves")) _advisoryStartT.Remove("Pves");
                    hasAlerts = true;
                }
                else if (v < PRESSURE_FLUSH_CM)
                {
                    _liveChart.SetLaneAlert("Pves", "Pves not flushed / extension not on holder");
                    _liveChart.ClearLaneAdvisory("Pves"); if (_advisoryStartT.ContainsKey("Pves")) _advisoryStartT.Remove("Pves");
                    hasAlerts = true;
                }
            }

            // ===== PABD / PIRP (index 1) =====
            // Use whichever lane name your UI shows for this test
            if (len > 1)
            {
                // Prefer exact lane name if shown
                bool checkPabd = LaneIsShown("Pabd");
                bool checkPirp = !checkPabd && LaneIsShown("Pirp"); // Whitaker

                if (checkPabd || checkPirp)
                {
                    string lane = checkPabd ? "Pabd" : "Pirp";
                    double v = eng[1];

                    if (Math.Abs(v) < PRESSURE_CONN_CM)
                    {
                        _liveChart.SetLaneAlert(lane, lane + " sensor not connected");
                        _liveChart.ClearLaneAdvisory(lane); if (_advisoryStartT.ContainsKey(lane)) _advisoryStartT.Remove(lane);
                        hasAlerts = true;
                    }
                    else if (v > PRESSURE_MAX_CM)
                    {
                        _liveChart.SetLaneAlert(lane, lane + " sensor faulty");
                        _liveChart.ClearLaneAdvisory(lane); if (_advisoryStartT.ContainsKey(lane)) _advisoryStartT.Remove(lane);
                        hasAlerts = true;
                    }
                    else if (v < PRESSURE_FLUSH_CM)
                    {
                        _liveChart.SetLaneAlert(lane, lane + " not flushed / extension not on holder");
                        _liveChart.ClearLaneAdvisory(lane); if (_advisoryStartT.ContainsKey(lane)) _advisoryStartT.Remove(lane);
                        hasAlerts = true;
                    }
                }
            }

            // ===== VINF (index 3, ml) =====
            // Being ~0 ml before pump starts is NORMAL → only flag extreme faults
            if (len > 3 && LaneIsShown("Vinf"))
            {
                double v = eng[3];
                if (v < -5.0 || v > 5000.0)
                {
                    _liveChart.SetLaneAlert("Vinf", "Infusion sensor faulty");
                    _liveChart.ClearLaneAdvisory("Vinf"); if (_advisoryStartT.ContainsKey("Vinf")) _advisoryStartT.Remove("Vinf");
                    hasAlerts = true;
                }
            }

            // ===== QVOL (index 4, ml) =====
            if (len > 4 && LaneIsShown("Qvol"))
            {
                double v = eng[4];
                if (v >= QVOL_JAR_FULL_ML)
                {
                    _liveChart.SetLaneAlert("Qvol", "Uroflow jar full — empty jar");
                    _liveChart.ClearLaneAdvisory("Qvol"); if (_advisoryStartT.ContainsKey("Qvol")) _advisoryStartT.Remove("Qvol");
                    hasAlerts = true;
                }
            }

            // ===== EMG (index 6) =====
            if (len > 6 && LaneIsShown("EMG"))
            {
                double v = eng[6];
                if (v < 0.0 || v > 5000.0)
                {
                    _liveChart.SetLaneAlert("EMG", "EMG sensor faulty");
                    _liveChart.ClearLaneAdvisory("EMG"); if (_advisoryStartT.ContainsKey("EMG")) _advisoryStartT.Remove("EMG");
                    hasAlerts = true;
                }
            }

            // If you have PURA in engineering frames, add similar block here and guard by index and LaneIsShown("Pura").

            // Final: freeze plotting if any alerts active
            _haltPlottingDueToAlert = hasAlerts;
        }

        private static bool IsWhitaker(string testName)
        {
            return !string.IsNullOrEmpty(testName) &&
                   testName.IndexOf("Whitaker", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int[] GetInputChannelsToCheckForTest(string test)
        {
            // Channels are NI indices:
            // 0=Pves, 1=Pabd/Pirp, 2=Qvol, 3=Vinf, 4=EMG, 5=UPP length, 6=Pura
            if (string.IsNullOrEmpty(test)) return new int[0];

            if (test.IndexOf("Uroflowmetry + EMG", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 2, 4 };
            if (test.IndexOf("Uroflowmetry", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 2 };
            if (test.IndexOf("Cystometry", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 3 };
            if (test.IndexOf("Pressure flow + EMG", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 2, 3, 4 };
            if (test.IndexOf("Pressure flow", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 2, 3 };
            if (test.IndexOf("UPP", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 5, 6 };
            if (test.IndexOf("Whitaker", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1 };
            if (test.IndexOf("Biofeedback", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 4 };
            if (test.IndexOf("Anal Manometry", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 6 };
            // Pressure Flow + Video, Pressure Flow + EMG + Video: same as PF/PF+EMG (+ video card check if you add one)
            if (test.IndexOf("EMG + Video", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 2, 3, 4 };
            if (test.IndexOf(" + Video", StringComparison.OrdinalIgnoreCase) >= 0) return new int[] { 0, 1, 2, 3 };

            return new int[0];
        }
        private string LaneNameForChannelIndex(int ch, string test)
        {
            // Match the labels you actually assign in SetChannels(...)
            switch (ch)
            {
                case 0: return "Pves";
                case 1: return IsWhitaker(test) ? "Pirp" : "Pabd";
                case 2: return "Qvol";
                case 3: return "Vinf";
                case 4: return "EMG";
                case 5: return "Urethra length"; // if not plotted, we’ll map its alerts to Pura below
                case 6: return "Pura";
            }
            return null;
        }


        private string SuggestNextScaleText(string laneName, double currentMax)
        {
            double[] opts;
            if (_scaleOptions.TryGetValue(laneName, out opts) && opts != null && opts.Length > 0)
            {
                // find the smallest option strictly greater than currentMax
                double candidate = 0;
                bool found = false;
                int i;
                for (i = 0; i < opts.Length; i++)
                {
                    if (opts[i] > currentMax)
                    {
                        candidate = opts[i];
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    string unit = "";
                    _unitMap.TryGetValue(laneName, out unit);
                    return (unit == null || unit.Length == 0)
                            ? ("Consider 0 - " + candidate.ToString())
                            : ("Consider 0 - " + candidate.ToString() + " " + unit);
                }
            }
            return "Near scale limit";
        }

        private void UpdateScaleAdvisories(double[] vals, double tNow)
        {
            if (_currentTestDef == null || _currentTestDef.Lanes == null) return;
            if (vals == null) return;

            int n = Math.Min(vals.Length, _currentTestDef.Lanes.Count);
            int i;

            for (i = 0; i < n; i++)
            {
                string laneName = _currentTestDef.Lanes[i].Name;
                if (string.IsNullOrEmpty(laneName)) continue;

                double max = _liveChart.GetChannelScaleMaxByName(laneName);
                if (max <= 0) { _liveChart.ClearLaneAdvisory(laneName); if (_advisoryStartT.ContainsKey(laneName)) _advisoryStartT.Remove(laneName); continue; }

                double v = vals[i];
                double threshold = SCALE_ADVISORY_FRAC * max;

                if (v >= threshold)
                {
                    // started exceeding?
                    if (!_advisoryStartT.ContainsKey(laneName))
                    {
                        _advisoryStartT[laneName] = tNow;
                    }
                    else
                    {
                        double tStart = _advisoryStartT[laneName];
                        if (tNow - tStart >= SCALE_ADVISORY_HOLD_S)
                        {
                            string msg = SuggestNextScaleText(laneName, max);
                            _liveChart.SetLaneAdvisory(laneName, msg);
                        }
                    }
                }
                else
                {
                    // below threshold → clear advisory and timer
                    _liveChart.ClearLaneAdvisory(laneName);
                    if (_advisoryStartT.ContainsKey(laneName)) _advisoryStartT.Remove(laneName);
                }
            }
        }


        //private void BuildScaleMaxOverlays()
        //{
        //    if (_liveChart == null) return;

        //    int n = _liveChart.GetChannelCount();
        //    int i;

        //    // Create/refresh combos for current lanes
        //    //for (i = 0; i < n; i++)
        //    //{
        //    //    string lane = _liveChart.GetChannelNameAt(i);
        //    //    if (string.IsNullOrEmpty(lane)) continue;

        //    //    ComboBox cb;
        //    //    if (!_scaleCombos.TryGetValue(lane, out cb))
        //    //    {
        //    //        cb = new ComboBox();
        //    //        cb.Name = "cmbScaleMax_" + lane;
        //    //        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        //    //        cb.FlatStyle = FlatStyle.Popup;
        //    //        cb.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        //    //        cb.BackColor = Color.White;
        //    //        cb.ForeColor = Color.Black;
        //    //        cb.Tag = lane;
        //    //        cb.DropDownWidth = MeasureComboDropDownWidth(cb);

        //    //        // Fill options from your PDF map
        //    //        double[] opts;
        //    //        if (_scaleOptions.TryGetValue(lane, out opts) && opts != null && opts.Length > 0)
        //    //        {
        //    //            string unit = "";
        //    //            //_unitMap.TryGetValue(lane, out unit);
        //    //            int k;
        //    //            for (k = 0; k < opts.Length; k++)
        //    //            {
        //    //                string display = (unit == null || unit.Length == 0)
        //    //                    ? opts[k].ToString()
        //    //                    : opts[k].ToString();
        //    //                cb.Items.Add(display);
        //    //            }

        //    //            double currentMax = _liveChart.GetChannelScaleMaxByName(lane);
        //    //            int sel = 0; double best = double.MaxValue;
        //    //            for (k = 0; k < opts.Length; k++)
        //    //            {
        //    //                double d = Math.Abs(opts[k] - currentMax);
        //    //                if (d < best) { best = d; sel = k; }
        //    //            }
        //    //            cb.SelectedIndex = sel;
        //    //        }
        //    //        else
        //    //        {
        //    //            double cur = _liveChart.GetChannelScaleMaxByName(lane);
        //    //            cb.Items.Add( cur.ToString());
        //    //            cb.SelectedIndex = 0;
        //    //        }

        //    //        cb.SelectedIndexChanged += new EventHandler(OnScaleMaxChanged);

        //    //        // add on top of chart
        //    //        _liveChart.Controls.Add(cb);
        //    //        cb.BringToFront();
        //    //        _scaleCombos[lane] = cb;
        //    //    }
        //    //}

        //    for (i = 0; i < n; i++)
        //    {
        //        string lane = _liveChart.GetChannelNameAt(i);
        //        if (string.IsNullOrEmpty(lane)) continue;

        //        ComboBox cb;

        //        // --- NEW FIX BELOW ---
        //        if (_scaleCombos.TryGetValue(lane, out cb))
        //        {
        //            // Check if control was destroyed or removed
        //            if (cb == null || cb.IsDisposed || cb.Parent != _liveChart)
        //            {
        //                _scaleCombos.Remove(lane);
        //                cb = null;
        //            }
        //        }
        //        // --- END FIX ---

        //        if (cb == null)
        //        {
        //            cb = new ComboBox();
        //            cb.Name = "cmbScaleMax_" + lane;
        //            cb.DropDownStyle = ComboBoxStyle.DropDownList;
        //            cb.FlatStyle = FlatStyle.Popup;
        //            cb.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        //            cb.BackColor = Color.White;
        //            cb.ForeColor = Color.Black;
        //            cb.Tag = lane;
        //            cb.DropDownWidth = MeasureComboDropDownWidth(cb);

        //            // Fill options
        //            double[] opts;
        //            if (_scaleOptions.TryGetValue(lane, out opts) && opts != null && opts.Length > 0)
        //            {
        //                string unit = "";
        //                int k;
        //                for (k = 0; k < opts.Length; k++)
        //                {
        //                    string display = opts[k].ToString();
        //                    cb.Items.Add(display);
        //                }

        //                double currentMax = _liveChart.GetChannelScaleMaxByName(lane);
        //                int sel = 0; double best = double.MaxValue;
        //                for (k = 0; k < opts.Length; k++)
        //                {
        //                    double d = Math.Abs(opts[k] - currentMax);
        //                    if (d < best) { best = d; sel = k; }
        //                }
        //                cb.SelectedIndex = sel;
        //            }
        //            else
        //            {
        //                double cur = _liveChart.GetChannelScaleMaxByName(lane);
        //                cb.Items.Add(cur.ToString());
        //                cb.SelectedIndex = 0;
        //            }

        //            cb.SelectedIndexChanged += new EventHandler(OnScaleMaxChanged);

        //            _liveChart.Controls.Add(cb);
        //            cb.BringToFront();
        //            _scaleCombos[lane] = cb;
        //        }
        //    }


        //    // Remove combos for lanes that disappeared (e.g., fewer channels test)
        //    List<string> toRemove = new List<string>();
        //    foreach (KeyValuePair<string, ComboBox> kv in _scaleCombos)
        //    {
        //        bool stillExists = false;
        //        for (i = 0; i < n; i++)
        //        {
        //            string nm = _liveChart.GetChannelNameAt(i);
        //            if (string.Equals(nm, kv.Key, StringComparison.OrdinalIgnoreCase))
        //            { stillExists = true; break; }
        //        }
        //        if (!stillExists) toRemove.Add(kv.Key);
        //    }
        //    for (i = 0; i < toRemove.Count; i++)
        //    {
        //        ComboBox gone;
        //        if (_scaleCombos.TryGetValue(toRemove[i], out gone))
        //        {
        //            if (gone.Parent != null) gone.Parent.Controls.Remove(gone);
        //            gone.Dispose();
        //            _scaleCombos.Remove(toRemove[i]);
        //        }
        //    }

        //    RepositionScaleCombos(); // final place
        //}


        private void BuildScaleMaxOverlays()
        {
            if (_liveChart == null) return;

            int n = _liveChart.GetChannelCount();
            int i;

            for (i = 0; i < n; i++)
            {
                string lane = _liveChart.GetChannelNameAt(i);
                if (string.IsNullOrEmpty(lane)) continue;

                ComboBox cb;

                // --- existing combo reuse check ---
                if (_scaleCombos.TryGetValue(lane, out cb))
                {
                    if (cb == null || cb.IsDisposed || cb.Parent != _liveChart)
                    {
                        _scaleCombos.Remove(lane);
                        cb = null;
                    }
                }

                if (cb == null)
                {
                    cb = new ComboBox();
                    cb.Name = "cmbScaleMax_" + lane;
                    cb.DropDownStyle = ComboBoxStyle.DropDownList;
                    cb.FlatStyle = FlatStyle.Popup;
                    cb.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    cb.BackColor = Color.White;
                    cb.ForeColor = Color.Black;
                    cb.Tag = lane;

                    double currentMax = _liveChart.GetChannelScaleMaxByName(lane);

                    // ✅ Fill available scale options
                    double[] opts;
                    if (_scaleOptions.TryGetValue(lane, out opts) && opts != null && opts.Length > 0)
                    {
                        // Add all predefined options
                        foreach (double opt in opts)
                            cb.Items.Add(opt.ToString());

                        // ✅ If saved (current) scale not in list, add it
                        if (!opts.Contains(currentMax))
                            cb.Items.Add(currentMax.ToString());

                        // ✅ Find the best match index
                        int sel = 0;
                        double best = double.MaxValue;
                        for (int k = 0; k < cb.Items.Count; k++)
                        {
                            if (double.TryParse(cb.Items[k].ToString(), out double val))
                            {
                                double diff = Math.Abs(val - currentMax);
                                if (diff < best)
                                {
                                    best = diff;
                                    sel = k;
                                }
                            }
                        }
                        cb.SelectedIndex = sel;
                    }
                    else
                    {
                        // Fallback: only current value available
                        cb.Items.Add(currentMax.ToString());
                        cb.SelectedIndex = 0;
                    }

                    // Optional: adjust dropdown width to fit largest text
                    cb.DropDownWidth = MeasureComboDropDownWidth(cb);

                    // --- Event ---
                    cb.SelectedIndexChanged += new EventHandler(OnScaleMaxChanged);

                    _liveChart.Controls.Add(cb);
                    cb.BringToFront();
                    _scaleCombos[lane] = cb;
                }
            }

            // Remove combos for disappeared lanes
            List<string> toRemove = new List<string>();
            foreach (var kv in _scaleCombos)
            {
                bool stillExists = false;
                for (i = 0; i < n; i++)
                {
                    string nm = _liveChart.GetChannelNameAt(i);
                    if (string.Equals(nm, kv.Key, StringComparison.OrdinalIgnoreCase))
                    { stillExists = true; break; }
                }
                if (!stillExists) toRemove.Add(kv.Key);
            }
            foreach (var laneKey in toRemove)
            {
                if (_scaleCombos.TryGetValue(laneKey, out ComboBox gone))
                {
                    if (gone.Parent != null) gone.Parent.Controls.Remove(gone);
                    gone.Dispose();
                    _scaleCombos.Remove(laneKey);
                }
            }

            RepositionScaleCombos();
        }



        private void RepositionScaleCombos()
        {
            if (_liveChart == null) return;

            int n = _liveChart.GetChannelCount();
            for (int i = 0; i < n; i++)
            {
                string lane = _liveChart.GetChannelNameAt(i);
                ComboBox cb;
                if (string.IsNullOrEmpty(lane)) continue;
                if (!_scaleCombos.TryGetValue(lane, out cb)) continue;

                Rectangle r = _liveChart.GetScaleMaxOverlayBounds(i);

                // widen: base right-band width + extra, but at least the minimum
                //int w = r.Width + _scaleComboExtraWidth;
                //if (w < _scaleComboMinWidth) w = _scaleComboMinWidth;

                int w = 50;

                // keep inside the chart
                int x = r.X;
                if (x + w > _liveChart.ClientSize.Width - 4) x = _liveChart.ClientSize.Width - w - 4;

                cb.SetBounds(x, r.Y, w, r.Height);
                cb.Visible = _liveChart.Visible;
                cb.BringToFront();
            }
        }

        private int MeasureComboDropDownWidth(ComboBox cb)
        {
            int max = cb.Width;
            try
            {
                using (Graphics g = cb.CreateGraphics())
                {
                    int i;
                    for (i = 0; i < cb.Items.Count; i++)
                    {
                        string s = cb.GetItemText(cb.Items[i]);
                        SizeF sz = g.MeasureString(s, cb.Font);
                        int w = (int)Math.Ceiling(sz.Width) + SystemInformation.VerticalScrollBarWidth + 18;
                        if (w > max) max = w;
                    }
                }
            }
            catch { }
            return max;
        }

        private void WireScaleOverlayEvents()
        {
            if (_liveChart == null) return;

            _liveChart.Resize += new EventHandler(OnChartResizedOrShown);
            _liveChart.VisibleChanged += new EventHandler(OnChartResizedOrShown);
            _liveChart.ChartLayoutChanged += new EventHandler(OnChartLayoutChanged);
            _liveChart.HandleDestroyed += new EventHandler(OnChartHandleDestroyed);
            _liveChart.HandleCreated += new EventHandler(OnChartHandleCreated);
        }

        private void OnChartResizedOrShown(object sender, EventArgs e)
        {
            RepositionScaleCombos();
        }

        private void OnChartLayoutChanged(object sender, EventArgs e)
        {
            // geometry changed: rebuild (adds/removes as needed) then reposition
            BuildScaleMaxOverlays();
        }

        private void OnChartHandleDestroyed(object sender, EventArgs e)
        {
            ClearScaleCombos(); // parent is going away
        }

        private void OnChartHandleCreated(object sender, EventArgs e)
        {
            // chart resurrected → rebuild overlays
            BuildScaleMaxOverlays();
        }

        private void ClearScaleCombos()
        {
            foreach (var kv in _scaleCombos)
            {
                if (kv.Value != null && !kv.Value.IsDisposed)
                {
                    if (kv.Value.Parent != null)
                        kv.Value.Parent.Controls.Remove(kv.Value);
                    kv.Value.Dispose();
                }
            }
            _scaleCombos.Clear();
        }


        //private void ClearScaleCombos()
        //{
        //    foreach (KeyValuePair<string, ComboBox> kv in _scaleCombos)
        //    {
        //        ComboBox cb = kv.Value;
        //        if (cb != null)
        //        {
        //            if (cb.Parent != null) cb.Parent.Controls.Remove(cb);
        //            cb.Dispose();
        //        }
        //    }
        //    _scaleCombos.Clear();
        //}

        private sealed class ScaleMaxLabelTarget
        {
            public Label Label;
            public string Lane;
            public ScaleMaxLabelTarget(Label lbl, string lane) { Label = lbl; Lane = lane; }
        }

        private bool LaneIsInCurrentTest(string lane)
        {
            if (_currentTestDef == null || _currentTestDef.Lanes == null) return false;
            for (int i = 0; i < _currentTestDef.Lanes.Count; i++)
                if (string.Equals(_currentTestDef.Lanes[i].Name, lane, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private string ExtractLaneNameFromLabel(Label lbl)
        {
            // Preferred: Tag = "ScaleMax:<Lane>"
            if (lbl.Tag is string)
            {
                string tag = (string)lbl.Tag;
                if (!string.IsNullOrEmpty(tag) &&
                    tag.StartsWith("ScaleMax:", StringComparison.OrdinalIgnoreCase))
                {
                    return tag.Substring(9).Trim();
                }
            }

            // Next: Name = "lblScaleMax_<Lane>"
            if (!string.IsNullOrEmpty(lbl.Name) &&
                lbl.Name.StartsWith("lblScaleMax_", StringComparison.OrdinalIgnoreCase))
            {
                return lbl.Name.Substring("lblScaleMax_".Length).Trim();
            }

            // Last resort: detect lane name inside label.Text
            if (_currentTestDef != null && _currentTestDef.Lanes != null)
            {
                for (int i = 0; i < _currentTestDef.Lanes.Count; i++)
                {
                    string lane = _currentTestDef.Lanes[i].Name;
                    if (!string.IsNullOrEmpty(lane) &&
                        lbl.Text.IndexOf(lane, StringComparison.OrdinalIgnoreCase) >= 0)
                        return lane;
                }
            }
            return null;
        }

        private void CollectScaleMaxLabels(Control root, List<ScaleMaxLabelTarget> outList)
        {
            if (root == null) return;

            for (int i = 0; i < root.Controls.Count; i++)
            {
                Control c = root.Controls[i];

                Label lbl = c as Label;
                if (lbl != null && lbl.Visible)
                {
                    string lane = ExtractLaneNameFromLabel(lbl);
                    if (!string.IsNullOrEmpty(lane) && LaneIsInCurrentTest(lane))
                    {
                        outList.Add(new ScaleMaxLabelTarget(lbl, lane));
                    }
                }

                if (c.HasChildren) CollectScaleMaxLabels(c, outList);
            }
        }


        private void SetupScaleMaxDropdownsForCurrentTest()
        {
            if (_currentTestDef == null || _currentTestDef.Lanes == null) return;

            // Find all eligible labels under the whole form (panels included)
            List<ScaleMaxLabelTarget> targets = new List<ScaleMaxLabelTarget>();
            CollectScaleMaxLabels(this, targets);

            for (int i = 0; i < targets.Count; i++)
            {
                Label lbl = targets[i].Label;
                string laneName = targets[i].Lane;

                // If we already created a combo for this lane, skip
                string comboName = "cmbScaleMax_" + laneName;
                Control[] existing = (lbl.Parent != null)
                    ? lbl.Parent.Controls.Find(comboName, false)
                    : this.Controls.Find(comboName, true);
                if (existing != null && existing.Length > 0) continue;

                // Build a dropdown at EXACT same bounds in the SAME parent
                ComboBox cb = new ComboBox();
                cb.Name = comboName;
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
                cb.Bounds = lbl.Bounds;
                cb.Tag = laneName;

                // Copy look from label (best-effort)
                cb.FlatStyle = FlatStyle.Popup;
                cb.Font = lbl.Font;
                cb.ForeColor = lbl.ForeColor;
                cb.BackColor = lbl.BackColor;
                cb.Anchor = lbl.Anchor;
                cb.Margin = lbl.Margin;
                cb.Padding = lbl.Padding;
                cb.TabStop = lbl.TabStop;
                cb.DropDownWidth = MeasureComboDropDownWidth(cb);

                // Fill options
                double[] opts;
                if (_scaleOptions.TryGetValue(laneName, out opts) && opts != null && opts.Length > 0)
                {
                    string unit = "";
                    // _unitMap.TryGetValue(laneName, out unit);

                    for (int k = 0; k < opts.Length; k++)
                    {
                        string display = (unit == null || unit.Length == 0)
                            ? opts[k].ToString()
                            : opts[k].ToString();
                        cb.Items.Add(display);
                    }

                    // preselect closest to current chart max
                    double currentMax = _liveChart.GetChannelScaleMaxByName(laneName);
                    int sel = 0;
                    double bestDiff = double.MaxValue;
                    for (int k = 0; k < opts.Length; k++)
                    {
                        double d = Math.Abs(opts[k] - currentMax);
                        if (d < bestDiff) { bestDiff = d; sel = k; }
                    }
                    cb.SelectedIndex = sel;
                }
                else
                {
                    double cur = _liveChart.GetChannelScaleMaxByName(laneName);
                    cb.Items.Add(cur.ToString());
                    cb.SelectedIndex = 0;
                }

                cb.SelectedIndexChanged += new EventHandler(OnScaleMaxChanged);

                // Hide original label and add the combo to the same parent
                lbl.Visible = false;
                if (lbl.Parent != null) lbl.Parent.Controls.Add(cb);
                else this.Controls.Add(cb);
                cb.BringToFront();
            }
        }

        private void OnScaleMaxChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;
            string laneName = cb.Tag as string;
            if (string.IsNullOrEmpty(laneName)) return;

            // Parse "0 - <max> [unit]" to get max
            string text = cb.SelectedItem as string;
            if (string.IsNullOrEmpty(text)) return;

            // extract last number
            string digits = "";
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char ch = text[i];
                if ((ch >= '0' && ch <= '9') || ch == '.')
                    digits = ch + digits;
                else if (digits.Length > 0) break;
            }

            double newMax;
            if (!double.TryParse(digits, System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out newMax))
                return;

            _liveChart.SetChannelScaleByName(laneName, 0, newMax);

            // Start Code For Change First Three ComboBox Value if Change "PVES" Channel ComboBox on 09/10/2025 At 10:54 Night  
            string[] pressureGroup = { "Pves", "Pabd", "Pirp", "Pura", "Pdet", "Pclo", "Prpg" };

            // if this changed lane is part of the pressure group
            if (pressureGroup.Contains(laneName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (string lane in pressureGroup)
                {
                    if (string.Equals(lane, laneName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (_scaleCombos.TryGetValue(lane, out ComboBox otherCb) && otherCb != null && !otherCb.IsDisposed)
                    {
                        // Find the matching item in that combo
                        for (int i = 0; i < otherCb.Items.Count; i++)
                        {
                            string s = otherCb.Items[i].ToString();
                            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double val) && val == newMax)
                            {
                                otherCb.SelectedIndexChanged -= OnScaleMaxChanged;
                                otherCb.SelectedIndex = i;
                                otherCb.SelectedIndexChanged += OnScaleMaxChanged;
                                break;
                            }
                        }

                        // Update chart scale for that lane
                        _liveChart.SetChannelScaleByName(lane, 0, newMax);
                    }
                }
            }
            // End Code For Change First Three ComboBox Value if Change "PVES" Channel ComboBox on 09/10/2025 At 10:54 Night  
        }

        //private void OnRawSampleForZeroing(SampleFrame raw)
        //{
        //    if (raw.Values == null) return;

        //    lock (_rawLock)
        //    {
        //        var src = raw.Values;

        //        if (_lastRawCounts == null || _lastRawCounts.Length != src.Length)
        //            _lastRawCounts = new double[src.Length];

        //        Array.Copy(src, _lastRawCounts, src.Length);
        //    }
        //}


        //Current working 28-02-2026
        private void OnRawSampleForZeroing(SampleFrame raw)
        {
            // System.Diagnostics.Debug.WriteLine($"OnRawSampleForZeroing called at {DateTime.Now:HH:mm:ss.fff}");

            if (raw.Values == null)
            {
                //  System.Diagnostics.Debug.WriteLine("OnRawSampleForZeroing: raw.Values is NULL");
                return;
            }

            lock (_rawLock)
            {
                var src = raw.Values;

                if (_lastRawCounts == null || _lastRawCounts.Length != src.Length)
                    _lastRawCounts = new double[src.Length];

                Array.Copy(src, _lastRawCounts, src.Length);

                //  System.Diagnostics.Debug.WriteLine($"OnRawSampleForZeroing: Updated _lastRawCounts with {src.Length} values");
            }
        }

        //Bhushan Commint On 06-01-2025
        //private void AttachOrch()
        //{
        //    if (_orch == null) return;

        //    if (!_orchSubscribed)
        //    {
        //        _orch.OnDisplayFrame += OnDisplayFrame;
        //        _orchSubscribed = true;
        //    }

        //    if (!_daqSubscribed)
        //    {
        //        _orch.Daq.OnRawSample += OnRawSampleForZeroing;
        //        _daqSubscribed = true;
        //    }
        //}

        //private void DetachOrch()
        //{
        //    if (_orch == null) return;

        //    if (_orchSubscribed)
        //    {
        //        _orch.OnDisplayFrame -= OnDisplayFrame;
        //        _orchSubscribed = false;
        //    }

        //    if (_daqSubscribed)
        //    {
        //        _orch.Daq.OnRawSample -= OnRawSampleForZeroing;
        //        _daqSubscribed = false;
        //    }
        //}

        private TestOrchestrator _attachedOrch = null;
        private IDaqService _attachedDaq = null;

        private void AttachOrch()
        {
            if (_orch == null) return;

            // If we are switching to a different orchestrator, detach from the previous one first
            if (_attachedOrch != null && !ReferenceEquals(_attachedOrch, _orch))
            {
                try { _attachedOrch.OnDisplayFrame -= OnDisplayFrame; } catch { }
                _orchSubscribed = false;

                try
                {
                    if (_attachedDaq != null)
                        _attachedDaq.OnRawSample -= OnRawSampleForZeroing;
                }
                catch { }
                _daqSubscribed = false;

                _attachedOrch = null;
                _attachedDaq = null;
            }

            if (!_orchSubscribed)
            {
                _orch.OnDisplayFrame += OnDisplayFrame;
                _orchSubscribed = true;
                _attachedOrch = _orch;
            }

            if (!_daqSubscribed && _orch.Daq != null)
            {
                _orch.Daq.OnRawSample += OnRawSampleForZeroing;
                _daqSubscribed = true;
                _attachedDaq = _orch.Daq;
            }
        }

        private void DetachOrch()
        {
            // Detach from whatever we last attached to (NOT just current _orch)
            if (_attachedOrch != null && _orchSubscribed)
            {
                try { _attachedOrch.OnDisplayFrame -= OnDisplayFrame; } catch { }
                _orchSubscribed = false;
            }

            if (_attachedDaq != null && _daqSubscribed)
            {
                try { _attachedDaq.OnRawSample -= OnRawSampleForZeroing; } catch { }
                _daqSubscribed = false;
            }

            _attachedOrch = null;
            _attachedDaq = null;
        }



        private void StartFromDemo()
        {
            // We are in demo (live preview with no recording) and want to transition
            // to a clean live test while keeping DAQ running.
            _isDemoMode = false;        // leaving demo mode
            _isPlaybackMode = false;
            _acceptLiveFrames = true;   // will be briefly gated inside ForceFreshStart...

            // Reuse the common graph + processor reset logic
            ForceFreshStartForLiveGraph();
        }

        private void StartLiveTest()
        {
            // Make sure we left demo/playback and are in live mode
            _isDemoMode = false;
            _isPlaybackMode = false;
            _acceptLiveFrames = true;

            _isRecording = true;
            _isPaused = false;

            // Ensure current test definition
            if (_currentTestDef == null && _testMgr != null)
                _currentTestDef = _testMgr.GetDefinition(currenttest);
            DetachOrch();
            // Ensure orchestrator exists and is wired to chart
            if (_orch == null)
            {
                DaqService daq = new DaqService();
                Calibration cal = new Calibration(_profile);

                if (_signalProcessor == null)
                    _signalProcessor = new SignalProcessor(cal, daq.SampleRateHz, displayHz: 8.0);
                _orch = new TestOrchestrator(daq, _signalProcessor, new PumpController(), new SysSetupStore());
            }

            AttachOrch();

            try
            {
                // Use the same AI_CONFIG you use elsewhere
                _orch.Start(AI_CONFIG);
            }
            catch
            {
                // swallow/log; you already handle DAQ errors elsewhere
            }

            // NOTE:
            // We do NOT clear the chart here – that’s done by ForceFreshStartForLiveGraph / StartFromDemo.
            // This method is now “responsible for live acquisition + recording”.
        }

        //Start this code for live test comment on 03/12/2025

        //private void StopLiveTest(bool savePrompt)
        //{
        //    _isRecording = false;
        //    _acceptLiveFrames = false;
        //    try { if (_orch != null) _orch.Stop(); } catch { }
        //    DetachOrch();

        //    if (savePrompt && _recorded.Count > 0)
        //    {
        //        if (_currentPatient != null && !string.IsNullOrEmpty(currenttest))
        //        {
        //            SaveCurrentTestToPatient();
        //        }
        //        else
        //        {
        //            // fallback to manual choose if no patient bound
        //            SaveFileDialog sfd = new SaveFileDialog();
        //            sfd.Filter = "UroTest (*.utt)|*.utt";
        //            sfd.FileName = "UroTest_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".utt";
        //            try
        //            {
        //                if (sfd.ShowDialog(this) == DialogResult.OK) SaveUtt(sfd.FileName);
        //            }
        //            finally { sfd.Dispose(); }
        //        }
        //    }
        //}
        //End this code for live test comment on 03/12/2025

        //Start Code For Live Test Saved and Go to Review Mode Test change on 03/12/2025
        private string StopLiveTest(bool savePrompt)
        {
            _isRecording = false;
            _acceptLiveFrames = false;

            try { if (_orch != null) _orch.Stop(); } catch { }
            DetachOrch();

            string savedFilePath = null;

            if (savePrompt && _recorded.Count > 0)
            {
                if (_currentPatient != null && !string.IsNullOrEmpty(currenttest))
                {
                    savedFilePath = SaveCurrentTestToPatient();   // <-- return path
                }
                else
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "UroTest (*.utt)|*.utt";
                        sfd.FileName = "UroTest_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".utt";

                        if (sfd.ShowDialog(this) == DialogResult.OK)
                        {
                            SaveUtt(sfd.FileName);
                            savedFilePath = sfd.FileName;         // <-- return this
                        }
                    }
                }
            }

            return savedFilePath;    // ⭐ VERY IMPORTANT
        }
        //End Code For Live Test Saved and Go to Review Mode Test change on 03/12/2025

        private void OpenSavedTest()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "UroTest (*.utt)|*.utt|All files (*.*)|*.*";
            try
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
            }
            finally { ofd.Dispose(); }

            // Stop live & gate frames
            StopLiveTest(false);
            _isPlaybackMode = true;
            _acceptLiveFrames = false;

            _liveChart.Stop();
            _liveChart.Clear();

            string[] fileLaneNames;
            double dt;
            List<SampleRecord> rows = LoadUtt(ofd.FileName, out fileLaneNames, out dt);
            if (rows == null || rows.Count == 0) { MessageBox.Show("File has no samples."); return; }

            string[] currentLaneNames = GetCurrentChartLaneNames();
            if (currentLaneNames.Length == 0) { MessageBox.Show("No lanes are active for plotting."); return; }

            int[] map = BuildColumnMap(currentLaneNames, fileLaneNames);

            int rCount = rows.Count;
            int cols = currentLaneNames.Length;
            double[,] block = new double[rCount, cols];

            int r, c;
            for (r = 0; r < rCount; r++)
            {
                double[] src = rows[r].Values; // in file's column order
                for (c = 0; c < cols; c++)
                {
                    int srcIdx = map[c];
                    block[r, c] = (srcIdx >= 0 && srcIdx < src.Length) ? src[srcIdx] : 0.0;
                }
            }

            double startT = rows[0].T;
            double endT = rows[rCount - 1].T;
            double span = endT - startT;
            if (span < 10.0) span = 10.0;

            _liveChart.SetVisibleDuration(_screenMinutes * 60.0);
            _liveChart.AppendBlock(block, startT, dt);
            _liveChart.Invalidate();
        }



        private void SaveUtt(string filePath)
        {
            string[] laneNames = GetCurrentChartLaneNames();

            System.IO.StreamWriter w = null;
            try
            {
                w = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);

                double t0 = (_recorded.Count > 0) ? _recorded[0].T : 0.0;

                // Header
                w.Write("Time");
                int i;
                for (i = 0; i < laneNames.Length; i++)
                {
                    w.Write(",");
                    w.Write(laneNames[i]);
                }
                w.WriteLine();

                // Samples
                System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
                int r;
                for (r = 0; r < _recorded.Count; r++)
                {
                    SampleRecord rec = _recorded[r];
                    // w.Write(rec.T.ToString("F3", inv));
                    w.Write((rec.T - t0).ToString("F3", inv));

                    int c;
                    for (c = 0; c < rec.Values.Length; c++)
                    {
                        w.Write(",");
                        w.Write(rec.Values[c].ToString("G", inv));
                    }
                    w.WriteLine();
                }

                // --- Markers section (optional) ---
                // Format per line: #M,<timeSec>,<base64(label)>,<colorARGB>,<width>,<dashInt>
                // Example:        #M,12.345,RlM=,-65536,2,0
                // We put a header line to make it obvious.
                w.WriteLine("#MARKERS");
                IReadOnlyList<SantronChart.MultiChannelLiveChart.Marker> mlist = _liveChart.Markers;
                for (i = 0; i < mlist.Count; i++)
                {
                    var m = mlist[i];
                    //string tS = m.T.ToString("F3", inv);
                    string tS = (m.T - t0).ToString("F3", inv);
                    string lbl64 = ToBase64(m.Label ?? "");
                    string argb = m.Color.ToArgb().ToString(inv);
                    string width = m.Width.ToString("G", inv);
                    string dash = ((int)m.Dash).ToString(inv);

                    w.WriteLine("#M," + tS + "," + lbl64 + "," + argb + "," + width + "," + dash);
                }
            }
            finally { if (w != null) w.Dispose(); }
        }



        private List<SampleRecord> LoadUtt(
    string path,
    out string[] fileLaneNames,
    out double dt,
    out List<SantronChart.MultiChannelLiveChart.Marker> markers)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            List<SampleRecord> data = new List<SampleRecord>();
            markers = new List<SantronChart.MultiChannelLiveChart.Marker>();
            fileLaneNames = new string[0];
            dt = 0.1;

            if (lines.Length < 2) return data;

            // header
            string[] head = lines[0].Split(',');
            if (head.Length < 2) return data;

            int laneCount = head.Length - 1;
            fileLaneNames = new string[laneCount];
            int i;
            for (i = 0; i < laneCount; i++) fileLaneNames[i] = head[i + 1].Trim();

            // rows + markers
            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            double? prevT = null;
            double? dtLocal = null;

            for (i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // markers section
                if (line.StartsWith("#M,", StringComparison.OrdinalIgnoreCase))
                {
                    // #M,<t>,<base64(label)>,<argb>,<width>,<dash>
                    string payload = line.Substring(3);
                    string[] partsM = payload.Split(',');
                    if (partsM.Length >= 5)
                    {
                        double tM; int argb; float width; int dashInt;
                        string lbl = FromBase64(partsM[1]);

                        if (!double.TryParse(partsM[0], System.Globalization.NumberStyles.Any, inv, out tM)) tM = 0.0;
                        if (!int.TryParse(partsM[2], System.Globalization.NumberStyles.Integer, inv, out argb)) argb = Color.Black.ToArgb();
                        if (!float.TryParse(partsM[3], System.Globalization.NumberStyles.Any, inv, out width)) width = 2f;
                        if (!int.TryParse(partsM[4], System.Globalization.NumberStyles.Integer, inv, out dashInt)) dashInt = (int)System.Drawing.Drawing2D.DashStyle.Custom;

                        Color col = Color.FromArgb(argb);
                        System.Drawing.Drawing2D.DashStyle dash = (System.Drawing.Drawing2D.DashStyle)dashInt;

                        markers.Add(new SantronChart.MultiChannelLiveChart.Marker(tM, lbl, col, width, dash));
                    }
                    continue;
                }
                if (line.StartsWith("#")) continue; // ignore any other comment lines

                // sample row
                string[] parts = line.Split(',');
                if (parts.Length != laneCount + 1) continue;

                double t;
                if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Any, inv, out t))
                    t = (i - 1) * 0.1;

                double[] vals = new double[laneCount];
                int k;
                for (k = 0; k < laneCount; k++)
                {
                    double v;
                    if (!double.TryParse(parts[k + 1], System.Globalization.NumberStyles.Any, inv, out v)) v = 0.0;
                    vals[k] = v;
                }

                if (prevT.HasValue && !dtLocal.HasValue)
                {
                    double diff = t - prevT.Value;
                    if (diff > 0) dtLocal = diff;
                }
                prevT = t;

                data.Add(new SampleRecord(t, vals));
            }

            if (dtLocal.HasValue) dt = dtLocal.Value;
            return data;
        }

        // keep your original signature for existing callers
        private List<SampleRecord> LoadUtt(string path, out string[] fileLaneNames, out double dt)
        {
            List<SantronChart.MultiChannelLiveChart.Marker> ignore;
            return LoadUtt(path, out fileLaneNames, out dt, out ignore);
        }


        // returns array same order as the chart lanes
        private string[] GetCurrentChartLaneNames()
        {
            if (_currentTestDef == null || _currentTestDef.Lanes == null) return new string[0];

            int n = _currentTestDef.Lanes.Count;
            string[] names = new string[n];
            for (int i = 0; i < n; i++)
            {
                // Prefer whatever property on your Channel is the “label” on UI.
                // If your Channel type uses .Label:
                names[i] = _currentTestDef.Lanes[i].Name;

                // If not, try .Name:
                // names[i] = _currentTestDef.Lanes[i].Name;
            }
            return names;
        }
        private int[] GetActiveIndices()
        {
            if (_currentTestDef == null || _currentTestDef.Indices == null) return new int[0];
            // clone so we don't mutate def
            int len = _currentTestDef.Indices.Length;
            int[] arr = new int[len];
            for (int i = 0; i < len; i++) arr[i] = _currentTestDef.Indices[i];
            return arr;
        }

        private int[] BuildColumnMap(string[] currentLanes, string[] fileLanes)
        {
            int n = currentLanes.Length;
            int[] map = new int[n];
            int i, j;

            for (i = 0; i < n; i++)
            {
                map[i] = -1;
                string want = currentLanes[i];

                for (j = 0; j < fileLanes.Length; j++)
                {
                    if (string.Equals(want, fileLanes[j], StringComparison.OrdinalIgnoreCase))
                    {
                        map[i] = j; // file column that matches this current lane
                        break;
                    }
                }
            }
            return map;
        }



        private static string SanitizeFilePart(string s)
        {
            if (s == null) return "";
            char[] arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char ch = arr[i];
                if (ch == '\\' || ch == '/' || ch == ':' || ch == '*' || ch == '?' ||
                    ch == '"' || ch == '<' || ch == '>' || ch == '|')
                {
                    arr[i] = '_';
                }
            }
            return new string(arr).Trim();
        }

        private string GetPatientsRootFolder()
        {
            // return System.IO.Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
            return AppPathManager.GetFolderPath("Samples");

        }

        private string GetPatientFolder(PatientRecord p)
        {
            // Example: Saved Data/PatientsData/000123_John_Doe
            string baseFolder = GetPatientsRootFolder();
            string id = SanitizeFilePart(p.PatientNo);
            string name = SanitizeFilePart(p.PatientName);
            string folderName = (string.IsNullOrEmpty(id) ? "" : id + "_") + name;
            if (string.IsNullOrEmpty(folderName)) folderName = "UnknownPatient";
            return System.IO.Path.Combine(baseFolder, folderName);
        }

        private string GetPatientTestsFolder(PatientRecord p, string testName)
        {
            // Example: .../000123_John_Doe/Tests/Cystometry
            string pf = GetPatientFolder(p);
            string tests = System.IO.Path.Combine(pf, "Tests", SanitizeFilePart(testName ?? "UnknownTest"));
            return tests;
        }

        //private bool TryFindLatestUttForPatient(PatientRecord p, string testName, out string uttPath)
        //{
        //    uttPath = null;
        //    try
        //    {
        //        string folder = GetPatientTestsFolder(p, testName);
        //        if (!System.IO.Directory.Exists(folder)) return false;

        //        string[] files = System.IO.Directory.GetFiles(folder, "*.utt");
        //        if (files == null || files.Length == 0) return false;

        //        // pick newest by write time
        //        string newest = files[0];
        //        System.DateTime newestTime = System.IO.File.GetLastWriteTime(newest);
        //        for (int i = 1; i < files.Length; i++)
        //        {
        //            System.DateTime t = System.IO.File.GetLastWriteTime(files[i]);
        //            if (t > newestTime) { newestTime = t; newest = files[i]; }
        //        }

        //        uttPath = newest;
        //        return true;
        //    }
        //    catch { return false; }
        //}


        private bool TryFindLatestUttForPatient(PatientRecord p, string testName, out string uttPath)
        {
            uttPath = null;

            if (p == null || string.IsNullOrEmpty(testName))
                return false;

            try
            {
                string folder = GetPatientTestsFolder(p, testName);
                if (!Directory.Exists(folder))
                    return false;

                // get all UTT files
                string[] files = Directory.GetFiles(folder, "*.utt");
                if (files.Length == 0)
                    return false;

                List<string> matching = new List<string>();

                foreach (var file in files)
                {
                    string fName = Path.GetFileNameWithoutExtension(file);
                    string[] parts = fName.Split('_');

                    // format required: TestName_MainId_PatientNo_yyyymmdd_HHmm
                    if (parts.Length < 4)
                        continue;

                    string fTest = parts[0];
                    string fMainId = parts[1];
                    string fPatNo = parts[2];

                    if (fTest == testName &&
                        fMainId == p.Id.ToString() &&
                        fPatNo == p.PatientNo.ToString())
                    {
                        matching.Add(file);
                    }
                }

                if (matching.Count == 0)
                    return false;

                // sort by last write (newest first)
                matching = matching.OrderBy(f => File.GetLastWriteTime(f)).ToList();

                // pick newest
                uttPath = matching[matching.Count - 1];
                return true;
            }
            catch
            {
                return false;
            }
        }


        //Start this code for live test comment on 03/12/2025
        //save Test
        //private void SaveCurrentTestToPatient()
        //{
        //    if (_currentPatient == null || string.IsNullOrEmpty(currenttest))
        //    {
        //        MessageBox.Show("No patient/test selected. Cannot save under patient.");
        //        return;
        //    }
        //    if (_recorded == null || _recorded.Count == 0)
        //    {
        //        MessageBox.Show("Nothing to save.");
        //        return;
        //    }

        //    string folder = GetPatientTestsFolder(_currentPatient, currenttest);
        //    try { System.IO.Directory.CreateDirectory(folder); } catch { }

        //    //string fileName = SanitizeFilePart(currenttest) + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm") + ".utt";

        //    // ✅ Updated filename: TestName_MainId_PatientNo_yyyyMMdd_HHmm.utt
        //    string fileName =
        //        $"{SanitizeFilePart(currenttest)}_{_currentPatient.Id}_{_currentPatient.PatientNo}_{DateTime.Now:yyyyMMdd_HHmm}.utt";

        //    string fullPath = System.IO.Path.Combine(folder, fileName);

        //    SaveUtt(fullPath);
        //    MessageBox.Show("Test saved to:\n" + fullPath);
        //}
        //End this code for live test comment on 03/12/2025


        //Start Code For Live Test Saved and Go to Review Mode Test change on 03/12/2025
        //this code chnage only Void to string 

        //Live Test Saved Code
        private string SaveCurrentTestToPatient()
        {
            if (_currentPatient == null || string.IsNullOrEmpty(currenttest))
            {
                MessageBox.Show("No patient/test selected. Cannot save under patient.");
                return null;
            }
            if (_recorded == null || _recorded.Count == 0)
            {
                MessageBox.Show("Nothing to save.");
                return null;
            }

            string folder = GetPatientTestsFolder(_currentPatient, currenttest);
            try { Directory.CreateDirectory(folder); } catch { }

            string fileName =
                $"{SanitizeFilePart(currenttest)}_{_currentPatient.Id}_{_currentPatient.PatientNo}_{DateTime.Now:yyyyMMdd_HHmm}.utt";

            string fullPath = Path.Combine(folder, fileName);

            SaveUtt(fullPath);

            //MessageBox.Show("Test saved to:\n" + fullPath);

            return fullPath;   // ⭐ return path
        }
        //End Code For Live Test Saved and Go to Review Mode Test change on 03/12/2025




        private void AllButtonEnable()
        {
            //Panel 1
            btnTestStart1.Enabled = true;
            btnTestPause1.Enabled = true;
            btnGP.Enabled = true;

            //Panel 2
            btnFS.Enabled = true;
            btnFD.Enabled = true;
            btnND.Enabled = true;
            btnSD.Enabled = true;
            btnBC.Enabled = true;
            btnTestStart2.Enabled = true;
            btnTestPause2.Enabled = true;
            btnEventGP.Enabled = true;
            btnEventLE.Enabled = true;
            btnEventCU.Enabled = true;
            btnEventUDC.Enabled = true;

            //Pane 4
            btnTestStart3.Enabled = true;
            btnTestPause3.Enabled = true;
            btnEventGP3.Enabled = true;
            btnEventLE3.Enabled = true;
            btnEventCU3.Enabled = true;
            btnEventUDC3.Enabled = true;

            //Panel 5
            btnTestStart4.Enabled = true;
            btnTestPause4.Enabled = true;

            //Panel 6
            btnTestStart5.Enabled = true;
            btnTestPause5.Enabled = true;
            btnGP6.Enabled = true;
            btnLE6.Enabled = true;
            btnCU6.Enabled = true;
            btnUDC6.Enabled = true;
            btnFS6.Enabled = true;
            btnFD6.Enabled = true;
            btnND6.Enabled = true;
            btnSD6.Enabled = true;
            btnBC6.Enabled = true;

        }

        private void AllButtonDisable()
        {
            //Panel 1
            btnTestStart1.Enabled = false;
            btnTestPause1.Enabled = false;
            btnGP.Enabled = false;

            //Panel 2
            btnFS.Enabled = false;
            btnFD.Enabled = false;
            btnND.Enabled = false;
            btnSD.Enabled = false;
            btnBC.Enabled = false;
            btnTestStart2.Enabled = false;
            btnTestPause2.Enabled = false;
            btnEventGP.Enabled = false;
            btnEventLE.Enabled = false;
            btnEventCU.Enabled = false;
            btnEventUDC.Enabled = false;

            //Pane 4
            btnTestStart3.Enabled = false;
            btnTestPause3.Enabled = false;
            btnEventGP3.Enabled = false;
            btnEventLE3.Enabled = false;
            btnEventCU3.Enabled = false;
            btnEventUDC3.Enabled = false;

            //Panel 5
            btnTestStart4.Enabled = false;
            btnTestPause4.Enabled = false;

            //Panel 6
            btnTestStart5.Enabled = false;
            btnTestPause5.Enabled = false;
            btnGP6.Enabled = false;
            btnLE6.Enabled = false;
            btnCU6.Enabled = false;
            btnUDC6.Enabled = false;
            btnFS6.Enabled = false;
            btnFD6.Enabled = false;
            btnND6.Enabled = false;
            btnSD6.Enabled = false;
            btnBC6.Enabled = false;

        }

        private bool _isDemoMode = false;

        // Add this one line with your other class fields
        private bool _isLiveTestRunning = false;

        private void EnterDemoMode()
        {
            panel8.Visible = false;
            mainContainerPanel.Visible = false;
            ResetTimeForNewTest();

            //ForceChartZero();
            _isPlaybackMode = false;
            _acceptLiveFrames = true;
            label36.Visible = true;
            if (_orch == null)
            {
                try
                {
                    LoadGraph(); // This starts DAQ and populates _lastRawCounts
                }
                catch
                {
                    MessageBox.Show("Failed to initialize device");
                    return;
                }
            }

            if (_deviceState == DeviceState.Connected)
            {
                //Connect
                AllButtonEnable();
                label37.Visible = false;
                //btnTestStart2.Enabled = true;
                label36.Visible = true;
                panel7.Visible = false;
            }
            else
            {
                //Disconnect
                AllButtonDisable();
                panel7.Visible = true;
                label37.Visible = true;
                //btnTestStart2.Enabled = false;
                label36.Visible = false;

            }



            _liveChart.Clear();
            _liveChart.ScrollToLive();

            ArmTimeZero();
            _liveChart.Start();

            // 🚫 Disable mouse hover for live graph
            _liveChart.EnableHover = false;

            // ✅ Force live tail to remain visible
            _liveChart.ToggleLive(true);

            _isDemoMode = true;

            // ✅ SIMPLE: Set flag to TRUE when entering demo mode
            _isLiveTestRunning = true;


            //Panel 3
            btnR1.Enabled = false;
            btnR2.Enabled = false;
            btnM.Enabled = false;
            btnS1.Enabled = false;
            btnS2.Enabled = false;

            //btnTestPause1.Enabled = false;
            btnTestStop1.Enabled = false;
            btnTestSave1.Enabled = false;


            //Panel 2
            btnGraphS1.Enabled = false;
            btnGraphS2.Enabled = false;
            btnArtifactR1.Enabled = false;
            btnArtifactR2.Enabled = false;
            btnArtifactM.Enabled = false;

            //test buttons 
            //btnTestPause2.Enabled = false;
            btnTestStop2.Enabled = false;
            btnTestSave2.Enabled = false;


            //Panel 4
            btnMarkingR1.Enabled = false;
            btnMarkingR2.Enabled = false;
            btnMarkingM.Enabled = false;
            btnSelectGraphS1.Enabled = false;
            btnSelectGraphS2.Enabled = false;

            //test buttons 
            //btnTestPause3.Enabled = false;
            btnTestStop3.Enabled = false;
            btnTestSave3.Enabled = false;

            //panel 5
            //test buttons 
            //btnTestPause4.Enabled = false;
            btnTestStop4.Enabled = false;
            btnTestSave4.Enabled = false;

            //Panel 6
            btnR1P6.Enabled = false;
            btnR2P6.Enabled = false;
            btnM6.Enabled = false;
            btnS1P6.Enabled = false;
            btnS2P6.Enabled = false;

            //test buttons 
            //btnTestPause5.Enabled = false;
            btnTestStop5.Enabled = false;
            btnTestSave5.Enabled = false;


            //Live Test Enabled Save,Pause,Stop buttons
            //btnTestStart1.Enabled = true;
            //btnTestPause1.Enabled = true;
            //btnTestStop1.Enabled = true;

            //btnTestStart2.Enabled = true;
            //btnTestPause2.Enabled = true;
            //btnTestStop2.Enabled = true;

            //btnTestStart3.Enabled = true;
            //btnTestPause3.Enabled = true;
            //btnTestStop3.Enabled = true;

            //btnTestStart4.Enabled = true;
            //btnTestPause4.Enabled = true;
            //btnTestStop4.Enabled = true;

            //btnTestStart5.Enabled = true;
            //btnTestPause5.Enabled = true;
            //btnTestStop5.Enabled = true;
        }


        private void ButtonsShow()
        {
            //_isDemoMode = false;
            //Panel 3
            btnR1.Enabled = true;
            btnR2.Enabled = true;
            btnM.Enabled = true;
            btnS1.Enabled = true;
            btnS2.Enabled = true;

            //Panel 2
            btnGraphS1.Enabled = true;
            btnGraphS2.Enabled = true;
            btnArtifactR1.Enabled = true;
            btnArtifactR2.Enabled = true;
            btnArtifactM.Enabled = true;

            //Panel 4
            btnMarkingR1.Enabled = true;
            btnMarkingR2.Enabled = true;
            btnMarkingM.Enabled = true;
            btnSelectGraphS1.Enabled = true;
            btnSelectGraphS2.Enabled = true;

            //Panel 6
            btnR1P6.Enabled = true;
            btnR2P6.Enabled = true;
            btnM6.Enabled = true;
            btnS1P6.Enabled = true;
            btnS2P6.Enabled = true;

            //Saved Test Disable Save,Puase,Stop buttons
            btnTestStart1.Enabled = false;
            //btnTestPause1.Enabled = false;
            //btnTestStop1.Enabled = false;

            btnTestStart2.Enabled = false;
            //btnTestPause2.Enabled = false;
            //btnTestStop2.Enabled = false;

            btnTestStart3.Enabled = false;
            //btnTestPause3.Enabled = false;
            //btnTestStop3.Enabled = false;

            btnTestStart4.Enabled = false;
            //btnTestPause4.Enabled = false;
            //btnTestStop4.Enabled = false;

            btnTestStart5.Enabled = false;
            //btnTestPause5.Enabled = false;
            //btnTestStop5.Enabled = false;

            _liveChart.EnableHover = true;
        }

        private bool _isUpdateMode = false;

        private void LoadUttForPlayback(string path, int v)
        {
            //Start Code For Update saved Test 15/12/2025
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show("Saved test file not found.");
                return;
            }
            StopCamera();
            ShowPrintButtons();

            // ⭐ REGISTER CURRENT TEST FOR UPDATE
            _currentSavedTestPath = path;
            _isUpdateMode = true;
            //End Code For Update saved Test 15/12/2025

            panel8.Visible = false;
            mainContainerPanel.Visible = false;

            //_isDemoMode = false;
            //Panel 3
            btnR1.Enabled = true;
            btnR2.Enabled = true;
            btnM.Enabled = true;
            btnS1.Enabled = true;
            btnS2.Enabled = true;

            //Panel 2
            btnGraphS1.Enabled = true;
            btnGraphS2.Enabled = true;
            btnArtifactR1.Enabled = true;
            btnArtifactR2.Enabled = true;
            btnArtifactM.Enabled = true;

            //Panel 4
            btnMarkingR1.Enabled = true;
            btnMarkingR2.Enabled = true;
            btnMarkingM.Enabled = true;
            btnSelectGraphS1.Enabled = true;
            btnSelectGraphS2.Enabled = true;

            //Panel 6
            btnR1P6.Enabled = true;
            btnR2P6.Enabled = true;
            btnM6.Enabled = true;
            btnS1P6.Enabled = true;
            btnS2P6.Enabled = true;

            //Saved Test Disable Save,Puase,Stop buttons
            btnTestStart1.Enabled = false;
            btnTestPause1.Enabled = false;
            btnTestStop1.Enabled = false;

            btnTestStart2.Enabled = false;
            btnTestPause2.Enabled = false;
            btnTestStop2.Enabled = false;

            btnTestStart3.Enabled = false;
            btnTestPause3.Enabled = false;
            btnTestStop3.Enabled = false;

            btnTestStart4.Enabled = false;
            btnTestPause4.Enabled = false;
            btnTestStop4.Enabled = false;

            btnTestStart5.Enabled = false;
            btnTestPause5.Enabled = false;
            btnTestStop5.Enabled = false;

            //Start Code For Update saved Test 15/12/2025
            DisableAllTestButtons();
            _isRecording = false;
            _acceptLiveFrames = false;
            _isPlaybackMode = true;
            //End Code For Update saved Test 15/12/2025

            _liveChart.EnableHover = true;

            _isRecording = false;
            StopLiveTest(false);

            _isPlaybackMode = true;
            _acceptLiveFrames = false;

            _liveChart.Stop();
            _liveChart.Clear();
            _liveChart.ClearMarkers(); // important: start clean

            string[] fileLaneNames;
            double dt;
            List<SantronChart.MultiChannelLiveChart.Marker> markers;
            List<SampleRecord> rows = LoadUtt(path, out fileLaneNames, out dt, out markers);
            if (rows == null || rows.Count == 0)
            {
                MessageBox.Show("Saved test is empty or invalid.");
                EnterDemoMode();
                return;
            }

            string[] currentLaneNames = GetCurrentChartLaneNames();
            if (currentLaneNames == null || currentLaneNames.Length == 0)
            {
                MessageBox.Show("No lanes are active for plotting.");
                EnterDemoMode();
                return;
            }


            //int[] map = BuildColumnMap(currentLaneNames, fileLaneNames);

            //int rCount = rows.Count;
            //int cols = currentLaneNames.Length;
            //double[,] block = new double[rCount, cols];

            //int r, c;
            //for (r = 0; r < rCount; r++)
            //{
            //    double[] src = rows[r].Values;
            //    for (c = 0; c < cols; c++)
            //    {
            //        int srcIdx = map[c];
            //        block[r, c] = (srcIdx >= 0 && srcIdx < src.Length) ? src[srcIdx] : 0.0;
            //    }
            //}

            //double startT = rows[0].T;
            //double endT = rows[rCount - 1].T;

            //double span = endT - startT;
            //if (span < 10.0) span = 10.0;



            //_liveChart.SetVisibleDuration(_screenMinutes * 60);


            //_liveChart.AppendBlock(block, startT, dt);

            //// Disable Live mode
            //_liveChart.ToggleLive(false);

            //// ⭐ Scroll to LEFT side (start of graph)
            //_liveChart.ScrollTo(startT);

            //FixScrollWindow(_liveChart, startT, endT);

            //// restore markers
            //for (int m = 0; m < markers.Count; m++)
            //{
            //    var mk = markers[m];
            //    _liveChart.AddMarker(mk.T, mk.Label, mk.Color, mk.Width, mk.Dash);
            //}

            //_liveChart.Invalidate();

            //// IMPORTANT:
            //// Review mode displays lanes using the mapped order (block[r,c] uses map[c]).
            //// PrintPreview uses _recorded -> so _recorded MUST be built in the SAME lane order as Review.
            //_recorded.Clear();

            //for (int i = 0; i < rows.Count; i++)
            //{
            //    var src = rows[i];
            //    double[] srcVals = src.Values;

            //    // Build values in CURRENT lane order (same as Review chart)
            //    double[] vals = new double[cols];
            //    for (int c2 = 0; c2 < cols; c2++)
            //    {
            //        int srcIdx = map[c2];
            //        vals[c2] = (srcIdx >= 0 && srcVals != null && srcIdx < srcVals.Length)
            //            ? srcVals[srcIdx]
            //            : 0.0;
            //    }

            //    _recorded.Add(new SampleRecord(src.T, vals));
            //}

            //// In playback/review mode, _recorded is already lane-ordered.
            //// Avoid any re-mapping in print.
            //_activeIndices = null;

            int[] map = BuildColumnMap(currentLaneNames, fileLaneNames);

            int rCount = rows.Count;
            int cols = currentLaneNames.Length;
            double[,] block = new double[rCount, cols];
            double[] times = new double[rCount];  // ✅ NEW: Store actual timestamps

            int r, c;
            for (r = 0; r < rCount; r++)
            {
                double[] src = rows[r].Values;
                times[r] = rows[r].T;  // ✅ NEW: Preserve actual timestamp from file

                for (c = 0; c < cols; c++)
                {
                    int srcIdx = map[c];
                    block[r, c] = (srcIdx >= 0 && srcIdx < src.Length) ? src[srcIdx] : 0.0;
                }
            }

            double startT = rows[0].T;
            double endT = rows[rCount - 1].T;

            double span = endT - startT;
            if (span < 10.0) span = 10.0;

            _liveChart.SetVisibleDuration(_screenMinutes * 60);

            // ✅ FIXED: Use new method that preserves actual timestamps
            _liveChart.AppendBlockWithTimes(block, times);

            // Disable Live mode
            _liveChart.ToggleLive(false);

            // ⭐ Scroll to LEFT side (start of graph)
            _liveChart.ScrollTo(startT);

            FixScrollWindow(_liveChart, startT, endT);

            // restore markers
            for (int m = 0; m < markers.Count; m++)
            {
                var mk = markers[m];
                _liveChart.AddMarker(mk.T, mk.Label, mk.Color, mk.Width, mk.Dash);
            }

            _liveChart.Invalidate();

            // IMPORTANT:
            // Review mode displays lanes using the mapped order (block[r,c] uses map[c]).
            // PrintPreview uses _recorded -> so _recorded MUST be built in the SAME lane order as Review.
            _recorded.Clear();

            for (int i = 0; i < rows.Count; i++)
            {
                var src = rows[i];
                double[] srcVals = src.Values;

                // Build values in CURRENT lane order (same as Review chart)
                double[] vals = new double[cols];
                for (int c2 = 0; c2 < cols; c2++)
                {
                    int srcIdx = map[c2];
                    vals[c2] = (srcIdx >= 0 && srcVals != null && srcIdx < srcVals.Length)
                        ? srcVals[srcIdx]
                        : 0.0;
                }

                _recorded.Add(new SampleRecord(src.T, vals));
            }

            // In playback/review mode, _recorded is already lane-ordered.
            // Avoid any re-mapping in print.
            _activeIndices = null;

        }



        private void FixScrollWindow(object chart, double startT, double endT)
        {
            if (chart == null) return;
            var type = chart.GetType();

            // try to set private fields for scroll window
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var f in fields)
            {
                if (f.FieldType == typeof(double))
                {
                    string name = f.Name.ToLower();

                    if (name.Contains("start"))
                        f.SetValue(chart, startT);

                    if (name.Contains("end") || name.Contains("max") || name.Contains("right"))
                        f.SetValue(chart, endT);
                }
            }
        }

        //Start Code For Update saved Test 15/12/2025
        private void DisableAllTestButtons()
        {
            btnTestStart1.Enabled = btnTestPause1.Enabled = btnTestStop1.Enabled = false;
            btnTestStart2.Enabled = btnTestPause2.Enabled = btnTestStop2.Enabled = false;
            btnTestStart3.Enabled = btnTestPause3.Enabled = btnTestStop3.Enabled = false;
            btnTestStart4.Enabled = btnTestPause4.Enabled = btnTestStop4.Enabled = false;
            btnTestStart5.Enabled = btnTestPause5.Enabled = btnTestStop5.Enabled = false;
        }
        //End Code For Update saved Test 15/12/2025

        private static string ToBase64(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try { byte[] b = System.Text.Encoding.UTF8.GetBytes(s); return Convert.ToBase64String(b); }
            catch { return ""; }
        }

        private static string FromBase64(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try { byte[] b = Convert.FromBase64String(s); return System.Text.Encoding.UTF8.GetString(b); }
            catch { return s; } // tolerate plain text if ever encountered
        }

        //private void OnDisplayFrame(SampleFrame f)
        //{
        //    // project: pick just the active columns and order them to match lanes
        //    var vals = new double[_activeIndices.Length];
        //    for (int i = 0; i < _activeIndices.Length; i++)
        //        vals[i] = f.Values[_activeIndices[i]];

        //    if (_warmupCounter < _calibWarmup)
        //    {
        //        _warmupCounter++;
        //        return; // skip plotting/learning
        //    }




        //    if (_isRecording && !_isPaused)
        //        _recorded.Add((f.T, (double[])vals.Clone())); // record exactly what's shown

        //    _lastPlotT = f.T;

        //    _liveChart.AppendSample(vals, f.T);
        //}


        //private void ShowMainContent()
        //{
        //    if (mainContentPanel == null || rightSidebarPanel == null)
        //    {
        //        CreateMainContent();
        //        CreateRightSidebar();
        //    }
        //    else
        //    {
        //        mainContentPanel.Visible = true;
        //        rightSidebarPanel.Visible = true;
        //    }
        //}





        //Start Graph Code
        private string GetScaleAndColorSetupFilePath(string channelZero)
        {
            string exeFolder = Application.StartupPath;
            //string folder = Path.Combine(exeFolder, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");


            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, channelZero + ".dat");
        }

        public ChannelSettings[] GetChannelSettings()
        {
            return channelSettings;
        }

        //Start Code For "ScaleANDColorSetup" Screen if update any data so immediately Reflect no need to restart applicaton 26-02-2025 (2 Method changes if find this methods copy the comment & search) 
        private ChannelSettings[] _channelSettings;
        public ChannelSettings[] ChannelSettings
        {
            get => _channelSettings;
            private set => _channelSettings = value;
        }
        public void ReloadScaleAndColorSetup()
        {
            try
            {
                var setup = LoadScaleAndColorModel("DefaultSetup");

                if (setup != null)
                {
                    _testMgr = new TestChannelManager(setup);

                    // Only update chart if it exists
                    if (_liveChart != null && !_liveChart.IsDisposed)
                    {
                        ConfigureChartLanes(setup);
                        BuildScaleMaxOverlays();
                        _liveChart.Invalidate();
                    }

                    // Always update the settings
                    ChannelSettings = LoadChannelSettings();

                    System.Diagnostics.Debug.WriteLine("Scale and Color settings reloaded successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading ScaleAndColor: {ex.Message}");
            }
        }
        //END Code For "ScaleANDColorSetup" Screen if update any data so immediately Reflect no need to restart app 26-02-2025
        private ChannelSettings[] LoadChannelSettings()
        {
            string defaultChannelZero = "PVES";
            string filePath = GetScaleAndColorSetupFilePath(defaultChannelZero);
            ScaleAndColorModel record = null;

            if (File.Exists(filePath))
            {
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(filePath);
                    string jsonData = CryptoHelper.Decrypt(encryptedData);

                    jsonData = FixJsonForIntFields(jsonData);

                    record = JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading Scale And Color Setup: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    record = null;
                }
            }



            // ✅ Safe parser (handles invalid or partial strings like "0 - 50" or "0-")
            double SafeParse(string input, double defaultValue)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return defaultValue;

                // Remove unwanted spaces
                input = input.Trim();

                // Try normal numeric parse
                if (double.TryParse(input, out double result))
                    return result;

                // Try "0-50" or "0 - 50" style
                if (input.Contains('-'))
                {
                    var parts = input.Split('-');
                    if (parts.Length > 0 && double.TryParse(parts[0].Trim(), out double min))
                        return min;
                }

                return defaultValue;
            }

            // For YMax specifically
            double SafeParseMax(string input, double defaultValue)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return defaultValue;

                input = input.Trim();

                if (double.TryParse(input, out double result))
                    return result;

                if (input.Contains('-'))
                {
                    var parts = input.Split('-');
                    if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double max))
                        return max;
                }

                return defaultValue;
            }

            // If no file or error, fall back to defaults
            if (record == null)
            {
                return new[]
                {
                    new ChannelSettings { Name = "PVES", YMin = 0, YMax = 6, LineColor = Color.Blue },
                    new ChannelSettings { Name = "PABD", YMin = 0, YMax = 8, LineColor = Color.Red },
                    new ChannelSettings { Name = "QVOL", YMin = 0, YMax = 10, LineColor = Color.Black },
                    new ChannelSettings { Name = "VINF", YMin = 0, YMax = 15, LineColor = Color.SkyBlue },
                    new ChannelSettings { Name = "EMG", YMin = 0.1, YMax = 6, LineColor = Color.Orange },
                    new ChannelSettings { Name = "UPP", YMin = 0.1, YMax = 7, LineColor = Color.Yellow },
                    new ChannelSettings { Name = "PURA", YMin = 0, YMax = 6, LineColor = Color.Green }
                };
            }

            // ✅ Safe parsing for all channels
            return new[]
            {
                new ChannelSettings
                {
                    Name = record.ChannelZero ?? "PVES",
                    YMin = SafeParse(record.PlotScaleZero, 0),
                    YMax = SafeParseMax(record.PlotScaleZero, 6),
                    LineColor = ColorTranslator.FromHtml(record.ColorZero ?? "#0000FF")
                },
                new ChannelSettings
                {
                    Name = record.ChannelOne ?? "PABD",
                    YMin = SafeParse(record.PlotScaleOne, 0),
                    YMax = SafeParseMax(record.PlotScaleOne, 8),
                    LineColor = ColorTranslator.FromHtml(record.ColorOne ?? "#FF0000")
                },
                new ChannelSettings
                {
                    Name = record.ChannelTwo ?? "QVOL",
                    YMin = SafeParse(record.PlotScaleTwo, 0),
                    YMax = SafeParseMax(record.PlotScaleTwo, 10),
                    LineColor = ColorTranslator.FromHtml(record.ColorTwo ?? "#000000")
                },
                new ChannelSettings
                {
                    Name = record.ChannelThree ?? "VINF",
                    YMin = SafeParse(record.PlotScaleThree, 0),
                    YMax = SafeParseMax(record.PlotScaleThree, 15),
                    LineColor = ColorTranslator.FromHtml(record.ColorThree ?? "#87CEEB")
                },
                new ChannelSettings
                {
                    Name = record.ChannelFour ?? "EMG",
                    YMin = SafeParse(record.PlotScaleFour, 0.1),
                    YMax = SafeParseMax(record.PlotScaleFour, 6),
                    LineColor = ColorTranslator.FromHtml(record.ColorFour ?? "#FFA500")
                },
                new ChannelSettings
                {
                    Name = record.ChannelFive ?? "UPP",
                    YMin = SafeParse(record.PlotScaleFive, 0.1),
                    YMax = SafeParseMax(record.PlotScaleFive, 7),
                    LineColor = ColorTranslator.FromHtml(record.ColorFive ?? "#FFFF00")
                },
                new ChannelSettings
                {
                    Name = record.ChannelSix ?? "PURA",
                    YMin = SafeParse(record.PlotScaleSix, 0),
                    YMax = SafeParseMax(record.PlotScaleSix, 6),
                    LineColor = ColorTranslator.FromHtml(record.ColorSix ?? "#008000")
                }
            };
        }



        private void ConfigureChartLanes(ScaleAndColorModel record)
        {
            List<MultiChannelLiveChart.Channel> lanes = new List<MultiChannelLiveChart.Channel>();

            lanes.Add(CreateChannel(record.ChannelZero, record.ColorZero, record.PlotScaleZero, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelOne, record.ColorOne, record.PlotScaleOne, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelTwo, record.ColorTwo, record.PlotScaleTwo, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelThree, record.ColorThree, record.PlotScaleThree, "ml"));
            lanes.Add(CreateChannel(record.ChannelFour, record.ColorFour, record.PlotScaleFour, "ml"));
            lanes.Add(CreateChannel(record.ChannelFive, record.ColorFive, record.PlotScaleFive, "ml/s"));
            lanes.Add(CreateChannel(record.ChannelSix, record.ColorSix, record.PlotScaleSix, "uV"));
            lanes.Add(CreateChannel(record.ChannelSeven, record.ColorSeven, record.PlotScaleSeven, "cmH2O"));
            //lanes.Add(CreateChannel(record.ChannelEight, record.ColorEight, record.PlotScaleEight, "cmH2O"));


            //BladderColor =  record.BladderSensation;
            //GPColor =record.GeneralPurose;
            //ResponseColor = record.ResponeMarkers;

            BladderColor = record.BladderSensation ?? Color.Magenta.ToArgb();  // Default if null
            GPColor = record.GeneralPurose ?? Color.Magenta.ToArgb();          // Default if null
            ResponseColor = record.ResponeMarkers ?? Color.Magenta.ToArgb();   // Default if null

            // Remove nulls (if channel name is empty)
            lanes = lanes.Where(x => x != null).ToList();

            _activeIndices = Enumerable.Range(0, lanes.Count).ToArray();

            _liveChart.Clear();
            _liveChart.SetChannels(lanes);
            // Enable mirror plotting for EMG only when using BLE
            if (_useBleEmg)
            {
                _liveChart.SetMirrorMode("EMG", true);

                // Mark EMG as high-speed channel
                var channelNames = _liveChart.GetChannelNames();
                for (int i = 0; i < channelNames.Count; i++)
                {
                    if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                    {
                        _liveChart.SetHighSpeedChannel(i, true);
                        break;
                    }
                }

            }
            _liveChart.ScrollToLive();
            WireScaleOverlayEvents();
            BuildScaleMaxOverlays();
        }



        private MultiChannelLiveChart.Channel CreateChannel(string name, string colorHex, string scaleText, string unit)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Color channelColor = ColorTranslator.FromHtml(colorHex);

            int min = -100;
            int max = 100; // default

            if (!string.IsNullOrWhiteSpace(scaleText))
            {
                string digits = new string(scaleText.Where(char.IsDigit).ToArray());

                if (int.TryParse(digits, out int parsedMin))
                    min = parsedMin;

                if (int.TryParse(digits, out int parsedMax))
                    max = parsedMax;
                min = -max;
            }

            return new MultiChannelLiveChart.Channel(name, channelColor, min, max, unit);
        }


        private ScaleAndColorModel LoadScaleAndColorModel(string setupName)
        {
            //string folder = Path.Combine(Application.StartupPath, "Saved Data", "ScaleAndColorSetup");
            string folder = AppPathManager.GetFolderPath("ScaleAndColorSetup");

            // make sure the folder exists
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, setupName + ".dat");

            // if requested file not found → try latest available
            if (!File.Exists(filePath))
            {
                var files = Directory.GetFiles(folder, "*.dat")
                                     .OrderByDescending(f => File.GetLastWriteTime(f))
                                     .ToList();

                if (files.Count == 0)
                    return null; // no saved setups at all

                filePath = files[0]; // pick the newest
            }

            try
            {
                // read and decrypt
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string jsonData = CryptoHelper.Decrypt(encryptedData);

                if (string.IsNullOrWhiteSpace(jsonData))
                    return null;

                jsonData = FixJsonForIntFields(jsonData);

                // deserialize into your model
                var model = System.Text.Json.JsonSerializer.Deserialize<ScaleAndColorModel>(jsonData);

                return model;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Scale And Color Setup: " + ex.Message);
                return null;
            }
        }

        //Start Code For Solve the "ScaleAndColorSetup" Error show every screen show this error now resolve add on 16-02-2026  //jsonData = FixJsonForIntFields(jsonData); add this in two methods
        private string FixJsonForIntFields(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            try
            {
                // Fix BladderSensation field
                json = FixJsonIntField(json, "BladderSensation");

                // Fix GeneralPurose field (note the spelling in your JSON)
                json = FixJsonIntField(json, "GeneralPurose");

                // Fix ResponeMarkers field (note the spelling in your JSON)
                json = FixJsonIntField(json, "ResponeMarkers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fixing JSON: {ex.Message}");
            }

            return json;
        }

        private string FixJsonIntField(string json, string fieldName)
        {
            // Pattern: "fieldName": "something" or "fieldName": something
            // This regex looks for the field name followed by a colon and then captures the value

            // Try quoted string value first: "fieldName": "value"
            string quotedPattern = "\"" + fieldName + "\"\\s*:\\s*\"([^\"]*)\"";
            var quotedMatch = System.Text.RegularExpressions.Regex.Match(json, quotedPattern);

            if (quotedMatch.Success)
            {
                string stringValue = quotedMatch.Groups[1].Value;
                int intValue = Color.Magenta.ToArgb(); // Default

                // Try to parse the string to int
                if (int.TryParse(stringValue, out int parsed))
                {
                    intValue = parsed;
                }
                else
                {
                    // If it's a color name or hex, try to parse it
                    try
                    {
                        Color color = ColorTranslator.FromHtml(stringValue);
                        intValue = color.ToArgb();
                    }
                    catch
                    {
                        // Keep default
                    }
                }

                // Replace with unquoted number: "fieldName": 12345
                string replacement = "\"" + fieldName + "\":" + intValue;
                json = System.Text.RegularExpressions.Regex.Replace(json, quotedPattern, replacement);
            }
            else
            {
                // Try unquoted value: "fieldName": value
                string unquotedPattern = "\"" + fieldName + "\"\\s*:\\s*([^,\\s}]+)";
                var unquotedMatch = System.Text.RegularExpressions.Regex.Match(json, unquotedPattern);

                if (unquotedMatch.Success)
                {
                    string valueStr = unquotedMatch.Groups[1].Value;

                    // If it's not a number, replace it
                    if (!int.TryParse(valueStr, out _))
                    {
                        string replacement = "\"" + fieldName + "\":" + Color.Magenta.ToArgb();
                        json = System.Text.RegularExpressions.Regex.Replace(json, unquotedPattern, replacement);
                    }
                }
            }

            return json;
        }
        //End Code For Solve the "ScaleAndColorSetup" Error show every screen show this error now resolve add on 16-02-2026 //jsonData = FixJsonForIntFields(jsonData); add this two methods


        //Start Graph Code
        private void CreateMainContent()
        {
            // === root canvas ===
            mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(0, 80, 0, 0),   // top space for menus/toolbars if any
            };
            Controls.Add(mainContentPanel);

            // ensure right sidebar exists
            CreateRightSidebar(); // must assign to 'rightSidebarPanel' inside this
            if (rightSidebarPanel == null && rightSidebarPanelForPatient != null)
                rightSidebarPanel = rightSidebarPanelForPatient;

            // right panel should fill its cell
            if (rightSidebarPanel != null)
            {
                rightSidebarPanel.Dock = DockStyle.Fill;
                rightSidebarPanel.MinimumSize = new Size(445, 0);
            }

            // === top command bar (replaces fixed button Locations) ===
            var cmdBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(12, 108, 12, 8),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false
            };

            _btnStart = new Button { Text = "Start", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnPause = new Button { Text = "Pause", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnResume = new Button { Text = "Resume", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnStop = new Button { Text = "Stop", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnSave = new Button { Text = "Save", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnMarker = new Button { Text = "Marker", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };

            cmdBar.Controls.AddRange(new Control[] { _btnStart, _btnPause, _btnResume, _btnStop, _btnSave, _btnMarker });

            // === center split: graph (stretch) + sidebar (fixed) ===
            var contentSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F)); //Old is 620F this code for increase the graph width and reduce the camera size change on 18-12-2025

            // graph host fills its cell
            panelGraph = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 0),
                BackColor = Color.White
            };

            // live chart
            _liveChart = new MultiChannelLiveChart { Dock = DockStyle.Fill };
            panelGraph.Controls.Add(_liveChart);
            _liveChart.Resize += delegate { RepositionScaleCombos(); };
            // overlay timer pinned top-right of the graph

            int rightOffset = 1;
            int topOffset = 12;

            _lblTimer = new Label
            {
                Text = "00:00",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                //Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelGraph.ClientSize.Width - 80, 8)
            };
            panelGraph.Controls.Add(_lblTimer);
            _lblTimer.BringToFront();

            // Hide Time 00.00
            _lblTimer.Visible = false;

            panelGraph.Resize += (_, __) =>
            {
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;
            };

            // --- Timer Setup ---
            //_testTimer = new Timer { Interval = 1000 };
            //_testTimer.Tick += (s, e) => UpdateTimerLabel();
            //_testTimer.Start();

            //UpdateTimerLabel();
            //UpdateButtons(recording: true, paused: false, canSave: false);

            // configure lanes/graph
            //var setup = LoadScaleAndColorModel("DefaultSetup");
            var setup = LoadScaleAndColorModel("DefaultSetup");
            if (setup != null)
            {
                _testMgr = new TestChannelManager(setup);
                ConfigureChartLanes(setup);
            }

            int infusionTopOffset = 42;   // second line under timer (adjust if needed)

            panelGraph.Resize += (_, __) =>
            {
                // Timer position (already correct)
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;

                _lblInfusion.Top = _lblTimer.Bottom - 37;
                _lblInfusion.Left = panelGraph.ClientSize.Width - _lblInfusion.Width - rightOffset;
            };


            _lblInfusion = new Label
            {
                Text = "Rate: 0 ml/min",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelGraph.Controls.Add(_lblInfusion);
            _lblInfusion.BringToFront();

            _lblInfusion.Visible = false;

            LoadGraph();
            DisableMenuStripItems();


            // wire clicks (keeping your existing handlers)
            if (_btnMarker != null)
            {
                _btnMarker.Click += btnFS_Click;
                _btnMarker.Click += btnFD_Click;
                _btnMarker.Click += btnND_Click;
                _btnMarker.Click += btnSD_Click;
                _btnMarker.Click += btnBC_Click;

                _btnMarker.Click += btnR1_Click;
                _btnMarker.Click += btnR2_Click;
                _btnMarker.Click += btnArtifactR1_Click;
                _btnMarker.Click += btnArtifactR2_Click;
                _btnMarker.Click += btnMarkingR1_Click;
                _btnMarker.Click += btnMarkingR2_Click;

                _btnMarker.Click += btnM_Click;
                _btnMarker.Click += btnMarkingM_Click;
                _btnMarker.Click += btnArtifactM_Click;
            }


            if (_btnStart != null)
            {
                _btnStart.Click += btnTestStart1_Click;
                _btnStart.Click += btnTestStart2_Click;
                _btnStart.Click += btnTestStart3_Click;
            }
            if (_btnPause != null)
            {
                _btnPause.Click += btnTestPause1_Click;
                _btnPause.Click += btnTestPause2_Click;
                _btnPause.Click += btnTestPause3_Click;
            }
            if (_btnStop != null)
            {
                _btnStop.Click += btnTestStop1_Click;
                _btnStop.Click += btnTestStop2_Click;
                _btnStop.Click += btnTestStop3_Click;
            }
            if (_btnSave != null)
            {
                _btnSave.Click += btnTestSave1_Click;
                _btnSave.Click += btnTestSave2_Click;
                _btnSave.Click += btnTestSave3_Click;

                _btnSave.Click += btnS1_Click;
                _btnSave.Click += btnS2_Click;
                _btnSave.Click += btnGraphS1_Click;
                _btnSave.Click += btnGraphS2_Click;
                _btnSave.Click += btnSelectGraphS1_Click;
                _btnSave.Click += btnSelectGraphS2_Click;

                _btnSave.Click += btnEventLE_Click;
                _btnSave.Click += btnEventLE3_Click;
                _btnSave.Click += btnEventCU_Click;
                _btnSave.Click += btnEventCU3_Click;
                _btnSave.Click += btnEventUDC_Click;
                _btnSave.Click += btnEventUDC3_Click;

                _btnSave.Click += btnRF_Click;
            }

            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += (s, e) => UpdateTimerLabel();

            // mount UI (TOP: commands, FILL: split)
            mainContentPanel.Controls.Add(contentSplit);
            mainContentPanel.Controls.Add(cmdBar);

            contentSplit.Controls.Add(panelGraph, 0, 0);
            if (rightSidebarPanel != null)
                contentSplit.Controls.Add(rightSidebarPanel, 1, 0);

            StartCamera();
            //StartSelectedCamera();
        }

        private void ReviewModeStopCamera()
        {
            // === root canvas ===
            mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(0, 80, 0, 0),   // top space for menus/toolbars if any
            };
            Controls.Add(mainContentPanel);

            // ensure right sidebar exists
            CreateRightStopCamera();
            if (rightSidebarPanel == null && rightSidebarPanelForPatient != null)
                rightSidebarPanel = rightSidebarPanelForPatient;

            // right panel should fill its cell
            if (rightSidebarPanel != null)
            {
                rightSidebarPanel.Dock = DockStyle.Fill;
                rightSidebarPanel.MinimumSize = new Size(445, 0);
            }

            // === top command bar (replaces fixed button Locations) ===
            var cmdBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(12, 108, 12, 8),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false
            };

            _btnStart = new Button { Text = "Start", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnPause = new Button { Text = "Pause", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnResume = new Button { Text = "Resume", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnStop = new Button { Text = "Stop", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnSave = new Button { Text = "Save", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnMarker = new Button { Text = "Marker", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };

            cmdBar.Controls.AddRange(new Control[] { _btnStart, _btnPause, _btnResume, _btnStop, _btnSave, _btnMarker });

            // === center split: graph (stretch) + sidebar (fixed) ===
            var contentSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F)); //Old is 620F this code for increase the graph width and reduce the camera size change on 18-12-2025

            // graph host fills its cell
            panelGraph = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 0),
                BackColor = Color.White
            };

            // live chart
            _liveChart = new MultiChannelLiveChart { Dock = DockStyle.Fill };
            panelGraph.Controls.Add(_liveChart);
            _liveChart.Resize += delegate { RepositionScaleCombos(); };
            // overlay timer pinned top-right of the graph

            int rightOffset = 1;
            int topOffset = 12;

            _lblTimer = new Label
            {
                Text = "00:00",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                //Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelGraph.ClientSize.Width - 80, 8)
            };
            panelGraph.Controls.Add(_lblTimer);
            _lblTimer.BringToFront();

            // Hide Time 00.00
            _lblTimer.Visible = false;

            panelGraph.Resize += (_, __) =>
            {
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;
            };


            var setup = LoadScaleAndColorModel("DefaultSetup");
            if (setup != null)
            {
                _testMgr = new TestChannelManager(setup);
                ConfigureChartLanes(setup);
            }

            int infusionTopOffset = 42;

            panelGraph.Resize += (_, __) =>
            {
                // Timer position (already correct)
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;

                _lblInfusion.Top = _lblTimer.Bottom - 37;
                _lblInfusion.Left = panelGraph.ClientSize.Width - _lblInfusion.Width - rightOffset;
            };


            _lblInfusion = new Label
            {
                Text = "Rate: 0 ml/min",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelGraph.Controls.Add(_lblInfusion);
            _lblInfusion.BringToFront();

            _lblInfusion.Visible = false;

            LoadGraph();
            DisableMenuStripItems();


            // wire clicks (keeping your existing handlers)
            if (_btnMarker != null)
            {
                _btnMarker.Click += btnFS_Click;
                _btnMarker.Click += btnFD_Click;
                _btnMarker.Click += btnND_Click;
                _btnMarker.Click += btnSD_Click;
                _btnMarker.Click += btnBC_Click;

                _btnMarker.Click += btnR1_Click;
                _btnMarker.Click += btnR2_Click;
                _btnMarker.Click += btnArtifactR1_Click;
                _btnMarker.Click += btnArtifactR2_Click;
                _btnMarker.Click += btnMarkingR1_Click;
                _btnMarker.Click += btnMarkingR2_Click;

                _btnMarker.Click += btnM_Click;
                _btnMarker.Click += btnMarkingM_Click;
                _btnMarker.Click += btnArtifactM_Click;
            }


            if (_btnStart != null)
            {
                _btnStart.Click += btnTestStart1_Click;
                _btnStart.Click += btnTestStart2_Click;
                _btnStart.Click += btnTestStart3_Click;
            }
            if (_btnPause != null)
            {
                _btnPause.Click += btnTestPause1_Click;
                _btnPause.Click += btnTestPause2_Click;
                _btnPause.Click += btnTestPause3_Click;
            }
            if (_btnStop != null)
            {
                _btnStop.Click += btnTestStop1_Click;
                _btnStop.Click += btnTestStop2_Click;
                _btnStop.Click += btnTestStop3_Click;
            }
            if (_btnSave != null)
            {
                _btnSave.Click += btnTestSave1_Click;
                _btnSave.Click += btnTestSave2_Click;
                _btnSave.Click += btnTestSave3_Click;

                _btnSave.Click += btnS1_Click;
                _btnSave.Click += btnS2_Click;
                _btnSave.Click += btnGraphS1_Click;
                _btnSave.Click += btnGraphS2_Click;
                _btnSave.Click += btnSelectGraphS1_Click;
                _btnSave.Click += btnSelectGraphS2_Click;

                _btnSave.Click += btnEventLE_Click;
                _btnSave.Click += btnEventLE3_Click;
                _btnSave.Click += btnEventCU_Click;
                _btnSave.Click += btnEventCU3_Click;
                _btnSave.Click += btnEventUDC_Click;
                _btnSave.Click += btnEventUDC3_Click;

                _btnSave.Click += btnRF_Click;
            }

            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += (s, e) => UpdateTimerLabel();

            // mount UI (TOP: commands, FILL: split)
            mainContentPanel.Controls.Add(contentSplit);
            mainContentPanel.Controls.Add(cmdBar);

            contentSplit.Controls.Add(panelGraph, 0, 0);
            if (rightSidebarPanel != null)
                contentSplit.Controls.Add(rightSidebarPanel, 1, 0);
        }


        //private void CreateRightStopCamera()
        //{
        //    rightSidebarPanel = new Panel();
        //    rightSidebarPanel.Dock = DockStyle.Fill;            // fills its table cell
        //    rightSidebarPanel.BackColor = Color.FromArgb(248, 249, 250);
        //    rightSidebarPanel.Padding = new Padding(12);
        //    //rightSidebarPanel.Padding = new Padding(170, 0, 5, 0);

        //    // ===== Sidebar grid: Preview (auto) + Buttons (auto) + Thumbs (fill) =====
        //    TableLayoutPanel sidebarGrid = new TableLayoutPanel();
        //    sidebarGrid.Dock = DockStyle.Fill;
        //    sidebarGrid.ColumnCount = 1;
        //    sidebarGrid.RowCount = 3;
        //    sidebarGrid.BackColor = Color.Transparent;
        //    sidebarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        //    sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // preview host
        //    sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // buttons
        //    sidebarGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));// thumbnails

        //    // ---- Camera preview (keeps ~4:3) ----
        //    previewHost = new Panel();
        //    previewHost.Dock = DockStyle.Top;
        //    previewHost.BackColor = rightSidebarPanel.BackColor; ;
        //    previewHost.Height = 445; // initial; will be adjusted on resize

        //    cameraPreview = new PictureBox();
        //    cameraPreview.Dock = DockStyle.Fill;
        //    cameraPreview.BackColor = rightSidebarPanel.BackColor; ;
        //    cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
        //    previewHost.Controls.Add(cameraPreview);

        //    // ---- Camera buttons (wrap if narrow) ----
        //    FlowLayoutPanel buttonRow = new FlowLayoutPanel();
        //    buttonRow.Dock = DockStyle.Top;
        //    buttonRow.FlowDirection = FlowDirection.LeftToRight;
        //    buttonRow.WrapContents = true;
        //    buttonRow.AutoSize = true;
        //    buttonRow.Padding = new Padding(0, 2, 0, 0);
        //    buttonRow.BackColor = Color.Transparent;

        //    captureButton = new Button();
        //    captureButton.Text = "📸 Capture";
        //    captureButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        //    captureButton.BackColor = Color.FromArgb(0, 73, 165);
        //    captureButton.ForeColor = Color.White;
        //    captureButton.FlatStyle = FlatStyle.Flat;
        //    captureButton.Width = 413;
        //    captureButton.Height = 35;
        //    captureButton.Margin = new Padding(0, 0, 5, 0);
        //    captureButton.Enabled = false;
        //    captureButton.FlatAppearance.BorderSize = 0;
        //    captureButton.Click += new EventHandler(CaptureButton_Click);




        //    buttonRow.Controls.Add(captureButton);

        //    // ---- Thumbnails list (fills remaining space) ----
        //    capturedImagesPanel = new FlowLayoutPanel();
        //    capturedImagesPanel.Dock = DockStyle.Fill;
        //    capturedImagesPanel.AutoScroll = true;
        //    capturedImagesPanel.WrapContents = true;
        //    capturedImagesPanel.FlowDirection = FlowDirection.LeftToRight;
        //    capturedImagesPanel.Padding = new Padding(0, 0, 0, 0);
        //    capturedImagesPanel.Margin = new Padding(0);

        //    // mount
        //    sidebarGrid.Controls.Add(previewHost, 0, 0);
        //    sidebarGrid.Controls.Add(buttonRow, 0, 1);
        //    sidebarGrid.Controls.Add(capturedImagesPanel, 0, 2);
        //    rightSidebarPanel.Controls.Add(sidebarGrid);

        //    rightSidebarPanel.Resize += new EventHandler(RightSidebarPanel_Resize);
        //    capturedImagesPanel.SizeChanged += new EventHandler(CapturedImagesPanel_SizeChanged);

        //    // initial layout pass
        //    UpdatePreviewSize();
        //    UpdateThumbSizes();
        //}



        // ================= IMAGE PREVIEW HELPERS =================

        private void ShowImageInCameraPreview(Image image)
        {
            if (cameraPreview == null || image == null)
                return;

            cameraPreview.Image?.Dispose();
            cameraPreview.Image = new Bitmap(image);
        }

        private Image CreatePlaceholderImage(string text)
        {
            Bitmap bmp = new Bitmap(400, 300);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                using (Font f = new Font("Segoe UI", 14, FontStyle.Bold))
                using (Brush b = new SolidBrush(Color.White))
                {
                    SizeF size = g.MeasureString(text, f);
                    g.DrawString(text, f, b,
                        (bmp.Width - size.Width) / 2,
                        (bmp.Height - size.Height) / 2);
                }
            }
            return bmp;
        }

        private void CreateRightStopCamera()
        {
            rightSidebarPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(12)
            };

            TableLayoutPanel sidebarGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // preview
            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // input controls
            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));// thumbnails

            // ===== PREVIEW =====
            previewHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 245
            };

            cameraPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = CreatePlaceholderImage("No Image")
            };

            previewHost.Controls.Add(cameraPreview);

            // ===== TEXTBOX + CHECKBOX (REPLACES CAPTURE BUTTON) =====
            FlowLayoutPanel inputRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 5)
            };

            TextBox txtRemark = new TextBox
            {
                Width = 260,
                ForeColor = Color.Gray,
                Text = "Enter remark..."
            };

            txtRemark.GotFocus += (s, e) =>
            {
                if (txtRemark.Text == "Enter remark...")
                {
                    txtRemark.Text = "";
                    txtRemark.ForeColor = Color.Black;
                }
            };

            txtRemark.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtRemark.Text))
                {
                    txtRemark.Text = "Enter remark...";
                    txtRemark.ForeColor = Color.Gray;
                }
            };

            CheckBox chkImportant = new CheckBox
            {
                Text = "Include In Report",
                AutoSize = true,
                Margin = new Padding(10, 5, 0, 0)
            };

            inputRow.Controls.Add(txtRemark);
            inputRow.Controls.Add(chkImportant);

            // ===== THUMBNAILS =====
            capturedImagesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            sidebarGrid.Controls.Add(previewHost, 0, 0);
            sidebarGrid.Controls.Add(inputRow, 0, 1);
            sidebarGrid.Controls.Add(capturedImagesPanel, 0, 2);

            rightSidebarPanel.Controls.Add(sidebarGrid);
        }


        private void AddThumbnailToPanelImage(Image captured)
        {
            Panel thumbPanel = new Panel
            {
                Size = new Size(40, 60),
                Margin = new Padding(3)
            };

            PictureBox thumbnail = new PictureBox
            {
                Image = captured,
                Size = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            // ⭐ CLICK → SHOW IN CAMERA SCREEN
            thumbnail.Click += (s, e) =>
            {
                SaveCurrentImageData();
                ShowImageInCameraPreview(captured);
                LoadImageData(captured);
            };

            Label countLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 15,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 250),
                Text = (capturedImagesPanel.Controls.Count + 1).ToString()
            };

            thumbPanel.Controls.Add(thumbnail);
            thumbPanel.Controls.Add(countLabel);

            capturedImagesPanel.Controls.Add(thumbPanel);
        }

        //---
        public class ImageNoteData
        {
            public string Remark { get; set; } = "";
            public bool IncludeInReport { get; set; } = false;
        }



        private Dictionary<Image, ImageNoteData> _imageNotesMap =
      new Dictionary<Image, ImageNoteData>();

        private Image _currentImage = null;

        private TextBox txtRemark;
        private CheckBox chkImportant;



        private void SaveCurrentImageData()
        {
            if (_currentImage == null || txtRemark == null || chkImportant == null)
                return;

            if (!_imageNotesMap.ContainsKey(_currentImage))
                _imageNotesMap[_currentImage] = new ImageNoteData();

            _imageNotesMap[_currentImage].Remark =
                txtRemark.Text == "Enter remark..." ? "" : txtRemark.Text;

            _imageNotesMap[_currentImage].IncludeInReport =
                chkImportant.Checked;
        }


        private void LoadImageData(Image image)
        {
            if (txtRemark == null || chkImportant == null)
                return;

            _currentImage = image;

            if (!_imageNotesMap.ContainsKey(image))
                _imageNotesMap[image] = new ImageNoteData();

            var note = _imageNotesMap[image];

            // Load text
            if (string.IsNullOrWhiteSpace(note.Remark))
            {
                txtRemark.Text = "Enter remark...";
                txtRemark.ForeColor = Color.Gray;
            }
            else
            {
                txtRemark.Text = note.Remark;
                txtRemark.ForeColor = Color.Black;
            }

            // Load checkbox
            chkImportant.Checked = note.IncludeInReport;
        }

        //---









        private Label _lblInfusion;

        private void UpdateInfusionLabel()
        {
            if (_lblInfusion == null) return;

            _lblInfusion.Text = $"Rate: {_infusionRate} ml/min";
        }


        //Graph Screen
        private void CreateMainContentOnlyGraph()
        {
            mainContentPanelGraphOnly = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(0, 80, 0, 0),
            };
            Controls.Add(mainContentPanelGraphOnly);


            // top command bar
            var cmdBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(12, 108, 12, 8),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false
            };

            _btnStart = new Button { Text = "Start", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnPause = new Button { Text = "Pause", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnResume = new Button { Text = "Resume", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnStop = new Button { Text = "Stop", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnSave = new Button { Text = "Save", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            _btnMarker = new Button { Text = "Marker", Width = 88, Height = 30, Margin = new Padding(0, 0, 8, 0) };
            cmdBar.Controls.AddRange(new Control[] { _btnStart, _btnPause, _btnResume, _btnStop, _btnSave, _btnMarker });

            // single-cell layout just for the graph
            var contentSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1
            };
            contentSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            panelGraph = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 0),
                BackColor = Color.White
            };

            _liveChart = new MultiChannelLiveChart { Dock = DockStyle.Fill };
            panelGraph.Controls.Add(_liveChart);
            _liveChart.Resize += delegate { RepositionScaleCombos(); };

            //Code For Change Timer Location
            int rightOffset = 1;
            int topOffset = 20;

            _lblTimer = new Label
            {
                Text = "00:00",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                //Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelGraph.Controls.Add(_lblTimer);

            // Hide Time 00.00
            _lblTimer.Visible = false;

            _lblTimer.BringToFront();
            panelGraph.Resize += (_, __) =>
            {
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;
            };

            var setup = LoadScaleAndColorModel("DefaultSetup");
            if (setup != null) ConfigureChartLanes(setup);

            int infusionTopOffset = 42;   // second line under timer (adjust if needed)

            panelGraph.Resize += (_, __) =>
            {
                // Timer position (already correct)
                _lblTimer.Top = topOffset;
                _lblTimer.Left = panelGraph.ClientSize.Width - _lblTimer.Width - rightOffset;

                _lblInfusion.Top = _lblTimer.Bottom - 45;
                _lblInfusion.Left = panelGraph.ClientSize.Width - _lblInfusion.Width - rightOffset;
            };


            _lblInfusion = new Label
            {
                Text = "Rate: 0 ml/min",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(32, 255, 255, 255),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelGraph.Controls.Add(_lblInfusion);
            _lblInfusion.BringToFront();

            _lblInfusion.Visible = false;

            LoadGraph();
            DisableMenuStripItems();

            // wire events (same as above — keep your handlers)
            if (_btnMarker != null)
            {
                _btnMarker.Click += btnFS_Click;
                _btnMarker.Click += btnFD_Click;
                _btnMarker.Click += btnND_Click;
                _btnMarker.Click += btnSD_Click;
                _btnMarker.Click += btnBC_Click;

                _btnMarker.Click += btnR1_Click;
                _btnMarker.Click += btnR2_Click;
                _btnMarker.Click += btnArtifactR1_Click;
                _btnMarker.Click += btnArtifactR2_Click;
                _btnMarker.Click += btnMarkingR1_Click;
                _btnMarker.Click += btnMarkingR2_Click;

                _btnMarker.Click += btnM_Click;
                _btnMarker.Click += btnMarkingM_Click;
                _btnMarker.Click += btnArtifactM_Click;
            }

            if (_btnStart != null)
            {
                _btnStart.Click += btnTestStart1_Click;
                _btnStart.Click += btnTestStart2_Click;
                _btnStart.Click += btnTestStart3_Click;
            }
            if (_btnPause != null)
            {
                _btnPause.Click += btnTestPause1_Click;
                _btnPause.Click += btnTestPause2_Click;
                _btnPause.Click += btnTestPause3_Click;
            }
            if (_btnStop != null)
            {
                _btnStop.Click += btnTestStop1_Click;
                _btnStop.Click += btnTestStop2_Click;
                _btnStop.Click += btnTestStop3_Click;
            }
            if (_btnSave != null)
            {
                _btnSave.Click += btnTestSave1_Click;
                _btnSave.Click += btnTestSave2_Click;
                _btnSave.Click += btnTestSave3_Click;

                _btnSave.Click += btnS1_Click;
                _btnSave.Click += btnS2_Click;
                _btnSave.Click += btnGraphS1_Click;
                _btnSave.Click += btnGraphS2_Click;
                _btnSave.Click += btnSelectGraphS1_Click;
                _btnSave.Click += btnSelectGraphS2_Click;

                _btnSave.Click += btnEventLE_Click;
                _btnSave.Click += btnEventLE3_Click;
                _btnSave.Click += btnEventCU_Click;
                _btnSave.Click += btnEventCU3_Click;
                _btnSave.Click += btnEventUDC_Click;
                _btnSave.Click += btnEventUDC3_Click;
            }

            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += (s, e) => UpdateTimerLabel();

            mainContentPanelGraphOnly.Controls.Add(contentSplit);
            mainContentPanelGraphOnly.Controls.Add(cmdBar);
            contentSplit.Controls.Add(panelGraph, 0, 0);
        }

        //End Graph Code 09/09/2025






        private void ButtonForPatient(object sender, EventArgs e)
        {
            var patientWithTestForm = new PatientWithTestForm();
            ScreenDimOverlay.ShowDialogWithDim(patientWithTestForm, alpha: 150);
        }


        //Start Code For Camera
        private PictureBox cameraPreview;
        private Button captureButton;
        private FlowLayoutPanel capturedImagesPanel;
        private Image dummyImage;
        private Button toggleCameraButton;
        private Button brightnessButton;
        private Button contrastButton;
        private bool isCameraOn = false;

        private TrackBar brightnessSlider;
        private Form brightnessForm;
        private Image originalImage;
        // private Button _btnMarker;
        private Label _lblTimer;
        private Button _btnStart, _btnPause, _btnResume, _btnStop, _btnSave, _btnOpen, _btnMarker;

        private void BrightnessButton_Click(object sender, EventArgs e)
        {
            if (cameraPreview.Image == null) return;
            originalImage = (Image)cameraPreview.Image.Clone();

            brightnessForm = new Form
            {
                Text = "Adjust Brightness",
                Size = new Size(300, 100),
                StartPosition = FormStartPosition.CenterParent
            };

            brightnessSlider = new TrackBar
            {
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10,
                Dock = DockStyle.Fill
            };

            brightnessSlider.Scroll += BrightnessSlider_Scroll;

            brightnessForm.Controls.Add(brightnessSlider);
            brightnessForm.ShowDialog();
        }

        private void BrightnessSlider_Scroll(object sender, EventArgs e)
        {
            if (originalImage == null) return;

            float brightnessValue = brightnessSlider.Value / 100f;

            cameraPreview.Image = AdjustBrightness(originalImage, brightnessValue);
        }

        private Bitmap AdjustBrightness(Image image, float brightness)
        {
            float b = brightness;

            float[][] ptsArray ={
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {b, b, b, 0, 1}
        };

            ColorMatrix colorMatrix = new ColorMatrix(ptsArray);
            ImageAttributes imgAttr = new ImageAttributes();
            imgAttr.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            Bitmap newBitmap = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.DrawImage(image, new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgAttr);
            }

            return newBitmap;
        }

        private void CreateRightSidebar1()
        {
            rightSidebarPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15)
            };

            // Camera Preview
            cameraPreview = new PictureBox();
            cameraPreview.Size = new Size(404, 397);
            cameraPreview.BackColor = Color.Black;
            cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
            cameraPreview.Location = new Point(0, 0);
            rightSidebarPanel.Controls.Add(cameraPreview);

            // Capture Button
            captureButton = new Button();
            captureButton.Text = "📸 Capture";
            captureButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            captureButton.BackColor = Color.FromArgb(0, 73, 165);
            captureButton.ForeColor = Color.White;
            captureButton.FlatStyle = FlatStyle.Flat;
            captureButton.Size = new Size(150, 40);
            captureButton.Location = new Point(3, 400);
            captureButton.Click += CaptureButton_Click;
            captureButton.Enabled = false;
            rightSidebarPanel.Controls.Add(captureButton);

            //Camera Brightness Button
            brightnessButton = new Button();
            brightnessButton.Text = "🎥 Brightness";
            brightnessButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            brightnessButton.BackColor = Color.YellowGreen;
            brightnessButton.ForeColor = Color.White;
            brightnessButton.FlatStyle = FlatStyle.Flat;
            brightnessButton.Size = new Size(120, 40);
            brightnessButton.Location = new Point(155, 400);
            brightnessButton.Click += BrightnessButton_Click;
            rightSidebarPanel.Controls.Add(brightnessButton);

            //Camera Brightness Button
            contrastButton = new Button();
            contrastButton.Text = "🎥 Contrast";
            contrastButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            contrastButton.BackColor = Color.Orange;
            contrastButton.ForeColor = Color.White;
            contrastButton.FlatStyle = FlatStyle.Flat;
            contrastButton.Size = new Size(120, 40);
            contrastButton.Location = new Point(277, 400);
            contrastButton.Click += BrightnessButton_Click;
            rightSidebarPanel.Controls.Add(contrastButton);



            // Thumbnails panel
            capturedImagesPanel = new FlowLayoutPanel();
            capturedImagesPanel.Location = new Point(0, 440);
            capturedImagesPanel.Size = new Size(404, 150);
            capturedImagesPanel.AutoScroll = true;
            capturedImagesPanel.WrapContents = true;
            capturedImagesPanel.FlowDirection = FlowDirection.TopDown;
            rightSidebarPanel.Controls.Add(capturedImagesPanel);
        }

        // Put these fields at class level if you don't already have them:

        private Panel previewHost;

        //Camera Screen Design Code
        private void CreateRightSidebar()
        {
            rightSidebarPanel = new Panel();
            rightSidebarPanel.Dock = DockStyle.Fill;            // fills its table cell
            rightSidebarPanel.BackColor = Color.FromArgb(248, 249, 250);
            rightSidebarPanel.Padding = new Padding(12);
            //rightSidebarPanel.Padding = new Padding(170, 0, 5, 0);

            // ===== Sidebar grid: Preview (auto) + Buttons (auto) + Thumbs (fill) =====
            TableLayoutPanel sidebarGrid = new TableLayoutPanel();
            sidebarGrid.Dock = DockStyle.Fill;
            sidebarGrid.ColumnCount = 1;
            sidebarGrid.RowCount = 3;
            sidebarGrid.BackColor = Color.Transparent;
            sidebarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // preview host
            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // buttons
            sidebarGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));// thumbnails

            // ---- Camera preview (keeps ~4:3) ----
            previewHost = new Panel();
            previewHost.Dock = DockStyle.Top;
            previewHost.BackColor = rightSidebarPanel.BackColor; ;
            previewHost.Height = 445; // initial; will be adjusted on resize

            cameraPreview = new PictureBox();
            cameraPreview.Dock = DockStyle.Fill;
            cameraPreview.BackColor = rightSidebarPanel.BackColor; ;
            cameraPreview.SizeMode = PictureBoxSizeMode.StretchImage;
            previewHost.Controls.Add(cameraPreview);

            // ---- Camera buttons (wrap if narrow) ----
            FlowLayoutPanel buttonRow = new FlowLayoutPanel();
            buttonRow.Dock = DockStyle.Top;
            buttonRow.FlowDirection = FlowDirection.LeftToRight;
            buttonRow.WrapContents = true;
            buttonRow.AutoSize = true;
            buttonRow.Padding = new Padding(0, 2, 0, 0);
            buttonRow.BackColor = Color.Transparent;

            captureButton = new Button();
            captureButton.Text = "📸 Capture";
            captureButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            captureButton.BackColor = Color.FromArgb(0, 73, 165);
            captureButton.ForeColor = Color.White;
            captureButton.FlatStyle = FlatStyle.Flat;
            captureButton.Width = 413;
            captureButton.Height = 35;
            captureButton.Margin = new Padding(0, 0, 5, 0);
            captureButton.Enabled = false;
            captureButton.FlatAppearance.BorderSize = 0;
            captureButton.Click += new EventHandler(CaptureButton_Click);

            //brightnessButton = new Button();
            //brightnessButton.Text = "🎥 Brightness";
            //brightnessButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            //brightnessButton.BackColor = Color.YellowGreen;
            //brightnessButton.ForeColor = Color.White;
            //brightnessButton.FlatStyle = FlatStyle.Flat;
            //brightnessButton.Width = 187;
            //brightnessButton.Height = 40;
            //brightnessButton.Margin = new Padding(0, 0, 5, 8);
            //brightnessButton.FlatAppearance.BorderSize = 0;
            //brightnessButton.Click += new EventHandler(BrightnessButton_Click);

            //contrastButton = new Button();
            //contrastButton.Text = "🎥 Contrast";
            //contrastButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            //contrastButton.BackColor = Color.Orange;
            //contrastButton.ForeColor = Color.White;
            //contrastButton.FlatStyle = FlatStyle.Flat;
            //contrastButton.Width = 187;
            //contrastButton.Height = 40;
            //contrastButton.Margin = new Padding(0, 0, 5, 8);
            //contrastButton.FlatAppearance.BorderSize = 0;


            buttonRow.Controls.Add(captureButton);
            //buttonRow.Controls.Add(brightnessButton);
            //buttonRow.Controls.Add(contrastButton);

            // ---- Thumbnails list (fills remaining space) ----
            capturedImagesPanel = new FlowLayoutPanel();
            capturedImagesPanel.Dock = DockStyle.Fill;
            capturedImagesPanel.AutoScroll = true;
            capturedImagesPanel.WrapContents = true;
            capturedImagesPanel.FlowDirection = FlowDirection.LeftToRight;
            capturedImagesPanel.Padding = new Padding(0, 0, 0, 0);
            capturedImagesPanel.Margin = new Padding(0);

            // mount
            sidebarGrid.Controls.Add(previewHost, 0, 0);
            sidebarGrid.Controls.Add(buttonRow, 0, 1);
            sidebarGrid.Controls.Add(capturedImagesPanel, 0, 2);
            rightSidebarPanel.Controls.Add(sidebarGrid);

            // hook resize handlers (older C# doesn’t use local functions)
            rightSidebarPanel.Resize += new EventHandler(RightSidebarPanel_Resize);
            capturedImagesPanel.SizeChanged += new EventHandler(CapturedImagesPanel_SizeChanged);

            // initial layout pass
            UpdatePreviewSize();
            UpdateThumbSizes();
        }

        // === helpers compatible with older C# ===

        private void RightSidebarPanel_Resize(object sender, EventArgs e)
        {
            UpdatePreviewSize();
        }

        private void CapturedImagesPanel_SizeChanged(object sender, EventArgs e)
        {
            UpdateThumbSizes();
        }

        // Keep preview ~4:3 within sidebar width (with min/max bounds)
        private void UpdatePreviewSize()
        {
            if (rightSidebarPanel == null || previewHost == null) return;

            int w = rightSidebarPanel.ClientSize.Width - rightSidebarPanel.Padding.Left - rightSidebarPanel.Padding.Right;
            if (w < 250) w = 250;

            // 4:3 aspect ratio
            int h = (int)(w * 3.0f / 4.0f);
            if (h < 180) h = 445;
            if (h > 420) h = 445;

            previewHost.Height = h;
        }

        // Make thumbnails 2 per row (4:3) and reflow on resize
        private void UpdateThumbSizes()
        {
            if (capturedImagesPanel == null) return;

            int w = capturedImagesPanel.ClientSize.Width - capturedImagesPanel.Padding.Left - capturedImagesPanel.Padding.Right;
            if (w < 100) w = 100;

            // 2 columns with ~8px gap
            int cellW = (w - 8) / 2;
            if (cellW < 80) cellW = 80;
            int cellH = (int)(cellW * 3.0f / 4.0f);

            // iterate without LINQ for older frameworks
            for (int i = 0; i < capturedImagesPanel.Controls.Count; i++)
            {
                PictureBox pb = capturedImagesPanel.Controls[i] as PictureBox;
                if (pb != null)
                {
                    pb.Size = new Size(cellW, cellH);
                }
            }
        }


        private void ToggleCameraButton_Click(object sender, EventArgs e)
        {
            if (!isCameraOn)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += (s, args) =>
                    {
                        Bitmap frame = (Bitmap)args.Frame.Clone();
                        cameraPreview.Invoke(new Action(() =>
                        {
                            cameraPreview.Image?.Dispose();
                            cameraPreview.Image = frame;
                        }));
                    };
                    videoSource.Start();
                    isCameraOn = true;
                    toggleCameraButton.Text = "⛔ Stop Camera";
                    toggleCameraButton.BackColor = Color.Maroon;
                    captureButton.Enabled = true;
                }
                else
                {
                    MessageBox.Show("No camera detected.");
                }
            }
            else
            {
                StopCamera();
            }
        }

        public class ImagePreviewForm : Form
        {
            private PictureBox previewBox;

            public ImagePreviewForm(Image image)
            {
                this.Text = "Image Preview";
                // 4 x 4 inches => convert to pixels (at 96 DPI ≈ 384x384)
                this.Size = new Size(650, 400);
                this.StartPosition = FormStartPosition.CenterParent;

                previewBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = image,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                this.Controls.Add(previewBox);
            }
        }


        private List<Image> _capturedImages = new List<Image>();
        //This code Capture image 
        private void CaptureButton_Click(object sender, EventArgs e)
        {
            if (cameraPreview.Image != null)
            {

                Image captured = (Image)cameraPreview.Image.Clone();

                // ✅ STORE IMAGE FOR LATER SAVING
                _capturedImages.Add(captured);

                Panel thumbPanel = new Panel();
                thumbPanel.Size = new Size(40, 60);
                thumbPanel.Margin = new Padding(3);

                // Thumbnail image
                PictureBox thumbnail = new PictureBox();
                thumbnail.Image = captured;
                thumbnail.Size = new Size(40, 40);
                thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
                thumbnail.BorderStyle = BorderStyle.FixedSingle;
                thumbnail.Location = new Point(0, 0);

                thumbnail.Click += (s, ev) =>
                {
                    using (var previewForm = new ImagePreviewForm(captured))
                    {
                        previewForm.ShowDialog();
                    }
                };

                // Count label
                Label countLabel = new Label();
                countLabel.AutoSize = false;
                countLabel.TextAlign = ContentAlignment.MiddleCenter;
                countLabel.Dock = DockStyle.Bottom;
                countLabel.Height = 15;
                countLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                countLabel.ForeColor = Color.White;
                countLabel.BackColor = Color.FromArgb(50, 50, 250);
                countLabel.Padding = new Padding(10, 0, 10, 0);
                countLabel.Text = (capturedImagesPanel.Controls.Count + 1).ToString();
                //countLabel.Text = "Image " + (capturedImagesPanel.Controls.Count + 1).ToString();


                // Add image + label to panel
                thumbPanel.Controls.Add(thumbnail);
                thumbPanel.Controls.Add(countLabel);

                // Add to main panel
                capturedImagesPanel.Controls.Add(thumbPanel);

                _liveChart.AddMarker(_lastPlotT, "I-" + (capturedImagesPanel.Controls.Count).ToString(), Color.Red);
                MultiClick(sender);
            }
        }



        //Start this code for Save Camere image in test folder 08/12/2025
        //private void SaveCapturedImages(string testFilePath)
        //{
        //    if (_capturedImages.Count == 0)
        //        return;

        //    string testFolder = Path.GetDirectoryName(testFilePath);
        //    string imageFolder = Path.Combine(testFolder, "CapturedImages");

        //    if (!Directory.Exists(imageFolder))
        //        Directory.CreateDirectory(imageFolder);

        //    for (int i = 0; i < _capturedImages.Count; i++)
        //    {
        //        string imgPath = Path.Combine(imageFolder, $"Image_{i + 1}.jpg");

        //        _capturedImages[i].Save(imgPath, System.Drawing.Imaging.ImageFormat.Jpeg);
        //    }

        //    MessageBox.Show($"{_capturedImages.Count} images saved successfully with test.");
        //}

        private void SaveCapturedImages(string testFilePath)
        {
            if (_capturedImages == null || _capturedImages.Count == 0)
                return;

            // ✅ Base test folder
            string testFolder = Path.GetDirectoryName(testFilePath);

            // ✅ Main CapturedImages folder
            string baseImageFolder = Path.Combine(testFolder, "CapturedImages");

            if (!Directory.Exists(baseImageFolder))
                Directory.CreateDirectory(baseImageFolder);

            // ✅ Patient-specific folder using MainId
            string patientId = _selectedPatientId;   // already string
            string patientFolder = Path.Combine(baseImageFolder, $"Patient_{patientId}");

            if (!Directory.Exists(patientFolder))
                Directory.CreateDirectory(patientFolder);

            // ✅ Save images with UNIQUE names to avoid overwrite
            for (int i = 0; i < _capturedImages.Count; i++)
            {
                string imgPath = Path.Combine(
                    patientFolder,
                    $"Image_{DateTime.Now:yyyyMMdd_HHmmss}_{i + 1}.jpg"
                );

                _capturedImages[i].Save(imgPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            //MessageBox.Show($"{_capturedImages.Count} images saved successfully for Patient ID: {patientId}");
        }

        //End this code for Save Camere image in test folder 08/12/2025

        private string _lastSavedTestFilePath;
        private void ClearSavedImagesForCurrentTest()
        {
            try
            {
                string testFolder = Path.GetDirectoryName(_lastSavedTestFilePath);
                if (testFolder == null) return;

                string baseImageFolder = Path.Combine(testFolder, "CapturedImages");
                string patientFolder = Path.Combine(baseImageFolder, $"Patient_{_selectedPatientId}");

                if (Directory.Exists(patientFolder))
                    Directory.Delete(patientFolder, true);  // delete everything
            }
            catch { }
        }


        //Start Code for Get saved image and show view test saved images 08/12/2025

        //This code for Get Saved Image on review mode i think
        private void LoadCapturedImagesForTest(string testFilePath)
        {
            try
            {
                // ✅ Clear old thumbnails and memory images
                if (capturedImagesPanel != null)
                    capturedImagesPanel.Controls.Clear();
                if (_capturedImages != null)
                    _capturedImages.Clear();

                string testFolder = Path.GetDirectoryName(testFilePath);
                string baseImageFolder = Path.Combine(testFolder, "CapturedImages");

                if (!Directory.Exists(baseImageFolder))
                    return;

                // ✅ Patient-specific folder
                string patientId = _selectedPatientId;
                string patientFolder = Path.Combine(baseImageFolder, $"Patient_{patientId}");

                if (!Directory.Exists(patientFolder))
                    return;

                // ✅ Load all images safely (NO FILE LOCKING)
                string[] imageFiles = Directory.GetFiles(patientFolder, "*.jpg")
                                               .OrderBy(f => f)
                                               .ToArray();

                foreach (string imgPath in imageFiles)
                {
                    using (var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read))
                    {
                        Image img = Image.FromStream(fs);
                        Image safeCopy = new Bitmap(img);   // ✅ prevents file lock

                        _capturedImages.Add(safeCopy);
                        //AddThumbnailToPanel(safeCopy);
                        AddThumbnailToPanelImage(safeCopy);
                    }
                }

                // ⭐⭐ SHOW FIRST IMAGE BY DEFAULT ⭐⭐
                if (_capturedImages.Count > 0)
                {
                    ShowImageInCameraPreview(_capturedImages[0]);
                }
                else
                {
                    // Optional: show placeholder again if no images
                    cameraPreview.Image = CreatePlaceholderImage("No Image");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading images: " + ex.Message);
            }
        }


        private void AddThumbnailToPanel(Image captured)
        {
            Panel thumbPanel = new Panel();
            thumbPanel.Size = new Size(40, 60);
            thumbPanel.Margin = new Padding(3);

            PictureBox thumbnail = new PictureBox();
            thumbnail.Image = captured;
            thumbnail.Size = new Size(40, 40);
            thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            thumbnail.BorderStyle = BorderStyle.FixedSingle;
            thumbnail.Location = new Point(0, 0);

            thumbnail.Click += (s, ev) =>
            {
                using (var previewForm = new ImagePreviewForm(captured))
                {
                    previewForm.ShowDialog();
                }
            };

            Label countLabel = new Label();
            countLabel.AutoSize = false;
            countLabel.TextAlign = ContentAlignment.MiddleCenter;
            countLabel.Dock = DockStyle.Bottom;
            countLabel.Height = 15;
            countLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            countLabel.ForeColor = Color.White;
            countLabel.BackColor = Color.FromArgb(50, 50, 250);
            countLabel.Text = (capturedImagesPanel.Controls.Count + 1).ToString();

            thumbPanel.Controls.Add(thumbnail);
            thumbPanel.Controls.Add(countLabel);
            capturedImagesPanel.Controls.Add(thumbPanel);
        }
        //Start Code for Get saved image and show view test saved images 08/12/2025


        //private object comboDevices;

        //Show ComboBox For Camere 
        //private void LoadVideoDevices()
        //{
        //    try
        //    {
        //        comboDevices.Items.Clear();

        //        videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        //        if (videoDevices.Count == 0)
        //        {
        //            MessageBox.Show("No video devices found.");
        //            return;
        //        }

        //        foreach (FilterInfo device in videoDevices)
        //            comboDevices.Items.Add(device.Name);

        //        comboDevices.SelectedIndex = 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error loading devices: " + ex.Message);
        //    }
        //}

        private void LoadVideoDevices()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            comboDevices.Items.Clear();

            int liveIndex = -1;

            for (int i = 0; i < videoDevices.Count; i++)
            {
                var device = videoDevices[i];

                comboDevices.Items.Add(device.Name);

                if (device.Name.Contains("Integrated Webcam"))
                {
                    liveIndex = i;
                }

                //if (device.Name.Contains("Live Streaming Video Device"))
                //{
                //    liveIndex = i;
                //}
            }

            if (liveIndex != -1)
                comboDevices.SelectedIndex = liveIndex;   // auto-select HDMI capture device
            else if (comboDevices.Items.Count > 0)
                comboDevices.SelectedIndex = 0;
        }

        //Start This code for Block System Camera and show massage "Device is not Connected"
        private void ShowCameraMessage(string message)
        {
            Bitmap bmp = new Bitmap(cameraPreview.Width, cameraPreview.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black); // background color
                using (Font f = new Font("Arial", 20, FontStyle.Bold))
                using (Brush b = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(message, f);
                    g.DrawString(message, f, b,
                        (bmp.Width - textSize.Width) / 2,
                        (bmp.Height - textSize.Height) / 2);
                }
            }

            // set image to picturebox
            if (cameraPreview.Image != null)
                cameraPreview.Image.Dispose();

            cameraPreview.Image = bmp;
        }


        //private void StartCamera()
        //{
        //    try
        //    {
        //        if (videoSource != null && videoSource.IsRunning)
        //            return;

        //        int index = comboDevices.SelectedIndex;
        //        if (index < 0)
        //        {
        //            MessageBox.Show("Please select a device.");
        //            return;
        //        }

        //        var selectedDevice = videoDevices[index];

        //        if (!selectedDevice.Name.Contains("Live Streaming Video Device"))
        //        {
        //            ShowCameraMessage("Device is not connected");
        //            return;
        //        }


        //        videoSource = new VideoCaptureDevice(selectedDevice.MonikerString);

        //        var modes1080p = videoSource.VideoCapabilities
        //            .Where(v => v.FrameSize.Width == 1920 && v.FrameSize.Height == 1080)
        //            .ToList();

        //        if (modes1080p.Count > 0)
        //        {
        //            var bestMode = modes1080p
        //                .OrderByDescending(v => v.AverageFrameRate)
        //                .First();

        //            videoSource.VideoResolution = bestMode;
        //        }
        //        else
        //        {
        //            var fallback = videoSource.VideoCapabilities
        //                .OrderByDescending(v => v.FrameSize.Width * v.FrameSize.Height)
        //                .FirstOrDefault();

        //            if (fallback != null)
        //                videoSource.VideoResolution = fallback;
        //            else
        //            {
        //                MessageBox.Show("No supported video format found.");
        //                return;
        //            }
        //        }

        //        videoSource.NewFrame += (s, args) =>
        //        {
        //            Bitmap frame = (Bitmap)args.Frame.Clone();
        //            cameraPreview.Invoke(new Action(() =>
        //            {
        //                cameraPreview.Image?.Dispose();
        //                cameraPreview.Image = frame;
        //            }));
        //        };

        //        videoSource.Start();
        //        captureButton.Enabled = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error starting capture: " + ex.Message);
        //    }
        //}
        //Start This code for Block System Camera and show massage "Device is not Connected"
        private string GetPatientFilePath(string videoDevice)
        {
            string exeFolder = Application.StartupPath;

            // string folder = Path.Combine(exeFolder, "Saved Data", "VideoDevice");
            string folder = AppPathManager.GetFolderPath("VideoDevice");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, videoDevice + ".dat");
        }

        //private string LoadCameraMonikerFromFile()
        //{
        //    try
        //    {
        //        string filePath = GetPatientFilePath("SelectedDevice");

        //        if (!File.Exists(filePath))
        //            return null;

        //        string moniker = File.ReadAllText(filePath)?.Trim();

        //        if (string.IsNullOrEmpty(moniker))
        //            return null;

        //        return moniker;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        private string LoadCameraMonikerFromFile()
        {
            try
            {
                string filePath = GetPatientFilePath("SelectedDevice");

                // ✅ FILE MISSING → DEFAULT LIVE DEVICE
                if (!File.Exists(filePath))
                    return "Live Streaming Video Device";

                string deviceName = File.ReadAllText(filePath);

                // ✅ FILE EMPTY / NULL → DEFAULT LIVE DEVICE
                if (string.IsNullOrWhiteSpace(deviceName))
                    return "Live Streaming Video Device";

                return deviceName.Trim();
            }
            catch
            {
                // ✅ ANY ERROR → DEFAULT LIVE DEVICE
                return "Live Streaming Video Device";
            }
        }
        //Start this code for Get Use "VideoDevice Form Data" show change on 08/12/2025
        private void StartCamera()
        {
            try
            {
                if (videoSource != null && videoSource.IsRunning)
                    return;

                string savedDeviceName = LoadCameraMonikerFromFile();

                if (string.IsNullOrWhiteSpace(savedDeviceName))
                {
                    ShowCameraMessage("No saved device found");
                    return;
                }

                FilterInfo selectedDevice = null;

                // ✅ FIND EXACT SAVED DEVICE
                foreach (FilterInfo device in videoDevices)
                {
                    if (device.Name.ToLower() == savedDeviceName.ToLower())
                    {
                        selectedDevice = device;
                        break;
                    }
                }

                // ✅ IF LIVE STREAM DEVICE IS SAVED BUT NOT CONNECTED
                if (selectedDevice == null &&
                    savedDeviceName.ToLower().IndexOf("live") >= 0)
                {
                    ShowCameraMessage("Device is not connected"); //Live Streaming \n
                    return;
                }

                // ✅ IF WEBCAM IS SAVED BUT NOT CONNECTED
                if (selectedDevice == null)
                {
                    ShowCameraMessage("Camera device is not connected");
                    return;
                }

                // ✅ START SELECTED DEVICE ONLY
                videoSource = new VideoCaptureDevice(selectedDevice.MonikerString);

                var modes1080p = videoSource.VideoCapabilities
                    .Where(v => v.FrameSize.Width == 1920 && v.FrameSize.Height == 1080)
                    .ToList();

                if (modes1080p.Count > 0)
                {
                    videoSource.VideoResolution = modes1080p
                        .OrderByDescending(v => v.AverageFrameRate)
                        .First();
                }
                else
                {
                    var fallback = videoSource.VideoCapabilities
                        .OrderByDescending(v => v.FrameSize.Width * v.FrameSize.Height)
                        .FirstOrDefault();

                    if (fallback != null)
                        videoSource.VideoResolution = fallback;
                    else
                    {
                        ShowCameraMessage("No supported camera format found");
                        return;
                    }
                }


                //Start Comment this code on 23-02-2026 getting error if Live test stop & Change camera design for right side
                //videoSource.NewFrame += (s, args) =>
                //{
                //    Bitmap frame = (Bitmap)args.Frame.Clone();
                //    cameraPreview.Invoke(new Action(() =>
                //    {
                //        cameraPreview.Image?.Dispose();
                //        cameraPreview.Image = frame;
                //    }));
                //};

                //videoSource.Start();
                //captureButton.Enabled = true;
                //END Comment this code on 23-02-2026 getting error if Live test stop & Change camera design for right side

                // 🔴 Start Code For Live Test Saved and Stop Camera & Get Saved Image & show Right side Camera design Change that time ADD on 23-02-2026
                videoSource.NewFrame += (s, args) =>
                {
                    try
                    {
                        // Check if cameraPreview still exists and is not disposed
                        if (cameraPreview == null || cameraPreview.IsDisposed)
                            return;

                        Bitmap frame = (Bitmap)args.Frame.Clone();

                        // Use BeginInvoke instead of Invoke to avoid deadlocks
                        if (cameraPreview.IsHandleCreated)
                        {
                            cameraPreview.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    // Double-check after marshaling to UI thread
                                    if (cameraPreview == null || cameraPreview.IsDisposed || !cameraPreview.IsHandleCreated)
                                        return;

                                    cameraPreview.Image?.Dispose();
                                    cameraPreview.Image = frame;
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Silently ignore - control was disposed
                                    frame?.Dispose();
                                }
                                catch (Exception)
                                {
                                    frame?.Dispose();
                                }
                            }));
                        }
                        else
                        {
                            frame?.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore any errors during frame processing
                    }
                };

                videoSource.Start();

                // Check if captureButton still exists before enabling
                if (captureButton != null && !captureButton.IsDisposed)
                {
                    captureButton.Enabled = true;
                }
                // 🔴 End Code For Live Test Saved and Stop Camera & Get Saved Image & show Right side Camera design Change that time ADD on 23-02-2026

            }
            catch (Exception ex)
            {
                ShowCameraMessage("Camera Error: " + ex.Message);
            }
        }

        //Show System Camera with Connect Device
        // This Code For Connect Other Laptop and show Screen in Camera Screen add on 13/11/2025
        //private void StartCamera()
        //{
        //    try
        //    {
        //        if (videoSource != null && videoSource.IsRunning)
        //            return;

        //        int index = comboDevices.SelectedIndex;
        //        if (index < 0)
        //        {
        //            MessageBox.Show("Please select a device.");
        //            return;
        //        }

        //        var selectedDevice = videoDevices[index];
        //        videoSource = new VideoCaptureDevice(selectedDevice.MonikerString);

        //        var modes1080p = videoSource.VideoCapabilities
        //            .Where(v => v.FrameSize.Width == 1920 && v.FrameSize.Height == 1080)
        //            .ToList();

        //        if (modes1080p.Count > 0)
        //        {
        //            var bestMode = modes1080p
        //                .OrderByDescending(v => v.AverageFrameRate)
        //                .First();

        //            videoSource.VideoResolution = bestMode;
        //        }
        //        else
        //        {
        //            var fallback = videoSource.VideoCapabilities
        //                .OrderByDescending(v => v.FrameSize.Width * v.FrameSize.Height)
        //                .FirstOrDefault();

        //            if (fallback != null)
        //                videoSource.VideoResolution = fallback;
        //            else
        //                MessageBox.Show("No supported video format found.");
        //        }

        //        videoSource.NewFrame += (s, args) =>
        //        {
        //            Bitmap frame = (Bitmap)args.Frame.Clone();
        //            cameraPreview.Invoke(new Action(() =>
        //            {
        //                cameraPreview.Image?.Dispose();
        //                cameraPreview.Image = frame;
        //            }));
        //        };

        //        videoSource.Start();

        //        captureButton.Enabled = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error starting capture: " + ex.Message);
        //    }
        //}


        //Start This Code For Open Laptop means Current System Camera commit on 13/11/2025
        //private void StartCamera()
        //{
        //    if (!isCameraOn)
        //    {

        //        try
        //        {
        //            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        //            if (videoDevices.Count > 0)
        //            {
        //                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
        //                videoSource.NewFrame += (s, args) =>
        //                {
        //                    Bitmap frame = (Bitmap)args.Frame.Clone();
        //                    cameraPreview.Invoke(new Action(() =>
        //                    {
        //                        cameraPreview.Image?.Dispose();
        //                        cameraPreview.Image = frame;
        //                    }));
        //                };
        //                videoSource.Start();
        //                isCameraOn = true;
        //                captureButton.Enabled = true;
        //            }
        //            else
        //            {
        //                MessageBox.Show("No camera detected.");
        //            }
        //        }
        //        catch { }
        //    }
        //}

        private void StopCamera()
        {
            try
            {
                if (videoSource != null)
                {
                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        //videoSource.NewFrame -= videoSource_NewFrame; // detach event
                        videoSource = null;

                    }
                }

                isCameraOn = false;

                if (cameraPreview != null)
                    cameraPreview.Image = null;

                if (captureButton != null)
                    captureButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping camera: " + ex.Message);
            }
        }



        //For Manual Create Home Pane
        private void CreateMainContentForPatient()
        {
            // Create main content panel
            mainContentPanelPatient = new Panel();
            mainContentPanelPatient.Dock = DockStyle.Fill;
            //mainContentPanelPatient.BackColor = Color.FromArgb(245, 245, 245);
            mainContentPanelPatient.BackColor = Color.White;
            mainContentPanelPatient.Padding = new Padding(0, 120, 0, 0);

            // Create graph container
            Panel graphContainer = new Panel();
            graphContainer.Dock = DockStyle.Fill;
            graphContainer.Padding = new Padding(10);
            graphContainer.BackColor = Color.FromArgb(245, 245, 245);

            // Create black placeholder box instead of image
            graphPictureBox = new PictureBox();
            graphPictureBox.Dock = DockStyle.Fill;
            graphPictureBox.BackColor = Color.White; // show only black space
            graphPictureBox.BorderStyle = BorderStyle.FixedSingle;
            graphPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            // Add the black box
            graphContainer.Controls.Add(graphPictureBox);

            // Start This Line Show Patinet List in Home Screent cards Formate comment on 07/11/2025
            //CreatePatientRightSidebar();
            // Start This Line Show Patinet List in Home Screent cards Formate comment on 07/11/2025

            // 🟩 Call the new helper method to show logo and buttons
            CreateLogoAndButtonsUI(graphContainer);

            graphPictureBox.SendToBack();

            // Add both panels to main content
            mainContentPanelPatient.Controls.Add(graphContainer);
            mainContentPanelPatient.Controls.Add(rightSidebarPanelForPatient);

            // Add main content to form
            this.Controls.Add(mainContentPanelPatient);
        }

        //this code for Show logo and two buttons
        private void CreateLogoAndButtonsUI(Panel parent)
        {
            // Overlay panel fills the parent area
            Panel overlayPanel = new Panel();
            overlayPanel.Dock = DockStyle.Fill;
            overlayPanel.BackColor = Color.White; // keep it simple and visible

            // Use TableLayoutPanel to center logo and buttons vertically
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F)); // logo area
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F)); // button area
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.BackColor = Color.White;

            // === LOGO ===
            PictureBox logo = new PictureBox();
            logo.Image = Properties.Resources.SantronLogo; // ✅ use your actual logo
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Anchor = AnchorStyles.None;
            logo.Margin = new Padding(0, 0, 0, 10);
            logo.Width = 600;   // ✅ make logo bigger
            logo.Height = 230;  // ✅ make logo taller

            // === BUTTON PANEL ===
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Anchor = AnchorStyles.None;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.AutoSize = true;
            buttonPanel.WrapContents = false;
            buttonPanel.BackColor = Color.White;
            buttonPanel.Margin = new Padding(0, 0, 0, 0);

            Button btnAddNew = new Button();
            btnAddNew.Size = new Size(150, 140);
            btnAddNew.BackColor = Color.White;
            btnAddNew.FlatStyle = FlatStyle.Flat;
            btnAddNew.FlatAppearance.BorderSize = 0;
            btnAddNew.BackgroundImage = Properties.Resources.NewPatinet; // ✅ replace with your patient add icon
            btnAddNew.BackgroundImageLayout = ImageLayout.Zoom;
            btnAddNew.Cursor = Cursors.Hand;
            btnAddNew.Margin = new Padding(40, 0, 40, 0);

            btnAddNew.Click += (s, e) =>
            {
                var patientWithTestForm = new PatientWithTestForm();
                ScreenDimOverlay.ShowDialogWithDim(patientWithTestForm, alpha: 150);

            };

            Button btnList = new Button();
            btnList.Size = new Size(150, 140);
            btnList.BackColor = Color.White;
            btnList.FlatStyle = FlatStyle.Flat;
            btnList.FlatAppearance.BorderSize = 0;
            btnList.BackgroundImage = Properties.Resources.OpenTest;
            btnList.BackgroundImageLayout = ImageLayout.Zoom;
            btnList.Cursor = Cursors.Hand;
            btnList.Margin = new Padding(40, 0, 40, 0);

            btnList.Click += (s, e) =>
            {
                var patientList = new PatientList();
                ScreenDimOverlay.ShowDialogWithDim(patientList, alpha: 150);

            };


            // Add buttons to flow panel
            buttonPanel.Controls.Add(btnAddNew);
            buttonPanel.Controls.Add(btnList);

            // Add logo and button panel to layout
            layout.Controls.Add(logo, 0, 0);
            layout.Controls.Add(buttonPanel, 0, 1);

            // Add layout to overlay panel
            overlayPanel.Controls.Add(layout);

            // Add overlay to parent panel
            parent.Controls.Add(overlayPanel);

            // Make sure it's visible on top
            overlayPanel.BringToFront();
        }


        //private string GetDoctorsFolder()
        //{
        //    string exeFolder = Application.StartupPath;
        //    string folder = Path.Combine(exeFolder, "Saved Data", "DoctorsData");

        //    if (!Directory.Exists(folder))
        //        Directory.CreateDirectory(folder);

        //    return folder;
        //}

        private List<string> GetAllDoctorNames()
        {
            // string folderPath = GetDoctorsFolder();
            string folderPath = AppPathManager.GetFolderPath("DoctorsData");
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

        //Not Delete this method again Use may be
        //Start This Code For Show PatientList In Home Screen (Reference :- CreateMainContentForPatient Method Call this method line No. 3643)  commint on 07/11/2025
        private void CreatePatientRightSidebar()
        {
            // === Right sidebar host ===
            rightSidebarPanelForPatient = new Panel
            {
                Width = 380,                                 // fixed rail width; can be made dynamic below
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(5)
            };

            // Optional: collapse the sidebar on very small widths
            this.Resize -= MainForm_Resize_ForPatientSidebar; // avoid multi-subscribe on rebuilds
            this.Resize += MainForm_Resize_ForPatientSidebar;

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // search
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // doctor
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // label
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // scroll area

            // === Add New Patient button (docked bottom so it sticks there even while scrolling) ===
            //var addPatientBtn = new Button
            //{
            //    Text = "+ Add New Patient",
            //    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            //    BackColor = Color.FromArgb(0, 73, 165),
            //    ForeColor = Color.White,
            //    FlatStyle = FlatStyle.Flat,
            //    Height = 35,
            //    Dock = DockStyle.Top,
            //    Cursor = Cursors.Hand,
            //    //Padding = new Padding(10, 7, 10, 7),
            //    Margin = new Padding(0, 0, 0, 7)
            //};
            //addPatientBtn.FlatAppearance.BorderSize = 0;

            //addPatientBtn.Click += (_, __) =>
            //{
            //    var patientForm = new Patient_Information();
            //    ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
            //};

            // === Search box with button ===
            var searchContainer = new Panel
            {
                Height = 33,
                Dock = DockStyle.Top,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 7, 10, 7),
                Margin = new Padding(0, 0, 0, 7)
            };

            var searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                Text = "Search here",
                BackColor = Color.White
            };

            //var searchButton = new Button
            //{
            //    Text = "🔍",
            //    Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            //    Dock = DockStyle.Right,
            //    Width = 35,
            //    FlatStyle = FlatStyle.Flat,
            //    BackColor = Color.FromArgb(0, 73, 165),
            //    ForeColor = Color.White,
            //    Cursor = Cursors.Hand,
            //    TabStop = false
            //};
            //searchButton.FlatAppearance.BorderSize = 0;

            // placeholder behavior
            searchBox.GotFocus += (_, __) =>
            {
                if (searchBox.Text == "Search here")
                {
                    searchBox.Text = "";
                    searchBox.ForeColor = Color.Black;
                }
            };
            searchBox.LostFocus += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    searchBox.Text = "Search here";
                    searchBox.ForeColor = Color.Gray;
                }
            };

            searchContainer.Controls.Add(searchBox);
            //searchContainer.Controls.Add(searchButton);

            // === Doctor filter ===
            var drNameCombo = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 10)
            };

            drNameCombo.Items.Clear();
            drNameCombo.Items.Add("All Doctors");
            var doctors = GetAllDoctorNames();
            if (doctors != null && doctors.Count > 0)
            {
                foreach (var name in doctors
                         .Select(n => n?.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(n => n))
                {
                    drNameCombo.Items.Add(name);
                }
            }
            else
            {
                drNameCombo.Items.Add("No Doctors Found");
            }
            drNameCombo.SelectedIndex = 0;

            // === Label ===
            var appointmentsLabel = new Label
            {
                Text = "Today's Appointments",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 6),
                Visible = false
            };

            // === Scroll area (fills) ===
            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(2, 0, 2, 0)
            };



            // inner container so the bottom button can be docked within scroll host
            var listAndButtonStack = new Panel
            {
                Dock = DockStyle.Fill
            };

            // the actual list container
            var patientCardsHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 0, 0, 8), // gutters look nicer
                Margin = new Padding(0),
                AutoSize = false

            };

            patientCardsHost.SizeChanged += (_, __) => ResizeCardsToHost(patientCardsHost);
            patientCardsHost.ControlAdded += (_, __) => BeginInvoke(new Action(() => ResizeCardsToHost(patientCardsHost)));


            // wire filters
            void TriggerFilter()
            {
                string doctor = drNameCombo.SelectedItem?.ToString() ?? "All Doctors";
                string text = (searchBox.Text == "Search here") ? "" : searchBox.Text.Trim();
                LoadPatients(patientCardsHost, doctor, text);
                int filteredpatientcount = LoadPatients(patientCardsHost, doctor, text);
                appointmentsLabel.Text = $"Today's Appointments ({filteredpatientcount})";

            }

            drNameCombo.SelectedIndexChanged += (_, __) => TriggerFilter();
            //searchButton.Click += (_, __) => TriggerFilter();
            searchBox.TextChanged += (_, __) =>
            {
                if (searchBox.Focused) TriggerFilter();
            };

            // initial load
            int patientcount = LoadPatients(patientCardsHost, "All Doctors", "");
            appointmentsLabel.Text = $"Today's Appointments ({patientcount})";
            LoadPatients(patientCardsHost, "All Doctors", "");

            // compose hierarchy
            listAndButtonStack.Controls.Add(patientCardsHost);
            //listAndButtonStack.Controls.Add(addPatientBtn);
            scrollHost.Controls.Add(listAndButtonStack);

            //grid.Controls.Add(addPatientBtn, 0, 0);
            grid.Controls.Add(searchContainer, 0, 1);
            grid.Controls.Add(drNameCombo, 0, 2);
            //grid.Controls.Add(appointmentsLabel, 0, 3);
            grid.Controls.Add(scrollHost, 0, 3);


            //grid.Controls.Add(searchContainer, 0, 0);
            //grid.Controls.Add(drNameCombo, 0, 1);
            //grid.Controls.Add(appointmentsLabel, 0, 2);
            //grid.Controls.Add(scrollHost, 0, 3);

            rightSidebarPanelForPatient.Controls.Add(grid);
            this.Controls.Add(rightSidebarPanelForPatient);
            rightSidebarPanelForPatient.BringToFront(); // ensure it sits above central content
        }

        // Optional: collapse/expand sidebar on very narrow widths
        private void MainForm_Resize_ForPatientSidebar(object sender, EventArgs e)
        {
            if (rightSidebarPanelForPatient == null) return;

            // tune breakpoints to your app
            if (this.ClientSize.Width < 1000)
            {
                rightSidebarPanelForPatient.Visible = true;   // could set false to hide
                rightSidebarPanelForPatient.Width = 380;      // slightly narrower on small screens
            }
            else
            {
                rightSidebarPanelForPatient.Visible = true;
                rightSidebarPanelForPatient.Width = 380;
            }
        }



        private List<MultiChannelLiveChart.Channel> GetTestChannels(string test, ScaleAndColorModel record)
        {

            var testChannelMap = new Dictionary<string, string[]>
            {
                ["Uroflowmetry"] = new[] { "Qvol", "Qrate" },
                ["Uroflowmetry + EMG"] = new[] { "Qvol", "Qrate" },
                ["Cystometry"] = new[] { "Pves", "Pabd", "Pdet", "Vinf" },
                ["Pressure Flow"] = new[] { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qrate" },
                ["Pressure Flow + EMG"] = new[] { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qrate", "EMG" },
                ["UPP"] = new[] { "Pves", "Pura", "Pclo" },
                ["Whitaker Test"] = new[] { "Pves", "Pirp", "Prpg" },
                ["Pressure Flow + Video"] = new[] { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qrate" },
                ["Pressure Flow + EMG + Video"] = new[] { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qrate", "EMG" },
                ["Biofeedback"] = new[] { "EMG" },
                ["Anal Manometry"] = new[] { "Pa1", "Pa2", "Pa3" }
            };

            if (!testChannelMap.ContainsKey(test)) return new List<MultiChannelLiveChart.Channel>();

            var requiredChannels = testChannelMap[test];


            List<MultiChannelLiveChart.Channel> lanes = new List<MultiChannelLiveChart.Channel>();

            lanes.Add(CreateChannel(record.ChannelZero, record.ColorZero, record.PlotScaleZero, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelOne, record.ColorOne, record.PlotScaleOne, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelTwo, record.ColorTwo, record.PlotScaleTwo, "cmH2O"));
            lanes.Add(CreateChannel(record.ChannelThree, record.ColorThree, record.PlotScaleThree, "ml"));
            lanes.Add(CreateChannel(record.ChannelFour, record.ColorFour, record.PlotScaleFour, "ml"));
            lanes.Add(CreateChannel(record.ChannelFive, record.ColorFive, record.PlotScaleFive, "ml/s"));
            lanes.Add(CreateChannel(record.ChannelSix, record.ColorSix, record.PlotScaleSix, "uV"));
            //lanes.Add(CreateChannel(record.ChannelSeven, record.ColorSeven, record.PlotScaleSeven, "cmH2O"));
            //lanes.Add(CreateChannel(record.ChannelEight, record.ColorEight, record.PlotScaleEight, "cmH2O"));

            // Remove nulls (if channel name is empty)
            lanes = lanes.Where(c => c != null && requiredChannels.Contains(c.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            _liveChart.SetChannels(lanes);
            _liveChart.ScrollToLive();
            _activeIndices = Enumerable.Range(0, lanes.Count).ToArray();


            return lanes
            .Where(c => c != null && requiredChannels.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
        }


        private int LoadPatients(FlowLayoutPanel flp, string doctorFilter = "", string searchText = "")
        {
            flp.SuspendLayout();
            var errors = new List<string>();
            int added = 0;

            try
            {
                flp.Controls.Clear();

                // string folderPath = Path.Combine(Application.StartupPath, "Saved Data", "PatientsData");
                string folderPath = AppPathManager.GetFolderPath("Samples");

                if (!Directory.Exists(folderPath))
                {
                    flp.Controls.Add(MakeEmptyLabel("No patient folder found."));
                    return 0;
                }

                string q = (searchText ?? "").Trim();
                bool hasSearch = !string.IsNullOrEmpty(q) && !q.Equals("Search here", StringComparison.OrdinalIgnoreCase);

                var files = Directory.GetFiles(folderPath, "*.dat")
                                     .OrderByDescending(File.GetLastWriteTimeUtc);

                foreach (var filePath in files)
                {
                    try
                    {
                        var record = TryReadRecord(filePath);
                        if (record == null) continue;

                        // filters
                        if (!string.IsNullOrWhiteSpace(doctorFilter) && doctorFilter != "All Doctors")
                            if (!string.Equals(record.ReferredBy?.Trim(), doctorFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                                continue;

                        if (hasSearch)
                            if (string.IsNullOrWhiteSpace(record.PatientName) ||
                                record.PatientName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                        // build card
                        var subtitle = $"ID: {record.PatientNo} • {record.Age} years";
                        var card = CreatePatientCard(record.PatientName, subtitle, record.MobileNo,
                                                     record.ReferredBy, record.Gender, record.Test, false, record.Id);


                        // ⬇️ size for FLP
                        card.AutoSize = false;
                        card.Margin = new Padding(0, 0, 0, 8);
                        card.Width = EffectiveWidth(flp);     // set width explicitly
                        card.Height = Math.Max(card.MinimumSize.Height, card.PreferredSize.Height);

                        int id = record.Id;
                        string patientId = record.PatientNo;
                        // build a single click action that does your routing + open-test
                        EventHandler unifiedClick = (s, e) =>
                        {
                            PatientWithTestForm patientTestForm = new PatientWithTestForm
                            {
                                PatientIdNumber = id,
                                PatientId = patientId
                            };
                            patientTestForm.Show();
                        };
                        //EventHandler unifiedClick = (s, e) =>
                        //{

                        //    PatientWithTestForm patientTestForm = new PatientWithTestForm();
                        //    patientTestForm.PatientId = patientId;
                        //    patientTestForm.Show();
                        //};

                        WireClickRecursive(card, unifiedClick);

                        flp.Controls.Add(card);
                        added++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                if (added == 0)
                    flp.Controls.Add(MakeEmptyLabel(hasSearch ? "No patients match your filter." : "No patient records found."));

                return added;
            }
            finally
            {
                flp.ResumeLayout(true);
                // recalc widths AFTER layout/scrollbar appear
                BeginInvoke(new Action(() => ResizeCardsToHost(flp)));

                if (errors.Count > 0)
                {
                    var first = string.Join("\n", errors.Take(5));
                    MessageBox.Show($"Some records couldn’t be loaded ({errors.Count}).\n\n{first}" + (errors.Count > 5 ? "\n..." : ""),
                                    "Load Patients", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // helpers
            PatientRecord TryReadRecord(string path)
            {
                var bytes = File.ReadAllBytes(path);
                var json = CryptoHelper.Decrypt(bytes);
                return JsonSerializer.Deserialize<PatientRecord>(json);
            }
            Label MakeEmptyLabel(string text) => new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 8, 0, 0)
            };
        }

        private static int EffectiveWidth(ScrollableControl flp)
        {
            int w = flp.ClientSize.Width - flp.Padding.Horizontal - 1;
            // if a vertical scrollbar is present, leave room
            if (flp.VerticalScroll.Visible) w -= SystemInformation.VerticalScrollBarWidth;
            return Math.Max(140, w);
        }

        private void ResizeCardsToHost(FlowLayoutPanel flp)
        {
            int w = EffectiveWidth(flp);
            foreach (Control c in flp.Controls)
                if (c is Panel p) p.Width = w;
            flp.PerformLayout();
        }

        private static void WireClickRecursive(Control root, EventHandler onClick)
        {
            root.Click += onClick;
            foreach (Control c in root.Controls) WireClickRecursive(c, onClick);
        }



        public void ShowEmpty(Control host, string text)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };
            host.Controls.Add(lbl);
        }

        // Small helper so the click handler stays clean

        //Start this Code For Open Only Graph Test chnage on 15/11/2025 
        private void OpenLiveTestOnly(PatientRecord record)
        {
            if (record == null || _liveChart == null || _testMgr == null) return;

            _currentPatient = record;
            currenttest = record.Test;

            TestChannelManager.TestDefinition def = _testMgr.GetDefinition(record.Test);
            if (def != null)
            {
                _currentTestDef = def;
                _activeIndices = (def.Indices != null && def.Indices.Length > 0)
                    ? def.Indices
                    : Enumerable.Range(0, def.Lanes.Count).ToArray();

                _liveChart.Clear();
                _liveChart.SetChannels(def.Lanes);
                _liveChart.ScrollToLive();
                BuildScaleMaxOverlays();
            }

            // --- decide playback vs demo ---
            string uttPath;
            if (TryFindLatestUttForPatient(record, record.Test, out uttPath))
            {
                EnterDemoMode();
            }


            // --- Your existing right-panel visibility rules ---
            string test = record.Test != null ? record.Test.Trim() : string.Empty;

            if (test.Equals("Uroflowmetry", StringComparison.OrdinalIgnoreCase) ||
                test.Equals("Uroflowmetry + EMG", StringComparison.OrdinalIgnoreCase))
            {
                panel3.Location = new System.Drawing.Point(0, 37);
                panel3.Visible = true; panel2.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.StartsWith("Cystometry", StringComparison.OrdinalIgnoreCase) ||
                     test.StartsWith("Pressure Flow", StringComparison.OrdinalIgnoreCase))
            {
                panel2.Location = new System.Drawing.Point(0, 37);
                panel2.Visible = true; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("UPP", StringComparison.OrdinalIgnoreCase) ||
                     test.Equals("Whitaker Test", StringComparison.OrdinalIgnoreCase))
            {
                panel4.Location = new System.Drawing.Point(0, 37);
                panel4.Visible = true; panel2.Visible = false; panel3.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("Biofeedback", StringComparison.OrdinalIgnoreCase))
            {
                panel5.Location = new System.Drawing.Point(0, 37);
                panel5.Visible = true; panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("Anal Manometry", StringComparison.OrdinalIgnoreCase))
            {
                panel6.Location = new System.Drawing.Point(0, 37);
                panel6.Visible = true; panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false;
            }
            else
            {
                panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }


            SetupScaleMaxDropdownsForCurrentTest();
        }


        private void OpenTestViewForRecord(PatientRecord record)
        {
            if (record == null || _liveChart == null || _testMgr == null) return;

            ResetTimeForNewTest();

            _currentPatient = record;
            currenttest = record.Test;

            TestChannelManager.TestDefinition def = _testMgr.GetDefinition(record.Test);
            if (def != null)
            {
                _currentTestDef = def;
                _activeIndices = (def.Indices != null && def.Indices.Length > 0)
                    ? def.Indices
                    : Enumerable.Range(0, def.Lanes.Count).ToArray();

                _liveChart.Clear();
                _liveChart.SetChannels(def.Lanes);
                _liveChart.ScrollToLive();
                BuildScaleMaxOverlays();
            }

            // --- decide playback vs demo ---
            string uttPath;
            if (TryFindLatestUttForPatient(record, record.Test, out uttPath))
            {
                LoadUttForPlayback(uttPath, V);

                // ✅ LOAD SAVED IMAGES ALSO
                LoadCapturedImagesForTest(uttPath);
            }
            else
            {
                EnterDemoMode();
            }

            // --- Your existing right-panel visibility rules ---
            string test = record.Test != null ? record.Test.Trim() : string.Empty;

            if (test.Equals("Uroflowmetry", StringComparison.OrdinalIgnoreCase) ||
                test.Equals("Uroflowmetry + EMG", StringComparison.OrdinalIgnoreCase))
            {
                panel3.Location = new System.Drawing.Point(0, 37);
                panel3.Visible = true; panel2.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.StartsWith("Cystometry", StringComparison.OrdinalIgnoreCase) ||
                     test.StartsWith("Pressure Flow", StringComparison.OrdinalIgnoreCase))
            {
                panel2.Location = new System.Drawing.Point(0, 37);
                panel2.Visible = true; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("UPP", StringComparison.OrdinalIgnoreCase) ||
                     test.Equals("Whitaker Test", StringComparison.OrdinalIgnoreCase))
            {
                panel4.Location = new System.Drawing.Point(0, 37);
                panel4.Visible = true; panel2.Visible = false; panel3.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("Biofeedback", StringComparison.OrdinalIgnoreCase))
            {
                panel5.Location = new System.Drawing.Point(0, 37);
                panel5.Visible = true; panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel6.Visible = false;
            }
            else if (test.Equals("Anal Manometry", StringComparison.OrdinalIgnoreCase))
            {
                panel6.Location = new System.Drawing.Point(0, 37);
                panel6.Visible = true; panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false;
            }
            else
            {
                panel2.Visible = false; panel3.Visible = false; panel4.Visible = false; panel5.Visible = false; panel6.Visible = false;
            }


            SetupScaleMaxDropdownsForCurrentTest();
        }

        private Panel CreatePatientCard(string name, string details, string phone, string doctor, string gender, string testName, bool isSelected, int id)
        {
            var normal = isSelected ? Color.FromArgb(230, 240, 255) : Color.White;
            var hover = isSelected ? Color.FromArgb(220, 235, 255) : Color.FromArgb(245, 247, 250);


            var card = new ElevatedPanel
            {
                // width is handled by Dock in LoadPatients (see step 1)
                BackColor = Color.Transparent,
                CornerRadius = 12,
                ShadowSize = 4,
                ShadowBlur = 20,
                ShadowColor = Color.FromArgb(200, 0, 0, 0),
                BorderThickness = 0,       // no hard border
                Margin = new Padding(1, 1, 1, 10),
                Padding = new Padding(10),
                AutoSize = false,
                MinimumSize = new Size(0, 110),
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,            // fill, so we color it too
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                BackColor = normal                // <- match card so hover is visible
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // labels (make them transparent so the grid color shows)
            Label MakeLabel(string t, Font f, Color col, bool bold = false)
            {
                return new Label { Text = t, Font = f, ForeColor = col, AutoSize = true, BackColor = Color.Transparent, Dock = DockStyle.Fill };
            }

            var nameLabel = MakeLabel(name, new Font("Segoe UI", 11f, FontStyle.Bold), Color.FromArgb(0, 73, 165));
            var genderLabel = new Label
            {
                Text = gender,
                AutoSize = true,
                BackColor = Color.FromArgb(235, 235, 235),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 0, 0, 4),
                Dock = DockStyle.Right
            };

            string[] parts = (details ?? "").Split('•');
            string idPart = parts.Length > 0 ? parts[0].Trim() : "";
            string agePart = parts.Length > 1 ? parts[1].Trim() : "";

            var idLabel = MakeLabel(idPart, new Font("Segoe UI", 9f, FontStyle.Bold), Color.Black);
            var ageLabel = MakeLabel(agePart, new Font("Segoe UI", 9f), Color.FromArgb(120, 120, 120));
            var infoLabel = MakeLabel($"Phone: {phone}\nDoctor: {doctor}", new Font("Segoe UI", 8f), Color.FromArgb(120, 120, 120));
            var testLabel = MakeLabel($"Test: {testName}", new Font("Segoe UI", 8f, FontStyle.Bold), Color.FromArgb(200, 80, 80));

            var line3 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, BackColor = Color.Transparent };
            line3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            line3.Controls.Add(ageLabel, 0, 0);
            line3.Controls.Add(infoLabel, 1, 0);

            grid.Controls.Add(nameLabel, 0, 0);
            grid.Controls.Add(genderLabel, 1, 0);
            grid.Controls.Add(idLabel, 0, 1); grid.SetColumnSpan(idLabel, 2);
            grid.Controls.Add(line3, 0, 2); grid.SetColumnSpan(line3, 2);
            grid.Controls.Add(testLabel, 0, 3); grid.SetColumnSpan(testLabel, 2);

            card.Controls.Add(grid);

            // unified hover (apply to card + grid so highlight isn't hidden by grid)
            void HoverOn(object s, EventArgs e) { card.BackColor = hover; grid.BackColor = hover; }
            void HoverOff(object s, EventArgs e) { card.BackColor = normal; grid.BackColor = normal; }
            WireHoverRecursive(card, HoverOn, HoverOff);

            return card;
        }

        // Hover wiring for all descendants

        private void WireHoverRecursive(Control root, EventHandler enter, EventHandler leave)
        {
            root.MouseEnter += enter;
            root.MouseLeave += leave;
            foreach (Control c in root.Controls) WireHoverRecursive(c, enter, leave);
        }






        private void CreateMenuStrip()
        {
            menuStrip1 = new MenuStrip();

            float dpi = GetDpiScale(this);
            menuStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow; // reflow + overflow
            menuStrip1.CanOverflow = true;
            menuStrip1.Stretch = true;                  // fill width
            menuStrip1.ImageScalingSize = new Size((int)(16 * dpi), (int)(16 * dpi));
            menuStrip1.Padding = new Padding((int)(8 * dpi), (int)(2 * dpi), (int)(8 * dpi), (int)(2 * dpi));
            menuStrip1.AutoSize = true;                 // let height adjust with DPI
            menuStrip1.MinimumSize = new Size(0, (int)(32 * dpi));

            menuStrip1.BackColor = Color.FromArgb(0, 73, 165);
            menuStrip1.ForeColor = Color.White;
            menuStrip1.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            menuStrip1.AutoSize = false;
            // menuStrip1.Height = 52;
            menuStrip1.Dock = DockStyle.Top;
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Renderer = new CustomMenuRenderer();


            int logoW = Math.Max(72, Math.Min(145, (int)(this.ClientSize.Width * 0.42))); // 12% of form, clamped
            int logoH = (int)(22 * dpi);
            int iconW = Math.Max(32, Math.Min(38, (int)(this.ClientSize.Width * 0.12))); // 12% of form, clamped
            int iconH = (int)(23 * dpi);
            //string imagePath = Path.Combine(Application.StartupPath, "Images", "SantronLogo.png");
            string imagePath = AppPathManager.GetFilePath("Images", "SantronLogo.png");
            try
            {
                Image logoImage = Properties.Resources.SantronLogo;


                if (logoImage != null)
                {
                    // Maintain aspect ratio
                    float scale = Math.Min((float)logoW / logoImage.Width, (float)logoH / logoImage.Height);
                    int newW = (int)(logoImage.Width * scale);
                    int newH = (int)(logoImage.Height * scale);

                    Bitmap resizedLogo = new Bitmap(newW, newH);
                    using (Graphics g = Graphics.FromImage(resizedLogo))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.DrawImage(logoImage, 0, 0, newW, newH);
                    }

                    ToolStripLabel logoLabel = new ToolStripLabel
                    {
                        Image = resizedLogo,
                        ImageScaling = ToolStripItemImageScaling.None, // prevent menu strip from scaling again
                        Margin = new Padding(10, 2, 20, 2),
                        Padding = new Padding(2, 1, 2, 0),
                        IsLink = true
                    };

                    // logoLabel.Click += (s, e) => { btnShowPatients_Click(s, e); };

                    menuStrip1.Items.Add(logoLabel);
                }
                else
                {
                    // fallback if logo is missing
                    ToolStripLabel logoText = new ToolStripLabel
                    {
                        Text = "Santron Meditronic",
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        Margin = new Padding(15, 5, 25, 5),
                        IsLink = true
                    };

                    logoText.Click += (s, e) => { btnShowPatients_Click(s, e); };

                    menuStrip1.Items.Add(logoText);
                }
            }
            catch
            {
                // extra safety fallback
                ToolStripLabel logoText = new ToolStripLabel
                {
                    Text = "Santron Meditronic",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    Margin = new Padding(15, 5, 25, 5),
                    IsLink = true
                };

                logoText.Click += (s, e) => { btnShowPatients_Click(s, e); };

                menuStrip1.Items.Add(logoText);
            }
            menuStrip1.ShowItemToolTips = true;

            ToolStripItem patientButton = CreateIconButton("\uE160", Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH), ButtonForPatient);
            patientButton.Name = "patientButton";
            patientButton.ToolTipText = "Add New Patients"; // Name to show on hover
            menuStrip1.Items.Add(patientButton);

            ToolStripItem menuButton = CreateIconButton("\uE8B7", Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH), OpenMenuItem_Click);
            menuButton.Name = "menuButton";
            menuButton.ToolTipText = "Opne File"; // Name to show on hover
            menuStrip1.Items.Add(menuButton);

            ToolStripItem settingsButton = CreateIconButton("\uE749", Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH), PrintReport_Click);
            settingsButton.Name = "settingsButton";
            settingsButton.ToolTipText = "Print Button";
            menuStrip1.Items.Add(settingsButton);

            ToolStripItem infoButton = CreateIconButton("\uE8A7", Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH), PrintPreview_Click);
            infoButton.Name = "infoButton";
            infoButton.ToolTipText = "Print Preview";
            menuStrip1.Items.Add(infoButton);



            _deviceStatusPill = new ToolStripLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(210, 30),
                Margin = new Padding(8, 4, 4, 4)
            };
            // IMPORTANT: hook a named method (don’t capture an out-of-scope variable)
            // _deviceStatusPill.Paint += DeviceStatusPill_Paint;
            _deviceStatusPill.Paint += new PaintEventHandler(DeviceStatusPill_Paint);
            menuStrip1.Items.Add(_deviceStatusPill);
            //string[] menuItems = { "File", "View", "Setup", "Markers", "Infusion", "Test Recording", "Windows", "Help" };
            string[] menuItems = { "File", "Setup", "Help", "Pump-Arm" }; //"Pump-Arm"
            for (int i = 0; i < menuItems.Length; i++)
            {
                ToolStripMenuItem menuItem = CreateMenuItem(menuItems[i]);
                menuItem.Margin = new Padding(10, 2, 10, 2);
                menuItem.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                menuItem.Name = menuItems[i] + "Menu";


                switch (menuItems[i])
                {
                    case "File": AddFileMenuItems(menuItem); break;
                    //case "View": AddViewMenuItems(menuItem); break;
                    case "Setup": AddSetupMenuItems(menuItem); break;
                    //case "Markers": AddMarkersMenuItems(menuItem); break;
                    //case "Infusion": AddInfusionMenuItems(menuItem); break;
                    //case "Test Recording": AddTestRecordingMenuItems(menuItem); break;
                    //case "Windows": AddWindowsMenuItems(menuItem); break;
                    case "Help": AddHelpMenuItems(menuItem); break;
                    case "Pump-Arm":
                        menuItem.Name = "PumpArmMenu";
                        menuItem.Click += PumpButton;
                        break;



                        //case "⛯":
                        //    menuItem.Alignment = ToolStripItemAlignment.Right;
                        //    AddSettingMenuItems(menuItem);
                        //    menuItem.ToolTipText = "Setting";
                        //    break;
                }

                menuStrip1.Items.Add(menuItem);
            }


            ToolStripButton CreateIconButton(string iconChar, Color backColor, Color foreColor, Size size, EventHandler onClick)
            {
                var btn = new ToolStripButton
                {
                    Text = iconChar,
                    Font = new Font("Segoe MDL2 Assets", 12F),
                    AutoSize = false,
                    Size = size,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(2, 0, 2, 0)
                };
                if (onClick != null)
                    btn.Click += onClick;

                return btn;
            }


            this.Controls.Add(menuStrip1);
            this.MainMenuStrip = menuStrip1;
        }


        private void PumpButton(object sender, EventArgs e)
        {
            panel8.Visible = !panel8.Visible;

            if (panel8.Visible)
                panel8.BringToFront();
        }


        private bool _isDeviceConnected = false;


        private Bitmap CreateBullet(Color color)
        {
            int size = 12; // image size
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int d = 6; // bullet diameter
                g.FillEllipse(new SolidBrush(color), (size - d) / 2, (size - d) / 2, d, d);
            }
            return bmp;
        }

        private void AddSettingMenuItems(ToolStripMenuItem settingMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem viewItem = new ToolStripMenuItem("View") { Image = bullet };
            AddViewMenuItems(viewItem);

            ToolStripMenuItem markersItem = new ToolStripMenuItem("Markers") { Image = bullet };
            AddMarkersMenuItems(markersItem);

            ToolStripMenuItem infusionItem = new ToolStripMenuItem("Infusion") { Image = bullet };
            AddInfusionMenuItems(infusionItem);

            ToolStripMenuItem testItem = new ToolStripMenuItem("Test Recording") { Image = bullet };
            AddTestRecordingMenuItems(testItem);

            ToolStripMenuItem windowItem = new ToolStripMenuItem("Windows") { Image = bullet };
            AddWindowsMenuItems(windowItem);

            settingMenu.DropDownItems.Add(viewItem);
            settingMenu.DropDownItems.Add(markersItem);
            settingMenu.DropDownItems.Add(infusionItem);
            settingMenu.DropDownItems.Add(testItem);
            settingMenu.DropDownItems.Add(windowItem);
        }

        private void AddToolStripSeparator(ToolStrip targetToolStrip, int height = 35)
        {
            ToolStripSeparator sep = new ToolStripSeparator();
            sep.AutoSize = false;
            sep.Width = 4;
            sep.Height = height;
            sep.Margin = new Padding(1, 0, 1, 30);

            sep.Paint += (s, e) =>
            {
                e.Graphics.Clear(targetToolStrip.BackColor);
                using (Pen p = new Pen(Color.Gray, 2))
                {
                    int x = sep.Width / 2;
                    e.Graphics.DrawLine(p, x, 2, x, sep.Height - 2);
                }
            };

            targetToolStrip.Items.Add(sep);
        }


        private ToolStripControlHost CreateGroup(string heading, params ToolStripButton[] buttons)
        {
            // Outer panel holds heading + buttons row
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Heading
            var lbl = new Label
            {
                Text = heading,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };
            panel.Controls.Add(lbl);

            // Button row
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0),
                WrapContents = false
            };

            foreach (var tsBtn in buttons)
            {
                var btn = new Button
                {
                    Text = tsBtn.Text,
                    BackColor = tsBtn.BackColor,
                    ForeColor = tsBtn.ForeColor,
                    Size = tsBtn.Size,
                    Font = tsBtn.Font,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(1),
                    Padding = new Padding(1),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseCompatibleTextRendering = true
                };

                btn.FlatAppearance.BorderSize = 0;


                if (tsBtn.Tag is EventHandler clickHandler)
                    btn.Click += clickHandler;

                buttonPanel.Controls.Add(btn);
            }

            panel.Controls.Add(buttonPanel);


            return new ToolStripControlHost(panel);
        }


        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private ToolStripMenuItem CreateMenuItem(string text)
        {
            return new ToolStripMenuItem(text)
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
        }

        private void Button_CM_Click(object sender, EventArgs e)
        {
            ReportComments reportsForm = new ReportComments();
            reportsForm.Show();
        }

        private void ButtonForTwo(object sender, EventArgs e)
        {
            Form2 twoForm = new Form2();
            twoForm.Show();
        }


        // All existing menu methods remain the same...
        private void AddFileMenuItems(ToolStripMenuItem fileMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("New") { Image = bullet };
            newMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newMenuItem.Click += (s, args) =>
            {
                //PatientList patientForm = new PatientList();
                //patientForm.Show();

                var patientWithTestForm = new PatientWithTestForm();
                ScreenDimOverlay.ShowDialogWithDim(patientWithTestForm, alpha: 150);

                //var patientForm = new PatientList();
                //ScreenDimOverlay.ShowDialogWithDim(patientForm, alpha: 150);
            };

            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open..") { Image = bullet };
            openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openMenuItem.Click += OpenMenuItem_Click;

            ToolStripMenuItem printSetupMenuItem = new ToolStripMenuItem("Print Setup...") { Image = bullet };
            printSetupMenuItem.Click += PrintSetupMenuItem_Click;

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit") { Image = bullet };
            exitMenuItem.Click += (s, args) => Application.Exit();

            fileMenu.DropDownItems.Add(newMenuItem);
            fileMenu.DropDownItems.Add(openMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(printSetupMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);
        }

        private void AddViewMenuItems(ToolStripMenuItem viewMenu)
        {
            ToolStripMenuItem toolBarMenuItem = new ToolStripMenuItem("Toolbar") { CheckOnClick = true, Checked = true };
            ToolStripMenuItem statusBarMenuItem = new ToolStripMenuItem("Status Bar") { CheckOnClick = true, Checked = true };

            viewMenu.DropDownItems.Add(toolBarMenuItem);
            viewMenu.DropDownItems.Add(statusBarMenuItem);
        }

        private void AddSetupMenuItems(ToolStripMenuItem setupMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem systemMenuItem = new ToolStripMenuItem("System") { Image = bullet };
            //systemMenuItem.Click += (s, args) =>
            //{
            //    var systemForm = new PasswordForm();
            //    ScreenDimOverlay.ShowDialogWithDim(systemForm, alpha: 150);
            //};

            systemMenuItem.Click += (s, args) =>
            {
                var passwordForm = new PasswordForm();

                // Subscribe to the SystemSetupSaved event
                passwordForm.SystemSetupSaved += (sender, e) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        // Call the reload method that runs Constants()
                        ReloadSystemSetupAndConstants();

                        // Show a message to confirm reload (optional)
                        System.Diagnostics.Debug.WriteLine("System Setup reloaded at " + DateTime.Now);
                    }));
                };

                ScreenDimOverlay.ShowDialogWithDim(passwordForm, alpha: 150);
            };

            ToolStripMenuItem hospitalMenuItem = new ToolStripMenuItem("Hospital / Doctor Information") { Image = bullet };
            hospitalMenuItem.Click += (s, args) =>
            {
                var systemForm = new HospitalAndDoctorInfoSetUp();
                ScreenDimOverlay.ShowDialogWithDim(systemForm, alpha: 150);
            };

            //ToolStripMenuItem pressureMenuItem = new ToolStripMenuItem("Scale And Color Setup") { Image = bullet };
            //pressureMenuItem.Click += (s, args) =>
            //{
            //    var pressureStudyForm = new ScaleAndColorSteup();
            //    ScreenDimOverlay.ShowDialogWithDim(pressureStudyForm, alpha: 150);
            //};

            //Start Code For "ScaleANDColorSetup" Screen if update any data so immediately Reflect no need to restart application 26-02-2025
            ToolStripMenuItem pressureMenuItem = new ToolStripMenuItem("Scale And Color Setup") { Image = bullet };
            pressureMenuItem.Click += (s, args) =>
            {
                var pressureStudyForm = new ScaleAndColorSteup();

                // Subscribe to the DataSaved event
                pressureStudyForm.DataSaved += (sender, e) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        ReloadScaleAndColorSetup();
                    }));
                };

                ScreenDimOverlay.ShowDialogWithDim(pressureStudyForm, alpha: 150);
            };
            //END Code For "ScaleANDColorSetup" Screen if update any data so immediately Reflect no need to restart application 26-02-2025

            ToolStripMenuItem patientHistoryMenuItem = new ToolStripMenuItem("Patient Database") { Image = bullet };
            patientHistoryMenuItem.Click += (s, args) =>
            {
                var patientHostoryForm = new PatientHistory();
                ScreenDimOverlay.ShowDialogWithDim(patientHostoryForm, alpha: 150);
            };

            ToolStripMenuItem docterMenuItem = new ToolStripMenuItem("Doctors") { Image = bullet };
            docterMenuItem.Click += (s, args) =>
            {
                DocterList dicterForm = new DocterList();
                dicterForm.Show();
            };

            ToolStripMenuItem symptomsMenuItem = new ToolStripMenuItem("Symptoms") { Image = bullet };
            symptomsMenuItem.Click += (s, args) =>
            {
                SymptomsList symptomsForm = new SymptomsList();
                symptomsForm.Show();
            };

            ToolStripMenuItem LangaugeMenuItem = new ToolStripMenuItem("Language") { Image = bullet };
            LangaugeMenuItem.Click += (s, args) =>
            {
                var languageForm = new LanguageForm();
                ScreenDimOverlay.ShowDialogWithDim(languageForm, alpha: 150);
            };

            ToolStripMenuItem VideoDeviceMenuItem = new ToolStripMenuItem("Video Device") { Image = bullet };
            VideoDeviceMenuItem.Click += (s, args) =>
            {
                var videoDeviceForm = new VideoDevice();
                ScreenDimOverlay.ShowDialogWithDim(videoDeviceForm, alpha: 150);
            };

            setupMenu.DropDownItems.Add(systemMenuItem);
            setupMenu.DropDownItems.Add(hospitalMenuItem);
            setupMenu.DropDownItems.Add(pressureMenuItem);
            setupMenu.DropDownItems.Add(patientHistoryMenuItem);

            setupMenu.DropDownItems.Add(docterMenuItem);
            setupMenu.DropDownItems.Add(symptomsMenuItem);
            setupMenu.DropDownItems.Add(LangaugeMenuItem);
            setupMenu.DropDownItems.Add(VideoDeviceMenuItem);
        }

        private void AddMarkersMenuItems(ToolStripMenuItem markersMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem bladderItem = new ToolStripMenuItem("Bladder Sensation") { Image = bullet };
            bladderItem.DropDownItems.Add(BulletItem("First Sensation (FS)"));
            bladderItem.DropDownItems.Add(BulletItem("First Desire (FD)"));
            bladderItem.DropDownItems.Add(BulletItem("Normal Desire (ND)"));
            bladderItem.DropDownItems.Add(BulletItem("Strong Desire (SD)"));
            bladderItem.DropDownItems.Add(BulletItem("Bladder Capacity (BC)"));
            bladderItem.DropDownItems.Add(BulletItem("End Filling Phase"));
            bladderItem.DropDownItems.Add(BulletItem("Start of Voiding"));

            ToolStripMenuItem generalItem = new ToolStripMenuItem("General") { Image = bullet };
            generalItem.DropDownItems.Add(BulletItem("Leak (L)"));
            generalItem.DropDownItems.Add(BulletItem("Cough (C)"));
            generalItem.DropDownItems.Add(BulletItem("Laugh (La)"));
            generalItem.DropDownItems.Add(BulletItem("Artifact (X)"));
            generalItem.DropDownItems.Add(BulletItem("General Purpose"));
            generalItem.DropDownItems.Add(BulletItem("Standing"));
            generalItem.DropDownItems.Add(BulletItem("Supine"));
            generalItem.DropDownItems.Add(BulletItem("Sitting"));

            markersMenu.DropDownItems.Add(bladderItem);
            markersMenu.DropDownItems.Add(generalItem);
        }

        private ToolStripMenuItem BulletItem(string text)
        {
            return new ToolStripMenuItem(text) { Image = CreateBullet(Color.Black) };
        }

        private void AddInfusionMenuItems(ToolStripMenuItem infusionMenu)
        {
            infusionMenu.DropDownItems.Add(BulletItem("Start Infusion (STI)"));
            infusionMenu.DropDownItems.Add(BulletItem("Stop Infusion (STP)"));
            infusionMenu.DropDownItems.Add(new ToolStripSeparator());
            infusionMenu.DropDownItems.Add(BulletItem("Increase Rate"));
            infusionMenu.DropDownItems.Add(BulletItem("Decrease Rate"));
        }

        private void AddTestRecordingMenuItems(ToolStripMenuItem testRecordingMenu)
        {
            testRecordingMenu.DropDownItems.Add(BulletItem("Start"));
            testRecordingMenu.DropDownItems.Add(BulletItem("Pause"));
            testRecordingMenu.DropDownItems.Add(BulletItem("Stop"));
        }

        private void AddWindowsMenuItems(ToolStripMenuItem windowsMenu)
        {
            windowsMenu.DropDownItems.Add(BulletItem("New Window"));
            windowsMenu.DropDownItems.Add(BulletItem("Cascade"));
            windowsMenu.DropDownItems.Add(BulletItem("Tile"));
            windowsMenu.DropDownItems.Add(BulletItem("Arrange Icons"));
            windowsMenu.DropDownItems.Add(new ToolStripSeparator());
            windowsMenu.DropDownItems.Add(BulletItem("Current Open"));
        }

        private void AddHelpMenuItems(ToolStripMenuItem helpMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About Santron UroDynamics...") { Image = bullet };
            aboutMenuItem.Click += (s, args) =>
            {
                SantronVersion aboutForm = new SantronVersion();
                aboutForm.Show();
            };

            ToolStripMenuItem helpMenuItem = new ToolStripMenuItem("Help Topics") { Image = bullet };
            helpMenuItem.Enabled = false;
            helpMenuItem.Click += (s, args) => MessageBox.Show("Read Only", "Help");

            helpMenu.DropDownItems.Add(aboutMenuItem);
            helpMenu.DropDownItems.Add(helpMenuItem);
        }



        private void OpenMenuItem_Click(object sender, EventArgs e)
        {

            var patientListForm = new PatientList();
            ScreenDimOverlay.ShowDialogWithDim(patientListForm, alpha: 150);

        }

        private void PrintSetupMenuItem_Click(object sender, EventArgs e)
        {
            using (var pageSetup = new PageSetupDialog())
            {
                pageSetup.PageSettings = new PageSettings();
                pageSetup.PrinterSettings = new PrinterSettings();

                if (pageSetup.ShowDialog(this) == DialogResult.OK)
                {
                    // Handle page setup changes
                }
            }
        }



        public class CustomMenuRenderer : ToolStripProfessionalRenderer
        {

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

                // Check if the item is in a DropDown (submenu)
                bool isInDropDown = e.ToolStrip is ToolStripDropDownMenu;

                if (e.Item.Selected)
                {
                    if (isInDropDown)
                    {
                        // Hover background for submenu items
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, 150, 255)), rect);
                    }
                    else
                    {
                        // Hover background for main menu items
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
                else
                {
                    if (!isInDropDown)
                    {
                        // Non-hover main menu background fix (prevent white flash)
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is MenuStrip)
                {
                    // Blue for top-level MenuStrip
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), e.AffectedBounds);
                }
                else
                {
                    // White for dropdown submenus
                    e.Graphics.FillRectangle(Brushes.White, e.AffectedBounds);
                }
            }
        }

        private void ButtonGoLive_Click(object sender, EventArgs e)
        {
            buttonGoLive.BackColor = Color.Green;
            buttonGoLive.Text = "Live";

            liveChart.Visible = true;

            if (updateTimer == null)
            {
                updateTimer = new Timer();
                updateTimer.Interval = 1000; // 1 second
                updateTimer.Tick += UpdateLiveChart;
                updateTimer.Start();
            }
        }


        private void UpdateLiveChart(object sender, EventArgs e)
        {
            foreach (Series s in liveChart.Series)
            {
                if (s.Points.Count > 50)
                    s.Points.RemoveAt(0);

                s.Points.AddY(rand.Next(20, 80)); // Simulated data
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {

        }

        private void button28_Click(object sender, EventArgs e)
        {
            var reportForm = new ReportComments();
            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }

        private void btnCM_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentMainId) ||
                 string.IsNullOrWhiteSpace(_currentPatientNo) ||
                 string.IsNullOrWhiteSpace(_currentTestName))
            {
                MessageBox.Show("No active patient/test. Please open a test first.");
                return;
            }

            var reportForm = new ReportComments();
            reportForm.SetPatientContext(
                _currentMainId,
                _currentPatientNo,
                _currentTestName
            );

            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }

        private void greenButton1_Click(object sender, EventArgs e)
        {
            // “PVES 0” – only zero PVES lane
            // RAW mapping: 0 = PVES
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[0]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(0, currentRawPves);
        }

        private void btnTestStrat_Click(object sender, EventArgs e)
        {

        }

        private void btnTestPause_Click(object sender, EventArgs e)
        {

        }

        private void btnTestStop_Click(object sender, EventArgs e)
        {

        }

        //private void btnCMP2_Click(object sender, EventArgs e)
        //{
        //    var reportForm = new ReportComments();
        //    ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        //}

        private void btnCMP2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentMainId) ||
                string.IsNullOrWhiteSpace(_currentPatientNo) ||
                string.IsNullOrWhiteSpace(_currentTestName))
            {
                MessageBox.Show("No active patient/test. Please open a test first.");
                return;
            }

            var reportForm = new ReportComments();
            reportForm.SetPatientContext(
                _currentMainId,
                _currentPatientNo,
                _currentTestName
            );

            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }


        private void btnCMP4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentMainId) ||
                 string.IsNullOrWhiteSpace(_currentPatientNo) ||
                 string.IsNullOrWhiteSpace(_currentTestName))
            {
                MessageBox.Show("No active patient/test. Please open a test first.");
                return;
            }

            var reportForm = new ReportComments();
            reportForm.SetPatientContext(
                _currentMainId,
                _currentPatientNo,
                _currentTestName
            );

            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }

        private void btnCM4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentMainId) ||
                 string.IsNullOrWhiteSpace(_currentPatientNo) ||
                 string.IsNullOrWhiteSpace(_currentTestName))
            {
                MessageBox.Show("No active patient/test. Please open a test first.");
                return;
            }

            var reportForm = new ReportComments();
            reportForm.SetPatientContext(
                _currentMainId,
                _currentPatientNo,
                _currentTestName
            );

            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }

        private void btnCM5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentMainId) ||
                string.IsNullOrWhiteSpace(_currentPatientNo) ||
                string.IsNullOrWhiteSpace(_currentTestName))
            {
                MessageBox.Show("No active patient/test. Please open a test first.");
                return;
            }

            var reportForm = new ReportComments();
            reportForm.SetPatientContext(
                _currentMainId,
                _currentPatientNo,
                _currentTestName
            );

            ScreenDimOverlay.ShowDialogWithDim(reportForm, alpha: 150);
        }


        //=============== EMG CODE START==============

        private ToolStripStatusLabel _batteryStatusLabel;
        private Timer _batteryCheckTimer;
        // In your constructor or initialization method, add:
        private void SetupBatteryIndication()
        {
            // Create a status strip at the bottom of the form
            var statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            // Create battery status label
            _batteryStatusLabel = new ToolStripStatusLabel
            {
                Text = "⚡ --%",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Alignment = ToolStripItemAlignment.Right,
                Image = GetBatteryIcon(null) // Start with unknown icon
            };

            statusStrip.Items.Add(_batteryStatusLabel);
            this.Controls.Add(statusStrip);

            // Setup battery check timer (read every 30 seconds)
            _batteryCheckTimer = new Timer { Interval = 30000 };
            _batteryCheckTimer.Tick += async (s, e) => await CheckBatteryLevelAsync();
        }

        private System.Drawing.Image GetBatteryIcon(int? level)
        {
            if (level == null)
            {
                // Question mark icon for unknown
                var bmp = new Bitmap(40, 20);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.DrawString("?", new Font("Segoe UI", 12, FontStyle.Bold),
                        Brushes.Gray, new PointF(5, 0));
                }
                return bmp;
            }

            // Create battery icon based on level
            var batteryBmp = new Bitmap(20, 20);
            using (var g = Graphics.FromImage(batteryBmp))
            {
                // Draw battery outline
                using (var pen = new Pen(Color.Black, 1))
                {
                    g.DrawRectangle(pen, 2, 4, 14, 8);
                    g.FillRectangle(Brushes.Black, 16, 6, 2, 4); // Battery tip
                }

                // Fill based on level
                int fillWidth = (int)((level.Value / 100.0) * 12);
                Color fillColor;
                if (level <= 10)
                    fillColor = Color.Red;
                else if (level <= 20)
                    fillColor = Color.Orange;
                else if (level <= 50)
                    fillColor = Color.Yellow;
                else
                    fillColor = Color.Green;

                using (var brush = new SolidBrush(fillColor))
                {
                    g.FillRectangle(brush, 3, 5, fillWidth, 6);
                }
            }
            return batteryBmp;
        }

        private async Task CheckBatteryLevelAsync()
        {
            if (_emgSrc != null && _emgSrc.IsConnected)
            {
                var level = await _emgSrc.ReadBatteryLevelAsync();
                UpdateBatteryDisplay(level);
            }
        }

        private void UpdateBatteryDisplay(int? level)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int?>(UpdateBatteryDisplay), level);
                return;
            }

            if (_batteryStatusLabel != null)
            {
                if (level.HasValue)
                {
                    _batteryStatusLabel.Text = $"⚡ {level.Value}%";
                    _batteryStatusLabel.Image = GetBatteryIcon(level.Value);

                    // Change color based on level
                    if (level <= 10)
                        _batteryStatusLabel.ForeColor = Color.Red;
                    else if (level <= 20)
                        _batteryStatusLabel.ForeColor = Color.Orange;
                    else
                        _batteryStatusLabel.ForeColor = Color.Black;
                }
                else
                {
                    _batteryStatusLabel.Text = "⚡ --%";
                    _batteryStatusLabel.Image = GetBatteryIcon(null);
                    _batteryStatusLabel.ForeColor = Color.Gray;
                }
            }
        }

        private void StartEmg()
        {
            if (_emgStreaming) return;

            _emgSrc = new EmgBleSource
            {
                DeviceNameContains = "NPG-",
                PlotChannelIndex = 0,
                SendStartCommand = true,
                EnableDebugLogs = true
            };
            _emgSrc.OnBatteryLevelChanged += level =>
            {
                UpdateBatteryDisplay(level);

                // Show warning for low battery
                if (level <= 10)
                {
                    BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show(
                            $"⚠️ Low Battery: {level}%\nPlease recharge the EMG device immediately.",
                            "Low Battery Warning",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }));
                }
            };


            if (_useBleEmg)
            {
                // For BLE: bypass EmgLiteEngine, connect directly to source for maximum speed
                _emgSrc.OnSample += (rawValue) =>
                {
                    if (_isPaused || _isPlaybackMode) return;

                    // Simple centering and scaling (bypass heavy filtering for speed)
                    double centered = rawValue - 2048.0; // Center around 0
                    double scaled = centered; // Clamp

                    // Find EMG lane
                    var channelNames = _liveChart?.GetChannelNames();
                    if (channelNames == null) return;

                    int emgIdx = -1;
                    for (int i = 0; i < channelNames.Count; i++)
                    {
                        if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                        {
                            emgIdx = i;
                            break;
                        }
                    }
                    if (emgIdx < 0) return;

                    // Update value and send to chart
                    _filteredEmgValue = Math.Abs(scaled);

                    double bleTime = _orchStartedAt != DateTime.MinValue
                        ? (DateTime.UtcNow - _orchStartedAt).TotalSeconds + _timeBaseShiftSeconds
                        : 0.0;

                    _liveChart?.AppendHighSpeedSample(emgIdx, scaled, bleTime);
                };
            }
            else
            {
                // For DAQ: use EmgLiteEngine with normal processing
                _emgEngine = new EmgLiteEngine(
                    _emgSrc,
                    chart: null,
                    chartUpdateHz: 500,
                    rmsWindowMs: 40,
                    smoothingPercent: 10,
                    outputMode: EmgOutputMode.RmsEnvelope
                );

                _emgEngine.OnEmgPoint += _emgPointHandler;
            }
            // Clear everything
            _emgNotchFilter?.Reset();
            _emgCircularBuffer.Clear();
            _emgHistory.Clear();
            _filteredEmgValue = 0;
            _lastEmgUv = 0;
            // BLE EMG handler with conditional processing
            _emgPointHandler = processedValue =>
            {
                double filtered;

                if (_useBleEmg)
                {
                    // BLE mode: value is already signed/filtered, skip notch filter
                    // (EmgSignalProcessor already applies notch internally)
                    filtered = processedValue;
                }
                else
                {
                    // DAQ mode: apply 50Hz notch filter as before
                    filtered = _emgNotchFilter.Process(processedValue);
                }

                // Store in circular buffer (keep as-is)
                lock (_emgCircularBuffer)
                {
                    _emgCircularBuffer.Enqueue(filtered);
                    if (_emgCircularBuffer.Count > EMG_BUFFER_SIZE)
                        _emgCircularBuffer.Dequeue();
                }
                // Update latest value
                _filteredEmgValue = filtered;
                _lastEmgUv = filtered;
                // ✅ PLOT TO LIVE CHART
                if (_liveChart == null || _isPlaybackMode || _isPaused) return;

                // Find EMG lane index
                var channelNames = _liveChart.GetChannelNames();
                int emgLaneIdx = -1;
                for (int i = 0; i < channelNames.Count; i++)
                {
                    if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                    {
                        emgLaneIdx = i;
                        break;
                    }
                }

                for (int i = 0; i < channelNames.Count; i++)
                {
                    if (channelNames[i].Equals("EMG", StringComparison.OrdinalIgnoreCase))
                    { emgLaneIdx = i; break; }
                }

                // DEBUG: Always log this
                // System.Diagnostics.Debug.WriteLine($"[EMG] Lane search: found={emgLaneIdx}, count={channelNames.Count}");
                if (emgLaneIdx < 0)
                {
                    // System.Diagnostics.Debug.WriteLine("[EMG] EMG lane NOT FOUND! Returning.");
                    return;
                }

                // DEBUG: Log channel info once
                if (_emgStreaming && emgLaneIdx >= 0)
                {
                    _emgStreaming = false; // Log only once
                }

                if (emgLaneIdx < 0) return; // EMG lane not shown for this test

                // Scale value for chart
                double scaledValue = filtered;
                if (_useBleEmg)
                {
                    scaledValue = Math.Abs(scaledValue);
                }

                // Use _lastPlotT as the time to stay synced with main plot clock  
                double emgT = _lastPlotT > double.NegativeInfinity ? _lastPlotT : 0.0;

                if (_useBleEmg)
                {


                    _filteredEmgValue = scaledValue;

                    // Calculate accurate BLE timestamp using elapsed time since test started
                    double bleTime;
                    if (_orchStartedAt != DateTime.MinValue)
                    {
                        bleTime = (DateTime.UtcNow - _orchStartedAt).TotalSeconds + _timeBaseShiftSeconds;
                    }
                    else
                    {
                        bleTime = _lastPlotT > double.NegativeInfinity ? _lastPlotT : 0.0;
                    }

                    // Track update frequency
                    int callCount = 0;
                    DateTime lastLog = DateTime.MinValue;
                    callCount++;
                    if ((DateTime.UtcNow - lastLog).TotalSeconds >= 1.0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EMG RATE] Handler called {callCount} times/sec");
                        callCount = 0;
                        lastLog = DateTime.UtcNow;
                    }

                    _liveChart.AppendHighSpeedSample(emgLaneIdx, scaledValue, bleTime);


                    System.Diagnostics.Debug.WriteLine($"[EMG HS] Value={scaledValue:F1}, Time={bleTime:F3}");
                    _liveChart.AppendHighSpeedSample(emgLaneIdx, scaledValue, bleTime);

                }
                else
                {
                    // DAQ mode: single sample
                    double[] vals = new double[channelNames.Count];
                    for (int i = 0; i < vals.Length; i++)
                        vals[i] = double.NaN;
                    vals[emgLaneIdx] = scaledValue;

                    _liveChart.AppendSample(vals, emgT);
                }
            };


            if (!_useBleEmg)
            {
                _emgEngine.OnEmgPoint += _emgPointHandler;
                _emgEngine.Start();
            }
            else
            {
                _emgSrc.Start();
            }

            _emgStreaming = true;


            _batteryCheckTimer?.Start();
        }


        private void StopEmg()
        {
            try
            {
                _batteryCheckTimer?.Stop();
                if (_emgEngine != null && _emgPointHandler != null)
                    _emgEngine.OnEmgPoint -= _emgPointHandler;

                if (_emgEngine != null)
                {
                    //_emgEngine.OnEmgPoint -= y => { }; // optional, or store handler in a field and detach properly
                    _emgEngine.Stop();
                    _emgEngine.Dispose();
                }
            }
            catch { }
            finally
            {
                _emgEngine = null;
                _emgSrc = null;
                _emgStreaming = false;
                _lastEmgUv = 0;
                _emgPointHandler = null;
                UpdateBatteryDisplay(null);
            }
        }
        //=============== EMG CODE END==============





        //--------------- New Graph -------------//

        private void button2_Click(object sender, EventArgs e)
        {
            _liveChart.AddMarker(_lastPlotT, "FS", Color.Green);
        }





        //Start Test Button Marun Color
        //private void btnTestStart1_Click(object sender, EventArgs e)
        //{
        //    _recorded.Clear();
        //    _isRecording = true;
        //    _isPaused = false;
        //    _pauseStartedAt = null;
        //    _pausedTime = TimeSpan.Zero;
        //    _testStartTime = DateTime.Now;

        //    _testTimer.Start();
        //    UpdateTimerLabel();
        //    UpdateButtons(recording: true, paused: false, canSave: false);
        //}

        private bool ShouldShowConfirmMessage(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return true;  // default: show

            testName = testName.Trim().ToLower();

            // Tests that do NOT require confirmation
            string[] noConfirmTests =
            {
                "uroflowmetry",
                "uroflowmetry + emg",
                "biofeedback"
            };

            return !noConfirmTests.Contains(testName);
        }



        private void btnTestStart1_Click(object sender, EventArgs e)
        {

            // If already recording and NOT paused -> do nothing (do NOT clear/reset)
            if (_isRecording && !_isPaused)
            {
                // simply ignore the click
                return;
            }

            // If recording but currently paused -> resume (do not reset)
            //if (_isRecording && _isPaused)
            //{
            //    _isPaused = false;
            //    if (_pauseStartedAt.HasValue)
            //    {
            //        _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
            //        _pauseStartedAt = null;
            //    }

            //    _testTimer.Start();
            //    UpdateTimerLabel();
            //    UpdateButtons(recording: true, paused: false, canSave: false);
            //    return;
            //}

            // If recording but currently paused -> resume (do not reset)
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    var pausedDuration = (DateTime.Now - _pauseStartedAt.Value);
                    _pausedTime += pausedDuration;
                    _pauseStartedAt = null;
                }

                _testTimer.Start();

                // RESTART the auto-save timer FRESH when resuming via start button
                if (_monitorNoFlow)
                {
                    _lastMovementTime = DateTime.Now;  // Reset to current time
                    _noFlowTimer.Start();              // Start fresh
                }

                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            // Show confirmation message before starting the test
            if (ShouldShowConfirmMessage(currenttest))
            {
                DialogResult result = MessageBox.Show(
                   "Have you taken cough test?",
                   "Confirm Start",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            btnTestStart1.Enabled = false;
            if (_orch == null)
            {
                try
                {
                    LoadGraph(); // This starts DAQ and wires OnRawSampleForZeroing
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to start device: " + ex.Message);
                    btnTestStart1.Enabled = true;
                    return;
                }
            }
            System.Threading.Thread.Sleep(500); // Give DAQ time to populate _lastRawCounts

            // ✅ NOW validate
            if (!ValidatePreTestConditions())
            {
                btnTestStart1.Enabled = true;
                return;
            }

            // Continue with rest of your code...
            ForceFreshStartForLiveGraph();

            //Start code for AUTO-STOP Means Auto Savedtest only for UROFLOW tests 12/12/2025
            if (currenttest.Equals("Uroflowmetry", StringComparison.OrdinalIgnoreCase) ||
                currenttest.Equals("Uroflowmetry + EMG", StringComparison.OrdinalIgnoreCase))
            {
                _monitorNoFlow = true;
                _lastMovementTime = DateTime.Now;
                _lastVolumeValue = 0;
                _noFlowTimer.Start();
            }
            else
            {
                _monitorNoFlow = false;
                _noFlowTimer.Stop();
            }
            //End code for AUTO-STOP Means Auto Savedtest only for UROFLOW tests 12/12/2025

            // ✅ Show the timer when the test starts
            if (_lblTimer != null)
                _lblTimer.Visible = true;

            label36.Visible = false;

            btnTestPause1.Enabled = true;
            btnTestStop1.Enabled = true;
            btnTestSave1.Enabled = true;

            // ✅ FIX: ALWAYS reset timer when starting a new test (not just if !_isRecording)
            // This ensures that clicking Start after Demo mode will start from 0
            if (!_isRecording)
            {
                // Starting fresh recording
                ResetTimeForNewTest();
                _recorded.Clear();
                _isRecording = true;
                _isPaused = false;
                _pauseStartedAt = null;
                _pausedTime = TimeSpan.Zero;
                _testStartTime = DateTime.Now;

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }



            StartLiveTest();



        }
        private TestOrchestrator _testOrchestrator;

        //Start code for AUTO-STOP Measn Auto Savedtest only for UROFLOW tests 12/12/2025
        private void NoFlowTimer_Tick(object sender, EventArgs e)
        {
            if (!_monitorNoFlow || !_isRecording) return;

            TimeSpan noFlowFor = DateTime.Now - _lastMovementTime;

            if (noFlowFor.TotalSeconds >= NO_FLOW_TIMEOUT_SECONDS)
            {
                _monitorNoFlow = false;
                _noFlowTimer.Stop();

                this.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        "Test stopped automatically due to no flow for 30 seconds.",
                        "Auto Stop",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    btnTestStop1_Click(btnTestStop1, EventArgs.Empty);

                }));
            }
        }
        //End code for AUTO-STOP Measn Auto Savedtest only for UROFLOW tests 12/12/2025


        //Bhushan comment on 13-01-2026
        //private void ForceFreshStartForLiveGraph()
        //{
        //    bool oldAccept = _acceptLiveFrames;
        //    _acceptLiveFrames = false;

        //    try
        //    {
        //        _liveChart.Stop();
        //        _liveChart.Clear();
        //        _liveChart.ClearMarkers();

        //        // 🔥 THIS IS THE MISSING LINE
        //        //_orch.Proc.ResetAllChannels();
        //        _orch.Proc.ZeroNonPressureChannels(_lastRawCounts, 0, 1);


        //        _liveChart.Start();
        //        _liveChart.ToggleLive(true);
        //        _liveChart.ScrollToLive();
        //        _liveChart.Invalidate();
        //    }
        //    finally
        //    {
        //        _acceptLiveFrames = oldAccept;
        //    }
        //}

        private void ForceFreshStartForLiveGraph()
        {
            bool oldAccept = _acceptLiveFrames;
            _acceptLiveFrames = false;

            try
            {
                _liveChart.Stop();
                _liveChart.Clear();
                _liveChart.ClearMarkers();

                // IMPORTANT: reset time clamps so a new test can start from 0
                _lastPlotT = double.NegativeInfinity;
                _lastChartEmitT = double.NegativeInfinity;

                _resumeShiftSeconds = 0.0;
                _resumeOffset = 0.0;
                _resumeArmed = false;

                ArmTimeZero();

                // 1) Reset the display pipeline so EMA/decim history and post-zero skip counter reset
                _orch.Proc.ResetForLiveGraphExceptPressureChannels();

                // 2) Apply baseline to non-pressure channels (keep pressure channels as-is)
                _orch.Proc.ZeroNonPressureChannels(_lastRawCounts, 0, 1);

                // 3) If you want QVOL to start exactly from 0 visually, force its baseline too
                //    (keep this only if channel index 6 is QVOL in your mapping)
                _orch.Proc.ForceChannelZero(6, _lastRawCounts[6]);

                _liveChart.Start();
                _liveChart.ToggleLive(true);
                _liveChart.ScrollToLive();
                _liveChart.Invalidate();
            }
            finally
            {
                _acceptLiveFrames = oldAccept;
            }
        }

        private void EnsureOrchestrator()
        {
            if (_testOrchestrator == null)
            {
                // 1) Create DAQ service
                IDaqService daq = new DaqService();

                // 2) Create calibration profile + calibration
                CalibrationProfile profile = new CalibrationProfile();
                ICalibration cal = new Calibration(profile);

                // 3) Signal processor with calibration
                //ISignalProcessor proc = new SignalProcessor(cal);
                ISignalProcessor proc = new SignalProcessor(cal, sampleRateHz: 400.0, displayHz: 8.0);

                // 4) Pump controller (your real class)
                IPumpController pump = new PumpController();  // no parameters needed

                // 5) System setup store (use YOUR real class)
                ISysSetupStore store = new SysSetupStore();   // <-- your existing class

                // 6) Create final orchestrator
                _testOrchestrator = new TestOrchestrator(daq, proc, pump, store);
            }
        }



        private void btnTestStart2_Click(object sender, EventArgs e)
        {

            // If already recording and NOT paused -> do nothing (do NOT clear/reset)
            if (_isRecording && !_isPaused)
            {
                // simply ignore the click
                return;
            }

            // If recording but currently paused -> resume (do not reset)
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            // Show confirmation message before starting the test
            if (ShouldShowConfirmMessage(currenttest))
            {
                DialogResult result = MessageBox.Show(
                   "Have you taken cough test?",
                   "Confirm Start",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            if (!ValidatePreTestConditions())
                return;

            ForceFreshStartForLiveGraph();
            PumpStart();
            _lblInfusion.Visible = true;

            btnTestStart2.Enabled = false;


            // ✅ Show the timer when the test starts
            if (_lblTimer != null)
                _lblTimer.Visible = true;

            label36.Visible = false;

            btnTestPause2.Enabled = true;
            btnTestStop2.Enabled = true;
            btnTestSave2.Enabled = true;

            // If not recording yet -> start a fresh session
            if (!_isRecording)
            {
                ResetTimeForNewTest();
                _recorded.Clear();
                _isRecording = true;
                _isPaused = false;
                _pauseStartedAt = null;
                _pausedTime = TimeSpan.Zero;
                _testStartTime = DateTime.Now;

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }
            StartLiveTest();

        }



        private void btnTestStart3_Click(object sender, EventArgs e)
        {
            // If already recording and NOT paused -> do nothing
            if (_isRecording && !_isPaused)
            {
                return;
            }

            // If recording but currently paused -> resume
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }
                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            // Show confirmation message
            if (ShouldShowConfirmMessage(currenttest))
            {
                DialogResult result = MessageBox.Show(
                   "Have you taken cough test?",
                   "Confirm Start",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }


            if (!ValidatePreTestConditions())
                return;


            // Now safe to proceed
            ForceFreshStartForLiveGraph();
            PumpStart();
            _lblInfusion.Visible = true;

            btnTestStart3.Enabled = false;

            if (_lblTimer != null)
                _lblTimer.Visible = true;

            label36.Visible = false;

            btnTestPause3.Enabled = true;
            btnTestStop3.Enabled = true;
            btnTestSave3.Enabled = true;

            if (!_isRecording)
            {
                ResetTimeForNewTest();
                _recorded.Clear();
                _isRecording = true;
                _isPaused = false;
                _pauseStartedAt = null;
                _pausedTime = TimeSpan.Zero;
                _testStartTime = DateTime.Now;



                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            StartLiveTest();
        }
        private void btnTestStart4_Click(object sender, EventArgs e)
        {

            // If already recording and NOT paused -> do nothing (do NOT clear/reset)
            if (_isRecording && !_isPaused)
            {
                // simply ignore the click
                return;
            }

            // If recording but currently paused -> resume (do not reset)
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            // Show confirmation message before starting the test
            if (ShouldShowConfirmMessage(currenttest))
            {
                DialogResult result = MessageBox.Show(
                   "Have you taken cough test?",
                   "Confirm Start",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            if (!ValidatePreTestConditions())
                return;

            ForceFreshStartForLiveGraph();

            btnTestStart4.Enabled = false;


            // ✅ Show the timer when the test starts
            if (_lblTimer != null)
                _lblTimer.Visible = true;

            label36.Visible = false;

            btnTestPause4.Enabled = true;
            btnTestStop4.Enabled = true;
            btnTestSave4.Enabled = true;

            // If not recording yet -> start a fresh session
            if (!_isRecording)
            {
                ResetTimeForNewTest();

                _recorded.Clear();
                _isRecording = true;
                _isPaused = false;
                _pauseStartedAt = null;
                _pausedTime = TimeSpan.Zero;
                _testStartTime = DateTime.Now;

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }




            StartLiveTest();
        }

        private void btnTestStart5_Click(object sender, EventArgs e)
        {

            // If already recording and NOT paused -> do nothing (do NOT clear/reset)
            if (_isRecording && !_isPaused)
            {
                // simply ignore the click
                return;
            }

            // If recording but currently paused -> resume (do not reset)
            if (_isRecording && _isPaused)
            {
                _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }

                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }

            // Show confirmation message before starting the test
            if (ShouldShowConfirmMessage(currenttest))
            {
                DialogResult result = MessageBox.Show(
                   "Have you taken cough test?",
                   "Confirm Start",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            if (!ValidatePreTestConditions())
                return;

            ForceFreshStartForLiveGraph();

            btnTestStart5.Enabled = false;

            // ✅ Show the timer when the test starts
            if (_lblTimer != null)
                _lblTimer.Visible = true;

            label36.Visible = false;

            btnTestPause5.Enabled = true;
            btnTestStop5.Enabled = true;
            btnTestSave5.Enabled = true;

            // If not recording yet -> start a fresh session
            if (!_isRecording)
            {

                ResetTimeForNewTest();
                _recorded.Clear();
                _isRecording = true;
                _isPaused = false;
                _pauseStartedAt = null;
                _pausedTime = TimeSpan.Zero;
                _testStartTime = DateTime.Now;



                _testTimer.Start();
                UpdateTimerLabel();
                UpdateButtons(recording: true, paused: false, canSave: false);
                return;
            }




            StartLiveTest();
        }

        //Time Code

        private const int MAX_TEST_DURATION_MINUTES = 90;
        private const int MAX_TEST_DURATION_MINUTES_UROFLOWMETRY = 10;
        private void UpdateTimerLabel()
        {
            if (!_isRecording)
            {
                _lblTimer.Text = "00:00";
                return;
            }

            var pauseSoFar = (_isPaused && _pauseStartedAt.HasValue)
                ? (DateTime.Now - _pauseStartedAt.Value)
                : TimeSpan.Zero;

            var elapsed = (DateTime.Now - _testStartTime) - (_pausedTime + pauseSoFar);
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            // Use TotalSeconds and truncate (not round) to match recorded data exactly
            int totalSeconds = (int)elapsed.TotalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            _lblTimer.Text = $"{minutes:00}:{seconds:00}";
            int maxDuration = MAX_TEST_DURATION_MINUTES;
            if (!string.IsNullOrEmpty(_currentTestName) &&
                _currentTestName.IndexOf("Uroflowmetry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                maxDuration = MAX_TEST_DURATION_MINUTES_UROFLOWMETRY;
            }
            // Auto-stop after 90 minutes
            if (minutes >= maxDuration)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    //MessageBox.Show($"Test automatically stopped after {maxDuration} minutes.",
                    //                "Auto Stop",
                    //                MessageBoxButtons.OK,
                    //                MessageBoxIcon.Information);
                    TestStop();

                });
            }

            //_lblTimer.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        //private void UpdateTimerLabel()
        //{
        //    if (!_isRecording)
        //    {
        //        _lblTimer.Text = "00:00";
        //        return;
        //    }

        //    var pauseSoFar = (_isPaused && _pauseStartedAt.HasValue)
        //        ? (DateTime.Now - _pauseStartedAt.Value)
        //        : TimeSpan.Zero;

        //    var elapsed = (DateTime.Now - _testStartTime) - (_pausedTime + pauseSoFar);
        //    if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        //    // Show HH:mm (minutes are lowercase mm)
        //    _lblTimer.Text = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        //    //_lblTimer.Text = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        //}

        private void UpdateButtons(bool recording, bool paused, bool canSave)
        {
            _btnStart.Enabled = !recording;
            _btnPause.Enabled = recording && !paused;
            _btnResume.Enabled = recording && paused;
            _btnStop.Enabled = recording;
            _btnSave.Enabled = canSave;
        }




        //Pause Test Button Marun Color
        //private void btnTestPause1_Click(object sender, EventArgs e)
        //{
        //    DialogResult result = MessageBox.Show(
        //        "Are you sure you want to Pause the test?",
        //        "Confirm Pause",
        //        MessageBoxButtons.YesNo,
        //        MessageBoxIcon.Question
        //    );

        //    if (result != DialogResult.Yes)
        //    {
        //        return;
        //    }

        //    if (!_isRecording || _isPaused) return;
        //    _isPaused = true;
        //    _pauseStartedAt = DateTime.Now;
        //    UpdateTimerLabel();
        //    UpdateButtons(recording: true, paused: true, canSave: _recorded.Count > 0);
        //}

        private async void btnTestPause1_Click(object sender, EventArgs e)
        {
            // If already paused, don't do anything
            if (_isPaused) return;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            // Stop timer and processing
            _testTimer.Stop();

            _liveChart.Pause();

            _arm?.StopArm();

            UpdateTimerLabel();

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            if (_orch != null)
                _orch.StopProcessing();
            // Show non-blocking pause form
            using (var pauseForm = new PauseForm())
            {
                pauseForm.ShowDialog();

                // Resume when form closes
                await ResumeAfterPause();
            }
        }

        //private void btnTestPause1_Click(object sender, EventArgs e)
        //{

        //    // If already paused, don't do anything
        //    if (_isPaused) return;

        //    _isPaused = true;
        //    _pauseStartedAt = DateTime.Now;

        //    // Stop timer and processing
        //    _testTimer.Stop();

        //    if (_orch != null)
        //        _orch.StopProcessing();

        //    _arm?.StopArm();

        //    UpdateTimerLabel();

        //    // Store pump state before stopping
        //    _pumpWasRunningBeforePause = _orch?.Pump?.IsRunning ?? false;

        //    // Change timer color to indicate pause
        //    if (_lblTimer != null)
        //        _lblTimer.ForeColor = Color.Red;

        //    // Show popup
        //    DialogResult result = MessageBox.Show(
        //        "Recording Paused. Click OK to resume.",
        //        "Recording Paused",
        //        MessageBoxButtons.OK,
        //        MessageBoxIcon.Information
        //    );

        //    // Resume when user clicks OK
        //    if (result == DialogResult.OK)
        //    {
        //        // Calculate pause duration
        //        if (_pauseStartedAt.HasValue)
        //        {
        //            var pausedDuration = (DateTime.Now - _pauseStartedAt.Value);

        //            // Add to total paused time
        //            _pausedTime += pausedDuration;

        //            // IMPORTANT: Store the offset but DON'T add it to _timeOffsetAfterPause
        //            // Instead, we'll use this to adjust future samples
        //            _pauseStartedAt = null;
        //        }

        //        _isPaused = false;

        //        // Restore color and resume timer
        //        if (_lblTimer != null)
        //            _lblTimer.ForeColor = Color.Black;

        //        _testTimer.Start();
        //        UpdateTimerLabel();

        //        // Resume processing
        //        if (_orch != null)
        //            _orch.StartProcessing();
        //    }

        //    UpdateButtons(recording: true, paused: _isPaused, canSave: _recorded.Count > 0);
        //}

        private bool _isPumpRunning = false;
        private bool _pumpWasRunningBeforePause = false;



        //private void btnTestPause2_Click(object sender, EventArgs e)
        //{
        //    // If already paused, don't do anything
        //    if (_isPaused) return;

        //    _isPaused = true;
        //    _pauseStartedAt = DateTime.Now;

        //    // ✅ Store TRUE actual state
        //    _pumpWasRunningBeforePause = _isPumpRunning;

        //    // Stop timer and processing
        //    _testTimer.Stop();

        //    if (_orch != null)
        //        _orch.StopProcessing();

        //    _arm?.StopArm();

        //    UpdateTimerLabel();

        //    // Stop pump ONLY if it is running
        //    if (_isPumpRunning)
        //    {
        //        PumpStop();
        //    }

        //    // Change timer color to indicate pause
        //    if (_lblTimer != null)
        //        _lblTimer.ForeColor = Color.Red;



        //    // Show popup
        //    DialogResult result = MessageBox.Show(
        //        "Recording Paused. Click OK to resume.",
        //        "Recording Paused",
        //        MessageBoxButtons.OK,
        //        MessageBoxIcon.Information
        //    );

        //    // Resume when user clicks OK
        //    if (result == DialogResult.OK)
        //    {
        //        // Calculate pause duration
        //        if (_pauseStartedAt.HasValue)
        //        {
        //            var pausedDuration = (DateTime.Now - _pauseStartedAt.Value );

        //            // Add to total paused time
        //            _pausedTime += pausedDuration; 

        //            // IMPORTANT: Store the offset but DON'T add it to _timeOffsetAfterPause
        //            // Instead, we'll use this to adjust future samples
        //            _pauseStartedAt = null;
        //        }

        //        _isPaused = false;

        //        // ✅ Start pump ONLY if it was running before pause
        //        if (_pumpWasRunningBeforePause)
        //        {
        //            PumpStart();
        //        }

        //        // Restore color and resume timer
        //        if (_lblTimer != null)
        //            _lblTimer.ForeColor = Color.Black;

        //        _testTimer.Start();
        //        UpdateTimerLabel();

        //        // Resume processing
        //        if (_orch != null)
        //            _orch.StartProcessing();
        //    }

        //    UpdateButtons(recording: true, paused: _isPaused, canSave: _recorded.Count > 0);
        //}









        //----------------------------------------------------------------27/02/2026--------------

        private async void btnTestPause2_Click(object sender, EventArgs e)
        {
            // If already paused, don't do anything
            if (_isPaused) return;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            // ✅ Store TRUE actual state
            _pumpWasRunningBeforePause = _isPumpRunning;

            // Stop timer and processing
            _testTimer.Stop();

            _liveChart.Pause();

            _arm?.StopArm();

            UpdateTimerLabel();

            // Stop pump ONLY if it is running
            if (_isPumpRunning)
            {
                PumpStop();
            }

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            if (_orch != null)
                _orch.StopProcessing();
            // Show non-blocking pause form
            using (var pauseForm = new PauseForm())
            {
                pauseForm.ShowDialog();

                // Resume when form closes
                await ResumeAfterPause();
            }
        }

        private async System.Threading.Tasks.Task ResumeAfterPause()
        {
            // Calculate pause duration
            if (_pauseStartedAt.HasValue)
            {
                var pausedDuration = (DateTime.Now - _pauseStartedAt.Value);
                _pausedTime += pausedDuration;
                _pauseStartedAt = null;
            }

            //p
            _resumeOffset = _lastPlotT;   // chart should continue from where it left off
            _resumeArmed = true;

            _isPaused = false;

            // Resume processing
            if (_orch != null)
                _orch.StartProcessing();

            _liveChart.Resume();
            // Start pump ONLY if it was running before pause
            if (_pumpWasRunningBeforePause)
            {
                PumpStart();
            }

            // Restore color and resume timer
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Black;

            _testTimer.Start();
            UpdateTimerLabel();

            // Small yield to let first data point arrive before chart unpauses
            await System.Threading.Tasks.Task.Delay(300);

            UpdateButtons(recording: true, paused: _isPaused, canSave: _recorded.Count > 0);
        }
        //-----------------------------------------------------------------------------------
        private async void btnTestPause3_Click(object sender, EventArgs e)
        {
            // If already paused, don't do anything
            if (_isPaused) return;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            // ✅ Store TRUE actual state
            _pumpWasRunningBeforePause = _isPumpRunning;

            // Stop timer and processing
            _testTimer.Stop();

            _liveChart.Pause();

            _arm?.StopArm();

            UpdateTimerLabel();

            // Stop pump ONLY if it is running
            if (_isPumpRunning)
            {
                PumpStop();
            }

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            if (_orch != null)
                _orch.StopProcessing();
            // Show non-blocking pause form
            using (var pauseForm = new PauseForm())
            {
                pauseForm.ShowDialog();

                // Resume when form closes
                await ResumeAfterPause();
            }
        }

        private async void btnTestPause4_Click(object sender, EventArgs e)
        {
            // If already paused, don't do anything
            if (_isPaused) return;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            // Stop timer and processing
            _testTimer.Stop();

            _liveChart.Pause();

            _arm?.StopArm();

            UpdateTimerLabel();

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            if (_orch != null)
                _orch.StopProcessing();
            // Show non-blocking pause form
            using (var pauseForm = new PauseForm())
            {
                pauseForm.ShowDialog();

                // Resume when form closes
                await ResumeAfterPause();
            }
        }

        private async void btnTestPause5_Click(object sender, EventArgs e)
        {
            // If already paused, don't do anything
            if (_isPaused) return;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            // Stop timer and processing
            _testTimer.Stop();

            _liveChart.Pause();

            _arm?.StopArm();

            UpdateTimerLabel();

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;

            if (_orch != null)
                _orch.StopProcessing();
            // Show non-blocking pause form
            using (var pauseForm = new PauseForm())
            {
                pauseForm.ShowDialog();

                // Resume when form closes
                await ResumeAfterPause();
            }
        }

        private void AddOrReplaceMarker(string label, Color color)
        {
            // Remove existing marker with same label
            _liveChart.RemoveMarkerByLabel(label);

            // Add a new marker at current time
            _liveChart.AddMarker(_lastPlotT, label, color);
        }

        private void ClearChartClickEvents()
        {
            _liveChart.ChartClicked -= LiveChart_ChartClicked;
            _liveChart.ChartClicked -= FS_ChartClicked;
        }



        private async void btnFS_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "FS";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("FS", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private async void btnFD_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "FD";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("FD", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }

        }

        private async void btnND_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "ND";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("ND", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }

        }

        private async void btnSD_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "SD";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("SD", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }

        }

        private async void btnBC_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            PumpStop();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "BC";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("BC", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }

        }

        private async void btnFS6_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "FS";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("FS", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private async void btnFD6_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "FD";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("FD", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private async void btnND6_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "ND";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("ND", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private async void btnSD6_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "SD";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("SD", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private async void btnBC6_Click(object sender, EventArgs e)
        {
            // Add a small delay to prevent double clicking
            await System.Threading.Tasks.Task.Delay(100);

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                // Enable marker placement mode for FS marker
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "BC";
                _pendingMarkerColor = Color.FromArgb(BladderColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'FS' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += FS_ChartClicked;
            }
            else
            {
                // live mode: remove old FS marker and add new
                AddOrReplaceMarker("BC", Color.FromArgb(BladderColor));
                MultiClick(sender);
            }
        }

        private int _markerCounter = 0;

        public async void MultiClick(object sender)
        {
            if (sender is Control ctrl)
            {
                if (!ctrl.Enabled) return;
                ctrl.Enabled = false;
                await System.Threading.Tasks.Task.Delay(200);
                ctrl.Enabled = true;
            }
        }

        private void RemoveAllGpMarkers()
        {
            // Remove all markers that have numeric labels (GP markers)
            var numericMarkers = _liveChart.Markers
                .Where(m => int.TryParse(m.Label, out _))
                .Select(m => m.Label)
                .ToList();

            foreach (var label in numericMarkers)
            {
                _liveChart.RemoveMarkerByLabel(label);
            }
        }

        private int GetNextAvailableMarkerNumber()
        {
            // Get all existing GP marker labels (they're numbered as strings)
            var existingNumbers = _liveChart.Markers
                .Where(m => int.TryParse(m.Label, out _))
                .Select(m => int.Parse(m.Label))
                .OrderBy(n => n)
                .ToList();

            // Find the first gap in the sequence
            int nextNumber = 1;
            foreach (var num in existingNumbers)
            {
                if (num == nextNumber)
                    nextNumber++;
                else
                    break;
            }

            return nextNumber;
        }


        private void btnGP_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            if (_isPlaybackMode)
            {

                _isMarkerPlacementModeForCount = true;
                _pendingMarkerLabelForCount = GetNextAvailableMarkerNumber().ToString();
                _pendingMarkerColorForCount = Color.FromArgb(GPColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the GP marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClickedForCount;
            }
            else
            {
                // Normal live mode
                int nextNum = GetNextAvailableMarkerNumber();
                _liveChart.AddMarker(_lastPlotT, nextNum.ToString(), Color.FromArgb(GPColor));
                MultiClick(sender);
            }

            //_markerCounter++;
            //_liveChart.AddMarker(_lastPlotT, _markerCounter.ToString(), Color.FromArgb(GPColor));
            //MultiClick(sender);
        }

        private DateTime _lastClickTime = DateTime.MinValue;
        private void btnEventGP_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            if (_isPlaybackMode)
            {

                _isMarkerPlacementModeForCount = true;
                _pendingMarkerLabelForCount = GetNextAvailableMarkerNumber().ToString();
                _pendingMarkerColorForCount = Color.FromArgb(GPColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the GP marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClickedForCount;
            }
            else
            {
                // Normal live mode
                int nextNum = GetNextAvailableMarkerNumber();
                _liveChart.AddMarker(_lastPlotT, nextNum.ToString(), Color.FromArgb(GPColor));
                MultiClick(sender);
            }
        }

        private void btnEventGP3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            if (_isPlaybackMode)
            {

                _isMarkerPlacementModeForCount = true;
                _pendingMarkerLabelForCount = GetNextAvailableMarkerNumber().ToString();
                _pendingMarkerColorForCount = Color.FromArgb(GPColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the GP marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClickedForCount;
            }
            else
            {
                // Normal live mode
                int nextNum = GetNextAvailableMarkerNumber();
                _liveChart.AddMarker(_lastPlotT, nextNum.ToString(), Color.FromArgb(GPColor));
                MultiClick(sender);
            }
            //_markerCounter++;
            //_liveChart.AddMarker(_lastPlotT, _markerCounter.ToString(), Color.FromArgb(GPColor));
            //MultiClick(sender);
        }

        private void btnGP6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            if (_isPlaybackMode)
            {
                _isMarkerPlacementModeForCount = true;
                _pendingMarkerLabelForCount = GetNextAvailableMarkerNumber().ToString();
                _pendingMarkerColorForCount = Color.FromArgb(GPColor);
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the GP marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClickedForCount;
            }
            else
            {
                // Normal live mode
                int nextNum = GetNextAvailableMarkerNumber();
                _liveChart.AddMarker(_lastPlotT, nextNum.ToString(), Color.FromArgb(GPColor));
                MultiClick(sender);
            }
        }


        private double? _r1 = null;
        private double? _r2 = null;

        private void btnR1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R1";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r1 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R1", Color.Red);
                MultiClick(sender);
            }
        }

        private void btnR2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R2";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r2 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R2", Color.Red);
                MultiClick(sender);
            }

        }

        private void btnArtifactR1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R1";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r1 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R1", Color.Red);
                MultiClick(sender);
            }
        }

        private void btnArtifactR2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R2";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R2' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r2 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R2", Color.Red);
                MultiClick(sender);
            }
        }


        private void btnMarkingR1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R1";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r1 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R1", Color.Red);
                MultiClick(sender);
            }
        }

        private void btnR1P6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R1";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r1 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R1", Color.Red);
                MultiClick(sender);
            }
        }

        private void btnMarkingR2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R2";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r2 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R2", Color.Red);

                MultiClick(sender);
            }

        }

        private void btnR2P6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();
            ExitMarkerRemoveMode();
            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "R2";
                _pendingMarkerColor = Color.Red;
                Cursor = Cursors.Cross;

                //MessageBox.Show("Click on the chart to place the 'R1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += Artifact_ChartClicked;
            }
            else
            {
                _r2 = _lastPlotT;
                _liveChart.AddMarker(_lastPlotT, "R2", Color.Red);

                MultiClick(sender);
            }
        }

        private bool _isMarkerRemoveMode;
        private bool _removeFsOnNextClick = false;
        private bool _blockFsPlacement;
        private bool _removeGpOnNextClick = false;
        private bool _blockGpPlacement = false;


        private void btnM_Click(object sender, EventArgs e)
        {

            _isMarkerRemoveMode = true;

            _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
            _liveChart.ChartClicked += Chart_RemoveMarkerOnClick;

            // Nothing to remove
            if (_artifactRanges.Count == 0)
                return;

            //Start this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
            //foreach (var range in _artifactRanges)
            //{
            //    _liveChart.RemoveSamplesBetween(range.R1, range.R2);
            //}

            //Cursor = Cursors.Hand;

            //ExitMarkerRemoveMode();
            //End this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
        }



        //Start Code Remove Already Added Marked Click on "M" button and click Mark Lines so Remove the Lines add on 19/11/2025

        //Code for If Click on M button so Remove this marker this Marker Line clicked chnage on 14-02-2026
        private void Chart_RemoveMarkerOnClick(double clickedX)
        {
            if (!_isMarkerRemoveMode)
                return;

            // Get all markers sorted by distance to click
            var markers = _liveChart.Markers
                .Select((m, index) => new { Marker = m, Index = index, Distance = Math.Abs(m.T - clickedX) })
                .Where(x => x.Distance < 0.2)  // Only consider markers within threshold
                .OrderBy(x => x.Distance)
                .ToList();

            if (markers.Count > 0)
            {
                // Get the closest marker
                var closest = markers[0];

                // Remove by index if your chart supports it
                _liveChart.RemoveMarkerAt(closest.Index);

                _isMarkerRemoveMode = false;
                _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
                Cursor = Cursors.Default;
            }
        }

        //Code for If Click on M button so Remove the first added marker this Marker Line not remove the  clicked marker line chnage on 14-02-2026

        //private void Chart_RemoveMarkerOnClick(double clickedX)
        //{
        //    if (!_isMarkerRemoveMode)
        //        return;

        //    // find marker with T closest to clicked X
        //    var marker = _liveChart.Markers
        //        .OrderBy(m => Math.Abs(m.T - clickedX))
        //        .FirstOrDefault();

        //    // Threshold depends on sampling, 0.2 sec works best
        //    if (marker != null && Math.Abs(marker.T - clickedX) < 0.2)
        //    {
        //        _liveChart.RemoveMarker(marker.Label);

        //        //Code For "M" button click Only one Mark line remove 
        //        _isMarkerRemoveMode = false;
        //        _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;

        //        Cursor = Cursors.Default;
        //    }
        //}


        //End Code Remove Already Added Marked Click on "M" button and click Mark Lines so Remove the Lines add on 19/11/2025

        //private void btnArtifactM_Click(object sender, EventArgs e)
        //{
        //    //_liveChart.ClearMarkers();

        //    if (_r1.HasValue && _r2.HasValue)
        //    {
        //        double min = Math.Min(_r1.Value, _r2.Value);
        //        double max = Math.Max(_r1.Value, _r2.Value);

        //        //_liveChart.ClearMarkers();

        //        //_liveChart.RemoveMarker("R1");
        //        //_liveChart.RemoveMarker("R2");

        //        //_liveChart.RemoveSamplesBetween(min, max);

        //        _r1 = _r2 = null;
        //    }

        //    // Enable remove mode
        //    _isMarkerRemoveMode = true;

        //    //This code For Remove Mark Line Click Line Button and remove 
        //    //_removeFsOnNextClick = true;
        //    //_blockFsPlacement = true;
        //    //_removeGpOnNextClick = true;
        //    //_blockGpPlacement = true;


        //    // subscribe Action<double>
        //    _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
        //    _liveChart.ChartClicked += Chart_RemoveMarkerOnClick;

        //    Cursor = Cursors.Hand;
        //}

        private void btnArtifactM_Click(object sender, EventArgs e)
        {
            _isMarkerRemoveMode = true;

            _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
            _liveChart.ChartClicked += Chart_RemoveMarkerOnClick;

            // Nothing to remove
            if (_artifactRanges.Count == 0)
                return;

            //Start this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
            //foreach (var range in _artifactRanges)
            //{
            //    _liveChart.RemoveSamplesBetween(range.R1, range.R2);
            //}

            //Cursor = Cursors.Hand;

            //ExitMarkerRemoveMode();
            //End this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
        }


        private void btnMarkingM_Click(object sender, EventArgs e)
        {

            _isMarkerRemoveMode = true;

            _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
            _liveChart.ChartClicked += Chart_RemoveMarkerOnClick;

            // Nothing to remove
            if (_artifactRanges.Count == 0)
                return;

            //Start this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
            //foreach (var range in _artifactRanges)
            //{
            //    _liveChart.RemoveSamplesBetween(range.R1, range.R2);
            //}
            //Cursor = Cursors.Hand;

            //ExitMarkerRemoveMode();
            //End this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
        }

        private void btnM6_Click(object sender, EventArgs e)
        {

            _isMarkerRemoveMode = true;

            _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
            _liveChart.ChartClicked += Chart_RemoveMarkerOnClick;

            // Nothing to remove
            if (_artifactRanges.Count == 0)
                return;

            //Start this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
            //foreach (var range in _artifactRanges)
            //{
            //    _liveChart.RemoveSamplesBetween(range.R1, range.R2);
            //}
            //Cursor = Cursors.Hand;

            //ExitMarkerRemoveMode();
            //End this code For Delete the plotting for "R1 & R2" middle part change on 31-12-2025
        }

        //private List<Marker> _markers = new List<Marker>();

        //public class Marker
        //{
        //    public double Time { get; set; }
        //    public string Label { get; set; }
        //    public Color Color { get; set; }
        //}




        private void btnS1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S1";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S1", Color.Green);

                MultiClick(sender);
            }
        }

        private void btnS2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S2";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S2", Color.Green);

                MultiClick(sender);
            }
        }

        private void btnGraphS1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {


                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S1";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S1", Color.Green);

                MultiClick(sender);
            }

        }

        private void btnGraphS2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S2";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S2' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S2", Color.Green);

                MultiClick(sender);
            }


        }

        private void btnSelectGraphS1_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S1";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S1", Color.Green);
                MultiClick(sender);
            }
        }

        private void btnS1P6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S1";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S1", Color.Green);
                MultiClick(sender);
            }
        }

        private void btnSelectGraphS2_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S2";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S2", Color.Green);

                MultiClick(sender);
            }
        }

        private void btnS2P6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "S2";
                _pendingMarkerColor = Color.Green;
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'S1' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //_liveChart.ChartClicked += LiveChart_ChartClicked;
                _liveChart.ChartClicked += FS_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "S2", Color.Green);

                MultiClick(sender);
            }
        }

        //private void btnEventLE_Click(object sender, EventArgs e)
        //{
        //    _liveChart.AddMarker(_lastPlotT, "LE", Color.Green);

        //    MultiClick(sender);
        //}



        private void btnEventLE_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "LE";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "LE", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }



        private void btnEventLE3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "LE";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "LE", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }

        private void btnLE6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "LE";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "LE", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }

        private void btnEventCU_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "CU";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'CU' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "CU", Color.FromArgb(ResponseColor));

                MultiClick(sender);
            }

        }

        private void btnEventCU3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "CU";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "CU", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }

        private void btnCU6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "CU";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "CU", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }

        private void btnChannelPves_Click(object sender, EventArgs e)
        {
            // “PVES 0” – only zero PVES lane
            // RAW mapping: 0 = PVES
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[0]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(0, currentRawPves);
        }

        private void btnEventUDC_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {
                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "UDC";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'UDC' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                _liveChart.AddMarker(_lastPlotT, "UDC", Color.FromArgb(ResponseColor));

                MultiClick(sender);
            }

        }

        private void btnEventUDC3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "UDC";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "UDC", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }

        private void btnUDC6_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay Means If Click this button multiples time that time added only one marker 26-02-2026

            ClearChartClickEvents();

            if (_isPlaybackMode)
            {

                _isMarkerPlacementMode = true;
                _pendingMarkerLabel = "UDC";
                _pendingMarkerColor = Color.FromArgb(ResponseColor);
                Cursor = Cursors.Cross;
                //MessageBox.Show("Click on the chart to place the 'LE' marker.","Marker Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _liveChart.ChartClicked += LiveChart_ChartClicked;

            }
            else
            {
                // Normal live mode behavior
                _liveChart.AddMarker(_lastPlotT, "UDC", Color.FromArgb(ResponseColor));
                MultiClick(sender);
            }
        }



        private void btnRF_Click(object sender, EventArgs e)
        {
            //using (var dlg = new OpenFileDialog { Filter = "UroTest (*.utt)|*.utt" })
            //{
            //    if (dlg.ShowDialog(this) != DialogResult.OK) return;

            //    var viewer = new FormTestViewer(dlg.FileName);
            //    viewer.Show();
            //}
            OpenSavedTest();
        }



        private void FreezeTestTime()
        {

            if (_testTimer != null)
            {
                _testTimer.Stop();

            }

            _isRecording = false;

            if (_autoPauseInProgress) return;
            _autoPauseInProgress = true;

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            _testTimer.Stop();

            _orch?.StopProcessing();
            _arm?.StopArm();

            _pumpWasRunningBeforePause = _orch?.Pump?.IsRunning ?? false;
            if (_pumpWasRunningBeforePause)
                PumpStop();




        }

        private void ResumeTestTime()
        {

            if (_testTimer != null)
            {
                _testTimer.Start();
            }

            _isRecording = true;

            _isPaused = false;

            if (_pauseStartedAt.HasValue)
            {
                _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                _pauseStartedAt = null;
            }

            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Black;

            _testTimer.Start();
            UpdateTimerLabel();

            _orch?.StartProcessing();

            if (_pumpWasRunningBeforePause)
                PumpStart();


            _autoPauseInProgress = false;

        }

        private void PauseForConfirmation()
        {
            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            _isRecording = false;

            _testTimer?.Stop();

            _orch?.StopProcessing();
            _arm?.StopArm();

            _pumpWasRunningBeforePause = _orch?.Pump?.IsRunning ?? false;

            if (_pumpWasRunningBeforePause)
                PumpStop();

            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;
        }

        private void ResumeAfterConfirmation()
        {
            _isPaused = false;
            _isRecording = true;

            if (_pauseStartedAt.HasValue)
            {
                _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                _pauseStartedAt = null;
            }

            _testTimer?.Start();

            _orch?.StartProcessing();

            if (_pumpWasRunningBeforePause)
                PumpStart();

            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Black;
        }

        // 🔴 Start Code For Live Test is Saved and Stop Camera & Get Saved Image & show Right side Camera design Change that time ADD on 23-02-2026
        //This Code Update Right Side Camera design After Saved Test
        private void UpdateToReviewModeSidebar()
        {
            try
            {
                // Stop the camera first
                StopCamera();

                // CHECK IF mainContentPanel IS NULL FIRST
                if (mainContentPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("Error: mainContentPanel is null");
                    // Option 1: Return silently
                    return;

                    // Option 2: Initialize it if possible
                    // mainContentPanel = new Panel(); // But this might not be the right approach
                }

                // Find the TableLayoutPanel that contains the graph and sidebar
                TableLayoutPanel contentSplit = null;
                foreach (Control c in mainContentPanel.Controls)
                {
                    if (c is TableLayoutPanel tlp && tlp.ColumnCount == 2)
                    {
                        contentSplit = tlp;
                        break;
                    }
                }

                if (contentSplit == null) return;

                // Remove the old sidebar if it exists
                if (contentSplit.Controls.Count > 1)
                {
                    Control oldSidebar = contentSplit.Controls[1];
                    contentSplit.Controls.Remove(oldSidebar);
                    oldSidebar.Dispose();
                }

                // Create and add the new review mode sidebar
                CreateRightStopCamera();

                // ADD NULL CHECK HERE
                if (rightSidebarPanel == null)
                {
                    // Option 1: Create a default panel if null
                    rightSidebarPanel = new Panel();
                    // Or throw an exception with more context
                    throw new InvalidOperationException("CreateRightStopCamera() failed to initialize rightSidebarPanel");
                }

                if (rightSidebarPanel != null)
                {
                    rightSidebarPanel.Dock = DockStyle.Fill;
                    rightSidebarPanel.MinimumSize = new Size(445, 0);
                    contentSplit.Controls.Add(rightSidebarPanel, 1, 0);
                }

                // Force the layout to update
                contentSplit.Invalidate();
                contentSplit.Update();
            }
            catch (Exception ex)
            {
                // Log the full exception details
                System.Diagnostics.Debug.WriteLine($"Error in UpdateToReviewModeSidebar: {ex}");
                throw; // Rethrow if you want to handle it at a higher level
            }

        }

        // 🔴 END Code For Live Test Saved and Stop Camera & Get Saved Image & show Right side Camera design Change that time ADD on 23-02-2026
        private void FinalStopAndSave()
        {
            //  FreezeTestTime();

            try
            {
                // Check if we have data to save
                if (_recorded == null || _recorded.Count == 0)
                {
                    MessageBox.Show(
                        "No test data recorded. Cannot save test.",
                        "No Data",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Save the test
                string savedFile = StopLiveTest(savePrompt: true);

                _lblTimer.Visible = false;
                ButtonsShow();
                ShowPrintButtons();
                btnTestStop1.Enabled = false;

                _isPlaybackMode = true;

                // 🔴 ONLY UPDATE THE SIDEBAR - GRAPH STAYS INTACT
                UpdateToReviewModeSidebar();

                // Verify file was saved
                if (!string.IsNullOrEmpty(savedFile) && File.Exists(savedFile))
                {
                    // Save camera images
                    try
                    {
                        SaveCapturedImages(savedFile);
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Image save failed: {imgEx.Message}");
                        // Continue even if image save fails
                    }

                    // Load for playback - this sets _isUpdateMode = true and _currentSavedTestPath
                    LoadUttForPlayback(savedFile, V);

                    LoadCapturedImagesForTest(savedFile);

                    // Verify the state was set correctly
                    System.Diagnostics.Debug.WriteLine($"✅ Test saved successfully:");
                    System.Diagnostics.Debug.WriteLine($"   File: {savedFile}");
                    System.Diagnostics.Debug.WriteLine($"   Test Type: {currenttest}");
                    System.Diagnostics.Debug.WriteLine($"   _isUpdateMode: {_isUpdateMode}");
                    System.Diagnostics.Debug.WriteLine($"   _currentSavedTestPath: {_currentSavedTestPath}");
                    System.Diagnostics.Debug.WriteLine($"   Samples recorded: {_recorded?.Count ?? 0}");
                }
                else
                {
                    MessageBox.Show(
                        $"Test save failed.\n\nSaved file: {savedFile ?? "NULL"}\n" +
                        $"Test type: {currenttest}\n" +
                        $"Samples recorded: {_recorded?.Count ?? 0}",
                        "Save Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving test:\n\n{ex.Message}\n\nTest Type: {currenttest}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                System.Diagnostics.Debug.WriteLine($"❌ Save error: {ex}");
            }
        }


        private void TestStop()
        {
            _monitorNoFlow = false;
            _noFlowTimer.Stop();
            PauseForConfirmation();

            DialogResult result = MessageBox.Show(
                "Are you sure you want to stop the test? If you stop, the test will be saved automatically.",
                "Confirm Stop",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _isLiveTestRunning = false;
                // Permanent Stop - this handles save and load
                FinalStopAndSave();
                PumpStop();

                ShowAllMenuStripItems();
            }
            else
            {
                // Resume test
                ResumeAfterConfirmation();
            }
        }
        //Test Pause Buttons
        private void btnTestStop1_Click(object sender, EventArgs e)
        {
            TestStop();
        }

        private void btnTestStop2_Click(object sender, EventArgs e)
        {
            TestStop();
        }

        private void btnTestStop3_Click(object sender, EventArgs e)
        {
            TestStop();
        }

        private void btnTestStop4_Click(object sender, EventArgs e)
        {
            TestStop();
        }

        private void btnTestStop5_Click(object sender, EventArgs e)
        {
            TestStop();
        }


        //Save Test Button Marun Color
        private void btnTestSave1_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                //Important This Line For Delete the R1 & R2 Under Plotting commint on 01-01-2026
                //ApplyArtifactDeletionToRecordedData();

                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                // ✅ NOW it is safe to clear
                ClearArtifactSelections();

                MessageBox.Show("Test updated successfully.",
                                "Update Test",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                ShowPrintButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }



        public void LoadFromFile(string filePath)
        {
            string section = "";
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("["))
                {
                    section = line.Trim('[', ']');
                    continue;
                }

                if (section == "Data")
                {
                    // Parse your recorded data here...
                }
                else if (section == "Markers")
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 5)
                    {
                        double tSec = double.Parse(parts[0]);
                        string label = parts[1];
                        Color col = Color.FromArgb(int.Parse(parts[2]));
                        float width = float.Parse(parts[3]);
                        var dash = (System.Drawing.Drawing2D.DashStyle)int.Parse(parts[4]);

                        _liveChart.AddMarker(tSec, label, col, width, dash);
                    }
                }
            }
        }

        // Holds currently saved test file path (for update)
        private string _currentSavedTestPath;


        //private void btnTestSave2_Click(object sender, EventArgs e)
        //{
        //    // Show confirmation message before Save the test
        //    DialogResult result = MessageBox.Show(
        //        "Test has been saved successfully!",
        //        "Save Successful",
        //        MessageBoxButtons.OK,
        //        MessageBoxIcon.Information
        //    );



        //    //StopLiveTest(savePrompt: true);
        //    //_lblTimer.Visible = false; 
        //    //ButtonsShow();

        //    //btnTestPause2.Enabled = false;
        //    //btnTestStop2.Enabled = false;

        //    //_isPlaybackMode = true;
        //}

        private void btnTestSave2_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                //Important This Line For Delete the R1 & R2 Under Plotting commint on 01-01-2026
                //ApplyArtifactDeletionToRecordedData();

                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                // ✅ NOW it is safe to clear
                ClearArtifactSelections();

                MessageBox.Show("Test updated successfully.",
                                "Update Test",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                ShowPrintButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }


        private void btnTestSave3_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                //Important This Line For Delete the R1 & R2 Under Plotting commint on 01-01-2026
                //ApplyArtifactDeletionToRecordedData();

                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                // ✅ NOW it is safe to clear
                ClearArtifactSelections();

                MessageBox.Show("Test updated successfully.",
                                "Update Test",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                ShowPrintButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }

        private void btnTestSave4_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                //Important This Line For Delete the R1 & R2 Under Plotting commint on 01-01-2026
                //ApplyArtifactDeletionToRecordedData();

                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                ClearArtifactSelections();

                MessageBox.Show("Test updated successfully.",
                                "Update Test",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                ShowPrintButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }

        private void btnTestSave5_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                // Important This Line For Delete the R1 & R2 Under Plotting commint on 01-01-2026
                //ApplyArtifactDeletionToRecordedData();

                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                ClearArtifactSelections();

                MessageBox.Show("Test updated successfully.",
                                "Update Test",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                ShowPrintButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }


        // 1) A spring label that expands to fill remaining space (pushes right-aligned items)
        sealed class ToolStripSpringLabel : ToolStripLabel
        {
            public override Size GetPreferredSize(Size constrainingSize)
            {
                if (IsOnOverflow || Owner == null) return base.GetPreferredSize(constrainingSize);

                int width = Owner.DisplayRectangle.Width;
                foreach (ToolStripItem item in Owner.Items)
                {
                    if (item == this || item.IsOnOverflow) continue;
                    width -= item.Margin.Horizontal + item.Width;
                }
                if (width < 0) width = 0;
                var h = base.GetPreferredSize(constrainingSize).Height;
                return new Size(width, h);
            }
        }

        // 2) DPI helper
        static float GetDpiScale(Control c)
        {
            using (var g = c.CreateGraphics())
            {
                return g.DpiX / 96f;
            }
        }

        private void btnEventRF_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        //private void DeviceStatusPill_Paint(object sender, PaintEventArgs e)
        //{
        //    var pill = sender as ToolStripLabel;
        //    if (pill == null) return;

        //    Rectangle rect = new Rectangle(0, 0, pill.Width, pill.Height);

        //    // rounded white background
        //    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        //    using (var path = RoundedRect(rect, 8))
        //    {
        //        using (var bg = new SolidBrush(Color.White)) e.Graphics.FillPath(bg, path);
        //        using (var pen = new Pen(Color.LightGray)) e.Graphics.DrawPath(pen, path);
        //    }

        //    // choose color/text by state
        //    Color dotColor; string text;
        //    switch (_deviceState)
        //    {
        //        case DeviceState.Connected: dotColor = Color.Green; text = "Device Connected"; break;

        //        default: dotColor = Color.Red; text = "Device Disconnected"; break;
        //    }

        //    // draw dot + text
        //    string dot = "●";
        //    SizeF dotSize = e.Graphics.MeasureString(dot, pill.Font);
        //    float y = (rect.Height - dotSize.Height) / 2f;

        //    using (var b = new SolidBrush(dotColor))
        //        e.Graphics.DrawString(dot, pill.Font, b, new PointF(10, y));

        //    using (var b2 = new SolidBrush(Color.Black))
        //        e.Graphics.DrawString(text, pill.Font, b2, new PointF(10 + dotSize.Width + 4, y));
        //}

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Sync again (just in case device state changed during startup)
            _lastPopupState = _deviceState;

            // Allow popups from now on
            _suppressPopupOnStartup = false;
        }

        // ✅ This method will be called when the device connects AFTER test is started
        private void OnDeviceConnectedStartLive()
        {
            try
            {
                // ✅ Only start live if test is already running
                if (!_acceptLiveFrames)
                    return;

                // ✅ Start device data stream
                if (_orch != null)
                {
                    _orch.Start();
                }

                // ✅ Restart chart safely
                if (_liveChart != null)
                {
                    _liveChart.EnableHover = false;
                    _liveChart.ToggleLive(true);
                    //_liveChart.Start();
                    _liveChart.ScrollToLive();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Live reconnect error: " + ex.Message);
            }
        }


        private DeviceState _lastPopupState;
        private bool _suppressPopupOnStartup = true;


        //private DeviceState _lastPopupState = DeviceState.Disconnected;

        // Device Status

        //Current working code
        private void DeviceStatusPill_Paint(object sender, PaintEventArgs e)
        {
            var pill = sender as ToolStripLabel;
            if (pill == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // choose color/text by state
            Color dotColor, backColor;
            string text;
            switch (_deviceState)
            {
                case DeviceState.Connected:
                    dotColor = Color.Green;
                    backColor = Color.LightGreen;
                    text = "Device Connected";
                    break;

                case DeviceState.Disconnected:
                default:
                    dotColor = Color.Red;
                    backColor = Color.MistyRose;
                    text = "Device Disconnected";
                    break;
            }

            // --- ensure we don't show popup during startup ---
            if (_suppressPopupOnStartup)
            {
                // Keep _lastPopupState in sync so first paint doesn't trigger a change
                _lastPopupState = _deviceState;
            }
            else
            {
                // Show popup only when real change happens (and not suppressed)
                if (_deviceState != _lastPopupState)
                {
                    if (_deviceState == DeviceState.Connected)
                    {
                        AllButtonEnable();
                        panel7.Visible = false;
                        label37.Visible = false;
                        //btnTestStart2.Enabled = true;
                        label36.Visible = true;
                        // safe null-check if needed



                        MessageBox.Show(
                            "Device Connected Successfully",
                            "Connected",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        _deviceState = DeviceState.Connected;
                        OnDeviceConnectedStartLive();

                        //_liveChart.Start();
                        if (_liveChart != null)
                        {
                            _liveChart.Start();
                        }

                        if (_testTimer != null)
                        {
                            _testTimer.Start();
                        }

                        if (_orch != null)
                            _orch.StartProcessing();

                        // Start pump ONLY if it was running before disconnect
                        if (_pumpWasRunningBeforePause)
                        {
                            PumpStart();
                            _pumpWasRunningBeforePause = false;
                        }

                    }
                    else // DeviceState.Disconnected
                    {
                        // Save pump state BEFORE stopping everything
                        _pumpWasRunningBeforePause = _isPumpRunning;

                        // Stop pump if it's running
                        if (_isPumpRunning)
                        {
                            PumpStop();
                        }

                        _liveChart.Stop();

                        if (_orch != null)
                            _orch.StopProcessing();
                        _testTimer.Stop();

                        //Code For Device is disconnected so reset the value for Zeror Button
                        try { if (_orch != null) _orch.Stop(); } catch { }
                        //DetachOrch();

                        // Start this code for DialogBox Still Show Only Live Test other Time Close the DialogBox add on 19-02-2025
                        if (_isLiveTestRunning)
                        {
                            // Flag is TRUE - keep showing message box
                            while (_deviceState == DeviceState.Disconnected && _isLiveTestRunning)
                            {
                                MessageBox.Show(
                                    "Device Disconnected — Please connect device",
                                    "Disconnected",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );

                                Application.DoEvents(); // allow device thread to update state
                            }
                        }
                        else
                        {
                            // Flag is FALSE - show once and close
                            MessageBox.Show(
                                "Device Disconnected — Please connect device",
                                "Disconnected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                        }
                        // Start this code for DialogBox Still Show Only Live Test other Time Close the DialogBox add on 19-02-2025

                    }

                    _lastPopupState = _deviceState;
                }
            }

            // measure dot + text
            string dot = "●";
            SizeF dotSize = e.Graphics.MeasureString(dot, pill.Font);
            SizeF textSize = e.Graphics.MeasureString(text, pill.Font);

            int totalWidth = (int)(10 + dotSize.Width + 4 + textSize.Width + 10);
            int totalHeight = (int)Math.Max(dotSize.Height, textSize.Height) + 6;

            pill.AutoSize = false;             // let us control width
            pill.Width = totalWidth;
            pill.Height = totalHeight;

            Rectangle rect = new Rectangle(0, 0, pill.Width, pill.Height);

            // rounded background (status-based color)
            using (var path = RoundedRect(rect, 8))
            {
                using (var bg = new SolidBrush(backColor))
                    e.Graphics.FillPath(bg, path);

                using (var pen = new Pen(Color.LightGray))
                    e.Graphics.DrawPath(pen, path);
            }

            float y = (rect.Height - dotSize.Height) / 2f;

            // draw dot
            using (var b = new SolidBrush(dotColor))
                e.Graphics.DrawString(dot, pill.Font, b, new PointF(10, y));

            // draw text
            using (var b2 = new SolidBrush(Color.Black))
                e.Graphics.DrawString(text, pill.Font, b2, new PointF(10 + dotSize.Width + 4, y));
        }



        // helper for rounded rectangles
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var gp = new GraphicsPath();
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }






        //private bool ProbeDaqOnce()
        //{
        //    DaqService daq = null;
        //    try
        //    {
        //        daq = new DaqService();
        //        daq.Start(AI_CONFIG);   // or "Dev1/ai0:6" if your HW requires it
        //        try { daq.Stop(); } catch { }
        //        try { daq.Dispose(); } catch { }
        //        return true;
        //    }
        //    catch
        //    {
        //        try { if (daq != null) daq.Dispose(); } catch { }
        //        return false;
        //    }
        //}
        private bool ProbeDaqOnce()
        {
            try
            {
                // Fast path: make sure the specific device from AI_CONFIG exists
                var deviceName = AI_CONFIG.Split('/')[0]; // "Dev1"
                var dev = NationalInstruments.DAQmx.DaqSystem.Local.LoadDevice(deviceName);
                if (dev == null) return false;

                // Touch the hardware via a tiny task so Windows "random USB" doesn't fool us
                using (var t = new NationalInstruments.DAQmx.Task())
                {
                    // A single lightweight analog channel is enough to verify presence
                    t.AIChannels.CreateVoltageChannel(
                        deviceName + "/ai0", "",
                        AITerminalConfiguration.Rse, 0, 10, AIVoltageUnits.Volts);

                    t.Control(TaskAction.Verify); // quick—doesn’t stream
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        private void Watcher_StateChanged(object sender, EventArgs e)
        {
            _deviceState = _watcher.IsConnected ? DeviceState.Connected : DeviceState.Disconnected;
            if (_deviceStatusPill != null) _deviceStatusPill.Invalidate();
        }

        private async void Watcher_Disconnected(object sender, EventArgs e)
        {
            // Debounce: Wait a short grace period and check again.
            await System.Threading.Tasks.Task.Delay(600);  // tolerate brief churn on other USB unplug

            // If hardware came back during the grace period, do nothing.
            if (_watcher.IsConnected) return;

            //_testTimer.Stop();
            //PumpStop();

            _isPaused = true;
            _pauseStartedAt = DateTime.Now;

            _testTimer.Stop();

            if (_orch != null)
                _orch.StopProcessing();

            _arm?.StopArm();

            UpdateTimerLabel();

            _pumpWasRunningBeforePause = _orch?.Pump?.IsRunning ?? false;

            if (_pumpWasRunningBeforePause)
                PumpStop();

            // Change timer color to indicate pause
            if (_lblTimer != null)
                _lblTimer.ForeColor = Color.Red;



            // Also require that we haven’t seen frames recently (heartbeat)
            var noFramesFor = DateTime.UtcNow - _lastFrameAt;
            if (noFramesFor < TimeSpan.FromMilliseconds(1200)) return;

            _wasAcquiringBeforeDisconnect = (_orch != null);

            // Remember timeline position so we continue after it on reconnect
            _resumeArmed = true;

            // Don’t clear the chart — just stop the DAQ side cleanly
            StopAcquisitionSafe();
        }

        private void Watcher_Connected(object sender, EventArgs e)
        {
            if (_wasAcquiringBeforeDisconnect)
                BeginInvoke((MethodInvoker)StartAcquisitionSafe); // keeps chart history
        }
        private bool ChartReady()
        {
            return _liveChart != null && !_liveChart.IsDisposed && _liveChart.IsHandleCreated;
        }

        private void StopAcquisitionSafe()
        {
            try { if (ChartReady()) _liveChart.Stop(); } catch { }
            try
            {
                if (_orch != null)
                {
                    // try { _orch.OnDisplayFrame -= OnDisplayFrame; } catch { }
                    try { _orch.Stop(); } catch { }
                    //  try { _orch.Dispose(); } catch { }
                    //   _orch = null;
                }
            }
            catch { }
        }

        private void StartAcquisitionSafe()
        {
            if (!ChartReady()) return;
            try
            {
                // LoadGraph(); // builds DAQ, wires OnDisplayFrame, sets lanes, starts chart

                //_testTimer.Start();
                //PumpStart();

                // _isPaused = false;
                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }

                // Restore color and resume timer
                if (_lblTimer != null)
                    _lblTimer.ForeColor = Color.Black;

                _testTimer.Start();
                UpdateTimerLabel();

                if (_orch != null)
                    _orch.StartProcessing(); _isPaused = false;

                _liveChart.Start();

                if (_pauseStartedAt.HasValue)
                {
                    _pausedTime += (DateTime.Now - _pauseStartedAt.Value);
                    _pauseStartedAt = null;
                }

                // Restore color and resume timer
                if (_lblTimer != null)
                    _lblTimer.ForeColor = Color.Black;

                _testTimer.Start();
                UpdateTimerLabel();

                //if (_orch != null)
                //    _orch.StartProcessing();

                if (_pumpWasRunningBeforePause)
                    PumpStart();

                try { _liveChart.ScrollToLive(); } catch { }
            }
            catch { }
        }


        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_watcher == null)
            {
                _watcher = new DeviceWatcher();
                _watcher.SetProbe(new DeviceWatcher.DeviceProbe(ProbeDaqOnce)); // your DAQ/COM quick check
                _watcher.StateChanged += new EventHandler(Watcher_StateChanged);
                _watcher.Connected += new EventHandler(Watcher_Connected);
                _watcher.Disconnected += new EventHandler(Watcher_Disconnected);
                _watcher.Start(this); // IMPORTANT: pass the form handle
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { if (_watcher != null) _watcher.Stop(); } catch { }
            base.OnHandleDestroyed(e);
        }

        // forward WndProc messages to watcher
        protected override void WndProc(ref Message m)
        {
            if (_watcher != null) _watcher.OnWndProc(ref m);
            base.WndProc(ref m);
        }





        //This Code For Buttons Use For Saved Test View Edit 


        //Start Code For Add Mark Selected Location Work Only Saved Test on 15/10/2025
        private bool _isMarkerPlacementMode = false;
        private string _pendingMarkerLabel = "";
        private Color _pendingMarkerColor = Color.Black;
        private void LiveChart_ChartClicked(double tSec)
        {
            if (_isMarkerPlacementMode)
            {
                _liveChart.AddMarker(tSec, _pendingMarkerLabel, _pendingMarkerColor);
                _isMarkerPlacementMode = false;
                _pendingMarkerLabel = "";
                Cursor = Cursors.Default;
                //MessageBox.Show("Marker added at selected location.","Marker Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnCross_Click(object sender, EventArgs e)
        {
            btnShowPatients_Click(sender, e);
        }

        private void btnCross1_Click(object sender, EventArgs e)
        {
            btnShowPatients_Click(sender, e);
        }

        private void btnCross2_Click(object sender, EventArgs e)
        {
            btnShowPatients_Click(sender, e);
        }

        private void btnCross3_Click(object sender, EventArgs e)
        {
            btnShowPatients_Click(sender, e);
        }

        private void btnCross4_Click(object sender, EventArgs e)
        {
            btnShowPatients_Click(sender, e);
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btn0_Click(object sender, EventArgs e)
        {
            // Global “Channel Zero” – mimic legacy ReadOffset0 (all channels)
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            // If you don’t want this to work in playback, guard:
            if (_isPlaybackMode)
                return;

            // Zero all available RAW channels based on the last sample
            int n = Math.Min(2, _lastRawCounts.Length); // 0..6 are valid raw lanes
            for (int rawIndex = 0; rawIndex < n; rawIndex++)
            {
                _orch.Proc.ZeroChannel(rawIndex, _lastRawCounts[rawIndex]);
            }
        }

        private void btnChannel0_Click(object sender, EventArgs e)
        {
            // Global “Channel Zero” – mimic legacy ReadOffset0 (all channels)
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            // If you don’t want this to work in playback, guard:
            if (_isPlaybackMode)
                return;

            // Zero all available RAW channels based on the last sample
            int n = Math.Min(2, _lastRawCounts.Length); // 0..6 are valid raw lanes
            for (int rawIndex = 0; rawIndex < n; rawIndex++)
            {
                _orch.Proc.ZeroChannel(rawIndex, _lastRawCounts[rawIndex]);
            }
        }

        private void btn0P6_Click(object sender, EventArgs e)
        {
            // Global “Channel Zero” – mimic legacy ReadOffset0 (all channels)
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            // If you don’t want this to work in playback, guard:
            if (_isPlaybackMode)
                return;

            // Zero all available RAW channels based on the last sample
            int n = Math.Min(2, _lastRawCounts.Length); // 0..6 are valid raw lanes
            for (int rawIndex = 0; rawIndex < n; rawIndex++)
            {
                _orch.Proc.ZeroChannel(rawIndex, _lastRawCounts[rawIndex]);
            }
        }

        private void btnPabd_Click(object sender, EventArgs e)
        {
            // “PABD 0” – only zero PABD lane
            // RAW mapping: 0 = PABD
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[1]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(1, currentRawPves);
        }

        private void btnPves6_Click(object sender, EventArgs e)
        {
            // “PVES 0” – only zero PVES lane
            // RAW mapping: 0 = PVES
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[0]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(0, currentRawPves);
        }

        private void btnPabd6_Click(object sender, EventArgs e)
        {
            // “PABD 0” – only zero PABD lane
            // RAW mapping: 0 = PABD
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[1]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(1, currentRawPves);
        }

        private void btnChannelPirp_Click(object sender, EventArgs e)
        {
            // “PURA 0” – only zero PURA lane
            // RAW mapping: 0 = PURA
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[6]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(6, currentRawPves);


            //// “PABD 0” – only zero PABD lane
            //// RAW mapping: 0 = PABD
            //if (_orch == null || _orch.Proc == null)
            //    return;

            //if (_lastRawCounts == null || _lastRawCounts.Length == 0)
            //    return;

            //if (_isPlaybackMode)
            //    return;

            //double currentRawPves = _lastRawCounts[1]; // RAW index 0 = PVES
            //_orch.Proc.ZeroChannel(1, currentRawPves);
        }

        private void btnPura_Click(object sender, EventArgs e)
        {
            // “PURA 0” – only zero PURA lane
            // RAW mapping: 0 = PURA
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[6]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(6, currentRawPves);
        }

        private void btnPura6_Click(object sender, EventArgs e)
        {
            // “PURA 0” – only zero PURA lane
            // RAW mapping: 0 = PURA
            if (_orch == null || _orch.Proc == null)
                return;

            if (_lastRawCounts == null || _lastRawCounts.Length == 0)
                return;

            if (_isPlaybackMode)
                return;

            double currentRawPves = _lastRawCounts[6]; // RAW index 0 = PVES
            _orch.Proc.ZeroChannel(6, currentRawPves);
        }

        private void label37_Click(object sender, EventArgs e)
        {

        }
































        //Start This Code For Count Line add Means GP Mark Line
        private bool _isMarkerPlacementModeForCount = false;



        private string _pendingMarkerLabelForCount = "";



        private Color _pendingMarkerColorForCount = Color.Black;

        private void LiveChart_ChartClickedForCount(double tSec)
        {
            if (_isMarkerPlacementModeForCount)
            {
                _liveChart.AddMarker(tSec, _pendingMarkerLabelForCount, _pendingMarkerColorForCount);
                _isMarkerPlacementModeForCount = false;
                _pendingMarkerLabelForCount = "";
                Cursor = Cursors.Default;
                //MessageBox.Show("Marker added at selected location.","Marker Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        //End This Code For Count Line add Means GP Mark Line



        //Start This Code For FS,FD,ND,SD,BC For Add Mark One time if Add So existing mark remove
        private void FS_ChartClicked(double tSec)
        {
            if (_isMarkerPlacementMode)
            {
                // Remove existing FS marker first
                _liveChart.RemoveMarkerByLabel(_pendingMarkerLabel);

                // Add FS marker at clicked time
                _liveChart.AddMarker(tSec, _pendingMarkerLabel, _pendingMarkerColor);

                _isMarkerPlacementMode = false;
                _pendingMarkerLabel = "";
                Cursor = Cursors.Default;

                //MessageBox.Show("FS marker placed at selected location.","Marker Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        //End This Code For FS,FD,ND,SD,BC For Add Mark One time if Add So existing mark remove


        //Start This Code For R1, R2 and M buttom code for remove the selected area ploting 
        //private void Artifact_ChartClicked(double tSec)
        //{
        //    if (!_isMarkerPlacementMode) return;

        //    _liveChart.RemoveMarkerByLabel(_pendingMarkerLabel);

        //    _liveChart.AddMarker(tSec, _pendingMarkerLabel, _pendingMarkerColor);

        //    if (_pendingMarkerLabel == "R1") _r1 = tSec;
        //    else if (_pendingMarkerLabel == "R2") _r2 = tSec;

        //    _isMarkerPlacementMode = false;
        //    _pendingMarkerLabel = "";
        //    Cursor = Cursors.Default;

        //    _liveChart.ChartClicked -= Artifact_ChartClicked;

        //    //MessageBox.Show("Marker added at selected location.","Marker Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //}


        //Start R1 & R2 add Multiple and and delete Plotting code
        private class ArtifactRange
        {
            public double R1;
            public double R2;
        }

        private readonly List<ArtifactRange> _artifactRanges =
            new List<ArtifactRange>();


        private void Artifact_ChartClicked(double tSec)
        {
            if (!_isMarkerPlacementMode)
                return;

            // Do NOT remove previous markers — allow multiple R1/R2
            _liveChart.AddMarker(tSec, _pendingMarkerLabel, _pendingMarkerColor);

            if (_pendingMarkerLabel == "R1")
            {
                // store R1 temporarily
                _r1 = tSec;
            }
            else if (_pendingMarkerLabel == "R2")
            {
                if (_r1.HasValue)
                {
                    double r1 = _r1.Value;
                    double r2 = tSec;

                    _artifactRanges.Add(new ArtifactRange
                    {
                        R1 = Math.Min(r1, r2),
                        R2 = Math.Max(r1, r2)
                    });


                    _r1 = null; // ready for next pair
                }
            }

            _isMarkerPlacementMode = false;
            _pendingMarkerLabel = string.Empty;
            Cursor = Cursors.Default;

            _liveChart.ChartClicked -= Artifact_ChartClicked;
        }


        private void ClearArtifactSelections()
        {
            _artifactRanges.Clear();
            _r1 = null;
            _r2 = null;
            _pendingMarkerLabel = string.Empty;
            _isMarkerPlacementMode = false;
            _isMarkerRemoveMode = false;
            Cursor = Cursors.Default;
        }

        private void ExitMarkerRemoveMode()
        {
            _isMarkerRemoveMode = false;
            _liveChart.ChartClicked -= Chart_RemoveMarkerOnClick;
            Cursor = Cursors.Default;
        }
        //End R1 & R2 add Multiple and and delete Plotting code

        List<SampleRecord> _recordedSamples;

        //Start this code for delete plotting using R1 & R2 for update and save
        private void ApplyArtifactDeletionToRecordedData()
        {
            if (_artifactRanges == null || _artifactRanges.Count == 0)
                return;

            if (_recorded == null || _recorded.Count == 0)
                return;

            foreach (var range in _artifactRanges)
            {
                _recorded.RemoveAll(s =>
                    s != null &&
                    s.T >= range.R1 &&
                    s.T <= range.R2);
            }
        }
        //Start this code for delete plotting using R1 & R2 for update and save







        //Start This code work only Live Test old code
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

        //    _lblInfusion.Text = $"Rate: 0 ml/min";
        //}

        //End This code work only Live Test





        //Start This code work for both Home screen & Live Test and Add "MainForm"  this two line _pump = new PumpController(1000, 1000); _infusionRate = 0;
        private void PumpStart()
        {
            _infusionRate = ClampInfusionRate(_infusionRate);
            _pump.StartInfusion(_infusionRate);
            UpdateInfusionLabel();

            _isPumpRunning = true;
        }

        private void PumpSpeedUp()
        {
            _infusionRate = ClampInfusionRate(_infusionRate + 1);
            _pump.SetRate(_infusionRate);
            UpdateInfusionLabel();
        }

        private void PumpSpeedDown()
        {
            _infusionRate = ClampInfusionRate(_infusionRate - 1);
            _pump.SetRate(_infusionRate);
            UpdateInfusionLabel();
        }

        private void PumpStop()
        {
            //_infusionRate = 0;
            _pump.StopInfusion();

            if (_lblInfusion != null)
                _lblInfusion.Text = "Rate: 0 ml/min";

            _isPumpRunning = false;
        }
        //End This code work for both Home screen & Live Test

        //Start Pumps Buttons for Live Test
        private void btnPumpStart_Click(object sender, EventArgs e)
        {
            PumpStart();
        }

        private void btnPumpSpUp_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay 
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay 

            PumpSpeedUp();
        }

        private void btnPumpSpDown_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay

            PumpSpeedDown();
        }

        private void btnPumpStop_Click(object sender, EventArgs e)
        {
            PumpStop();
        }

        private void btnPumpStart3_Click(object sender, EventArgs e)
        {
            PumpStart();
        }

        private void btnPumpSpUp3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay 
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay 

            PumpSpeedUp();
        }

        private void btnPumpSpDown3_Click(object sender, EventArgs e)
        {
            //Start Code For Add Delay 
            if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
            {
                return;
            }
            _lastClickTime = DateTime.Now;
            //END Code For Add Delay 

            PumpSpeedDown();
        }

        private void btnPumpStop3_Click(object sender, EventArgs e)
        {
            PumpStop();
        }
        //END Pump Buttons for Live Test

        //--------------------------------------------//

        //START Pump Buttons for Home Screen
        private void btnPStart_Click(object sender, EventArgs e)
        {
            PumpStart();
            //if (_orch?.Pump == null) return;
            //_infusionRate = ClampInfusionRate(_infusionRate);
            //_orch.Pump.StartInfusion((int)_infusionRate);

            //UpdateInfusionLabel();

        }

        private void btnSpeedUp_Click(object sender, EventArgs e)
        {
            //_infusionRate = ClampInfusionRate(_infusionRate + 1); //if change 0.25 use double for next line not use int
            //_pump.SetRate((int)_infusionRate);
            //UpdateInfusionLabel();
            PumpSpeedUp();
        }

        private void btnSpeedDown_Click(object sender, EventArgs e)
        {
            //_infusionRate = ClampInfusionRate(_infusionRate - 1); //if change 0.25 use double for next line not use int
            //_pump.SetRate((int)_infusionRate);
            //UpdateInfusionLabel();
            PumpSpeedDown();
        }

        private void btnPStop_Click(object sender, EventArgs e)
        {
            //_pump.StopInfusion();
            //if (_lblInfusion != null)
            //    _lblInfusion.Text = "Rate: 0 ml/min";
            //_isPumpRunning = false;
            PumpStop();
        }

        private void btnArmStart_Click(object sender, EventArgs e)
        {
            _arm.StartArm();
        }

        private void btnArmStop_Click(object sender, EventArgs e)
        {
            _arm.StopArm();
        }

        private void btnDIR_Click(object sender, EventArgs e)
        {
            _arm.ToggleDirection();
        }

        private void btnArmSpeed_Click(object sender, EventArgs e)
        {
            _arm.ToggleSpeed();
        }
        //END Pump Buttons for Home Screen



        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Optional: reset the arm outputs and clean up
            try
            {
                _arm?.StopArm();
                _arm?.Dispose();
            }
            catch { }
            base.OnFormClosed(e);
        }

        // === Map to your existing buttons ===


        private void btnUppStart_Click(object sender, EventArgs e)
        {
            _arm.StartArm();
        }

        private void btnUppStop_Click(object sender, EventArgs e)
        {
            _arm.StopArm();
        }

        //Comment on 13-02-2026 this code not have delay
        //private void btnUppDIR_Click(object sender, EventArgs e)
        //{
        //    _arm.ToggleDirection();

        //}

        private async void btnUppDIR_Click(object sender, EventArgs e)
        {
            //_arm.ToggleDirection();

            btnUppDIR.Enabled = false;   // prevent double click

            _arm.ToggleDirection();      // send command first

            await System.Threading.Tasks.Task.Delay(100);       // delay AFTER sending command

            btnUppDIR.Enabled = true;
        }

        //Comment on 13-02-2026 this code not have delay
        //private void btnUppSpeed_Click(object sender, EventArgs e)
        //{
        //    _arm.ToggleSpeed();
        //}

        //This code added Delay for button click add on 13-02-2026
        private async void btnUppSpeed_Click(object sender, EventArgs e)
        {
            //_arm.ToggleSpeed();

            btnUppSpeed.Enabled = false;   // prevent double click

            _arm.ToggleSpeed();      // send command first

            await System.Threading.Tasks.Task.Delay(100);       // delay AFTER sending command

            btnUppSpeed.Enabled = true;
        }

        private void mainContainerPanel_Paint(object sender, PaintEventArgs e)
        {

        }


        //End This Code For R1, R2 and M buttom code for remove the selected area ploting 

        //End Code For Add Mark Selected Location Work Only Saved Test on 15/10/2025

        private void label36_Click(object sender, EventArgs e)
        {

        }

        private TestMode _activemode;





        private HospitalAndDocterModel LoadHospitalAndDoctorSettings()
        {
            try
            {
                //string folder = Path.Combine(Application.StartupPath, "Saved Data", "HospitalAndDocter");
                string folder = AppPathManager.GetFolderPath("HospitalAndDocter");

                if (!Directory.Exists(folder))
                    return null;

                string[] files = Directory.GetFiles(folder, "*.dat");

                if (files.Length == 0)
                    return null;

                string filePath = files[0];

                byte[] encrypted = File.ReadAllBytes(filePath);
                string json = CryptoHelper.Decrypt(encrypted);

                return JsonSerializer.Deserialize<HospitalAndDocterModel>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load error: " + ex.Message);
                return null;
            }
        }



        private PatientRecord GetPatinetData()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedPatientNo) ||
                    string.IsNullOrEmpty(_selectedPatientId))
                {
                    return null;
                }

                // string folder = Path.Combine(Application.StartupPath,"Saved Data", "PatientsData");
                string folder = AppPathManager.GetFolderPath("Samples");


                if (!Directory.Exists(folder))
                    return null;

                foreach (var file in Directory.GetFiles(folder, "*.dat"))
                {
                    try
                    {
                        byte[] encrypted = File.ReadAllBytes(file);
                        string json = CryptoHelper.Decrypt(encrypted);
                        var record = JsonSerializer.Deserialize<PatientRecord>(json);

                        if (record == null)
                            continue;

                        // 🟢 Match exact patient
                        if (record.PatientNo == _selectedPatientNo &&
                            record.Id.ToString() == _selectedPatientId)
                        {
                            return record;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load error: " + ex.Message);
                return null;
            }
        }

        private ReportCommentViewModel LoadReportCommentsForPrint()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentMainId) ||
                    string.IsNullOrWhiteSpace(_currentPatientNo) ||
                    string.IsNullOrWhiteSpace(_currentTestName))
                    return null;

                string folder1 = Path.Combine(
                    Application.StartupPath,
                    "System",
                    "ReportComment",
                    $"{_currentPatientNo}_{_currentMainId}"
                );
                string specificFolder = Path.Combine("ReportComment", $"{_currentPatientNo}_{_currentMainId}");
                string folder = AppPathManager.GetFolderPath(specificFolder);
                if (!Directory.Exists(folder))
                    return null;

                string filePath = Path.Combine(folder, $"{_currentTestName}.dat");

                if (!File.Exists(filePath))
                    return null;   // ✅ NO REPORT = OK

                byte[] encrypted = File.ReadAllBytes(filePath);
                string json = CryptoHelper.Decrypt(encrypted);

                return JsonSerializer.Deserialize<ReportCommentViewModel>(json);
            }
            catch
            {
                return null; // printing must never crash
            }
        }

        //Code For Get saved Images in Video Test
        private List<Image> LoadCapturedImagesForPrint()
        {
            List<Image> images = new List<Image>();

            try
            {
                if (string.IsNullOrWhiteSpace(_selectedPatientId) ||
                    string.IsNullOrWhiteSpace(_currentTestName) ||
                    string.IsNullOrWhiteSpace(_currentPatientNo))
                    return images;

                //string patientsRoot = Path.Combine(
                //    Application.StartupPath,
                //    "Saved Data",
                //    "PatientsData"
                //);
                string patientsRoot = AppPathManager.GetFolderPath("Samples");
                if (!Directory.Exists(patientsRoot))
                    return images;

                string patientFolder = Directory.GetDirectories(patientsRoot)
                    .FirstOrDefault(d =>
                        Path.GetFileName(d)
                            .StartsWith(_currentPatientNo + "_")
                    );

                if (patientFolder == null)
                    return images;

                string imageFolder = Path.Combine(
                    patientFolder,
                    "Tests",
                    _currentTestName.Trim(),
                    "CapturedImages",
                    $"Patient_{_selectedPatientId.Trim()}"
                );

                if (!Directory.Exists(imageFolder))
                    return images;

                var files = Directory.GetFiles(imageFolder)
                    .Where(f =>
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f);

                foreach (var path in files)
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        using (var ms = new MemoryStream(bytes))
                        using (var img = Image.FromStream(ms))
                        {
                            images.Add(new Bitmap(img));
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return images;
        }



        private HospitalAndDocterModel _hospitalSetup;
        private PatientRecord _patientSetup;



        private PrintPreviewDialog _previewDialog;
        private PrintPreviewControl _previewControl;

        private void PrintPreview_Click(object sender, EventArgs e)
        {
            AutoSaveGraphTest_Click(sender, e);

            _hospitalSetup = LoadHospitalAndDoctorSettings();
            if (_hospitalSetup == null)
            {
                MessageBox.Show("No hospital setup found!");
                return;
            }

            _patientSetup = GetPatinetData();
            if (_patientSetup == null)
            {
                MessageBox.Show("No patient data found!");
                return;
            }

            SetupReport();
            _currentPage = 1;

            _doc.DefaultPageSettings.Margins =
                new System.Drawing.Printing.Margins(10, 10, 10, 10);

            _doc.OriginAtMargins = false;

            try
            {
                _previewDialog = new PrintPreviewDialog
                {
                    Document = _doc,
                    Width = 1200,
                    Height = 800,
                    StartPosition = FormStartPosition.CenterScreen,
                    UseAntiAlias = true
                };

                _previewControl = _previewDialog.PrintPreviewControl;
                _previewControl.AutoZoom = false;
                _previewControl.Zoom = 1.2;
                _previewControl.StartPage = 0;
                _previewControl.Dock = DockStyle.Fill;
                _previewControl.TabStop = true;

                //EnableMouseWheelScroll(_previewControl);
                //EnableMouseWheelPaging(_previewDialog, _previewControl);
                EnableMouseWheelVerticalScroll(_previewDialog, _previewControl);


                AddCustomNavigationButtons(_previewDialog);

                _previewDialog.Shown += (s, args) =>
                {
                    AttachToolbarHandler(_previewDialog);
                    _previewControl.Focus();
                };

                ((Form)_previewDialog).WindowState = FormWindowState.Maximized;

                _previewDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to show print preview.\n" + ex.Message,
                    "Print Preview Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }





        private void AddCustomNavigationButtons(PrintPreviewDialog preview)
        {
            ToolStrip toolStrip = preview.Controls
                .OfType<ToolStrip>()
                .FirstOrDefault();

            if (toolStrip == null) return;

            toolStrip.RenderMode = ToolStripRenderMode.System;
            toolStrip.ImageScalingSize = new Size(24, 24);
            toolStrip.Height = 44;

            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.AutoSize = false;
                item.Height = 34;
                item.Margin = new Padding(4, 5, 4, 5);
            }

            //Start Code For PrintPreview Print Button Not Show Print Setting Direct Print Comment on 19-02-2026 and code for next method

            //// ---- MODIFY EXISTING BUTTONS ----
            //foreach (ToolStripItem item in toolStrip.Items)
            //{
            //    if (item.ToolTipText == "Print")
            //    {
            //        item.Text = "Print";
            //        item.Image = SystemIcons.Application.ToBitmap(); //WinLogo
            //        item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            //        item.ImageScaling = ToolStripItemImageScaling.None;
            //        item.TextImageRelation = TextImageRelation.ImageBeforeText;
            //        item.BackColor = Color.Gainsboro;
            //        item.Width = 90;

            //        item.Click -= PrintReport_Click; // Remove if already attached
            //        item.Click += PrintReport_Click; // Add your handler
            //    }

            //    if (item.ToolTipText == "Close")
            //    {
            //        item.Text = "Close";
            //        item.Image = SystemIcons.Error.ToBitmap();
            //        item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            //        item.ImageScaling = ToolStripItemImageScaling.None;
            //        item.TextImageRelation = TextImageRelation.ImageBeforeText;
            //        item.BackColor = Color.Gainsboro;
            //        item.Width = 90;
            //    }
            //}
            //End Code For PrintPreview Print Button Not Show Print Setting Direct Print Comment on 19-02-2026 and code for next method

            //Start Code For Print Preview Print Button Show Print Setting 19-02-2026
            // Store original items to remove later
            List<ToolStripItem> itemsToRemove = new List<ToolStripItem>();
            int printButtonIndex = -1;
            int closeButtonIndex = -1;

            // First pass: find indexes and mark items for removal
            for (int i = 0; i < toolStrip.Items.Count; i++)
            {
                ToolStripItem item = toolStrip.Items[i];

                if (item.ToolTipText == "Print")
                {
                    printButtonIndex = i;
                    itemsToRemove.Add(item);
                }
                else if (item.ToolTipText == "Close")
                {
                    closeButtonIndex = i;
                    itemsToRemove.Add(item);
                }
            }

            // Remove the original buttons
            foreach (var item in itemsToRemove)
            {
                toolStrip.Items.Remove(item);
            }

            // ---- ADD CUSTOM PRINT BUTTON ----
            ToolStripButton btnCustomPrint = new ToolStripButton
            {
                Text = "Print",
                Image = SystemIcons.Application.ToBitmap(),
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 100,
                Height = 34,
                Margin = new Padding(4, 5, 4, 5),
                ToolTipText = "Print with settings"
            };

            // Add your PrintReport_Click handler
            btnCustomPrint.Click += PrintReport_Click;

            // Insert at the original print button position or at beginning
            if (printButtonIndex >= 0 && printButtonIndex < toolStrip.Items.Count)
                toolStrip.Items.Insert(printButtonIndex, btnCustomPrint);
            else
                toolStrip.Items.Insert(0, btnCustomPrint);

            // ---- ADD CUSTOM CLOSE BUTTON ----
            ToolStripButton btnCustomClose = new ToolStripButton
            {
                Text = "Close",
                Image = SystemIcons.Error.ToBitmap(),
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 110,
                Height = 34,
                Margin = new Padding(4, 5, 4, 5),
                ToolTipText = "Close preview"
            };

            // Add close handler
            btnCustomClose.Click += (s, e) => preview.Close();

            // Insert at the original close button position
            if (closeButtonIndex >= 0 && closeButtonIndex < toolStrip.Items.Count)
                toolStrip.Items.Insert(closeButtonIndex, btnCustomClose);
            else
                toolStrip.Items.Add(btnCustomClose);
            //End Code For Print Preview Print Button Show Print Setting 19-02-2026


            // ---- DOWNLOAD PDF ----
            ToolStripButton btnDownloadPdf = new ToolStripButton
            {
                Text = "Download PDF",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = SystemIcons.Shield.ToBitmap(),
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 140,
                Height = 34,
                Margin = new Padding(8, 4, 6, 4)
            };

            btnDownloadPdf.Click += DownloadPdf_Click;

            // Add to toolbar (RIGHT SIDE looks best)
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(btnDownloadPdf);


            // ---- Start Code For DOWNLOAD WORD ----
            ToolStripButton btnDownloadWord = new ToolStripButton
            {
                Text = "Download Word",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = SystemIcons.WinLogo.ToBitmap(),
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 150,
                Height = 34,
                Margin = new Padding(6, 4, 6, 4)
            };

            btnDownloadWord.Click += DownloadWord_Click;

            toolStrip.Items.Add(btnDownloadWord);
            // ---- End Code For DOWNLOAD WORD ----


            // ---- PREVIOUS ----
            ToolStripButton btnPrev = new ToolStripButton
            {
                Text = "◀ Previous",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 110,
                Height = 34,
                Padding = new Padding(6, 4, 70, 4),
                Margin = new Padding(6, 4, 8, 4)   // RIGHT GAP
            };

            // ---- NEXT ----
            ToolStripButton btnNext = new ToolStripButton
            {
                Text = "Next ▶",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ImageScaling = ToolStripItemImageScaling.None,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                BackColor = Color.Gainsboro,
                Width = 90,
                Height = 34,
                Padding = new Padding(12, 4, 70, 4),
                Margin = new Padding(12, 4, 6, 4)   // LEFT GAP
            };


            btnPrev.Click += (s, e) =>
            {
                if (_previewControl.StartPage > 0)
                {
                    _previewControl.StartPage--;
                    ResetPreviewScroll(_previewControl);
                }
            };

            btnNext.Click += (s, e) =>
            {
                _previewControl.StartPage++;
                ResetPreviewScroll(_previewControl);
            };


            _previewDialog.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.PageUp)
                {
                    if (_previewControl.StartPage > 0) _previewControl.StartPage--;
                    ResetPreviewScroll(_previewControl);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    _previewControl.StartPage++;
                    ResetPreviewScroll(_previewControl);
                    e.Handled = true;
                }
            };

            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(btnPrev);
            toolStrip.Items.Add(btnNext);
        }

        private void ResetPreviewScroll(PrintPreviewControl preview)
        {
            if (preview == null || preview.IsDisposed) return;

            // Reset both vertical & horizontal scroll to start
            SendMessage(preview.Handle, WM_VSCROLL, (IntPtr)SB_TOP, IntPtr.Zero);
            SendMessage(preview.Handle, WM_HSCROLL, (IntPtr)SB_LEFT, IntPtr.Zero);

            // Force redraw
            preview.Invalidate();
            preview.Update();
        }
        private void EnableMouseWheelVerticalScroll(PrintPreviewDialog dialog, PrintPreviewControl preview)
        {
            if (dialog == null || preview == null) return;

            // Important: page must be bigger than control height to show internal scrollbars.
            preview.AutoZoom = false;
            // Increase zoom if you still don't see vertical scrolling
            // preview.Zoom = 1.2;  // keep your existing value

            // Make sure preview actually receives wheel events
            dialog.Shown += (s, e) => preview.Focus();
            preview.MouseEnter += (s, e) => preview.Focus();

            preview.MouseWheel += (s, e) =>
            {
                // Scroll inside the page using the preview's internal scrollbar
                if (e.Delta > 0)
                    SendMessage(preview.Handle, WM_VSCROLL, (IntPtr)SB_LINEUP, IntPtr.Zero);
                else if (e.Delta < 0)
                    SendMessage(preview.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
            };
        }



        private void EnableMouseWheelPaging(PrintPreviewDialog dialog, PrintPreviewControl preview)
        {
            if (dialog == null || preview == null) return;

            // Make sure keys/wheel can be handled even when toolbar has focus
            dialog.KeyPreview = true;

            void goPrev()
            {
                if (preview.StartPage > 0)
                    preview.StartPage--;
            }

            void goNext()
            {
                // If you know total pages, clamp here; otherwise allow increment
                preview.StartPage++;
            }

            // Handle mouse wheel on BOTH dialog + preview (toolbar often steals focus)
            MouseEventHandler wheelHandler = (s, e) =>
            {
                if (e.Delta > 0) goPrev();
                else if (e.Delta < 0) goNext();
            };

            dialog.MouseWheel -= wheelHandler;
            dialog.MouseWheel += wheelHandler;

            preview.MouseWheel -= wheelHandler;
            preview.MouseWheel += wheelHandler;

            // Ensure preview gets focus when user clicks anywhere inside
            dialog.Shown += (s, e) => preview.Focus();
            preview.MouseEnter += (s, e) => preview.Focus();

            // Optional: keyboard paging too
            dialog.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.PageUp) { goPrev(); e.Handled = true; }
                if (e.KeyCode == Keys.PageDown) { goNext(); e.Handled = true; }
            };
        }

        private void DownloadPdf_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF Files (*.pdf)|*.pdf";
                sfd.FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    PrintDocument pdfDoc = _doc;   // SAME document used in preview

                    PrinterSettings ps = new PrinterSettings
                    {
                        PrinterName = "Microsoft Print to PDF",
                        PrintToFile = true,
                        PrintFileName = sfd.FileName
                    };

                    pdfDoc.PrinterSettings = ps;
                    pdfDoc.PrintController = new StandardPrintController(); // no print dialog

                    pdfDoc.Print();

                    MessageBox.Show(
                        "PDF downloaded successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Failed to generate PDF.\n" + ex.Message,
                        "PDF Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }




        // ---- Start Code For DOWNLOAD WORD ----
        private void DownloadWord_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Word Document (*.docx)|*.docx";
                sfd.FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmm}.docx";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    //CreateWordReport(sfd.FileName);

                    MessageBox.Show(
                        "Word document downloaded successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Failed to generate Word file.\n" + ex.Message,
                        "Word Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }


        private List<string> GetReportTextLines()
        {
            List<string> lines = new List<string>();

            // Example – replace with your real report data
            lines.Add("UPP Result:");
            lines.Add("Flow Rate   : Normal");
            lines.Add("Pressure    : Stable");
            lines.Add("Conclusion  : Test Passed");

            return lines;
        }



        // ---- End Code For DOWNLOAD WORD ----

        //Start This code for show 5 button show page count
        private void AttachToolbarHandler(PrintPreviewDialog preview)
        {
            ToolStrip toolStrip = preview.Controls
                .OfType<ToolStrip>()
                .FirstOrDefault();

            if (toolStrip == null) return;

            toolStrip.ItemClicked -= ToolStrip_ItemClicked;
            toolStrip.ItemClicked += ToolStrip_ItemClicked;
        }

        private void ToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string tip = (e.ClickedItem.ToolTipText ?? "").Trim();

            double? zoom = null;

            if (tip.IndexOf("One", StringComparison.OrdinalIgnoreCase) >= 0)
                zoom = 0.60;
            else if (tip.IndexOf("Two", StringComparison.OrdinalIgnoreCase) >= 0)
                zoom = 0.50;
            else if (tip.IndexOf("Three", StringComparison.OrdinalIgnoreCase) >= 0)
                zoom = 0.50;
            else if (tip.IndexOf("Four", StringComparison.OrdinalIgnoreCase) >= 0)
                zoom = 0.25;
            else if (tip.IndexOf("Six", StringComparison.OrdinalIgnoreCase) >= 0)
                zoom = 0.25;

            if (zoom.HasValue)
            {
                _previewControl.AutoZoom = false;
                _previewControl.Zoom = zoom.Value;
            }
        }
        //End This code for show 5 button show page count

        private void PrintReport_Click(object sender, EventArgs e)
        {
            AutoSaveGraphTest_Click(sender, e);

            _hospitalSetup = LoadHospitalAndDoctorSettings();

            if (_hospitalSetup == null)
            {
                MessageBox.Show("No hospital setup found!");
                return;
            }

            _patientSetup = GetPatinetData();
            if (_patientSetup == null)
            {
                MessageBox.Show("No patient data found!");
                return;
            }

            SetupReport();



            _currentPage = 1;



            //using (PrintDialog dlg = new PrintDialog())
            //{
            //    dlg.Document = _doc;
            //    dlg.AllowSomePages = false;
            //    dlg.UseEXDialog = true; // 🔥 modern Windows dialog

            //    if (dlg.ShowDialog(this) == DialogResult.OK)
            //    {
            //        _doc.Print(); // 🔥 Direct system print
            //    }
            //}


            using (PrintDialog dlg = new PrintDialog())
            {
                dlg.Document = _doc;
                _doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(10, 10, 10, 10);
                _doc.OriginAtMargins = false;
                dlg.AllowSomePages = false;
                dlg.UseEXDialog = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _currentPage = 1;
                    try
                    {


                        PrintPreviewDialog preview = new PrintPreviewDialog
                        {
                            Document = _doc,
                            Width = 1200,
                            Height = 800,
                            StartPosition = FormStartPosition.CenterScreen
                        };

                        // Optional – better rendering quality
                        ((Form)preview).WindowState = FormWindowState.Maximized;
                        _doc.Print();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Unable to show print preview.\n" + ex.Message,
                            "Print Preview Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }


                }
            }

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.P))
            {
                PrintReport_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        //

        private void SetupReport()
        {
            var h = _hospitalSetup;
            var p = _patientSetup;

            var rc = LoadReportCommentsForPrint();

            // Safe Age Conversion
            int ageValue = 0;

            if (!string.IsNullOrWhiteSpace(p.Age))
            {
                string cleanAge = new string(p.Age.Where(char.IsDigit).ToArray());
                int.TryParse(cleanAge, out ageValue);
            }

            var data = new ReportDataPrint
            {
                FontFamily = "Arial",
                HospitalName = h.HospitalName,
                HospitalAddressLine1 = h.Address,
                HospitalAddressLine2 = h.AddressTwo,
                HospitalPoneNo = h.PhoneNo,
                HospitalGmail = h.Email,

                DoctorName = h.DocterName,
                DoctorPost = h.Post,
                DoctorDegree = h.Degree,

                PatientId = p.PatientNo,
                TestName = p.Test,
                PatientName = p.PatientName,
                PatientAddress = p.Address,
                TechnicianName = p.TechnicianName,
                PatientMobile = p.MobileNo,
                Age = ageValue,
                Sex = p.Gender,
                Weight = p.Weight,
                ReferredBy = p.ReferredBy,
                Symptoms = p.Symptoms,
                TestDate = DateTime.Now,

                //Code Print Header 
                HeadOne = h.HeadOne,
                HeadTwo = h.HeadTwo,
                HeadThree = h.HeadThree,
                HeadFour = h.HeadFour,
                HeadFive = h.HeadFive,

                Nomograms = h.Nomograms,
                PrintFlowNomograms = h.PrintFlowNomograms,
                PrintImpression = h.PrintImpression,
                CompanyName = h.CompanyName,
                LogoImage = h.LogoImage,
                LetterHeadImage = h.LetterHeadImage,

                DefaultHeader = h.DefaultHeader,
                LetterHead = h.LetterHead,
                HospitalLogo = h.HospitalLogo,
                HospitalDataAndLogo = h.TextAndLogo,

                //Report COmment 
                PatientHistory = rc?.PatientHistory ?? string.Empty,
                CatheterType = rc?.CatheterType ?? string.Empty,
                InfusionRate = rc?.InfusionRate ?? string.Empty,
                TestPosition = rc?.TestPosition ?? string.Empty,

                Sensations = rc?.Sensations ?? string.Empty,
                AnalTone = rc?.AnalTone ?? string.Empty,
                ClitoraBulboca = rc?.ClitoraBulboca ?? string.Empty,
                ValuntaryContraction = rc?.ValuntaryContraction ?? string.Empty,
                FlowRate = rc?.FlowRate ?? string.Empty,
                VoidedVolume = rc?.VoidedVolume ?? string.Empty,
                PostVoid = rc?.PostVoid ?? string.Empty,
                BladderCapacity = rc?.BladderCapacity ?? string.Empty,
                Proprioception = rc?.Proprioception ?? string.Empty,
                Complaince = rc?.Complaince ?? string.Empty,
                Detrusor = rc?.Detrusor ?? string.Empty,
                PdetVoid = rc?.PdetVoid ?? string.Empty,
                PDET = rc?.PDET ?? string.Empty,
                PdetLeak = rc?.PdetLeak ?? string.Empty,
                VoidingPressure = rc?.VoidingPressure ?? string.Empty,
                Sphincter = rc?.Sphincter ?? string.Empty,
                UrethralClosure = rc?.UrethralClosure ?? string.Empty,
                CoughStress = rc?.CoughStress ?? string.Empty,
                ThereIsTwo = rc?.ThereIsTwo ?? string.Empty,
                ThereIsOne = rc?.ThereIsOne ?? string.Empty,
                ReportBy = rc?.ReportBy ?? string.Empty,

                ResultConclusion = rc?.ResultConclusion ?? string.Empty,

                // 🔥 LOAD IMAGES HERE
                CapturedImages = LoadCapturedImagesForPrint(),

                // Marker colors from ScaleAndColorSetup
                BladderSensationColor = BladderColor,
                ResponseMarkerColor = ResponseColor,
                GeneralPurposeColor = GPColor  //textGeneralPurpose
            };

            // Build graph config from recorded samples + current test def
            ReportGraphConfig graphCfg = new ReportGraphConfig();
            graphCfg.Samples = new List<ReportSample>();

            if (_recorded != null)
            {
                for (int i = 0; i < _recorded.Count; i++)
                {
                    var r = _recorded[i];
                    double[] copy = (double[])r.Values.Clone();
                    graphCfg.Samples.Add(new ReportSample { T = r.T, Values = copy });
                }
            }

            //graphCfg.Samples = new List<ReportSample>();

            //if (_recorded != null && _recorded.Count > 0)
            //{
            //    // Baseline per channel from first sample so each channel starts from 0 in print preview
            //    double[] base0 = null;
            //    if (_recorded[0].Values != null)
            //        base0 = (double[])_recorded[0].Values.Clone();

            //    for (int i = 0; i < _recorded.Count; i++)
            //    {
            //        var r = _recorded[i];
            //        double[] copy = (r.Values != null) ? (double[])r.Values.Clone() : null;

            //        if (copy != null && base0 != null)
            //        {
            //            int n = Math.Min(copy.Length, base0.Length);
            //            for (int ch = 0; ch < n; ch++)
            //            {
            //                // Skip NaN/Infinity to avoid polluting output
            //                double b = base0[ch];
            //                if (!double.IsNaN(b) && !double.IsInfinity(b))
            //                    copy[ch] = copy[ch] - b;
            //            }
            //        }

            //        graphCfg.Samples.Add(new ReportSample { T = r.T, Values = copy });
            //    }
            //}


            graphCfg.TestDef = _currentTestDef;
            graphCfg.ActiveIndices = (_activeIndices != null && _activeIndices.Length > 0)
                ? (int[])_activeIndices.Clone()
                : null;

            if (_liveChart != null && _liveChart.Markers != null && _liveChart.Markers.Count > 0)
            {
                graphCfg.Markers = _liveChart.Markers
                    .Select(m => new ReportMarker
                    {
                        T = m.T,
                        Label = m.Label
                    })
                    .ToList();
            }

            //  System.Diagnostics.Debug.WriteLine("PRINT _recorded first sample: " +
            // string.Join(",", _recorded[0].Values.Select(v => v.ToString("0.##"))));


            _printer = new LegacyUroReportPrinter(data, graphCfg);

            _currentPage = 1;
            _doc = new PrintDocument();
            _doc.PrintPage += (s, e) =>
            {
                _printer.PrintPage(s, e, _currentPage);
                if (e.HasMorePages)
                    _currentPage++;
                else
                    _currentPage = 1;
            };
            //_doc.PrintPage += delegate (object sender, PrintPageEventArgs e)
            //{
            //    _printer.PrintPage(sender, e, _currentPage);
            //    if (e.HasMorePages) _currentPage++;
            //    else _currentPage = 1;
            //};
        }


        //This code for Review mode any change on test so auto save test and Print Report
        private void AutoSaveGraphTest_Click(object sender, EventArgs e)
        {
            if (!_isUpdateMode || string.IsNullOrEmpty(_currentSavedTestPath))
            {
                MessageBox.Show("No loaded test available to update.");
                return;
            }

            if (!File.Exists(_currentSavedTestPath))
            {
                MessageBox.Show("Original test file not found.");
                return;
            }

            try
            {
                SaveUtt(_currentSavedTestPath);
                //This Line save the images comment on 09-01-2026
                //SaveCapturedImages(_currentSavedTestPath);

                // ✅ NOW it is safe to clear
                ClearArtifactSelections();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update test.\n" + ex.Message);
            }
        }


        private static TestMode GetModeFromTestName(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return TestMode.Cystometry;

            string key = testName.Trim().ToLowerInvariant();

            // accept more real-world names
            if (key == "upp" || key.Contains("urethral") || key.Contains("u.p.p") || key.Contains("u p p"))
                return TestMode.UPP;

            if (key.Contains("whitaker"))
                return TestMode.Whitaker;

            return TestMode.CystoUroflowEMG;
        }

    }
}