using System.Windows.Forms;

namespace SantronWinApp
{
    partial class Patient_Information
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
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.label10 = new System.Windows.Forms.Label();
            this.dtpTime = new System.Windows.Forms.DateTimePicker();
            this.txtReferredBy = new System.Windows.Forms.TextBox();
            this.txtSymptoms = new System.Windows.Forms.TextBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.cmbSelectTest = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel14 = new System.Windows.Forms.Panel();
            this.existButton = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Edit_Button = new System.Windows.Forms.Button();
            this.label36 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.panel13 = new System.Windows.Forms.Panel();
            this.panel12 = new System.Windows.Forms.Panel();
            this.panel11 = new System.Windows.Forms.Panel();
            this.panel10 = new System.Windows.Forms.Panel();
            this.panel9 = new System.Windows.Forms.Panel();
            this.panel8 = new System.Windows.Forms.Panel();
            this.panel7 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txtMobileNo = new SantronWinApp.ElevatedTextBox();
            this.txtAge = new SantronWinApp.ElevatedTextBox();
            this.txtAddress = new SantronWinApp.ElevatedTextBox();
            this.txtWeight = new SantronWinApp.ElevatedTextBox();
            this.txtPatientName = new SantronWinApp.ElevatedTextBox();
            this.txtPatientIdN = new SantronWinApp.ElevatedTextBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label1.Location = new System.Drawing.Point(63, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Patient ID";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label2.Location = new System.Drawing.Point(69, 171);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Patient Name";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label3.Location = new System.Drawing.Point(450, 252);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Age";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label4.Location = new System.Drawing.Point(611, 172);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "Sex";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label5.Location = new System.Drawing.Point(614, 253);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(105, 17);
            this.label5.TabIndex = 8;
            this.label5.Text = "Mobile Number";
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label6.Location = new System.Drawing.Point(69, 252);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(57, 17);
            this.label6.TabIndex = 10;
            this.label6.Text = "Address";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label7.Location = new System.Drawing.Point(71, 401);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(79, 17);
            this.label7.TabIndex = 12;
            this.label7.Text = "Referred by";
            this.label7.Click += new System.EventHandler(this.label7_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label8.Location = new System.Drawing.Point(451, 333);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(73, 17);
            this.label8.TabIndex = 14;
            this.label8.Text = "Symptoms";
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label9.Location = new System.Drawing.Point(69, 333);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(73, 17);
            this.label9.TabIndex = 16;
            this.label9.Text = "Select Test";
            this.label9.Click += new System.EventHandler(this.label9_Click);
            // 
            // dtpDate
            // 
            this.dtpDate.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDate.Location = new System.Drawing.Point(424, 99);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(123, 29);
            this.dtpDate.TabIndex = 12;
            this.dtpDate.ValueChanged += new System.EventHandler(this.dtpDate_ValueChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label10.Location = new System.Drawing.Point(450, 72);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(116, 17);
            this.label10.TabIndex = 18;
            this.label10.Text = "Select Date & Time";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // dtpTime
            // 
            this.dtpTime.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dtpTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTime.Location = new System.Drawing.Point(592, 99);
            this.dtpTime.Name = "dtpTime";
            this.dtpTime.ShowUpDown = true;
            this.dtpTime.Size = new System.Drawing.Size(134, 29);
            this.dtpTime.TabIndex = 13;
            this.dtpTime.ValueChanged += new System.EventHandler(this.dtpTime_ValueChanged);
            // 
            // txtReferredBy
            // 
            this.txtReferredBy.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtReferredBy.Location = new System.Drawing.Point(12, 3);
            this.txtReferredBy.Multiline = true;
            this.txtReferredBy.Name = "txtReferredBy";
            this.txtReferredBy.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.txtReferredBy.Size = new System.Drawing.Size(75, 25);
            this.txtReferredBy.TabIndex = 9;
            this.txtReferredBy.Visible = false;
            this.txtReferredBy.TextChanged += new System.EventHandler(this.txtReferredBy_TextChanged);
            this.txtReferredBy.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtReferredBy_KeyDown);
            // 
            // txtSymptoms
            // 
            this.txtSymptoms.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSymptoms.Location = new System.Drawing.Point(11, 35);
            this.txtSymptoms.Multiline = true;
            this.txtSymptoms.Name = "txtSymptoms";
            this.txtSymptoms.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.txtSymptoms.Size = new System.Drawing.Size(76, 25);
            this.txtSymptoms.TabIndex = 10;
            this.txtSymptoms.Visible = false;
            this.txtSymptoms.TextChanged += new System.EventHandler(this.txtSymptoms_TextChanged);
            this.txtSymptoms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSymptoms_KeyDown);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton1.Location = new System.Drawing.Point(573, 204);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(53, 19);
            this.radioButton1.TabIndex = 4;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Male";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton2.Location = new System.Drawing.Point(624, 204);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(67, 19);
            this.radioButton2.TabIndex = 5;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Female";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton3.Location = new System.Drawing.Point(689, 204);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(55, 19);
            this.radioButton3.TabIndex = 6;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "Other";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.radioButton3_CheckedChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(213)))), ((int)(((byte)(219)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.btnCancel.Location = new System.Drawing.Point(677, 18);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 16;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(183)))), ((int)(((byte)(73)))));
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(554, 18);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 35);
            this.btnOk.TabIndex = 15;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // cmbSelectTest
            // 
            this.cmbSelectTest.BackColor = System.Drawing.Color.WhiteSmoke;
            this.cmbSelectTest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelectTest.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSelectTest.ForeColor = System.Drawing.Color.Black;
            this.cmbSelectTest.FormattingEnabled = true;
            this.cmbSelectTest.Items.AddRange(new object[] {
            "Uroflowmetry",
            "Uroflowmetry + EMG",
            "Cystometry",
            "Pressure Flow",
            "Pressure Flow + EMG",
            "Pressure Flow + Video",
            "Pressure Flow + EMG + Video",
            "UPP",
            "Whitaker Test",
            "Biofeedback",
            "Anal Manometry"});
            this.cmbSelectTest.Location = new System.Drawing.Point(44, 355);
            this.cmbSelectTest.Name = "cmbSelectTest";
            this.cmbSelectTest.Size = new System.Drawing.Size(322, 29);
            this.cmbSelectTest.TabIndex = 33;
            this.cmbSelectTest.SelectedIndexChanged += new System.EventHandler(this.cmbSelectTest_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(69)))), ((int)(((byte)(157)))));
            this.panel1.Controls.Add(this.panel14);
            this.panel1.Controls.Add(this.existButton);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(796, 48);
            this.panel1.TabIndex = 37;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // panel14
            // 
            this.panel14.BackgroundImage = global::SantronWinApp.Properties.Resources.Santron_Icon;
            this.panel14.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel14.Location = new System.Drawing.Point(7, 7);
            this.panel14.Name = "panel14";
            this.panel14.Size = new System.Drawing.Size(50, 35);
            this.panel14.TabIndex = 3;
            // 
            // existButton
            // 
            this.existButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.existButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.existButton.FlatAppearance.BorderSize = 0;
            this.existButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.existButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.existButton.ForeColor = System.Drawing.Color.White;
            this.existButton.Location = new System.Drawing.Point(753, 7);
            this.existButton.Name = "existButton";
            this.existButton.Size = new System.Drawing.Size(37, 32);
            this.existButton.TabIndex = 1;
            this.existButton.Text = "X";
            this.existButton.UseVisualStyleBackColor = true;
            this.existButton.Click += new System.EventHandler(this.existButton_Click);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.White;
            this.label12.Location = new System.Drawing.Point(67, 14);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(161, 21);
            this.label12.TabIndex = 0;
            this.label12.Text = "Patient Information";
            this.label12.Click += new System.EventHandler(this.label12_Click_1);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(232)))), ((int)(((byte)(241)))));
            this.panel2.Controls.Add(this.Edit_Button);
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Controls.Add(this.btnOk);
            this.panel2.Controls.Add(this.txtReferredBy);
            this.panel2.Controls.Add(this.txtSymptoms);
            this.panel2.Location = new System.Drawing.Point(1, 479);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(795, 65);
            this.panel2.TabIndex = 38;
            this.panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // Edit_Button
            // 
            this.Edit_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Edit_Button.BackColor = System.Drawing.Color.Tomato;
            this.Edit_Button.FlatAppearance.BorderSize = 0;
            this.Edit_Button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Edit_Button.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.Edit_Button.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.Edit_Button.Location = new System.Drawing.Point(428, 18);
            this.Edit_Button.Margin = new System.Windows.Forms.Padding(2);
            this.Edit_Button.Name = "Edit_Button";
            this.Edit_Button.Size = new System.Drawing.Size(100, 35);
            this.Edit_Button.TabIndex = 51;
            this.Edit_Button.Text = "Edit";
            this.Edit_Button.UseVisualStyleBackColor = false;
            this.Edit_Button.Click += new System.EventHandler(this.Edit_Button_Click);
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label36.ForeColor = System.Drawing.Color.Red;
            this.label36.Location = new System.Drawing.Point(38, 138);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(330, 15);
            this.label36.TabIndex = 124;
            this.label36.Text = "Please do not use  special character like . , / \' : \" ;  in Patient ID";
            this.label36.Click += new System.EventHandler(this.label36_Click);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.Color.Red;
            this.label15.Location = new System.Drawing.Point(131, 71);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(12, 15);
            this.label15.TabIndex = 128;
            this.label15.Text = "*";
            this.label15.Click += new System.EventHandler(this.label15_Click);
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.Red;
            this.label17.Location = new System.Drawing.Point(142, 333);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(12, 15);
            this.label17.TabIndex = 130;
            this.label17.Text = "*";
            this.label17.Click += new System.EventHandler(this.label17_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox1.ForeColor = System.Drawing.Color.Black;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(44, 423);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(322, 29);
            this.comboBox1.TabIndex = 131;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox2.ForeColor = System.Drawing.Color.Black;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(426, 353);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(300, 29);
            this.comboBox2.TabIndex = 132;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.label11.Location = new System.Drawing.Point(450, 171);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(53, 17);
            this.label11.TabIndex = 134;
            this.label11.Text = "Weight";
            this.label11.Click += new System.EventHandler(this.label11_Click_1);
            // 
            // panel13
            // 
            this.panel13.BackgroundImage = global::SantronWinApp.Properties.Resources.virus_1;
            this.panel13.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel13.Location = new System.Drawing.Point(426, 332);
            this.panel13.Name = "panel13";
            this.panel13.Size = new System.Drawing.Size(20, 17);
            this.panel13.TabIndex = 138;
            // 
            // panel12
            // 
            this.panel12.BackgroundImage = global::SantronWinApp.Properties.Resources.phone;
            this.panel12.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel12.Location = new System.Drawing.Point(588, 252);
            this.panel12.Name = "panel12";
            this.panel12.Size = new System.Drawing.Size(20, 17);
            this.panel12.TabIndex = 138;
            // 
            // panel11
            // 
            this.panel11.BackgroundImage = global::SantronWinApp.Properties.Resources.heading_icon;
            this.panel11.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel11.Location = new System.Drawing.Point(424, 252);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(20, 17);
            this.panel11.TabIndex = 138;
            // 
            // panel10
            // 
            this.panel10.BackgroundImage = global::SantronWinApp.Properties.Resources.reference;
            this.panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel10.Location = new System.Drawing.Point(45, 401);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(20, 17);
            this.panel10.TabIndex = 138;
            // 
            // panel9
            // 
            this.panel9.BackgroundImage = global::SantronWinApp.Properties.Resources.test;
            this.panel9.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel9.Location = new System.Drawing.Point(45, 333);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(20, 17);
            this.panel9.TabIndex = 138;
            // 
            // panel8
            // 
            this.panel8.BackgroundImage = global::SantronWinApp.Properties.Resources.AddressOne;
            this.panel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel8.Location = new System.Drawing.Point(45, 252);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(20, 17);
            this.panel8.TabIndex = 138;
            // 
            // panel7
            // 
            this.panel7.BackgroundImage = global::SantronWinApp.Properties.Resources.gender;
            this.panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel7.Location = new System.Drawing.Point(585, 171);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(20, 17);
            this.panel7.TabIndex = 138;
            // 
            // panel6
            // 
            this.panel6.BackgroundImage = global::SantronWinApp.Properties.Resources.weight;
            this.panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel6.Location = new System.Drawing.Point(424, 172);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(20, 17);
            this.panel6.TabIndex = 138;
            // 
            // panel5
            // 
            this.panel5.BackgroundImage = global::SantronWinApp.Properties.Resources.calendar;
            this.panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel5.Location = new System.Drawing.Point(424, 72);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(20, 17);
            this.panel5.TabIndex = 137;
            // 
            // panel4
            // 
            this.panel4.BackgroundImage = global::SantronWinApp.Properties.Resources.Patient;
            this.panel4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel4.Location = new System.Drawing.Point(45, 172);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(20, 17);
            this.panel4.TabIndex = 136;
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = global::SantronWinApp.Properties.Resources.Patient;
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel3.Location = new System.Drawing.Point(39, 72);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(20, 17);
            this.panel3.TabIndex = 135;
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
            this.txtMobileNo.Location = new System.Drawing.Point(583, 271);
            this.txtMobileNo.Name = "txtMobileNo";
            this.txtMobileNo.PasswordChar = '\0';
            this.txtMobileNo.ReadOnly = false;
            this.txtMobileNo.SelectionLength = 0;
            this.txtMobileNo.SelectionStart = 0;
            this.txtMobileNo.ShadowColor = System.Drawing.Color.Black;
            this.txtMobileNo.ShadowOpacity = 35;
            this.txtMobileNo.ShadowSize = 5;
            this.txtMobileNo.Size = new System.Drawing.Size(145, 44);
            this.txtMobileNo.TabIndex = 144;
            this.txtMobileNo.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtMobileNo.UseSystemPasswordChar = false;
            this.txtMobileNo.TextChanged += new System.EventHandler(this.txtMobileNo_TextChanged);
            this.txtMobileNo.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMobileNo_KeyPress);
            // 
            // txtAge
            // 
            this.txtAge.AutoFocusOnFormShown = false;
            this.txtAge.BorderColor = System.Drawing.Color.Transparent;
            this.txtAge.BorderThickness = 0;
            this.txtAge.CornerRadius = 3;
            this.txtAge.FillColor = System.Drawing.Color.White;
            this.txtAge.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtAge.ForeColor = System.Drawing.Color.Black;
            this.txtAge.Location = new System.Drawing.Point(419, 272);
            this.txtAge.Name = "txtAge";
            this.txtAge.PasswordChar = '\0';
            this.txtAge.ReadOnly = false;
            this.txtAge.SelectionLength = 0;
            this.txtAge.SelectionStart = 0;
            this.txtAge.ShadowColor = System.Drawing.Color.Black;
            this.txtAge.ShadowOpacity = 35;
            this.txtAge.ShadowSize = 5;
            this.txtAge.Size = new System.Drawing.Size(129, 44);
            this.txtAge.TabIndex = 143;
            this.txtAge.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtAge.UseSystemPasswordChar = false;
            this.txtAge.TextChanged += new System.EventHandler(this.txtAge_TextChanged);
            this.txtAge.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtAge_KeyPress);
            // 
            // txtAddress
            // 
            this.txtAddress.AutoFocusOnFormShown = false;
            this.txtAddress.BorderColor = System.Drawing.Color.Transparent;
            this.txtAddress.BorderThickness = 0;
            this.txtAddress.CornerRadius = 3;
            this.txtAddress.FillColor = System.Drawing.Color.White;
            this.txtAddress.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtAddress.ForeColor = System.Drawing.Color.Black;
            this.txtAddress.Location = new System.Drawing.Point(38, 272);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.PasswordChar = '\0';
            this.txtAddress.ReadOnly = false;
            this.txtAddress.SelectionLength = 0;
            this.txtAddress.SelectionStart = 0;
            this.txtAddress.ShadowColor = System.Drawing.Color.Black;
            this.txtAddress.ShadowOpacity = 35;
            this.txtAddress.ShadowSize = 5;
            this.txtAddress.Size = new System.Drawing.Size(330, 44);
            this.txtAddress.TabIndex = 142;
            this.txtAddress.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtAddress.UseSystemPasswordChar = false;
            // 
            // txtWeight
            // 
            this.txtWeight.AutoFocusOnFormShown = false;
            this.txtWeight.BorderColor = System.Drawing.Color.Transparent;
            this.txtWeight.BorderThickness = 0;
            this.txtWeight.CornerRadius = 3;
            this.txtWeight.FillColor = System.Drawing.Color.White;
            this.txtWeight.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtWeight.ForeColor = System.Drawing.Color.Black;
            this.txtWeight.Location = new System.Drawing.Point(419, 193);
            this.txtWeight.Name = "txtWeight";
            this.txtWeight.PasswordChar = '\0';
            this.txtWeight.ReadOnly = false;
            this.txtWeight.SelectionLength = 0;
            this.txtWeight.SelectionStart = 0;
            this.txtWeight.ShadowColor = System.Drawing.Color.Black;
            this.txtWeight.ShadowOpacity = 35;
            this.txtWeight.ShadowSize = 5;
            this.txtWeight.Size = new System.Drawing.Size(129, 44);
            this.txtWeight.TabIndex = 141;
            this.txtWeight.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtWeight.UseSystemPasswordChar = false;
            this.txtWeight.TextChanged += new System.EventHandler(this.txtWeight_TextChanged);
            this.txtWeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtWeight_KeyPress);
            // 
            // txtPatientName
            // 
            this.txtPatientName.AutoFocusOnFormShown = false;
            this.txtPatientName.BorderColor = System.Drawing.Color.Transparent;
            this.txtPatientName.BorderThickness = 0;
            this.txtPatientName.CornerRadius = 3;
            this.txtPatientName.FillColor = System.Drawing.Color.White;
            this.txtPatientName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPatientName.ForeColor = System.Drawing.Color.Black;
            this.txtPatientName.Location = new System.Drawing.Point(39, 193);
            this.txtPatientName.Name = "txtPatientName";
            this.txtPatientName.PasswordChar = '\0';
            this.txtPatientName.ReadOnly = false;
            this.txtPatientName.SelectionLength = 0;
            this.txtPatientName.SelectionStart = 0;
            this.txtPatientName.ShadowColor = System.Drawing.Color.Black;
            this.txtPatientName.ShadowOpacity = 35;
            this.txtPatientName.ShadowSize = 5;
            this.txtPatientName.Size = new System.Drawing.Size(330, 44);
            this.txtPatientName.TabIndex = 140;
            this.txtPatientName.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtPatientName.UseSystemPasswordChar = false;
            // 
            // txtPatientIdN
            // 
            this.txtPatientIdN.AutoFocusOnFormShown = false;
            this.txtPatientIdN.BorderColor = System.Drawing.Color.Transparent;
            this.txtPatientIdN.BorderThickness = 0;
            this.txtPatientIdN.CornerRadius = 3;
            this.txtPatientIdN.FillColor = System.Drawing.Color.White;
            this.txtPatientIdN.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtPatientIdN.ForeColor = System.Drawing.Color.Black;
            this.txtPatientIdN.Location = new System.Drawing.Point(38, 92);
            this.txtPatientIdN.Name = "txtPatientIdN";
            this.txtPatientIdN.PasswordChar = '\0';
            this.txtPatientIdN.ReadOnly = false;
            this.txtPatientIdN.SelectionLength = 0;
            this.txtPatientIdN.SelectionStart = 0;
            this.txtPatientIdN.ShadowColor = System.Drawing.Color.Black;
            this.txtPatientIdN.ShadowOpacity = 35;
            this.txtPatientIdN.ShadowSize = 5;
            this.txtPatientIdN.Size = new System.Drawing.Size(330, 44);
            this.txtPatientIdN.TabIndex = 139;
            this.txtPatientIdN.TextPadding = new System.Windows.Forms.Padding(12, 7, 12, 10);
            this.txtPatientIdN.UseSystemPasswordChar = false;
            this.txtPatientIdN.TextChanged += new System.EventHandler(this.txtPatientIdN_TextChanged);
            this.txtPatientIdN.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPatientIdN_KeyDown);
            this.txtPatientIdN.Leave += new System.EventHandler(this.txtPatientIdN_Leave);
            // 
            // Patient_Information
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(796, 543);
            this.Controls.Add(this.txtMobileNo);
            this.Controls.Add(this.txtAge);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.txtWeight);
            this.Controls.Add(this.txtPatientName);
            this.Controls.Add(this.txtPatientIdN);
            this.Controls.Add(this.panel13);
            this.Controls.Add(this.panel12);
            this.Controls.Add(this.panel11);
            this.Controls.Add(this.panel10);
            this.Controls.Add(this.panel9);
            this.Controls.Add(this.panel8);
            this.Controls.Add(this.panel7);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label36);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cmbSelectTest);
            this.Controls.Add(this.radioButton3);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.dtpTime);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.dtpDate);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "Patient_Information";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Patient Information";
            this.Load += new System.EventHandler(this.Patient_Information_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private DateTimePicker dtpDate;
        private Label label10;
        private DateTimePicker dtpTime;
        private TextBox txtReferredBy;
        private TextBox txtSymptoms;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private RadioButton radioButton3;
        private Button btnCancel;
        private Button btnOk;
        private ComboBox cmbSelectTest;
        private Panel panel1;
        private Label label12;
        private Button existButton;
        private Panel panel2;
        private Label label36;
        private Button Edit_Button;
        private Label label15;
        private Label label17;
        private ComboBox comboBox1;
        private ComboBox comboBox2;
        private Label label11;
        private Panel panel3;
        private Panel panel4;
        private Panel panel5;
        private Panel panel6;
        private Panel panel7;
        private Panel panel8;
        private Panel panel9;
        private Panel panel10;
        private Panel panel11;
        private Panel panel12;
        private Panel panel13;
        private ElevatedTextBox txtPatientIdN;
        private ElevatedTextBox txtPatientName;
        private ElevatedTextBox txtWeight;
        private ElevatedTextBox txtAddress;
        private ElevatedTextBox txtAge;
        private ElevatedTextBox txtMobileNo;
        private Panel panel14;
    }
}