using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SantronWinApp
{
    public partial class ScreenDimOverlay : Form
    {
        private readonly byte _alpha;

        public ScreenDimOverlay(byte alpha = 140)
        {
            _alpha = alpha;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;

            // Cover the screen where the caller lives
            // (You can change to PrimaryScreen if you prefer.)
            Bounds = Screen.FromPoint(Cursor.Position).Bounds;

            //InitializeComponent();
        }

        // Pure layered, hidden from Alt+Tab, doesn’t steal focus
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_LAYERED = 0x00080000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_NOACTIVATE = 0x08000000;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;

        // No normal painting → prevents any opaque rectangle
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyLayeredBitmap();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyLayeredBitmap();
        }

        private void ApplyLayeredBitmap()
        {
            if (Width <= 0 || Height <= 0 || Handle == IntPtr.Zero) return;

            using (var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                // Uniform semi-transparent black over entire screen area
                g.Clear(Color.FromArgb(_alpha, 0, 0, 0));
                SetBitmap(bmp);
            }
        }

        // ---- UpdateLayeredWindow plumbing ----
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pprSrc, int crKey,
            ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private const int ULW_ALPHA = 0x00000002;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        private void SetBitmap(Bitmap bitmap)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBits = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBits = SelectObject(memDc, hBitmap);

                var size = new SIZE { cx = bitmap.Width, cy = bitmap.Height };
                var src = new POINT { x = 0, y = 0 };
                var dst = new POINT { x = Left, y = Top };

                var blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,   // use per-pixel alpha
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(Handle, screenDc, ref dst, ref size,
                                    memDc, ref src, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (oldBits != IntPtr.Zero) SelectObject(memDc, oldBits);
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        // Helper to show any dialog with full-screen dim behind it
        public static void ShowDialogWithDim(Form dialog, byte alpha = 140)
        {
            using (var overlay = new ScreenDimOverlay(alpha))
            {
                overlay.Show();                         // show dim overlay
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ShowDialog(overlay);             // modal above overlay
            }                                           // overlay disposed after close
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ScreenDimOverlay
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "ScreenDimOverlay";
            this.Load += new System.EventHandler(this.ScreenDimOverlay_Load);
            this.ResumeLayout(false);

        }

        private void ScreenDimOverlay_Load(object sender, EventArgs e)
        {

        }

    }
}
