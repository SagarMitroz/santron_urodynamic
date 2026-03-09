namespace SantronWinApp
{
    partial class DocterEditForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DocterEditForm));
            this.existButton = new System.Windows.Forms.Button();
            this.label31 = new System.Windows.Forms.Label();
            this.cmbSelectTest = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Edit_Button = new System.Windows.Forms.Button();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.panel7 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel11 = new System.Windows.Forms.Panel();
            this.panel10 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel15 = new System.Windows.Forms.Panel();
            this.panel13 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtDepartment = new SantronWinApp.ElevatedTextBox();
            this.txtEmail = new SantronWinApp.ElevatedTextBox();
            this.txtMobileNo = new SantronWinApp.ElevatedTextBox();
            this.txtQualification = new SantronWinApp.ElevatedTextBox();
            this.txtSpecialization = new SantronWinApp.ElevatedTextBox();
            this.txtDocterName = new SantronWinApp.ElevatedTextBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // existButton
            // 
            this.existButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.existButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.existButton.FlatAppearance.BorderSize = 0;
            this.existButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.existButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.existButton.ForeColor = System.Drawing.Color.White;
            this.existButton.Location = new System.Drawing.Point(733, 8);
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
            this.label31.Location = new System.Drawing.Point(59, 13);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(139, 21);
            this.label31.TabIndex = 0;
            this.label31.Text = "Edit Doctor Form";
            // 
            // cmbSelectTest
            // 
            this.cmbSelectTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbSelectTest.BackColor = System.Drawing.Color.WhiteSmoke;
            this.cmbSelectTest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelectTest.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSelectTest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.cmbSelectTest.FormattingEnabled = true;
            this.cmbSelectTest.Items.AddRange(new object[] {
            "Indoor",
            "Outdoor"});
            this.cmbSelectTest.Location = new System.Drawing.Point(413, 333);
            this.cmbSelectTest.Name = "cmbSelectTest";
            this.cmbSelectTest.Size = new System.Drawing.Size(299, 29);
            this.cmbSelectTest.TabIndex = 129;
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
            this.panel1.Size = new System.Drawing.Size(775, 48);
            this.panel1.TabIndex = 126;
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel3.BackgroundImage")));
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel3.Location = new System.Drawing.Point(5, 5);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(50, 35);
            this.panel3.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(213)))), ((int)(((byte)(219)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.btnCancel.Location = new System.Drawing.Point(653, 15);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 73;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(232)))), ((int)(((byte)(241)))));
            this.panel2.Controls.Add(this.Edit_Button);
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Location = new System.Drawing.Point(0, 405);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(773, 65);
            this.panel2.TabIndex = 127;
            // 
            // Edit_Button
            // 
            this.Edit_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Edit_Button.BackColor = System.Drawing.Color.Tomato;
            this.Edit_Button.FlatAppearance.BorderSize = 0;
            this.Edit_Button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Edit_Button.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.Edit_Button.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.Edit_Button.Location = new System.Drawing.Point(533, 15);
            this.Edit_Button.Margin = new System.Windows.Forms.Padding(2);
            this.Edit_Button.Name = "Edit_Button";
            this.Edit_Button.Size = new System.Drawing.Size(100, 35);
            this.Edit_Button.TabIndex = 74;
            this.Edit_Button.Text = "Edit";
            this.Edit_Button.UseVisualStyleBackColor = false;
            this.Edit_Button.Click += new System.EventHandler(this.Edit_Button_Click);
            // 
            // radioButton1
            // 
            this.radioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton1.AutoSize = true;
            this.radioButton1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton1.Location = new System.Drawing.Point(423, 170);
            this.radioButton1.Margin = new System.Windows.Forms.Padding(2);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(56, 21);
            this.radioButton1.TabIndex = 149;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Male";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            this.radioButton3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton3.AutoSize = true;
            this.radioButton3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.radioButton3.Location = new System.Drawing.Point(556, 171);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(61, 21);
            this.radioButton3.TabIndex = 161;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "Other";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton2.Location = new System.Drawing.Point(481, 170);
            this.radioButton2.Margin = new System.Windows.Forms.Padding(2);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(70, 21);
            this.radioButton2.TabIndex = 160;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Female";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // panel7
            // 
            this.panel7.BackgroundImage = global::SantronWinApp.Properties.Resources.Department;
            this.panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel7.Location = new System.Drawing.Point(56, 309);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(20, 17);
            this.panel7.TabIndex = 170;
            // 
            // panel6
            // 
            this.panel6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel6.BackgroundImage = global::SantronWinApp.Properties.Resources.Typess;
            this.panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel6.Location = new System.Drawing.Point(418, 309);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(20, 17);
            this.panel6.TabIndex = 177;
            // 
            // panel11
            // 
            this.panel11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel11.BackgroundImage = global::SantronWinApp.Properties.Resources.Email;
            this.panel11.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel11.Location = new System.Drawing.Point(417, 223);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(20, 17);
            this.panel11.TabIndex = 176;
            // 
            // panel10
            // 
            this.panel10.BackgroundImage = global::SantronWinApp.Properties.Resources.phone;
            this.panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel10.Location = new System.Drawing.Point(56, 223);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(20, 17);
            this.panel10.TabIndex = 175;
            // 
            // panel5
            // 
            this.panel5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel5.BackgroundImage = global::SantronWinApp.Properties.Resources.gender;
            this.panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel5.Location = new System.Drawing.Point(418, 145);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(20, 17);
            this.panel5.TabIndex = 174;
            // 
            // panel4
            // 
            this.panel4.BackgroundImage = global::SantronWinApp.Properties.Resources.Qualification;
            this.panel4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel4.Location = new System.Drawing.Point(56, 145);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(20, 17);
            this.panel4.TabIndex = 173;
            // 
            // panel15
            // 
            this.panel15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel15.BackgroundImage = global::SantronWinApp.Properties.Resources.Specialization1;
            this.panel15.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel15.Location = new System.Drawing.Point(417, 70);
            this.panel15.Name = "panel15";
            this.panel15.Size = new System.Drawing.Size(20, 17);
            this.panel15.TabIndex = 172;
            // 
            // panel13
            // 
            this.panel13.BackgroundImage = global::SantronWinApp.Properties.Resources.DocterName;
            this.panel13.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel13.Location = new System.Drawing.Point(56, 70);
            this.panel13.Name = "panel13";
            this.panel13.Size = new System.Drawing.Size(20, 17);
            this.panel13.TabIndex = 171;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label3.Location = new System.Drawing.Point(442, 309);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 17);
            this.label3.TabIndex = 169;
            this.label3.Text = "Type";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label7.Location = new System.Drawing.Point(80, 309);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(82, 17);
            this.label7.TabIndex = 168;
            this.label7.Text = "Department";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label6.Location = new System.Drawing.Point(442, 222);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 17);
            this.label6.TabIndex = 167;
            this.label6.Text = "Email";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label5.Location = new System.Drawing.Point(80, 223);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(105, 17);
            this.label5.TabIndex = 166;
            this.label5.Text = "Mobile Number";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label4.Location = new System.Drawing.Point(443, 145);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 17);
            this.label4.TabIndex = 165;
            this.label4.Text = "Gender";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label9.Location = new System.Drawing.Point(80, 145);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(88, 17);
            this.label9.TabIndex = 164;
            this.label9.Text = "Qualification";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label2.Location = new System.Drawing.Point(442, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(182, 17);
            this.label2.TabIndex = 163;
            this.label2.Text = "Specialization / Designation";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label1.Location = new System.Drawing.Point(80, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 17);
            this.label1.TabIndex = 162;
            this.label1.Text = "Doctor Name";
            // 
            // txtDepartment
            // 
            this.txtDepartment.AutoFocusOnFormShown = false;
            this.txtDepartment.BorderColor = System.Drawing.Color.Transparent;
            this.txtDepartment.BorderThickness = 0;
            this.txtDepartment.CornerRadius = 3;
            this.txtDepartment.FillColor = System.Drawing.Color.White;
            this.txtDepartment.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtDepartment.ForeColor = System.Drawing.Color.Black;
            this.txtDepartment.Location = new System.Drawing.Point(51, 327);
            this.txtDepartment.Name = "txtDepartment";
            this.txtDepartment.PasswordChar = '\0';
            this.txtDepartment.ReadOnly = false;
            this.txtDepartment.SelectionLength = 0;
            this.txtDepartment.SelectionStart = 0;
            this.txtDepartment.ShadowColor = System.Drawing.Color.Black;
            this.txtDepartment.ShadowOpacity = 35;
            this.txtDepartment.ShadowSize = 5;
            this.txtDepartment.Size = new System.Drawing.Size(300, 44);
            this.txtDepartment.TabIndex = 159;
            this.txtDepartment.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtDepartment.UseSystemPasswordChar = false;
            // 
            // txtEmail
            // 
            this.txtEmail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEmail.AutoFocusOnFormShown = false;
            this.txtEmail.BorderColor = System.Drawing.Color.Transparent;
            this.txtEmail.BorderThickness = 0;
            this.txtEmail.CornerRadius = 3;
            this.txtEmail.FillColor = System.Drawing.Color.White;
            this.txtEmail.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEmail.ForeColor = System.Drawing.Color.Black;
            this.txtEmail.Location = new System.Drawing.Point(412, 241);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.PasswordChar = '\0';
            this.txtEmail.ReadOnly = false;
            this.txtEmail.SelectionLength = 0;
            this.txtEmail.SelectionStart = 0;
            this.txtEmail.ShadowColor = System.Drawing.Color.Black;
            this.txtEmail.ShadowOpacity = 35;
            this.txtEmail.ShadowSize = 5;
            this.txtEmail.Size = new System.Drawing.Size(300, 44);
            this.txtEmail.TabIndex = 158;
            this.txtEmail.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtEmail.UseSystemPasswordChar = false;
            // 
            // txtMobileNo
            // 
            this.txtMobileNo.AutoFocusOnFormShown = false;
            this.txtMobileNo.BorderColor = System.Drawing.Color.Transparent;
            this.txtMobileNo.BorderThickness = 0;
            this.txtMobileNo.CornerRadius = 3;
            this.txtMobileNo.FillColor = System.Drawing.Color.White;
            this.txtMobileNo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtMobileNo.ForeColor = System.Drawing.Color.Black;
            this.txtMobileNo.Location = new System.Drawing.Point(51, 241);
            this.txtMobileNo.Name = "txtMobileNo";
            this.txtMobileNo.PasswordChar = '\0';
            this.txtMobileNo.ReadOnly = false;
            this.txtMobileNo.SelectionLength = 0;
            this.txtMobileNo.SelectionStart = 0;
            this.txtMobileNo.ShadowColor = System.Drawing.Color.Black;
            this.txtMobileNo.ShadowOpacity = 35;
            this.txtMobileNo.ShadowSize = 5;
            this.txtMobileNo.Size = new System.Drawing.Size(300, 44);
            this.txtMobileNo.TabIndex = 157;
            this.txtMobileNo.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtMobileNo.UseSystemPasswordChar = false;
            this.txtMobileNo.TextChanged += new System.EventHandler(this.txtMobileNo_TextChanged);
            // 
            // txtQualification
            // 
            this.txtQualification.AutoFocusOnFormShown = false;
            this.txtQualification.BorderColor = System.Drawing.Color.Transparent;
            this.txtQualification.BorderThickness = 0;
            this.txtQualification.CornerRadius = 3;
            this.txtQualification.FillColor = System.Drawing.Color.White;
            this.txtQualification.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtQualification.ForeColor = System.Drawing.Color.Black;
            this.txtQualification.Location = new System.Drawing.Point(51, 163);
            this.txtQualification.Name = "txtQualification";
            this.txtQualification.PasswordChar = '\0';
            this.txtQualification.ReadOnly = false;
            this.txtQualification.SelectionLength = 0;
            this.txtQualification.SelectionStart = 0;
            this.txtQualification.ShadowColor = System.Drawing.Color.Black;
            this.txtQualification.ShadowOpacity = 35;
            this.txtQualification.ShadowSize = 5;
            this.txtQualification.Size = new System.Drawing.Size(300, 44);
            this.txtQualification.TabIndex = 156;
            this.txtQualification.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtQualification.UseSystemPasswordChar = false;
            // 
            // txtSpecialization
            // 
            this.txtSpecialization.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSpecialization.AutoFocusOnFormShown = false;
            this.txtSpecialization.BorderColor = System.Drawing.Color.Transparent;
            this.txtSpecialization.BorderThickness = 0;
            this.txtSpecialization.CornerRadius = 3;
            this.txtSpecialization.FillColor = System.Drawing.Color.White;
            this.txtSpecialization.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSpecialization.ForeColor = System.Drawing.Color.Black;
            this.txtSpecialization.Location = new System.Drawing.Point(412, 88);
            this.txtSpecialization.Name = "txtSpecialization";
            this.txtSpecialization.PasswordChar = '\0';
            this.txtSpecialization.ReadOnly = false;
            this.txtSpecialization.SelectionLength = 0;
            this.txtSpecialization.SelectionStart = 0;
            this.txtSpecialization.ShadowColor = System.Drawing.Color.Black;
            this.txtSpecialization.ShadowOpacity = 35;
            this.txtSpecialization.ShadowSize = 5;
            this.txtSpecialization.Size = new System.Drawing.Size(300, 44);
            this.txtSpecialization.TabIndex = 155;
            this.txtSpecialization.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtSpecialization.UseSystemPasswordChar = false;
            // 
            // txtDocterName
            // 
            this.txtDocterName.AutoFocusOnFormShown = false;
            this.txtDocterName.BorderColor = System.Drawing.Color.Transparent;
            this.txtDocterName.BorderThickness = 0;
            this.txtDocterName.CornerRadius = 3;
            this.txtDocterName.FillColor = System.Drawing.Color.White;
            this.txtDocterName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtDocterName.ForeColor = System.Drawing.Color.Black;
            this.txtDocterName.Location = new System.Drawing.Point(51, 88);
            this.txtDocterName.Name = "txtDocterName";
            this.txtDocterName.PasswordChar = '\0';
            this.txtDocterName.ReadOnly = false;
            this.txtDocterName.SelectionLength = 0;
            this.txtDocterName.SelectionStart = 0;
            this.txtDocterName.ShadowColor = System.Drawing.Color.Black;
            this.txtDocterName.ShadowOpacity = 35;
            this.txtDocterName.ShadowSize = 5;
            this.txtDocterName.Size = new System.Drawing.Size(300, 44);
            this.txtDocterName.TabIndex = 154;
            this.txtDocterName.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtDocterName.UseSystemPasswordChar = false;
            // 
            // DocterEditForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(774, 470);
            this.Controls.Add(this.panel7);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel11);
            this.Controls.Add(this.panel10);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel15);
            this.Controls.Add(this.panel13);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.radioButton3);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.txtDepartment);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.txtMobileNo);
            this.Controls.Add(this.txtQualification);
            this.Controls.Add(this.txtSpecialization);
            this.Controls.Add(this.txtDocterName);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.cmbSelectTest);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DocterEditForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DocterEditForm";
            this.Load += new System.EventHandler(this.DocterEditForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button existButton;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.ComboBox cmbSelectTest;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button Edit_Button;
        private System.Windows.Forms.Panel panel3;
        private ElevatedTextBox txtDepartment;
        private ElevatedTextBox txtEmail;
        private ElevatedTextBox txtMobileNo;
        private ElevatedTextBox txtSpecialization;
        private ElevatedTextBox txtDocterName;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel11;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel15;
        private System.Windows.Forms.Panel panel13;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private ElevatedTextBox txtQualification;
    }
}