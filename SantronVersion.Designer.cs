namespace SantronWinApp
{
    partial class SantronVersion
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SantronVersion));
            this.treeHelp = new System.Windows.Forms.TreeView();
            this.pdfViewer = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.panel3 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pdfViewer)).BeginInit();
            this.SuspendLayout();
            // 
            // treeHelp
            // 
            this.treeHelp.Location = new System.Drawing.Point(0, 1);
            this.treeHelp.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.treeHelp.Name = "treeHelp";
            this.treeHelp.Size = new System.Drawing.Size(226, 690);
            this.treeHelp.TabIndex = 64;
            this.treeHelp.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeHelp_AfterSelect);
            // 
            // pdfViewer
            // 
            this.pdfViewer.AllowExternalDrop = true;
            this.pdfViewer.CreationProperties = null;
            this.pdfViewer.DefaultBackgroundColor = System.Drawing.Color.White;
            this.pdfViewer.Location = new System.Drawing.Point(229, 8);
            this.pdfViewer.Margin = new System.Windows.Forms.Padding(2);
            this.pdfViewer.Name = "pdfViewer";
            this.pdfViewer.Size = new System.Drawing.Size(1091, 642);
            this.pdfViewer.TabIndex = 66;
            this.pdfViewer.ZoomFactor = 1D;
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.BackColor = System.Drawing.Color.Transparent;
            this.panel3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel3.BackgroundImage")));
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel3.Location = new System.Drawing.Point(235, 21);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1087, 619);
            this.panel3.TabIndex = 65;
            // 
            // SantronVersion
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1334, 661);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.pdfViewer);
            this.Controls.Add(this.treeHelp);
            this.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Name = "SantronVersion";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            this.Load += new System.EventHandler(this.SantronVersion_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pdfViewer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TreeView treeHelp;
        private Microsoft.Web.WebView2.WinForms.WebView2 pdfViewer;
        private System.Windows.Forms.Panel panel3;
    }
}