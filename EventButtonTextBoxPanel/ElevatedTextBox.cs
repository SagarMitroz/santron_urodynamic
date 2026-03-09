using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SantronWinApp
{
    [DefaultEvent("TextChanged")]
    [DefaultProperty("Text")]
    public class ElevatedTextBox : Control
    {
       
        private readonly TextBox _tb = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = false
        };

        // Appearance
        private int _cornerRadius = 3;
        private Padding _textPadding = new Padding(12, 7, 12, 10);
        private int _shadowSize = 5; 
        private Color _shadowColor = Color.Black;
        private int _shadowOpacity = 35; 
        private int _borderThickness = 0;
        private Color _borderColor = Color.Transparent;
        private Color _fillColor = Color.White;

        private bool _hovered;

        public ElevatedTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            _tb.BackColor = _fillColor;  // we paint our own background
            ForeColor = Color.Black;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            // default size (taller)
            Size = new Size(260, 64);

            // wire inner TextBox
            _tb.Font = Font;
            _tb.ForeColor = ForeColor;
            _tb.BackColor = _fillColor;  // ignored; host paints
            _tb.TabStop = true;
            _tb.TextChanged += (s, e) => OnTextChanged(e);
            _tb.KeyDown += (s, e) => OnKeyDown(e);
            _tb.GotFocus += (s, e) => { Invalidate(); };
            _tb.LostFocus += (s, e) => { Invalidate(); };

            Controls.Add(_tb);

            // mouse state for subtle shadow lift
            MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _hovered = false; Invalidate(); };
            GotFocus += (s, e) => _tb.Focus();
        }




        #region Exposed Properties

        [Category("Appearance")]
        public override string Text
        {
            get => _tb.Text;
            set { _tb.Text = value; Invalidate(); }
        }

        [Category("Appearance")]
        public override Font Font
        {
            get => base.Font;
            set { base.Font = value; if (_tb != null) _tb.Font = value; LayoutTextBox(); Invalidate(); }
        }

        [Category("Appearance")]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set { base.ForeColor = value; if (_tb != null) _tb.ForeColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Inner padding for the text area.")]
        public Padding TextPadding
        {
            get => _textPadding;
            set { _textPadding = value; LayoutTextBox(); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Corner radius of the textbox background.")]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Fill color of the textbox body.")]
        public Color FillColor
        {
            get => _fillColor;
            set { _fillColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Shadow blur/elevation size in pixels (drawn inside the control).")]
        public int ShadowSize
        {
            get => _shadowSize;
            set { _shadowSize = Math.Max(0, value); LayoutTextBox(); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Base color of the shadow (alpha is controlled via ShadowOpacity).")]
        public Color ShadowColor
        {
            get => _shadowColor;
            set { _shadowColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Opacity of the shadow (0-255).")]
        public int ShadowOpacity
        {
            get => _shadowOpacity;
            set { _shadowOpacity = Math.Min(255, Math.Max(0, value)); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Border thickness around the fill (0 = none).")]
        public int BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0, value); LayoutTextBox(); Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Border color when BorderThickness > 0.")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Behavior")]
        public bool ReadOnly
        {
            get => _tb.ReadOnly;
            set => _tb.ReadOnly = value;
        }

        [Category("Behavior")]
        public char PasswordChar
        {
            get => _tb.PasswordChar;
            set => _tb.PasswordChar = value;
        }

        [Category("Behavior")]
        public bool UseSystemPasswordChar
        {
            get => _tb.UseSystemPasswordChar;
            set => _tb.UseSystemPasswordChar = value;
        }

        #endregion

        #region Layout & Paint

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            LayoutTextBox();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            LayoutTextBox();
        }

        private void LayoutTextBox()
        {
            // Reserve inner area: shadow is drawn *inside* the control, so shrink fill by ShadowSize
            var outer = ClientRectangle;
            var fill = Rectangle.Inflate(outer, -_shadowSize, -_shadowSize);

            // Also reserve border
            fill = Rectangle.Inflate(fill, -_borderThickness, -_borderThickness);

            // Now apply text padding to place the inner TextBox
            var textRect = new Rectangle(
                fill.X + _textPadding.Left,
                fill.Y + _textPadding.Top,
                Math.Max(0, fill.Width - _textPadding.Horizontal),
                Math.Max(0, fill.Height - _textPadding.Vertical));

            _tb.Location = textRect.Location;
            _tb.Size = new Size(Math.Max(10, textRect.Width), Math.Max(10, textRect.Height));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var outer = ClientRectangle;
            if (outer.Width <= 0 || outer.Height <= 0) return;

            // Fill background transparent (parent shows through)
            using (var clear = new SolidBrush(Parent?.BackColor ?? SystemColors.Control))
                e.Graphics.FillRectangle(clear, outer);

            // Compute rectangles
            var shadowRect = Rectangle.Inflate(outer, -1, -1);   // to avoid clipping edges
            var fillRect = Rectangle.Inflate(outer, -_shadowSize, -_shadowSize);

            // Shadow intensity: lift on hover/focus
            int lift = (Focused || _tb.Focused) ? 10 : (_hovered ? 5 : 0);
            int baseAlpha = Math.Min(255, Math.Max(0, _shadowOpacity + lift));

            DrawShadow(e.Graphics, shadowRect, _cornerRadius, _shadowSize, _shadowColor, baseAlpha);

            // Fill
            using (var gp = RoundedRect(fillRect, _cornerRadius))
            using (var fill = new SolidBrush(_fillColor))
            {
                e.Graphics.FillPath(fill, gp);
            }

            // Border (optional)
            if (_borderThickness > 0)
            {
                var bRect = Rectangle.Inflate(fillRect, -_borderThickness / 2, -_borderThickness / 2);
                using (var gp = RoundedRect(bRect, Math.Max(0, _cornerRadius - _borderThickness / 2)))
                using (var pen = new Pen(_borderColor, _borderThickness))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            }

            // Focus cue (very subtle)
            if (Focused || _tb.Focused)
            {
                var focusRect = Rectangle.Inflate(fillRect, -2, -2);
                using (var gp = RoundedRect(focusRect, Math.Max(0, _cornerRadius - 2)))
                using (var pen = new Pen(Color.FromArgb(30, Color.Black), 1))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Simple soft shadow: draw multiple inflated rounded paths with decreasing alpha
        private static void DrawShadow(Graphics g, Rectangle rect, int radius, int levels, Color color, int baseAlpha)
        {
            if (levels <= 0 || baseAlpha <= 0) return;

            // Clip to control; everything happens inside
            using (var clip = new Region(rect))
            {
                g.SetClip(clip, CombineMode.Replace);

                for (int i = levels; i >= 1; i--)
                {
                    float t = (float)i / levels; // 1..0
                    int a = (int)(baseAlpha * t * 0.6f); // fade out
                    if (a <= 0) continue;

                    var r = Rectangle.Inflate(rect, -i, -i);
                    using (var gp = RoundedRect(r, Math.Max(0, radius - (int)(i * 0.6f))))
                    using (var br = new SolidBrush(Color.FromArgb(a, color)))
                    {
                        g.FillPath(br, gp);
                    }
                }
            }
            g.ResetClip();
        }

        // Soft shadow but only bottom & right
        //private static void DrawShadow(Graphics g, Rectangle rect, int radius, int levels, Color color, int baseAlpha)
        //{
        //    if (levels <= 0 || baseAlpha <= 0) return;

        //    // Clip region: bottom + right half of the rectangle
        //    using (var clip = new Region(new Rectangle(rect.X, rect.Y + rect.Height / 2, rect.Width, rect.Height / 2)))
        //    {
        //        clip.Union(new Rectangle(rect.X + rect.Width / 2, rect.Y, rect.Width / 2, rect.Height));
        //        g.SetClip(clip, CombineMode.Replace);

        //        for (int i = levels; i >= 1; i--)
        //        {
        //            float t = (float)i / levels;
        //            int a = (int)(baseAlpha * t * 0.6f);
        //            if (a <= 0) continue;

        //            var r = Rectangle.Inflate(rect, -i, -i);
        //            using (var gp = RoundedRect(r, Math.Max(0, radius - (int)(i * 0.6f))))
        //            using (var br = new SolidBrush(Color.FromArgb(a, color)))
        //            {
        //                g.FillPath(br, gp);
        //            }
        //        }
        //    }

        //    g.ResetClip();
        //}


        #endregion

        #region Focus/Tab & Forwarders

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            _tb.Focus();
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            Invalidate();
        }

        public new void SelectAll() => _tb.SelectAll();

        public int SelectionStart { get => _tb.SelectionStart; set => _tb.SelectionStart = value; }
        public int SelectionLength { get => _tb.SelectionLength; set => _tb.SelectionLength = value; }
        public int TextLength { get; internal set; }

        protected override bool IsInputKey(Keys keyData) => true;

        // Ensure Tab key moves focus out (standard TextBox behavior)
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                e.Handled = false;
                e.SuppressKeyPress = false;
                base.OnKeyDown(e);
                return;
            }
            base.OnKeyDown(e);
        }

        #endregion

        public void Clear()
        {
            _tb.Clear();
        }

        //public new void Focus()
        //{
        //    _tb.Focus();
        //    _tb.SelectionStart = _tb.Text.Length;

        //}


        //Start This Code For Auto Focus TextBox For Every Form
        private bool _autoFocusOnFormShown = false;

        public new bool Focus()
        {
            if (DesignMode) return base.Focus();

            if (!IsHandleCreated) CreateControl();

            BeginInvoke((Action)(() =>
            {
                if (!_tb.IsDisposed && _tb.Enabled && _tb.Visible)
                {
                    _tb.Focus();
                    _tb.SelectionStart = _tb.TextLength;
                }
            }));

            return true;
        }

        public new void Select()
        {
            Focus();
        }

        [Category("Behavior")]
        [Description("If true, the control will try to focus itself when its parent form is shown.")]
        public bool AutoFocusOnFormShown
        {
            get => _autoFocusOnFormShown;
            set => _autoFocusOnFormShown = value;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            var oldForm = FindForm();
            if (oldForm != null) oldForm.Shown -= ParentForm_Shown;

            var form = FindForm();
            if (form != null)
            {
                form.Shown -= ParentForm_Shown;
                form.Shown += ParentForm_Shown;
            }
        }

        private void ParentForm_Shown(object sender, EventArgs e)
        {
            if (_autoFocusOnFormShown) Focus();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _tb.Focus();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _tb.Focus();
        }

        //End This Code For Auto Focus TextBox For Every Form

    }
}
