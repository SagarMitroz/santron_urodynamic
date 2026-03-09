using Org.BouncyCastle.Utilities;
using SantronChart;
using SantronWinApp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static SantronWinApp.TestChannelManager;
using static SantronWinApp.ScaleAndColorModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace SantronReports
{

    internal sealed class GdiContext : IDisposable
    {
        private readonly Graphics _g;
        private Pen _pen;
        private Font _font;
        private Brush _textBrush;
        private StringFormat _stringFormat;
        private Point _cursor;


        //Pratik Sir Print Preview Code

        public GdiContext(Graphics g)
        {
            _g = g ?? throw new ArgumentNullException(nameof(g));

            // Do NOT do: _g.ResetTransform();
            // Do NOT do: _g.ResetClip();
            // Do NOT force PageUnit/PageScale here (leave as-is)

            _g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            _g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            _g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            _g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;


            _pen = new Pen(Color.Black, 1);
            _font = SystemFonts.DefaultFont;
            _textBrush = Brushes.Black;
            _stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoClip
            };

        }

        public void DrawImage(Image image, Rectangle destRect)
        {
            if (image == null) return;
            _g.DrawImage(image, destRect);
        }

        public int HorzRes => (int)_g.VisibleClipBounds.Width;
        public int VertRes => (int)_g.VisibleClipBounds.Height;

        public Size MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text)) text = " ";
            var s = _g.MeasureString(text, _font);
            return new Size((int)Math.Ceiling(s.Width), (int)Math.Ceiling(s.Height));
        }

        #region State

        public void SetPen(Color color, float width = 1, DashStyle dashStyle = DashStyle.Solid)
        {
            _pen?.Dispose();
            _pen = new Pen(color, width) { DashStyle = dashStyle };
        }

        public void SetFont(string familyName, float emSizePoints, FontStyle style = FontStyle.Regular)
        {
            _font?.Dispose();
            _font = new Font(familyName, emSizePoints, style, GraphicsUnit.Point);
        }

        public void SetTextColor(Color c)
        {
            _textBrush = new SolidBrush(c);
        }

        public void SetTextAlign(StringAlignment align)
        {
            _stringFormat.Alignment = align;
        }

        #endregion

        #region Primitives

        public void MoveTo(int x, int y)
        {
            _cursor = new Point(x, y);
        }

        public void LineTo(int x, int y)
        {
            var p2 = new Point(x, y);
            _g.DrawLine(_pen, _cursor, p2);
            _cursor = p2;
        }

        public void Rectangle(Rectangle r)
        {
            _g.DrawRectangle(_pen, r);
        }

        public void FillRectangle(Brush brush, Rectangle r)
        {
            _g.FillRectangle(brush, r);
        }

        // Put these helpers INSIDE GdiContext class (anywhere, e.g. above FillRectangle)
        //private static bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));

        //private static bool IsFinite(RectangleF r) =>
        //    IsFinite(r.X) && IsFinite(r.Y) && IsFinite(r.Width) && IsFinite(r.Height);

        //private static RectangleF IntersectF(RectangleF a, RectangleF b)
        //{
        //    float left = Math.Max(a.Left, b.Left);
        //    float top = Math.Max(a.Top, b.Top);
        //    float right = Math.Min(a.Right, b.Right);
        //    float bottom = Math.Min(a.Bottom, b.Bottom);

        //    if (!IsFinite(left) || !IsFinite(top) || !IsFinite(right) || !IsFinite(bottom))
        //        return RectangleF.Empty;

        //    if (right <= left || bottom <= top)
        //        return RectangleF.Empty;

        //    return RectangleF.FromLTRB(left, top, right, bottom);
        //}

        //// REPLACE your existing FillRectangle with this
        //public void FillRectangle(Brush brush, Rectangle r)
        //{
        //    if (brush == null || _g == null) return;
        //    if (r.Width <= 0 || r.Height <= 0) return;

        //    try
        //    {
        //        // Prefer ClipBounds (more stable than VisibleClipBounds in metafile/preview)
        //        Rectangle clip = System.Drawing.Rectangle.Empty;

        //        try
        //        {
        //            RectangleF cb = _g.ClipBounds;

        //            // Validate clip bounds
        //            bool valid =
        //                !float.IsNaN(cb.X) && !float.IsNaN(cb.Y) &&
        //                !float.IsNaN(cb.Width) && !float.IsNaN(cb.Height) &&
        //                !float.IsInfinity(cb.X) && !float.IsInfinity(cb.Y) &&
        //                !float.IsInfinity(cb.Width) && !float.IsInfinity(cb.Height) &&
        //                cb.Width > 0 && cb.Height > 0;

        //            if (valid)
        //            {
        //                // Convert safely without Rectangle.Round (avoids overflow/invalid rounding)
        //                int left = (int)Math.Floor(cb.Left);
        //                int top = (int)Math.Floor(cb.Top);
        //                int right = (int)Math.Ceiling(cb.Right);
        //                int bottom = (int)Math.Ceiling(cb.Bottom);

        //                // Guard against crazy values (metafile can sometimes report huge bounds)
        //                const int LIM = 200000;
        //                left = Math.Max(-LIM, Math.Min(LIM, left));
        //                top = Math.Max(-LIM, Math.Min(LIM, top));
        //                right = Math.Max(-LIM, Math.Min(LIM, right));
        //                bottom = Math.Max(-LIM, Math.Min(LIM, bottom));

        //                int w = right - left;
        //                int h = bottom - top;

        //                if (w > 0 && h > 0)
        //                    clip = new Rectangle(left, top, w, h);
        //            }
        //        }
        //        catch
        //        {
        //            clip = System.Drawing.Rectangle.Empty;
        //        }

        //        if (!clip.IsEmpty)
        //        {
        //            Rectangle rr = System.Drawing.Rectangle.Intersect(r, clip);
        //            if (rr.Width <= 0 || rr.Height <= 0) return;

        //            _g.FillRectangle(brush, rr);
        //        }
        //        else
        //        {
        //            // No reliable clip -> draw directly (still protected by try/catch)
        //            _g.FillRectangle(brush, r);
        //        }
        //    }
        //    catch (ArgumentException)
        //    {
        //        // swallow drawing failures (print preview/metafile can be strict)
        //    }
        //    catch (System.Runtime.InteropServices.ExternalException)
        //    {
        //        // swallow GDI+ edge cases during print/preview
        //    }
        //}

        //// REPLACE your existing Rectangle(...) with this (same safety)
        //public void Rectangle(Rectangle r)
        //{
        //    if (_g == null) return;
        //    if (r.Width <= 0 || r.Height <= 0) return;

        //    try
        //    {
        //        RectangleF rf = new RectangleF(r.X, r.Y, r.Width, r.Height);
        //        if (!IsFinite(rf)) return;

        //        RectangleF clipF = _g.VisibleClipBounds;

        //        if (!IsFinite(clipF) || clipF.Width <= 0 || clipF.Height <= 0)
        //        {
        //            _g.DrawRectangle(_pen, r);
        //            return;
        //        }

        //        RectangleF rr = IntersectF(rf, clipF);
        //        if (rr.Width <= 0 || rr.Height <= 0) return;

        //        // DrawRectangle has no RectangleF overload; convert safely by flooring/ceiling.
        //        int x = (int)Math.Floor(rr.X);
        //        int y = (int)Math.Floor(rr.Y);
        //        int w = (int)Math.Ceiling(rr.Width);
        //        int h = (int)Math.Ceiling(rr.Height);

        //        if (w <= 0 || h <= 0) return;

        //        _g.DrawRectangle(_pen, new Rectangle(x, y, w, h));
        //    }
        //    catch (Exception ex) when (
        //        ex is ArgumentException ||
        //        ex is System.Runtime.InteropServices.ExternalException ||
        //        ex is OverflowException)
        //    {
        //        // Ignore drawing failures in preview/print pipeline
        //    }
        //}



        //Old Code

        //public void TextOutLeft(int x, int y, string text)
        //{
        //    var saved = _stringFormat.Alignment;
        //    _stringFormat.Alignment = StringAlignment.Near;
        //    _g.DrawString(text ?? string.Empty, _font, _textBrush, x, y, _stringFormat);
        //    _stringFormat.Alignment = saved;
        //}

        //Code Copy text in pdf

        //New Code Add on 02-02-2026 Code For Report Text Copy
        public void TextOutLeft(int x, int y, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var state = _g.Save();

            // Save current transform (contains scaling)
            var transform = _g.Transform;

            // Remove scaling so printer keeps vector text
            _g.ResetTransform();

            // Transform logical coordinates manually
            float drawX = transform.Elements[0] * x + transform.Elements[4];
            float drawY = transform.Elements[3] * y + transform.Elements[5];

            var savedAlign = _stringFormat.Alignment;
            _stringFormat.Alignment = StringAlignment.Near;

            _g.DrawString(text, _font, _textBrush, drawX, drawY, _stringFormat);

            _stringFormat.Alignment = savedAlign;

            _g.Restore(state);
        }



        //public void TextOutRight(int x, int y, string text)
        //{
        //    var saved = _stringFormat.Alignment;
        //    _stringFormat.Alignment = StringAlignment.Far;
        //    _g.DrawString(text ?? string.Empty, _font, _textBrush, x, y, _stringFormat);
        //    _stringFormat.Alignment = saved;
        //}

        //New Code Add on 03-02-2026 Code For Report Text Copy
        public void TextOutRight(int x, int y, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var state = _g.Save();

            // Save current transform (contains scaling)
            var transform = _g.Transform;

            // Remove scaling so printer keeps vector text
            _g.ResetTransform();

            // Transform logical coordinates manually
            float drawX = transform.Elements[0] * x + transform.Elements[4];
            float drawY = transform.Elements[3] * y + transform.Elements[5];

            var savedAlign = _stringFormat.Alignment;
            _stringFormat.Alignment = StringAlignment.Far;

            _g.DrawString(text, _font, _textBrush, drawX, drawY, _stringFormat);

            _stringFormat.Alignment = savedAlign;

            _g.Restore(state);
        }

        public void DrawTextBlock(string text, Rectangle rect, bool wordBreak = true)
        {
            var sf = (StringFormat)_stringFormat.Clone();
            if (wordBreak)
            {
                sf.FormatFlags &= ~StringFormatFlags.NoWrap;
            }
            _g.DrawString(text ?? string.Empty, _font, _textBrush, rect, sf);
        }

        #endregion

        public void Dispose()
        {
            _pen?.Dispose();
            _font?.Dispose();
            if (!ReferenceEquals(_textBrush, Brushes.Black) &&
                !ReferenceEquals(_textBrush, Brushes.Blue) &&
                !ReferenceEquals(_textBrush, Brushes.Red))
            {
                _textBrush?.Dispose();
            }
            _stringFormat?.Dispose();
        }
    }

    public sealed class LegacyUroReportPrinter
    {
        private readonly ReportDataPrint _data;
        private readonly MultiChannelLiveChart _chart;
        private readonly ReportGraphConfig _graph;

     

        private int _totalPages = 0;

        private int _currentStaticIndex = 0;
        private bool _inImageMode = false;

        // ===================== PATCH: Dynamic page numbering =====================
        private int _printPageCounter = 0;          // 0-based counter across the *actual* printed pages
        private bool _printStateInitialized = false;

        // Total pages must include expanded image pages (page #5)
        private int ComputeTotalPagesForCurrentJob()
        {
            int basePages = (_pagesToPrint != null && _pagesToPrint.Count > 0) ? _pagesToPrint.Count : 1;

            bool hasImagesPage = (_pagesToPrint != null && _pagesToPrint.Contains(5));
            if (!hasImagesPage)
                return basePages;

            // If "page 5" is part of the flow, it may expand to multiple physical pages.
            // Count it as at least 1 page even if there are 0 images.
            int imagePages = Math.Max(1, _totalImagePages);

            // Replace the single placeholder "5" with actual imagePages
            return basePages - 1 + imagePages;
        }
        // =================== END PATCH: Dynamic page numbering ===================


        public LegacyUroReportPrinter(ReportDataPrint data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

       

        private List<int> _pagesToPrint;

        public LegacyUroReportPrinter(ReportDataPrint data, ReportGraphConfig graph)
        {
            _data = data;
            _graph = graph;

            _pagesToPrint = ResolvePagesForTest(_data.TestName);

            if (_data.CapturedImages != null && _data.CapturedImages.Count > 0)
            {
                _totalImagePages =
                    (_data.CapturedImages.Count + PerPageImages - 1) / PerPageImages;
            }
        }

        public sealed class ReportSample
        {
            public double T;
            public double[] Values;
        }
        public sealed class ReportMarker
        {
            public double T { get; set; }      // seconds from start
            public string Label { get; set; }  // "FS", "FD", "BC", "C1", etc.
        }
        public sealed class ReportGraphConfig
        {
            public List<ReportSample> Samples;
            public TestDefinition TestDef;
            public int[] ActiveIndices;
            public List<ReportMarker> Markers { get; set; }
        }

        private sealed class MarkerTableCol
        {
            public string Header;
            public string Unit;
            public Func<MarkerRow, string> Value;
            public float Weight;
            public bool BlueBold;
        }



        private List<int> ResolvePagesForTest(string testName)
        {
            string t = (testName ?? "").ToLowerInvariant();
            // 🔴 MOST SPECIFIC FIRST
            // 5–6 PAGES – Pressure Flow + EMG + Video (images before conclusion)
            if (t.Contains("pressure flow") && t.Contains("emg") && t.Contains("video"))
                return new List<int> { 1, 2, 3, 5, 4 };
            // 5–6 pages – Pressure Flow + Video (no EMG) (images before conclusion)
            if (t.Contains("pressure flow") && t.Contains("video"))
                return new List<int> { 1, 2, 3, 5, 4 };
            // 4 pages – Pressure Flow (+ EMG or without)
            if (t.Contains("pressure flow"))
                return new List<int> { 1, 2, 3, 4 };
            // 3 pages – Cystometry
            if (t.Contains("cystometry"))
                return new List<int> { 1, 3, 4 };
            // 1 page – UPP / Whitaker
            if (t.Contains("upp") || t.Contains("whitaker"))
                return new List<int> { 8 };
            // 1 page – Uroflowmetry (+ EMG or without)
            if (t.Contains("uroflowmetry"))
                return new List<int> { 7 };
            // fallback
            return new List<int> { 1 };
        }

        public void PrintPage(object sender, PrintPageEventArgs e, int pageIndex)
        {

            // Detect preview EARLY (PrintPreview relies heavily on DefaultPageSettings)
            var pd = sender as PrintDocument;
            bool isPreview = pd != null && pd.PrintController is PreviewPrintController;

            // ---------------------------------------------------------------------
            // FORCE A4 + deterministic margins for BOTH: e.PageSettings + DefaultPageSettings
            // This is the key to make PrintPreview match actual print.
            // ---------------------------------------------------------------------
            try
            {
                // A4 in hundredths of an inch: 8.27" × 11.69" => 827 × 1169
                var a4 = new PaperSize("A4", 827, 1169) { RawKind = (int)PaperKind.A4 };

                // Choose margins once (hundredths of an inch). Tune if needed.
                var fixedMargins = new Margins(10, 10, 10, 10);

                if (pd != null)
                {
                    // Preview uses DefaultPageSettings to size the EMF pages
                    pd.DefaultPageSettings.PaperSize = a4;
                    pd.DefaultPageSettings.Margins = fixedMargins;

                    // Makes MarginBounds consistent across preview/print
                    pd.OriginAtMargins = true;
                }

                if (e.PageSettings != null)
                {
                    e.PageSettings.PaperSize = a4;
                    e.PageSettings.Margins = fixedMargins;
                }
            }
            catch
            {
                // Ignore driver exceptions; continue with whatever the driver provides.
            }


            Rectangle target = e.MarginBounds;
            // ===================== PATCH: init per print job =====================
            if (!_printStateInitialized || (pageIndex == 0 && _printPageCounter != 0))
            {
                _currentStaticIndex = 0;
                _imagePageIndex = 0;
                _inImageMode = false;

                _printPageCounter = 0;
                _totalPages = ComputeTotalPagesForCurrentJob();
                _printStateInitialized = true;
            }
            // =================== END PATCH: init per print job ===================
            if (_totalPages <= 0)
            {
                int pages = 0;
                pages++; // Filling
                pages++; // Voiding
                pages++;
                pages++;
                pages++;
                pages++;
                _totalPages = pages;
            }



            // Refresh target after forcing margins/paper
            target = e.MarginBounds;


            // 1️⃣ Logical page size (design once)
            const int LOGICAL_W = 1000;
            const int LOGICAL_H = 1414;
            float sx = (float)target.Width / LOGICAL_W;
            float sy = (float)target.Height / LOGICAL_H;
            Graphics g = e.Graphics;
            var state = g.Save();
            // Detect preview
            //  bool isPreview = sender is PrintDocument pd &&
            //                  pd.PrintController is PreviewPrintController;
            // 2️⃣ Quality settings
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = isPreview
                ? TextRenderingHint.ClearTypeGridFit
                : TextRenderingHint.SingleBitPerPixelGridFit;
            // 3️⃣ Clip & transform ONCE
            // 3️⃣ Transform first, then clip in LOGICAL coordinates (fixes PrintPreview metafile issues)
            g.TranslateTransform(target.Left, target.Top);
            g.ScaleTransform(sx, sy); // responsive fill

            // Replace clip AFTER transform so ClipBounds/VisibleClipBounds stay sane in logical space
            g.ResetClip();
            g.SetClip(new Rectangle(0, 0, LOGICAL_W, LOGICAL_H));

            int leftMargin = 50;
            int rightMargin = 20;
            int topMargin = 10;
            int bottomMargin = 100;
            Rectangle logicalRect = new Rectangle(
                leftMargin,
                topMargin,
                LOGICAL_W - leftMargin - rightMargin,
                LOGICAL_H - topMargin - bottomMargin
            );
            using (var ctx = new GdiContext(g))
            {
                // ===================== PATCH: current page for footer =====================
                int currentPrintedPage = _printPageCounter + 1; // 1-based for display
                                                                // =================== END PATCH: current page for footer ===================
                                                                // Determine pageNumber based on mode
                int pageNumber;
                if (_inImageMode)
                {
                    pageNumber = 5;
                }
                else
                {
                    pageNumber = _pagesToPrint[_currentStaticIndex];
                }

                switch (pageNumber)
                {
                    case 1:
                        DrawFillingPhasePage(ctx, g, logicalRect);
                        break;
                    case 2:
                        ResetVoidingCache();
                        DrawVoidingPhasePage(ctx, g, logicalRect);
                        break;
                    case 3:
                        DrawMarkerPage(ctx, g, logicalRect);
                        break;
                    case 4:
                        DrawConclusionPage(ctx, g, logicalRect);
                        break;
                    case 5:
                        DrawCapturedImagesPaged(ctx, g, logicalRect, e);
                        break;
                    case 7:
                        DrawUroflowmetryTestPage(ctx, g, logicalRect);
                        break;
                    case 8:
                        DrawUPPTestPage(ctx, g, logicalRect);
                        break;
                }

                // 5️⃣ Page continuation logic (updated for multi-page images in middle)
                if (pageNumber == 5)
                {
                    bool moreImagePages = _imagePageIndex < _totalImagePages - 1;
                    if (moreImagePages)
                    {
                        _imagePageIndex++;
                        _inImageMode = true;

                        // Next physical page
                        _printPageCounter++;

                        e.HasMorePages = true;
                        g.Restore(state);
                        return;
                    }
                    else
                    {
                        if (pageNumber == 5)  // Reset as in original
                            _imagePageIndex = 0;
                        _inImageMode = false;
                        // Fall through to advance static index
                    }
                }

                // Advance to next static page if not in image mode
                _currentStaticIndex++;

                // Next physical page
                _printPageCounter++;

                e.HasMorePages = _currentStaticIndex < _pagesToPrint.Count;

                // If this was the last page, allow next preview/print to re-init cleanly
                if (!e.HasMorePages)
                    _printStateInitialized = false;

            }
            g.Restore(state);
        }


        //Start this code For PDF Download show all page same 

        //public void BeginPrint(object sender, PrintEventArgs e)
        //{
        //    var pd = sender as PrintDocument;
        //    if (pd == null) return;

        //    var a4 = new PaperSize("A4", 827, 1169)
        //    {
        //        RawKind = (int)PaperKind.A4
        //    };

        //    var fixedMargins = new Margins(10, 10, 10, 10);

        //    pd.DefaultPageSettings.PaperSize = a4;
        //    pd.DefaultPageSettings.Margins = fixedMargins;
        //    pd.OriginAtMargins = false;

        //    _printStateInitialized = false;
        //}


        //public void PrintPage(object sender, PrintPageEventArgs e, int pageIndex)
        //{
        //    var pd = sender as PrintDocument;
        //    bool isPreview = pd?.PrintController is PreviewPrintController;

        //    // ==================== FORCE A4 CONSISTENTLY ====================
        //    //try
        //    //{
        //    //    var a4 = new PaperSize("A4", 827, 1169)
        //    //    {
        //    //        RawKind = (int)PaperKind.A4
        //    //    };

        //    //    var fixedMargins = new Margins(10, 10, 10, 10);

        //    //    if (pd != null)
        //    //    {
        //    //        pd.DefaultPageSettings.PaperSize = a4;
        //    //        pd.DefaultPageSettings.Margins = fixedMargins;
        //    //        pd.OriginAtMargins = false; // 🔴 IMPORTANT FIX
        //    //    }

        //    //    e.PageSettings.PaperSize = a4;
        //    //    e.PageSettings.Margins = fixedMargins;
        //    //}
        //    //catch { }

        //    Rectangle target = e.MarginBounds;

        //    // ================= INIT PRINT STATE (UNCHANGED) =================
        //    if (!_printStateInitialized || (pageIndex == 0 && _printPageCounter != 0))
        //    {
        //        _currentStaticIndex = 0;
        //        _imagePageIndex = 0;
        //        _inImageMode = false;

        //        _printPageCounter = 0;
        //        _totalPages = ComputeTotalPagesForCurrentJob();
        //        _printStateInitialized = true;
        //    }

        //    if (_totalPages <= 0)
        //        _totalPages = 6;

        //    // ================= LOGICAL COORDINATE SYSTEM =================
        //    const int LOGICAL_W = 1000;
        //    const int LOGICAL_H = 1414;

        //    Graphics g = e.Graphics;
        //    var state = g.Save();

        //    // 🔴 RESET EVERYTHING — CRITICAL
        //    g.ResetTransform();
        //    g.PageUnit = GraphicsUnit.Display;

        //    g.SmoothingMode = SmoothingMode.AntiAlias;
        //    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        //    g.TextRenderingHint = isPreview
        //        ? TextRenderingHint.ClearTypeGridFit
        //        : TextRenderingHint.SingleBitPerPixelGridFit;

        //    // ================= STABLE SCALE =================
        //    float sx = target.Width / (float)LOGICAL_W;
        //    float sy = target.Height / (float)LOGICAL_H;
        //    //float scale = Math.Min(sx, sy); // 🔴 prevents stretch drift

        //    g.TranslateTransform(target.Left, target.Top);
        //    //g.ScaleTransform(scale, scale);
        //    g.ScaleTransform(sx, sy);

        //    // 🔴 CLIP IN LOGICAL SPACE
        //    g.SetClip(new Rectangle(0, 0, LOGICAL_W, LOGICAL_H));

        //    // ================= LOGICAL MARGINS =================
        //    Rectangle logicalRect = new Rectangle(
        //        50,    // left
        //        10,    // top
        //        LOGICAL_W - 70,
        //        LOGICAL_H - 110
        //    );

        //    using (var ctx = new GdiContext(g))
        //    {
        //        int pageNumber = _inImageMode
        //            ? 5
        //            : _pagesToPrint[_currentStaticIndex];

        //        switch (pageNumber)
        //        {
        //            case 1: DrawFillingPhasePage(ctx, g, logicalRect); break;
        //            case 2: ResetVoidingCache(); DrawVoidingPhasePage(ctx, g, logicalRect); break;
        //            case 3: DrawMarkerPage(ctx, g, logicalRect); break;
        //            case 4: DrawConclusionPage(ctx, g, logicalRect); break;
        //            case 5: DrawCapturedImagesPaged(ctx, g, logicalRect, e); break;
        //            case 7: DrawUroflowmetryTestPage(ctx, g, logicalRect); break;
        //            case 8: DrawUPPTestPage(ctx, g, logicalRect); break;
        //        }

        //        // ================= PAGING LOGIC (UNCHANGED) =================
        //        if (pageNumber == 5)
        //        {
        //            if (_imagePageIndex < _totalImagePages - 1)
        //            {
        //                _imagePageIndex++;
        //                _inImageMode = true;
        //                _printPageCounter++;
        //                e.HasMorePages = true;
        //                g.Restore(state);
        //                return;
        //            }

        //            _imagePageIndex = 0;
        //            _inImageMode = false;
        //        }

        //        _currentStaticIndex++;
        //        _printPageCounter++;
        //        e.HasMorePages = _currentStaticIndex < _pagesToPrint.Count;

        //        if (!e.HasMorePages)
        //            _printStateInitialized = false;
        //    }

        //    g.Restore(state);
        //}

        //End this code For PDF Download show all page same 









        //Start Code For Show images on report print and Image Marker Details add on 09-01-2026 

        //const int ImageWidth = 850;
        //const int ImageHeight = 625;
        //const int RowGap = 160;
        //const int TableStartGap = 100;
        const int ImageWidth = 180;        // logical units
        const int ImageHeight = 150;
        const int RowGap = 40;
        const int TableStartGap = 40;

        //private int _imagePageIndex = 0;
        //private const int PerPageImages = 4;
        //private int _totalImagePages = 0;

        private int DrawMarkerHeader(GdiContext ctx, int left, int top, Size fSize)
        {
            int colGap = 17;

            int c0 = left;                 // Marker
            int c1 = left + colGap * 15;    // Time
            int c2 = left + colGap * 20;   // Pves
            int c3 = left + colGap * 25;   // Pabd
            int c4 = left + colGap * 30;   // Pdet
            int c5 = left + colGap * 35;   // Vinf
            int c6 = left + colGap * 40;   // Qvol
            int c7 = left + colGap * 45;   // Qura
            int c8 = left + colGap * 50;   // EMG

            int r = 2;

            // -----------------------------
            // Title
            // -----------------------------
            //ctx.SetFont(_data.FontFamily, 12f, FontStyle.Bold);
            //ctx.SetTextColor(Color.Red);
            //ctx.TextOutLeft(left, top + fSize.Height * r, "Markers Details");
            //r += 2;

            // =====================================================
            // 🔷 GRADIENT HEADER : "Markers Details"
            // =====================================================
            int rowH = fSize.Height + 8;
            int titleHeight = rowH + 6;

            int headerLeft = left - 4;
            int headerTop = top + fSize.Height * r - 30;

            // header width based on last column
            int headerWidth = (c8 + colGap * 4) - headerLeft;

            // remove any active pen to avoid border lines
            ctx.SetPen(Color.Transparent, 0);

            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,          // left
                    Color.FromArgb(210, 225, 240),
                    //Color.RoyalBlue,      // right
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
                );
            }

            ctx.SetFont(_data.FontFamily, 12f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                headerLeft + 12,
                headerTop + (titleHeight - fSize.Height) / 2,
                "Marker Details"
            );

            // move rows below header
            r += 2;
            top += titleHeight + fSize.Height;
            r = 1;
            // =====================================================

            // -----------------------------
            // Header row
            // -----------------------------
            ctx.SetFont(_data.FontFamily, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(c0, top + fSize.Height * r, "Marker");
            ctx.TextOutLeft(c1, top + fSize.Height * r, "Time");
            ctx.TextOutLeft(c2, top + fSize.Height * r, "Pves");
            ctx.TextOutLeft(c3, top + fSize.Height * r, "Pabd");
            ctx.TextOutLeft(c4, top + fSize.Height * r, "Pdet");
            ctx.TextOutLeft(c5, top + fSize.Height * r, "Vinf");
            ctx.TextOutLeft(c6, top + fSize.Height * r, "Qvol");
            ctx.TextOutLeft(c7, top + fSize.Height * r, "Qura");
            ctx.TextOutLeft(c8, top + fSize.Height * r, "EMG");
            r++;

            // -----------------------------
            // Units row
            // -----------------------------
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);

            ctx.TextOutLeft(c0, top + fSize.Height * r, "Type");
            ctx.TextOutLeft(c1, top + fSize.Height * r, "mins.");
            ctx.TextOutLeft(c2, top + fSize.Height * r, "cmH2O");
            ctx.TextOutLeft(c3, top + fSize.Height * r, "cmH2O");
            ctx.TextOutLeft(c4, top + fSize.Height * r, "cmH2O");
            ctx.TextOutLeft(c5, top + fSize.Height * r, "ml");
            ctx.TextOutLeft(c6, top + fSize.Height * r, "ml");
            ctx.TextOutLeft(c7, top + fSize.Height * r, "ml/sec");
            ctx.TextOutLeft(c8, top + fSize.Height * r, "uV");
            r += 2;

            return top + fSize.Height * r;
        }


        private void DrawImageMarkerRow(GdiContext ctx, int left, int top, MarkerRow m)
        {
            int colGap = 17;

            //int c0 = left;
            int c1 = left + colGap * 2;
            int c2 = left + colGap * 7;
            int c3 = left + colGap * 12;
            int c4 = left + colGap * 17;
            int c5 = left + colGap * 22;
            int c6 = left + colGap * 27;
            int c7 = left + colGap * 32;
            int c8 = left + colGap * 37;

            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);

            //ctx.TextOutLeft(c0, top, m.Type);             // I-1 / I-2 / I-3
            ctx.TextOutLeft(c1, top, m.TimeText);
            ctx.TextOutLeft(c2, top, m.Pves.ToString("F0"));
            ctx.TextOutLeft(c3, top, m.Pabd.ToString("F0"));
            ctx.TextOutLeft(c4, top, m.Pdet.ToString("F0"));
            ctx.TextOutLeft(c5, top, m.Vinf.ToString("F0"));
            ctx.TextOutLeft(c6, top, m.Qvol.ToString("F0"));
            ctx.TextOutLeft(c7, top, m.Qura.ToString("F0"));
            ctx.TextOutLeft(c8, top, m.EMG.ToString("F0"));
        }







        //Start Pratik Sir code

        private const double PreSec = 5.0;          // show 3 sec before capture
        private const double PostSec = 5.0;         // show 3 sec after capture

        private const int GridCols = 1;
        private const int GridRows = 3;
        private const int ItemsPerPage = GridCols * GridRows;   // 3

        private const int CellGap = 12;       // gap between cells
        private const int RowSeparatorGap = 30;

        private const int CellPad = 8;
        private const int ImageGraphGap = 15;

        // Cell internal split: left image, right graph
        private const float ImageBlockRatio = 0.55f;  // 52% image, 48% graph (tune)
        private const int ImageLabelH = 16;           // height reserved for “Image I-n”
        private const int GraphMinH = 300;            // increase graph height (tune)

        private const int CellTopSpace = 110;     // 👈 adds top space inside each cell
        private const int ImageHeightReduce = 5; // 👈 reduces image height
        private const int GraphHeightReduce = 5; // 👈 reduces graph height

        private int _imagePageIndex = 0;
        private const int PerPageImages = 3;
        private int _totalImagePages = 0;



        private void DrawCapturedImagesPaged(GdiContext ctx, Graphics g, Rectangle rect, PrintPageEventArgs e)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            //DrawHeaderBlock(ctx, rect, fSize);
            DrawPatientShortDetails(ctx, g, rect, fSize);
            //DrawPatientBlock(ctx, g, rect, fSize);

            if (_data.CapturedImages == null || _data.CapturedImages.Count == 0)
            {
                e.HasMorePages = false;
                return;
            }

            // content area start (below patient block)
            int margin = 10;
            int top = rect.Top + fSize.Height * 4;
            int left = rect.Left + margin;
            int right = rect.Right - margin;
            int bottom = rect.Bottom - 140; // reserve footer

            int contentW = right - left;
            int contentH = bottom - top;

            // Calculate cell dimensions for single column layout
            int cellW = contentW - CellGap * 2;  // Full width minus margins
            int cellH = (contentH - (GridRows - 1) * RowSeparatorGap) / GridRows;

            int startIndex = _imagePageIndex * ItemsPerPage;
            int endIndex = Math.Min(startIndex + ItemsPerPage, _data.CapturedImages.Count);

 
            for (int idx = startIndex; idx < endIndex; idx++)
            {
                int local = idx - startIndex;        // 0,1,2
                int row = local;                       // 0,1,2 (since GridCols = 1)
                int col = 0;                            // always column 0

                int cellLeft = left + CellGap;
                int cellTop = top + row * (cellH + RowSeparatorGap);

                Rectangle cellRect = new Rectangle(cellLeft, cellTop, cellW, cellH);
                DrawImageGraphCell(ctx, g, cellRect, fSize, idx);
            }


            // separator line between two rows
            //int sepY = top + cellH + CellGap + (RowSeparatorGap / 2);
            //g.DrawLine(Pens.Gray, left, sepY, right, sepY);

            // Footer 
            DrawFooter(ctx, g, rect, fSize, _imagePageIndex, _totalPages);

        }

       
        private void DrawImageGraphCell(GdiContext ctx, Graphics g, Rectangle cell, Size fSize, int imageIndex)
        {
            string font = _data.FontFamily;

            Rectangle inner = new Rectangle(
                cell.Left + CellPad,
                cell.Top + CellPad + CellTopSpace,
                cell.Width - CellPad * 2,
                cell.Height - CellPad * 2 - CellTopSpace
            );

            if (inner.Width < 50 || inner.Height < 50) return;

            // Left (image) block and right (graph) block
            int imgBlockW = (int)(inner.Width * ImageBlockRatio);
            int graphBlockW = inner.Width - imgBlockW - ImageGraphGap;
            if (graphBlockW < 70) graphBlockW = 70; // Reduced minimum graph width from 80 to 70

            Rectangle imgBlock = new Rectangle(inner.Left, inner.Top, imgBlockW, inner.Height);

            // Calculate image rectangle with increased width
            int imgHeight = Math.Max(
                150,  // Minimum image height
                imgBlock.Height - ImageLabelH - ImageHeightReduce + 100
            );

            Rectangle imgRect = new Rectangle(
                imgBlock.Left,
                imgBlock.Top,
                imgBlock.Width,  // Now wider due to increased ImageBlockRatio
                imgHeight
            );

            // Graph block - now narrower
            Rectangle graphBlock = new Rectangle(
                imgBlock.Right + ImageGraphGap,
                inner.Top,
                graphBlockW,
                Math.Max(
                    GraphMinH,
                    inner.Height - GraphHeightReduce
                )
            );

            Rectangle imgLabelRect = new Rectangle(
                imgBlock.Left,
                imgRect.Bottom,
                imgBlock.Width,
                ImageLabelH
            );

            // Draw image frame + image
            using (Pen grayPen = new Pen(Color.Gray, 1))
            {
                g.DrawRectangle(grayPen, imgRect);
            }

            if (_data.CapturedImages != null && imageIndex < _data.CapturedImages.Count)
            {
                g.DrawImage(_data.CapturedImages[imageIndex], imgRect);
            }

            // Label
            string markerLabel = $"I-{imageIndex + 1}";
            string imgLabel = $"Image {markerLabel}";
            ctx.SetFont(font, 9f);

            int labelX = imgLabelRect.Left + (imgLabelRect.Width - ctx.MeasureText(imgLabel).Width) / 2;
            int labelY = imgLabelRect.Top + 2;
            ctx.TextOutLeft(labelX, labelY, imgLabel);

            // Resolve capture time
            double captureSec = -1;
            var markerRow = GetImageMarkerRow(markerLabel);
            if (markerRow != null && TryParseMmSs(markerRow.TimeText, out double secFromText))
                captureSec = secFromText;

            // Fallback: from graph markers if available
            if (captureSec < 0)
            {
                var m = _graph?.Markers?.FirstOrDefault(x =>
                    string.Equals(x.Label, markerLabel, StringComparison.OrdinalIgnoreCase));
                if (m != null) captureSec = m.T;
            }

            // Draw graph on the RIGHT
            using (Pen grayPen = new Pen(Color.Gray, 1))
            {
                g.DrawRectangle(grayPen, graphBlock);
            }

            if (graphBlock.Height < GraphMinH)
            {
                graphBlock = new Rectangle(graphBlock.Left, graphBlock.Top, graphBlock.Width, GraphMinH);
            }

            if (captureSec >= 0 && _graph?.Samples != null && _graph.Samples.Count > 1 && graphBlock.Height > 40)
            {
                DrawClippedGraphSameAsPage1(ctx, g, graphBlock, fSize, captureSec, markerLabel);
            }
        }

       


        private void DrawClippedGraphSameAsPage1(GdiContext ctx, Graphics g, Rectangle outerRect, Size fSize, double captureSec, string markerLabel)
        {
            // Clip window
            double clipStart = captureSec - PreSec;
            double clipEnd = captureSec + PostSec;

            // Clamp to sample range
            double startAll = _graph.Samples[0].T;
            double endAll = _graph.Samples[_graph.Samples.Count - 1].T;
            if (clipStart < startAll) clipStart = startAll;
            if (clipEnd > endAll) clipEnd = endAll;
            if (clipEnd <= clipStart) clipEnd = clipStart + 1.0;

            // Mini layout: smaller label area than page-1 (so it looks clean)
            int leftAxisW = Math.Max(fSize.Width * 5, outerRect.Width / 3);

            Rectangle labelRect = new Rectangle(outerRect.Left, outerRect.Top, leftAxisW, outerRect.Height);
            Rectangle gRect = new Rectangle(labelRect.Right, outerRect.Top, outerRect.Right - labelRect.Right, outerRect.Height);

            // Reserve a small bottom space for time axis
            //int bottomAxis = Math.Min(fSize.Height * 2, outerRect.Height / 5);
            //Rectangle curvesRect = new Rectangle(gRect.Left, gRect.Top, gRect.Width, Math.Max(gRect.Height - bottomAxis, fSize.Height * 2));
            Rectangle curvesRect = gRect;

            // Draw axes exactly like page-1
            DrawFillingAxes(ctx, g, labelRect, curvesRect, fSize);

            // Draw curves from recorded (same as page-1), clipped
            Region oldClip = g.Clip;
            g.SetClip(curvesRect);

            DrawFillingCurvesFromRecorded(g, curvesRect, clipStart, clipEnd);

            // Draw ONLY marker line
            //DrawSingleMarkerLineOnly(g, curvesRect, markerLabel, captureSec, clipStart, clipEnd);
            DrawMarkerLineWithValues(g, curvesRect, markerLabel, captureSec, clipStart, clipEnd);

            g.SetClip(oldClip, CombineMode.Replace);

            // This Line For Show Starting Scale & Ending Scale Time in Bottom (Bottom time axis)
            //DrawTimeAxisBottom(g, gRect, clipStart, clipEnd);

            // Draw marker time at the bottom of the graph
            DrawMarkerTimeAtBottom(g, gRect, markerLabel, captureSec, clipStart, clipEnd);
        }

        private void DrawMarkerLineWithValues(Graphics g, Rectangle plotRect, string label, double markerSec, double clipStart, double clipEnd)
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return;

            double duration = clipEnd - clipStart;
            if (duration <= 0.0) duration = 1.0;

            double t = Math.Max(clipStart, Math.Min(clipEnd, markerSec));
            float x = (float)(plotRect.Left + (t - clipStart) / duration * plotRect.Width);

            // Find the sample closest to the marker time
            var sample = _graph.Samples
                .OrderBy(s => Math.Abs(s.T - markerSec))
                .FirstOrDefault();

            if (sample?.Values == null)
                return;

            int laneCount = _graph.TestDef?.Lanes?.Count ?? 0;
            if (laneCount == 0)
                return;

            float laneHeight = (float)plotRect.Height / laneCount;

            // Draw vertical marker line
            using (Pen pen = new Pen(Color.DarkRed, 1.5f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pen, x, plotRect.Top, x, plotRect.Bottom - 1);
            }

            // Draw marker label at the top
            if (!string.IsNullOrWhiteSpace(label))
            {
                using (Font font = new Font("Arial", 8f, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.DarkRed))
                {
                    string lbl = label.Trim().ToUpperInvariant();
                    SizeF sz = g.MeasureString(lbl, font);

                    //float textX = x - sz.Width / 2;
                    //textX = Math.Max(plotRect.Left + 2, Math.Min(textX, plotRect.Right - sz.Width - 2));

                    // Position label to the LEFT of the marker line
                    float textX = x - sz.Width - 6;  // 6px gap from the marker line

                    // Ensure label stays within graph bounds
                    if (textX < plotRect.Left + 2)
                        textX = plotRect.Left + 2;

                    g.DrawString(lbl, font, brush, textX, plotRect.Top + 4);
                }
            }

            // Draw values for each channel/lane
            using (Font valueFont = new Font("Arial", 7f, FontStyle.Regular))
            {
                // Get lane indices for different channels
                int idxPves = FindLaneIndex("pves");
                int idxPabd = FindLaneIndex("pabd");
                int idxPdet = FindLaneIndex("pdet");
                int idxVinf = FindLaneIndex("vinf");
                int idxQvol = FindLaneIndex("qvol");
                int idxQura = FindLaneIndex("qura");
                int idxEmg = FindLaneIndex("emg");

                for (int lane = 0; lane < laneCount; lane++)
                {
                    if (lane >= sample.Values.Length)
                        continue;

                    double value = sample.Values[lane];
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        continue;

                    string valueText = Math.Round(value).ToString();

                    // Calculate Y position for this lane (center of lane)
                    float laneTop = plotRect.Top + lane * laneHeight;
                    float laneBottom = laneTop + laneHeight;
                    float y = laneTop + (laneHeight / 2f) - 4;

                    // Position text to the right of the marker line
                    float tx = x + 5;

                    // If too close to right edge, position to the left
                    if (tx + 30 > plotRect.Right)
                        tx = x - 25;

                    // Get lane color for text
                    Color laneColor = Color.Black;
                    if (_graph.TestDef?.Lanes != null && lane < _graph.TestDef.Lanes.Count)
                    {
                        laneColor = _graph.TestDef.Lanes[lane].Color;
                    }

                    using (Brush laneBrush = new SolidBrush(laneColor))
                    {
                        g.DrawString(valueText, valueFont, laneBrush, tx, y);
                    }
                }
            }
        }

        private void DrawMarkerTimeAtBottom(Graphics g, Rectangle gRect, string label, double markerSec, double clipStart, double clipEnd)
        {
            double duration = clipEnd - clipStart;
            if (duration <= 0.0) duration = 1.0;

            double t = Math.Max(clipStart, Math.Min(clipEnd, markerSec));
            float x = (float)(gRect.Left + (t - clipStart) / duration * gRect.Width);

            // Format marker time as MM:SS
            string timeText = FormatMarkerTime(markerSec);

            using (Font font = new Font("Arial", 7f, FontStyle.Regular))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                SizeF tsz = g.MeasureString(timeText, font);

                float timeX = x - (tsz.Width / 2f);
                float timeY = gRect.Bottom + 5;

                // Ensure text stays within graph bounds
                timeX = Math.Max(gRect.Left, Math.Min(timeX, gRect.Right - tsz.Width));

                // Draw time text
                g.DrawString(timeText, font, brush, timeX, timeY);

                // Optionally draw a small tick mark at the marker position
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    g.DrawLine(pen, x, gRect.Bottom - 2, x, gRect.Bottom + 2);
                }
            }
        }


        private void DrawSingleMarkerLineOnly(Graphics g, Rectangle plotRect, string label, double markerSec, double clipStart, double clipEnd)
        {
            double duration = clipEnd - clipStart;
            if (duration <= 0.0) duration = 1.0;

            double t = Math.Max(clipStart, Math.Min(clipEnd, markerSec));
            float x = (float)(plotRect.Left + (t - clipStart) / duration * plotRect.Width);

            using (Font font = new Font("Arial", 7f, FontStyle.Bold))
            using (Pen pen = new Pen(Color.DarkRed, 1.2f) { DashStyle = DashStyle.Dash })
            using (Brush brush = new SolidBrush(Color.DarkRed))
            {
                g.DrawLine(pen, x, plotRect.Top, x, plotRect.Bottom - 1);

                if (!string.IsNullOrWhiteSpace(label))
                {
                    string lbl = label.Trim().ToUpperInvariant();
                    SizeF sz = g.MeasureString(lbl, font);

                    float textX = x - sz.Width / 2;
                    textX = Math.Max(plotRect.Left + 2, Math.Min(textX, plotRect.Right - sz.Width - 2));

                    g.DrawString(lbl, font, brush, textX, plotRect.Top + 4);
                }
            }
        }

        private static bool TryParseMmSs(string s, out double sec)
        {
            sec = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Trim().Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int mm)) return false;
            if (!int.TryParse(parts[1], out int ss)) return false;
            sec = mm * 60 + ss;
            return true;
        }

        private MarkerRow GetImageMarkerRow(string type)
        {
            return BuildDynamicMarkerRows()
                   .FirstOrDefault(r => r.Type == type);
        }





        #region Page 1 – Filling Phase

        #region Page 1 – Filling Phase

        //Page 1 First Design

        //Pressure Flow + EMG + Video
        private void DrawFillingPhasePage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            int graphShiftDown = fSize.Height * 2;   // adjust as needed


            // header area + space for results
            int topHeaderSpace = fSize.Height * 15;
            int bottomResultsSpace = fSize.Height * 22;   // room for "Filling Phase Result"

            //int graphTop = rect.Top + topHeaderSpace;
            //int graphHeight = rect.Height - topHeaderSpace - bottomResultsSpace;

            int graphTop = rect.Top + topHeaderSpace + graphShiftDown;
            int graphHeight = rect.Height - topHeaderSpace - bottomResultsSpace - graphShiftDown;


            if (graphHeight < fSize.Height * 5)
                graphHeight = fSize.Height * 5;

            // --- LEFT LABEL BOX (channel names + units + scales) ---
            int labelBoxWidth = fSize.Width * 4;

            Rectangle labelRect = new Rectangle(
                rect.Left + fSize.Width,   // small inner margin from page left
                graphTop,
                labelBoxWidth,
                graphHeight
            );

            // --- GRAPH FRAME (to the right of label box) ---
            int graphLeft = labelRect.Right;        // gap between label & graph
            int graphRight = rect.Right - fSize.Width;               // inner right margin
            int graphWidth = Math.Max(graphRight - graphLeft, fSize.Width * 5);

            Rectangle gRect = new Rectangle(
                graphLeft,
                graphTop,
                graphWidth,
                graphHeight
            );

            // Inner rectangle for curves only (leave room at bottom for time axis)
            //int curvesBottomMargin = fSize.Height * 4;
            //Rectangle curvesRect = new Rectangle(
            //    gRect.Left,
            //    gRect.Top,
            //    gRect.Width,
            //    Math.Max(gRect.Height - curvesBottomMargin, fSize.Height * 3)
            //);
            // Curves must fill the full graph frame.
            // Time axis is drawn BELOW the graph by DrawTimeAxisBottom, so reserving space here creates blank area.
            Rectangle curvesRect = gRect;

            // 1) Header + patient block
            //DrawHeaderBlock(ctx, rect, fSize);

            if (_data.HeadOne == true)
            {
                if (_data.DefaultHeader == true)
                {
                    DrawHeaderBlock(ctx, rect, fSize);
                }
                else if (_data.LetterHead == true)
                {
                    DrawLatterHeadImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalLogo == true)
                {
                    DrawHeaderLogoImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalDataAndLogo == true)
                {
                    DrawHeaderBlockWithLogoLeft(ctx, g, rect, fSize);
                }
            }
            else
            {

            }

            DrawPatientBlock(ctx, g, rect, fSize);

            // 2) Axes + labels + ranges + grid using label box + graph frame
            DrawFillingAxes(ctx, g, labelRect, curvesRect, fSize);

            // 3) Curves + markers, clipped to graph frame
            Region oldClip = g.Clip;
            g.SetClip(gRect);

            //DrawFillingCurvesFromRecorded(g, curvesRect);
            //DrawMarkersOnGraph(g, gRect);

            if (TryGetS1S2Window(out double t1, out double t2))
            {
                DrawFillingCurvesFromRecorded(g, curvesRect, t1, t2);
                DrawMarkersOnGraph(g, curvesRect, t1, t2);
            }
            else
            {
                DrawFillingCurvesFromRecorded(g, curvesRect);
                DrawMarkersOnGraph(g, curvesRect);
            }


            g.SetClip(oldClip, CombineMode.Replace);

            // 4) Time axis at bottom of graph (0 and total duration)
            // DrawTimeAxisBottom(g, gRect);
            if (TryGetS1S2Window(out double t1Axis, out double t2Axis))
                DrawTimeAxisBottom(g, gRect, t1Axis, t2Axis);
            else
                DrawTimeAxisBottom(g, gRect);

            // 5) Results block below graph with comfortable gap
            int resultsTop = gRect.Bottom + fSize.Height * 2;
            DrawFillingResults(ctx, rect.Left, resultsTop, fSize);
            //  DrawFillingResults(ctx, rect, fSize);
            // After switch block
            DrawFooter(ctx, g, rect, fSize, 0, _totalPages);

        }



        //Start Code for Uroflowmetry & Uroflowmetry + EMG Test 
        private void DrawUroflowmetryTestPage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            // header area + space for results
            int topHeaderSpace = fSize.Height * 16;

            //Adjust Graph Height
            int graphHeight = (int)(rect.Height * 0.26);
            int reservedBottomSpace = fSize.Height * 35;

            int graphTop = rect.Top + topHeaderSpace;

            if (graphHeight < fSize.Height * 5)
                graphHeight = fSize.Height * 5;

            int labelBoxWidth = fSize.Width * 4;

            Rectangle labelRect = new Rectangle(
                rect.Left + fSize.Width,
                graphTop,
                labelBoxWidth,
                graphHeight
            );

            int graphLeft = labelRect.Right;
            int graphRight = rect.Right - fSize.Width;
            int graphWidth = Math.Max(graphRight - graphLeft, fSize.Width * 5);

            Rectangle gRect = new Rectangle(
                graphLeft,
                graphTop,
                graphWidth,
                graphHeight
            );

           
            Rectangle curvesRect = gRect;


            //DrawHeaderBlock(ctx, rect, fSize);
            if (_data.HeadOne == true)
            {
                if (_data.DefaultHeader == true)
                {
                    DrawHeaderBlock(ctx, rect, fSize);
                }
                else if (_data.LetterHead == true)
                {
                    DrawLatterHeadImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalLogo == true)
                {
                    DrawHeaderLogoImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalDataAndLogo == true)
                {
                    DrawHeaderBlockWithLogoLeft(ctx, g, rect, fSize);
                }
            }
            else
            {

            }
            UroflowPatientDetails(ctx, g, rect, fSize);

            //Code For show Big Graph (structer channels not show plotting)
            DrawUroflowmetryGraph(ctx, g, labelRect, curvesRect, fSize);

            Region oldClip = g.Clip;
            g.SetClip(curvesRect);


            if (TryGetS1S2Window(out double t1, out double t2))
            {
                //Big Graph Plotting show 
                UroflowmetryBigGraphPlotting(g, curvesRect, t1, t2);
                UroflowmetryMarkersOnGraph(g, curvesRect, t1, t2);
            }
            else
            {
                //Big Graph Plotting show 
                UroflowmetryBigGraphPlotting(g, curvesRect);
                UroflowmetryMarkersOnGraph(g, curvesRect);
            }


            g.SetClip(oldClip, CombineMode.Replace);

            // DrawTimeAxisBottom(g, gRect);
            //if (TryGetS1S2Window(out double t1Axis, out double t2Axis))
            //    DrawTimeAxisBottom(g, gRect, t1Axis, t2Axis);
            //else
            //    DrawTimeAxisBottom(g, gRect);

            // Calculate time range based on test duration
            if (_graph != null && _graph.Samples != null && _graph.Samples.Count > 0)
            {
                double testDuration = _graph.Samples[_graph.Samples.Count - 1].T - _graph.Samples[0].T;

                if (TryGetS1S2Window(out double t1Axis, out double t2Axis))
                {
                    // If we have S1S2 window, use that with appropriate scale
                    if (testDuration <= 90)
                        UroflowDrawTimeAxisBottom(g, gRect, t1Axis, t2Axis, 90); // Show 0-90 scale
                    else if (testDuration <= 180)
                        UroflowDrawTimeAxisBottom(g, gRect, t1Axis, t2Axis, 180); // Show 0-180 scale
                    else
                        UroflowDrawTimeAxisBottom(g, gRect, t1Axis, t2Axis, testDuration); // Show actual duration
                }
                else
                {
                    // No S1S2 window, just use test duration with appropriate scale
                    if (testDuration <= 90)
                        UroflowDrawTimeAxisBottom(g, gRect, 90); // Show 0-90 scale
                    else if (testDuration <= 180)
                        UroflowDrawTimeAxisBottom(g, gRect, 180); // Show 0-180 scale
                    else
                        UroflowDrawTimeAxisBottom(g, gRect, testDuration); // Show actual duration
                }
            }
            else
            {
                // Fallback if no graph data
                if (TryGetS1S2Window(out double t1Axis, out double t2Axis))
                    DrawTimeAxisBottom(g, gRect, t1Axis, t2Axis);
                else
                    DrawTimeAxisBottom(g, gRect);
            }


            int resultsTop = gRect.Bottom + fSize.Height * 2;

            //Small Two graph
            DrawUroflowOnePage(ctx, g, rect, fSize);

        }

        private void UroflowDrawTimeAxisBottom(Graphics g, Rectangle rect, double? maxTime = null)
        {
            if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
                return;

            double startTime = _graph.Samples[0].T;
            double endTime = maxTime ?? (_graph.Samples[_graph.Samples.Count - 1].T - _graph.Samples[0].T);

            DrawTimeAxis(g, rect, startTime, startTime + endTime);
        }

        private void UroflowDrawTimeAxisBottom(Graphics g, Rectangle rect, double startTime, double endTime, double? maxScale = null)
        {
            double actualEndTime = maxScale ?? endTime;

            // Your existing axis drawing code here, using actualEndTime as the max scale
            DrawTimeAxis(g, rect, startTime, actualEndTime);
        }
        private void DrawTimeAxis(Graphics g, Rectangle rect, double startTime, double endTime)
        {
            using (Pen pen = new Pen(Color.Black, 1))
            using (Font font = new Font("Arial", 8))
            {
                // Draw axis line
                int y = rect.Bottom + 5;
                g.DrawLine(pen, rect.Left, y, rect.Right, y);

                // Calculate range
                double range = endTime - startTime;
                double interval = GetTimeInterval(range);

                // Draw tick marks and labels from startTime to endTime
                for (double t = startTime; t <= endTime; t += interval)
                {
                    // Calculate x position based on time proportion
                    double proportion = (t - startTime) / range;
                    int x = rect.Left + (int)(proportion * rect.Width);

                    // Only draw if within graph bounds
                    if (x >= rect.Left && x <= rect.Right)
                    {
                        // Draw tick
                        g.DrawLine(pen, x, y - 3, x, y + 3);

                        // Draw time label
                        string timeText = (t - startTime).ToString("F0") + "s"; // Show seconds from start
                        SizeF textSize = g.MeasureString(timeText, font);
                        float textX = x - textSize.Width / 2;
                        float textY = y + 5;

                        // Ensure text stays within bounds
                        textX = Math.Max(rect.Left, Math.Min(rect.Right - textSize.Width, textX));

                        g.DrawString(timeText, font, Brushes.Black, textX, textY);
                    }
                }

                // Draw the last label if not already drawn and if it's within range
                double lastLabelTime = startTime + (Math.Floor(range / interval) * interval);
                if (lastLabelTime < endTime - 0.1)
                {
                    double proportion = 1.0;
                    int x = rect.Right;

                    string timeText = (endTime - startTime).ToString("F0") + "s";
                    SizeF textSize = g.MeasureString(timeText, font);
                    float textX = x - textSize.Width;
                    float textY = y + 5;

                    if (textX >= rect.Left)
                    {
                        g.DrawString(timeText, font, Brushes.Black, textX, textY);
                    }
                }
            }
        }
       
        private double GetTimeInterval(double range)
        {
            // Determine appropriate interval based on range
            if (range <= 30)
                return 5; // 5 second intervals for short tests
            else if (range <= 90)
                return 10; // 10 second intervals for medium tests
            else if (range <= 180)
                return 20; // 20 second intervals for longer tests
            else
                return 30; // 30 second intervals for very long tests
        }

        private void UroflowmetryBigGraphPlotting(Graphics g, Rectangle curvesRect, double? clipStart = null, double? clipEnd = null)
        {
            if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
                return;

            List<ReportSample> samples = _graph.Samples;
            var def = _graph.TestDef;

            bool hasDef = def?.Lanes != null && def.Lanes.Count > 0;

            int valueCount = samples[0].Values?.Length ?? 0;
            if (valueCount <= 0)
                return;

            int laneCount = hasDef ? def.Lanes.Count : valueCount;
            if (laneCount <= 0)
                return;

            // ===== START MODIFICATION =====
            // Calculate test duration to determine fixed scale
            double actualStartT = samples[0].T;
            double actualEndT = samples[samples.Count - 1].T;
            double testDuration = actualEndT - actualStartT;

            // Determine fixed time scale based on test duration
            double fixedEndT;
            if (testDuration <= 90)
                fixedEndT = actualStartT + 90; // Show 0-90 scale
            else if (testDuration <= 180)
                fixedEndT = actualStartT + 180; // Show 0-180 scale
            else
                fixedEndT = actualStartT + Math.Ceiling(testDuration); // Show actual duration rounded up

            // Base time window - use fixed scale for display, but keep actual data range for sampling
            double startT = clipStart ?? actualStartT;
            double displayEndT = clipEnd ?? fixedEndT;
            double actualEndTForData = actualEndT; // Keep actual end for data filtering
                                                   // ===== END MODIFICATION =====

            // Get all R1-R2 windows
            var r1r2Windows = GetAllR1R2Windows();
            bool hasHoles = r1r2Windows.Count > 0;
            double totalHoleWidth = hasHoles ? GetTotalHoleWidth(r1r2Windows) : 0.0;

            // Compressed timeline for display
            double effectiveDisplayEndT = displayEndT - totalHoleWidth;
            if (effectiveDisplayEndT <= startT)
                effectiveDisplayEndT = startT + 1.0;

            double displayDuration = effectiveDisplayEndT - startT;

            // Identity mapping
            int[] laneToValIndex = new int[laneCount];
            for (int c = 0; c < laneCount; c++)
                laneToValIndex[c] = Math.Min(c, valueCount - 1);

            float laneHeight = (float)curvesRect.Height / laneCount;
            RectangleF plot = curvesRect;

            int total = samples.Count;
            int px = Math.Max(1, curvesRect.Width);
            int step = Math.Max(1, total / px);

            // Compute data min/max per lane
            double[] dataMin = new double[laneCount];
            double[] dataMax = new double[laneCount];
            for (int c = 0; c < laneCount; c++)
            {
                dataMin[c] = double.MaxValue;
                dataMax[c] = double.MinValue;
            }

            for (int i = 0; i < total; i++)
            {
                var s = samples[i];
                if (s.Values == null) continue;

                double t = s.T;
                if (t < actualStartT || t > actualEndTForData) continue; // Use actual data range
                if (hasHoles && IsPointInAnyHole(t, r1r2Windows)) continue;

                for (int c = 0; c < laneCount; c++)
                {
                    int vi = laneToValIndex[c];
                    if (vi < 0 || vi >= s.Values.Length) continue;

                    double v = s.Values[vi];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                    if (v < dataMin[c]) dataMin[c] = v;
                    if (v > dataMax[c]) dataMax[c] = v;
                }
            }

            // Finalize data min/max
            for (int c = 0; c < laneCount; c++)
            {
                if (dataMin[c] == double.MaxValue)
                {
                    dataMin[c] = 0;
                    dataMax[c] = 1;
                }
                if (dataMax[c] - dataMin[c] < 1e-6)
                    dataMax[c] = dataMin[c] + 1.0;
            }

            Color[] fallbackColors =
            {
                Color.Red, Color.Blue, Color.Green,
                Color.DarkOrange, Color.Purple,
                Color.Brown, Color.Magenta
            };

            // Draw per lane
            for (int c = 0; c < laneCount; c++)
            {
                int valIndex = laneToValIndex[c];

                double min, max;
                Color penColor;

                if (hasDef && c < def.Lanes.Count)
                {
                    var laneDef = def.Lanes[c];
                    penColor = laneDef.Color;

                    double defMin = laneDef.ScaleMin;
                    double defMax = laneDef.ScaleMax;

                    bool defValid = (defMax > defMin) && (Math.Abs(defMax - defMin) > 1e-6);

                    if (!defValid)
                    {
                        min = dataMin[c];
                        max = dataMax[c];
                    }
                    else
                    {
                        min = defMin;
                        max = defMax;
                    }
                }
                else
                {
                    min = dataMin[c];
                    max = dataMax[c];
                    penColor = fallbackColors[c % fallbackColors.Length];
                }

                double range = max - min;
                if (range <= 0) range = 1.0;

                float laneTop = plot.Top + c * laneHeight;
                float laneBottom = laneTop + laneHeight;

                using (Pen pen = new Pen(penColor, 1.5f))
                {
                    List<PointF> allPoints = new List<PointF>();
                    Dictionary<int, PointF> lastPointBeforeHoles = new Dictionary<int, PointF>();
                    int currentHoleIndex = -1;

                    for (int i = 0; i < total; i += step)
                    {
                        var s = samples[i];
                        if (s.Values == null || s.Values.Length <= valIndex)
                            continue;

                        double t = s.T;

                        // ===== MODIFICATION: Use actual data range for filtering =====
                        if (t < actualStartT || t > actualEndTForData)
                            continue;
                        // ===== END MODIFICATION =====

                        // Check if we're in any hole
                        bool inHole = false;
                        int holeIdx = -1;
                        if (hasHoles)
                        {
                            for (int h = 0; h < r1r2Windows.Count; h++)
                            {
                                if (t >= r1r2Windows[h].R1Time && t <= r1r2Windows[h].R2Time)
                                {
                                    inHole = true;
                                    holeIdx = h;
                                    break;
                                }
                            }
                        }

                        // Get compressed time based on ALL holes before this point
                        double tCompressed = GetCompressedTime(t, r1r2Windows);

                        // ===== MODIFICATION: Use display end time for X positioning =====
                        float x = (float)(plot.Left + (tCompressed - startT) / displayDuration * plot.Width);
                        // ===== END MODIFICATION =====

                        double v = s.Values[valIndex];
                        if (double.IsNaN(v) || double.IsInfinity(v))
                        {
                            if (!inHole && allPoints.Count >= 2)
                            {
                                g.DrawLines(pen, allPoints.ToArray());
                                allPoints.Clear();
                            }
                            continue;
                        }

                        float y = (float)(laneBottom - (v - min) / range * laneHeight);

                        if (x < plot.Left || x > plot.Right)
                            continue;

                        if (inHole)
                        {
                            // Store the last valid point before entering this specific hole
                            if (allPoints.Count > 0 && !lastPointBeforeHoles.ContainsKey(holeIdx))
                            {
                                lastPointBeforeHoles[holeIdx] = allPoints[allPoints.Count - 1];
                            }
                            continue;
                        }

                        // If we're out of holes and have stored bridge points, add them in order
                        if (lastPointBeforeHoles.Count > 0)
                        {
                            // Add bridge points in the order of holes
                            foreach (var kvp in lastPointBeforeHoles.OrderBy(k => k.Key))
                            {
                                allPoints.Add(kvp.Value);
                            }
                            lastPointBeforeHoles.Clear();
                        }

                        allPoints.Add(new PointF(x, y));
                    }

                    // Draw the complete line
                    if (allPoints.Count >= 2)
                        g.DrawLines(pen, allPoints.ToArray());
                }
            }
        }
        
        
        //private void UroflowmetryBigGraphPlotting(Graphics g, Rectangle curvesRect, double? clipStart = null, double? clipEnd = null)
        //{
        //    if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
        //        return;

        //    List<ReportSample> samples = _graph.Samples;
        //    var def = _graph.TestDef;

        //    bool hasDef = def?.Lanes != null && def.Lanes.Count > 0;

        //    int valueCount = samples[0].Values?.Length ?? 0;
        //    if (valueCount <= 0)
        //        return;

        //    int laneCount = hasDef ? def.Lanes.Count : valueCount;
        //    if (laneCount <= 0)
        //        return;

        //    // Base time window
        //    double startT = clipStart ?? samples[0].T;
        //    double endT = clipEnd ?? samples[samples.Count - 1].T;

        //    // Get all R1-R2 windows
        //    var r1r2Windows = GetAllR1R2Windows();
        //    bool hasHoles = r1r2Windows.Count > 0;
        //    double totalHoleWidth = hasHoles ? GetTotalHoleWidth(r1r2Windows) : 0.0;

        //    // Compressed timeline
        //    double effectiveEndT = endT - totalHoleWidth;
        //    if (effectiveEndT <= startT)
        //        effectiveEndT = startT + 1.0;

        //    double duration = effectiveEndT - startT;

        //    // Identity mapping
        //    int[] laneToValIndex = new int[laneCount];
        //    for (int c = 0; c < laneCount; c++)
        //        laneToValIndex[c] = Math.Min(c, valueCount - 1);

        //    float laneHeight = (float)curvesRect.Height / laneCount;
        //    RectangleF plot = curvesRect;

        //    int total = samples.Count;
        //    int px = Math.Max(1, curvesRect.Width);
        //    int step = Math.Max(1, total / px);

        //    // Compute data min/max per lane
        //    double[] dataMin = new double[laneCount];
        //    double[] dataMax = new double[laneCount];
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        dataMin[c] = double.MaxValue;
        //        dataMax[c] = double.MinValue;
        //    }

        //    for (int i = 0; i < total; i++)
        //    {
        //        var s = samples[i];
        //        if (s.Values == null) continue;

        //        double t = s.T;
        //        if (t < startT || t > endT) continue;
        //        if (hasHoles && IsPointInAnyHole(t, r1r2Windows)) continue;

        //        for (int c = 0; c < laneCount; c++)
        //        {
        //            int vi = laneToValIndex[c];
        //            if (vi < 0 || vi >= s.Values.Length) continue;

        //            double v = s.Values[vi];
        //            if (double.IsNaN(v) || double.IsInfinity(v)) continue;

        //            if (v < dataMin[c]) dataMin[c] = v;
        //            if (v > dataMax[c]) dataMax[c] = v;
        //        }
        //    }

        //    // Finalize data min/max
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        if (dataMin[c] == double.MaxValue)
        //        {
        //            dataMin[c] = 0;
        //            dataMax[c] = 1;
        //        }
        //        if (dataMax[c] - dataMin[c] < 1e-6)
        //            dataMax[c] = dataMin[c] + 1.0;
        //    }

        //    Color[] fallbackColors =
        //    {
        //    Color.Red, Color.Blue, Color.Green,
        //    Color.DarkOrange, Color.Purple,
        //    Color.Brown, Color.Magenta
        //};

        //    // Draw per lane
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        int valIndex = laneToValIndex[c];

        //        double min, max;
        //        Color penColor;

        //        if (hasDef && c < def.Lanes.Count)
        //        {
        //            var laneDef = def.Lanes[c];
        //            penColor = laneDef.Color;

        //            double defMin = laneDef.ScaleMin;
        //            double defMax = laneDef.ScaleMax;

        //            bool defValid = (defMax > defMin) && (Math.Abs(defMax - defMin) > 1e-6);

        //            if (!defValid)
        //            {
        //                min = dataMin[c];
        //                max = dataMax[c];
        //            }
        //            else
        //            {
        //                min = defMin;
        //                max = defMax;
        //            }
        //        }
        //        else
        //        {
        //            min = dataMin[c];
        //            max = dataMax[c];
        //            penColor = fallbackColors[c % fallbackColors.Length];
        //        }

        //        double range = max - min;
        //        if (range <= 0) range = 1.0;

        //        float laneTop = plot.Top + c * laneHeight;
        //        float laneBottom = laneTop + laneHeight;

        //        using (Pen pen = new Pen(penColor, 1.5f))
        //        {
        //            List<PointF> allPoints = new List<PointF>();
        //            Dictionary<int, PointF> lastPointBeforeHoles = new Dictionary<int, PointF>();
        //            int currentHoleIndex = -1;

        //            for (int i = 0; i < total; i += step)
        //            {
        //                var s = samples[i];
        //                if (s.Values == null || s.Values.Length <= valIndex)
        //                    continue;

        //                double t = s.T;
        //                if (t < startT || t > endT)
        //                    continue;

        //                // Check if we're in any hole
        //                bool inHole = false;
        //                int holeIdx = -1;
        //                if (hasHoles)
        //                {
        //                    for (int h = 0; h < r1r2Windows.Count; h++)
        //                    {
        //                        if (t >= r1r2Windows[h].R1Time && t <= r1r2Windows[h].R2Time)
        //                        {
        //                            inHole = true;
        //                            holeIdx = h;
        //                            break;
        //                        }
        //                    }
        //                }

        //                // Get compressed time based on ALL holes before this point
        //                double tCompressed = GetCompressedTime(t, r1r2Windows);
        //                float x = (float)(plot.Left + (tCompressed - startT) / duration * plot.Width);

        //                double v = s.Values[valIndex];
        //                if (double.IsNaN(v) || double.IsInfinity(v))
        //                {
        //                    if (!inHole && allPoints.Count >= 2)
        //                    {
        //                        g.DrawLines(pen, allPoints.ToArray());
        //                        allPoints.Clear();
        //                    }
        //                    continue;
        //                }

        //                float y = (float)(laneBottom - (v - min) / range * laneHeight);

        //                if (x < plot.Left || x > plot.Right)
        //                    continue;

        //                if (inHole)
        //                {
        //                    // Store the last valid point before entering this specific hole
        //                    if (allPoints.Count > 0 && !lastPointBeforeHoles.ContainsKey(holeIdx))
        //                    {
        //                        lastPointBeforeHoles[holeIdx] = allPoints[allPoints.Count - 1];
        //                    }
        //                    continue;
        //                }

        //                // If we're out of holes and have stored bridge points, add them in order
        //                if (lastPointBeforeHoles.Count > 0)
        //                {
        //                    // Add bridge points in the order of holes
        //                    foreach (var kvp in lastPointBeforeHoles.OrderBy(k => k.Key))
        //                    {
        //                        allPoints.Add(kvp.Value);
        //                    }
        //                    lastPointBeforeHoles.Clear();
        //                }

        //                allPoints.Add(new PointF(x, y));
        //            }

        //            // Draw the complete line
        //            if (allPoints.Count >= 2)
        //                g.DrawLines(pen, allPoints.ToArray());
        //        }
        //    }
        //}

        void UroflowmetryMarkersOnGraph(Graphics g, Rectangle plotRect, double? clipStart = null, double? clipEnd = null)
        {
            if (_graph?.Markers == null || _graph?.Samples == null || _graph.Samples.Count < 2)
                return;

            // Get all R1-R2 windows for time compression
            var r1r2Windows = GetAllR1R2Windows();
            bool hasHoles = r1r2Windows.Count > 0;
            double totalHoleWidth = hasHoles ? GetTotalHoleWidth(r1r2Windows) : 0.0;

            // Reset clip temporarily so marker time labels always render
            Region oldClip = g.Clip;
            g.ResetClip();

            try
            {
                double startT = clipStart ?? _graph.Samples[0].T;
                double endT = clipEnd ?? _graph.Samples[_graph.Samples.Count - 1].T;

                // Compressed timeline
                double effectiveEndT = endT - totalHoleWidth;
                if (effectiveEndT <= startT)
                    effectiveEndT = startT + 1.0;

                double duration = effectiveEndT - startT;

                TryGetR1R2Window(out double r1t, out double r2t);

                RectangleF plot = plotRect;

                int laneCount = _graph.TestDef?.Lanes?.Count ?? 0;
                if (laneCount == 0)
                    return;

                float laneHeight = plot.Height / laneCount;

                int gpCounter = 0; // GP numbering

                using (Font font = new Font("Arial", 7f, FontStyle.Bold))
                using (Font valueFont = new Font("Arial", 7f, FontStyle.Regular))
                using (Font timeFont = new Font("Arial", 7f, FontStyle.Regular))
                {
                    float lastTimeLabelRight = float.NegativeInfinity;

                    foreach (var m in _graph.Markers)
                    {
                        if (string.IsNullOrWhiteSpace(m.Label))
                            continue;

                        string rawLabel = m.Label.Trim().ToUpperInvariant();

                        // Skip R1 / R2 markers themselves
                        if (rawLabel == "R1" || rawLabel == "R2")
                            continue;

                        double t = m.T;

                        // Skip if marker is inside any R1-R2 hole
                        if (hasHoles && IsPointInAnyHole(t, r1r2Windows))
                            continue;

                        if (t < startT || t > endT)
                            continue;

                        // Get compressed time for marker position
                        double tCompressed = GetCompressedTime(t, r1r2Windows);
                        float x = (float)(plot.Left + (tCompressed - startT) / duration * plot.Width);

                        // GP numbering
                        string displayLabel = rawLabel;
                        if (rawLabel.StartsWith("GP"))
                        {
                            gpCounter++;
                            displayLabel = gpCounter.ToString();
                        }

                        // Get color from label
                        Color markerColor = GetMarkerColor(rawLabel);

                        using (Pen pen = new Pen(markerColor, 1.2f))
                        using (Brush brush = new SolidBrush(markerColor))
                        {
                            // Marker line
                            g.DrawLine(pen, x, plot.Top, x, plot.Bottom);

                            // Marker label (LEFT side)
                            SizeF sz = g.MeasureString(displayLabel, font);
                            float textX = x - sz.Width - 6;
                            if (textX < plot.Left + 2)
                                textX = plot.Left + 2;

                            g.DrawString(displayLabel, font, brush, textX, plot.Top + 4);

                            // Draw marker time under the plot (absolute test time)
                            string timeText = FormatMarkerTime(t); // Use original time for display
                            SizeF tsz = g.MeasureString(timeText, timeFont);

                            float timeX = x - (tsz.Width / 2f);
                            if (timeX < plot.Left) timeX = plot.Left;
                            if (timeX + tsz.Width > plot.Right) timeX = plot.Right - tsz.Width;

                            //Start this code not show correct marker line time show in bottom 19-02-2026
                            // Simple overlap control: skip time text if too close to previous one
                            //if (timeX > lastTimeLabelRight + 2)
                            //{
                            //    using (Brush tb = new SolidBrush(Color.Black))
                            //    {
                            //        float timeY = plot.Bottom + 2;
                            //        if (timeY + tsz.Height > g.VisibleClipBounds.Bottom)
                            //            timeY = g.VisibleClipBounds.Bottom - tsz.Height;

                            //        g.DrawString(timeText, timeFont, tb, timeX, timeY);
                            //    }
                            //    lastTimeLabelRight = timeX + tsz.Width;
                            //}

                            //Start this code for show every marker line time show in bottom 19-02-2026
                            using (Brush tb = new SolidBrush(Color.Black))
                            {
                                float timeY = plot.Bottom + 2;

                                // Ensure not clipped by physical page bottom
                                if (timeY + tsz.Height > g.VisibleClipBounds.Bottom)
                                    timeY = g.VisibleClipBounds.Bottom - tsz.Height;

                                g.DrawString(timeText, timeFont, tb, timeX, timeY);
                            }
                            //EnD this code for show every marker line time show in bottom 19-02-2026
                        }

                        // Marker values
                        var sample = GetSampleAtTime(t);
                        if (sample?.Values == null)
                            continue;

                        for (int lane = 0; lane < laneCount; lane++)
                        {
                            if (lane >= sample.Values.Length)
                                continue;

                            string txt = Math.Round(sample.Values[lane]).ToString();

                            float y = plot.Top + lane * laneHeight + laneHeight / 2f;

                            float tx = x + 4;
                            if (tx + 20 > plot.Right)
                                tx = x - 22;

                            //This Code for Marker Line Right Side Values show in dynamic color chnage on 19-02-2026
                            //Color laneColor = _graph.TestDef.Lanes[lane].Color;

                            //using (Brush laneBrush = new SolidBrush(laneColor))
                            //{
                            //    g.DrawString(txt, valueFont, laneBrush, tx, y - 6);
                            //}

                            //This Code for Marker Line Right Side Values show in by Default Black color change on 19-02-2026
                            using (Brush laneBrush = new SolidBrush(Color.Black))
                            {
                                g.DrawString(txt, valueFont, laneBrush, tx, y - 6);
                            }
                        }
                    }
                }
            }
            finally
            {
                g.Clip = oldClip;
            }
        }

        private void DrawUroflowmetryGraph(GdiContext ctx, Graphics g, Rectangle labelRect, Rectangle curvesRect, Size fSize)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 9f);

            // Optional small top padding INSIDE the graph.
            // IMPORTANT: if you move Top down, you MUST reduce Height by the same amount.
            int topPad = fSize.Height * 1;

            labelRect = new Rectangle(
                labelRect.Left,
                labelRect.Top ,
                labelRect.Width,
                Math.Max(1, labelRect.Height)
            );

            curvesRect = new Rectangle(
                curvesRect.Left,
                curvesRect.Top,
                curvesRect.Width,
                Math.Max(1, curvesRect.Height)
            );

            // Draw outer boxes
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(labelRect);
            ctx.Rectangle(curvesRect);

            var def = _graph != null ? _graph.TestDef : null;

            // Fallback definitions for uroflow page
            string[] fallbackNames = { "Qvol", "Qura", "EMG" };
            string[] fallbackUnits = { "ml", "ml/sec", "uV" };
            double[] fallbackMin = { 0, 0, 0 };
            double[] fallbackMax = { 500, 25, 2000 };

            int laneCount =
                (def != null && def.Lanes != null && def.Lanes.Count > 0)
                ? def.Lanes.Count
                : fallbackNames.Length;

            float laneHeight = (float)curvesRect.Height / laneCount;

            for (int i = 0; i < laneCount; i++)
            {
                float laneTop = curvesRect.Top + i * laneHeight;
                float laneBottom = laneTop + laneHeight;
                float midY = (laneTop + laneBottom) / 2f;

                // Strong horizontal separator across label + graph
                if (i > 0)
                {
                    ctx.MoveTo(labelRect.Left, (int)laneTop);
                    ctx.LineTo(curvesRect.Right, (int)laneTop);
                }

                // Dotted grid lines inside the graph box only
                for (int k = 1; k <= 4; k++)
                {
                    float y = laneTop + k * laneHeight / 5f;
                    using (Pen gridPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dot })
                    {
                        g.DrawLine(gridPen, curvesRect.Left, y, curvesRect.Right, y);
                    }
                }

                // Lane meta
                string name, unit;
                double min, max;
                Color laneColor;

                if (def != null && def.Lanes != null && i < def.Lanes.Count)
                {
                    var laneDef = def.Lanes[i];

                    name = laneDef.Name ?? fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
                    unit = laneDef.Unit;
                    if (string.IsNullOrEmpty(unit))
                        unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];

                    min = laneDef.ScaleMin;
                    max = laneDef.ScaleMax;

                    // If scale is invalid, fall back
                    if (max <= min)
                    {
                        min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
                        max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
                    }

                    laneColor = laneDef.Color;
                }
                else
                {
                    name = fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
                    unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];
                    min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
                    max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
                    laneColor = Color.Black;
                }

                // Min/Max near graph edge inside labelRect
                int minMaxRightX = labelRect.Right - (int)(fSize.Width * 0.2);
                int labelRightX = labelRect.Right - (int)(fSize.Width * 1.2);

                ctx.SetTextColor(Color.Black);

                // MAX (top)
                ctx.TextOutRight(
                    minMaxRightX,
                    (int)(laneTop + fSize.Height * 0.1),
                    max.ToString("0")
                );

                // MIN (bottom)
                ctx.TextOutRight(
                    minMaxRightX,
                    (int)(laneBottom - fSize.Height * 0.9),
                    min.ToString("0")
                );

                // Name and unit
                ctx.SetTextColor(laneColor);
                ctx.TextOutRight(
                    labelRightX,
                    (int)(midY - fSize.Height * 0.6),
                    name
                );

                ctx.SetTextColor(Color.Black);
                ctx.TextOutRight(
                    labelRightX,
                    (int)(midY + fSize.Height * 0.2),
                    unit
                );
            }

            ctx.SetTextColor(Color.Black);
        }

       
        
        private void UroflowPatientDetails(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            int startRow = 7;
            int row = startRow;
            int left = rect.Left;
            int right = rect.Right;


            ctx.SetFont(_data.FontFamily, 13f, FontStyle.Bold);
            ctx.SetTextColor(Color.Red);

            ctx.TextOutLeft(
                left,
                rect.Top + fSize.Height * row + 5,
                $"Test :-  {_data.TestName} Report"
            );

            // restore normal font/color
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            row += 1;

            int labelX = left + 10;
            int valueOffset = 90;
            int rightLabelX = right - 360;
            int rightValueOffset = 120;

            // ✅ Small extra gap in pixels (adjust 4–8)
            int extraGap = 6;
            int topPadding = 12;




            // ✅ Helper for Y position (NO float)
            int Y(int r) => rect.Top + topPadding + (fSize.Height * r) + (extraGap * (r - startRow));

            // ================= BACKGROUND =================
            int totalRows = 4;
            int bgTop = Y(startRow) - 10;
            int bgHeight = (fSize.Height * totalRows) + (extraGap * totalRows) + 12;

            Rectangle bgRect = new Rectangle(
                left - 5,
                bgTop + 25,
                rect.Width + 10,
                bgHeight
            );



            // ================= TEXT =================
            ctx.SetFont(_data.FontFamily, 11f, FontStyle.Regular);

            // -------- ROW 1 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Patient ID :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientId);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Age :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Age.ToString());

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Date & Time :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TestDate.ToString("dd/M/yyyy HH:mm:ss"));
            row++;

            // -------- ROW 2 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Name :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientName);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Sex :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Sex);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Referred By :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.ReferredBy);
            row++;

            // -------- ROW 3 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Mobile No.:");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientMobile);


            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 386, Y(row), "Weight :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Weight);

            if (!string.IsNullOrEmpty(_data.TechnicianName))
            {
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX, Y(row), "Technician :");
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TechnicianName);
            }
            row++;

           

            // -------- ROW 4 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Address :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientAddress);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Symptoms :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.Symptoms);


            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + fSize.Height + 250;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);

        }


        private void DrawCenteredText(GdiContext ctx, int centerX, int y, string text)
        {
            Size sz = ctx.MeasureText(text);
            ctx.TextOutLeft(centerX - sz.Width / 2, y, text);
        }

        //For small two graph & Text
         private void DrawUroflowOnePage(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            string font = _data.FontFamily;
            int halfTop = rect.Top + rect.Height / 2;

            Rectangle workRect = new Rectangle(
                rect.Left,
                halfTop,
                rect.Width,
                rect.Bottom - halfTop
            );

            int topSpace = fSize.Height * 0;

            // ================= COLORS =================
            Color blue = Color.White;
            Brush blueBrush = new SolidBrush(blue);

            // ================= TABLE =================
            int tableWidth = fSize.Width * 43;
            int colWidth = tableWidth / 2;
            int headerH = fSize.Height * 1;
            int contentH = (int)(workRect.Height * 0.40);
            int tableLeft = workRect.Left + fSize.Width;
            int tableTop = workRect.Top + fSize.Height + topSpace + 40;

            // =====================================================
            // 🔷 GRADIENT HEADER : "Siroky Nomograms"
            // =====================================================
            int nomRowH = fSize.Height + 10;
            int nomTitleHeight = nomRowH + 10;
            int nomHeaderLeft = tableLeft;
            int nomHeaderWidth = tableWidth;
            int nomHeaderTop = tableTop - nomTitleHeight - 15;

            ctx.SetPen(Color.Transparent, 0);

            using (LinearGradientBrush nomHeaderBrush =
                new LinearGradientBrush(
                    new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    nomHeaderBrush,
                    new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight)
                );
            }

            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(
                nomHeaderLeft + 12,
                nomHeaderTop + (nomTitleHeight - fSize.Height) / 2,
                "Siroky Nomograms"
            );

            Rectangle tableRect = new Rectangle(
                tableLeft,
                tableTop,
                tableWidth,
                headerH + contentH
            );

            int dividerX = tableLeft + colWidth;
            // ================= CONTENT AREA =================
            int contentTop = tableTop + headerH;

            // SHIFT "ml/sec" LABELS UP
            int mlSecShiftAmount = 30; // Adjust this value to shift up more or less

            ctx.SetFont(font, 8.5f);
            ctx.SetTextColor(Color.Red);
            ctx.TextOutLeft(tableLeft + fSize.Width * 2, contentTop + fSize.Height / 2 - mlSecShiftAmount, "ml/sec");
            ctx.TextOutLeft(dividerX + fSize.Width * 2, contentTop + fSize.Height / 2 - mlSecShiftAmount, "ml/sec");

            int graphTop = contentTop + fSize.Height * 2;

            Rectangle leftGraphRect = new Rectangle(
                tableLeft + fSize.Width,
                graphTop - 25,
                colWidth - fSize.Width * 2,
                contentH - fSize.Height * 5
            );

            Rectangle rightGraphRect = new Rectangle(
                dividerX + fSize.Width,
                graphTop - 25,
                colWidth - fSize.Width * 2,
                contentH - fSize.Height * 5
            );

            UroflowLeftNomogram(ctx, g, leftGraphRect, fSize);
            UroflowRightNomogram(ctx, g, rightGraphRect, fSize);

            ctx.SetFont(font, 8.5f);
            ctx.SetTextColor(Color.Red);

            // SHIFT "Volume ml" LABELS UP
            int volumeShiftAmount = 30;

            int volumeY = contentTop + contentH - fSize.Height * 1 - volumeShiftAmount;
            DrawCenteredText(ctx, tableLeft + colWidth / 2, volumeY, "Volume ml");
            DrawCenteredText(ctx, dividerX + colWidth / 2, volumeY, "Volume ml");

            int textTop = rightGraphRect.Bottom + fSize.Height * 5;

            // ================= CHARACTERISTICS HEADER =================
            int rowH = fSize.Height + 10;
            int titleHeight = rowH + 10;
            int headerLeft = workRect.Left + fSize.Width - 4;
            int headerWidth = fSize.Width * 43;
            int headerTop = textTop - 10;

            ctx.SetPen(Color.Transparent, 0);

            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(headerBrush, new Rectangle(headerLeft, headerTop, headerWidth, titleHeight));
            }

            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(headerLeft + 12, headerTop + (titleHeight - fSize.Height) / 2, "CHARACTERISTICS");

            int lineGap = fSize.Height + 5;

            ctx.SetFont(font, 11f);
            ctx.SetTextColor(Color.Black);

            int y = textTop + fSize.Height + 30;
            y -= 10;

            // LEFT COLUMN - Volume and Flow Values
            ctx.TextOutLeft(workRect.Left + fSize.Width, y,
                $"Voided Volume : {Convert.ToInt32(_data.Voiding.VoidedVolume)} ml");
            y += lineGap;

            ctx.TextOutLeft(workRect.Left + fSize.Width, y,
                $"Peak Flow Rate : {Convert.ToInt32(_data.Voiding.PeakFlowRate)} ml/sec");
            y += lineGap;

            ctx.TextOutLeft(workRect.Left + fSize.Width, y,
                $"Average Flow Rate : {Convert.ToInt32(_data.Voiding.AverageFlowRate)} ml/sec");
            y += lineGap;

            y += fSize.Height;

            ctx.TextOutLeft(workRect.Left + fSize.Width, y,
                $"Post Voiding Residual (PVR) : {Convert.ToInt32(_data.Voiding.ComputedPostVoidResidual)} ml");

            // RIGHT COLUMN - Time Values
            int rightX = workRect.Left + workRect.Width / 2 + fSize.Width;
            y = textTop + fSize.Height + 30;

            y -= 10;

            ctx.TextOutLeft(rightX, y,
                $"Voiding Time : {_data.Voiding.VoidingTime:F0} secs");
            y += lineGap;

            ctx.TextOutLeft(rightX, y,
                $"Flow Time : {_data.Voiding.FlowTime:F0} secs");
            y += lineGap;

            ctx.TextOutLeft(rightX, y,
                $"Time To Peak : {_data.Voiding.TimeToPeakFlow:F0} secs");
            y += lineGap;

            // Note: DelayTime is not in your model, you may need to add it or calculate it
            ctx.TextOutLeft(rightX, y,
                $"Delay Time : {_data.Voiding.DelayTime:F0} secs");  // Not available in current model
            y += lineGap;

            // Note: IntervalTime is not in your model, you may need to add it or calculate it
            ctx.TextOutLeft(rightX, y,
                $"Interval Time : {_data.Voiding.IntervalTime:F0} secs");  // Not available in current model
            y += fSize.Height;

            int commentsTop = y + fSize.Height - 30;

            // ================= COMMENTS HEADER =================
            int commentsHeaderLeft = workRect.Left + fSize.Width - 4;
            int commentsHeaderWidth = fSize.Width * 43;
            int commentsHeaderTop = commentsTop - 20;

            // ================= CONCLUSIONS SECTION =================
            if (!string.IsNullOrWhiteSpace(_data.ResultConclusion))
            {
                int headerHeight = fSize.Height + 12;
                int left = commentsHeaderLeft;
                int y1 = commentsTop + fSize.Height + 20;

                // Gradient header background
                using (LinearGradientBrush headerBrush =
                    new LinearGradientBrush(
                        new Rectangle(left - 6, y1 - 6, commentsHeaderWidth, headerHeight),
                        Color.White,
                        //Color.RoyalBlue,
                        Color.FromArgb(210, 225, 240),
                        LinearGradientMode.Horizontal))
                {
                    ctx.FillRectangle(
                        headerBrush,
                        new Rectangle(left - 6, y1 - 6, commentsHeaderWidth, headerHeight)
                    );
                }

                // Header text
                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(left + 10, y1, "Conclusions");

                y1 += headerHeight + 6;

                // Conclusion text
                ctx.SetFont(font, 10f, FontStyle.Regular);
                ctx.SetTextColor(Color.Black);

                Rectangle conclRect = new Rectangle(
                    left,
                    y1,
                    commentsHeaderWidth - 20,
                    fSize.Height * 4
                );

                //y -= 10;

                string text = _data.ResultConclusion ?? string.Empty;

                // Maximum width available
                int maxWidth = commentsHeaderWidth - 150;

                // Break text into lines
                string[] words = text.Split(' ');
                string line = "";

                foreach (string word in words)
                {
                    string testLine = line.Length == 0 ? word : line + " " + word;
                    Size sz = ctx.MeasureText(testLine);

                    if (sz.Width > maxWidth)
                    {
                        ctx.TextOutLeft(left, y1, line);
                        y1 += fSize.Height;
                        line = word;
                    }
                    else
                    {
                        line = testLine;
                    }
                }

                // Print remaining line
                if (!string.IsNullOrEmpty(line))
                {
                    ctx.TextOutLeft(left, y1, line);
                    y1 += fSize.Height;
                }

                y = conclRect.Bottom + fSize.Height;
                y += 10;
            }

            //Reported by

            // ================= REPORTED BY =================
            //if (!string.IsNullOrWhiteSpace(_data?.ReportBy))
            //{
            //    int reportedByLeft = workRect.Left + fSize.Width;

            //    ctx.SetFont(font, 11f, FontStyle.Bold);

            //    // Build the full reported by text
            //    string reportedByText = "Reported By :- ";
            //    string doctorText = _data.ReportBy;

            //    if (!string.IsNullOrWhiteSpace(_data.DoctorDegree))
            //    {
            //        doctorText += $" ({_data.DoctorDegree})";
            //    }

            //    // Draw label in red
            //    ctx.SetTextColor(Color.Red);
            //    ctx.TextOutLeft(reportedByLeft, y, reportedByText);

            //    // Draw doctor info in black
            //    SizeF labelSize = ctx.MeasureText(reportedByText);
            //    ctx.SetTextColor(Color.Black);
            //    ctx.TextOutLeft(reportedByLeft + (int)labelSize.Width, y, doctorText);

            //    y += fSize.Height * 3;

            //    // Signature
            //    ctx.SetFont(font, 11f, FontStyle.Bold);
            //    ctx.SetTextColor(Color.Black);
            //    ctx.TextOutLeft(reportedByLeft, y, "Signature _____________");

            //    y += fSize.Height * 2;
            //}

            // ================= REPORTED BY =================
            if (!string.IsNullOrWhiteSpace(_data?.ReportBy))
            {
                int reportedByLeft = workRect.Left + fSize.Width;

                ctx.SetFont(font, 11f, FontStyle.Bold);

                // Build the full reported by text
                string reportedByText = "Reported By :- ";
                string doctorText = _data.ReportBy;

                if (!string.IsNullOrWhiteSpace(_data.DoctorDegree))
                {
                    doctorText += $" ({_data.DoctorDegree})";
                }

                // Draw label in red
                ctx.SetTextColor(Color.Red);
                ctx.TextOutLeft(reportedByLeft, y, reportedByText);

                // Calculate position for doctor name with proper gap
                SizeF labelSize = ctx.MeasureText(reportedByText);

                // Add extra gap after "Reported By :- "
                int extraGap = 25; // Additional pixels gap after the label

                // Draw doctor info in black with proper spacing
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(reportedByLeft + (int)labelSize.Width + extraGap, y, doctorText);

                // Calculate position for signature
                SizeF doctorTextSize = ctx.MeasureText(doctorText);
                int signatureSpacing = 400; // Space between doctor info and signature

                // Signature on the same line
                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(
                    reportedByLeft + (int)labelSize.Width + extraGap + (int)doctorTextSize.Width + signatureSpacing,
                    y,
                    "Signature _____________"
                );

                // Move y down for next elements (if any)
                y += fSize.Height * 2;
            }

            if (_data.CompanyName == true)
            {
                DrawFooter(ctx, g, rect, fSize, 0, _totalPages);
            }
         }


        //This code for two Small Graph means Nomogram ghraph show in table formate comment on 20-02-2026
        //private void DrawUroflowOnePage(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        //{
        //    string font = _data.FontFamily;
        //    int halfTop = rect.Top + rect.Height / 2;

        //    Rectangle workRect = new Rectangle(
        //        rect.Left,
        //        halfTop,
        //        rect.Width,
        //        rect.Bottom - halfTop
        //    );

        //    int topSpace = fSize.Height * 2;

        //    // ================= COLORS =================
        //    Color blue = Color.White;
        //    Brush blueBrush = new SolidBrush(blue);

        //    // ================= TABLE =================
        //    int tableWidth = fSize.Width * 43;
        //    int colWidth = tableWidth / 2;
        //    int headerH = fSize.Height * 1;
        //    int contentH = (int)(workRect.Height * 0.40);
        //    int tableLeft = workRect.Left + fSize.Width;
        //    int tableTop = workRect.Top + fSize.Height + topSpace + 40;

        //    // =====================================================
        //    // 🔷 GRADIENT HEADER : "Siroky Nomograms"
        //    // =====================================================
        //    int nomRowH = fSize.Height + 10;
        //    int nomTitleHeight = nomRowH + 10;
        //    int nomHeaderLeft = tableLeft;
        //    int nomHeaderWidth = tableWidth;
        //    int nomHeaderTop = tableTop - nomTitleHeight - 15;

        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush nomHeaderBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            nomHeaderBrush,
        //            new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight)
        //        );
        //    }

        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);
        //    ctx.TextOutLeft(
        //        nomHeaderLeft + 12,
        //        nomHeaderTop + (nomTitleHeight - fSize.Height) / 2,
        //        "Siroky Nomograms"
        //    );

        //    Rectangle tableRect = new Rectangle(
        //        tableLeft,
        //        tableTop,
        //        tableWidth,
        //        headerH + contentH
        //    );

        //    // ================= TABLE BORDER =================
        //    ctx.SetPen(Color.Black, 1);
        //    ctx.Rectangle(tableRect);

        //    int dividerX = tableLeft + colWidth;

        //    using (Pen pen = new Pen(Color.Black, 1))
        //    {
        //        g.DrawLine(pen, dividerX, tableTop, dividerX, tableRect.Bottom);
        //    }

        //    // ================= CONTENT AREA =================
        //    int contentTop = tableTop + headerH;

        //    ctx.SetFont(font, 8.5f);
        //    ctx.SetTextColor(Color.Red);
        //    ctx.TextOutLeft(tableLeft + fSize.Width * 2, contentTop + fSize.Height / 2, "ml/sec");
        //    ctx.TextOutLeft(dividerX + fSize.Width * 2, contentTop + fSize.Height / 2, "ml/sec");

        //    int graphTop = contentTop + fSize.Height * 2;

        //    Rectangle leftGraphRect = new Rectangle(
        //        tableLeft + fSize.Width,
        //        graphTop,
        //        colWidth - fSize.Width * 2,
        //        contentH - fSize.Height * 5
        //    );

        //    Rectangle rightGraphRect = new Rectangle(
        //        dividerX + fSize.Width,
        //        graphTop,
        //        colWidth - fSize.Width * 2,
        //        contentH - fSize.Height * 5
        //    );

        //    UroflowLeftNomogram(ctx, g, leftGraphRect, fSize);
        //    UroflowRightNomogram(ctx, g, rightGraphRect, fSize);

        //    ctx.SetFont(font, 8.5f);
        //    ctx.SetTextColor(Color.Red);
        //    int volumeY = contentTop + contentH - fSize.Height * 1;
        //    DrawCenteredText(ctx, tableLeft + colWidth / 2, volumeY, "Volume ml");
        //    DrawCenteredText(ctx, dividerX + colWidth / 2, volumeY, "Volume ml");

        //    int textTop = rightGraphRect.Bottom + fSize.Height * 5;

        //    // ================= CHARACTERISTICS HEADER =================
        //    int rowH = fSize.Height + 10;
        //    int titleHeight = rowH + 10;
        //    int headerLeft = workRect.Left + fSize.Width - 4;
        //    int headerWidth = fSize.Width * 43;
        //    int headerTop = textTop - 10;

        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(headerBrush, new Rectangle(headerLeft, headerTop, headerWidth, titleHeight));
        //    }

        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);
        //    ctx.TextOutLeft(headerLeft + 12, headerTop + (titleHeight - fSize.Height) / 2, "CHARACTERISTICS");

        //    int lineGap = fSize.Height + 5;

        //    ctx.SetFont(font, 11f);
        //    ctx.SetTextColor(Color.Black);

        //    int y = textTop + fSize.Height + 30;

        //    // LEFT COLUMN - Volume and Flow Values
        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Voided Volume : {_data.Voiding.VoidedVolume:F1} ml");
        //    y += lineGap;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Peak Flow Rate : {_data.Voiding.PeakFlowRate:F1} ml/sec");
        //    y += lineGap;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Average Flow Rate : {_data.Voiding.AverageFlowRate:F1} ml/sec");
        //    y += lineGap;

        //    y += fSize.Height;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Post Voiding Residual (PVR) : {_data.Voiding.ComputedPostVoidResidual:F1} ml");

        //    // RIGHT COLUMN - Time Values
        //    int rightX = workRect.Left + workRect.Width / 2 + fSize.Width;
        //    y = textTop + fSize.Height + 30;

        //    ctx.TextOutLeft(rightX, y,
        //        $"Voiding Time : {_data.Voiding.VoidingTime:F0} secs");
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //        $"Flow Time : {_data.Voiding.FlowTime:F0} secs");
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //        $"Time To Peak : {_data.Voiding.TimeToPeakFlow:F0} secs");
        //    y += lineGap;

        //    // Note: DelayTime is not in your model, you may need to add it or calculate it
        //    ctx.TextOutLeft(rightX, y,
        //        $"Delay Time : {_data.Voiding.DelayTime:F0} secs");  // Not available in current model
        //    y += lineGap;

        //    // Note: IntervalTime is not in your model, you may need to add it or calculate it
        //    ctx.TextOutLeft(rightX, y,
        //        $"Interval Time : {_data.Voiding.IntervalTime:F0} secs");  // Not available in current model
        //    y += fSize.Height;

        //    int commentsTop = y + fSize.Height + 30;

        //    // ================= COMMENTS HEADER =================
        //    int commentsHeaderLeft = workRect.Left + fSize.Width - 4;
        //    int commentsHeaderWidth = fSize.Width * 43;
        //    int commentsHeaderTop = commentsTop - 20;

        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(commentsHeaderLeft, commentsHeaderTop, commentsHeaderWidth, titleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(headerBrush,
        //            new Rectangle(commentsHeaderLeft, commentsHeaderTop, commentsHeaderWidth, titleHeight));
        //    }

        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);
        //    ctx.TextOutLeft(commentsHeaderLeft + 12, commentsHeaderTop + (titleHeight - fSize.Height) / 2, "COMMENTS");

        //    ctx.SetFont(font, 11f);
        //    ctx.SetTextColor(Color.Black);

        //    int y1 = commentsTop + fSize.Height + 20;


        //    if (_data.CompanyName == true)
        //    {
        //        DrawFooter(ctx, g, rect, fSize, 0, _totalPages);
        //    }
        //}

        //private void DrawUroflowOnePage(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        //{

        //    string font = _data.FontFamily;

        //    int halfTop = rect.Top + rect.Height / 2;

        //    Rectangle workRect = new Rectangle(
        //        rect.Left,
        //        halfTop,
        //        rect.Width,
        //        rect.Bottom - halfTop
        //    );

        //    int topSpace = fSize.Height * 2;




        //    // ================= COLORS =================
        //    Color blue = Color.White;
        //    Brush blueBrush = new SolidBrush(blue);

        //    // ================= TABLE =================

        //    int tableWidth = fSize.Width * 43;
        //    int colWidth = tableWidth / 2;

        //    int headerH = fSize.Height * 1;
        //    int contentH = (int)(workRect.Height * 0.40);


        //    int tableLeft = workRect.Left + fSize.Width;
        //    int tableTop = workRect.Top + fSize.Height + topSpace + 40;


        //    // =====================================================
        //    // 🔷 GRADIENT HEADER : "Nomogram Graph"
        //    // =====================================================

        //    int nomRowH = fSize.Height + 10;
        //    int nomTitleHeight = nomRowH + 10;

        //    int nomHeaderLeft = tableLeft;
        //    int nomHeaderWidth = tableWidth;
        //    int nomHeaderTop = tableTop - nomTitleHeight - 15;

        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush nomHeaderBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            nomHeaderBrush,
        //            new Rectangle(nomHeaderLeft, nomHeaderTop, nomHeaderWidth, nomTitleHeight)
        //        );
        //    }

        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);

        //    ctx.TextOutLeft(
        //        nomHeaderLeft + 12,
        //        nomHeaderTop + (nomTitleHeight - fSize.Height) / 2,
        //        "Siroky Nomograms"
        //    );



        //    Rectangle tableRect = new Rectangle(
        //        tableLeft,
        //        tableTop,
        //        tableWidth,
        //        headerH + contentH
        //    );

        //    // ================= TABLE BORDER =================

        //    ctx.SetPen(Color.Black, 1);
        //    ctx.Rectangle(tableRect);

        //    int dividerX = tableLeft + colWidth;

        //    using (Pen pen = new Pen(Color.Black, 1))
        //    {
        //        // vertical divider
        //        g.DrawLine(pen, dividerX, tableTop, dividerX, tableRect.Bottom);

        //        // header separator
        //        //g.DrawLine(pen, tableLeft, tableTop + headerH, tableRect.Right, tableTop + headerH);
        //    }


        //    // ================= CONTENT AREA =================
        //    int contentTop = tableTop + headerH;

        //    // ---- ml/sec (TOP of content)
        //    ctx.SetFont(font, 8.5f);
        //    ctx.SetTextColor(Color.Red);

        //    ctx.TextOutLeft(
        //        tableLeft + fSize.Width * 2,
        //        contentTop + fSize.Height / 2,
        //        "ml/sec"
        //    );

        //    ctx.TextOutLeft(
        //        dividerX + fSize.Width * 2,
        //        contentTop + fSize.Height / 2,
        //        "ml/sec"
        //    );

        //    // ---- Graph rectangles (MIDDLE of content)
        //    int graphTop = contentTop + fSize.Height * 2;

        //    Rectangle leftGraphRect = new Rectangle(
        //        tableLeft + fSize.Width,
        //        graphTop,
        //        colWidth - fSize.Width * 2,
        //        contentH - fSize.Height * 5
        //    );

        //    Rectangle rightGraphRect = new Rectangle(
        //        dividerX + fSize.Width,
        //        graphTop,
        //        colWidth - fSize.Width * 2,
        //        contentH - fSize.Height * 5
        //    );

        //    UroflowLeftNomogram(ctx, g, leftGraphRect, fSize);
        //    UroflowRightNomogram(ctx, g, rightGraphRect, fSize);

        //    // ---- Volume ml (BOTTOM of content)
        //    ctx.SetFont(font, 8.5f);
        //    ctx.SetTextColor(Color.Red);

        //    int volumeY = contentTop + contentH - fSize.Height * 1;

        //    DrawCenteredText(
        //        ctx,
        //        tableLeft + colWidth / 2,
        //        volumeY,
        //        "Volume ml"
        //    );

        //    DrawCenteredText(
        //        ctx,
        //        dividerX + colWidth / 2,
        //        volumeY,
        //        "Volume ml"
        //    );

        //    int textTop = rightGraphRect.Bottom + fSize.Height * 5;

        //    // CHARACTERISTICS
        //    //ctx.SetFont(font, 12f, FontStyle.Bold);
        //    //ctx.SetTextColor(Color.Red);
        //    //ctx.TextOutLeft(workRect.Left + fSize.Width, textTop, "CHARACTERISTICS");

        //    // ================= CHARACTERISTICS HEADER (Styled) =================

        //    int rowH = fSize.Height + 10;
        //    int titleHeight = rowH + 10;

        //    int headerLeft = workRect.Left + fSize.Width - 4;
        //    int headerWidth = fSize.Width * 43;   // adjust if needed
        //    int headerTop = textTop -10;

        //    // remove border
        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            headerBrush,
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
        //        );
        //    }

        //    // Header text
        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);

        //    ctx.TextOutLeft(
        //        headerLeft + 12,
        //        headerTop + (titleHeight - fSize.Height) / 2,
        //        "CHARACTERISTICS"
        //    );

        //    int lineGap = fSize.Height + 5;   // 👈 middle spacing (tune 4–8)
        //    int sectionGap = fSize.Height + 18; // 👈 bigger gap when needed

        //    ctx.SetFont(font, 11f);
        //    ctx.SetTextColor(Color.Black);

        //    int y = textTop + fSize.Height + 30;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Voided Volume : {_data.Voiding.VoidedVolume:F1} ml");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Peak Flow Rate : {_data.Voiding.PeakFlowRate:F1} ml/sec");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //        $"Average Flow Rate : {_data.Voiding.AverageFlowRate:F1} ml/sec");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    y += fSize.Height;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y,
        //     $"Post Voiding Residual (PVR)  : ________ ml");

        //    // TIME VALUES (RIGHT)
        //    int rightX = workRect.Left + workRect.Width / 2 + fSize.Width;
        //    y = textTop + fSize.Height + 30;

        //    ctx.TextOutLeft(rightX, y,
        //        $"Voiding Time : {_data.Voiding.VoidingTime:F0} secs");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //       $"Flow Time : {_data.Voiding.VoidingTime:F0} secs");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //       $"Time To Peak : {_data.Voiding.VoidingTime:F0} secs");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //       $"Delay Time : {_data.Voiding.VoidingTime:F0} secs");
        //    //y += fSize.Height;
        //    y += lineGap;

        //    ctx.TextOutLeft(rightX, y,
        //       $"Interval Time : {_data.Voiding.VoidingTime:F0} secs");
        //    y += fSize.Height;



        //    int commentsTop = y + fSize.Height + 30;

        //    // COMMENTS
        //    //ctx.SetFont(font, 12f, FontStyle.Bold);
        //    //ctx.SetTextColor(Color.Red);
        //    //ctx.TextOutLeft(workRect.Left + fSize.Width, commentsTop, "COMMENTS");

        //    // ================= COMMENTS HEADER (Styled) =================

        //    //int rowH = fSize.Height + 10;
        //    //int titleHeight = rowH + 10;

        //    int commentsHeaderLeft = workRect.Left + fSize.Width - 4;
        //    int commentsHeaderWidth = fSize.Width * 43;
        //    int commentsHeaderTop = commentsTop - 20;

        //    // remove border
        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(commentsHeaderLeft, commentsHeaderTop, commentsHeaderWidth, titleHeight),
        //            Color.White,
        //            Color.FromArgb(210, 225, 240),
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            headerBrush,
        //            new Rectangle(commentsHeaderLeft, commentsHeaderTop, commentsHeaderWidth, titleHeight)
        //        );
        //    }

        //    // Header text
        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);

        //    ctx.TextOutLeft(
        //        commentsHeaderLeft + 12,
        //        commentsHeaderTop + (titleHeight - fSize.Height) / 2,
        //        "COMMENTS"
        //    );

        //    // move content below header
        //    //int y1 = commentsHeaderTop + titleHeight + fSize.Height;


        //    ctx.SetFont(font, 11f);
        //    ctx.SetTextColor(Color.Black);




        //    int y1 = commentsTop + fSize.Height + 20;

        //    ctx.TextOutLeft(workRect.Left + fSize.Width, y1,
        //        $"Comments data : {_data.Voiding.VoidedVolume:F1} ml");
        //    y1 += fSize.Height;
        //    y1 += fSize.Height;
        //    y1 += fSize.Height;


        //    if (_data.CompanyName == true)
        //    {
        //        //FooterBlueText(ctx, g, rect, fSize);
        //        DrawFooter(ctx, g, rect, fSize,0,_totalPages);
        //    }
        //    else
        //    {

        //    }
        //}



        //Small Left Side Graph code
        private void UroflowLeftNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            EnsureVoidingCalculated();
            string font = _data.FontFamily;

            // ----------- layout: make inner area nearly square -----------
            int margin = fSize.Width * 1;

            int innerLeft = rect.Left + 50;
            int innerTop = rect.Top + 2;
            int innerRight = rect.Right;
            int innerBottom = rect.Bottom;

            int innerWidth = innerRight - innerLeft;
            int innerHeight = innerBottom - innerTop;

            // keep height as-is
            int graphHeight = innerHeight;

            // increase width (1.25x or 1.3x looks good)
            int graphWidth = (int)(innerWidth * 0.75);

            Rectangle box = new Rectangle(
                innerLeft,
                innerTop,
                graphWidth,
                graphHeight
            );


            // ----------- title at top (same line, shifted up) -----------
            ctx.SetFont(font, 8f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // move title ABOVE the box
            int titleY = box.Top - fSize.Height - 15;

            // "ml/sec"
            ctx.TextOutLeft(
                box.Left,
                titleY,
                ""
            );

            // "Av.flow Nomogram" on same line
            ctx.TextOutLeft(
                box.Left + fSize.Width * 3,
                titleY,
                "Average flow Nomogram"
            );


            // ----------- outer box -----------
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(box);

            // ----------- axes ranges & mapping -----------
            // X 0..500 (volume), Y 0..30 (flow)
            double xMin = 0.0, xMax = 500.0;
            double yMin = 0.0, yMax = 30.0;

            float X(double v) => (float)(box.Left + (v - xMin) / (xMax - xMin) * box.Width);
            float Y(double f) => (float)(box.Bottom - (f - yMin) / (yMax - yMin) * box.Height);

            // ----------- dashed grid -----------
            using (var gridPen = new Pen(Color.LightGray, 1f))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                // vertical grid 0,100,...,500
                for (int v = 100; v <= 500; v += 100)
                {
                    float x = X(v);
                    g.DrawLine(gridPen, x, box.Top, x, box.Bottom);
                }

                // horizontal grid 5,10,15,20,25
                for (int f = 5; f < 30; f += 5)
                {
                    float y = Y(f);
                    g.DrawLine(gridPen, box.Left, y, box.Right, y);
                }
            }

            // ----------- axes numeric labels -----------
            ctx.SetFont(font, 8f, FontStyle.Regular);

            // Y numbers (red): 0..30 on left
            ctx.SetTextColor(Color.Red);
            for (int f = 0; f <= 30; f += 5)
            {
                float y = Y(f);
                string s = f.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutRight(box.Left - 3, (int)(y - sz.Height / 2), s);
            }

            // X numbers (blue): 0,100,...,500
            ctx.SetTextColor(Color.Blue);
            for (int v = 0; v <= 500; v += 100)
            {
                float x = X(v);
                string s = v.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutLeft((int)(x - sz.Width / 2), box.Bottom + fSize.Height / 4, s);
            }


            // ----------- curves (hard-coded shape, visually matched) -----------
            using (var blackPen = new Pen(Color.Black, 1f))
            using (var bluePen = new Pen(Color.Blue, 1f))
            {
                double[] xs = { 0, 100, 200, 300, 400, 500 };

                // approximate Y values (ml/sec) for each curve
                double[] yPlus1 = { 12, 17, 21, 24, 27, 29 };
                double[] y0 = { 10, 15, 19, 22, 24, 26 };
                double[] yMinus1 = { 8, 13, 16, 19, 21, 23 };
                double[] yMinus2 = { 5, 8, 10, 11.5, 12.5, 13 }; // blue
                double[] yMinus3 = { 2, 4, 6, 7.5, 8.5, 9.5 };

                void DrawCurve(double[] ys, Pen pen)
                {
                    PointF[] pts = new PointF[xs.Length];
                    for (int i = 0; i < xs.Length; i++)
                        pts[i] = new PointF(X(xs[i]), Y(ys[i]));
                    g.DrawLines(pen, pts);
                }

                DrawCurve(yPlus1, blackPen);
                DrawCurve(y0, blackPen);
                DrawCurve(yMinus1, blackPen);
                DrawCurve(yMinus3, blackPen);
                DrawCurve(yMinus2, bluePen); // blue curve (-2)
            }

            // ----------- X-axis label at bottom center -----------
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Red);

            //string xLabel = "Volume ml";
            //Size xlSize = ctx.MeasureText(xLabel);

            //ctx.TextOutLeft(
            //    box.Left + (box.Width - xlSize.Width) / 2,   // center horizontally
            //    box.Bottom + fSize.Height * 1,                // below x-axis numbers
            //    xLabel
            //);


            // ----------- right-side labels (+1,0 mean,-1,-2,-3) -----------
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // positions near right edge of box
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(26), "+1");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(22), "0 mean");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(18), "-1");
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(12), "-2");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(8), "-3");


            // -----------------------------
            // Patient red dot (VoidedVolume vs AverageFlowRate)
            // -----------------------------
            double vv = _data?.Voiding?.VoidedVolume ?? 0.0;
            double qavg = _data?.Voiding?.AverageFlowRate ?? 0.0;

            if (double.IsNaN(vv)) vv = 0;
            if (double.IsNaN(qavg)) qavg = 0;

            // clamp to visible range of this nomogram
            vv = Math.Max(0, Math.Min(500, vv));
            qavg = Math.Max(0, Math.Min(30, qavg));

            float px = X(vv);
            float py = Y(qavg);

            float radius = Math.Max(4f, fSize.Width * 0.2f);

            using (Brush redBrush = new SolidBrush(Color.Red))
            using (Pen redPen = new Pen(Color.Red, 1f))
            {
                g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
                g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
            }

        }

        //private void UroflowLeftNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        //{
        //    EnsureVoidingCalculated();
        //    string font = _data.FontFamily;

        //    // ----------- layout: make inner area nearly square -----------
        //    int margin = fSize.Width * 1;

        //    int innerLeft = rect.Left + 50;
        //    int innerTop = rect.Top + 2;
        //    int innerRight = rect.Right;
        //    int innerBottom = rect.Bottom;

        //    int innerWidth = innerRight - innerLeft;
        //    int innerHeight = innerBottom - innerTop;

        //    // keep height as-is
        //    int graphHeight = innerHeight;

        //    // increase width (1.25x or 1.3x looks good)
        //    int graphWidth = (int)(innerWidth * 0.75);

        //    Rectangle box = new Rectangle(
        //        innerLeft,
        //        innerTop,
        //        graphWidth,
        //        graphHeight
        //    );

        //    // ----------- SHIFT THE GRAPH UP -----------
        //    // Decrease this value to shift up more
        //    int shiftUpAmount = 25; // Adjust this value as needed

        //    // Create new box with shifted position
        //    Rectangle shiftedBox = new Rectangle(
        //        box.Left,
        //        box.Top - shiftUpAmount,  // Subtract to move up
        //        box.Width,
        //        box.Height
        //    );

        //    // ----------- title at top (same line, shifted up) -----------
        //    ctx.SetFont(font, 8f, FontStyle.Regular);
        //    ctx.SetTextColor(Color.Black);

        //    // move title ABOVE the box - also shift this up
        //    int titleY = shiftedBox.Top - fSize.Height - 15;

        //    // "ml/sec"
        //    ctx.TextOutLeft(
        //        shiftedBox.Left,
        //        titleY,
        //        ""
        //    );

        //    // "Av.flow Nomogram" on same line
        //    ctx.TextOutLeft(
        //        shiftedBox.Left + fSize.Width * 3,
        //        titleY,
        //        "Average flow Nomogram"
        //    );

        //    // ----------- outer box (use shiftedBox) -----------
        //    ctx.SetPen(Color.Black, 1);
        //    ctx.Rectangle(shiftedBox);

        //    // ----------- axes ranges & mapping (use shiftedBox) -----------
        //    // X 0..500 (volume), Y 0..30 (flow)
        //    double xMin = 0.0, xMax = 500.0;
        //    double yMin = 0.0, yMax = 30.0;

        //    float X(double v) => (float)(shiftedBox.Left + (v - xMin) / (xMax - xMin) * shiftedBox.Width);
        //    float Y(double f) => (float)(shiftedBox.Bottom - (f - yMin) / (yMax - yMin) * shiftedBox.Height);

        //    // ----------- dashed grid (use shiftedBox) -----------
        //    using (var gridPen = new Pen(Color.LightGray, 1f))
        //    {
        //        gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

        //        // vertical grid 0,100,...,500
        //        for (int v = 100; v <= 500; v += 100)
        //        {
        //            float x = X(v);
        //            g.DrawLine(gridPen, x, shiftedBox.Top, x, shiftedBox.Bottom);
        //        }

        //        // horizontal grid 5,10,15,20,25
        //        for (int f = 5; f < 30; f += 5)
        //        {
        //            float y = Y(f);
        //            g.DrawLine(gridPen, shiftedBox.Left, y, shiftedBox.Right, y);
        //        }
        //    }

        //    // ----------- axes numeric labels (use shiftedBox) -----------
        //    ctx.SetFont(font, 8f, FontStyle.Regular);

        //    // Y numbers (red): 0..30 on left
        //    ctx.SetTextColor(Color.Red);
        //    for (int f = 0; f <= 30; f += 5)
        //    {
        //        float y = Y(f);
        //        string s = f.ToString();
        //        SizeF sz = ctx.MeasureText(s);
        //        ctx.TextOutRight(shiftedBox.Left - 3, (int)(y - sz.Height / 2), s);
        //    }

        //    // X numbers (blue): 0,100,...,500
        //    ctx.SetTextColor(Color.Blue);
        //    for (int v = 0; v <= 500; v += 100)
        //    {
        //        float x = X(v);
        //        string s = v.ToString();
        //        SizeF sz = ctx.MeasureText(s);
        //        ctx.TextOutLeft((int)(x - sz.Width / 2), shiftedBox.Bottom + fSize.Height / 4, s);
        //    }

        //    // ----------- curves (hard-coded shape, visually matched) -----------
        //    using (var blackPen = new Pen(Color.Black, 1f))
        //    using (var bluePen = new Pen(Color.Blue, 1f))
        //    {
        //        double[] xs = { 0, 100, 200, 300, 400, 500 };

        //        // approximate Y values (ml/sec) for each curve
        //        double[] yPlus1 = { 12, 17, 21, 24, 27, 29 };
        //        double[] y0 = { 10, 15, 19, 22, 24, 26 };
        //        double[] yMinus1 = { 8, 13, 16, 19, 21, 23 };
        //        double[] yMinus2 = { 5, 8, 10, 11.5, 12.5, 13 }; // blue
        //        double[] yMinus3 = { 2, 4, 6, 7.5, 8.5, 9.5 };

        //        void DrawCurve(double[] ys, Pen pen)
        //        {
        //            PointF[] pts = new PointF[xs.Length];
        //            for (int i = 0; i < xs.Length; i++)
        //                pts[i] = new PointF(X(xs[i]), Y(ys[i]));
        //            g.DrawLines(pen, pts);
        //        }

        //        DrawCurve(yPlus1, blackPen);
        //        DrawCurve(y0, blackPen);
        //        DrawCurve(yMinus1, blackPen);
        //        DrawCurve(yMinus3, blackPen);
        //        DrawCurve(yMinus2, bluePen); // blue curve (-2)
        //    }

        //    // ----------- right-side labels (+1,0 mean,-1,-2,-3) -----------
        //    ctx.SetFont(font, 8.5f, FontStyle.Regular);
        //    ctx.SetTextColor(Color.Black);

        //    // positions near right edge of box
        //    ctx.TextOutLeft(shiftedBox.Right + fSize.Width / 2, (int)Y(26), "+1");
        //    ctx.TextOutLeft(shiftedBox.Right + fSize.Width / 2, (int)Y(22), "0 mean");
        //    ctx.TextOutLeft(shiftedBox.Right + fSize.Width / 2, (int)Y(18), "-1");
        //    ctx.SetTextColor(Color.Blue);
        //    ctx.TextOutLeft(shiftedBox.Right + fSize.Width / 2, (int)Y(12), "-2");
        //    ctx.SetTextColor(Color.Black);
        //    ctx.TextOutLeft(shiftedBox.Right + fSize.Width / 2, (int)Y(8), "-3");

        //    // -----------------------------
        //    // Patient red dot (VoidedVolume vs AverageFlowRate)
        //    // -----------------------------
        //    double vv = _data?.Voiding?.VoidedVolume ?? 0.0;
        //    double qavg = _data?.Voiding?.AverageFlowRate ?? 0.0;

        //    if (double.IsNaN(vv)) vv = 0;
        //    if (double.IsNaN(qavg)) qavg = 0;

        //    // clamp to visible range of this nomogram
        //    vv = Math.Max(0, Math.Min(500, vv));
        //    qavg = Math.Max(0, Math.Min(30, qavg));

        //    float px = X(vv);
        //    float py = Y(qavg);

        //    float radius = Math.Max(4f, fSize.Width * 0.2f);

        //    using (Brush redBrush = new SolidBrush(Color.Red))
        //    using (Pen redPen = new Pen(Color.Red, 1f))
        //    {
        //        g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
        //        g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
        //    }
        //}

        //Small Right Side Graph code
        private void UroflowRightNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            EnsureVoidingCalculated();
            string font = _data.FontFamily;

            int margin = fSize.Width * 1;

            int innerLeft = rect.Left + 50;
            int innerTop = rect.Top + 2;
            int innerRight = rect.Right;
            int innerBottom = rect.Bottom;

            int innerWidth = innerRight - innerLeft;
            int innerHeight = innerBottom - innerTop;

            // keep height as-is
            int graphHeight = innerHeight;

            // increase width (1.25x or 1.3x looks good)
            int graphWidth = (int)(innerWidth * 0.75);

            Rectangle box = new Rectangle(
                innerLeft,
                innerTop,
                graphWidth,
                graphHeight
            );

            // Title
            ctx.SetFont(font, 8f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // move title ABOVE the box
            int titleY = box.Top - fSize.Height - 15;

            // "ml/sec"
            ctx.TextOutLeft(
                box.Left,
                titleY,
                ""
            );

            // "Av.flow Nomogram" on same line
            ctx.TextOutLeft(
                box.Left + fSize.Width * 3,
                titleY,
                "Max. flow Nomogram"
            );

            // Outer box
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(box);

            // Axes ranges
            double xMin = 0.0, xMax = 500.0;
            double yMin = 0.0, yMax = 30.0;

            float X(double v) => (float)(box.Left + (v - xMin) / (xMax - xMin) * box.Width);
            float Y(double f) => (float)(box.Bottom - (f - yMin) / (yMax - yMin) * box.Height);

            // Grid
            using (var gridPen = new Pen(Color.LightGray, 1f))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                for (int v = 100; v <= 500; v += 100)
                    g.DrawLine(gridPen, X(v), box.Top, X(v), box.Bottom);

                for (int f = 5; f < 30; f += 5)
                    g.DrawLine(gridPen, box.Left, Y(f), box.Right, Y(f));
            }

            // Y numbers (red)
            ctx.SetFont(font, 8f, FontStyle.Regular);
            ctx.SetTextColor(Color.Red);
            for (int f = 0; f <= 30; f += 5)
            {
                float y = Y(f);
                string s = f.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutRight(box.Left - 3, (int)(y - sz.Height / 2), s);
            }

            // X numbers (blue)
            ctx.SetTextColor(Color.Blue);
            for (int v = 0; v <= 500; v += 100)
            {
                float x = X(v);
                string s = v.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutLeft((int)(x - sz.Width / 2), box.Bottom + fSize.Height / 4, s);
            }

            // Curves
            using (var blackPen = new Pen(Color.Black, 1f))
            using (var bluePen = new Pen(Color.Blue, 1f))
            {
                double[] xs = { 0, 100, 200, 300, 400, 500 };

                // approximate Y values (ml/sec)
                double[] y0 = { 8, 14, 21, 26, 29, 30 };
                double[] yMinus1 = { 6, 11, 17, 21, 24, 26 };
                double[] yMinus2 = { 5, 9, 13, 16, 18, 19.5 }; // blue
                double[] yMinus3 = { 3, 5.5, 8, 10, 11.5, 13 };

                PointF[] pts = new PointF[xs.Length];

                void DrawCurve(double[] ys, Pen pen)
                {
                    for (int i = 0; i < xs.Length; i++)
                        pts[i] = new PointF(X(xs[i]), Y(ys[i]));
                    g.DrawLines(pen, pts);
                }

                DrawCurve(y0, blackPen);
                DrawCurve(yMinus1, blackPen);
                DrawCurve(yMinus3, blackPen);
                DrawCurve(yMinus2, bluePen);
            }

            // ----------- X-axis label at bottom center -----------
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Red);

            //string xLabel = "Volume ml";
            //Size xlSize = ctx.MeasureText(xLabel);

            //ctx.TextOutLeft(
            //    box.Left + (box.Width - xlSize.Width) / 2,   // center horizontally
            //    box.Bottom + fSize.Height * 1,                // below x-axis numbers
            //    xLabel
            //);

            // right-side labels: 0, -1 mean, -2, -3
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(27), "0");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(22), "-1 mean");
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(16), "-2");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(10), "-3");

            // -----------------------------
            // Patient red dot (VoidedVolume vs PeakFlowRate)
            // -----------------------------
            double vv = _data?.Voiding?.VoidedVolume ?? 0.0;
            double qmax = _data?.Voiding?.PeakFlowRate ?? 0.0;

            if (double.IsNaN(vv)) vv = 0;
            if (double.IsNaN(qmax)) qmax = 0;

            // clamp to visible range of this nomogram
            vv = Math.Max(0, Math.Min(500, vv));
            qmax = Math.Max(0, Math.Min(30, qmax));

            float px = X(vv);
            float py = Y(qmax);

            float radius = Math.Max(4f, fSize.Width * 0.2f);

            using (Brush redBrush = new SolidBrush(Color.Red))
            using (Pen redPen = new Pen(Color.Red, 1f))
            {
                g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
                g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
            }

        }
        //End Code for Uroflowmetry & Uroflowmetry + EMG Test 




        //UPP Test Page For Single Page Test

        //Start Code for UPP & Whitaker Test
        private void DrawUPPTestPage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            // header area + space for results
            int topHeaderSpace = fSize.Height * 15;
            int bottomResultsSpace = fSize.Height * 22;

            int graphTop = rect.Top + topHeaderSpace + 30;
            int graphHeight = rect.Height - topHeaderSpace - bottomResultsSpace;
            if (graphHeight < fSize.Height * 5)
                graphHeight = fSize.Height * 5;
            int labelBoxWidth = fSize.Width * 4;

            Rectangle labelRect = new Rectangle(
                rect.Left + fSize.Width,
                graphTop,
                labelBoxWidth,
                graphHeight
            );

            int graphLeft = labelRect.Right;
            int graphRight = rect.Right - fSize.Width;
            int graphWidth = Math.Max(graphRight - graphLeft, fSize.Width * 5);

            Rectangle gRect = new Rectangle(
                graphLeft,
                graphTop,
                graphWidth,
                graphHeight
            );

            //int curvesBottomMargin = fSize.Height * 4;
            //Rectangle curvesRect = new Rectangle(
            //    gRect.Left,
            //    gRect.Top,
            //    gRect.Width,
            //    Math.Max(gRect.Height - curvesBottomMargin, fSize.Height * 3)
            //);

            // Curves must fill the full graph frame.
            // Time axis is drawn BELOW the graph by DrawTimeAxisBottom, so reserving space here creates blank area.
            Rectangle curvesRect = gRect;

            //DrawHeaderBlock(ctx, rect, fSize);

            if (_data.HeadOne == true)
            {
                if (_data.DefaultHeader == true)
                {
                    DrawHeaderBlock(ctx, rect, fSize);
                }
                else if (_data.LetterHead == true)
                {
                    DrawLatterHeadImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalLogo == true)
                {
                    DrawHeaderLogoImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalDataAndLogo == true)
                {
                    DrawHeaderBlockWithLogoLeft(ctx, g, rect, fSize);
                }
            }
            else
            {

            }

            DrawPatientBlock(ctx, g, rect, fSize);

            DrawFillingAxes(ctx, g, labelRect, curvesRect, fSize);

            Region oldClip = g.Clip;
            g.SetClip(gRect);

            //Start UPP Test S1 S2 under graph show Only 

            //if (TryGetS1S2Window(out double t1, out double t2))
            //{
            //    DrawFillingCurvesFromRecorded(g, curvesRect, t1, t2);
            //    DrawMarkersOnGraph(g, curvesRect, t1, t2);
            //}
            //else
            //{
            //    DrawFillingCurvesFromRecorded(g, curvesRect);
            //    DrawMarkersOnGraph(g, curvesRect);
            //}

            //End UPP Test S1 S2 under graph show Only 

            //START Show Complete Graph S1 S2 outer part also show
            DrawFillingCurvesFromRecorded(g, curvesRect);
            DrawMarkersOnGraph(g, curvesRect);
            //END Show Complete Graph S1 S2 outer part also show


            g.SetClip(oldClip, CombineMode.Replace);

            //DrawTimeAxisBottom(g, gRect);
            if (TryGetS1S2Window(out double t1Axis, out double t2Axis))
                DrawTimeAxisBottom(g, gRect, t1Axis, t2Axis);
            else
                DrawTimeAxisBottom(g, gRect);

            int resultsTop = gRect.Bottom + fSize.Height * 2;
            DrawUPPTestText(ctx, rect.Left, resultsTop, fSize);
            if (_data.CompanyName == true)
            {
                //FooterBlueText(ctx, g, rect, fSize);
                DrawFooter(ctx, g, rect, fSize, 0, _totalPages);
            }
            else
            {

            }


        }

        //Start Code for UPP Test Calculation for Lura means "Functional Profile Length" 29-01-2026
        private int GetLuraLaneIndex()
        {
            // In C++ sample[][3] is LURA
            // But in C# we must resolve by name safely
            return FindLaneIndex("lura");
        }
        private double GetFunctionalProfileLengthCm_CppMatched()
        {
            if (_graph?.Samples == null || _graph.Markers == null)
                return 0;

            int luraIdx = GetLuraLaneIndex();
            if (luraIdx < 0)
                return 0;

            double? s1Lura = null;
            double? s2Lura = null;

            foreach (var m in _graph.Markers)
            {
                if (string.IsNullOrWhiteSpace(m.Label))
                    continue;

                // Convert marker time → sample index (same as C++)
                int i = FindFirstSampleAtOrAfter(m.T);
                if (i < 0 || i >= _graph.Samples.Count)
                    continue;

                var s = _graph.Samples[i];
                if (s.Values == null || luraIdx >= s.Values.Length)
                    continue;

                // 🔴 EXACT C++ mapping
                if (m.Label.Equals("S1", StringComparison.OrdinalIgnoreCase))
                {
                    s1Lura = s.Values[luraIdx];
                }
                else if (m.Label.Equals("S2", StringComparison.OrdinalIgnoreCase))
                {
                    s2Lura = s.Values[luraIdx];
                }
            }

            if (!s1Lura.HasValue || !s2Lura.HasValue)
                return 0;

            // C++: sfnlen = sfnlentime2 - sfnlentime1
            return Math.Round(s2Lura.Value - s1Lura.Value, 1);
        }
        //End Code for UPP Test Calculation for Lura means "Functional Profile Length" 29-01-2026


        //Code For Show Text
        private void DrawUPPTestText(GdiContext ctx, int left, int top, Size fSize)
        {
            string font = _data.FontFamily;
            int r = 0;

            //ctx.SetFont(font, 12f, FontStyle.Bold);
            //ctx.SetTextColor(Color.Red);
            //ctx.TextOutLeft(left, top + fSize.Height * r, "UPP Result");
            //r += 2;

            int rowH = fSize.Height + 8;
            int titleHeight = rowH + 6;

            int sideMargin = fSize.Width;
            int headerLeft = left - 6;
            int headerWidth = fSize.Width * 44;
            int headerTop = top + fSize.Height * r - 10;

            // remove border
            ctx.SetPen(Color.Transparent, 0);

            // gradient background
            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,
                    Color.RoyalBlue,
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
                );
            }

            // ---- MAX VALUES ----
            double maxPves = GetMaxPositiveValue("pves");
            double maxPura = GetMaxPositiveValue("pura");
            double maxPclo = GetMaxPositiveValue("pclo");
            //double maxLura = GetMaxPositiveValue("lura");

            double fpl = GetFunctionalProfileLengthCm_CppMatched();


            // header text
            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                headerLeft + 12,
                headerTop + (titleHeight - fSize.Height) / 2,
                "UPP Result"
            );


            // move content below header
            r += 2;


            ctx.SetFont(font, 11f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(left, top + fSize.Height * r,
                $"Vesical Pressure (Pves) :-  {Convert.ToInt32(maxPves)}  cmH2O");
            r++;

            ctx.TextOutLeft(left, top + fSize.Height * r,
                $"Maximum Urethral Pressure (Pura max) :-  {Convert.ToInt32(maxPura)} cmH2O");
            r++;

            ctx.TextOutLeft(left, top + fSize.Height * r,
                $"Maximum Closure Pressure (Pclo max) :-  {Convert.ToInt32(maxPclo)} cmH2O");
            r++;


            string fplText = fpl > 0 ? $"{fpl:F1} cms." : "0";
            ctx.TextOutLeft(left, top + fSize.Height * r,
                $"Functional Profile Length :-  {Convert.ToInt32(fplText)}");
            r += 4;



            // Header
            //ctx.SetFont(font, 12f, FontStyle.Bold);
            //ctx.SetTextColor(Color.Red);
            //ctx.TextOutLeft(left, top + fSize.Height * r, "Conclusions :");
            //r += 2;

            headerTop = top + fSize.Height * r - 10;

            // gradient background
            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,
                    Color.RoyalBlue,
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
                );
            }

            // header text
            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);



            // Convert row index to Y position
            int y = top + fSize.Height * r;

            // ================= CONCLUSIONS =================
            if (!string.IsNullOrWhiteSpace(_data.ResultConclusion))
            {
                int headerHeight = fSize.Height + 12;

                // Gradient header background
                using (LinearGradientBrush headerBrush =
                    new LinearGradientBrush(
                        new Rectangle(left - 6, y - 6, headerWidth, headerHeight),
                        Color.White,
                        Color.RoyalBlue,
                        LinearGradientMode.Horizontal))
                {
                    ctx.FillRectangle(
                        headerBrush,
                        new Rectangle(left - 6, y - 6, headerWidth, headerHeight)
                    );
                }

                // Header text
                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(left + 10, y, "Conclusions");

                y += headerHeight + 6;

                // Conclusion text
                ctx.SetFont(font, 10f, FontStyle.Regular);
                ctx.SetTextColor(Color.Black);

                Rectangle conclRect = new Rectangle(
                    left,
                    y,
                    headerWidth - 20,
                    fSize.Height * 4
                );



                //ctx.DrawTextBlock(_data.ResultConclusion, conclRect);
                string text = _data.ResultConclusion ?? string.Empty;

                // Maximum width available
                int maxWidth = headerWidth - 150;

                // Break text into lines
                string[] words = text.Split(' ');
                string line = "";

                foreach (string word in words)
                {
                    string testLine = line.Length == 0 ? word : line + " " + word;
                    Size sz = ctx.MeasureText(testLine);

                    if (sz.Width > maxWidth)
                    {
                        ctx.TextOutLeft(left, y, line);
                        y += fSize.Height;
                        line = word;
                    }
                    else
                    {
                        line = testLine;
                    }
                }

                // Print remaining line
                if (!string.IsNullOrEmpty(line))
                {
                    ctx.TextOutLeft(left, y, line);
                    y += fSize.Height;
                }


                y = conclRect.Bottom + fSize.Height;
            }

            // ================= REPORTED BY =================
            if (!string.IsNullOrWhiteSpace(_data?.ReportBy))
            {
                ctx.SetFont(font, 11f, FontStyle.Bold);

                // Label (RED)
                ctx.SetTextColor(Color.Red);
                string label = "Reported By :- ";
                ctx.TextOutLeft(left, y, label);

                // Measure label width
                SizeF labelSize = ctx.MeasureText(label);

                // Value (BLACK)
                //ctx.SetTextColor(Color.Black);
                //ctx.TextOutLeft(
                //    left + (int)labelSize.Width,
                //    y,
                //    _data.ReportBy ?? string.Empty
                //);

                ctx.SetTextColor(Color.Black);

                string reportedText =
                    $"      {_data.ReportBy}  ({_data.DoctorDegree})";

                ctx.TextOutLeft((int)(left + labelSize.Width), y, reportedText);

                y += fSize.Height * 3;

                // ================= SIGNATURE =================
                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(left, y, "Signature _____________");

                y += fSize.Height * 2;

                // Update row counter back
                r = (y - top) / fSize.Height;
            }
        }

        //Code For Footer blue line i page end
        private void FooterBlueText(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            string font = _data.FontFamily;

            int halfTop = rect.Top + rect.Height / 2;

            Rectangle workRect = new Rectangle(
                rect.Left,
                halfTop,
                rect.Width,
                rect.Bottom - halfTop
            );


            if (_data.CompanyName && _data.LogoImage != null && _data.LogoImage.Length > 0)
            {
                // --------------------------------------------------
                // FOOTER BASE LINE
                // --------------------------------------------------
                int footerTopY = rect.Bottom - fSize.Height + 90;

                // 1️⃣ Separator line (black)
                using (Pen linePen = new Pen(Color.Black, 1))
                {
                    g.DrawLine(
                        linePen,
                        rect.Left,
                        footerTopY,
                        rect.Right,
                        footerTopY
                    );
                }

                // --------------------------------------------------
                // FOOTER CONTENT AREA
                // --------------------------------------------------
                int contentY = (int)(footerTopY + fSize.Height + 10);

                using (var ms = new MemoryStream(_data.LogoImage))
                using (Image logoImg = Image.FromStream(ms))
                {
                    // 2️⃣ Logo size
                    int logoHeight = fSize.Height * 2;
                    int logoWidth = logoHeight * logoImg.Width / logoImg.Height;

                    int logoX = rect.Left + fSize.Width - 20;   // LEFT aligned
                    int logoY = contentY - logoHeight / 4;

                    // Draw logo
                    g.DrawImage(
                        logoImg,
                        new Rectangle(logoX, logoY, logoWidth, logoHeight)
                    );

                    // 3️⃣ Footer text (right of logo)
                    ctx.SetFont(font, 12f);
                    ctx.SetTextColor(Color.Blue);

                    int textX = logoX + logoWidth + fSize.Width * 2;

                    ctx.TextOutLeft(
                        textX,
                        contentY,
                        "Santron Meditronic, India  |  www.santronmeditronic.com"
                    );
                }

                ctx.SetTextColor(Color.Black);
            }

        }
        //End Code For UPP & Whitaker Test



        private string lbl;

        private bool TryGetR1R2Window(out double r1, out double r2)
        {
            r1 = r2 = 0;

            if (_graph?.Markers == null)
                return false;

            var m1 = _graph.Markers.FirstOrDefault(m =>
                string.Equals(m.Label, "R1", StringComparison.OrdinalIgnoreCase));
            var m2 = _graph.Markers.FirstOrDefault(m =>
                string.Equals(m.Label, "R2", StringComparison.OrdinalIgnoreCase));

            if (m1 == null || m2 == null)
                return false;

            r1 = Math.Min(m1.T, m2.T);
            r2 = Math.Max(m1.T, m2.T);

            return (r2 - r1) > 0.2; // safety
        }

        private ReportSample GetSampleAtTime(double t)
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return null;

            return _graph.Samples
                .OrderBy(s => Math.Abs(s.T - t))
                .FirstOrDefault();
        }

        private Color GetMarkerColor(string label)
        {

            if (string.IsNullOrWhiteSpace(label))
                return Color.Black;

            label = label.Trim().ToUpperInvariant();

            if (label.StartsWith("I-"))
                return Color.Red;

            // Check if the label is numeric (GP marker)
            if (IsNumeric(label))
            {
                int colorName = _data?.GeneralPurposeColor ?? 000000;
                return Color.FromArgb(colorName);
            }

            // Bladder sensation markers (FS, FD, ND, SD, BC)
            if (label == "FS" || label == "FD" || label == "ND" ||
                label == "SD" || label == "BC")
            {
                int colorName = _data?.BladderSensationColor ?? 000000;
                return Color.FromArgb(colorName);
            }

            // Response markers (S1, S2, LE, CU, UDC)
            if (label == "S1" || label == "S2" ||
                label == "LE" || label == "CU" || label == "UDC")
            {
                int colorName = _data?.ResponseMarkerColor ?? 000000;
                return Color.FromArgb(colorName);
            }

            // GP markers (GP1, GP2, ...)
            if (label.StartsWith("GP"))
            {
                int colorName = _data?.GeneralPurposeColor ?? 000000;
                return Color.FromArgb(colorName);
            }

            return Color.Black;
        }
        //void DrawMarkersOnGraph(Graphics g, Rectangle plotRect,
        //                         double? clipStart = null, double? clipEnd = null)
        //{
        //    if (_graph?.Markers == null || _graph?.Samples == null || _graph.Samples.Count < 2)
        //        return;

        //    // Reset clip temporarily so marker time labels always render.
        //    Region oldClip = g.Clip;
        //    g.ResetClip();
        //    try
        //    {

        //        double startT = clipStart ?? _graph.Samples[0].T;
        //    double endT = clipEnd ?? _graph.Samples[_graph.Samples.Count - 1].T;

        //    TryGetR1R2Window(out double r1t, out double r2t);

        //    double duration = endT - startT;
        //    if (duration <= 0.0) duration = 1.0;

        //    RectangleF plot = plotRect;

        //    int laneCount = _graph.TestDef?.Lanes?.Count ?? 0;
        //    if (laneCount == 0)
        //        return;

        //    float laneHeight = plot.Height / laneCount;

        //    int gpCounter = 0; // GP numbering

        //    using (Font font = new Font("Arial", 7f, FontStyle.Bold))
        //    using (Font valueFont = new Font("Arial", 7f, FontStyle.Regular))
        //    using (Font timeFont = new Font("Arial", 7f, FontStyle.Regular))
        //    {

        //        float lastTimeLabelRight = float.NegativeInfinity; // avoid overlapping time labels
        //        foreach (var m in _graph.Markers)
        //        {
        //            if (string.IsNullOrWhiteSpace(m.Label))
        //                continue;

        //            string rawLabel = m.Label.Trim().ToUpperInvariant();

        //            // Skip R1 / R2
        //            if (rawLabel == "R1" || rawLabel == "R2")
        //                continue;

        //            double t = m.T;

        //            // Skip R1–R2 window
        //            if (r2t > r1t && t >= r1t && t <= r2t)
        //                continue;

        //            if (t < startT || t > endT)
        //                continue;

        //            //float x;
        //            //if (clipStart.HasValue && Math.Abs(t - startT) < 1.01)
        //            //    x = plot.Left + 25; // BC padding
        //            //else
        //            //    x = (float)(plot.Left + (t - startT) / duration * plot.Width);
        //            // Always compute X strictly from time, so review/ preview / print match.
        //            float x = (float)(plot.Left + (t - startT) / duration * plot.Width);

        //            // GP numbering
        //            string displayLabel = rawLabel;
        //            if (rawLabel.StartsWith("GP"))
        //            {
        //                gpCounter++;
        //                displayLabel = gpCounter.ToString();
        //            }

        //            // 🔑 GET COLOR FROM LABEL
        //            Color markerColor = GetMarkerColor(rawLabel);

        //            using (Pen pen = new Pen(markerColor, 1.2f))
        //            using (Brush brush = new SolidBrush(markerColor))
        //            {
        //                // Marker line
        //                g.DrawLine(pen, x, plot.Top, x, plot.Bottom);

        //                // Marker label (LEFT side)
        //                SizeF sz = g.MeasureString(displayLabel, font);
        //                float textX = x - sz.Width - 6;
        //                if (textX < plot.Left + 2)
        //                    textX = plot.Left + 2;

        //                g.DrawString(displayLabel, font, brush, textX, plot.Top + 4);

        //                // Draw marker time under the plot (absolute test time)
        //                string timeText = FormatMarkerTime(t); // time relative to current page window
        //                SizeF tsz = g.MeasureString(timeText, timeFont);

        //                float timeX = x - (tsz.Width / 2f);
        //                if (timeX < plot.Left) timeX = plot.Left;
        //                if (timeX + tsz.Width > plot.Right) timeX = plot.Right - tsz.Width;

        //                // Simple overlap control: skip time text if too close to previous one
        //                if (timeX > lastTimeLabelRight + 2)
        //                {
        //                    using (Brush tb = new SolidBrush(Color.Black))
        //                    {
        //                            //  g.DrawString(timeText, timeFont, tb, timeX, plot.Bottom + 2);
        //                            float timeY = plot.Bottom + 2;

        //                            // ensure not clipped by physical page bottom
        //                            if (timeY + tsz.Height > g.VisibleClipBounds.Bottom)
        //                                timeY = g.VisibleClipBounds.Bottom - tsz.Height;

        //                            g.DrawString(timeText, timeFont, tb, timeX, timeY);

        //                        }
        //                        lastTimeLabelRight = timeX + tsz.Width;
        //                }

        //            }

        //            // Marker values
        //            var sample = GetSampleAtTime(t);
        //            if (sample?.Values == null)
        //                continue;

        //            for (int lane = 0; lane < laneCount; lane++)
        //            {
        //                if (lane >= sample.Values.Length)
        //                    continue;

        //                string txt = Math.Round(sample.Values[lane]).ToString();

        //                float y = plot.Top + lane * laneHeight + laneHeight / 2f;

        //                float tx = x + 4;
        //                if (tx + 20 > plot.Right)
        //                    tx = x - 22;

        //                Color laneColor = _graph.TestDef.Lanes[lane].Color;

        //                using (Brush laneBrush = new SolidBrush(laneColor))
        //                {
        //                    g.DrawString(txt, valueFont, laneBrush, tx, y - 6);
        //                }
        //            }
        //        }
        //    }
        //    }
        //    finally
        //    {
        //        g.Clip = oldClip;
        //    }
        //}

        //Add on 13-02-2025 code for i think if R1 & R2 moddle part remove so that time change the location for other marker  
        void DrawMarkersOnGraph(Graphics g, Rectangle plotRect, double? clipStart = null, double? clipEnd = null)
        {
            if (_graph?.Markers == null || _graph?.Samples == null || _graph.Samples.Count < 2)
                return;

            // Get all R1-R2 windows for time compression
            var r1r2Windows = GetAllR1R2Windows();
            bool hasHoles = r1r2Windows.Count > 0;
            double totalHoleWidth = hasHoles ? GetTotalHoleWidth(r1r2Windows) : 0.0;

            // Reset clip temporarily so marker time labels always render
            Region oldClip = g.Clip;
            g.ResetClip();

            try
            {
                double startT = clipStart ?? _graph.Samples[0].T;
                double endT = clipEnd ?? _graph.Samples[_graph.Samples.Count - 1].T;

                // Compressed timeline
                double effectiveEndT = endT - totalHoleWidth;
                if (effectiveEndT <= startT)
                    effectiveEndT = startT + 1.0;

                double duration = effectiveEndT - startT;

                TryGetR1R2Window(out double r1t, out double r2t);

                RectangleF plot = plotRect;

                int laneCount = _graph.TestDef?.Lanes?.Count ?? 0;
                if (laneCount == 0)
                    return;

                float laneHeight = plot.Height / laneCount;

                int gpCounter = 0; // GP numbering

                using (Font font = new Font("Arial", 7f, FontStyle.Bold))
                using (Font valueFont = new Font("Arial", 7f, FontStyle.Regular))
                using (Font timeFont = new Font("Arial", 7f, FontStyle.Regular))
                {
                    float lastTimeLabelRight = float.NegativeInfinity;

                    foreach (var m in _graph.Markers)
                    {
                        if (string.IsNullOrWhiteSpace(m.Label))
                            continue;

                        string rawLabel = m.Label.Trim().ToUpperInvariant();

                        // Skip R1 / R2 markers themselves
                        if (rawLabel == "R1" || rawLabel == "R2")
                            continue;

                        double t = m.T;

                        // Skip if marker is inside any R1-R2 hole
                        if (hasHoles && IsPointInAnyHole(t, r1r2Windows))
                            continue;

                        if (t < startT || t > endT)
                            continue;

                        // Get compressed time for marker position
                        double tCompressed = GetCompressedTime(t, r1r2Windows);
                        float x = (float)(plot.Left + (tCompressed - startT) / duration * plot.Width);

                        // GP numbering
                        string displayLabel = rawLabel;
                        if (rawLabel.StartsWith("GP"))
                        {
                            gpCounter++;
                            displayLabel = gpCounter.ToString();
                        }

                        // Get color from label
                        Color markerColor = GetMarkerColor(rawLabel);

                        using (Pen pen = new Pen(markerColor, 1.2f))
                        using (Brush brush = new SolidBrush(markerColor))
                        {
                            // Marker line
                            g.DrawLine(pen, x, plot.Top, x, plot.Bottom);

                            // Marker label (LEFT side)
                            SizeF sz = g.MeasureString(displayLabel, font);
                            float textX = x - sz.Width - 6;
                            if (textX < plot.Left + 2)
                                textX = plot.Left + 2;

                            g.DrawString(displayLabel, font, brush, textX, plot.Top + 4);

                            // Draw marker time under the plot (absolute test time)
                            string timeText = FormatMarkerTime(t); // Use original time for display
                            SizeF tsz = g.MeasureString(timeText, timeFont);

                            float timeX = x - (tsz.Width / 2f);
                            if (timeX < plot.Left) timeX = plot.Left;
                            if (timeX + tsz.Width > plot.Right) timeX = plot.Right - tsz.Width;

                            //Start this code not show correct marker line time show in bottom 19-02-2026
                            // Simple overlap control: skip time text if too close to previous one
                            //if (timeX > lastTimeLabelRight + 2)
                            //{
                            //    using (Brush tb = new SolidBrush(Color.Black))
                            //    {
                            //        float timeY = plot.Bottom + 2;
                            //        if (timeY + tsz.Height > g.VisibleClipBounds.Bottom)
                            //            timeY = g.VisibleClipBounds.Bottom - tsz.Height;

                            //        g.DrawString(timeText, timeFont, tb, timeX, timeY);
                            //    }
                            //    lastTimeLabelRight = timeX + tsz.Width;
                            //}

                            //Start this code for show every marker line time show in bottom 19-02-2026
                            using (Brush tb = new SolidBrush(Color.Black))
                            {
                                float timeY = plot.Bottom + 2;

                                // Ensure not clipped by physical page bottom
                                if (timeY + tsz.Height > g.VisibleClipBounds.Bottom)
                                    timeY = g.VisibleClipBounds.Bottom - tsz.Height;

                                g.DrawString(timeText, timeFont, tb, timeX, timeY);
                            }
                            //EnD this code for show every marker line time show in bottom 19-02-2026
                        }

                        // Marker values
                        var sample = GetSampleAtTime(t);
                        if (sample?.Values == null)
                            continue;

                        for (int lane = 0; lane < laneCount; lane++)
                        {
                            if (lane >= sample.Values.Length)
                                continue;

                            string txt = Math.Round(sample.Values[lane]).ToString();

                            float y = plot.Top + lane * laneHeight + laneHeight / 2f;

                            float tx = x + 4;
                            if (tx + 20 > plot.Right)
                                tx = x - 22;

                            //This Code for Marker Line Right Side Values show in dynamic color chnage on 19-02-2026
                            //Color laneColor = _graph.TestDef.Lanes[lane].Color;

                            //using (Brush laneBrush = new SolidBrush(laneColor))
                            //{
                            //    g.DrawString(txt, valueFont, laneBrush, tx, y - 6);
                            //}

                            //This Code for Marker Line Right Side Values show in by Default Black color change on 19-02-2026
                            using (Brush laneBrush = new SolidBrush(Color.Black))
                            {
                                g.DrawString(txt, valueFont, laneBrush, tx, y - 6);
                            }
                        }
                    }
                }
            }
            finally
            {
                g.Clip = oldClip;
            }
        }

        private static string FormatMarkerTime(double seconds)
        {
            if (seconds < 0) seconds = 0;

            // Use mm:ss for readability; show fractional only for sub-10s if you want.
            int total = (int)Math.Round(seconds);
            int mm = total / 60;
            int ss = total % 60;

            return $"{mm:D2}:{ss:D2}";
        }

        //New Code
        private List<(double R1Time, double R2Time, int Index)> GetAllR1R2Windows()
        {
            var windows = new List<(double, double, int)>();

            if (_graph?.Markers == null)
                return windows;

            // Get all R1 and R2 markers sorted by time
            var r1Markers = _graph.Markers
                .Where(m => string.Equals(m.Label, "R1", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.T)
                .ToList();

            var r2Markers = _graph.Markers
                .Where(m => string.Equals(m.Label, "R2", StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.T)
                .ToList();

            // Pair them sequentially (first R1 with first R2, etc.)
            int count = Math.Min(r1Markers.Count, r2Markers.Count);
            for (int i = 0; i < count; i++)
            {
                double r1 = r1Markers[i].T;
                double r2 = r2Markers[i].T;

                // Ensure proper ordering
                if (r1 < r2)
                    windows.Add((r1, r2, i));
                else
                    windows.Add((r2, r1, i));
            }

            return windows;
        }

        private double GetCompressedTime(double originalTime, List<(double R1Time, double R2Time, int Index)> windows)
        {
            if (windows == null || windows.Count == 0)
                return originalTime;

            double compressedTime = originalTime;
            double totalSubtracted = 0;

            // Sort windows by R1 time to ensure proper order
            var sortedWindows = windows.OrderBy(w => w.R1Time).ToList();

            foreach (var window in sortedWindows)
            {
                if (originalTime > window.R2Time)
                {
                    // Point is after this hole, subtract the full hole width
                    totalSubtracted += (window.R2Time - window.R1Time);
                }
                else if (originalTime > window.R1Time && originalTime <= window.R2Time)
                {
                    // Point is inside this hole - we'll handle this separately
                    // For now, just subtract up to the hole start
                    totalSubtracted += (originalTime - window.R1Time);
                    break;
                }
                // else point is before this hole, no subtraction needed
            }

            return originalTime - totalSubtracted;
        }

        private bool IsPointInAnyHole(double t, List<(double R1Time, double R2Time, int Index)> windows)
        {
            foreach (var window in windows)
            {
                if (t >= window.R1Time && t <= window.R2Time)
                    return true;
            }
            return false;
        }

        private double GetTotalHoleWidth(List<(double R1Time, double R2Time, int Index)> windows)
        {
            double total = 0;
            foreach (var window in windows)
            {
                total += (window.R2Time - window.R1Time);
            }
            return total;
        }

        // This code For Show "R1 & R2" show middel part plotting remove

        private void DrawFillingCurvesFromRecorded(Graphics g, Rectangle curvesRect, double? clipStart = null, double? clipEnd = null)
        {
            if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
                return;

            List<ReportSample> samples = _graph.Samples;
            var def = _graph.TestDef;

            bool hasDef = def?.Lanes != null && def.Lanes.Count > 0;

            int valueCount = samples[0].Values?.Length ?? 0;
            if (valueCount <= 0)
                return;

            int laneCount = hasDef ? def.Lanes.Count : valueCount;
            if (laneCount <= 0)
                return;

            // Base time window
            double startT = clipStart ?? samples[0].T;
            double endT = clipEnd ?? samples[samples.Count - 1].T;

            // Get all R1-R2 windows
            var r1r2Windows = GetAllR1R2Windows();
            bool hasHoles = r1r2Windows.Count > 0;
            double totalHoleWidth = hasHoles ? GetTotalHoleWidth(r1r2Windows) : 0.0;

            // Compressed timeline
            double effectiveEndT = endT - totalHoleWidth;
            if (effectiveEndT <= startT)
                effectiveEndT = startT + 1.0;

            double duration = effectiveEndT - startT;

            // Identity mapping
            int[] laneToValIndex = new int[laneCount];
            for (int c = 0; c < laneCount; c++)
                laneToValIndex[c] = Math.Min(c, valueCount - 1);

            float laneHeight = (float)curvesRect.Height / laneCount;
            RectangleF plot = curvesRect;

            int total = samples.Count;
            int px = Math.Max(1, curvesRect.Width);
            int step = Math.Max(1, total / px);

            // Compute data min/max per lane
            double[] dataMin = new double[laneCount];
            double[] dataMax = new double[laneCount];
            for (int c = 0; c < laneCount; c++)
            {
                dataMin[c] = double.MaxValue;
                dataMax[c] = double.MinValue;
            }

            for (int i = 0; i < total; i++)
            {
                var s = samples[i];
                if (s.Values == null) continue;

                double t = s.T;
                if (t < startT || t > endT) continue;
                if (hasHoles && IsPointInAnyHole(t, r1r2Windows)) continue;

                for (int c = 0; c < laneCount; c++)
                {
                    int vi = laneToValIndex[c];
                    if (vi < 0 || vi >= s.Values.Length) continue;

                    double v = s.Values[vi];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                    if (v < dataMin[c]) dataMin[c] = v;
                    if (v > dataMax[c]) dataMax[c] = v;
                }
            }

            // Finalize data min/max
            for (int c = 0; c < laneCount; c++)
            {
                if (dataMin[c] == double.MaxValue)
                {
                    dataMin[c] = 0;
                    dataMax[c] = 1;
                }
                if (dataMax[c] - dataMin[c] < 1e-6)
                    dataMax[c] = dataMin[c] + 1.0;
            }

            Color[] fallbackColors =
            {
        Color.Red, Color.Blue, Color.Green,
        Color.DarkOrange, Color.Purple,
        Color.Brown, Color.Magenta
    };

            // Draw per lane
            for (int c = 0; c < laneCount; c++)
            {
                int valIndex = laneToValIndex[c];

                double min, max;
                Color penColor;

                if (hasDef && c < def.Lanes.Count)
                {
                    var laneDef = def.Lanes[c];
                    penColor = laneDef.Color;

                    double defMin = laneDef.ScaleMin;
                    double defMax = laneDef.ScaleMax;

                    bool defValid = (defMax > defMin) && (Math.Abs(defMax - defMin) > 1e-6);

                    if (!defValid)
                    {
                        min = dataMin[c];
                        max = dataMax[c];
                    }
                    else
                    {
                        min = defMin;
                        max = defMax;
                    }
                }
                else
                {
                    min = dataMin[c];
                    max = dataMax[c];
                    penColor = fallbackColors[c % fallbackColors.Length];
                }

                double range = max - min;
                if (range <= 0) range = 1.0;

                float laneTop = plot.Top + c * laneHeight;
                float laneBottom = laneTop + laneHeight;

                using (Pen pen = new Pen(penColor, 1.5f))
                {
                    List<PointF> allPoints = new List<PointF>();
                    Dictionary<int, PointF> lastPointBeforeHoles = new Dictionary<int, PointF>();
                    int currentHoleIndex = -1;

                    for (int i = 0; i < total; i += step)
                    {
                        var s = samples[i];
                        if (s.Values == null || s.Values.Length <= valIndex)
                            continue;

                        double t = s.T;
                        if (t < startT || t > endT)
                            continue;

                        // Check if we're in any hole
                        bool inHole = false;
                        int holeIdx = -1;
                        if (hasHoles)
                        {
                            for (int h = 0; h < r1r2Windows.Count; h++)
                            {
                                if (t >= r1r2Windows[h].R1Time && t <= r1r2Windows[h].R2Time)
                                {
                                    inHole = true;
                                    holeIdx = h;
                                    break;
                                }
                            }
                        }

                        // Get compressed time based on ALL holes before this point
                        double tCompressed = GetCompressedTime(t, r1r2Windows);
                        float x = (float)(plot.Left + (tCompressed - startT) / duration * plot.Width);

                        double v = s.Values[valIndex];
                        if (double.IsNaN(v) || double.IsInfinity(v))
                        {
                            if (!inHole && allPoints.Count >= 2)
                            {
                                g.DrawLines(pen, allPoints.ToArray());
                                allPoints.Clear();
                            }
                            continue;
                        }

                        float y = (float)(laneBottom - (v - min) / range * laneHeight);

                        if (x < plot.Left || x > plot.Right)
                            continue;

                        if (inHole)
                        {
                            // Store the last valid point before entering this specific hole
                            if (allPoints.Count > 0 && !lastPointBeforeHoles.ContainsKey(holeIdx))
                            {
                                lastPointBeforeHoles[holeIdx] = allPoints[allPoints.Count - 1];
                            }
                            continue;
                        }

                        // If we're out of holes and have stored bridge points, add them in order
                        if (lastPointBeforeHoles.Count > 0)
                        {
                            // Add bridge points in the order of holes
                            foreach (var kvp in lastPointBeforeHoles.OrderBy(k => k.Key))
                            {
                                allPoints.Add(kvp.Value);
                            }
                            lastPointBeforeHoles.Clear();
                        }

                        allPoints.Add(new PointF(x, y));
                    }

                    // Draw the complete line
                    if (allPoints.Count >= 2)
                        g.DrawLines(pen, allPoints.ToArray());
                }
            }
        }

        //private void DrawFillingCurvesFromRecorded(Graphics g, Rectangle curvesRect, double? clipStart = null, double? clipEnd = null)
        //{
        //    if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
        //        return;

        //    //System.Diagnostics.Debug.WriteLine("DrawFillingCurvesFromRecorded HIT");

        //    List<ReportSample> samples = _graph.Samples;
        //    var def = _graph.TestDef;

        //    bool hasDef = def?.Lanes != null && def.Lanes.Count > 0;

        //    int valueCount = samples[0].Values?.Length ?? 0;
        //    if (valueCount <= 0)
        //        return;

        //    int laneCount = hasDef ? def.Lanes.Count : valueCount;
        //    if (laneCount <= 0)
        //        return;

        //    // Base time window
        //    double startT = clipStart ?? samples[0].T;
        //    double endT = clipEnd ?? samples[samples.Count - 1].T;

        //    // R1–R2 hole detection
        //    bool hasHole = TryGetR1R2Window(out double r1t, out double r2t);
        //    double holeWidth = hasHole ? (r2t - r1t) : 0.0;

        //    // Compressed timeline
        //    double effectiveEndT = endT - holeWidth;
        //    if (effectiveEndT <= startT)
        //        effectiveEndT = startT + 1.0;

        //    double duration = effectiveEndT - startT;

        //    // Identity mapping (confirmed by your debug: laneCount == valueCount)
        //    // If later you need remap, add it here, but right now mapping is correct.
        //    int[] laneToValIndex = new int[laneCount];
        //    for (int c = 0; c < laneCount; c++)
        //        laneToValIndex[c] = Math.Min(c, valueCount - 1);

        //    float laneHeight = (float)curvesRect.Height / laneCount;
        //    RectangleF plot = curvesRect;

        //    int total = samples.Count;
        //    int px = Math.Max(1, curvesRect.Width);
        //    int step = Math.Max(1, total / px);

        //    // ------------------------------------------------------------
        //    // Compute data min/max per lane from the SAVED samples (same as Review)
        //    // We will use these for autoscale fallback when laneDef scale is invalid.
        //    // ------------------------------------------------------------
        //    double[] dataMin = new double[laneCount];
        //    double[] dataMax = new double[laneCount];
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        dataMin[c] = double.MaxValue;
        //        dataMax[c] = double.MinValue;
        //    }

        //    for (int i = 0; i < total; i++)
        //    {
        //        var s = samples[i];
        //        if (s.Values == null) continue;

        //        double t = s.T;
        //        if (t < startT || t > endT) continue;
        //        if (hasHole && t >= r1t && t <= r2t) continue;

        //        for (int c = 0; c < laneCount; c++)
        //        {
        //            int vi = laneToValIndex[c];
        //            if (vi < 0 || vi >= s.Values.Length) continue;

        //            double v = s.Values[vi];
        //            if (double.IsNaN(v) || double.IsInfinity(v)) continue;

        //            if (v < dataMin[c]) dataMin[c] = v;
        //            if (v > dataMax[c]) dataMax[c] = v;
        //        }
        //    }

        //    // Finalize data min/max (handle empty lanes)
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        if (dataMin[c] == double.MaxValue)
        //        {
        //            dataMin[c] = 0;
        //            dataMax[c] = 1;
        //        }
        //        if (dataMax[c] - dataMin[c] < 1e-6)
        //            dataMax[c] = dataMin[c] + 1.0;
        //    }

        //    Color[] fallbackColors =
        //    {
        //        Color.Red, Color.Blue, Color.Green,
        //        Color.DarkOrange, Color.Purple,
        //        Color.Brown, Color.Magenta
        //    };

        //    // -------- DRAW PER LANE --------
        //    for (int c = 0; c < laneCount; c++)
        //    {
        //        int valIndex = laneToValIndex[c];

        //        // Decide scale for this lane:
        //        // - If TestDef provides a VALID scale range, use it
        //        // - Otherwise autoscale from saved data (so print matches review)
        //        double min, max;
        //        Color penColor;

        //        if (hasDef && c < def.Lanes.Count)
        //        {
        //            var laneDef = def.Lanes[c];

        //            penColor = laneDef.Color;

        //            double defMin = laneDef.ScaleMin;
        //            double defMax = laneDef.ScaleMax;

        //            bool defValid = (defMax > defMin) && (Math.Abs(defMax - defMin) > 1e-6);

        //            // Common bad case: both are 0 (or equal) in print pipeline
        //            if (!defValid)
        //            {
        //                min = dataMin[c];
        //                max = dataMax[c];
        //            }
        //            else
        //            {
        //                min = defMin;
        //                max = defMax;

        //                // If def range exists but data is totally outside, still keep def (clinical fixed scale)
        //                // If you prefer to clamp instead, we can adjust later.
        //            }
        //        }
        //        else
        //        {
        //            min = dataMin[c];
        //            max = dataMax[c];
        //            penColor = fallbackColors[c % fallbackColors.Length];
        //        }
        //       // System.Diagnostics.Debug.WriteLine($"lane {c} first={samples[0].Values[valIndex]:0.##} scale=[{min:0.##},{max:0.##}] data=[{dataMin[c]:0.##},{dataMax[c]:0.##}]");

        //        double range = max - min;
        //        if (range <= 0) range = 1.0;

        //        float laneTop = plot.Top + c * laneHeight;
        //        float laneBottom = laneTop + laneHeight;

        //        using (Pen pen = new Pen(penColor, 1.5f))
        //        {
        //            List<PointF> segment = new List<PointF>();

        //            PointF? lastValidPoint = null;

        //            for (int i = 0; i < total; i += step)
        //            {
        //                var s = samples[i];
        //                if (s.Values == null || s.Values.Length <= valIndex)
        //                    continue;

        //                double t = s.T;
        //                if (t < startT || t > endT)
        //                    continue;

        //                // Gap for R1–R2
        //                if (hasHole && t >= r1t && t <= r2t)
        //                {
        //                    //Start code for join the Graph Gap For R1 R2

        //                    //if (segment.Count >= 2)
        //                    //    g.DrawLines(pen, segment.ToArray());
        //                    //segment.Clear();
        //                    //continue;

        //                    //End code for join the Graph Gap For R1 R2

        //                    if (segment.Count > 0)
        //                    {
        //                        lastValidPoint = segment[segment.Count - 1];
        //                    }
        //                    continue; // Skip points in the gap
        //                }

        //                // Compress time after R2
        //                double tCompressed = t;
        //                if (hasHole && t > r2t)
        //                    tCompressed -= holeWidth;

        //                float x = (float)(plot.Left + (tCompressed - startT) / duration * plot.Width);

        //                // IMPORTANT: print EXACT saved value
        //                double v = s.Values[valIndex];
        //                if (double.IsNaN(v) || double.IsInfinity(v)) continue;

        //                float y = (float)(laneBottom - (v - min) / range * laneHeight);

        //                if (x < plot.Left || x > plot.Right)
        //                    continue;

        //                segment.Add(new PointF(x, y));
        //            }

        //            if (segment.Count >= 2)
        //                g.DrawLines(pen, segment.ToArray());
        //        }
        //    }
        //}


        private string FormatSeconds(double t)
        {
            if (t < 0) t = 0;
            int sec = (int)Math.Round(t);
            int m = sec / 60;
            int s = sec % 60;
            return $"{m}:{s:00}";
        }

        #endregion
        //// Axes + labels + ranges for filling graph
        //void DrawFillingAxes(GdiContext ctx, Graphics g, Rectangle labelRect, Rectangle curvesRect, Size fSize)
        //{
        //    string font = _data.FontFamily;
        //    ctx.SetFont(font, 9f);

        //    // Outer rectangles: left label box + graph box
        //    ctx.SetPen(Color.Black, 1);
        //    ctx.Rectangle(labelRect);    // box for channel names + scales
        //    ctx.Rectangle(curvesRect);        // box for graph

        //    var def = _graph != null ? _graph.TestDef : null;

        //    // Fallback definitions if no TestDef
        //    string[] fallbackNames = { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qura", "EMG" };
        //    string[] fallbackUnits = { "cmH2O", "cmH2O", "cmH2O", "ml", "ml", "ml/sec", "uV" };
        //    double[] fallbackMin = { 0, 0, 0, 0, 0, 0, 0 };
        //    double[] fallbackMax = { 100, 100, 100, 1000, 500, 25, 2000 };

        //    int laneCount =
        //        (def != null && def.Lanes != null && def.Lanes.Count > 0)
        //        ? def.Lanes.Count
        //        : fallbackNames.Length;

        //    float laneHeight = (float)curvesRect.Height / laneCount;

        //    for (int i = 0; i < laneCount; i++)
        //    {
        //        float laneTop = curvesRect.Top + i * laneHeight;
        //        float laneBottom = laneTop + laneHeight;
        //        float midY = (laneTop + laneBottom) / 2f;

        //        // 1) Strong horizontal separator across label box + graph
        //        if (i > 0)
        //        {
        //            ctx.MoveTo(labelRect.Left, (int)laneTop);
        //            ctx.LineTo(curvesRect.Right, (int)laneTop);
        //        }

        //        // 2) Dotted grid lines inside the graph box only (4 per lane)
        //        for (int k = 1; k <= 4; k++)
        //        {
        //            float y = laneTop + k * laneHeight / 5f;
        //            using (Pen gridPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dot })
        //            {
        //                g.DrawLine(gridPen, curvesRect.Left, y, curvesRect.Right, y);
        //            }
        //        }

        //        // 3) Lane meta: name, unit, min, max, color
        //        string name, unit;
        //        double min, max;
        //        Color laneColor;

        //        if (def != null && def.Lanes != null && i < def.Lanes.Count)
        //        {
        //            var laneDef = def.Lanes[i];
        //            name = laneDef.Name ?? fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
        //            unit = laneDef.Unit;
        //            if (string.IsNullOrEmpty(unit))
        //                unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];

        //            min = laneDef.ScaleMin;
        //            max = laneDef.ScaleMax;
        //            if (max <= min)
        //            {
        //                min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
        //                max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
        //            }

        //            laneColor = laneDef.Color;
        //        }
        //        else
        //        {
        //            name = fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
        //            unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];
        //            min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
        //            max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
        //            laneColor = Color.Black;
        //        }

        //        // 4) Min/Max to the RIGHT of channel labels, close to graph
        //        int minMaxRightX = labelRect.Right - (int)(fSize.Width * 0.2); // a few px left of graph

        //        ctx.SetTextColor(Color.Black);

        //        // MAX (top of lane)
        //        ctx.TextOutRight(
        //            minMaxRightX,
        //            (int)(laneTop + fSize.Height * 0.1),
        //            max.ToString("0")
        //        );

        //        // MIN (bottom of lane)
        //        ctx.TextOutRight(
        //            minMaxRightX,
        //            (int)(laneBottom - fSize.Height * 0.9),
        //            min.ToString("0")
        //        );

        //        // Channel name & unit: right aligned inside label box, but left of min/max
        //        int labelRightX = labelRect.Right - (int)(fSize.Width * 1.2);

        //        ctx.SetTextColor(laneColor);
        //        ctx.TextOutRight(
        //            (int)labelRightX,
        //            (int)(midY - fSize.Height * 0.6),
        //            name
        //        );

        //        ctx.SetTextColor(Color.Black);
        //        ctx.TextOutRight(
        //            (int)labelRightX,
        //            (int)(midY + fSize.Height * 0.2),
        //            unit
        //        );
        //    }

        //    // reset text color to black
        //    ctx.SetTextColor(Color.Black);
        //}

        // Axes + labels + ranges for filling graph
        void DrawFillingAxes(GdiContext ctx, Graphics g, Rectangle labelRect, Rectangle curvesRect, Size fSize)
        {
            // Backward compatible wrapper: old call treated curvesRect as the full graph frame.
            DrawFillingAxes(ctx, g, labelRect, curvesRect, curvesRect, fSize);
        }

        void DrawFillingAxes(GdiContext ctx, Graphics g, Rectangle labelRect, Rectangle graphFrameRect, Rectangle curvesRect, Size fSize)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 9f);

            // Outer rectangles: left label box + graph box (graph box includes time axis area)
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(labelRect);
            ctx.Rectangle(graphFrameRect);

            // Separator between plot area and bottom time-axis area
            if (graphFrameRect.Bottom > curvesRect.Bottom)
            {
                ctx.MoveTo(graphFrameRect.Left, curvesRect.Bottom);
                ctx.LineTo(graphFrameRect.Right, curvesRect.Bottom);
            }

            var def = _graph != null ? _graph.TestDef : null;

            // Fallback definitions if no TestDef
            string[] fallbackNames = { "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qura", "EMG" };
            string[] fallbackUnits = { "cmH2O", "cmH2O", "cmH2O", "ml", "ml", "ml/sec", "uV" };
            double[] fallbackMin = { 0, 0, 0, 0, 0, 0, 0 };
            double[] fallbackMax = { 100, 100, 100, 1000, 500, 25, 2000 };

            int laneCount =
                (def != null && def.Lanes != null && def.Lanes.Count > 0)
                ? def.Lanes.Count
                : fallbackNames.Length;

            // IMPORTANT: laneHeight must be based on curvesRect (plot area), not the full graph frame.
            float laneHeight = (float)curvesRect.Height / laneCount;

            for (int i = 0; i < laneCount; i++)
            {
                float laneTop = curvesRect.Top + i * laneHeight;
                float laneBottom = laneTop + laneHeight;
                float midY = (laneTop + laneBottom) / 2f;

                // 1) Strong horizontal separator across label box + graph frame
                if (i > 0)
                {
                    ctx.MoveTo(labelRect.Left, (int)laneTop);
                    ctx.LineTo(graphFrameRect.Right, (int)laneTop);
                }

                // 2) Dotted grid lines inside the plot area only
                for (int k = 1; k <= 4; k++)
                {
                    float y = laneTop + k * laneHeight / 5f;
                    using (Pen gridPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dot })
                    {
                        g.DrawLine(gridPen, curvesRect.Left, y, curvesRect.Right, y);
                    }
                }

                // 3) Lane meta: name, unit, min, max, color
                string name, unit;
                double min, max;
                Color laneColor;

                if (def != null && def.Lanes != null && i < def.Lanes.Count)
                {
                    var laneDef = def.Lanes[i];
                    name = laneDef.Name ?? fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
                    unit = laneDef.Unit;
                    if (string.IsNullOrEmpty(unit))
                        unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];

                    min = laneDef.ScaleMin;
                    max = laneDef.ScaleMax;
                    if (max <= min)
                    {
                        min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
                        max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
                    }

                    laneColor = laneDef.Color;
                }
                else
                {
                    name = fallbackNames[Math.Min(i, fallbackNames.Length - 1)];
                    unit = fallbackUnits[Math.Min(i, fallbackUnits.Length - 1)];
                    min = fallbackMin[Math.Min(i, fallbackMin.Length - 1)];
                    max = fallbackMax[Math.Min(i, fallbackMax.Length - 1)];
                    laneColor = Color.Black;
                }

                // 4) Min/Max near the graph edge inside labelRect
                int minMaxRightX = labelRect.Right - (int)(fSize.Width * 0.2);

                ctx.SetTextColor(Color.Black);

                // MAX (top)
                ctx.TextOutRight(
                    minMaxRightX,
                    (int)(laneTop + fSize.Height * 0.1),
                    max.ToString("0")
                );

                // MIN (bottom)
                ctx.TextOutRight(
                    minMaxRightX,
                    (int)(laneBottom - fSize.Height * 0.9),
                    min.ToString("0")
                );

                // Channel name & unit (right aligned)
                int labelRightX = labelRect.Right - (int)(fSize.Width * 1.2);

                ctx.SetTextColor(laneColor);
                ctx.TextOutRight(
                    labelRightX,
                    (int)(midY - fSize.Height * 0.6),
                    name
                );

                ctx.SetTextColor(Color.Black);
                ctx.TextOutRight(
                    labelRightX,
                    (int)(midY + fSize.Height * 0.2),
                    unit
                );
            }

            ctx.SetTextColor(Color.Black);
        }



        private bool HasMarker(string prefix)
        {
            if (_graph?.Markers == null)
                return false;

            return _graph.Markers.Any(m =>
                !string.IsNullOrWhiteSpace(m.Label) &&
                m.Label.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private BladderPoint GetLastNonZeroVinfPoint()
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return null;

            int idxVinf = FindLaneIndex("vinf");
            int idxPves = FindLaneIndex("pves");
            int idxPabd = FindLaneIndex("pabd");
            int idxPdet = FindLaneIndex("pdet");

            // ❗ VINF does not exist in this test
            if (idxVinf < 0)
                return null;

            for (int i = _graph.Samples.Count - 1; i >= 0; i--)
            {
                var s = _graph.Samples[i];

                if (s?.Values == null)
                    continue;

                if (idxVinf >= s.Values.Length)
                    continue;

                double vinf = s.Values[idxVinf];

                if (double.IsNaN(vinf))
                    continue;

                return new BladderPoint
                {
                    Vinf = vinf,
                    Pves = (idxPves >= 0 && idxPves < s.Values.Length) ? s.Values[idxPves] : 0,
                    Pabd = (idxPabd >= 0 && idxPabd < s.Values.Length) ? s.Values[idxPabd] : 0,
                    Pdet = (idxPdet >= 0 && idxPdet < s.Values.Length) ? s.Values[idxPdet] : 0
                };
            }

            return null;
        }

        private double GetMarkerTime(string label)
        {
            if (_graph?.Markers == null)
                return 0;

            var marker = _graph.Markers
                .FirstOrDefault(m =>
                    string.Equals(m.Label?.Trim(), label, StringComparison.OrdinalIgnoreCase));

            return marker?.T ?? 0;
        }


        //End This Code for Get The Bladder Capacity data using "VINF" value and BC Mark Point add on "05-01-2026"

        private void DrawFillingResults(GdiContext ctx, int left, int top, Size fSize)
        {
            string fontName = _data.FontFamily;

            int rowH = fSize.Height + 11;
            int lineH = fSize.Height + 3;
            int tableWidth = fSize.Width * 44;

            //Color blue = Color.FromArgb(40, 70, 160);
            Color blueLight = Color.FromArgb(230, 230, 254);
            Color blue = Color.RoyalBlue;
            Brush blueLightBrush = new SolidBrush(blueLight);
            Brush blueBrush = new SolidBrush(blue);
            Brush whiteBrush = Brushes.White;

            // ================= TABLE LINE STYLE (SAME AS FIRST TABLE) =================
            Color gridGray = Color.FromArgb(180, 180, 180);
            ctx.SetPen(gridGray, 1);

            // ================= HELPER : MEASURE TEXT WIDTH =================
            int MeasureText(string text, float size, FontStyle style)
            {
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                using (var f = new Font(fontName, size, style))
                {
                    return (int)g.MeasureString(text, f).Width;
                }
            }


            //int titleHeight = rowH + 6;
            int titleHeight = rowH;

            // ===== GRADIENT HEADER BAR =====
            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(left - 4, top, tableWidth + 4, titleHeight),
                    //Color.FromArgb(210, 225, 240),   // light blue (left)
                    Color.White,   // light blue (left)
                                   //Color.FromArgb(40, 90, 160),    
                    Color.FromArgb(210, 225, 240),
                    //Color.RoyalBlue,
                    LinearGradientMode.Horizontal))
            {
                int w = Math.Max(1, tableWidth);
                int h = Math.Max(1, titleHeight);
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(left, top, w, h)
                );
            }

            // ===== TITLE TEXT (BLACK, LEFT) =====
            ctx.SetFont(fontName, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                left + 12,
                top + (titleHeight - fSize.Height) / 2,
                "Filling Phase Result"
            );

            // ===== MOVE DOWN =====
            top += titleHeight + lineH;



            // =====================================================================

            // ================= BLADDER CAPACITY LOGIC =================
            BladderPoint bcPoint =
                HasMarker("BC")
                    ? ComputeBladderPointForMarker("BC")
                    : GetLastNonZeroVinfPoint();


            // ================= CALCULATIONS =================
            double infusedVolume = Math.Round(bcPoint.Vinf);   // VINF at BC
                                                               // double leakedVolume = Math.Round(bcPoint.Qvol);    // QVOL at BC

            double leakedVolume = 0;

            if (HasMarker("BC"))
            {
                leakedVolume = Math.Round(bcPoint.Qvol); // QVOL at BC
            }
            else
            {
                // C++ sets leakvolume = 0 when BC marker is missing
                leakedVolume = 0;
            }


            // ===== Persist for voiding calculator (COMPRU) =====
            if (_data?.Filling != null)
            {
                _data.Filling.InfusedVolume = infusedVolume;
                _data.Filling.LeakVolume = leakedVolume;
                // _data.Filling.PreTestResidual = ... (only if you actually have it somewhere)
            }

            double bladderFilledVolume = infusedVolume - leakedVolume;

            if (bladderFilledVolume < 0)
                bladderFilledVolume = 0;

            // ================= COMPLIANCE Start =================
            double compliance = 0;

            if (HasMarker("FS") && HasMarker("BC") && bcPoint != null)
            {
                BladderPoint fsPoint = ComputeBladderPointForMarker("FS");

                // C++ compliance uses raw VINF, not (VINF - QVOL)
                double deltaV = bcPoint.Vinf - fsPoint.Vinf;
                double deltaP = bcPoint.Pdet - fsPoint.Pdet;

                if (Math.Abs(deltaP) > 1e-9)
                    compliance = Math.Round(deltaV / deltaP, 2);
            }

            // ================= COMPLIANCE End =================

            double bcTime = 0;

            if (HasMarker("BC"))
            {
                bcTime = GetMarkerTime("BC");
            }


            double averageInfusionRate = 0;

            if (bcTime > 0)
            {
                //averageInfusionRate = infusedVolume / bcTime;
                //averageInfusionRate = Math.Round(averageInfusionRate, 2);
                // If marker time is in seconds (same as sample T), convert to ml/min
                averageInfusionRate = (infusedVolume / bcTime) * 60.0;
                averageInfusionRate = Math.Round(averageInfusionRate, 2);
            }



            // ================= SIMPLE TEXT (NO TABLE) =================
            ctx.SetFont(fontName, 11f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);


            ctx.TextOutLeft(
               left + 12,
               top,
                 $"Infused Volume : {Convert.ToInt32(infusedVolume)} ml"
            );

            top += lineH;

            ctx.TextOutLeft(
               left + 12,
               top,
               $"Leaked Volume : {Convert.ToInt32(leakedVolume)} ml"
            );

            top += lineH;

            ctx.TextOutLeft(
               left + 12,
               top,
                $"Bladder Filled Volume : {Convert.ToInt32(bladderFilledVolume)} ml"
            );

            top += lineH;

            ctx.TextOutLeft(
               left + 12,
               top,
                $"Compliance : {Convert.ToInt32(compliance)} ml/cmH₂O"
            );

            top += lineH;

            // Line 2
            ctx.TextOutLeft(
                left + 12,
                top,
                $"Average Infusion Rate : {Convert.ToInt32(averageInfusionRate)} ml/min"
            );

            top += rowH * 1;






            // ===== GRADIENT HEADER BAR =====
            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(left - 4, top, tableWidth + 4, titleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    //Color.RoyalBlue,
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(left, top, tableWidth, titleHeight)
                );
            }

            // ===== TITLE TEXT (BLACK, LEFT) =====
            ctx.SetFont(fontName, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                left + 12,
                top + (titleHeight - fSize.Height) / 2,
                "Bladder Sensations Results"
            );

            // ===== MOVE DOWN =====
            top += rowH + 10;

            // ================= TABLE HEADER =================
            int[] cols1 =
            {
                left,
                left + fSize.Width * 10,
                left + fSize.Width * 15,
                left + fSize.Width * 22,
                left + fSize.Width * 30
            };

            Rectangle tableHeader1 = new Rectangle(left, top, tableWidth, rowH);
            ctx.FillRectangle(blueBrush, tableHeader1);
            ctx.Rectangle(tableHeader1);

            ctx.SetFont(fontName, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.White);

            ctx.TextOutLeft((int)(cols1[0] * 6), top + 5, "Bladder Filled");
            ctx.TextOutLeft((int)(cols1[1] * 2.5), top + 5, "Pressure");
            ctx.TextOutLeft(cols1[2] + 6, top + 5, " ");
            ctx.TextOutLeft(cols1[3] + 6, top + 5, " ");
            ctx.TextOutLeft(cols1[4] + 6, top + 5, " ");

            top += rowH;

            // ================= TABLE HEADER =================
            int[] cols =
            {
            left,
            left + fSize.Width * 10,
            left + fSize.Width * 15,
            left + fSize.Width * 22,
            left + fSize.Width * 30
        };

            Rectangle tableHeader = new Rectangle(left, top, tableWidth, rowH);
            ctx.FillRectangle(blueBrush, tableHeader);
            ctx.Rectangle(tableHeader);

            ctx.SetFont(fontName, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.White);

            // ---- column widths ----
            int w1 = cols[1] - cols[0];
            int w2 = cols[2] - cols[1];
            int w3 = cols[3] - cols[2];
            int w4 = cols[4] - cols[3];
            int w5 = tableWidth - cols[4];

            // ================= HEADER =================
            Rectangle header = new Rectangle(left, top, tableWidth, rowH);
            ctx.FillRectangle(blueLightBrush, header);
            ctx.Rectangle(header);

            ctx.SetFont(fontName, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(cols[0] + (w1 - MeasureText("Sensation", 9f, FontStyle.Bold)) / 2, top + 5, "Sensation");
            ctx.TextOutLeft(cols[1] + (w2 + 70 - MeasureText("Vinf ml", 9f, FontStyle.Bold)) / 2, top + 5, "Vinf ml");
            ctx.TextOutLeft(cols[2] + (w3 + 110 - MeasureText("Pves cm H2O", 9f, FontStyle.Bold)) / 2, top + 5, "Pves cm H2O");
            ctx.TextOutLeft(cols[3] + (w4 + 150 - MeasureText("Pabd cmH2O", 9f, FontStyle.Bold)) / 2, top + 5, "Pabd cmH2O");
            ctx.TextOutLeft(cols[4] + (w5 + 70 - MeasureText("Pdet cmH2O", 9f, FontStyle.Bold)) / 2, top + 5, "Pdet cmH2O");

            top += rowH;

            // ================= ROW DRAW =================
            void DrawRow(string label, BladderPoint p)
            {
                Rectangle row = new Rectangle(left, top, tableWidth, rowH);
                ctx.FillRectangle(whiteBrush, row);
                ctx.Rectangle(row);

                ctx.SetFont(fontName, 11f, FontStyle.Bold);
                ctx.SetTextColor(blue);
                ctx.TextOutLeft(cols[0] + (w1 - MeasureText(label, 9f, FontStyle.Bold)) / 2, top + 5, label);

                ctx.SetFont(fontName, 11f, FontStyle.Regular);
                ctx.SetTextColor(Color.Black);

                string s1 = p.Vinf.ToString("F0");
                string s2 = p.Pves.ToString("F0");
                string s3 = p.Pabd.ToString("F0");
                string s4 = p.Pdet.ToString("F0");

                ctx.TextOutLeft(cols[1] + (w2 + 70 - MeasureText(s1, 9f, FontStyle.Regular)) / 2, top + 5, s1);
                ctx.TextOutLeft(cols[2] + (w3 + 110 - MeasureText(s2, 9f, FontStyle.Regular)) / 2, top + 5, s2);
                ctx.TextOutLeft(cols[3] + (w4 + 150 - MeasureText(s3, 9f, FontStyle.Regular)) / 2, top + 5, s3);
                ctx.TextOutLeft(cols[4] + (w5 + 70 - MeasureText(s4, 9f, FontStyle.Regular)) / 2, top + 5, s4);

                top += rowH;
            }

            // ================= DATA =================
            if (HasMarker("FS")) DrawRow("First Sensation (FS)", ComputeBladderPointForMarker("FS"));
            if (HasMarker("FD")) DrawRow("First Desire (FD)", ComputeBladderPointForMarker("FD"));
            if (HasMarker("ND")) DrawRow("Normal Desire (ND)", ComputeBladderPointForMarker("ND"));
            if (HasMarker("SD")) DrawRow("Strong Desire (SD)", ComputeBladderPointForMarker("SD"));
            if (HasMarker("BC")) DrawRow("Bladder Capacity (BC)", ComputeBladderPointForMarker("BC"));

        }


        private void DrawFooter(GdiContext ctx, Graphics g, Rectangle rect, Size fSize, int pageIndex, int totalPages)
        {
            if (!_data.CompanyName)
                return;

            string font = _data.FontFamily;

            // Footer height inside rect (stable math)
            int footerHeight = Math.Max(5, fSize.Height * 1);
            int footerTopY = rect.Bottom - footerHeight;
            int contentY = footerTopY + (fSize.Height) + 70;
            // Footer separator line
            using (Pen pen = new Pen(Color.Black, 1))
            {
                g.DrawLine(pen, rect.Left, contentY - 20, rect.Right, contentY - 20);
            }

            //int contentY = footerTopY + (fSize.Height ) + 70;

            // Logo
            Image logoImg = global::SantronWinApp.Properties.Resources.SantronLogo;
            int logoHeight = Math.Max(18, fSize.Height * 2);
            int logoWidth = logoHeight * logoImg.Width / logoImg.Height;

            int logoX = rect.Left + 10;
            int logoY = footerTopY + (footerHeight - logoHeight) + 90;

            g.DrawImage(logoImg, new Rectangle(logoX, logoY, logoWidth, logoHeight));

            // Center footer text
            ctx.SetFont(font, 11f);
            ctx.SetTextColor(Color.Blue);

            string footerText = "Santron Meditronic, India  |  www.santronmeditronic.com";
            Size footerTextSize = ctx.MeasureText(footerText);

            int centerX = rect.Left + (rect.Width - footerTextSize.Width) / 2;
            ctx.TextOutLeft(centerX, contentY, footerText);

            // Page text (keep your existing static text if you want; this keeps it inside the footer)
            ctx.SetFont(font, 11f);
            ctx.SetTextColor(Color.Black);

            // string pageText = "Page (2/5)"; // keep as-is for now
            // ===================== PATCH: dynamic page numbering =====================
            int current = Math.Max(1, _printPageCounter + 1);
            int total = Math.Max(1, _totalPages);

            string pageText = $"Page ({current}/{total})";
            // =================== END PATCH: dynamic page numbering ===================

            Size pageTextSize = ctx.MeasureText(pageText);

            int pageX = rect.Right - pageTextSize.Width - 10;
            ctx.TextOutLeft(pageX, contentY, pageText);

            ctx.SetTextColor(Color.Black);
        }

        //Footer Code 14-01-2026
        //Footer Code For Show Static Logo Image
        //private void DrawFooter(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        //{
        //    if (!_data.CompanyName)
        //        return;

        //    string font = _data.FontFamily;

        //    //int footerLineY = rect.Bottom - (int)(fSize.Height - 90);
        //    // int footerLineY = rect.Bottom - (fSize.Height * 4);
        //    int footerBlockH = (fSize.Height * 3) + 16;      // tune if needed
        //    int footerLineY = rect.Bottom - footerBlockH;   // always INSIDE page
        //    int contentY = footerLineY + 8;
        //    using (Pen pen = new Pen(Color.Black, 1))
        //    {
        //        g.DrawLine(
        //            pen,
        //            rect.Left,
        //            footerLineY,
        //            rect.Right,
        //            footerLineY
        //        );
        //    }

        //    // int contentY = footerLineY + fSize.Height + 10;



        //    // ✅ STATIC IMAGE FROM RESOURCES
        //    Image logoImg = global::SantronWinApp.Properties.Resources.SantronLogo;

        //    int logoHeight = fSize.Height * 2;
        //    int logoWidth = logoHeight * logoImg.Width / logoImg.Height;

        //    int logoX = rect.Left + fSize.Width - 20;
        //    int logoY = contentY - logoHeight / 2;

        //    // Draw logo
        //    g.DrawImage(
        //        logoImg,
        //        new Rectangle(logoX, logoY, logoWidth, logoHeight)
        //    );

        //    // Footer text
        //    ctx.SetFont(font, 12f);
        //    ctx.SetTextColor(Color.Blue);

        //    //int textX = rect.Right - fSize.Width * 20;

        //    //ctx.TextOutLeft(
        //    //    textX,
        //    //    contentY,
        //    //    "Santron Meditronic, India  |  www.santronmeditronic.com"
        //    //);

        //    string footerText = "Santron Meditronic, India  |  www.santronmeditronic.com";
        //    Size footerTextSize = ctx.MeasureText(footerText);

        //    int centerX = rect.Left + (rect.Width - footerTextSize.Width) / 2;

        //    ctx.TextOutLeft(
        //        centerX,
        //        contentY,
        //        footerText
        //    );

        //    // ================= STATIC PAGE TEXT (RIGHT SIDE) =================
        //    ctx.SetFont(font, 12f);
        //    ctx.SetTextColor(Color.Black);

        //    string pageText = "Page (2/5)";   // 🔹 STATIC as you requested
        //    Size pageTextSize = ctx.MeasureText(pageText);

        //    int pageX = rect.Right - pageTextSize.Width - fSize.Width;

        //    ctx.TextOutLeft(
        //        pageX,
        //        contentY,
        //        pageText
        //    );

        //    ctx.SetTextColor(Color.Black);
        //}


        private BladderPoint ComputeBladderPointForMarker(string prefix)
        {
            if (_graph == null || _graph.Markers == null || _graph.Samples == null)
                return new BladderPoint();

            // 1) Locate marker
            ReportMarker marker = _graph.Markers
                .FirstOrDefault(m => m.Label != null && m.Label.StartsWith(prefix));

            if (marker == null)
                return new BladderPoint();

            // 2) Find closest sample
            double t = marker.T;
            var best = _graph.Samples.OrderBy(s => Math.Abs(s.T - t)).FirstOrDefault();
            if (best == null || best.Values == null)
                return new BladderPoint();

            // 3) Determine Lane indices
            int idxPves = FindLaneIndex("pves");
            int idxPabd = FindLaneIndex("pabd");
            int idxPdet = FindLaneIndex("pdet");
            int idxVinf = FindLaneIndex("vinf");
            int idxQvol = FindLaneIndex("qvol");

            if (idxPves < 0) idxPves = 0;
            if (idxPabd < 0) idxPabd = 1;
            if (idxPdet < 0) idxPdet = 2;
            if (idxVinf < 0) idxVinf = 3;
            if (idxQvol < 0) idxQvol = 4;

            // 4) Build point
            return new BladderPoint
            {
                Vinf = SafeSample(best.Values, idxVinf),
                Pves = SafeSample(best.Values, idxPves),
                Pabd = SafeSample(best.Values, idxPabd),
                Pdet = SafeSample(best.Values, idxPdet),
                Qvol = SafeSample(best.Values, idxQvol),   // 👈 THIS
                //Qura = SafeSample(best.Values, idxQura),
                //EMG = SafeSample(best.Values, idxEmg),
            };
        }

        // Put this inside ReportPrinter.cs (same class) and use it everywhere
        //private int FindLaneIndex(string key)
        //{
        //    if (string.IsNullOrWhiteSpace(key) || _graph.TestDef?.Lanes == null) return -1;

        //    key = key.Trim();

        //    // 1) Exact match first (case-insensitive)
        //    for (int i = 0; i < _graph.TestDef?.Lanes.Count; i++)
        //    {
        //        var ln = _graph.TestDef?.Lanes[i];
        //        if (ln?.Name == null) continue;

        //        if (string.Equals(ln.Name.Trim(), key, StringComparison.OrdinalIgnoreCase))
        //            return i;
        //    }

        //    // 2) Alias map (IMPORTANT: qura must map to flow, never to Pura)
        //    // Adjust aliases if your lane names differ (e.g., "Qrate", "FlowRate", "FRATE")
        //    string alias = null;
        //    switch (key.ToLowerInvariant())
        //    {
        //        case "qura":
        //        case "flow":
        //        case "qrate":
        //        case "frate":
        //            // try common flow lane names in order
        //            int idx;
        //            idx = FindLaneIndex("Flow"); if (idx >= 0) return idx;
        //            idx = FindLaneIndex("FRATE"); if (idx >= 0) return idx;
        //            idx = FindLaneIndex("Qrate"); if (idx >= 0) return idx;
        //            idx = FindLaneIndex("Frate"); if (idx >= 0) return idx;
        //            return -1;

        //        default:
        //            return -1;
        //    }
        //}

        private int FindLaneIndex(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || _graph.TestDef?.Lanes == null)
                return -1;

            key = key.Trim();

            // 1) Exact match first (case-insensitive)
            for (int i = 0; i < _graph.TestDef.Lanes.Count; i++)
            {
                var ln = _graph.TestDef.Lanes[i];
                if (ln?.Name == null) continue;

                if (string.Equals(ln.Name.Trim(), key, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            // 2) Handle aliases with a dictionary for faster lookup
            var aliasMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "qura", new[] { "Qura", "Flow", "Qrate", "Frate", "FlowRate" } },
        { "flow", new[] { "Flow", "Qrate", "Frate", "Qura", "FlowRate" } },
        { "qrate", new[] { "Qrate", "Flow", "Frate", "Qura", "FlowRate" } },
        { "frate", new[] { "Frate", "Flow", "Qrate", "Qura", "FlowRate" } },
        // Add more aliases as needed
        // { "pves", new[] { "Pves", "Pves Pressure" } },
        // { "pura", new[] { "Pura", "Urethral Pressure" } },
    };

            string lowerKey = key.ToLowerInvariant();
            if (aliasMap.TryGetValue(lowerKey, out string[] possibleNames))
            {
                foreach (string possibleName in possibleNames)
                {
                    for (int i = 0; i < _graph.TestDef.Lanes.Count; i++)
                    {
                        var ln = _graph.TestDef.Lanes[i];
                        if (ln?.Name == null) continue;

                        if (string.Equals(ln.Name.Trim(), possibleName, StringComparison.OrdinalIgnoreCase))
                            return i;
                    }
                }
            }

            return -1;
        }
        private double SafeSample(double[] arr, int idx)
        {
            if (idx < 0 || idx >= arr.Length) return 0;
            return arr[idx];
        }

        private void DrawHeaderBlockWithLogoLeft(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            string font = _data.FontFamily;

            // Header height - keep similar to your current header
            int headerH = fSize.Height * 6;

            // Padding
            int padX = fSize.Width;
            int padY = fSize.Height / 2;

            // --- Logo box on LEFT ---
            int logoMaxH = fSize.Height * 4;
            int logoMaxW = fSize.Width * 14;     // max width allowed for logo

            int logoX = rect.Left + padX;
            int logoY = rect.Top + (headerH - logoMaxH) / 2;

            int logoDrawW = 0, logoDrawH = 0;

            if (_data.LogoImage != null && _data.LogoImage.Length > 0)
            {
                using (var ms = new MemoryStream(_data.LogoImage))
                using (var img = Image.FromStream(ms))
                {
                    // Preserve aspect ratio: fit inside logoMaxW x logoMaxH
                    double imgRatio = (double)img.Width / img.Height;
                    double boxRatio = (double)logoMaxW / logoMaxH;

                    if (imgRatio >= boxRatio)
                    {
                        logoDrawW = logoMaxW;
                        logoDrawH = (int)Math.Round(logoMaxW / imgRatio);
                    }
                    else
                    {
                        logoDrawH = logoMaxH;
                        logoDrawW = (int)Math.Round(logoMaxH * imgRatio);
                    }

                    // Vertically center within header
                    int drawY = rect.Top + (headerH - logoDrawH) / 2;
                    int drawX = logoX;

                    var oldInterp = g.InterpolationMode;
                    var oldPixel = g.PixelOffsetMode;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    g.DrawImage(img, new Rectangle(drawX, drawY, logoDrawW, logoDrawH));

                    g.InterpolationMode = oldInterp;
                    g.PixelOffsetMode = oldPixel;
                }
            }

            // --- Content area starts after logo ---
            //int contentLeft = logoX + Math.Max(logoDrawW, logoMaxW); // reserve logoMaxW even if logo is small
            int contentLeft = logoX + logoDrawW + padX;
            int contentTop = rect.Top + padY;

            // Right column anchor
            int rightX = rect.Right - padX;

            // ================= Right Column: Doctor =================
            ctx.SetFont(font, 14f, FontStyle.Regular);
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutRight(rightX, contentTop + (int)(fSize.Height * 0.1), _data.DoctorName);

            ctx.SetFont(font, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutRight(rightX, contentTop + fSize.Height * 2, _data.DoctorPost);
            ctx.TextOutRight(rightX, contentTop + fSize.Height * 3, _data.DoctorDegree);

            // ================= Left Column: Hospital (next to logo) =================
            int y = contentTop;

            ctx.SetFont(font, 14f, FontStyle.Regular);
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(contentLeft, y + (int)(fSize.Height * 0.1), _data.HospitalName);

            ctx.SetFont(font, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            y += fSize.Height + 10;

            if (!string.IsNullOrWhiteSpace(_data.HospitalAddressLine1))
            {
                ctx.TextOutLeft(contentLeft, y, _data.HospitalAddressLine1);
                y += fSize.Height + 2;
            }

            if (!string.IsNullOrWhiteSpace(_data.HospitalAddressLine2))
            {
                ctx.TextOutLeft(contentLeft, y, _data.HospitalAddressLine2);
                y += fSize.Height + 2;
            }

            if (!string.IsNullOrWhiteSpace(_data.HospitalPoneNo))
            {
                ctx.TextOutLeft(contentLeft, y, _data.HospitalPoneNo);
                y += fSize.Height + 2;
            }

            if (!string.IsNullOrWhiteSpace(_data.HospitalGmail))
            {
                ctx.TextOutLeft(contentLeft, y, _data.HospitalGmail);
                y += fSize.Height + 2;
            }

            // ================= Bottom Separator =================
            //ctx.SetPen(Color.Black, 3);
            //int lineY = rect.Top + headerH;
            //ctx.MoveTo(rect.Left, lineY);
            //ctx.LineTo(rect.Right, lineY);

            // A thin line under the header.
            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + fSize.Height + 108;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);
        }

        //Header Code
        private void DrawHeaderBlock(GdiContext ctx, Rectangle rect, Size fSize)
        {
            // Fonts roughly match CreatePointFont sizes in the C++ code.
            string font = _data.FontFamily;

            int right = rect.Left + rect.Width - fSize.Width;

            // Doctor name – extra bold, blue, right aligned.
            ctx.SetFont(font, 14f, FontStyle.Regular);
            ctx.SetTextColor(Color.Blue);

            ///int right = rect.Left + rect.Width - fSize.Width;
            ctx.TextOutRight(right, rect.Top + (int)(fSize.Height * 0.1), _data.DoctorName);

            // Degree + post – normal black, right aligned below name.
            ctx.SetFont(font, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutRight(right, rect.Top - 3 + fSize.Height * 2, _data.DoctorPost);
            ctx.TextOutRight(right, rect.Top + 2 + fSize.Height * 3, _data.DoctorDegree);


            int leftX = rect.Left;
            int y = rect.Top;

            // Hospital Name (Blue)
            ctx.SetFont(font, 14f, FontStyle.Regular);
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(leftX, y + (int)(fSize.Height * 0.1), _data.HospitalName);

            // Move downward for next lines
            ctx.SetFont(font, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            y += fSize.Height + 12;

            // Address line 1
            ctx.TextOutLeft(leftX, y, _data.HospitalAddressLine1);
            y += fSize.Height + 2;

            // Address line 2 (only if exists)
            if (!string.IsNullOrWhiteSpace(_data.HospitalAddressLine2))
            {
                ctx.TextOutLeft(leftX, y, _data.HospitalAddressLine2);
                y += fSize.Height + 2;
            }

            // Phone number
            ctx.TextOutLeft(leftX, y, _data.HospitalPoneNo);
            y += fSize.Height + 2;

            // Gmail
            ctx.TextOutLeft(leftX, y, _data.HospitalGmail);

            // A thin line under the header.
            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + fSize.Height + 100;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);
        }

        private void DrawLatterHeadImage(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            // Height reserved for header image
            int headerHeight = fSize.Height * 6;   // adjust if needed

            // Draw header image
            if (_data.LetterHeadImage != null && _data.LetterHeadImage.Length > 0)
            {
                using (var ms = new MemoryStream(_data.LetterHeadImage))
                using (var img = Image.FromStream(ms))
                {
                    // Full width header image
                    Rectangle imageRect = new Rectangle(
                        rect.Left,
                        rect.Top,
                        rect.Width,
                        headerHeight
                    );

                    // Draw image stretched to header area
                    g.DrawImage(img, imageRect);
                }
            }

            // Optional: thin separator line under header
            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + headerHeight;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);
        }

        private void DrawHeaderLogoImage(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            // Header height
            int headerHeight = fSize.Height * 6;

            // Logo size (small, left aligned)
            int logoWidth = fSize.Width * 12;   // adjust if needed
            int logoHeight = fSize.Height * 4;

            int logoX = rect.Left;
            int logoY = rect.Top + (headerHeight - logoHeight) / 2; // vertical center

            if (_data.LogoImage != null && _data.LogoImage.Length > 0)
            {
                using (var ms = new MemoryStream(_data.LogoImage))
                using (var img = Image.FromStream(ms))
                {
                    // Fit inside max box while preserving aspect ratio
                    int maxW = logoWidth;
                    int maxH = logoHeight;

                    double imgRatio = (double)img.Width / img.Height;
                    double boxRatio = (double)maxW / maxH;

                    int drawW, drawH;

                    if (imgRatio >= boxRatio)
                    {
                        drawW = maxW;
                        drawH = (int)Math.Round(maxW / imgRatio);
                    }
                    else
                    {
                        drawH = maxH;
                        drawW = (int)Math.Round(maxH * imgRatio);
                    }

                    // CENTER horizontally within the full header rect, and center vertically in header height
                    int drawX = rect.Left + (rect.Width - drawW) / 2;
                    int drawY = rect.Top + (headerHeight - drawH) / 2;

                    Rectangle logoRect = new Rectangle(drawX, drawY, drawW, drawH);

                    // Better scaling for print
                    var oldInterp = g.InterpolationMode;
                    var oldPixel = g.PixelOffsetMode;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    g.DrawImage(img, logoRect);

                    g.InterpolationMode = oldInterp;
                    g.PixelOffsetMode = oldPixel;
                }
            }

            // Optional separator line under header
            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + headerHeight;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);
        }


        private void DrawPatientBlock(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {

            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            int startRow = 7;
            int row = startRow;
            int left = rect.Left;
            int right = rect.Right;


            ctx.SetFont(_data.FontFamily, 13f, FontStyle.Bold);
            ctx.SetTextColor(Color.Red);

            ctx.TextOutLeft(
                left,
                rect.Top + fSize.Height * row + 5,
                $"Test :-  {_data.TestName}"
            );

            // restore normal font/color
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            row += 1;

            int labelX = left + 10;
            int valueOffset = 90;
            int rightLabelX = right - 360;
            int rightValueOffset = 120;

            // ✅ Small extra gap in pixels (adjust 4–8)
            int extraGap = 6;
            int topPadding = 12;

            // ✅ Helper for Y position (NO float)
            int Y(int r) => rect.Top + topPadding + (fSize.Height * r) + (extraGap * (r - startRow));

            // ================= BACKGROUND =================
            int totalRows = 4;
            int bgTop = Y(startRow) - 10;
            int bgHeight = (fSize.Height * totalRows) + (extraGap * totalRows) + 12;

            Rectangle bgRect = new Rectangle(
                left - 5,
                bgTop + 25,
                rect.Width + 10,
                bgHeight
            );

            //Code For Patient Details Backgroun Color Show
            //using (Brush bgBrush = new SolidBrush(Color.White)) // #E6E6FE //Color.FromArgb(230, 230, 254)
            //{
            //    g.FillRectangle(bgBrush, bgRect);
            //}

            // ================= TEXT =================
            ctx.SetFont(_data.FontFamily, 11f, FontStyle.Regular);

            // -------- ROW 1 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Patient ID :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientId);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Age :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Age.ToString());

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Date & Time :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TestDate.ToString("dd/M/yyyy HH:mm:ss"));
            row++;

            // -------- ROW 2 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Name :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientName);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Sex :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Sex);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Referred By :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.ReferredBy);
            row++;

            // -------- ROW 3 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Mobile No.:");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientMobile);


            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 386, Y(row), "Weight :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Weight);

            if (!string.IsNullOrEmpty(_data.TechnicianName))
            {
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX, Y(row), "Technician :");
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TechnicianName);
            }
            row++;

            // -------- ROW 4 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Address :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientAddress);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Symptoms :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.Symptoms);


            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + fSize.Height + 250;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);
        }



        private void DrawPatientShortDetails(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            int startRow = 7;
            int row = startRow;
            int left = rect.Left;
            int right = rect.Right;


            ctx.SetFont(_data.FontFamily, 13f, FontStyle.Bold);
            ctx.SetTextColor(Color.Red);

            ctx.TextOutLeft(
                left,
                rect.Top + fSize.Height * row - 112,
                $"Test :-  {_data.TestName}"
            );

            // restore normal font/color
            ctx.SetFont(_data.FontFamily, 10f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            row += 1;

            int labelX = left + 10;
            int valueOffset = 90;
            int rightLabelX = right - 360;
            int rightValueOffset = 120;

            // ✅ Small extra gap in pixels (adjust 4–8)
            int extraGap = 6;
            int topPadding = -100;

            // ✅ Helper for Y position (NO float)
            int Y(int r) => rect.Top + topPadding + (fSize.Height * r) + (extraGap * (r - startRow));

            // ================= BACKGROUND =================
            int totalRows = 4;
            int bgTop = Y(startRow) - 10;
            int bgHeight = (fSize.Height * totalRows) + (extraGap * totalRows) + 12;

            Rectangle bgRect = new Rectangle(
                left - 5,
                bgTop + 25,
                rect.Width + 10,
                bgHeight
            );

            //Code For Patient Details Backgroun Color Show
            //using (Brush bgBrush = new SolidBrush(Color.White)) // #E6E6FE //Color.FromArgb(230, 230, 254)
            //{
            //    g.FillRectangle(bgBrush, bgRect);
            //}

            // ================= TEXT =================
            ctx.SetFont(_data.FontFamily, 11f, FontStyle.Regular);

            // -------- ROW 1 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Patient ID :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientId);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Age :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Age.ToString());

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Date & Time :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TestDate.ToString("dd/M/yyyy HH:mm:ss"));
            row++;

            // -------- ROW 2 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Name :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientName);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 410, Y(row), "Sex :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Sex);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Referred By :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.ReferredBy);
            row++;

            // -------- ROW 3 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Mobile No.:");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientMobile);


            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 386, Y(row), "Weight :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + 460, Y(row), _data.Weight);

            if (!string.IsNullOrEmpty(_data.TechnicianName))
            {
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX, Y(row), "Technician :");
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.TechnicianName);
            }
            row++;

            // -------- ROW 4 --------
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX - 10, Y(row), "Address :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(labelX + valueOffset, Y(row), _data.PatientAddress);

            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX, Y(row), "Symptoms :");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(rightLabelX + rightValueOffset, Y(row), _data.Symptoms);


            ctx.SetPen(Color.Black, 1);
            int lineY = rect.Top + fSize.Height + 140;
            ctx.MoveTo(rect.Left, lineY);
            ctx.LineTo(rect.Left + rect.Width, lineY);

        }

        private void DrawTimeAxisBottom(Graphics g, Rectangle gRect, double? clipStart = null, double? clipEnd = null)
        {
            if (_graph == null || _graph.Samples == null || _graph.Samples.Count < 2)
                return;

            //double startT = _graph.Samples[0].T;
            //double endT = _graph.Samples[_graph.Samples.Count - 1].T;
            //double duration = endT - startT;

            double startT = clipStart ?? _graph.Samples[0].T;
            double endT = clipEnd ?? _graph.Samples[_graph.Samples.Count - 1].T;
            double duration = endT - startT;


            if (duration <= 0.0) duration = 1.0;

            using (Font f = new Font("Arial", 8f))
            using (Brush b = new SolidBrush(Color.Black))
            {
                string t0 = "0:00";
                string tEnd = FormatSeconds(duration);

                float y = gRect.Bottom + 2;

                g.DrawString(t0, f, b, gRect.Left, y);
                SizeF szEnd = g.MeasureString(tEnd, f);
                g.DrawString(tEnd, f, b, gRect.Right - szEnd.Width, y);
            }
        }


        #endregion



        // =====================
        // Page 2 – Voiding Phase
        // =====================
        #region Page 2 – Voiding Phase



        private void DrawVoidingPhasePage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");



            //DrawPatientBlock(ctx, rect, fSize);
            DrawPatientShortDetails(ctx, g, rect, fSize);

            // 2) Title
            //ctx.SetFont(font, 12f, FontStyle.Bold);
            //ctx.SetTextColor(Color.Red);
            //int titleY = rect.Top + fSize.Height * 13;
            //ctx.TextOutLeft(rect.Left + fSize.Width, titleY, "Voiding Phase");
            //ctx.SetTextColor(Color.Black);

            // ================= VOIDsING PHASE HEADER (ROW BACKGROUND) =================
            int rowH = fSize.Height + 8;
            int titleHeight = rowH + 6;

            // left & width same as content area
            int sideMargin = fSize.Width;
            int headerLeft = rect.Left + sideMargin;
            int headerWidth = rect.Width - sideMargin * 2;

            // vertical position (same place where title was)
            int headerTop = rect.Top + fSize.Height * 13 - fSize.Height * 3;

            // remove any active pen to avoid lines
            ctx.SetPen(Color.Transparent, 0);

            // gradient background
            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,          // left
                                          //Color.RoyalBlue,      // right
                    Color.FromArgb(210, 225, 240),
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
                );
            }

            // header text
            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                headerLeft + 12,
                headerTop + (titleHeight - fSize.Height) / 2,
                "Voiding Phase"
            );


            // 3) Define top and bottom boundaries for graph + results area
            int topHeaderSpace = fSize.Height * 15;      // header + patient + title
                                                         // int footerSpace = fSize.Height * 4;          // space reserved for footer
            int resultsHeightEst = fSize.Height * 20;    // approx height for results block
            int gapGraphResults = fSize.Height * 2;      // gap between left graph and results

            int graphTop = rect.Top + topHeaderSpace;
            //int graphBottom = rect.Bottom - footerSpace; // where graphs can go till
            // Reserve only what footer really needs (prevents excessive blank + missing 3rd nomogram)
            int footerSpace = _data.CompanyName ? ((fSize.Height * 3) + 16) : (fSize.Height * 1);
            int graphBottom = rect.Bottom - footerSpace;
            // 4) Common width for both columns
            //int sideMargin = fSize.Width;
            int totalGraphWidth = rect.Width - sideMargin * 2;

            int hGap = fSize.Width * 2;
            int leftWidth = (int)(totalGraphWidth * 0.5); // ~half page left
            int rightWidth = totalGraphWidth - leftWidth - hGap;
            if (rightWidth < fSize.Width * 10)
            {
                // safety: ensure right column not too narrow
                rightWidth = fSize.Width * 10;
                leftWidth = totalGraphWidth - rightWidth - hGap;
            }

            int leftX = rect.Left + sideMargin;
            int rightX = leftX + leftWidth + hGap;

            // 5) LEFT: tall multi-channel graph + results under it
            int totalLeftSpan = graphBottom - graphTop;
            int leftGraphHeight = totalLeftSpan - resultsHeightEst - gapGraphResults;

            if (leftGraphHeight < fSize.Height * 2)
                leftGraphHeight = fSize.Height * 2;

            Rectangle leftGraphRect = new Rectangle(
                leftX,
                graphTop,
                leftWidth,
                leftGraphHeight
            );

            int resultsTop = leftGraphRect.Bottom + gapGraphResults;

            // 6) RIGHT: 3 stacked single-lane graphs
            int rightColumnHeight = graphBottom - graphTop;
            int vGap = fSize.Height * 4;
            int singleRightHeight = (rightColumnHeight - 2 * vGap) / 3;

            Rectangle rightTopRect = new Rectangle(
                rightX,
                graphTop,
                rightWidth,
                singleRightHeight
            );

            Rectangle rightMiddleRect = new Rectangle(
                rightX,
                graphTop + singleRightHeight + vGap,
                rightWidth,
                singleRightHeight
            );

            Rectangle rightBottomRect = new Rectangle(
                rightX,
                graphTop + 2 * (singleRightHeight + vGap),
                rightWidth,
                singleRightHeight
            );

            // 7) LEFT: big combined multi-channel voiding graph
            //Start Code For Second Page (Voiding Phase) Graph
            DrawVoidingGraph(ctx, g, leftGraphRect, fSize);

            //if (_data.Nomograms && _data.PrintFlowNomograms)
            //{
            //    // (a) Flow (FRATE_OR_UPP) – 0..50 ml/sec
            //    int flowLane = ResolveLaneIndex(
            //        ChannelCol.FRATE_OR_UPP,
            //        new[] { "qura", "qrate", "flow" },
            //        defaultIndex: 5
            //    );
            //    if (flowLane >= 0)
            //    {
            //        DrawObstructionNomogram(ctx, g, rightTopRect, fSize);
            //    }

            //    // (b) Voided Volume (QVOL) – 0..500 ml
            //    int volLane = ResolveLaneIndex(
            //        ChannelCol.QVOL,
            //        new[] { "qvol", "volume", "voided" },
            //        defaultIndex: 4
            //    );
            //    if (volLane >= 0)
            //    {
            //        DrawAverageFlowNomogram(ctx, g, rightMiddleRect, fSize);
            //    }

            //    // (c) EMG – 0..2000 uV
            //    int emgLane = ResolveLaneIndex(
            //        ChannelCol.EMG,
            //        new[] { "emg" },
            //        defaultIndex: 6
            //    );
            //    if (emgLane >= 0)
            //    {
            //        DrawPeakFlowNomogram(ctx, g, rightBottomRect, fSize);
            //    }
            //}
            //else if (_data.Nomograms && !_data.PrintFlowNomograms)
            //{
            //    // (a) Flow (FRATE_OR_UPP) – 0..50 ml/sec
            //    int flowLane = ResolveLaneIndex(
            //        ChannelCol.FRATE_OR_UPP,
            //        new[] { "qura", "qrate", "flow" },
            //        defaultIndex: 5
            //    );
            //    if (flowLane >= 0)
            //    {
            //        DrawObstructionNomogram(ctx, g, rightTopRect, fSize);
            //    }
            //}
            //else
            //{

            //}

            //if (_data.Nomograms)
            //{
            //    // (a) Flow (FRATE_OR_UPP) – 0..50 ml/sec
            //    int flowLane = ResolveLaneIndex(
            //        ChannelCol.FRATE_OR_UPP,
            //        new[] { "qura", "qrate", "flow" },
            //        defaultIndex: 5
            //    );
            //    if (flowLane >= 0)
            //    {
            //        DrawObstructionNomogram(ctx, g, rightTopRect, fSize);
            //    }

            //    if (_data.PrintFlowNomograms)
            //    {
            //        // (b) Voided Volume (QVOL) – 0..500 ml
            //        int volLane = ResolveLaneIndex(
            //            ChannelCol.QVOL,
            //            new[] { "qvol", "volume", "voided" },
            //            defaultIndex: 4
            //        );
            //        if (volLane >= 0)
            //        {
            //            DrawAverageFlowNomogram(ctx, g, rightMiddleRect, fSize);
            //        }

            //        // (c) EMG – 0..2000 uV
            //        int emgLane = ResolveLaneIndex(
            //            ChannelCol.EMG,
            //            new[] { "emg" },
            //            defaultIndex: 6
            //        );
            //        if (emgLane >= 0)
            //        {
            //            DrawPeakFlowNomogram(ctx, g, rightBottomRect, fSize);
            //        }
            //    }
            //}
            //else
            //{

            //}

            if (_data.Nomograms)
            {
                // IMPORTANT: defaults must match your lane order:
                // 0 PVES, 1 PABD, 2 QVOL, 3 VINF, 4 EMG, 5 FRATE_OR_UPP, 6 PURA, 7 PDET
                int flowLane = ResolveLaneIndex(
                    ChannelCol.FRATE_OR_UPP,
                    new[] { "qura", "qrate", "flow", "frate" },
                    defaultIndex: 5
                );

                if (flowLane >= 0)
                {
                    DrawObstructionNomogram(ctx, g, rightTopRect, fSize);
                }

                if (_data.PrintFlowNomograms)
                {
                    int volLane = ResolveLaneIndex(
                        ChannelCol.QVOL,
                        new[] { "qvol", "volume", "voided" },
                        defaultIndex: 2
                    );
                    if (volLane >= 0)
                    {
                        DrawAverageFlowNomogram(ctx, g, rightMiddleRect, fSize);
                    }

                    int emgLane = ResolveLaneIndex(
                        ChannelCol.EMG,
                        new[] { "emg" },
                        defaultIndex: 4
                    );
                    if (emgLane >= 0)
                    {
                        DrawPeakFlowNomogram(ctx, g, rightBottomRect, fSize);
                    }
                }
            }


            // 8) Voiding Results under left graph
            DrawVoidingResults(ctx, leftX, resultsTop, fSize);

            DrawFooter(ctx, g, rect, fSize, 1, _totalPages);

        }



        //Start Code For Second Page (Voiding Phase Results) value show add on 10-01-2026

        private ReportSample GetLongestStablePeakSample(string laneName, int minStableCount = 3, double tolerance = 1.0)
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return null;

            int idx = FindLaneIndex(laneName);
            if (idx < 0)
                return null;

            double currentValue = double.NaN;
            int currentCount = 0;
            ReportSample currentStartSample = null;

            double bestValue = double.NaN;
            int bestCount = 0;
            ReportSample bestStartSample = null;

            foreach (var s in _graph.Samples)
            {
                if (s.Values == null || idx >= s.Values.Length)
                {
                    currentCount = 0;
                    currentValue = double.NaN;
                    currentStartSample = null;
                    continue;
                }

                double v = s.Values[idx];

                // only positive values
                if (v <= 0)
                {
                    currentCount = 0;
                    currentValue = double.NaN;
                    currentStartSample = null;
                    continue;
                }

                // start new plateau
                if (double.IsNaN(currentValue) || Math.Abs(v - currentValue) > tolerance)
                {
                    currentValue = v;
                    currentCount = 1;
                    currentStartSample = s;
                }
                else
                {
                    currentCount++;
                }

                // 🔑 keep the LONGEST plateau
                if (currentCount >= minStableCount && currentCount > bestCount)
                {
                    bestCount = currentCount;
                    bestValue = currentValue;
                    bestStartSample = currentStartSample;
                }
            }

            return bestStartSample;
        }


        //Max Value Means Go Highest Top value for Pves, Pabd, Pdet channel
        private double GetMaxPositiveValue(string laneName)
        {
            if (_graph?.Samples == null)
                return 0;

            int idx = FindLaneIndex(laneName);
            if (idx < 0)
                return 0;

            double max = 0;

            foreach (var s in _graph.Samples)
            {
                if (s.Values == null || idx >= s.Values.Length)
                    continue;

                double v = s.Values[idx];
                if (v > max)
                    max = v;
            }

            return max;
        }


        private ReportSample GetOpeningPressureSample(string laneName, int stableCount = 3, double tolerance = 1.0)
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return null;

            int idx = FindLaneIndex(laneName);
            if (idx < 0)
                return null;

            double referenceValue = double.NaN;
            int count = 0;
            ReportSample firstPlateauSample = null;

            foreach (var s in _graph.Samples)
            {
                if (s.Values == null || idx >= s.Values.Length)
                {
                    count = 0;
                    referenceValue = double.NaN;
                    firstPlateauSample = null;
                    continue;
                }

                double v = s.Values[idx];

                // Only positive values
                if (v <= 0)
                {
                    count = 0;
                    referenceValue = double.NaN;
                    firstPlateauSample = null;
                    continue;
                }

                // First valid value
                if (double.IsNaN(referenceValue))
                {
                    referenceValue = v;
                    count = 1;
                    firstPlateauSample = s;
                    continue;
                }

                // Stable within tolerance
                if (Math.Abs(v - referenceValue) <= tolerance)
                {
                    count++;

                    if (count >= stableCount)
                    {
                        // 🔑 RETURN FIRST plateau sample, not last
                        return firstPlateauSample;
                    }
                }
                else
                {
                    // Reset and start new plateau
                    referenceValue = v;
                    count = 1;
                    firstPlateauSample = s;
                }
            }

            return null;
        }



        //Static
        private void DrawVoidingResults(GdiContext ctx, int left, int top, Size fSize)
        {
            EnsureVoidingCalculated();
            string font = _data.FontFamily;
            int r = 0;

            // =====================================================
            // 🔷 GRADIENT HEADER : "Voiding Phase Results"
            // =====================================================
            int rowH = fSize.Height + 10;
            int titleHeight = rowH + 10;

            int headerLeft = left - 4;
            int headerWidth = fSize.Width * 24;
            int headerTop = top - 20;

            ctx.SetPen(Color.Transparent, 0);

            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
                );
            }

            ctx.SetFont(font, 11f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                headerLeft + 12,
                headerTop + (titleHeight - fSize.Height) / 2,
                "Voiding Phase Results"
            );

            top += titleHeight + fSize.Height;
            r -= 1;

            ctx.SetFont(font, 11f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // ---- REMOVE ALL THESE OLD METHOD CALLS ----
            // double maxPdet = GetMaxPositiveValue("pdet");
            // var peakPdet = GetLongestStablePeakSample("pdet");
            // var OpeningPresPdet = GetOpeningPressureSample("pdet");
            // etc...

            int lineGap = fSize.Height / 5;
            int Y(int row) => top + (fSize.Height * row) + (lineGap * row);

            ctx.TextOutLeft(left, Y(r),
                 //$"Voided Volume : {_data.Voiding.VoidedVolume:F1} ml");
                 $"Voided Volume : {Convert.ToInt32(_data.Voiding.VoidedVolume)} ml");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Average Flow Rate : {_data.Voiding.AverageFlowRate:F1} ml/sec.");
                $"Average Flow Rate : {Convert.ToInt32(_data.Voiding.AverageFlowRate)} ml/sec.");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Peak Flow Rate : {_data.Voiding.PeakFlowRate:F1} ml/sec.");
                $"Peak Flow Rate : {Convert.ToInt32(_data.Voiding.PeakFlowRate)} ml/sec.");
            r++;

            ctx.TextOutLeft(left, Y(r),
                 //$"Voiding Time : {_data.Voiding.VoidingTime:F0} secs.");
                 $"Voiding Time : {Convert.ToInt32(_data.Voiding.VoidingTime)} secs.");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Flow Time : {_data.Voiding.FlowTime:F0} secs");
                $"Flow Time : {Convert.ToInt32(_data.Voiding.FlowTime)} secs");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Time to Peakflow (Pdet) : {_data.Voiding.TimeToPeakFlow:F0} secs");
                $"Time to Peakflow (Pdet) : {Convert.ToInt32(_data.Voiding.TimeToPeakFlow)} secs");
            r++;
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Pdet at Peakflow : {_data.Voiding.PdetAtPeakflow:F0} cmH2O");
                $"Pdet at Peakflow : {Convert.ToInt32(_data.Voiding.PdetAtPeakflow)} cmH2O");
            r++;
            r++;

            ctx.TextOutLeft(left, Y(r),
                 //$"Opening Pressure Pdet : {_data.Voiding.OpeningPdet:F0} cmH2O");
                 $"Opening Pressure Pdet : {Convert.ToInt32(_data.Voiding.OpeningPdet)} cmH2O");
            r++;
            r++;

            ctx.TextOutLeft(left, Y(r),
                 //$"Max. Pdet : {_data.Voiding.MaxPdet:F0} cmH2O");
                 $"Max. Pdet : {Convert.ToInt32(_data.Voiding.MaxPdet)} cmH2O");
            r++;
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"BOOI : {_data.Voiding.BOOI:F1} ");
                $"BOOI : {Convert.ToInt32(_data.Voiding.BOOI)}");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"BCI : {_data.Voiding.BCI:F1}");
                $"BCI : {Convert.ToInt32(_data.Voiding.BCI)}");
            r++;

            ctx.TextOutLeft(left, Y(r),
                //$"Computed Post Void Residual Volume : {_data.Voiding.ComputedPostVoidResidual:F1} ml");
                $"Computed Post Void Residual Volume : {Convert.ToInt32(_data.Voiding.ComputedPostVoidResidual)} ml");
        }
        //private void DrawVoidingResults(GdiContext ctx, int left, int top, Size fSize)
        //{

        //    EnsureVoidingCalculated();
        //    string font = _data.FontFamily;
        //    int r = 0;

        //    // Header
        //    //ctx.SetFont(font, 12f, FontStyle.Bold);
        //    //ctx.SetTextColor(Color.Red);
        //    //ctx.TextOutLeft(left, top + fSize.Height * r, "Voiding Phase Results");
        //    //r += 2;

        //    // =====================================================
        //    // 🔷 GRADIENT HEADER : "Voiding Phase Results"
        //    // =====================================================
        //    int rowH = fSize.Height + 10;
        //    int titleHeight = rowH + 10;

        //    int headerLeft = left -4;
        //    int headerWidth = fSize.Width * 24;   // same content width used elsewhere
        //    int headerTop = top - 20;

        //    // remove pen to avoid border/line
        //    ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
        //            Color.White,          // left
        //            Color.FromArgb(210, 225, 240),
        //            //Color.RoyalBlue,      // right
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            headerBrush,
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
        //        );
        //    }

        //    ctx.SetFont(font, 11f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);

        //    ctx.TextOutLeft(
        //        headerLeft + 12,
        //        headerTop + (titleHeight - fSize.Height) / 2,
        //        "Voiding Phase Results"
        //    );

        //    // move content below header
        //    top += titleHeight + fSize.Height;
        //    r -= 1;
        //    // =====================================================

        //    ctx.SetFont(font, 11f, FontStyle.Regular);
        //    ctx.SetTextColor(Color.Black);


        //    // ---- MAX VALUES ----
        //    double maxPdet = GetMaxPositiveValue("pdet");
        //    double maxPves = GetMaxPositiveValue("pves");
        //    double maxPabd = GetMaxPositiveValue("pabd");

        //    // ---- PEAK SAMPLES ----
        //    var peakPdet = GetLongestStablePeakSample("pdet");
        //    var peakPves = GetLongestStablePeakSample("pves");
        //    var peakPabd = GetLongestStablePeakSample("pabd");

        //    // ---- GetOpeningPressureSample ----
        //    var OpeningPresPdet = GetOpeningPressureSample("pdet");
        //    var OpeningPresPves = GetOpeningPressureSample("pves");
        //    var OpeningPresPabd = GetOpeningPressureSample("pabd");

        //    double openingPdet = OpeningPresPdet != null
        //        ? Math.Max(0, OpeningPresPdet.Values[FindLaneIndex("pdet")]) : 0;

        //    double openingPves = OpeningPresPves != null
        //        ? Math.Max(0, OpeningPresPves.Values[FindLaneIndex("pves")]) : 0;

        //    double openingPabd = OpeningPresPabd != null
        //        ? Math.Max(0, OpeningPresPabd.Values[FindLaneIndex("pabd")]) : 0;



        //    // ---- TIME TO PEAK ----
        //    double timeToPeakPdet = peakPdet?.T ?? 0;
        //    double timeToPeakPves = peakPves?.T ?? 0;
        //    double timeToPeakPabd = peakPabd?.T ?? 0;

        //    // ---- VALUES AT PEAK ----
        //    double pdetAtPeak = peakPdet != null ? Math.Max(0, peakPdet.Values[FindLaneIndex("pdet")]) : 0;
        //    double pvesAtPeak = peakPves != null ? Math.Max(0, peakPves.Values[FindLaneIndex("pves")]) : 0;
        //    double pabdAtPeak = peakPabd != null ? Math.Max(0, peakPabd.Values[FindLaneIndex("pabd")]) : 0;

        //    int lineGap = fSize.Height / 5;
        //    int Y(int row) => top + (fSize.Height * row) + (lineGap * row);

        //  //ctx.TextOutLeft(left, top + fSize.Height * r,
        //  ctx.TextOutLeft(left, Y(r),
        //        $"Voided Volume : {_data.Voiding.VoidedVolume:F1} ml");
        //    r++;

        //    ctx.TextOutLeft(left, Y(r),
        //        $"Average Flow Rate : {_data.Voiding.AverageFlowRate:F1} ml/sec.");
        //    r++;

        //    ctx.TextOutLeft(left, Y(r),
        //          $"Peak Flow Rate : {_data.Voiding.PeakFlowRate:F1} ml/sec.");
        //    r++;

        //    ctx.TextOutLeft(left, Y(r),
        //        $"Voiding Time : {_data.Voiding.VoidingTime:F0} secs.");
        //    r++;

        //    ctx.TextOutLeft(left, Y(r),
        //        $"Flow Time : {_data.Voiding.FlowTime:F0} secs");
        //    r++;

        //    ctx.TextOutLeft(left, Y(r),
        //        $"Time to Peakflow (Pdet) : {timeToPeakPdet:F0} secs");
        //    r++;
        //    r++;


        //    ctx.TextOutLeft(left, Y(r),
        //        $"Pdet at Peakflow : {pdetAtPeak:F0} cmH2O");
        //    r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Pves at Peakflow : {pvesAtPeak:F0} cmH2O");
        //    //r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Pabd at Peakflow : {pabdAtPeak:F0} cmH2O");
        //    //r++;
        //    r++;



        //    ctx.TextOutLeft(left, Y(r),
        //    $"Opening Pressure Pdet : {openingPdet:F0} cmH2O");
        //    r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Opening Pressure Pves : {openingPves:F0} cmH2O");
        //    //r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Opening Pressure Pabd : {openingPabd:F0} cmH2O");
        //    //r++;
        //    r++;


        //    ctx.TextOutLeft(left, Y(r),
        //        $"Max. Pdet : {maxPdet:F0} cmH2O");
        //    r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Max. Pves : {maxPves:F0} cmH2O");
        //    //r++;

        //    //ctx.TextOutLeft(left, Y(r),
        //    //    $"Max. Pabd : {maxPabd:F0} cmH2O");
        //    //r++;
        //    r++;


        //    ctx.TextOutLeft(left, Y(r),
        //        $"BOOI : {_data.Voiding.BOOI:F1} ");
        //    r++;
        //    ctx.TextOutLeft(left, Y(r),
        //        $"BCI : {_data.Voiding.BCI:F1}");
        //    r++;
        //    ctx.TextOutLeft(left, Y(r),
        //        $"Computed Post Void Residual Volume : {_data.Voiding.ComputedPostVoidResidual:F1} ml");

        //}
        //End Code For Second Page (Voiding Phase Results) value show add on 10-01-2026


        // 13/01 pratik code for voiding calculation

        // ===============================
        // Voiding Phase Calculations (Page 2)
        // ===============================
        private bool _voidingComputed;
        private int _voidingComputedSampleCount = -1;   // simple change detector

        private void ResetVoidingCache()
        {
            _voidingComputed = false;
            _voidingComputedSampleCount = -1;
        }


        private void EnsureVoidingCalculated()
        {
            if (_voidingComputed) return;
            _voidingComputed = true;

            if (_data == null) return;
            if (_data.Voiding == null)
                _data.Voiding = new VoidingPhaseData();

            if (_graph?.Samples == null || _graph.Samples.Count == 0)
                return;

            int laneCount = _graph?.TestDef?.Lanes?.Count ?? 0;

            int defFlow = (laneCount == 2) ? 0 : 5;
            int defVol = (laneCount == 2) ? 1 : 4;

            int idxQura = ResolveLaneIndex(ChannelCol.FRATE_OR_UPP, new[] { "qura", "qrate", "flow", "frate" }, defFlow);
            int idxQvol = ResolveLaneIndex(ChannelCol.QVOL, new[] { "qvol", "vol" }, defVol);
            int idxPves = ResolveLaneIndex(ChannelCol.PVES, new[] { "pves", "ves" }, 0);
            int idxPabd = ResolveLaneIndex(ChannelCol.PABD, new[] { "pabd", "abd" }, 1);

            int idxPdet = ResolveLaneIndex(ChannelCol.PDET_or_PCLO_or_PRPG, new[] { "pdet", "det" }, 2);
            if (idxPdet < 0 && _graph.Samples.Count > 0)
            {
                int len = _graph.Samples[0].Values?.Length ?? 0;
                if ((int)ChannelId.PDET < len)
                    idxPdet = (int)ChannelId.PDET;
            }

            if (idxQura < 0 || idxQvol < 0)
                return;

            // ---------------- Voiding start ----------------
            // Use BC marker position if available, otherwise use first flow rise
            int startIdx = 0;

            // First try to get BC marker position (permission to void)
            if (TryGetBCStartTime(out double bcTime))
            {
                startIdx = FindFirstSampleAtOrAfter(bcTime);
                if (startIdx < 0) startIdx = 0;
            }
            else
            {
                // No BC marker - fall back to first flow rise
                double startT;
                bool hasStart = TryGetFirstQuraRise(out startT);
                startIdx = hasStart ? FindFirstSampleAtOrAfter(startT) : 0;
                startIdx = AdvanceUntilThreshold(startIdx, idxQura, 1.0);
            }

            if (startIdx < 0 || startIdx >= _graph.Samples.Count)
                return;





            // ---------------- Markers (R1/R2 -> 9/10) ----------------
            var markers = new List<(int Code, int Index)>();

            if (_graph.Markers != null && _graph.Markers.Count > 0)
            {
                var rMarkers = _graph.Markers
                    .Where(m => string.Equals(m.Label, "R1", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(m.Label, "R2", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.T)
                    .ToList();

                double? pendingR1 = null;

                foreach (var mk in rMarkers)
                {
                    if (string.Equals(mk.Label, "R1", StringComparison.OrdinalIgnoreCase))
                    {
                        pendingR1 = mk.T;
                    }
                    else if (string.Equals(mk.Label, "R2", StringComparison.OrdinalIgnoreCase) && pendingR1.HasValue)
                    {
                        int i1 = FindSampleIndexByTime(_graph.Samples, pendingR1.Value);
                        int i2 = FindSampleIndexByTime(_graph.Samples, mk.T);

                        if (i1 >= 0 && i2 >= 0)
                        {
                            markers.Add((9, i1));
                            markers.Add((10, i2));
                        }

                        pendingR1 = null;
                    }
                }
            }

            // ---------------- const2 (ms/sample) ----------------
            double const2Ms = 33.33;
            if (_graph.Samples.Count >= 2)
            {
                double dt = _graph.Samples[1].T - _graph.Samples[0].T;
                if (dt > 0)
                    const2Ms = dt * 1000.0;
            }

            // ---------------- Volumes ----------------
            double infusedVolume = _data?.Filling?.InfusedVolume ?? 0.0;
            double leakVolume = _data?.Filling?.LeakVolume ?? 0.0;
            double residualUrine = 0.0;
            double pretestPvr = _data?.Filling?.PreTestResidual ?? 0.0;

            // ---------------- Build resample ----------------
            double[,] resample = BuildResampleArrayFromGraph();
            int arraySize = resample.GetLength(0);

            // ---------------- bcFlowVolume (C++: sample[BCtime][4]) ----------------
            double bcFlowVolume = 0.0;

            if (TryGetBCStartTime(out bcTime))
            {
                int bcIdx = FindFirstSampleAtOrAfter(bcTime);
                if (bcIdx >= 0 && bcIdx < arraySize)
                    bcFlowVolume = resample[bcIdx, 4];
            }

            // ---------------- CALL LEGACY-MATCHING CALCULATOR ----------------
            var metrics = SantronReports.LegacyMatching.VoidingPhaseCalculator.Compute(
                resample: resample,
                startIndex: startIdx,
                arraySize: arraySize,
                const2Ms: const2Ms,
                markers: markers,
                infusedVolume: infusedVolume,
                leakVolume: leakVolume,
                residualUrine: residualUrine,
                pretestResidualUrine: pretestPvr,
                bcFlowVolume: bcFlowVolume
            );

            // ---------------- STORE RESULTS ----------------
            _data.Voiding.VoidedVolume = metrics.VoidedVolumeMl;
            _data.Voiding.AverageFlowRate = metrics.AvgFlowMlPerSec;
            _data.Voiding.PeakFlowRate = Math.Max(0, metrics.PeakFlowMlPerSec);
            _data.Voiding.VoidingTime = metrics.VoidingTimeSec;
            _data.Voiding.FlowTime = metrics.FlowTimeSec;
            _data.Voiding.TimeToPeakFlow = metrics.MaxTimeSec;

            _data.Voiding.PdetAtPeakflow = metrics.PdetAtQmax;
            _data.Voiding.PvesAtPeakflow = metrics.PvesAtQmax;
            _data.Voiding.PabdAtPeakflow = metrics.PabdAtQmax;

            _data.Voiding.OpeningPdet = metrics.Opdet;
            _data.Voiding.OpeningPves = metrics.Opves;
            _data.Voiding.OpeningPabd = metrics.Opabd;

            _data.Voiding.MaxPdet = metrics.MaxPdetVp;
            _data.Voiding.MaxPves = metrics.MaxPvesVp;
            _data.Voiding.MaxPabd = metrics.MaxPabdVp;

            _data.Voiding.DelayTime = metrics.DelayTimeSec;
            _data.Voiding.IntervalTime = metrics.IntervalTimeSec;

            _data.Voiding.BOOI = metrics.BOOI;
            _data.Voiding.BCI = metrics.BCI;

            _data.Voiding.ComputedPostVoidResidual = metrics.Compru;

            _voidingComputed = true;
            _voidingComputedSampleCount = _graph.Samples.Count;
        }



        //Old Code Comment on 28-01-2026
        //private void EnsureVoidingCalculated()
        //{
        //    if (_voidingComputed) return;
        //    _voidingComputed = true;

        //    if (_data == null) return;
        //    if (_data.Voiding == null) _data.Voiding = new VoidingPhaseData();

        //    if (_graph?.Samples == null || _graph.Samples.Count == 0)
        //        return;

        //    //int idxQura = FindLaneIndex("qura"); // flow (ml/s)
        //    //int idxQvol = FindLaneIndex("qvol"); // volume (ml)
        //    //int idxPdet = FindLaneIndex("pdet");
        //    //int idxPves = FindLaneIndex("pves");
        //    //int idxPabd = FindLaneIndex("pabd");
        //    int laneCount = _graph?.TestDef?.Lanes?.Count ?? 0;

        //    // Resolve lanes the same way we do for the C++-matching calculator.
        //    // Flow MUST resolve to processed flow (FRATE/Qura/Qrate/Flow), not PURA.
        //    int defFlow = (laneCount == 2) ? 0 : 5;
        //    int defVol = (laneCount == 2) ? 1 : 4;

        //    int idxQura = ResolveLaneIndex(ChannelCol.FRATE_OR_UPP, new[] { "qura", "qrate", "flow", "frate" }, defFlow); // flow (ml/s)
        //    int idxQvol = ResolveLaneIndex(ChannelCol.QVOL, new[] { "qvol", "vol" }, defVol); // volume (ml)
        //    int idxPves = ResolveLaneIndex(ChannelCol.PVES, new[] { "pves", "ves" }, 0);
        //    int idxPabd = ResolveLaneIndex(ChannelCol.PABD, new[] { "pabd", "abd" }, 1);

        //    // Pdet is derived; prefer labeled lane, then ChannelId-derived slot if sample arrays are full length.
        //    int idxPdet = ResolveLaneIndex(ChannelCol.PDET_or_PCLO_or_PRPG, new[] { "pdet", "det" }, 2);
        //    if (idxPdet < 0 && _graph?.Samples != null && _graph.Samples.Count > 0)
        //    {
        //        int valueLen = _graph.Samples[0].Values?.Length ?? 0;
        //        if ((int)ChannelId.PDET < valueLen) idxPdet = (int)ChannelId.PDET;
        //    }


        //    // If critical lanes are missing, we cannot compute the voiding metrics.
        //    if (idxQura < 0 || idxQvol < 0)
        //        return;

        //    // 1) Determine voiding start:
        //    // Prefer "first real voiding" start based on qura rise threshold (your helper uses >1.5).
        //    double startT;
        //    bool hasStart = TryGetFirstQuraRise(out startT);
        //    int startIdx = hasStart ? FindFirstSampleAtOrAfter(startT) : 0;

        //    // As a safety, also ensure we are starting at a meaningful flow threshold (>= 1.0 ml/s)
        //    startIdx = AdvanceUntilThreshold(startIdx, idxQura, 1.0);

        //    if (startIdx < 0 || startIdx >= _graph.Samples.Count)
        //        return;

        //    // 2) Determine voiding end:
        //    // Use a robust rule similar in spirit to legacy logic:
        //    // after "strong flow" has occurred (>= 1.6), end when flow drops <= 0.3
        //    int endIdx = FindVoidingEndIndex(startIdx, idxQura);

        //    // 3) Peak flow (Qmax) inside start..end
        //    int peakIdx = startIdx;
        //    double qmax = double.MinValue;

        //    for (int i = startIdx; i <= endIdx; i++)
        //    {
        //        var s = _graph.Samples[i];
        //        if (s?.Values == null || idxQura >= s.Values.Length) continue;

        //        double q = s.Values[idxQura];
        //        if (q > qmax)
        //        {
        //            qmax = q;
        //            peakIdx = i;
        //        }
        //    }
        //    if (qmax == double.MinValue) qmax = 0;

        //    // 4) Times
        //    double tStart = _graph.Samples[startIdx].T;
        //    double tEnd = _graph.Samples[endIdx].T;

        //    double voidingTime = Math.Max(0, tEnd - tStart);

        //    // FlowTime: from start until first time flow drops <= 0.2 (or end if never drops)
        //    int stopIdx = endIdx;
        //    for (int i = startIdx; i <= endIdx; i++)
        //    {
        //        var s = _graph.Samples[i];
        //        if (s?.Values == null || idxQura >= s.Values.Length) continue;

        //        if (s.Values[idxQura] <= 0.2)
        //        {
        //            stopIdx = i;
        //            break;
        //        }
        //    }
        //    double flowTime = Math.Max(0, _graph.Samples[stopIdx].T - tStart);

        //    double timeToPeak = Math.Max(0, _graph.Samples[peakIdx].T - tStart);

        //    // 5) Volumes
        //    //double v0 = GetSampleValue(startIdx, idxQvol);
        //    //double v1 = GetSampleValue(endIdx, idxQvol);
        //    //double voidedVol = Math.Max(0, v1 - v0);

        //    double v0 = GetSampleValue(startIdx, idxQvol);

        //    // C++ behaviour makes QVOL monotonic (never decreases).
        //    // Equivalent for report: use max QVOL within the voiding window.
        //    double vMax = v0;
        //    for (int i = startIdx; i <= endIdx; i++)
        //    {
        //        double v = GetSampleValue(i, idxQvol);
        //        if (v > vMax) vMax = v;
        //    }

        //    double voidedVol = Math.Max(0, vMax - v0);

        //    // 6) Average flow (ml/s) = voided volume / voiding time
        //    double qavg = (voidingTime > 0.0001) ? (voidedVol / voidingTime) : 0;

        //    // 7) Pressures at peak flow
        //    double pdetAtPeak = (idxPdet >= 0) ? Math.Max(0, GetSampleValue(peakIdx, idxPdet)) : 0;
        //    double pvesAtPeak = (idxPves >= 0) ? Math.Max(0, GetSampleValue(peakIdx, idxPves)) : 0;
        //    double pabdAtPeak = (idxPabd >= 0) ? Math.Max(0, GetSampleValue(peakIdx, idxPabd)) : 0;

        //    // 8) Opening pressures at voiding start
        //    double opPdet = (idxPdet >= 0) ? Math.Max(0, GetSampleValue(startIdx, idxPdet)) : 0;
        //    double opPves = (idxPves >= 0) ? Math.Max(0, GetSampleValue(startIdx, idxPves)) : 0;
        //    double opPabd = (idxPabd >= 0) ? Math.Max(0, GetSampleValue(startIdx, idxPabd)) : 0;

        //    // 9) Max pressures during voiding window
        //    double maxPdet = (idxPdet >= 0) ? GetMaxPositiveInWindow(idxPdet, startIdx, endIdx) : 0;
        //    double maxPves = (idxPves >= 0) ? GetMaxPositiveInWindow(idxPves, startIdx, endIdx) : 0;
        //    double maxPabd = (idxPabd >= 0) ? GetMaxPositiveInWindow(idxPabd, startIdx, endIdx) : 0;

        //    // 10) Indices (standard definitions used in legacy urodynamics implementations)
        //    double booi = pdetAtPeak - 2.0 * qmax;
        //    double bci = pdetAtPeak + 5.0 * qmax;

        //    //// 11) Computed PVR: if bladder capacity is known, use BC - voided volume; else 0.
        //    //double bcVol = 0;

        //    //// Prefer already-populated filling capacity point (if available)
        //    //if (_data.Filling != null && _data.Filling.BladderCapacityPoint != null)
        //    //    bcVol = _data.Filling.BladderCapacityPoint.Qvol;

        //    //// If not set, try reading Qvol at BC marker time
        //    //if (bcVol <= 0 && TryGetBCStartTime(out double bcTime))
        //    //{
        //    //    int bcIdx = FindFirstSampleAtOrAfter(bcTime);
        //    //    if (bcIdx >= 0) bcVol = GetSampleValue(bcIdx, idxQvol);
        //    //}

        //    //double computedPvr = (bcVol > 0) ? Math.Max(0, bcVol - voidedVol) : 0;

        //    // Computed PVR should be based on NET bladder volume (Vinf - Qvol), not Qvol.
        //    // Best practical approximation: net volume at voiding start - voided volume.
        //    int idxVinf = FindLaneIndex("vinf"); // may exist in combined tests
        //    double netAtStart = 0;

        //    if (idxVinf >= 0)
        //    {
        //        double vinfStart = GetSampleValue(startIdx, idxVinf);
        //        double qvolStart = GetSampleValue(startIdx, idxQvol);
        //        netAtStart = Math.Max(0, vinfStart - qvolStart);
        //    }

        //    // Fallback: if Vinf not present, try BC marker net volume
        //    if (netAtStart <= 0 && idxVinf >= 0 && TryGetBCStartTime(out double bcTime))
        //    {
        //        int bcIdx = FindFirstSampleAtOrAfter(bcTime);
        //        if (bcIdx >= 0)
        //        {
        //            double vinfBC = GetSampleValue(bcIdx, idxVinf);
        //            double qvolBC = GetSampleValue(bcIdx, idxQvol);
        //            netAtStart = Math.Max(0, vinfBC - qvolBC);
        //        }
        //    }

        //    double computedPvr = (netAtStart > 0) ? Math.Max(0, netAtStart - voidedVol) : 0;

        //    // Then assign:
        //    // metrics.VoidedVolumeMl, metrics.AvgFlowMlPerSec, metrics.PeakFlowMlPerSec,
        //    // metrics.VoidingTimeSec, metrics.FlowTimeSec, metrics.PdetAtQmax, metrics.BOOI, metrics.BCI, etc.

        //    // Build marker list in the format expected by calculator
        //    // If you already have markers in some other structure, just adapt to (Code, Index).
        //    var markers = new List<(int Code, int Index)>();

        //    if (_graph.Markers != null && _graph.Markers.Count > 0)
        //    {
        //        // collect R1/R2 markers in time order
        //        var rMarkers = _graph.Markers
        //            .Where(m => string.Equals(m.Label, "R1", StringComparison.OrdinalIgnoreCase) ||
        //                        string.Equals(m.Label, "R2", StringComparison.OrdinalIgnoreCase))
        //            .OrderBy(m => m.T)
        //            .ToList();

        //        // pair sequentially: R1 then next R2 after it
        //        double? pendingR1 = null;

        //        foreach (var mk in rMarkers)
        //        {
        //            if (string.Equals(mk.Label, "R1", StringComparison.OrdinalIgnoreCase))
        //            {
        //                pendingR1 = mk.T;
        //            }
        //            else if (string.Equals(mk.Label, "R2", StringComparison.OrdinalIgnoreCase) && pendingR1.HasValue)
        //            {
        //                double t1 = pendingR1.Value;
        //                double t2 = mk.T;

        //                if (t2 < t1) { var tmp = t1; t1 = t2; t2 = tmp; }

        //                int i1 = FindSampleIndexByTime(_graph.Samples, t1);
        //                int i2 = FindSampleIndexByTime(_graph.Samples, t2);

        //                markers.Add((9, i1));
        //                markers.Add((10, i2));

        //                pendingR1 = null;
        //            }
        //        }
        //    }

        //    // Determine the "startIndex" of voiding phase (C++ temp)
        //    // IMPORTANT: startIndex must match what C++ uses as temp (often end of filling / EFP time index).
        //    // int startIndex = _data.Times.VoidingStartSampleIndex; // <-- Replace with your actual value
        //    int startIndex = startIdx;

        //    // const2 in C++ = ms per sample (used for converting counts -> seconds)
        //    // double const2Ms = _data.Const2Ms; // <-- Replace with your actual value
        //    double const2Ms =33.33;

        //    if (_graph.Samples.Count >= 2)
        //    {
        //        double dtSec = _graph.Samples[1].T - _graph.Samples[0].T;
        //        if (dtSec > 0) const2Ms = dtSec * 1000.0;
        //    }

        //    // Optional: if you have the original raw volume series (C++ uses sample[][4] not resample[][4])
        //    // If you do NOT have it, pass null and calculator will use resample[][4].
        //    double[] rawVolumeSeries = null; // or yourSampleVolumeArray;

        //    // Inputs needed for computed PVR / COMPRU (C++ alignment)
        //    double infusedVolume = _data?.Filling?.InfusedVolume ?? 0.0;     // C++: InfusedVolume
        //    double leakVolume = _data?.Filling?.LeakVolume ?? 0.0;        // C++: leakvol

        //    // If you have measured PVR somewhere else, plug it here. If not, keep 0.
        //    double residualUrine = 0.0;

        //    // Optional: if you track pre-test residual (baseline urine before test)
        //    double pretestPvr = _data?.Filling?.PreTestResidual ?? 0.0;
        //    // Inputs needed for computed PVR / COMPRU
        //    //double infusedVolume = _data.Filling.InfusedVolume;      // ensure this matches C++ "InfusedVolume"
        //    //double leakVolume = _data.Filling.LeakVolume;         // ensure this matches C++ "leakvol"
        //    //double residualUrine = _data.Voiding.PostVoidResidual;   // if you store measured PVR separately
        //    //double pretestPvr = _data.Filling.PreTestResidual;    // if available
        //    // Replace these RHS values with whatever your code already uses/stores:
        //    //double infusedVolume = infusedVol;          // total infused volume during filling (ml)
        //    //double leakVolume = leakVol;             // leak volume (ml)
        //    //double residualUrine = postVoidResidual;    // measured post-void residual (ml), if you have it
        //    //double pretestPvr = preTestResidual;     // pre-test residual (ml), if you have it

        //    var metrics = SantronReports.LegacyMatching.VoidingPhaseCalculator.Compute(
        //        resample: BuildResampleArrayFromGraph(),          // double[,] with columns: 0=Pves,1=Pabd,2=Pdet,4=Vol,5=Flow              
        //        startIndex: startIndex,
        //        const2Ms: const2Ms,
        //        markers: markers,
        //           infusedVolume: infusedVolume,
        //           leakVolume: leakVolume,
        //            residualUrine: residualUrine,
        //            pretestResidualUrine: pretestPvr,
        //        volumeSeries: rawVolumeSeries
        //    );

        //    // ===== Store results (mapped 1:1) =====
        //    _data.Voiding.VoidedVolume = metrics.VoidedVolumeMl;
        //    _data.Voiding.AverageFlowRate = metrics.AvgFlowMlPerSec;
        //    _data.Voiding.PeakFlowRate = Math.Max(0, metrics.PeakFlowMlPerSec); // Qmax
        //    _data.Voiding.VoidingTime = metrics.VoidingTimeSec;               // seconds (C++ style)
        //    _data.Voiding.FlowTime = metrics.FlowTimeSec;                  // seconds (C++ style)
        //    _data.Voiding.TimeToPeakFlow = metrics.MaxTimeSec;                   // seconds (C++ style)

        //    // Pressures at peak flow (P@Qmax using idx = MaxTime+Delay-1)
        //    _data.Voiding.PdetAtPeakflow = metrics.PdetAtQmax;
        //    _data.Voiding.PvesAtPeakflow = metrics.PvesAtQmax;
        //    _data.Voiding.PabdAtPeakflow = metrics.PabdAtQmax;

        //    // Opening pressures at DelayTime-1
        //    _data.Voiding.OpeningPdet = metrics.Opdet;
        //    _data.Voiding.OpeningPves = metrics.Opves;
        //    _data.Voiding.OpeningPabd = metrics.Opabd;

        //    // Max pressures during voiding (excluding marker 9..10 blocks)
        //    _data.Voiding.MaxPdet = metrics.MaxPdetVp;
        //    _data.Voiding.MaxPves = metrics.MaxPvesVp;
        //    _data.Voiding.MaxPabd = metrics.MaxPabdVp;

        //    // BOOI / BCI from Pdet@Qmax and Qmax
        //    _data.Voiding.BOOI = metrics.BOOI;
        //    _data.Voiding.BCI = metrics.BCI;

        //    // "ComputedPostVoidResidual"
        //    // In C++ this is effectively: (InfusedVolume - VoidedVolume) - LeakVolume  (named Compru in code)
        //    _data.Voiding.ComputedPostVoidResidual = metrics.Compru;

        //    _voidingComputed = true;
        //    _voidingComputedSampleCount = _graph.Samples.Count;

        //    // old calculations 25/01/2026
        //    //// ===== Store results =====
        //    //_data.Voiding.VoidedVolume = voidedVol;
        //    //_data.Voiding.AverageFlowRate = qavg;
        //    //_data.Voiding.PeakFlowRate = Math.Max(0, qmax);
        //    //_data.Voiding.VoidingTime = voidingTime;
        //    //_data.Voiding.FlowTime = flowTime;
        //    //_data.Voiding.TimeToPeakFlow = timeToPeak;

        //    //_data.Voiding.PdetAtPeakflow = pdetAtPeak;
        //    //_data.Voiding.PvesAtPeakflow = pvesAtPeak;
        //    //_data.Voiding.PabdAtPeakflow = pabdAtPeak;

        //    //_data.Voiding.OpeningPdet = opPdet;
        //    //_data.Voiding.OpeningPves = opPves;
        //    //_data.Voiding.OpeningPabd = opPabd;

        //    //_data.Voiding.MaxPdet = maxPdet;
        //    //_data.Voiding.MaxPves = maxPves;
        //    //_data.Voiding.MaxPabd = maxPabd;

        //    //_data.Voiding.BOOI = booi;
        //    //_data.Voiding.BCI = bci;
        //    //_data.Voiding.ComputedPostVoidResidual = computedPvr;

        //    //_voidingComputed = true;
        //    //_voidingComputedSampleCount = _graph.Samples.Count;


        //}




        private static int FindSampleIndexByTime(IList<LegacyUroReportPrinter.ReportSample> samples, double tSec)
        {
            if (samples == null || samples.Count == 0) return 0;

            // samples are time-ordered; use binary search
            int lo = 0, hi = samples.Count - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) / 2);
                double t = samples[mid].T;

                if (t < tSec) lo = mid + 1;
                else if (t > tSec) hi = mid - 1;
                else return mid;
            }

            // lo is first index with T > tSec; choose nearest of lo and lo-1
            int idx = lo;
            if (idx <= 0) return 0;
            if (idx >= samples.Count) return samples.Count - 1;

            double dtLo = Math.Abs(samples[idx].T - tSec);
            double dtPrev = Math.Abs(samples[idx - 1].T - tSec);

            return (dtPrev <= dtLo) ? (idx - 1) : idx;
        }


        private static double SafeAt(double[] arr, int idx)
        {
            if (arr == null) return 0.0;
            if (idx < 0 || idx >= arr.Length) return 0.0;
            return arr[idx];
        }

        private double[,] BuildResampleArrayFromGraph()
        {
            // Build the exact "resample" matrix expected by the C++-matching voiding calculator:
            // columns: 0=Pves, 1=Pabd, 2=Pdet, 3=Vinf, 4=Qvol, 5=Flow(Qura)
            var samples = _graph?.Samples;
            if (samples == null || samples.Count == 0)
                return new double[0, 0];

            int n = samples.Count;
            var first = samples[0].Values;
            int valueLen = first?.Length ?? 0;

            // Always return the correct column count (6) so downstream logic is stable.
            var res = new double[n, 6];
            if (valueLen == 0)
                return res;

            int laneCount = _graph?.TestDef?.Lanes?.Count ?? 0;

            // Helper: pick lane index primarily by ActiveIndices/name patterns; ensure it's within the sample value array.
            int ResolveIdx(ChannelCol ch, string[] patterns, int defaultIndex)
            {
                int idx = ResolveLaneIndex(ch, patterns, defaultIndex);

                // In most cases, sample.Values is lane-ordered; ensure bounds.
                if (idx >= 0 && idx < valueLen) return idx;

                // Fallback: if sample.Values is "full channel array" (8 channels) and indexes match ChannelId,
                // use ChannelId-based mapping.
                switch (ch)
                {
                    case ChannelCol.PVES: idx = (int)ChannelId.PVES; break;
                    case ChannelCol.PABD: idx = (int)ChannelId.PABD; break;
                    case ChannelCol.QVOL: idx = (int)ChannelId.QVOL; break;
                    case ChannelCol.VINF: idx = (int)ChannelId.VINF; break;
                    case ChannelCol.EMG: idx = (int)ChannelId.EMG; break;
                    case ChannelCol.FRATE_OR_UPP: idx = (int)ChannelId.FRATE_OR_UPP; break;
                    default: idx = -1; break;
                }

                if (idx >= 0 && idx < valueLen) return idx;

                // Last-chance: scan lane names (only within bounds)
                if (patterns != null && laneCount > 0)
                {
                    for (int i = 0; i < laneCount && i < valueLen; i++)
                    {
                        var ln = _graph.TestDef?.Lanes?[i];
                        var nm = (ln?.Name ?? string.Empty).ToLowerInvariant();
                        foreach (var p in patterns)
                        {
                            if (!string.IsNullOrWhiteSpace(p) && nm.Contains(p.ToLowerInvariant()))
                                return i;
                        }
                    }
                }

                return -1;
            }

            // Dynamic defaults for 2-lane tests (uroflow: Flow + Qvol)
            int defFlow = (laneCount == 2) ? 0 : 5;
            int defVol = (laneCount == 2) ? 1 : 4;

            int idxPves = ResolveIdx(ChannelCol.PVES, new[] { "pves", "ves" }, 0);
            int idxPabd = ResolveIdx(ChannelCol.PABD, new[] { "pabd", "abd" }, 1);
            int idxVinf = ResolveIdx(ChannelCol.VINF, new[] { "vinf", "infus" }, 3);

            // Flow MUST map to processed flow lane (FRATE/Qura/Qrate/Flow), never PURA.
            int idxFlow = ResolveIdx(ChannelCol.FRATE_OR_UPP, new[] { "qura", "qrate", "flow", "frate" }, defFlow);

            // Volume lane
            int idxQvol = ResolveIdx(ChannelCol.QVOL, new[] { "qvol", "vol" }, defVol);

            // Pdet: prefer labeled lane; fallback to derived channel slot if present
            int idxPdet = ResolveIdx(ChannelCol.PDET_or_PCLO_or_PRPG, new[] { "pdet", "det" }, 2);
            if (idxPdet < 0 && (int)ChannelId.PDET < valueLen) idxPdet = (int)ChannelId.PDET;

            for (int i = 0; i < n; i++)
            {
                var v = samples[i].Values;
                if (v == null) continue;

                res[i, 0] = SafeAt(v, idxPves);
                res[i, 1] = SafeAt(v, idxPabd);
                res[i, 2] = SafeAt(v, idxPdet);
                res[i, 3] = SafeAt(v, idxVinf);
                res[i, 4] = SafeAt(v, idxQvol);
                res[i, 5] = SafeAt(v, idxFlow);
            }

            return res;
        }

        private int FindFirstSampleAtOrAfter(double t)
        {
            if (_graph?.Samples == null || _graph.Samples.Count == 0) return -1;
            for (int i = 0; i < _graph.Samples.Count; i++)
                if (_graph.Samples[i].T >= t) return i;
            return _graph.Samples.Count - 1;
        }

        private int AdvanceUntilThreshold(int startIdx, int laneIdx, double threshold)
        {
            if (_graph?.Samples == null) return -1;
            for (int i = Math.Max(0, startIdx); i < _graph.Samples.Count; i++)
            {
                var s = _graph.Samples[i];
                if (s?.Values == null || laneIdx >= s.Values.Length) continue;
                if (s.Values[laneIdx] >= threshold) return i;
            }
            return -1;
        }

        private int FindVoidingEndIndex(int startIdx, int idxQura)
        {
            int lastIdx = _graph.Samples.Count - 1;

            bool hadStrong = false; // >= 1.6 at least once
            for (int i = startIdx; i <= lastIdx; i++)
            {
                var s = _graph.Samples[i];
                if (s?.Values == null || idxQura >= s.Values.Length) continue;

                double q = s.Values[idxQura];
                if (q >= 1.6) hadStrong = true;

                if (hadStrong && q <= 0.3)
                    return i;
            }

            // fallback: last point where flow is still "meaningful"
            for (int i = lastIdx; i >= startIdx; i--)
            {
                var s = _graph.Samples[i];
                if (s?.Values == null || idxQura >= s.Values.Length) continue;

                if (s.Values[idxQura] > 0.2)
                    return i;
            }

            return lastIdx;
        }

        private double GetSampleValue(int sampleIndex, int laneIndex)
        {
            if (_graph?.Samples == null) return 0;
            if (sampleIndex < 0 || sampleIndex >= _graph.Samples.Count) return 0;

            var s = _graph.Samples[sampleIndex];
            if (s?.Values == null || laneIndex < 0 || laneIndex >= s.Values.Length) return 0;

            return s.Values[laneIndex];
        }

        private double GetMaxPositiveInWindow(int laneIdx, int startIdx, int endIdx)
        {
            double max = 0;
            if (_graph?.Samples == null) return 0;

            startIdx = Math.Max(0, startIdx);
            endIdx = Math.Min(_graph.Samples.Count - 1, endIdx);

            for (int i = startIdx; i <= endIdx; i++)
            {
                var s = _graph.Samples[i];
                if (s?.Values == null || laneIdx >= s.Values.Length) continue;

                double v = s.Values[laneIdx];
                if (v > max) max = v;
            }
            return max;
        }





        // end of voiding calulation



        //Add NEW Bhushan
        private bool TryGetS1S2Window(out double t1, out double t2)
        {
            t1 = t2 = 0;

            if (_graph?.Markers == null)
                return false;

            var s1 = _graph.Markers.FirstOrDefault(m =>
                string.Equals(m.Label, "S1", StringComparison.OrdinalIgnoreCase));

            var s2 = _graph.Markers.FirstOrDefault(m =>
                string.Equals(m.Label, "S2", StringComparison.OrdinalIgnoreCase));

            if (s1 == null || s2 == null)
                return false;

            t1 = Math.Min(s1.T, s2.T);
            t2 = Math.Max(s1.T, s2.T);

            // safety: minimum width
            if (t2 - t1 < 0.5)
                return false;

            return true;
        }

        private bool TryGetBCStartTime(out double bcTime)
        {
            bcTime = 0;

            if (_graph?.Markers == null)
                return false;

            var bc = _graph.Markers.FirstOrDefault(m =>
                string.Equals(m.Label, "BC", StringComparison.OrdinalIgnoreCase));

            if (bc == null)
                return false;

            bcTime = bc.T;
            return true;
        }

        private bool TryGetFirstQuraRise(out double startTime)
        {
            startTime = 0;

            if (_graph?.Samples == null)
                return false;

            int quraIndex = FindLaneIndex("qura");
            if (quraIndex < 0)
                return false;

            foreach (var s in _graph.Samples)
            {
                if (s.Values == null || quraIndex >= s.Values.Length)
                    continue;

                // 🔑 threshold: real voiding
                if (s.Values[quraIndex] > 1.5)
                {
                    startTime = s.T;
                    return true;
                }
            }

            return false;
        }

        private void DrawVoidingGraph(GdiContext ctx, Graphics g, Rectangle outerRect, Size fSize)
        {
            // ================= this code for second page big graph shift top =================
            int shiftUp = fSize.Height * 2;
            int extraHeight = fSize.Height * 1; //this line for increase the graph Height
            outerRect = new Rectangle(
                outerRect.Left,
                outerRect.Top - shiftUp,
                outerRect.Width,
                outerRect.Height + extraHeight
            );

            int labelBoxWidth = fSize.Width * 4;

            Rectangle labelRect = new Rectangle(
                outerRect.Left,
                outerRect.Top,
                labelBoxWidth,
                outerRect.Height
            );

            Rectangle gRect = new Rectangle(
                labelRect.Right,
                outerRect.Top,
                outerRect.Width - labelBoxWidth,
                outerRect.Height
            );

            if (gRect.Width < fSize.Width * 5 || gRect.Height < fSize.Height * 4)
                return;

            //int curvesBottomMargin = fSize.Height * 3;
            //Rectangle curvesRect = new Rectangle(
            //    gRect.Left,
            //    gRect.Top,
            //    gRect.Width,
            //    Math.Max(gRect.Height - curvesBottomMargin, fSize.Height * 3)
            //);

            // Curves must fill the full graph frame.
            // Time axis is drawn BELOW the graph by DrawTimeAxisBottom, so reserving space here creates blank area.
            Rectangle curvesRect = gRect;

            //// ----------------------------
            //// 🔑 Decide START TIME
            //// ----------------------------
            //double? clipStart = null;
            //double? clipEnd = null;

            //// Case 1️⃣ BC exists → start at BC
            //if (TryGetBCStartTime(out double bcT))
            //{
            //    clipStart = bcT;
            //}
            //else
            //{
            //    // Case 2️⃣ No BC → try Qura rise
            //    if (TryGetFirstQuraRise(out double quraStart))
            //    {
            //        clipStart = quraStart;
            //    }
            //    // else → show full graph (clipStart stays null)
            //}

            //// End always = test end
            //if (_graph?.Samples != null && _graph.Samples.Count > 1)
            //    clipEnd = _graph.Samples.Last().T;

            // ----------------------------
            // PRINT MUST MATCH REVIEW: show FULL saved test
            // ----------------------------
            //double? clipStart = null;
            //double? clipEnd = null;

            //if (_graph?.Samples != null && _graph.Samples.Count > 1)
            //{
            //    clipStart = _graph.Samples[0].T;                 // usually 0
            //    clipEnd = _graph.Samples[_graph.Samples.Count - 1].T;
            //}

            // ----------------------------
            // START TIME (match Review behavior)
            // ----------------------------
            double? clipStart = null;
            double? clipEnd = null;

            if (_graph?.Samples != null && _graph.Samples.Count > 1)
            {
                // End always = last saved time
                clipEnd = _graph.Samples[_graph.Samples.Count - 1].T;

                // Review typically starts at BC if present, otherwise first Qura rise,
                // otherwise it shows full graph.
                if (TryGetBCStartTime(out double bcT))
                {
                    clipStart = bcT;
                }
                else if (TryGetFirstQuraRise(out double quraStart))
                {
                    clipStart = quraStart;
                }
                else
                {
                    clipStart = _graph.Samples[0].T; // fallback: full graph
                }

                // Safety
                if (clipStart.HasValue && clipEnd.HasValue && clipEnd.Value <= clipStart.Value)
                    clipEnd = clipStart.Value + 1.0;
            }

            // ----------------------------
            // Axes
            // ----------------------------
            DrawFillingAxes(ctx, g, labelRect, curvesRect, fSize);

            Region oldClip = g.Clip;
            g.SetClip(curvesRect);

            // ----------------------------
            // Curves
            // ----------------------------
            DrawFillingCurvesFromRecorded(g, curvesRect, clipStart, clipEnd);

            // ----------------------------
            // Markers
            // ----------------------------
            // Draw markers ONLY if BC exists OR full graph
            //if (clipStart == null || TryGetBCStartTime(out _))
            //{
            //    DrawMarkersOnGraph(g, curvesRect, clipStart, clipEnd);
            //}
            DrawMarkersOnGraph(g, curvesRect, clipStart, clipEnd);

            g.SetClip(oldClip, CombineMode.Replace);

            // ----------------------------
            // Time axis
            // ----------------------------
            DrawTimeAxisBottom(g, gRect, clipStart, clipEnd);
        }


        //private void DrawVoidingGraph(GdiContext ctx, Graphics g, Rectangle outerRect, Size fSize)
        //{
        //    // LEFT LABEL BOX
        //    int labelBoxWidth = fSize.Width * 4;

        //    Rectangle labelRect = new Rectangle(
        //        outerRect.Left,
        //        outerRect.Top,
        //        labelBoxWidth,
        //        outerRect.Height
        //    );

        //    // GRAPH FRAME
        //    Rectangle gRect = new Rectangle(
        //        labelRect.Right,
        //        outerRect.Top,
        //        outerRect.Width - labelBoxWidth,
        //        outerRect.Height
        //    );

        //    if (gRect.Width < fSize.Width * 5 || gRect.Height < fSize.Height * 4)
        //        return; // too small, avoid crashes

        //    // Inner rectangle for curves only (leave room at bottom for time axis)
        //    int curvesBottomMargin = fSize.Height * 3;
        //    Rectangle curvesRect = new Rectangle(
        //        gRect.Left,
        //        gRect.Top,
        //        gRect.Width,
        //        Math.Max(gRect.Height - curvesBottomMargin, fSize.Height * 3)
        //    );

        //    // Axes + labels + grid (re-use Filling logic)
        //    DrawFillingAxes(ctx, g, labelRect, gRect, fSize);

        //    // Curves + markers (same data)
        //    Region oldClip = g.Clip;
        //    g.SetClip(gRect);

        //    DrawFillingCurvesFromRecorded(g, curvesRect);
        //    DrawMarkersOnGraph(g, gRect);
        //    g.SetClip(oldClip, CombineMode.Replace);

        //    // Time axis at bottom
        //    DrawTimeAxisBottom(g, gRect);
        //}


        private int ResolveLaneIndex(ChannelCol channel, string[] namePatterns, int defaultIndex)
        {
            // 1) Try ActiveIndices mapping (channel -> lane)
            if (_graph?.ActiveIndices != null)
            {
                int chIndex = (int)channel;
                if (chIndex >= 0 && chIndex < _graph.ActiveIndices.Length)
                {
                    int laneIndex = _graph.ActiveIndices[chIndex];
                    if (_graph.TestDef?.Lanes != null &&
                        laneIndex >= 0 &&
                        laneIndex < _graph.TestDef.Lanes.Count)
                    {
                        return laneIndex;
                    }
                }
            }

            // 2) Try to find lane by name patterns (e.g. "qura", "flow", "qvol", "emg")
            if (_graph?.TestDef?.Lanes != null)
            {
                for (int i = 0; i < _graph.TestDef.Lanes.Count; i++)
                {
                    var lane = _graph.TestDef.Lanes[i];
                    string name = lane.Name ?? string.Empty;
                    string lower = name.ToLowerInvariant();

                    foreach (var p in namePatterns)
                    {
                        if (string.IsNullOrWhiteSpace(p)) continue;
                        if (lower.Contains(p.ToLowerInvariant()))
                            return i;
                    }
                }
            }

            // 3) Fallback to default index if it exists
            if (_graph?.TestDef?.Lanes != null &&
                defaultIndex >= 0 &&
                defaultIndex < _graph.TestDef.Lanes.Count)
            {
                return defaultIndex;
            }

            return -1;
        }





        void DrawObstructionNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {
            EnsureVoidingCalculated();
            string font = _data.FontFamily;

            // Layout (keep it close to your slot but consistent)
            int leftMargin = fSize.Width * 3;
            int rightMargin = fSize.Width * 2;
            int topMargin = fSize.Height * 1;
            int bottomMargin = fSize.Height * 1;

            Rectangle plot = new Rectangle(
                rect.Left + leftMargin,
                rect.Top + topMargin,
                Math.Max(10, rect.Width - leftMargin - rightMargin),
                Math.Max(10, rect.Height - topMargin - bottomMargin)
            );

            int x0 = plot.Left;
            int y0 = plot.Bottom;
            int x1 = plot.Right;
            int y1 = plot.Top;

            // Axes ranges from the image
            const double Q_MIN = 0.0, Q_MAX = 30.0;   // Qmax ml/sec
            const double P_MIN = 0.0, P_MAX = 150.0;  // Pdet cmH2O

            float X(double q)
            {
                if (q < Q_MIN) q = Q_MIN;
                if (q > Q_MAX) q = Q_MAX;
                return (float)(x0 + (q - Q_MIN) / (Q_MAX - Q_MIN) * plot.Width);
            }

            float Y(double p)
            {
                if (p < P_MIN) p = P_MIN;
                if (p > P_MAX) p = P_MAX;
                // IMPORTANT: divide by 150 (not 100)
                return (float)(y0 - (p - P_MIN) / (P_MAX - P_MIN) * plot.Height);
            }

            // Border box like the reference image
            ctx.SetPen(Color.Black, 1f);
            ctx.Rectangle(plot);

            // Axes (left + bottom bold)
            using (Pen axisPen = new Pen(Color.Black, 1.5f))
            {
                g.DrawLine(axisPen, x0, y1, x0, y0); // Y axis
                g.DrawLine(axisPen, x0, y0, x1, y0); // X axis
            }

            // Clip everything to plot to avoid drawing outside
            var oldState = g.Save();
            g.SetClip(plot);

            // Draw diagonal lines
            using (Pen bluePen = new Pen(Color.RoyalBlue, 1f))
            using (Pen greenPen = new Pen(Color.ForestGreen, 1f))
            {
                // -------- Obstruction (blue): BOOI lines --------
                // BOOI = Pdet - 2Q
                // Unobstructed/Equivocal boundary: BOOI=20 -> P = 2Q + 20
                DrawLineFunc(g, bluePen, X, Y, q => 2.0 * q + 20.0, Q_MIN, Q_MAX);

                // Equivocal/Obstructed boundary: BOOI=40 -> P = 2Q + 40
                DrawLineFunc(g, bluePen, X, Y, q => 2.0 * q + 40.0, Q_MIN, Q_MAX);

                // -------- Contractility (green): BCI lines --------
                // BCI = Pdet + 5Q
                // Weak/Normal boundary: BCI=100 -> P = 100 - 5Q
                DrawLineFunc(g, greenPen, X, Y, q => 100.0 - 5.0 * q, Q_MIN, Q_MAX);

                // Normal/Strong boundary: BCI=150 -> P = 150 - 5Q
                DrawLineFunc(g, greenPen, X, Y, q => 150.0 - 5.0 * q, Q_MIN, Q_MAX);
            }

            g.Restore(oldState);

            // Ticks + labels (outside clip)
            using (Font axisFont = new Font(font, 8f, FontStyle.Regular))
            using (Brush axisBrush = new SolidBrush(Color.Black))
            using (Pen tickPen = new Pen(Color.Black, 1f))
            {
                // Y ticks: 0, 50, 100, 150
                int[] yTicks = { 0, 50, 100, 150 };
                foreach (int v in yTicks)
                {
                    float yy = Y(v);
                    g.DrawLine(tickPen, x0 - 8, yy, x0, yy);
                    string s = v.ToString();
                    SizeF sz = g.MeasureString(s, axisFont);
                    g.DrawString(s, axisFont, axisBrush, x0 - 10 - sz.Width, yy - sz.Height / 2);
                }

                // X ticks: 0..30 step 5
                for (int v = 0; v <= 30; v += 5)
                {
                    float xx = X(v);
                    g.DrawLine(tickPen, xx, y0, xx, y0 + 8);
                    string s = v.ToString();
                    SizeF sz = g.MeasureString(s, axisFont);
                    g.DrawString(s, axisFont, axisBrush, xx - sz.Width / 2, y0 + 10);
                }
            }

            // Axis titles
            ctx.SetFont(font, 8f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // Y label (stacked)
            ctx.TextOutLeft(plot.Left - fSize.Width * 2, plot.Top + fSize.Height * 1, "Pdet");
            ctx.TextOutLeft(plot.Left - fSize.Width * 2, plot.Top + fSize.Height * 2, "cmH2O");

            // X label
            string xLabel = "Qmax Flow Rate ml/sec";
            Size xLabSize = ctx.MeasureText(xLabel);
            ctx.TextOutLeft(plot.Left + (plot.Width - xLabSize.Width) / 2, plot.Bottom + fSize.Height * 2, xLabel);

            // Title (match image text)
            ctx.SetTextColor(Color.Black);
            ctx.SetFont(font, 9f, FontStyle.Regular);
            string title = "Detrusor/Flow plot according to the ICS";
            Size tSz = ctx.MeasureText(title);
            ctx.TextOutLeft(plot.Left + (plot.Width - tSz.Width) / 2, rect.Top, title);

            // Region labels (green: Weak/Normal/Strong, blue: Unobstructed/Equivocal/Obstructed)
            using (Font regFont = new Font(font, 8f, FontStyle.Regular))
            using (Brush blueBrush = new SolidBrush(Color.RoyalBlue))
            using (Brush greenBrush = new SolidBrush(Color.ForestGreen))
            {
                // Green (contractility) - place near the green bands
                g.DrawString("Strong", regFont, greenBrush, X(2), Y(148));
                g.DrawString("Normal", regFont, greenBrush, X(2), Y(105));
                g.DrawString("Weak", regFont, greenBrush, X(2), Y(65));

                // Blue (obstruction) - place near the blue bands
                g.DrawString("Obstructed", regFont, blueBrush, X(22), Y(148));
                g.DrawString("Equivocal", regFont, blueBrush, X(22), Y(85));
                g.DrawString("Unobstructed", regFont, blueBrush, X(22), Y(55));
            }

            // Patient point (Qmax, Pdet@Qmax)
            double qmax = _data?.Voiding?.PeakFlowRate ?? 0.0;
            double pdet = _data?.Voiding?.PdetAtPeakflow ?? 0.0;

            if (double.IsNaN(qmax)) qmax = 0;
            if (double.IsNaN(pdet)) pdet = 0;

            qmax = Math.Max(Q_MIN, Math.Min(Q_MAX, qmax));
            pdet = Math.Max(P_MIN, Math.Min(P_MAX, pdet));

            float px = X(qmax);
            float py = Y(pdet);

            float radius = Math.Max(4f, fSize.Width * 0.25f);
            using (Brush redBrush = new SolidBrush(Color.Red))
            using (Pen redPen = new Pen(Color.Red, 1f))
            {
                g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
                g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
            }
        }

        // Helper to draw a function line clipped to plot range
        private static void DrawLineFunc(Graphics g, Pen pen, Func<double, float> X, Func<double, float> Y,
                                         Func<double, double> pOfQ, double qMin, double qMax)
        {
            var pts = new List<PointF>(64);
            for (double q = qMin; q <= qMax; q += 0.5)
            {
                double p = pOfQ(q);
                pts.Add(new PointF(X(q), Y(p)));
            }
            if (pts.Count >= 2)
                g.DrawLines(pen, pts.ToArray());
        }

        private void DrawAverageFlowNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {

            EnsureVoidingCalculated();
            string font = _data.FontFamily;

            // ----------- layout: make inner area nearly square -----------
            int margin = fSize.Width * 1;

            //int innerLeft = rect.Left;
            //int innerTop = rect.Top +  2;
            //int innerRight = rect.Right + 4;
            //int innerBottom = rect.Bottom + 2;

            int shiftRight = fSize.Width * 3;

            int innerLeft = rect.Left + shiftRight;
            int innerTop = rect.Top + 2;
            int innerRight = rect.Right;
            int innerBottom = rect.Bottom;

            int innerWidth = innerRight - innerLeft;
            int innerHeight = innerBottom - innerTop;

            // keep height as-is
            int graphHeight = innerHeight;

            // increase width (1.25x or 1.3x looks good)
            int graphWidth = (int)(innerWidth * 0.85);

            Rectangle box = new Rectangle(
                innerLeft,
                innerTop,
                graphWidth,
                graphHeight
            );



            // ----------- title at top (same line, shifted up) -----------
            ctx.SetFont(font, 9f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // move title ABOVE the box
            int titleY = box.Top - fSize.Height - 5;

            // "ml/sec"
            ctx.TextOutLeft(
                box.Left,
                titleY,
                "ml/sec"
            );

            // "Av.flow Nomogram" on same line
            ctx.TextOutLeft(
                box.Left + fSize.Width * 4,
                titleY,
                "Av.flow Nomogram"
            );

            // ----------- outer box -----------
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(box);

            // ----------- axes ranges & mapping -----------
            // X 0..500 (volume), Y 0..30 (flow)
            double xMin = 0.0, xMax = 500.0;
            double yMin = 0.0, yMax = 30.0;

            float X(double v) => (float)(box.Left + (v - xMin) / (xMax - xMin) * box.Width);
            float Y(double f) => (float)(box.Bottom - (f - yMin) / (yMax - yMin) * box.Height);

            // ----------- dashed grid -----------
            using (var gridPen = new Pen(Color.LightGray, 1f))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                // vertical grid 0,100,...,500
                for (int v = 100; v <= 500; v += 100)
                {
                    float x = X(v);
                    g.DrawLine(gridPen, x, box.Top, x, box.Bottom);
                }

                // horizontal grid 5,10,15,20,25
                for (int f = 5; f < 30; f += 5)
                {
                    float y = Y(f);
                    g.DrawLine(gridPen, box.Left, y, box.Right, y);
                }
            }

            // ----------- axes numeric labels -----------
            ctx.SetFont(font, 8f, FontStyle.Regular);

            // Y numbers (red): 0..30 on left
            ctx.SetTextColor(Color.Red);
            for (int f = 0; f <= 30; f += 5)
            {
                float y = Y(f);
                string s = f.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutRight(box.Left - 3, (int)(y - sz.Height / 2), s);
            }

            // X numbers (blue): 0,100,...,500
            ctx.SetTextColor(Color.Blue);
            for (int v = 0; v <= 500; v += 100)
            {
                float x = X(v);
                string s = v.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutLeft((int)(x - sz.Width / 2), box.Bottom + fSize.Height / 4, s);
            }


            // ----------- curves (hard-coded shape, visually matched) -----------
            using (var blackPen = new Pen(Color.Black, 1f))
            using (var bluePen = new Pen(Color.Blue, 1f))
            {
                double[] xs = { 0, 100, 200, 300, 400, 500 };

                // approximate Y values (ml/sec) for each curve
                double[] yPlus1 = { 12, 17, 21, 24, 27, 29 };
                double[] y0 = { 10, 15, 19, 22, 24, 26 };
                double[] yMinus1 = { 8, 13, 16, 19, 21, 23 };
                double[] yMinus2 = { 5, 8, 10, 11.5, 12.5, 13 }; // blue
                double[] yMinus3 = { 2, 4, 6, 7.5, 8.5, 9.5 };

                void DrawCurve(double[] ys, Pen pen)
                {
                    PointF[] pts = new PointF[xs.Length];
                    for (int i = 0; i < xs.Length; i++)
                        pts[i] = new PointF(X(xs[i]), Y(ys[i]));
                    g.DrawLines(pen, pts);
                }

                DrawCurve(yPlus1, blackPen);
                DrawCurve(y0, blackPen);
                DrawCurve(yMinus1, blackPen);
                DrawCurve(yMinus3, blackPen);
                DrawCurve(yMinus2, bluePen); // blue curve (-2)
            }

            // ----------- right-side labels (+1,0 mean,-1,-2,-3) -----------
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // positions near right edge of box
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(26), "+1");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(22), "0 mean");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(18), "-1");
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(12), "-2");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(8), "-3");


            // -----------------------------
            // Patient red dot (VoidedVolume vs AverageFlowRate)
            // -----------------------------
            double vv = _data?.Voiding?.VoidedVolume ?? 0.0;
            double qavg = _data?.Voiding?.AverageFlowRate ?? 0.0;

            if (double.IsNaN(vv)) vv = 0;
            if (double.IsNaN(qavg)) qavg = 0;

            // clamp to visible range of this nomogram
            vv = Math.Max(0, Math.Min(500, vv));
            qavg = Math.Max(0, Math.Min(30, qavg));

            float px = X(vv);
            float py = Y(qavg);

            float radius = Math.Max(4f, fSize.Width * 0.2f);

            using (Brush redBrush = new SolidBrush(Color.Red))
            using (Pen redPen = new Pen(Color.Red, 1f))
            {
                g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
                g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
            }




        }

        private void DrawPeakFlowNomogram(GdiContext ctx, Graphics g, Rectangle rect, Size fSize)
        {

            EnsureVoidingCalculated();
            string font = _data.FontFamily;

            int margin = fSize.Width * 1;

            //int innerLeft = rect.Left + 2;
            //int innerTop = rect.Top + 2;
            //int innerRight = rect.Right;
            //int innerBottom = rect.Bottom;

            int shiftRight = fSize.Width * 3;

            int innerLeft = rect.Left + shiftRight;
            int innerTop = rect.Top + 2;
            int innerRight = rect.Right;
            int innerBottom = rect.Bottom;

            int innerWidth = innerRight - innerLeft;
            int innerHeight = innerBottom - innerTop;

            // keep height as-is
            int graphHeight = innerHeight;

            // increase width (1.25x or 1.3x looks good)
            int graphWidth = (int)(innerWidth * 0.85);

            Rectangle box = new Rectangle(
                innerLeft,
                innerTop,
                graphWidth,
                graphHeight
            );



            // ----------- title at top (same line, shifted up) -----------
            ctx.SetFont(font, 9f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);

            // move title ABOVE the box
            int titleY = box.Top - fSize.Height - 5;

            // "ml/sec"
            ctx.TextOutLeft(
                box.Left,
                titleY,
                "ml/sec"
            );

            // "Av.flow Nomogram" on same line
            ctx.TextOutLeft(
                box.Left + fSize.Width * 4,
                titleY,
                "Peak flow Nomogram"
            );

            // Outer box
            ctx.SetPen(Color.Black, 1);
            ctx.Rectangle(box);

            // Axes ranges
            double xMin = 0.0, xMax = 500.0;
            double yMin = 0.0, yMax = 30.0;

            float X(double v) => (float)(box.Left + (v - xMin) / (xMax - xMin) * box.Width);
            float Y(double f) => (float)(box.Bottom - (f - yMin) / (yMax - yMin) * box.Height);

            // Grid
            using (var gridPen = new Pen(Color.LightGray, 1f))
            {
                gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                for (int v = 100; v <= 500; v += 100)
                    g.DrawLine(gridPen, X(v), box.Top, X(v), box.Bottom);

                for (int f = 5; f < 30; f += 5)
                    g.DrawLine(gridPen, box.Left, Y(f), box.Right, Y(f));
            }

            // Y numbers (red)
            ctx.SetFont(font, 8f, FontStyle.Regular);
            ctx.SetTextColor(Color.Red);
            for (int f = 0; f <= 30; f += 5)
            {
                float y = Y(f);
                string s = f.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutRight(box.Left - 3, (int)(y - sz.Height / 2), s);
            }

            // X numbers (blue)
            ctx.SetTextColor(Color.Blue);
            for (int v = 0; v <= 500; v += 100)
            {
                float x = X(v);
                string s = v.ToString();
                SizeF sz = ctx.MeasureText(s);
                ctx.TextOutLeft((int)(x - sz.Width / 2), box.Bottom + fSize.Height / 4, s);
            }

            // Curves
            using (var blackPen = new Pen(Color.Black, 1f))
            using (var bluePen = new Pen(Color.Blue, 1f))
            {
                double[] xs = { 0, 100, 200, 300, 400, 500 };

                // approximate Y values (ml/sec)
                double[] y0 = { 8, 14, 21, 26, 29, 30 };
                double[] yMinus1 = { 6, 11, 17, 21, 24, 26 };
                double[] yMinus2 = { 5, 9, 13, 16, 18, 19.5 }; // blue
                double[] yMinus3 = { 3, 5.5, 8, 10, 11.5, 13 };

                PointF[] pts = new PointF[xs.Length];

                void DrawCurve(double[] ys, Pen pen)
                {
                    for (int i = 0; i < xs.Length; i++)
                        pts[i] = new PointF(X(xs[i]), Y(ys[i]));
                    g.DrawLines(pen, pts);
                }

                DrawCurve(y0, blackPen);
                DrawCurve(yMinus1, blackPen);
                DrawCurve(yMinus3, blackPen);
                DrawCurve(yMinus2, bluePen);
            }

            // right-side labels: 0, -1 mean, -2, -3
            ctx.SetFont(font, 8.5f, FontStyle.Regular);
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(27), "0");
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(22), "-1 mean");
            ctx.SetTextColor(Color.Blue);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(16), "-2");
            ctx.SetTextColor(Color.Black);
            ctx.TextOutLeft(box.Right + fSize.Width / 2, (int)Y(10), "-3");

            // -----------------------------
            // Patient red dot (VoidedVolume vs PeakFlowRate)
            // -----------------------------
            double vv = _data?.Voiding?.VoidedVolume ?? 0.0;
            double qmax = _data?.Voiding?.PeakFlowRate ?? 0.0;

            if (double.IsNaN(vv)) vv = 0;
            if (double.IsNaN(qmax)) qmax = 0;

            // clamp to visible range of this nomogram
            vv = Math.Max(0, Math.Min(500, vv));
            qmax = Math.Max(0, Math.Min(30, qmax));

            float px = X(vv);
            float py = Y(qmax);

            float radius = Math.Max(4f, fSize.Width * 0.2f);

            using (Brush redBrush = new SolidBrush(Color.Red))
            using (Pen redPen = new Pen(Color.Red, 1f))
            {
                g.FillEllipse(redBrush, px - radius, py - radius, radius * 2, radius * 2);
                g.DrawEllipse(redPen, px - radius, py - radius, radius * 2, radius * 2);
            }

        }


        #endregion

        #region Page 3 – Markers Details

        private void DrawMarkerPage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            // Header + patient info
            //DrawHeaderBlock(ctx, rect, fSize);

            if (_data.HeadThree == true)
            {
                if (_data.DefaultHeader == true)
                {
                    DrawHeaderBlock(ctx, rect, fSize);
                }
                else if (_data.LetterHead == true)
                {
                    DrawLatterHeadImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalLogo == true)
                {
                    DrawHeaderLogoImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalDataAndLogo == true)
                {
                    DrawHeaderBlockWithLogoLeft(ctx, g, rect, fSize);
                }
            }
            else
            {

            }

            DrawPatientBlock(ctx, g, rect, fSize);
            //DrawPatientShortDetails(ctx, g, rect, fSize);

            int left = rect.Left;
            int top = rect.Top + fSize.Height * 12;

            DrawMarkersTable(ctx, left, top, fSize);

            DrawFooter(ctx, g, rect, fSize, 0, _totalPages);

            //if (_data.CompanyName && _data.LogoImage != null && _data.LogoImage.Length > 0)
            //{
            //    // --------------------------------------------------
            //    // FOOTER BASE LINE
            //    // --------------------------------------------------
            //    int footerTopY = rect.Bottom - fSize.Height + 90;

            //    // 1️⃣ Separator line (black)
            //    using (Pen linePen = new Pen(Color.Black, 1))
            //    {
            //        g.DrawLine(
            //            linePen,
            //            rect.Left,
            //            footerTopY,
            //            rect.Right,
            //            footerTopY
            //        );
            //    }

            //    // --------------------------------------------------
            //    // FOOTER CONTENT AREA
            //    // --------------------------------------------------
            //    int contentY = (int)(footerTopY + fSize.Height + 10);

            //    using (var ms = new MemoryStream(_data.LogoImage))
            //    using (Image logoImg = Image.FromStream(ms))
            //    {
            //        // 2️⃣ Logo size
            //        int logoHeight = fSize.Height * 2;
            //        int logoWidth = logoHeight * logoImg.Width / logoImg.Height;

            //        int logoX = rect.Left + fSize.Width - 20;   // LEFT aligned
            //        int logoY = contentY - logoHeight / 4;

            //        // Draw logo
            //        g.DrawImage(
            //            logoImg,
            //            new Rectangle(logoX, logoY, logoWidth, logoHeight)
            //        );

            //        // 3️⃣ Footer text (right of logo)
            //        ctx.SetFont(font, 12f);
            //        ctx.SetTextColor(Color.Blue);

            //        int textX = logoX + logoWidth + fSize.Width * 2;

            //        ctx.TextOutLeft(
            //            textX,
            //            contentY,
            //            "Santron Meditronic, India  |  www.santronmeditronic.com"
            //        );
            //    }

            //    ctx.SetTextColor(Color.Black);
            //}

        }

        //private void DrawMarkersTable(GdiContext ctx, int left, int top, Size fSize)
        //{
        //    string font = _data.FontFamily;
        //    int r = 2;


        //    // Title
        //    ctx.SetFont(font, 12f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Red);
        //    ctx.TextOutLeft(left, top + fSize.Height * r, "Markers Details");
        //    r += 2;

        //    // Column headers
        //    ctx.SetFont(font, 11f, FontStyle.Regular);
        //    ctx.SetTextColor(Color.Black);

        //    int colType = left;
        //    int colTime = left + fSize.Width * 8;
        //    int colPves = left + fSize.Width * 12;
        //    int colPabd = left + fSize.Width * 16;
        //    int colPdet = left + fSize.Width * 20;
        //    int colVinf = left + fSize.Width * 24;
        //    int colQvol = left + fSize.Width * 28;
        //    int colQura = left + fSize.Width * 32;
        //    int colEmg = left + fSize.Width * 36;

        //    // First header row
        //    ctx.TextOutLeft(colType, top + fSize.Height * r, "Marker");
        //    ctx.TextOutLeft(colTime, top + fSize.Height * r, "Time");
        //    ctx.TextOutLeft(colPves, top + fSize.Height * r, "Pves");
        //    ctx.TextOutLeft(colPabd, top + fSize.Height * r, "Pabd");
        //    ctx.TextOutLeft(colPdet, top + fSize.Height * r, "Pdet");
        //    ctx.TextOutLeft(colVinf, top + fSize.Height * r, "Vinf");
        //    ctx.TextOutLeft(colQvol, top + fSize.Height * r, "Qvol");
        //    ctx.TextOutLeft(colQura, top + fSize.Height * r, "Qura");
        //    ctx.TextOutLeft(colEmg, top + fSize.Height * r, "EMG");
        //    r++;

        //    // Units row
        //    ctx.SetFont(font, 8f, FontStyle.Regular);
        //    ctx.TextOutLeft(colType, top + fSize.Height * r, "Type");
        //    ctx.TextOutLeft(colTime, top + fSize.Height * r, "mins.");
        //    ctx.TextOutLeft(colPves, top + fSize.Height * r, "cmH2O");
        //    ctx.TextOutLeft(colPabd, top + fSize.Height * r, "cmH2O");
        //    ctx.TextOutLeft(colPdet, top + fSize.Height * r, "cmH2O");
        //    ctx.TextOutLeft(colVinf, top + fSize.Height * r, "ml");
        //    ctx.TextOutLeft(colQvol, top + fSize.Height * r, "ml");
        //    ctx.TextOutLeft(colQura, top + fSize.Height * r, "ml/sec");
        //    ctx.TextOutLeft(colEmg, top + fSize.Height * r, "uV");
        //    r += 2;

        //    // Data rows
        //    ctx.SetFont(font, 9f, FontStyle.Regular);


        //    var rows = new List<MarkerRow>();

        //    // Existing fixed markers (FS, FD, etc.)
        //    if (_data.Markers != null)
        //        rows.AddRange(_data.Markers);

        //    // Dynamic graph markers
        //    rows.AddRange(BuildDynamicMarkerRows());

        //    foreach (var m in rows)
        //    {
        //        //Start This line for MARKERS DETAILS section not show the images marker details add on 09-01-2026
        //        if (m.Type.StartsWith("I-"))
        //            continue;
        //        //End This line for MARKERS DETAILS section not show the images marker details add on 09-01-2026

        //        ctx.TextOutLeft(colType, top + fSize.Height * r, m.Type);
        //        ctx.TextOutLeft(colTime, top + fSize.Height * r, m.TimeText);
        //        ctx.TextOutLeft(colPves, top + fSize.Height * r, m.Pves.ToString("F0"));
        //        ctx.TextOutLeft(colPabd, top + fSize.Height * r, m.Pabd.ToString("F0"));
        //        ctx.TextOutLeft(colPdet, top + fSize.Height * r, m.Pdet.ToString("F0"));
        //        ctx.TextOutLeft(colVinf, top + fSize.Height * r, m.Vinf.ToString("F0"));
        //        ctx.TextOutLeft(colQvol, top + fSize.Height * r, m.Qvol.ToString("F0"));
        //        ctx.TextOutLeft(colQura, top + fSize.Height * r, m.Qura.ToString("F0"));
        //        ctx.TextOutLeft(colEmg, top + fSize.Height * r, m.EMG.ToString("F0"));
        //        r++;
        //    }

        //}

        //private void DrawMarkersTable(GdiContext ctx, int left, int top, Size fSize)
        //{
        //    string font = _data.FontFamily;

        //    int rowH = fSize.Height + 15;
        //    int tableWidth = fSize.Width * 44;

        //    //Color blue = Color.FromArgb(40, 70, 160);
        //    Color blue = Color.RoyalBlue;
        //    Color gridGray = Color.FromArgb(180, 180, 180);

        //    Brush blueBrush = new SolidBrush(blue);
        //    Brush whiteBrush = Brushes.White;

        //    // Force gray borders
        //    ctx.SetPen(gridGray, 1);

        //    // ================= HELPER =================
        //    int MeasureText(string text, float size, FontStyle style)
        //    {
        //        using (var bmp = new Bitmap(1, 1))
        //        using (var g = Graphics.FromImage(bmp))
        //        using (var f = new Font(font, size, style))
        //        {
        //            return (int)g.MeasureString(text, f).Width;
        //        }
        //    }

        //    // ================= TITLE =================
        //    //ctx.SetFont(font, 12f, FontStyle.Bold);
        //    //ctx.SetTextColor(Color.Red);

        //    //string title = "Marker Details";
        //    //ctx.TextOutLeft(left + (tableWidth - MeasureText(title, 12f, FontStyle.Bold)) / 2, top, title);
        //    //top += rowH * 2;

        //    // =====================================================
        //    // 🔷 GRADIENT HEADER : "Marker Details"
        //    // =====================================================
        //    int titleHeight = rowH + 8;
        //    int headerLeft = left - 4;
        //    int headerTop = top + 80;
        //    int headerWidth = tableWidth + 8;

        //    // remove any active pen to avoid border lines
        //    //ctx.SetPen(Color.Transparent, 0);

        //    using (LinearGradientBrush headerBrush =
        //        new LinearGradientBrush(
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
        //            Color.White,          // left
        //            Color.FromArgb(210, 225, 240),
        //            //Color.RoyalBlue,      // right
        //            LinearGradientMode.Horizontal))
        //    {
        //        ctx.FillRectangle(
        //            headerBrush,
        //            new Rectangle(headerLeft, headerTop, headerWidth, titleHeight)
        //        );
        //    }

        //    ctx.SetFont(font, 12f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Black);

        //    ctx.TextOutLeft(
        //        headerLeft + 12,
        //        headerTop + (titleHeight - fSize.Height) / 2,
        //        "Marker Details"
        //    );

        //    // move table below header
        //    top += titleHeight + rowH;
        //    top += rowH * 3;
        //    // =====================================================

        //    // ================= COLUMN POSITIONS =================
        //    int[] cols =
        //    {
        //        left,
        //        left + fSize.Width * 9,
        //        left + fSize.Width * 13,
        //        left + fSize.Width * 17,
        //        left + fSize.Width * 21,
        //        left + fSize.Width * 25,
        //        left + fSize.Width * 29,
        //        left + fSize.Width * 33,
        //        left + fSize.Width * 37,
        //        left + fSize.Width * 41
        //    };

        //    int[] w =
        //    {
        //        cols[1] - cols[0],
        //        cols[2] - cols[1],
        //        cols[3] - cols[2],
        //        cols[4] - cols[3],
        //        cols[5] - cols[4],
        //        cols[6] - cols[5],
        //        cols[7] - cols[6],
        //        cols[8] - cols[7],
        //        tableWidth - cols[8]
        //    };

        //    // ================= HEADER ROW =================
        //    Rectangle header = new Rectangle(left, top, tableWidth, rowH);
        //    ctx.FillRectangle(blueBrush, header);
        //    ctx.Rectangle(header);

        //    ctx.SetFont(font, 12f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.White);

        //    string[] headers =
        //    {
        //        "Marker", "Time", "Pves", "Pabd", "Pdet", "Vinf", "Qvol", "Qura", "EMG"
        //    };

        //    for (int i = 0; i < headers.Length; i++)
        //    {
        //        ctx.TextOutLeft(
        //            cols[i] + (w[i] - MeasureText(headers[i], 9f, FontStyle.Bold)) / 2,
        //            top + 5,
        //            headers[i]);
        //    }

        //    top += rowH;

        //    Color unitBgColor = Color.FromArgb(230, 230, 254);
        //    Brush unitBgBrush = new SolidBrush(unitBgColor);


        //    // ================= UNITS ROW (FIRST COLUMN BLUE) =================
        //    Rectangle unitRow = new Rectangle(left, top, tableWidth, rowH);
        //    ctx.FillRectangle(unitBgBrush, unitRow);
        //    ctx.Rectangle(unitRow);


        //    ctx.SetFont(font, 12f, FontStyle.Bold);

        //    string[] units =
        //    {
        //        "Type", "mins", "cmH2O", "cmH2O", "cmH2O", "ml", "ml", "ml/sec", "uV"
        //    };

        //    for (int i = 0; i < units.Length; i++)
        //    {
        //        ctx.SetTextColor(i == 0 ? Color.Black : Color.Black);
        //        ctx.TextOutLeft(
        //            cols[i] + (w[i] - MeasureText(units[i], 12f, FontStyle.Bold)) / 2,
        //            top + 5,
        //            units[i]);
        //    }

        //    top += rowH;

        //    // ================= BLANK ROW =================
        //    Rectangle blankRow = new Rectangle(left, top, tableWidth, rowH);
        //    ctx.FillRectangle(whiteBrush, blankRow);
        //    ctx.Rectangle(blankRow);

        //    top += rowH;

        //    // ================= DATA ROWS =================
        //    ctx.SetFont(font, 12f, FontStyle.Regular);

        //    var rows = new List<MarkerRow>();
        //    if (_data.Markers != null)
        //        rows.AddRange(_data.Markers);

        //    rows.AddRange(BuildDynamicMarkerRows());

        //    foreach (var m in rows)
        //    {
        //        if (m.Type.StartsWith("I-"))
        //            continue;

        //        Rectangle row = new Rectangle(left, top, tableWidth, rowH);
        //        ctx.FillRectangle(whiteBrush, row);
        //        ctx.Rectangle(row);

        //        string[] values =
        //        {
        //            m.Type,
        //            m.TimeText,
        //            m.Pves.ToString("F0"),
        //            m.Pabd.ToString("F0"),
        //            m.Pdet.ToString("F0"),
        //            m.Vinf.ToString("F0"),
        //            m.Qvol.ToString("F0"),
        //            m.Qura.ToString("F0"),
        //            m.EMG.ToString("F0")
        //        };


        //        for (int i = 0; i < values.Length; i++)
        //        {
        //            // 🔵 First column = BLUE + BOLD
        //            if (i == 0)
        //            {
        //                ctx.SetFont(font, 12f, FontStyle.Bold);
        //                ctx.SetTextColor(blue);
        //            }
        //            else
        //            {
        //                ctx.SetFont(font, 12f, FontStyle.Regular);
        //                ctx.SetTextColor(Color.Black);
        //            }

        //            ctx.TextOutLeft(
        //                cols[i] + (w[i] - MeasureText(values[i], 12f, i == 0 ? FontStyle.Bold : FontStyle.Regular)) / 2,
        //                top + 5,
        //                values[i]);
        //        }

        //        top += rowH;
        //    }
        //}

        private void DrawMarkersTable(GdiContext ctx, int left, int top, Size fSize)
        {
            string font = _data.FontFamily;

            int rowH = fSize.Height + 15;
            int tableWidth = fSize.Width * 44;

            Color blue = Color.RoyalBlue;
            Color gridGray = Color.FromArgb(180, 180, 180);

            using (Brush blueBrush = new SolidBrush(blue))
            using (Brush whiteBrush = new SolidBrush(Color.White))
            using (Brush unitBgBrush = new SolidBrush(Color.FromArgb(230, 230, 254)))
            {
                // Force gray borders
                ctx.SetPen(gridGray, 1);

                // ================= HELPER =================
                int MeasureText(string text, float size, FontStyle style)
                {
                    using (var bmp = new Bitmap(1, 1))
                    using (var g = Graphics.FromImage(bmp))
                    using (var f = new Font(font, size, style))
                    {
                        return (int)g.MeasureString(text ?? "", f).Width;
                    }
                }

                bool HasLane(params string[] tokens)
                {
                    var lanes = _graph?.TestDef?.Lanes;
                    if (lanes == null || lanes.Count == 0) return false;

                    for (int i = 0; i < lanes.Count; i++)
                    {
                        string n = (lanes[i]?.Name ?? "").ToLowerInvariant();
                        for (int k = 0; k < tokens.Length; k++)
                        {
                            string t = (tokens[k] ?? "").ToLowerInvariant();
                            if (t.Length > 0 && n.Contains(t))
                                return true;
                        }
                    }
                    return false;
                }

                // ================= TITLE HEADER STRIP =================
                int titleHeight = rowH + 8;
                int headerLeft = left - 4;
                int headerTop = top + 80;
                int headerWidth = tableWidth + 8;

                using (var headerBrush = new LinearGradientBrush(
                    new Rectangle(headerLeft, headerTop, headerWidth, titleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    LinearGradientMode.Horizontal))
                {
                    ctx.FillRectangle(headerBrush, new Rectangle(headerLeft, headerTop, headerWidth, titleHeight));
                }

                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);
                ctx.TextOutLeft(headerLeft + 12, headerTop + (titleHeight - fSize.Height) / 2, "Marker Details");

                // Move table below header
                top += titleHeight + rowH;
                top += rowH * 3;

                // ================= BUILD DYNAMIC COLUMNS =================
                // Always show Marker + Time
                var headers = new List<string>();
                var units = new List<string>();
                var getters = new List<Func<MarkerRow, string>>();
                var weights = new List<float>();
                var blueBold = new List<bool>();

                headers.Add("Marker"); units.Add("Type"); getters.Add(m => m?.Type ?? ""); weights.Add(2.5f); blueBold.Add(true);
                headers.Add("Time"); units.Add("mins"); getters.Add(m => m?.TimeText ?? ""); weights.Add(1.0f); blueBold.Add(false);

                // Only include channels present in THIS test’s lanes
                if (HasLane("pves", "ves"))
                {
                    headers.Add("Pves"); units.Add("cmH2O"); getters.Add(m => m.Pves.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                if (HasLane("pabd", "abd", "pirp"))
                {
                    headers.Add("Pabd"); units.Add("cmH2O"); getters.Add(m => m.Pabd.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                if (HasLane("pdet", "det"))
                {
                    headers.Add("Pdet"); units.Add("cmH2O"); getters.Add(m => m.Pdet.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                if (HasLane("vinf", "infus"))
                {
                    headers.Add("Vinf"); units.Add("ml"); getters.Add(m => m.Vinf.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                if (HasLane("qvol", "vol"))
                {
                    headers.Add("Qvol"); units.Add("ml"); getters.Add(m => m.Qvol.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                // Flow/Qura can be named differently in your tests (Flow/Frate/Qrate/Qura)
                if (HasLane("qura", "qrate", "flow", "frate"))
                {
                    headers.Add("Qura"); units.Add("ml/sec"); getters.Add(m => m.Qura.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }
                if (HasLane("emg"))
                {
                    headers.Add("EMG"); units.Add("uV"); getters.Add(m => m.EMG.ToString("F0")); weights.Add(1.0f); blueBold.Add(false);
                }

                // ================= CALC COLUMN POSITIONS (WEIGHTED) =================
                float totalW = 0f;
                for (int i = 0; i < weights.Count; i++) totalW += Math.Max(0.5f, weights[i]);

                int colCount = headers.Count;
                int[] colX = new int[colCount];
                int[] colW = new int[colCount];

                int x = left;
                for (int i = 0; i < colCount; i++)
                {
                    colX[i] = x;
                    int w = (int)Math.Round(tableWidth * (Math.Max(0.5f, weights[i]) / totalW));
                    if (i == colCount - 1) w = (left + tableWidth) - x; // consume remainder
                    colW[i] = w;
                    x += w;
                }

                // ================= HEADER ROW =================
                Rectangle header = new Rectangle(left, top, tableWidth, rowH);
                ctx.FillRectangle(blueBrush, header);
                ctx.Rectangle(header);

                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.White);

                for (int i = 0; i < colCount; i++)
                {
                    string h = headers[i];
                    ctx.TextOutLeft(
                        colX[i] + (colW[i] - MeasureText(h, 12f, FontStyle.Bold)) / 2,
                        top + 5,
                        h
                    );
                }

                top += rowH;

                // ================= UNITS ROW =================
                Rectangle unitRow = new Rectangle(left, top, tableWidth, rowH);
                ctx.FillRectangle(unitBgBrush, unitRow);
                ctx.Rectangle(unitRow);

                ctx.SetFont(font, 11f, FontStyle.Bold);
                ctx.SetTextColor(Color.Black);

                for (int i = 0; i < colCount; i++)
                {
                    string u = units[i];
                    ctx.TextOutLeft(
                        colX[i] + (colW[i] - MeasureText(u, 12f, FontStyle.Bold)) / 2,
                        top + 5,
                        u
                    );
                }

                top += rowH;

                // ================= BLANK ROW =================
                Rectangle blankRow = new Rectangle(left, top, tableWidth, rowH);
                ctx.FillRectangle(whiteBrush, blankRow);
                ctx.Rectangle(blankRow);
                top += rowH;

                // ================= DATA ROWS =================
                ctx.SetFont(font, 11f, FontStyle.Regular);

                var rows = new List<MarkerRow>();
                if (_data.Markers != null)
                    rows.AddRange(_data.Markers);

                rows.AddRange(BuildDynamicMarkerRows());

                // Sort markers by time in ascending order
                rows = rows.OrderBy(m => ParseTimeToSeconds(m.TimeText)).ToList();


                int generalEventCounter = 0;

                foreach (var m in rows)
                {
                    if (!string.IsNullOrEmpty(m.Type) && m.Type.StartsWith("I-"))
                        continue;

                    bool isGPMarker = IsNumeric(m.Type);

                    string displayType = m.Type;

                    // If it's a general event marker (numeric), prefix with "General Event"
                    if (isGPMarker)
                    {
                        generalEventCounter++;
                        displayType = $"General Event {generalEventCounter}";
                    }

                    Rectangle row = new Rectangle(left, top, tableWidth, rowH);
                    ctx.FillRectangle(whiteBrush, row);
                    ctx.Rectangle(row);

                    for (int i = 0; i < colCount; i++)
                    {
                        bool isBlueBold = blueBold[i];

                        //if (isBlueBold)
                        //{
                        //    ctx.SetFont(font, 11f, FontStyle.Bold);
                        //    ctx.SetTextColor(blue);
                        //}
                        //else
                        //{
                        //    ctx.SetFont(font, 11f, FontStyle.Regular);
                        //    ctx.SetTextColor(Color.Black);
                        //}


                        //Start This Code Add For GP Marker Color show Dynamic previously show Black add on 18-02-2026 chnage location (Client Office)
                        // For the Type column (first column), use GP color if it's a GP marker
                        if (i == 0 && isGPMarker)
                        {
                            int gpColorValue = _data?.GeneralPurposeColor ?? 0;
                            if (gpColorValue != 0)
                            {
                                ctx.SetFont(font, 11f, FontStyle.Bold);
                                ctx.SetTextColor(Color.FromArgb(gpColorValue));
                            }
                            else
                            {
                                ctx.SetFont(font, 11f, FontStyle.Bold);
                                ctx.SetTextColor(blue); // Fallback to blue
                            }
                        }
                        else if (isBlueBold)
                        {
                            ctx.SetFont(font, 11f, FontStyle.Bold);
                            ctx.SetTextColor(blue);
                        }
                        else
                        {
                            ctx.SetFont(font, 11f, FontStyle.Regular);
                            ctx.SetTextColor(Color.Black);
                        }
                        //End This Code Add For GP Marker Color show Dynamic previously show Black add on 18-02-2026 chnage location (Client Office)

                        //string val = "";
                        //try { val = getters[i]?.Invoke(m) ?? ""; } catch { val = ""; }

                        //ctx.TextOutLeft(
                        //    colX[i] + (colW[i] - MeasureText(val, 11f, isBlueBold ? FontStyle.Bold : FontStyle.Regular)) / 2,
                        //    top + 5,
                        //    val
                        //);

                        string val = "";
                        try
                        {
                            // Use displayType for the first column, otherwise use the getter
                            if (i == 0)
                                val = displayType;
                            else
                                val = getters[i]?.Invoke(m) ?? "";
                        }
                        catch { val = ""; }

                        ctx.TextOutLeft(
                            colX[i] + (colW[i] - MeasureText(val, 11f, (i == 0 || isBlueBold) ? FontStyle.Bold : FontStyle.Regular)) / 2,
                            top + 5,
                            val
                        );

                    }

                    top += rowH;
                }
            }
        }

        private double ParseTimeToSeconds(string timeText)
        {
            if (string.IsNullOrEmpty(timeText))
                return double.MaxValue; // Put empty times at the end

            try
            {
                // Assuming time format is like "MM:SS" or minutes with decimal
                if (timeText.Contains(":"))
                {
                    var parts = timeText.Split(':');
                    if (parts.Length == 2)
                    {
                        double minutes = double.Parse(parts[0]);
                        double seconds = double.Parse(parts[1]);
                        return (minutes * 60) + seconds;
                    }
                }

                // Try parsing as double (minutes)
                return double.Parse(timeText) * 60;
            }
            catch
            {
                return double.MaxValue; // If parsing fails, put at the end
            }
        }

        private bool IsNumeric(string s)
        {
            return int.TryParse(s, out _);
        }


        private List<MarkerRow> BuildDynamicMarkerRows()
        {
            var rows = new List<MarkerRow>();

            if (_graph?.Markers == null || _graph?.Samples == null)
                return rows;

            int gpCounter = 0;
            int imageCounter = 0;

            foreach (var m in _graph.Markers)
            {
                if (string.IsNullOrWhiteSpace(m.Label))
                    continue;

                string raw = m.Label.Trim().ToUpperInvariant();
                string displayName = null;

                if (raw.StartsWith("I"))   // or use your exact image rule
                {
                    imageCounter++;
                    displayName = $"I-{imageCounter}";
                }

                // 🔑 GP detection: numeric labels
                else if (IsNumeric(raw))
                {
                    //Start This Code Add For GP Marker Color show Dynamic previously show Black add on 18-02-2026 chnage location (Client Office)
                    gpCounter++;
                    displayName = raw;
                    //Old This Code Add For GP Marker Color show Dynamic previously show Black add on 18-02-2026 chnage location (Client Office)

                    //Old Code
                    //gpCounter++;
                    //displayName = $"General Event {raw}";
                }
                else if (raw == "CU")
                {
                    displayName = "Cough";
                }
                else if (raw == "LE")
                {
                    displayName = "Leak";
                }
                else if (raw == "UDC")
                {
                    displayName = "UDC";
                }
                else
                {
                    continue; // ignore unknown markers
                }

                var p = ComputeBladderPointForTime(m.T);

                rows.Add(new MarkerRow
                {
                    Type = displayName,
                    TimeText = FormatSeconds(m.T),
                    Vinf = p.Vinf,
                    Pves = p.Pves,
                    Pabd = p.Pabd,
                    Pdet = p.Pdet,
                    Qvol = p.Qvol,
                    Qura = p.Qura,
                    EMG = p.EMG
                });
            }

            return rows;
        }


        private BladderPoint ComputeBladderPointForTime(double t)
        {
            if (_graph == null || _graph.Samples == null || _graph.Samples.Count == 0)
                return new BladderPoint();

            var best = _graph.Samples
                .OrderBy(s => Math.Abs(s.T - t))
                .FirstOrDefault();

            if (best?.Values == null)
                return new BladderPoint();

            int idxPves = FindLaneIndex("pves");
            int idxPabd = FindLaneIndex("pabd");
            int idxPdet = FindLaneIndex("pdet");
            int idxVinf = FindLaneIndex("vinf");
            int idxQvol = FindLaneIndex("qvol");
            int idxQura = FindLaneIndex("qura");
            int idxEmg = FindLaneIndex("emg");

            return new BladderPoint
            {
                Vinf = SafeSample(best.Values, idxVinf),
                Pves = SafeSample(best.Values, idxPves),
                Pabd = SafeSample(best.Values, idxPabd),
                Pdet = SafeSample(best.Values, idxPdet),
                Qvol = SafeSample(best.Values, idxQvol),
                Qura = SafeSample(best.Values, idxQura),
                EMG = SafeSample(best.Values, idxEmg),
            };
        }


        #endregion


        #region Page 4 – Patient History

        private bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private int DrawGradientSectionHeader(GdiContext ctx, Rectangle rect, int top, Size fSize, string title)
        {
            //DrawConclusionPageData(ctx, g, logicalRect);
            int rowH = fSize.Height + 8;
            int titleHeight = rowH + 6;

            int sideMargin = fSize.Width;
            int headerLeft = rect.Left - 25 + sideMargin;
            int headerWidth = rect.Width - sideMargin;

            // remove any active pen to avoid lines
            ctx.SetPen(Color.Transparent, 0);

            using (LinearGradientBrush headerBrush =
                new LinearGradientBrush(
                    new Rectangle(headerLeft, top, headerWidth, titleHeight),
                    Color.White,
                    Color.FromArgb(210, 225, 240),
                    //Color.RoyalBlue,
                    LinearGradientMode.Horizontal))
            {
                ctx.FillRectangle(
                    headerBrush,
                    new Rectangle(headerLeft, top, headerWidth, titleHeight)
                );
            }

            ctx.SetFont(_data.FontFamily, 12f, FontStyle.Bold);
            ctx.SetTextColor(Color.Black);

            ctx.TextOutLeft(
                headerLeft + 12,
                top + (titleHeight - fSize.Height) / 2,
                title
            );

            // return new Y position after header
            return top + titleHeight + fSize.Height;
        }
        private void DrawConclusionPage(GdiContext ctx, Graphics g, Rectangle rect)
        {
            //DrawConclusionPageData(ctx, g, logicalRect);

            string font = _data.FontFamily;
            ctx.SetFont(font, 10f);
            Size fSize = ctx.MeasureText("50");

            int graphShiftDown = fSize.Height * 2;   // adjust as needed


            // header area + space for results
            int topHeaderSpace = fSize.Height * 15;
            int bottomResultsSpace = fSize.Height * 22;   // room for "Filling Phase Results"

            //int graphTop = rect.Top + topHeaderSpace;
            //int graphHeight = rect.Height - topHeaderSpace - bottomResultsSpace;

            int graphTop = rect.Top + topHeaderSpace + graphShiftDown;
            int graphHeight = rect.Height - topHeaderSpace - bottomResultsSpace - graphShiftDown;


            if (graphHeight < fSize.Height * 5)
                graphHeight = fSize.Height * 5;

            // --- LEFT LABEL BOX (channel names + units + scales) ---
            int labelBoxWidth = fSize.Width * 4;

            Rectangle labelRect = new Rectangle(
                rect.Left + fSize.Width,   // small inner margin from page left
                graphTop,
                labelBoxWidth,
                graphHeight
            );

            // --- GRAPH FRAME (to the right of label box) ---
            int graphLeft = labelRect.Right;        // gap between label & graph
            int graphRight = rect.Right - fSize.Width;               // inner right margin
            int graphWidth = Math.Max(graphRight - graphLeft, fSize.Width * 5);

            Rectangle gRect = new Rectangle(
                graphLeft,
                graphTop,
                graphWidth,
                graphHeight
            );

            // Inner rectangle for curves only (leave room at bottom for time axis)
            //int curvesBottomMargin = fSize.Height * 4;
            //Rectangle curvesRect = new Rectangle(
            //    gRect.Left,
            //    gRect.Top,
            //    gRect.Width,
            //    Math.Max(gRect.Height - curvesBottomMargin, fSize.Height * 3)
            //);

            // Curves must fill the full graph frame.
            // Time axis is drawn BELOW the graph by DrawTimeAxisBottom, so reserving space here creates blank area.
            Rectangle curvesRect = gRect;

            // 1) Header + patient block
            //DrawHeaderBlock(ctx, rect, fSize);

            if (_data.HeadOne == true)
            {
                if (_data.DefaultHeader == true)
                {
                    DrawHeaderBlock(ctx, rect, fSize);
                }
                else if (_data.LetterHead == true)
                {
                    DrawLatterHeadImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalLogo == true)
                {
                    DrawHeaderLogoImage(ctx, g, rect, fSize);
                }
                else if (_data.HospitalDataAndLogo == true)
                {
                    DrawHeaderBlockWithLogoLeft(ctx, g, rect, fSize);
                }
            }
            else
            {

            }

            DrawPatientBlock(ctx, g, rect, fSize);
            DrawConclusionPageData(ctx, g, rect);


            DrawFooter(ctx, g, rect, fSize, 0, _totalPages);


        }


        private void DrawConclusionPageData(GdiContext ctx, Graphics g, Rectangle rect)
        {
            string font = _data.FontFamily;
            //Not Change this set 12F
            ctx.SetFont(font, 12f);
            Size fSize = ctx.MeasureText("50");

            int left = rect.Left;
            int width = rect.Width - fSize.Width * 2;
            //int y = rect.Top + fSize.Height * 14;
            int y = rect.Top + fSize.Height + 260;

            if (HasValue(_data.PatientHistory))
            {

                y = DrawGradientSectionHeader(
               ctx, rect, y, fSize, "Patient History");

                ctx.SetFont(font, 11f, FontStyle.Regular);
                Rectangle histRect = new Rectangle(left, y, width, fSize.Height * 0);
                //ctx.DrawTextBlock(_data.PatientHistory, histRect);
                //y = histRect.Bottom + fSize.Height * 2;
            }

            int headerWidth1 = fSize.Width * 36;

            // ================= Patient History Get Data  =================
            if (!string.IsNullOrWhiteSpace(_data.PatientHistory))
            {
                // Conclusion text
                ctx.SetFont(font, 10f, FontStyle.Regular);
                ctx.SetTextColor(Color.Black);

                Rectangle conclRect = new Rectangle(
                    left,
                    y,
                    headerWidth1 - 20,
                    fSize.Height * 4
                );

                y -= 10;

                //ctx.DrawTextBlock(_data.ResultConclusion, conclRect);
                string text = _data.PatientHistory ?? string.Empty;

                // Maximum width available
                int maxWidth = headerWidth1 - 150;

                // Break text into lines
                string[] words = text.Split(' ');
                string line = "";

                foreach (string word in words)
                {
                    string testLine = line.Length == 0 ? word : line + " " + word;
                    Size sz = ctx.MeasureText(testLine);

                    if (sz.Width > maxWidth)
                    {
                        ctx.TextOutLeft(left, y, line);
                        y += fSize.Height;
                        line = word;
                    }
                    else
                    {
                        line = testLine;
                    }
                }

                // Print remaining line
                if (!string.IsNullOrEmpty(line))
                {
                    ctx.TextOutLeft(left, y, line);
                    y += fSize.Height;
                }


                y = conclRect.Bottom + fSize.Height;
            }


            if (HasValue(_data.CatheterType) || HasValue(_data.InfusionRate) || HasValue(_data.TestPosition))
            {
                // Technique used
                //ctx.SetFont(font, 12f, FontStyle.Bold);
                //ctx.SetTextColor(Color.Red);
                //ctx.TextOutLeft(left, y, "TECHNIQUE USED");
                //ctx.SetTextColor(Color.Black);
                //y += fSize.Height * 2;

                y = DrawGradientSectionHeader(
           ctx, rect, y, fSize, "TECHNIQUE USED");

                y -= 10;

                ctx.SetFont(font, 11f, FontStyle.Regular);
                if (HasValue(_data.CatheterType) || HasValue(_data.InfusionRate))
                {
                    ctx.TextOutLeft(left, y,
                        $"Catheter Type used : {_data.CatheterType}  Infusion Rate (Normal Saline) : {_data.InfusionRate:F0} ml/min");
                    y += fSize.Height;
                }
                //string techLine1 =
                //    $"Catheter Type used : {_data.CatheterType} " +
                //    $"Infusion Rate (Normal Saline) : {_data.InfusionRate:F0} ml/min ";
                //ctx.TextOutLeft(left, y, techLine1);
                //y += fSize.Height;

                //ctx.TextOutLeft(left, y, $"Patient Position : {_data.TestPosition}");
                //y += fSize.Height * 2;
                if (HasValue(_data.TestPosition))
                {
                    ctx.TextOutLeft(left, y, $"Patient Position : {_data.TestPosition}");
                    y += fSize.Height;
                }

                //y += fSize.Height;
                y += 10;
            }

            bool hasNeuroData =
            HasValue(_data.Sensations) || HasValue(_data.AnalTone) || HasValue(_data.ClitoraBulboca) ||
            HasValue(_data.ValuntaryContraction) || HasValue(_data.FlowRate) || HasValue(_data.VoidedVolume) ||
            HasValue(_data.PostVoid) || HasValue(_data.BladderCapacity) || HasValue(_data.Proprioception) ||
            HasValue(_data.Complaince) || HasValue(_data.Detrusor) || HasValue(_data.PdetVoid) ||
            HasValue(_data.PDET) || HasValue(_data.PdetLeak) || HasValue(_data.VoidingPressure) ||
            HasValue(_data.Sphincter) || HasValue(_data.UrethralClosure) || HasValue(_data.CoughStress) ||
            HasValue(_data.ThereIsTwo) || HasValue(_data.ThereIsOne);

            if (hasNeuroData)
            {
                 y = DrawGradientSectionHeader(
               ctx, rect, y, fSize, "Regional Neurological Examination");

                y -= 10;

                // Sensation
                ctx.SetFont(font, 11f, FontStyle.Regular);
                if (HasValue(_data.Sensations))
                {
                    ctx.TextOutLeft(left, y, $"Sensation is {_data.Sensations}");
                    y += fSize.Height;
                }

                // Anal Tone
                if (HasValue(_data.AnalTone))
                {
                    ctx.TextOutLeft(left, y, $"Anal tone is {_data.AnalTone}");
                    y += fSize.Height;
                }

                // Clitoral / Bulbocavernosus Reflex
                if (HasValue(_data.ClitoraBulboca))
                {
                    ctx.TextOutLeft(left, y, $"Clitoral / Bulbocavernosus Reflex is {_data.ClitoraBulboca}");
                    y += fSize.Height;
                }

                if (HasValue(_data.ValuntaryContraction))
                {
                    ctx.TextOutLeft(left, y, $"Voluntary Contraction of the Sphincter is {_data.ValuntaryContraction}");
                    y += fSize.Height;
                    //y += fSize.Height;
                    y += 10;
                }



               

                if (_data.PrintImpression == true)
                {
                    // Impression / Main Results
                    //ctx.SetFont(font, 14f, FontStyle.Bold);
                    //ctx.SetTextColor(Color.Red);
                    //ctx.TextOutLeft(left, y, "IMPRESSION / MAIN RESULTS");
                    //ctx.SetTextColor(Color.Black);
                    //y += fSize.Height * 2;

                    y = DrawGradientSectionHeader(ctx, rect, y, fSize, "IMPRESSION / MAIN RESULTS");
                }
                else
                {

                }

                y -= 10;

                ctx.SetFont(font, 11f, FontStyle.Regular);
                if (HasValue(_data.FlowRate))
                {
                    ctx.TextOutLeft(left, y, $"Flow Rates are {_data.FlowRate}");
                    y += fSize.Height;
                }

                if (HasValue(_data.VoidedVolume))
                {
                    ctx.TextOutLeft(left, y, $"Voided Volume is {_data.VoidedVolume}");
                    y += fSize.Height;
                }

                if (HasValue(_data.PostVoid))
                {
                    ctx.TextOutLeft(left, y, $"Post Void Residual is {_data.PostVoid} ml");
                    y += fSize.Height;
                }

                if (HasValue(_data.BladderCapacity))
                {
                    ctx.TextOutLeft(left, y, $"Bladder Capacity is {_data.BladderCapacity}");
                    y += fSize.Height;
                }

                if (HasValue(_data.Proprioception))
                {
                    ctx.TextOutLeft(left, y, $"Proprioception is {_data.Proprioception}");
                    y += fSize.Height;
                }

                if (HasValue(_data.Complaince))
                {
                    ctx.TextOutLeft(left, y, $"Complaince is {_data.Complaince}");
                    y += fSize.Height;
                }

                if (HasValue(_data.Detrusor))
                {
                    ctx.TextOutLeft(left, y, $"Detrusor is {_data.Detrusor}");
                    y += fSize.Height;
                }

                if (HasValue(_data.PdetVoid))
                {

                    ctx.TextOutLeft(left, y, $"Pdet Void is {_data.PdetVoid}  cms H2O");
                    y += fSize.Height;
                }

                if (HasValue(_data.PDET))
                {
                    ctx.TextOutLeft(left, y, $"Pdet isometric is {_data.PDET}  cms H2O");
                    y += fSize.Height;
                }

                if (HasValue(_data.PdetLeak))
                {
                    ctx.TextOutLeft(left, y, $"Detrusor Leak Pressure is {_data.PdetLeak}   cms H2O at bladder volume       ml");
                    y += fSize.Height;
                }

                if (HasValue(_data.VoidingPressure))
                {
                    ctx.TextOutLeft(left, y, $"Voiding Pressure is {_data.VoidingPressure}");
                    y += fSize.Height;
                }

                if (HasValue(_data.Sphincter))
                {
                    ctx.TextOutLeft(left, y, $"Sphincter is {_data.Sphincter}");
                    y += fSize.Height;
                }

                if (HasValue(_data.UrethralClosure))
                {
                    ctx.TextOutLeft(left, y, $"Urethral Closure Mechanism is {_data.UrethralClosure}");
                    y += fSize.Height;
                }

                if (HasValue(_data.CoughStress))
                {
                    ctx.TextOutLeft(left, y, $"Cough Stress Test is {_data.CoughStress}");
                    y += fSize.Height;
                }

                // Stress Urinary Incontinence
                if (HasValue(_data.ThereIsTwo))
                {
                    ctx.TextOutLeft(left, y, $"There is {_data.ThereIsTwo} stress urinary incontinence demonstrated");
                    y += fSize.Height;
                }

                if (HasValue(_data.ThereIsOne))
                {
                    ctx.TextOutLeft(left, y, $"There is {_data.ThereIsOne} Bladder Outlet Obstruction");
                    y += fSize.Height;
                }
                //y += fSize.Height;
                y += 10;
            }


            if (HasValue(_data.ResultConclusion))
            {

                y = DrawGradientSectionHeader(
               ctx, rect, y, fSize, "Conclusions");

                ctx.SetFont(font, 11f, FontStyle.Regular);
                Rectangle conclRect = new Rectangle(left, y, width, fSize.Height * 4);
                //ctx.DrawTextBlock(_data.ResultConclusion, conclRect);
                //y = conclRect.Bottom + fSize.Height * 1;

            }

            int headerWidth = fSize.Width * 36;

            // ================= CONCLUSIONS Get Data =================
            if (!string.IsNullOrWhiteSpace(_data.ResultConclusion))
            {

                // Conclusion text
                ctx.SetFont(font, 10f, FontStyle.Regular);
                ctx.SetTextColor(Color.Black);

                Rectangle conclRect = new Rectangle(
                    left,
                    y,
                    headerWidth - 20,
                    fSize.Height * 4
                );

                y -= 10;

                //ctx.DrawTextBlock(_data.ResultConclusion, conclRect);
                string text = _data.ResultConclusion ?? string.Empty;

                // Maximum width available
                int maxWidth = headerWidth - 150;

                // Break text into lines
                string[] words = text.Split(' ');
                string line = "";

                foreach (string word in words)
                {
                    string testLine = line.Length == 0 ? word : line + " " + word;
                    Size sz = ctx.MeasureText(testLine);

                    if (sz.Width > maxWidth)
                    {
                        ctx.TextOutLeft(left, y, line);
                        y += fSize.Height;
                        line = word;
                    }
                    else
                    {
                        line = testLine;
                    }
                }

                // Print remaining line
                if (!string.IsNullOrEmpty(line))
                {
                    ctx.TextOutLeft(left, y, line);
                    y += fSize.Height;
                }


                y = conclRect.Bottom + fSize.Height;
            }



            // ================= Reported By =================
            if (!string.IsNullOrWhiteSpace(_data?.ReportBy))
            {
                ctx.SetFont(font, 11f, FontStyle.Bold);

                // 1️⃣ Draw label in RED
                ctx.SetTextColor(Color.Red);
                string label = "Reported By :- ";
                ctx.TextOutLeft(left, y, label);

                // 2️⃣ Measure label width
                SizeF labelSize = ctx.MeasureText(label);

                // 3️⃣ Draw value in BLACK (same line with spacing)
                ctx.SetTextColor(Color.Black);

                string reportedText =
                    $"      {_data.ReportBy}  ({_data.DoctorDegree})";

                ctx.TextOutLeft((int)(left + labelSize.Width), y, reportedText);


                y += fSize.Height * 3;

                // Signature text (BOLD)
                ctx.TextOutLeft(left, y, "Signature _____________");
                y += fSize.Height * 2;
            }
        }


        //private void DrawConclusionPage(GdiContext ctx, Graphics g, Rectangle rect)
        //{
        //    string font = _data.FontFamily;
        //    ctx.SetFont(font, 10f);
        //    Size fSize = ctx.MeasureText("50");

        //    // Header + patient info
        //    //DrawHeaderBlock(ctx, rect, fSize);

        //    if (_data.HeadFour == true)
        //    {
        //        if (_data.DefaultHeader == true)
        //        {
        //            DrawHeaderBlock(ctx, rect, fSize);
        //        }
        //        else if (_data.LetterHead == true)
        //        {
        //            DrawLatterHeadImage(ctx, g, rect, fSize);
        //        }
        //        else if (_data.HospitalLogo == true)
        //        {
        //            DrawHeaderLogoImage(ctx, g, rect, fSize);
        //        }
        //    }
        //    else
        //    {

        //    }

        //    DrawPatientBlock(ctx, g, rect, fSize);

        //    int left = rect.Left;
        //    int width = rect.Width - fSize.Width * 2;
        //    int y = rect.Top + fSize.Height * 14;

        //    if (HasValue(_data.PatientHistory))
        //    {
        //        // Patient History
        //        ctx.SetFont(font, 12f, FontStyle.Bold);
        //        ctx.SetTextColor(Color.Black);
        //        ctx.SetTextColor(Color.Red);
        //        ctx.TextOutLeft(left, y, "Patient History");
        //        ctx.SetTextColor(Color.Black);
        //        y += fSize.Height * 2;

        //        ctx.SetFont(font, 10f, FontStyle.Regular);
        //        Rectangle histRect = new Rectangle(left, y, width, fSize.Height * 4);
        //        ctx.DrawTextBlock(_data.PatientHistory, histRect);
        //        y = histRect.Bottom + fSize.Height * 2;
        //    }


        //    if (HasValue(_data.CatheterType) || HasValue(_data.InfusionRate)  || HasValue(_data.TestPosition))
        //    {
        //        // Technique used
        //        ctx.SetFont(font, 12f, FontStyle.Bold);
        //        ctx.SetTextColor(Color.Red);
        //        ctx.TextOutLeft(left, y, "TECHNIQUE USED");
        //        ctx.SetTextColor(Color.Black);
        //        y += fSize.Height * 2;

        //        ctx.SetFont(font, 10f, FontStyle.Regular);
        //        if (HasValue(_data.CatheterType) || HasValue(_data.InfusionRate))
        //        {
        //            ctx.TextOutLeft(left, y,
        //                $"Catheter Type used : {_data.CatheterType}  Infusion Rate (Normal Saline) : {_data.InfusionRate:F0} ml/min");
        //            y += fSize.Height;
        //        }
        //        //string techLine1 =
        //        //    $"Catheter Type used : {_data.CatheterType} " +
        //        //    $"Infusion Rate (Normal Saline) : {_data.InfusionRate:F0} ml/min ";
        //        //ctx.TextOutLeft(left, y, techLine1);
        //        //y += fSize.Height;

        //        //ctx.TextOutLeft(left, y, $"Patient Position : {_data.TestPosition}");
        //        //y += fSize.Height * 2;
        //        if (HasValue(_data.TestPosition))
        //        {
        //            ctx.TextOutLeft(left, y, $"Patient Position : {_data.TestPosition}");
        //            y += fSize.Height;
        //        }

        //        y += fSize.Height;
        //    }

        //    bool hasNeuroData =
        //    HasValue(_data.Sensations) || HasValue(_data.AnalTone) || HasValue(_data.ClitoraBulboca) ||
        //    HasValue(_data.ValuntaryContraction) || HasValue(_data.FlowRate) || HasValue(_data.VoidedVolume) ||
        //    HasValue(_data.PostVoid) || HasValue(_data.BladderCapacity) || HasValue(_data.Proprioception) ||
        //    HasValue(_data.Complaince) || HasValue(_data.Detrusor) || HasValue(_data.PdetVoid) ||
        //    HasValue(_data.PDET) || HasValue(_data.PdetLeak) || HasValue(_data.VoidingPressure) ||
        //    HasValue(_data.Sphincter) || HasValue(_data.UrethralClosure) || HasValue(_data.CoughStress) ||
        //    HasValue(_data.ThereIsTwo) || HasValue(_data.ThereIsOne);

        //    if (hasNeuroData)
        //    {
        //        if(_data.PrintImpression == true)
        //        {
        //            // Impression / Main Results
        //            ctx.SetFont(font, 14f, FontStyle.Bold);
        //            ctx.SetTextColor(Color.Red);
        //            ctx.TextOutLeft(left, y, "IMPRESSION / MAIN RESULTS");
        //            ctx.SetTextColor(Color.Black);
        //            y += fSize.Height * 2;
        //        }
        //        else
        //        {

        //        }


        //        // Regional Neurological Examination
        //        ctx.SetFont(font, 12f, FontStyle.Bold);
        //        ctx.SetTextColor(Color.Red);
        //        ctx.TextOutLeft(left, y, "Regional Neurological Examination");
        //        ctx.SetTextColor(Color.Black);
        //        y += fSize.Height * 2;

        //        // Sensation
        //        ctx.SetFont(font, 12f, FontStyle.Regular);
        //        if (HasValue(_data.Sensations))
        //        {
        //            ctx.TextOutLeft(left, y, $"Sensation is {_data.Sensations}");
        //            y += fSize.Height;
        //        }

        //        // Anal Tone
        //        if (HasValue(_data.AnalTone))
        //        {
        //            ctx.TextOutLeft(left, y, $"Anal tone is {_data.AnalTone}");
        //            y += fSize.Height;
        //        }

        //        // Clitoral / Bulbocavernosus Reflex
        //        if (HasValue(_data.ClitoraBulboca))
        //        {
        //            ctx.TextOutLeft(left, y, $"Clitoral / Bulbocavernosus Reflex is {_data.ClitoraBulboca}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.ValuntaryContraction))
        //        {
        //            ctx.TextOutLeft(left, y, $"Voluntary Contraction of the Sphincter is {_data.ValuntaryContraction}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.FlowRate))
        //        {
        //            ctx.TextOutLeft(left, y, $"Flow Rates are {_data.FlowRate}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.VoidedVolume))
        //        {
        //            ctx.TextOutLeft(left, y, $"Voided Volume is {_data.VoidedVolume}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.PostVoid))
        //        {
        //            ctx.TextOutLeft(left, y, $"Post Void Residual is {_data.PostVoid} ml");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.BladderCapacity))
        //        {
        //            ctx.TextOutLeft(left, y, $"Bladder Capacity is {_data.BladderCapacity}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.Proprioception))
        //        {
        //            ctx.TextOutLeft(left, y, $"Proprioception is {_data.Proprioception}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.Complaince))
        //        {
        //            ctx.TextOutLeft(left, y, $"Complaince is {_data.Complaince}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.Detrusor))
        //        {
        //            ctx.TextOutLeft(left, y, $"Detrusor is {_data.Detrusor}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.PdetVoid))
        //        {

        //            ctx.TextOutLeft(left, y, $"Pdet Void is {_data.PdetVoid}  cms H2O");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.PDET))
        //        {
        //            ctx.TextOutLeft(left, y, $"Pdet isometric is {_data.PDET}  cms H2O");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.PdetLeak))
        //        {
        //            ctx.TextOutLeft(left, y, $"Detrusor Leak Pressure is {_data.PdetLeak}   cms H2O at bladder volume       ml");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.VoidingPressure))
        //        {
        //            ctx.TextOutLeft(left, y, $"Voiding Pressure is {_data.VoidingPressure}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.Sphincter))
        //        {
        //            ctx.TextOutLeft(left, y, $"Sphincter is {_data.Sphincter}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.UrethralClosure))
        //        {
        //            ctx.TextOutLeft(left, y, $"Urethral Closure Mechanism is {_data.UrethralClosure}");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.CoughStress))
        //        {
        //            ctx.TextOutLeft(left, y, $"Cough Stress Test is {_data.CoughStress}");
        //            y += fSize.Height;
        //        }

        //        // Stress Urinary Incontinence
        //        if (HasValue(_data.ThereIsTwo))
        //        {
        //            ctx.TextOutLeft(left, y, $"There is {_data.ThereIsTwo} stress urinary incontinence demonstrated");
        //            y += fSize.Height;
        //        }

        //        if (HasValue(_data.ThereIsOne))
        //        {
        //            ctx.TextOutLeft(left, y, $"There is {_data.ThereIsOne} Bladder Outlet Obstruction");
        //            y += fSize.Height;
        //        }
        //        y += fSize.Height;
        //    }


        //    if (HasValue(_data.ResultConclusion))
        //    {
        //        // Conclusions
        //        ctx.SetFont(font, 12f, FontStyle.Bold);
        //        ctx.SetTextColor(Color.Red);
        //        ctx.TextOutLeft(left, y, "Conclusions : ");
        //        ctx.SetTextColor(Color.Black);
        //        y += fSize.Height * 2;

        //        ctx.SetFont(font, 10f, FontStyle.Regular);
        //        Rectangle conclRect = new Rectangle(left, y, width, fSize.Height * 4);
        //        ctx.DrawTextBlock(_data.ResultConclusion, conclRect);
        //        y += fSize.Height;
        //        y += fSize.Height;
        //        y += fSize.Height;
        //    }

        //    // Referred By
        //    ctx.SetFont(font, 12f, FontStyle.Bold);
        //    ctx.SetTextColor(Color.Red);
        //    ctx.TextOutLeft(left, y, "Referred By : ");
        //    ctx.SetTextColor(Color.Black);
        //    y += fSize.Height * 2;

        //    //ctx.SetFont(font, 10f, FontStyle.Regular);
        //    //Rectangle conclRect1 = new Rectangle(left, y, width, fSize.Height * 4);
        //    //ctx.DrawTextBlock(_data.ReportBy, conclRect1);

        //    // Doctor / Report By Name
        //    ctx.SetFont(font, 10f, FontStyle.Regular);
        //    ctx.TextOutLeft(left, y, _data.ReportBy);
        //    y += fSize.Height * 4;   // space after name

        //    // Signature line (BOLD)
        //    ctx.SetFont(font, 10f, FontStyle.Bold);
        //    ctx.TextOutLeft(left, y, "__________");
        //    y += fSize.Height;

        //    // Signature text (BOLD)
        //    ctx.TextOutLeft(left, y, "Signature");
        //    y += fSize.Height * 2;

        //    DrawFooter(ctx, g, rect, fSize);

        //    //if (_data.CompanyName && _data.LogoImage != null && _data.LogoImage.Length > 0)
        //    //{
        //    //    // --------------------------------------------------
        //    //    // FOOTER BASE LINE
        //    //    // --------------------------------------------------
        //    //    int footerTopY = rect.Bottom - fSize.Height + 90;

        //    //    // 1️⃣ Separator line (black)
        //    //    using (Pen linePen = new Pen(Color.Black, 1))
        //    //    {
        //    //        g.DrawLine(
        //    //            linePen,
        //    //            rect.Left,
        //    //            footerTopY,
        //    //            rect.Right,
        //    //            footerTopY
        //    //        );
        //    //    }

        //    //    // --------------------------------------------------
        //    //    // FOOTER CONTENT AREA
        //    //    // --------------------------------------------------
        //    //    int contentY = (int)(footerTopY + fSize.Height + 10);

        //    //    using (var ms = new MemoryStream(_data.LogoImage))
        //    //    using (Image logoImg = Image.FromStream(ms))
        //    //    {
        //    //        // 2️⃣ Logo size
        //    //        int logoHeight = fSize.Height * 2;
        //    //        int logoWidth = logoHeight * logoImg.Width / logoImg.Height;

        //    //        int logoX = rect.Left + fSize.Width - 20;   // LEFT aligned
        //    //        int logoY = contentY - logoHeight / 4;

        //    //        // Draw logo
        //    //        g.DrawImage(
        //    //            logoImg,
        //    //            new Rectangle(logoX, logoY, logoWidth, logoHeight)
        //    //        );

        //    //        // 3️⃣ Footer text (right of logo)
        //    //        ctx.SetFont(font, 12f);
        //    //        ctx.SetTextColor(Color.Blue);

        //    //        int textX = logoX + logoWidth + fSize.Width * 2;

        //    //        ctx.TextOutLeft(
        //    //            textX,
        //    //            contentY,
        //    //            "Santron Meditronic, India  |  www.santronmeditronic.com"
        //    //        );
        //    //    }

        //    //    ctx.SetTextColor(Color.Black);
        //    //}

        //}





        #endregion
    }




    #region Simple data model used by the printer

    public sealed class ReportDataPrint
    {
        public string FontFamily { get; set; } = "";

        // Header.
        public string DoctorName { get; set; } = "";
        public string DoctorDegree { get; set; } = "";
        public string DoctorPost { get; set; } = "";

        public string HospitalName { get; set; } = "";
        public string HospitalAddressLine1 { get; set; } = "";
        public string HospitalAddressLine2 { get; set; } = "";
        public string HospitalPoneNo { get; set; } = "";
        public string HospitalGmail { get; set; } = "";

        // Patient.
        public string TestName { get; set; } = "";
        public string PatientId { get; set; } = "";
        public DateTime TestDate { get; set; } = DateTime.Now;
        public string PatientName { get; set; } = "";
        public int Age { get; set; } = 0;
        public string Sex { get; set; } = "";
        public string Weight { get; set; } = "";
        public string PatientMobile { get; set; } = "";
        public string PatientAddress { get; set; } = "";
        public string TechnicianName { get; set; } = "";
        public string ReferredBy { get; set; } = "";
        public string Symptoms { get; set; } = "";

        // Graph & phases
        public Bitmap FillingGraphImage { get; set; }
        public FillingPhaseData Filling { get; set; } = new FillingPhaseData();
        public VoidingPhaseData Voiding { get; set; } = new VoidingPhaseData();

        // Markers page
        public List<MarkerRow> Markers { get; set; } = new List<MarkerRow>();

        // Page 4 – ReportComment Page Data
        public string PatientHistory { get; set; } = string.Empty;
        public string CatheterType { get; set; } = string.Empty;
        public string InfusionRate { get; set; } = string.Empty;
        public string TestPosition { get; set; } = string.Empty; //Patient Position
        public string Sensations { get; set; } = string.Empty;
        public string AnalTone { get; set; } = string.Empty;
        public string ClitoraBulboca { get; set; } = string.Empty;
        public string ValuntaryContraction { get; set; } = string.Empty;
        public string FlowRate { get; set; } = string.Empty;
        public string VoidedVolume { get; set; } = string.Empty;
        public string PostVoid { get; set; } = string.Empty;
        public string BladderCapacity { get; set; } = string.Empty;
        public string Proprioception { get; set; } = string.Empty;
        public string Complaince { get; set; } = string.Empty;
        public string Detrusor { get; set; } = string.Empty;
        public string PdetVoid { get; set; } = string.Empty;
        public string PDET { get; set; } = string.Empty;
        public string PdetLeak { get; set; } = string.Empty;
        public string VoidingPressure { get; set; } = string.Empty;
        public string Sphincter { get; set; } = string.Empty;
        public string UrethralClosure { get; set; } = string.Empty;
        public string CoughStress { get; set; } = string.Empty;
        public string ThereIsTwo { get; set; } = string.Empty;
        public string ThereIsOne { get; set; } = string.Empty;

        //Conclusion
        public string ResultConclusion { get; set; } = string.Empty;
        public string ReportBy { get; set; } = string.Empty;

        //For Images 
        public List<Image> CapturedImages { get; set; }

        //

        public string CatheterTypeUsed { get; set; } = "Foley No. 10";
        public double TechniqueInfusionRate { get; set; } = 2; // ml/min
        public string PatientPosition { get; set; } = "Erect";

        public string ImpressionText { get; set; } = string.Empty;
        public string NeurologicalText { get; set; } = string.Empty;
        public string Conclusions { get; set; } = string.Empty;

        public bool HeadOne { get; set; }
        public bool HeadTwo { get; set; }
        public bool HeadThree { get; set; }
        public bool HeadFour { get; set; }
        public bool HeadFive { get; set; }

        public bool Nomograms { get; set; }
        public bool PrintFlowNomograms { get; set; }
        public bool PrintImpression { get; set; }
        public bool CompanyName { get; set; }

        public bool DefaultHeader { get; set; }
        public bool LetterHead { get; set; }
        public bool HospitalLogo { get; set; }

        public bool HospitalDataAndLogo { get; set; }

        //Logo Image
        public byte[] LogoImage { get; set; }
        public byte[] LetterHeadImage { get; set; }

        // Marker colors from ScaleAndColorSetup
        public int BladderSensationColor { get; set; }
        public int ResponseMarkerColor { get; set; }
        public int GeneralPurposeColor { get; set; }
    }

    public sealed class FillingPhaseData
    {
        public double BladdedCapacity { get; set; }
        public double InfusionRate { get; set; }

        // ===== Added for C++-matching voiding COMPRU inputs =====
        public double InfusedVolume { get; set; }     // C++: InfusedVolume
        public double LeakVolume { get; set; }        // C++: leakvol
        public double PreTestResidual { get; set; }   // optional if you have it (else keep 0)

        public BladderPoint FirstSensation { get; set; } = new BladderPoint();
        public BladderPoint FirstDesire { get; set; } = new BladderPoint();
        public BladderPoint NormalDesire { get; set; } = new BladderPoint();
        public BladderPoint StrongDesire { get; set; } = new BladderPoint();
        public BladderPoint BladderCapacityPoint { get; set; } = new BladderPoint();
    }

    public sealed class VoidingPhaseData
    {
        public double VoidedVolume { get; set; }
        public double AverageFlowRate { get; set; }
        public double PeakFlowRate { get; set; }
        public double VoidingTime { get; set; }
        public double FlowTime { get; set; }
        public double TimeToPeakFlow { get; set; }


        public double PdetAtPeakflow { get; set; }
        public double PvesAtPeakflow { get; set; }
        public double PabdAtPeakflow { get; set; }

        public double OpeningPdet { get; set; }
        public double OpeningPves { get; set; }
        public double OpeningPabd { get; set; }

        public double MaxPdet { get; set; }
        public double MaxPves { get; set; }
        public double MaxPabd { get; set; }

        public double BOOI { get; set; }
        public double BCI { get; set; }
        public double ComputedPostVoidResidual { get; set; }
        public double DelayTime { get; set; }
        public double IntervalTime { get; set; }
    }

    public sealed class BladderPoint
    {
        public double Vinf { get; set; }
        public double Pves { get; set; }
        public double Pabd { get; set; }
        public double Pdet { get; set; }
        public double Qvol { get; set; }
        public double Qura { get; set; }
        public double EMG { get; set; }
    }

    public sealed class MarkerRow
    {
        public string Type { get; set; }
        public string TimeText { get; set; }   // e.g. "0:28"
        public double Pves { get; set; }
        public double Pabd { get; set; }
        public double Pdet { get; set; }
        public double Vinf { get; set; }
        public double Qvol { get; set; }
        public double Qura { get; set; }
        public double EMG { get; set; }

        public int Index;
    }

    #endregion
}