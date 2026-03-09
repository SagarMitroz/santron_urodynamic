// CameraController.cs  (add to your project)
using System;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;

namespace SantronWinApp
{
    public sealed class CameraController : IDisposable
    {
        private FilterInfoCollection _devices;
        private VideoCaptureDevice _source;
        private bool _isRunning;

        public event Action<Bitmap> Frame;   // fires on every new frame
        public bool IsRunning { get { return _isRunning; } }
        public int DeviceCount { get { return _devices != null ? _devices.Count : 0; } }

        public void Initialize()
        {
            _devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            // don’t throw if none; caller can check DeviceCount
        }

        public void Start(int deviceIndex = 0, int desiredWidth = 640, int desiredHeight = 480)
        {
            if (_isRunning) return;
            if (_devices == null) Initialize();
            if (_devices == null || _devices.Count == 0) throw new InvalidOperationException("No camera found.");

            if (deviceIndex < 0 || deviceIndex >= _devices.Count) deviceIndex = 0;

            _source = new VideoCaptureDevice(_devices[deviceIndex].MonikerString);

            // pick a matching resolution if available (keep it defensive for older drivers)
            try
            {
                var caps = _source.VideoCapabilities;
                for (int i = 0; i < caps.Length; i++)
                {
                    if (caps[i].FrameSize.Width == desiredWidth && caps[i].FrameSize.Height == desiredHeight)
                    {
                        _source.VideoResolution = caps[i];
                        break;
                    }
                }
            }
            catch { /* ignore, use default */ }

            _source.NewFrame += OnNewFrame;
            _source.Start();
            _isRunning = true;
        }

        public void Stop()
        {
            if (_source != null)
            {
                try
                {
                    if (_source.IsRunning)
                    {
                        _source.SignalToStop();
                        _source.WaitForStop();
                    }
                }
                catch { /* ignore on shutdown */ }
                finally
                {
                    _source.NewFrame -= OnNewFrame;
                    _source = null;
                }
            }
            _isRunning = false;
        }

        private void OnNewFrame(object sender, NewFrameEventArgs e)
        {
            Bitmap frame = null;
            try { frame = (Bitmap)e.Frame.Clone(); }
            catch { /* skip */ }

            if (frame != null && Frame != null)
                Frame(frame);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
