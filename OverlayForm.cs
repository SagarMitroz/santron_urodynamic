using System;
using System.Drawing;
using System.Windows.Forms;

namespace SantronWinApp
{
    public class OverlayForm : Form
    {
        private readonly Form _owner;
        private Bitmap _backdrop;

        public OverlayForm(Form owner)
        {
            _owner = owner;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.Black;     // opaque; no transparency tricks

            // match owner
            Bounds = owner.Bounds;

            // follow owner
            owner.LocationChanged += SyncToOwner;
            owner.SizeChanged += SyncToOwner;
            owner.ResizeEnd += SyncToOwner;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BuildBackdrop();         // capture + darken once we’re visible
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_backdrop != null)
            {
                e.Graphics.DrawImageUnscaled(_backdrop, Point.Empty);
            }
            else
            {
                // fallback (solid dim) – still looks fine
                using (var b = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_backdrop != null) { _backdrop.Dispose(); _backdrop = null; }
                if (_owner != null)
                {
                    _owner.LocationChanged -= SyncToOwner;
                    _owner.SizeChanged -= SyncToOwner;
                    _owner.ResizeEnd -= SyncToOwner;
                }
            }
            base.Dispose(disposing);
        }

        private void SyncToOwner(object sender, EventArgs e)
        {
            Bounds = _owner.Bounds;
            BuildBackdrop();
            Invalidate();
        }

        private void BuildBackdrop()
        {
            try
            {
                // dispose previous
                if (_backdrop != null) { _backdrop.Dispose(); _backdrop = null; }

                // 1) snapshot owner
                var bmp = new Bitmap(_owner.ClientSize.Width, _owner.ClientSize.Height);
                _owner.DrawToBitmap(bmp, new Rectangle(Point.Empty, _owner.ClientSize));

                // 2) darken by painting semi-transparent black ON the bitmap
                using (var g = Graphics.FromImage(bmp))
                using (var dim = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                {
                    g.FillRectangle(dim, new Rectangle(Point.Empty, bmp.Size));
                }

                // 3) if owner has borders/title, we’re aligning to Bounds (screen coords).
                //    So pad the snapshot to full overlay size.
                var full = new Bitmap(Width, Height);
                using (var g2 = Graphics.FromImage(full))
                {
                    // place client snapshot at correct offset within the whole window rect
                    var clientScreen = _owner.PointToScreen(Point.Empty);
                    var windowScreen = _owner.Bounds.Location;
                    var offset = new Point(clientScreen.X - windowScreen.X, clientScreen.Y - windowScreen.Y);
                    g2.Clear(Color.Black);
                    g2.DrawImageUnscaled(bmp, offset);
                }

                bmp.Dispose();
                _backdrop = full;
            }
            catch
            {
                // If anything fails, leave _backdrop null; OnPaint will draw a solid dim
            }
        }

        /// <summary>Show any child form as popup with dim overlay.</summary>
        public static void ShowPopup(Form owner, Form child)
        {
            using (var overlay = new OverlayForm(owner))
            {
                overlay.Show();                                  // modeless overlay
                child.StartPosition = FormStartPosition.CenterParent;
                child.ShowDialog(overlay);                       // modal to overlay
            }                                                    // overlay auto-disposed
        }
    }
}
