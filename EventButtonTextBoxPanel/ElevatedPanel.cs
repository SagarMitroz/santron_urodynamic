using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace SantronWinApp
{
  
    public class ElevatedPanel : Panel
    {
        public int CornerRadius { get; set; } = 8;
        public int ShadowSize { get; set; } = 6;             // offset
        public int ShadowBlur { get; set; } = 12;            // softness
        public Color ShadowColor { get; set; } = Color.FromArgb(80, 0, 0, 0);
        public Color BorderColor { get; set; } = Color.Empty;  // set to Empty for no border
        public int BorderThickness { get; set; } = 0;

        public ElevatedPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(12, 10, 12, 10);
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } // WS_EX_COMPOSITED (less flicker)
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Card rect (leave room for shadow on right/bottom)
            var card = ClientRectangle;
            card.Width -= ShadowSize;
            card.Height -= ShadowSize;

            // ---- shadow ----
            using (var shadowBmp = new Bitmap(Width, Height))
            using (var sg = Graphics.FromImage(shadowBmp))
            {
                sg.SmoothingMode = SmoothingMode.AntiAlias;

                var shadowRect = new Rectangle(card.X + ShadowSize, card.Y + ShadowSize, card.Width, card.Height);
                using (var path = Rounded(card, CornerRadius))
                using (var shadowPath = Rounded(shadowRect, CornerRadius))
                using (var pgb = new PathGradientBrush(shadowPath))
                {
                    pgb.CenterColor = ShadowColor;
                    pgb.SurroundColors = new[] { Color.Transparent };
                    sg.FillPath(pgb, shadowPath);
                }

                // blur-lite pass: draw the shadow image slightly scaled to soften
                g.DrawImage(shadowBmp, 0, 0);
            }

            // ---- card ----
            using (var path = Rounded(card, CornerRadius))
            using (var fill = new SolidBrush(BackColor))
            {
                g.FillPath(fill, path);

                if (BorderThickness > 0 && BorderColor != Color.Empty)
                {
                    using (var pen = new Pen(BorderColor, BorderThickness))
                        g.DrawPath(pen, path);
                }
            }
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
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
    }

}
