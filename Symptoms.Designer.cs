namespace SantronWinApp
{
    partial class Symptoms
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.CategoryDropdown = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.existButton = new System.Windows.Forms.Button();
            this.label31 = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.txtSymptom = new SantronWinApp.ElevatedTextBox();
            this.txtSymCode = new SantronWinApp.ElevatedTextBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel13 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label1.Location = new System.Drawing.Point(60, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 17);
            this.label1.TabIndex = 27;
            this.label1.Text = "Symptom";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label2.Location = new System.Drawing.Point(59, 145);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 17);
            this.label2.TabIndex = 29;
            this.label2.Text = "Symptom Code";
            this.label2.Click += new System.EventHandler(this.label2_Click_1);
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label9.Location = new System.Drawing.Point(58, 221);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 17);
            this.label9.TabIndex = 31;
            this.label9.Text = "Category";
            this.label9.Click += new System.EventHandler(this.label9_Click);
            // 
            // CategoryDropdown
            // 
            this.CategoryDropdown.BackColor = System.Drawing.Color.White;
            this.CategoryDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CategoryDropdown.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CategoryDropdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.CategoryDropdown.FormattingEnabled = true;
            this.CategoryDropdown.Items.AddRange(new object[] {
            "Remitting Symptoms",
            "Chronic Symptoms",
            "Relapsing Symptoms",
            "Prodromal Symptoms",
            "Delayed Symptoms"});
            this.CategoryDropdown.Location = new System.Drawing.Point(35, 243);
            this.CategoryDropdown.Name = "CategoryDropdown";
            this.CategoryDropdown.Size = new System.Drawing.Size(297, 25);
            this.CategoryDropdown.TabIndex = 32;
            this.CategoryDropdown.SelectedIndexChanged += new System.EventHandler(this.cmbSelectTest_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(69)))), ((int)(((byte)(157)))));
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.existButton);
            this.panel1.Controls.Add(this.label31);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(361, 48);
            this.panel1.TabIndex = 107;
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = global::SantronWinApp.Properties.Resources.Santron_Icon;
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel3.Location = new System.Drawing.Point(4, 6);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(50, 35);
            this.panel3.TabIndex = 5;
            // 
            // existButton
            // 
            this.existButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.existButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.existButton.FlatAppearance.BorderSize = 0;
            this.existButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.existButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.existButton.ForeColor = System.Drawing.Color.White;
            this.existButton.Location = new System.Drawing.Point(318, 8);
            this.existButton.Name = "existButton";
            this.existButton.Size = new System.Drawing.Size(37, 32);
            this.existButton.TabIndex = 1;
            this.existButton.Text = "X";
            this.existButton.UseVisualStyleBackColor = true;
            this.existButton.Click += new System.EventHandler(this.existButton_Click);
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label31.ForeColor = System.Drawing.Color.White;
            this.label31.Location = new System.Drawing.Point(59, 12);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(119, 21);
            this.label31.TabIndex = 0;
            this.label31.Text = "Add Symptom";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(183)))), ((int)(((byte)(73)))));
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(117, 19);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 35);
            this.btnOk.TabIndex = 48;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(213)))), ((int)(((byte)(219)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.btnCancel.Location = new System.Drawing.Point(232, 19);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 49;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(232)))), ((int)(((byte)(241)))));
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Controls.Add(this.btnOk);
            this.panel2.Location = new System.Drawing.Point(0, 290);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(361, 65);
            this.panel2.TabIndex = 108;
            // 
            // txtSymptom
            // 
            this.txtSymptom.AutoFocusOnFormShown = false;
            this.txtSymptom.BorderColor = System.Drawing.Color.Transparent;
            this.txtSymptom.BorderThickness = 0;
            this.txtSymptom.CornerRadius = 3;
            this.txtSymptom.FillColor = System.Drawing.Color.White;
            this.txtSymptom.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSymptom.ForeColor = System.Drawing.Color.Black;
            this.txtSymptom.Location = new System.Drawing.Point(32, 87);
            this.txtSymptom.Name = "txtSymptom";
            this.txtSymptom.PasswordChar = '\0';
            this.txtSymptom.ReadOnly = false;
            this.txtSymptom.SelectionLength = 0;
            this.txtSymptom.SelectionStart = 0;
            this.txtSymptom.ShadowColor = System.Drawing.Color.Black;
            this.txtSymptom.ShadowOpacity = 35;
            this.txtSymptom.ShadowSize = 5;
            this.txtSymptom.Size = new System.Drawing.Size(300, 44);
            this.txtSymptom.TabIndex = 141;
            this.txtSymptom.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtSymptom.UseSystemPasswordChar = false;
            this.txtSymptom.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSymptom_KeyDown);
            this.txtSymptom.Leave += new System.EventHandler(this.txtSymptom_Leave);
            // 
            // txtSymCode
            // 
            this.txtSymCode.AutoFocusOnFormShown = false;
            this.txtSymCode.BorderColor = System.Drawing.Color.Transparent;
            this.txtSymCode.BorderThickness = 0;
            this.txtSymCode.CornerRadius = 3;
            this.txtSymCode.FillColor = System.Drawing.Color.White;
            this.txtSymCode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSymCode.ForeColor = System.Drawing.Color.Black;
            this.txtSymCode.Location = new System.Drawing.Point(32, 165);
            this.txtSymCode.Name = "txtSymCode";
            this.txtSymCode.PasswordChar = '\0';
            this.txtSymCode.ReadOnly = false;
            this.txtSymCode.SelectionLength = 0;
            this.txtSymCode.SelectionStart = 0;
            this.txtSymCode.ShadowColor = System.Drawing.Color.Black;
            this.txtSymCode.ShadowOpacity = 35;
            this.txtSymCode.ShadowSize = 5;
            this.txtSymCode.Size = new System.Drawing.Size(300, 44);
            this.txtSymCode.TabIndex = 142;
            this.txtSymCode.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtSymCode.UseSystemPasswordChar = false;
            // 
            // panel5
            // 
            this.panel5.BackgroundImage = global::SantronWinApp.Properties.Resources.Category;
            this.panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel5.Location = new System.Drawing.Point(35, 221);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(20, 17);
            this.panel5.TabIndex = 145;
            // 
            // panel4
            // 
            this.panel4.BackgroundImage = global::SantronWinApp.Properties.Resources.SymptomsCode;
            this.panel4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel4.Location = new System.Drawing.Point(35, 145);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(20, 17);
            this.panel4.TabIndex = 144;
            // 
            // panel13
            // 
            this.panel13.BackgroundImage = global::SantronWinApp.Properties.Resources.virus_1;
            this.panel13.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel13.Location = new System.Drawing.Point(35, 66);
            this.panel13.Name = "panel13";
            this.panel13.Size = new System.Drawing.Size(20, 17);
            this.panel13.TabIndex = 143;
            // 
            // Symptoms
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(361, 356);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel13);
            this.Controls.Add(this.txtSymCode);
            this.Controls.Add(this.txtSymptom);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.CategoryDropdown);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Symptoms";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Notes";
            this.Load += new System.EventHandler(this.Symptoms_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox CategoryDropdown;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button existButton;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private ElevatedTextBox txtSymptom;
        private ElevatedTextBox txtSymCode;
        private System.Windows.Forms.Panel panel13;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
    }
}