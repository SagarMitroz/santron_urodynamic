// DeviceWatcher.cs
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SantronWinApp
{
    public class DeviceWatcher : IDisposable
    {
        public delegate bool DeviceProbe();
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler StateChanged;
        private Timer _disconnectCheckTimer;
        private int _disconnectChecks;

        private const int _disconnectChecksNeeded = 3;   // 3 consecutive fails
        private const int _disconnectCheckIntervalMs = 300;  // total ~900ms
        private DateTime _lastStateChangeAt = DateTime.MinValue;
        private const int _minDwellMs = 900; // don’t flip state faster than this

        public bool IsConnected { get { return _isConnected; } }

        public void SetProbe(DeviceProbe probe) { _probe = probe; }

        public void Start(IWin32Window owner)
        {
            if (_started || owner == null) return;
            _hwnd = owner.Handle;
            RegisterForNotifications();
            _started = true;

            // initial state
            UpdateState(ProbeIfAvailable());
        }

        public void Stop()
        {
            UnregisterNotifications();
            _hwnd = IntPtr.Zero;
            _started = false;
            if (_retryTimer != null) { _retryTimer.Stop(); _retryTimer.Tick -= RetryTimer_Tick; _retryTimer = null; }
        }

        public void Dispose() { Stop(); }

        // Call from owner's WndProc
        public void OnWndProc(ref Message m)
        {
            if (m.Msg != WM_DEVICECHANGE) return;
            int wp = m.WParam.ToInt32();
            if (wp == DBT_DEVICEARRIVAL)
            {
                bool ok = ProbeIfAvailable();
                if (ok)
                {
                    CancelRetry();
                    CancelDisconnectCheck();
                    UpdateStateDebounced(true);   // flip to Connected only when probe passes
                }
                else
                {
                    StartRetry();                 // retry a few times; UpdateState(true) happens in RetryTimer_Tick
                }
            }

            //if (wp == DBT_DEVICEARRIVAL)
            //{
            //    // Arrived: probe; if not ready yet, retry briefly (existing logic)
            //    bool ok = ProbeIfAvailable();
            //    if (!ok) StartRetry(); else CancelRetry();
            //    CancelDisconnectCheck();              // cancel any pending "disconnect confirm"
            //    UpdateStateDebounced(true);           // flip to Connected (with dwell guard)
            //}
            else if (wp == DBT_DEVICEREMOVECOMPLETE)
            {
                // Start a delayed confirmation instead of instant disconnect
                StartDisconnectCheck();
            }
            else if (wp == DBT_DEVNODES_CHANGED)
            {
                // Generic churn; probe but don’t flip to Disconnected immediately
                bool ok = ProbeIfAvailable();
                if (ok)
                {
                    CancelDisconnectCheck();
                    UpdateStateDebounced(true);
                }
                else
                {
                    StartDisconnectCheck();
                }
            }
        }
        // New helpers 
        private void StartDisconnectCheck()
        {
            CancelDisconnectCheck();
            _disconnectChecks = 0;
            _disconnectCheckTimer = new Timer();
            _disconnectCheckTimer.Interval = _disconnectCheckIntervalMs;
            _disconnectCheckTimer.Tick += DisconnectCheckTimer_Tick;
            _disconnectCheckTimer.Start();
        }

        private void DisconnectCheckTimer_Tick(object sender, EventArgs e)
        {
            bool ok = ProbeIfAvailable();
            if (ok)
            {
                CancelDisconnectCheck();
                UpdateStateDebounced(true);
                return;
            }

            _disconnectChecks++;
            if (_disconnectChecks >= _disconnectChecksNeeded)
            {
                CancelRetry();
                CancelDisconnectCheck();
                UpdateStateDebounced(false);
            }
        }

        private void CancelDisconnectCheck()
        {
            if (_disconnectCheckTimer != null)
            {
                _disconnectCheckTimer.Stop();
                _disconnectCheckTimer.Tick -= DisconnectCheckTimer_Tick;
                _disconnectCheckTimer = null;
            }
        }

        // Wrap existing UpdateState with minimum dwell time protection
        private void UpdateStateDebounced(bool nowConnected)
        {
            // Don’t flap state if last flip was too recent.
            var dwell = (DateTime.UtcNow - _lastStateChangeAt).TotalMilliseconds;
            if (_isConnected == nowConnected) return;
            if (dwell < _minDwellMs) return;

            UpdateState(nowConnected);           // existing method
            _lastStateChangeAt = DateTime.UtcNow;
        }
       

        // ---------- internals ----------
        private IntPtr _hwnd = IntPtr.Zero;
        private IntPtr _usbNotify = IntPtr.Zero;
        private IntPtr _comNotify = IntPtr.Zero;
        private bool _isConnected = false;
        private bool _started = false;
        private DeviceProbe _probe;

        // tiny retry (old WinForms Timer) to handle “arrived but not ready yet”
        private Timer _retryTimer;
        private int _retriesLeft;

        private void StartRetry()
        {
            CancelRetry();
            _retriesLeft = 3;   // try 3 times
            _retryTimer = new Timer();
            _retryTimer.Interval = 500; // 0.5s apart
            _retryTimer.Tick += new EventHandler(RetryTimer_Tick);
            _retryTimer.Start();
        }

        private void CancelRetry()
        {
            if (_retryTimer != null)
            {
                _retryTimer.Stop();
                _retryTimer.Tick -= new EventHandler(RetryTimer_Tick);
                _retryTimer = null;
            }
            _retriesLeft = 0;
        }

        private void RetryTimer_Tick(object sender, EventArgs e)
        {
            if (_retriesLeft <= 0) { CancelRetry(); return; }
            _retriesLeft--;

            bool ok = ProbeIfAvailable();
            if (ok)
            {
                UpdateState(true);
                CancelRetry();
            }
        }

        private bool ProbeIfAvailable()
        {
            if (_probe == null) return true;  // trust OS if no probe supplied
            try { return _probe(); } catch { return false; }
        }

        private void UpdateState(bool nowConnected)
        {
            if (_isConnected == nowConnected) return;
            _isConnected = nowConnected;
            if (StateChanged != null) StateChanged(this, EventArgs.Empty);
            if (_isConnected) { if (Connected != null) Connected(this, EventArgs.Empty); }
            else { if (Disconnected != null) Disconnected(this, EventArgs.Empty); }
        }

        private void RegisterForNotifications()
        {
            if (_hwnd == IntPtr.Zero) return;

            DEV_BROADCAST_DEVICEINTERFACE f = new DEV_BROADCAST_DEVICEINTERFACE();
            f.dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE));
            f.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;

            // USB
            f.dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE;
            _usbNotify = RegisterDeviceNotification(_hwnd, ref f, DEVICE_NOTIFY_WINDOW_HANDLE);

            // COM
            f.dbcc_classguid = GUID_DEVINTERFACE_COMPORT;
            _comNotify = RegisterDeviceNotification(_hwnd, ref f, DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        private void UnregisterNotifications()
        {
            try { if (_usbNotify != IntPtr.Zero) UnregisterDeviceNotification(_usbNotify); } catch { }
            try { if (_comNotify != IntPtr.Zero) UnregisterDeviceNotification(_comNotify); } catch { }
            _usbNotify = IntPtr.Zero; _comNotify = IntPtr.Zero;
        }

        // P/Invoke + constants
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x0005;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
        private static readonly Guid GUID_DEVINTERFACE_COMPORT = new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73");

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, ref DEV_BROADCAST_DEVICEINTERFACE notificationFilter, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);
    }
}
