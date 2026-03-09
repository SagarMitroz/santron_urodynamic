using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
 
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SantronWinApp
{
    public partial class SantronVersion : Form
    {

        public enum HelpItemType { Pdf, Video, About }

        public sealed class HelpItem
        {
            public HelpItemType Type { get; set; }
            public string RelativePath { get; set; }  // e.g. @"HelpDocs\UserManual.pdf"
            public string Title { get; set; }
        }
        public SantronVersion()
        {
            InitializeComponent();
            this.MaximizeBox = false;                
            this.FormBorderStyle = FormBorderStyle.FixedSingle;   
        }

        private void SantronVersion_Load(object sender, EventArgs e)
        {
            LoadHelpTree();
            treeHelp.AfterSelect -= treeHelp_AfterSelect;   // avoid double subscribe
            treeHelp.AfterSelect += treeHelp_AfterSelect;

            ShowAboutPanel();

        }

        private void LoadHelpTree()
        {
            pdfViewer.Visible = true;
            panel3.Visible = false;
            treeHelp.Nodes.Clear();

            var root = new TreeNode("Help");

            root.Nodes.Add(new TreeNode("About Info")
            {
                Tag = new HelpItem { Type = HelpItemType.About, RelativePath = null }
            });

            // PDFs
            root.Nodes.Add(new TreeNode("User Manual")
            {
                Tag = new HelpItem { Type = HelpItemType.Pdf, RelativePath = @"HelpDocs\UserManual.pdf" }
            });

            root.Nodes.Add(new TreeNode("Troubleshooting")
            {
                Tag = new HelpItem { Type = HelpItemType.Pdf, RelativePath = @"HelpDocs\Troubleshooting.pdf" }
            });

            // Videos
            var vids = new TreeNode("Videos");
            vids.Nodes.Add(new TreeNode("How to Start Test")
            {
                Tag = new HelpItem { Type = HelpItemType.Video, RelativePath = @"HelpDocs\Videos\StartTest.mp4" }
            });
            vids.Nodes.Add(new TreeNode("How to Capture Image")
            {
                Tag = new HelpItem { Type = HelpItemType.Video, RelativePath = @"HelpDocs\Videos\CapturePhoto.mp4" }
            });

            root.Nodes.Add(vids);
            treeHelp.Nodes.Add(root);
            root.Expand();
            vids.Expand();
        }

        private bool _helpHostMapped = false;

        private async Task EnsureHelpHostMappingAsync()
        {
            await pdfViewer.EnsureCoreWebView2Async();

            if (_helpHostMapped) return;

          //  string helpRoot = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "HelpDocs");
            string helpRoot = AppPathManager.GetFolderPath("HelpDocs");
            pdfViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "help",
                helpRoot,
                CoreWebView2HostResourceAccessKind.Allow
            );

            _helpHostMapped = true;
        }

        private string BuildVideoHtmlFromUrl(string videoUrl)
        {
            return @"
                    <html>
                    <body style='margin:0;background:black;'>
                    <video controls style='width:100%;height:100%;' preload='auto'>
                      <source src='" + videoUrl + @"' type='video/mp4'>
                    </video>
                    </body>
                    </html>";
        }


        private async void treeHelp_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e == null || e.Node == null || e.Node.Tag == null)
                    return;

                HelpItem item = e.Node.Tag as HelpItem;
                if (item == null)
                    return;

                // ABOUT
                if (item.Type == HelpItemType.About)
                {
                    ShowAboutPanel();
                    return;
                }

                // PDF / VIDEO uses viewer
                ShowPdfViewer();

               // string absPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, item.RelativePath);
                string absPath = AppPathManager.GetFolderPath(item.RelativePath);
                if (!System.IO.File.Exists(absPath))
                {
                    MessageBox.Show("Help item not found:\n" + absPath);
                    return;
                }

                await pdfViewer.EnsureCoreWebView2Async();

                if (item.Type == HelpItemType.Pdf)
                {
                    pdfViewer.CoreWebView2.Navigate(new Uri(absPath).AbsoluteUri);
                }
                else if (item.Type == HelpItemType.Video)
                {
                    await EnsureHelpHostMappingAsync();

                    // item.RelativePath should be like @"HelpDocs\Videos\StartTest.mp4"
                    // Convert to path inside HelpDocs:
                    string relInsideHelpDocs = item.RelativePath
                        .Replace(@"HelpDocs\", "")
                        .Replace("\\", "/");

                    string videoUrl = "https://help/" + relInsideHelpDocs;

                    pdfViewer.CoreWebView2.Navigate(videoUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Help load error:\n" + ex.Message);
            }
        }


        private void label5_Click(object sender, EventArgs e)
        {

        }


        private void ShowAboutPanel()
        {
            panel3.Visible = true;
            pdfViewer.Visible = false;
        }

        private void ShowPdfViewer()
        {
            panel3.Visible = false;
            pdfViewer.Visible = true;
        }


        private void label23_Click(object sender, EventArgs e)
        {
            
        }

        private void label3_Click(object sender, EventArgs e)
        {
             
        }

        private void label4_Click(object sender, EventArgs e)
        {
             
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }
    }
}
