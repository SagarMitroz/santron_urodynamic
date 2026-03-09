using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace SantronWinApp
{
    public class GreenButton : Control
    {
        private Padding _textPadding = new Padding(6);
        private Color _backColor = Color.FromArgb(4, 254, 1);
        private Color _foreColor = Color.Black;
        private Color _borderColor = Color.Silver;
        private int _borderThickness = 1;
        private Font _cachedFitFont; 
        private string _lastTextForCache = "";
        private Size _lastSizeForCache = Size.Empty;
        private Padding _lastPaddingForCache = Padding.Empty;
        private Font _lastBaseFontForCache;

        public GreenButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(40, 34);
        }

        [Category("Appearance")]
        [Description("Inner padding reserved around the text.")]
        public Padding TextPadding
        {
            get { return _textPadding; }
            set { _textPadding = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Border color.")]
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Border thickness in pixels.")]
        public int BorderThickness
        {
            get { return _borderThickness; }
            set { _borderThickness = Math.Max(0, value); Invalidate(); }
        }

        public override Color BackColor
        {
            get { return _backColor; }
            set { _backColor = value; Invalidate(); }
        }

        public override Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; Invalidate(); }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            InvalidateFontCache();
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            InvalidateFontCache();
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            InvalidateFontCache();
            Invalidate();
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            InvalidateFontCache();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Invalidate(); // simple hover visual (optional)
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    // Flat look: just fill + border + centered text
        //    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //    e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        //    var rect = ClientRectangle;
        //    using (var bg = new SolidBrush(_backColor))
        //        e.Graphics.FillRectangle(bg, rect);

        //    // Border
        //    if (_borderThickness > 0)
        //    {
        //        using (var pen = new Pen(_borderColor, _borderThickness))
        //        {
        //            // inset the rect so border is fully visible
        //            var bRect = Rectangle.Inflate(rect, -_borderThickness / 2, -_borderThickness / 2);
        //            e.Graphics.DrawRectangle(pen, bRect);
        //        }
        //    }

        //    // Compute text area (respect TextPadding + inside border)
        //    int inset = _borderThickness > 0 ? _borderThickness : 0;
        //    Rectangle textArea = Rectangle.Inflate(rect, -(inset + _textPadding.Left + _textPadding.Right) / 2,
        //                                                -(inset + _textPadding.Top + _textPadding.Bottom) / 2);

        //    // Make sure textArea is valid
        //    textArea = new Rectangle(
        //        rect.Left + inset + _textPadding.Left,
        //        rect.Top + inset + _textPadding.Top,
        //        Math.Max(0, rect.Width - (inset * 2) - _textPadding.Horizontal),
        //        Math.Max(0, rect.Height - (inset * 2) - _textPadding.Vertical)
        //    );

        //    // Fit font
        //    var fitFont = GetBestFitFont(e.Graphics, Text, Font, textArea.Size);

        //    // Draw text centered
        //    TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
        //                            TextFormatFlags.VerticalCenter |
        //                            TextFormatFlags.NoPadding |
        //                            TextFormatFlags.NoPrefix |
        //                            TextFormatFlags.EndEllipsis; // safety, usually won’t ellipsize if fit

        //    Color fore = Enabled ? _foreColor : SystemColors.GrayText;

        //    // Slight hover/focus cue (optional): darken border or draw focus rectangle
        //    if (Focused)
        //    {
        //        ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(textArea, 2, 2));
        //    }

        //    TextRenderer.DrawText(e.Graphics, Text, fitFont, textArea, fore, flags);
        //}


        protected override void OnPaint(PaintEventArgs e)
        {
            // Flat look: just fill + border + centered text
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = ClientRectangle;
            using (var bg = new SolidBrush(_backColor))
                e.Graphics.FillRectangle(bg, rect);

            // Border
            if (_borderThickness > 0)
            {
                using (var pen = new Pen(_borderColor, _borderThickness))
                {
                    // inset the rect so border is fully visible
                    var bRect = Rectangle.Inflate(rect, -_borderThickness / 2, -_borderThickness / 2);
                    e.Graphics.DrawRectangle(pen, bRect);
                }
            }

            // Compute text area (respect TextPadding + inside border)
            int inset = _borderThickness > 0 ? _borderThickness : 0;
            Rectangle textArea = new Rectangle(
                rect.Left + inset + _textPadding.Left,
                rect.Top + inset + _textPadding.Top,
                Math.Max(0, rect.Width - (inset * 2) - _textPadding.Horizontal),
                Math.Max(0, rect.Height - (inset * 2) - _textPadding.Vertical)
            );

            // Draw text centered and wrapped
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                    TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.WordBreak |   // ✅ allow multi-line
                                    TextFormatFlags.NoPrefix;

            Color fore = Enabled ? _foreColor : SystemColors.GrayText;

            if (Focused)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(textArea, 2, 2));
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, textArea, fore, flags);
        }


        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    base.OnPaint(e);
        //    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        //    e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        //    Rectangle rect = ClientRectangle;
        //    rect.Inflate(-_textPadding.Left, -_textPadding.Top);

        //    using (StringFormat sf = new StringFormat())
        //    {
        //        sf.Alignment = StringAlignment.Center;
        //        sf.LineAlignment = StringAlignment.Center;
        //        sf.Trimming = StringTrimming.Word;
        //        sf.FormatFlags = StringFormatFlags.LineLimit; 

        //        using (Brush brush = new SolidBrush(_foreColor))
        //        {
        //            e.Graphics.DrawString(Text, Font, brush, rect, sf);
        //        }
        //    }
        //}


        private void InvalidateFontCache()
        {
            if (_cachedFitFont != null)
            {
                _cachedFitFont.Dispose();
                _cachedFitFont = null;
            }
            _lastTextForCache = "";
            _lastSizeForCache = Size.Empty;
            _lastPaddingForCache = Padding.Empty;
            _lastBaseFontForCache = null;
        }

        private Font GetBestFitFont(Graphics g, string text, Font baseFont, Size target)
        {
            if (string.IsNullOrEmpty(text) || target.Width <= 0 || target.Height <= 0)
                return baseFont;

            // Reuse cached font when possible
            if (_cachedFitFont != null &&
                text == _lastTextForCache &&
                target == _lastSizeForCache &&
                _textPadding.Equals(_lastPaddingForCache) &&
                baseFont.Equals(_lastBaseFontForCache))
            {
                return _cachedFitFont;
            }

            // start with a reasonable range
            float min = 2f;
            // estimate an upper bound based on height
            float max = Math.Max(4f, target.Height * 1.5f);

            // Binary search for the largest size that fits both width & height
            Font testFont = null;
            for (int i = 0; i < 25; i++) // 25 iterations is plenty
            {
                float mid = (min + max) / 2f;
                if (testFont != null) testFont.Dispose();
                testFont = new Font(baseFont.FontFamily, mid, baseFont.Style, GraphicsUnit.Point);

                Size proposed = TextRenderer.MeasureText(g, text, testFont,
                                new Size(int.MaxValue, int.MaxValue),
                                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                if (proposed.Width <= target.Width && proposed.Height <= target.Height)
                {
                    // Fits – try bigger
                    min = mid;
                }
                else
                {
                    // Too big – go smaller
                    max = mid;
                }
            }

            if (_cachedFitFont != null) _cachedFitFont.Dispose();
            _cachedFitFont = new Font(baseFont.FontFamily, min, baseFont.Style, GraphicsUnit.Point);

            _lastTextForCache = text;
            _lastSizeForCache = target;
            _lastPaddingForCache = _textPadding;
            _lastBaseFontForCache = baseFont;

            if (testFont != null) testFont.Dispose();
            return _cachedFitFont;
        }

        // Keyboard/mouse click behavior to act like a button
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
                OnClick(EventArgs.Empty);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                OnClick(EventArgs.Empty);
        }
    }
}
