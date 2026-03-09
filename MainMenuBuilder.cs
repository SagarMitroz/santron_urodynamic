using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace SantronWinApp
{
    public class MainMenuBuilder
    {
        private readonly MainForm _mainForm;
        private MenuStrip _menuStrip;
        private readonly Action _showPatientsAction;
        private readonly Action _openPatientListAction;
        private readonly Action _printReportAction;
        private readonly Action _printPreviewAction;
        private readonly Action _togglePumpPanelAction;
        private readonly Func<float> _getDpiScaleFunc;
        private ToolStripLabel _deviceStatusPill;

        public MainMenuBuilder(
            MainForm mainForm,
            Action showPatientsAction,
            Action openPatientListAction,
            Action printReportAction,
            Action printPreviewAction,
            Action togglePumpPanelAction,
            Func<float> getDpiScaleFunc)
        {
            _mainForm = mainForm;
            _showPatientsAction = showPatientsAction;
            _openPatientListAction = openPatientListAction;
            _printReportAction = printReportAction;
            _printPreviewAction = printPreviewAction;
            _togglePumpPanelAction = togglePumpPanelAction;
            _getDpiScaleFunc = getDpiScaleFunc;
        }

        public MenuStrip CreateMenuStrip()
        {
            _menuStrip = new MenuStrip();

            float dpi = _getDpiScaleFunc();
            _menuStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            _menuStrip.CanOverflow = true;
            _menuStrip.Stretch = true;
            _menuStrip.ImageScalingSize = new Size((int)(16 * dpi), (int)(16 * dpi));
            _menuStrip.Padding = new Padding((int)(8 * dpi), (int)(2 * dpi), (int)(8 * dpi), (int)(2 * dpi));
            _menuStrip.AutoSize = false;
            _menuStrip.Height = (int)(52 * dpi);
            _menuStrip.MinimumSize = new Size(0, (int)(32 * dpi));

            _menuStrip.BackColor = Color.FromArgb(0, 73, 165);
            _menuStrip.ForeColor = Color.White;
            _menuStrip.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            _menuStrip.Dock = DockStyle.Top;
            _menuStrip.RenderMode = ToolStripRenderMode.Professional;
            _menuStrip.Renderer = new CustomMenuRenderer();

            int logoW = Math.Max(72, Math.Min(145, (int)(_mainForm.ClientSize.Width * 0.42)));
            int logoH = (int)(22 * dpi);
            int iconW = Math.Max(32, Math.Min(38, (int)(_mainForm.ClientSize.Width * 0.12)));
            int iconH = (int)(23 * dpi);

            AddLogo(logoW, logoH);
            _menuStrip.ShowItemToolTips = true;

            AddIconButtons(iconW, iconH);
            AddDeviceStatusPill();
            AddMenuItems();

            return _menuStrip;
        }

        private void AddLogo(int logoW, int logoH)
        {
            try
            {
                Image logoImage = Properties.Resources.SantronLogo;

                if (logoImage != null)
                {
                    float scale = Math.Min((float)logoW / logoImage.Width, (float)logoH / logoImage.Height);
                    int newW = (int)(logoImage.Width * scale);
                    int newH = (int)(logoImage.Height * scale);

                    Bitmap resizedLogo = new Bitmap(newW, newH);
                    using (Graphics g = Graphics.FromImage(resizedLogo))
                    {
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawImage(logoImage, 0, 0, newW, newH);
                    }

                    ToolStripLabel logoLabel = new ToolStripLabel
                    {
                        Image = resizedLogo,
                        ImageScaling = ToolStripItemImageScaling.None,
                        Margin = new Padding(10, 2, 20, 2),
                        Padding = new Padding(2, 1, 2, 0),
                        IsLink = true
                    };

                    logoLabel.Click += (s, e) => _showPatientsAction?.Invoke();
                    _menuStrip.Items.Add(logoLabel);
                }
                else
                {
                    AddFallbackLogo();
                }
            }
            catch
            {
                AddFallbackLogo();
            }
        }

        private void AddFallbackLogo()
        {
            ToolStripLabel logoText = new ToolStripLabel
            {
                Text = "Santron Meditronic",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Margin = new Padding(15, 5, 25, 5),
                IsLink = true
            };

            logoText.Click += (s, e) => _showPatientsAction?.Invoke();
            _menuStrip.Items.Add(logoText);
        }

        private void AddIconButtons(int iconW, int iconH)
        {
            // Patient Button
            ToolStripItem patientButton = CreateIconButton("\uE160",
                Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH),
                (s, e) => _showPatientsAction?.Invoke());
            patientButton.Name = "patientButton";
            patientButton.ToolTipText = "Add New Patients";
            _menuStrip.Items.Add(patientButton);

            // Menu Button
            ToolStripItem menuButton = CreateIconButton("\uE8B7",
                Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH),
                (s, e) => _openPatientListAction?.Invoke());
            menuButton.Name = "menuButton";
            menuButton.ToolTipText = "Open File";
            _menuStrip.Items.Add(menuButton);

            // Settings/Print Button
            ToolStripItem settingsButton = CreateIconButton("\uE749",
                Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH),
                (s, e) => _printReportAction?.Invoke());
            settingsButton.Name = "settingsButton";
            settingsButton.ToolTipText = "Print Button";
            _menuStrip.Items.Add(settingsButton);

            // Info/Print Preview Button
            ToolStripItem infoButton = CreateIconButton("\uE8A7",
                Color.FromArgb(0, 73, 165), Color.White, new Size(iconW, iconH),
                (s, e) => _printPreviewAction?.Invoke());
            infoButton.Name = "infoButton";
            infoButton.ToolTipText = "Print Preview";
            _menuStrip.Items.Add(infoButton);
        }

        private ToolStripButton CreateIconButton(string iconChar, Color backColor, Color foreColor,
            Size size, EventHandler onClick)
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

        private void AddDeviceStatusPill()
        {
            _deviceStatusPill = new ToolStripLabel
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(210, 30),
                Margin = new Padding(8, 4, 4, 4)
            };
            _deviceStatusPill.Paint += DeviceStatusPill_Paint;
            _menuStrip.Items.Add(_deviceStatusPill);
        }

        private void DeviceStatusPill_Paint(object sender, PaintEventArgs e)
        {
            var pill = sender as ToolStripLabel;
            if (pill == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Default to disconnected - MainForm will update this
            Color dotColor = Color.Red;
            Color backColor = Color.MistyRose;
            string text = "Device Disconnected";

            string dot = "●";
            SizeF dotSize = e.Graphics.MeasureString(dot, pill.Font);
            SizeF textSize = e.Graphics.MeasureString(text, pill.Font);

            int totalWidth = (int)(10 + dotSize.Width + 4 + textSize.Width + 10);
            int totalHeight = (int)Math.Max(dotSize.Height, textSize.Height) + 6;

            pill.AutoSize = false;
            pill.Width = totalWidth;
            pill.Height = totalHeight;

            Rectangle rect = new Rectangle(0, 0, pill.Width, pill.Height);

            using (var path = RoundedRect(rect, 8))
            {
                using (var bg = new SolidBrush(backColor))
                    e.Graphics.FillPath(bg, path);

                using (var pen = new Pen(Color.LightGray))
                    e.Graphics.DrawPath(pen, path);
            }

            float y = (rect.Height - dotSize.Height) / 2f;

            using (var b = new SolidBrush(dotColor))
                e.Graphics.DrawString(dot, pill.Font, b, new PointF(10, y));

            using (var b2 = new SolidBrush(Color.Black))
                e.Graphics.DrawString(text, pill.Font, b2, new PointF(10 + dotSize.Width + 4, y));
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
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

        private void AddMenuItems()
        {
            string[] menuItems = { "File", "Setup", "Help", "Pump-Arm" };

            for (int i = 0; i < menuItems.Length; i++)
            {
                ToolStripMenuItem menuItem = CreateMenuItem(menuItems[i]);
                menuItem.Margin = new Padding(10, 2, 10, 2);
                menuItem.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                menuItem.Name = menuItems[i] + "Menu";

                switch (menuItems[i])
                {
                    case "File":
                        AddFileMenuItems(menuItem);
                        break;
                    case "Setup":
                        AddSetupMenuItems(menuItem);
                        break;
                    case "Help":
                        AddHelpMenuItems(menuItem);
                        break;
                    case "Pump-Arm":
                        menuItem.Name = "PumpArmMenu";
                        menuItem.Click += (s, e) => _togglePumpPanelAction?.Invoke();
                        break;
                }

                _menuStrip.Items.Add(menuItem);
            }
        }

        private ToolStripMenuItem CreateMenuItem(string text)
        {
            return new ToolStripMenuItem(text)
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
        }

        private Bitmap CreateBullet(Color color)
        {
            int size = 12;
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int d = 6;
                g.FillEllipse(new SolidBrush(color), (size - d) / 2, (size - d) / 2, d, d);
            }
            return bmp;
        }

        private void AddFileMenuItems(ToolStripMenuItem fileMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("New") { Image = bullet };
            newMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newMenuItem.Click += (s, args) =>
            {
                var patientWithTestForm = new PatientWithTestForm();
                ScreenDimOverlay.ShowDialogWithDim(patientWithTestForm, alpha: 150);
            };

            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open..") { Image = bullet };
            openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openMenuItem.Click += (s, args) => _openPatientListAction?.Invoke();

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

        private void AddSetupMenuItems(ToolStripMenuItem setupMenu)
        {
            Image bullet = CreateBullet(Color.Black);

            // System Menu Item
            ToolStripMenuItem systemMenuItem = new ToolStripMenuItem("System") { Image = bullet };
            systemMenuItem.Click += (s, args) =>
            {
                var passwordForm = new PasswordForm();
                passwordForm.SystemSetupSaved += (sender, e) =>
                {
                    _mainForm.BeginInvoke(new Action(() =>
                    {
                        _mainForm.ReloadSystemSetupAndConstants();
                    }));
                };
                ScreenDimOverlay.ShowDialogWithDim(passwordForm, alpha: 150);
            };

            // Hospital/Doctor Info
            ToolStripMenuItem hospitalMenuItem = new ToolStripMenuItem("Hospital / Doctor Information") { Image = bullet };
            hospitalMenuItem.Click += (s, args) =>
            {
                var systemForm = new HospitalAndDoctorInfoSetUp();
                ScreenDimOverlay.ShowDialogWithDim(systemForm, alpha: 150);
            };

            // Scale and Color Setup
            ToolStripMenuItem pressureMenuItem = new ToolStripMenuItem("Scale And Color Setup") { Image = bullet };
            pressureMenuItem.Click += (s, args) =>
            {
                var pressureStudyForm = new ScaleAndColorSteup();
                pressureStudyForm.DataSaved += (sender, e) =>
                {
                    _mainForm.BeginInvoke(new Action(() =>
                    {
                        _mainForm.ReloadScaleAndColorSetup();
                    }));
                };
                ScreenDimOverlay.ShowDialogWithDim(pressureStudyForm, alpha: 150);
            };

            // Patient Database
            ToolStripMenuItem patientHistoryMenuItem = new ToolStripMenuItem("Patient Database") { Image = bullet };
            patientHistoryMenuItem.Click += (s, args) =>
            {
                var patientHostoryForm = new PatientHistory();
                ScreenDimOverlay.ShowDialogWithDim(patientHostoryForm, alpha: 150);
            };

            // Doctors
            ToolStripMenuItem docterMenuItem = new ToolStripMenuItem("Doctors") { Image = bullet };
            docterMenuItem.Click += (s, args) =>
            {
                DocterList dicterForm = new DocterList();
                dicterForm.Show();
            };

            // Symptoms
            ToolStripMenuItem symptomsMenuItem = new ToolStripMenuItem("Symptoms") { Image = bullet };
            symptomsMenuItem.Click += (s, args) =>
            {
                SymptomsList symptomsForm = new SymptomsList();
                symptomsForm.Show();
            };

            // Language
            ToolStripMenuItem LangaugeMenuItem = new ToolStripMenuItem("Language") { Image = bullet };
            LangaugeMenuItem.Click += (s, args) =>
            {
                var languageForm = new LanguageForm();
                ScreenDimOverlay.ShowDialogWithDim(languageForm, alpha: 150);
            };

            // Video Device
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

        private void PrintSetupMenuItem_Click(object sender, EventArgs e)
        {
            using (var pageSetup = new PageSetupDialog())
            {
                pageSetup.PageSettings = new PageSettings();
                pageSetup.PrinterSettings = new PrinterSettings();

                if (pageSetup.ShowDialog() == DialogResult.OK)
                {
                    // Handle page setup changes
                }
            }
        }

        public ToolStripLabel GetDeviceStatusPill() => _deviceStatusPill;

        // Custom renderer class
        public class CustomMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                bool isInDropDown = e.ToolStrip is ToolStripDropDownMenu;

                if (e.Item.Selected)
                {
                    if (isInDropDown)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, 150, 255)), rect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
                else
                {
                    if (!isInDropDown)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), rect);
                    }
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is MenuStrip)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 73, 165)), e.AffectedBounds);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.White, e.AffectedBounds);
                }
            }
        }
    }
}