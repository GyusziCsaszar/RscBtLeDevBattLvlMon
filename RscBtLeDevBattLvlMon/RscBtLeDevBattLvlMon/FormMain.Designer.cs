namespace RscBtLeDevBattLvlMon
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnEnum = new System.Windows.Forms.Button();
            this.lbLog = new System.Windows.Forms.ListBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.chbAutoStopOnEnumComp = new System.Windows.Forms.CheckBox();
            this.btnTogleIcon = new System.Windows.Forms.Button();
            this.tmrUpdate = new System.Windows.Forms.Timer(this.components);
            this.btnInfoBar = new System.Windows.Forms.Button();
            this.lblAlertLevel = new System.Windows.Forms.Label();
            this.tbAlertLevel = new System.Windows.Forms.TextBox();
            this.chbLog = new System.Windows.Forms.CheckBox();
            this.chbAutoHide = new System.Windows.Forms.CheckBox();
            this.chbAutoStart = new System.Windows.Forms.CheckBox();
            this.lblInterval = new System.Windows.Forms.Label();
            this.tbUpdateInterval = new System.Windows.Forms.TextBox();
            this.chbDebugDelay = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnEnum
            // 
            this.btnEnum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEnum.Location = new System.Drawing.Point(12, 31);
            this.btnEnum.Name = "btnEnum";
            this.btnEnum.Size = new System.Drawing.Size(160, 25);
            this.btnEnum.TabIndex = 0;
            this.btnEnum.Text = "Disvover devices";
            this.btnEnum.UseVisualStyleBackColor = true;
            this.btnEnum.Click += new System.EventHandler(this.btnEnum_Click);
            // 
            // lbLog
            // 
            this.lbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbLog.FormattingEnabled = true;
            this.lbLog.ItemHeight = 15;
            this.lbLog.Location = new System.Drawing.Point(12, 296);
            this.lbLog.Name = "lbLog";
            this.lbLog.Size = new System.Drawing.Size(755, 137);
            this.lbLog.TabIndex = 1;
            this.lbLog.Visible = false;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(178, 31);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(91, 25);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Visible = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // lvDevices
            // 
            this.lvDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvDevices.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.HideSelection = false;
            this.lvDevices.Location = new System.Drawing.Point(12, 114);
            this.lvDevices.MultiSelect = false;
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(755, 319);
            this.lvDevices.TabIndex = 3;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            // 
            // chbAutoStopOnEnumComp
            // 
            this.chbAutoStopOnEnumComp.AutoSize = true;
            this.chbAutoStopOnEnumComp.Checked = true;
            this.chbAutoStopOnEnumComp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbAutoStopOnEnumComp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chbAutoStopOnEnumComp.Location = new System.Drawing.Point(12, 62);
            this.chbAutoStopOnEnumComp.Name = "chbAutoStopOnEnumComp";
            this.chbAutoStopOnEnumComp.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.chbAutoStopOnEnumComp.Size = new System.Drawing.Size(257, 19);
            this.chbAutoStopOnEnumComp.TabIndex = 4;
            this.chbAutoStopOnEnumComp.Text = "Auto stop when device discovery completed";
            this.chbAutoStopOnEnumComp.UseVisualStyleBackColor = true;
            // 
            // btnTogleIcon
            // 
            this.btnTogleIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTogleIcon.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTogleIcon.Location = new System.Drawing.Point(532, 31);
            this.btnTogleIcon.Name = "btnTogleIcon";
            this.btnTogleIcon.Size = new System.Drawing.Size(235, 25);
            this.btnTogleIcon.TabIndex = 5;
            this.btnTogleIcon.Text = "Toggle Notification Icon";
            this.btnTogleIcon.UseVisualStyleBackColor = true;
            this.btnTogleIcon.Click += new System.EventHandler(this.btnTogleIcon_Click);
            // 
            // tmrUpdate
            // 
            this.tmrUpdate.Interval = 300000;
            this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
            // 
            // btnInfoBar
            // 
            this.btnInfoBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInfoBar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInfoBar.Location = new System.Drawing.Point(0, 0);
            this.btnInfoBar.Name = "btnInfoBar";
            this.btnInfoBar.Size = new System.Drawing.Size(780, 25);
            this.btnInfoBar.TabIndex = 6;
            this.btnInfoBar.Text = "N/A";
            this.btnInfoBar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInfoBar.UseVisualStyleBackColor = true;
            this.btnInfoBar.Visible = false;
            this.btnInfoBar.Click += new System.EventHandler(this.btnInfoBar_Click);
            // 
            // lblAlertLevel
            // 
            this.lblAlertLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAlertLevel.AutoSize = true;
            this.lblAlertLevel.Location = new System.Drawing.Point(607, 63);
            this.lblAlertLevel.Name = "lblAlertLevel";
            this.lblAlertLevel.Size = new System.Drawing.Size(86, 15);
            this.lblAlertLevel.TabIndex = 7;
            this.lblAlertLevel.Text = "Alert Level (%):";
            // 
            // tbAlertLevel
            // 
            this.tbAlertLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAlertLevel.Location = new System.Drawing.Point(699, 60);
            this.tbAlertLevel.MaxLength = 2;
            this.tbAlertLevel.Name = "tbAlertLevel";
            this.tbAlertLevel.Size = new System.Drawing.Size(68, 23);
            this.tbAlertLevel.TabIndex = 8;
            this.tbAlertLevel.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbAlertLevel.TextChanged += new System.EventHandler(this.tbAlertLevel_TextChanged);
            this.tbAlertLevel.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbAlertLevel_KeyPress);
            // 
            // chbLog
            // 
            this.chbLog.AutoSize = true;
            this.chbLog.Location = new System.Drawing.Point(12, 87);
            this.chbLog.Name = "chbLog";
            this.chbLog.Size = new System.Drawing.Size(75, 19);
            this.chbLog.TabIndex = 9;
            this.chbLog.Text = "Show log";
            this.chbLog.UseVisualStyleBackColor = true;
            this.chbLog.CheckedChanged += new System.EventHandler(this.chbLog_CheckedChanged);
            // 
            // chbAutoHide
            // 
            this.chbAutoHide.AutoSize = true;
            this.chbAutoHide.Location = new System.Drawing.Point(319, 35);
            this.chbAutoHide.Name = "chbAutoHide";
            this.chbAutoHide.Size = new System.Drawing.Size(146, 19);
            this.chbAutoHide.TabIndex = 10;
            this.chbAutoHide.Text = "Auto hide app window";
            this.chbAutoHide.UseVisualStyleBackColor = true;
            this.chbAutoHide.CheckedChanged += new System.EventHandler(this.chbAutoHide_CheckedChanged);
            // 
            // chbAutoStart
            // 
            this.chbAutoStart.AutoSize = true;
            this.chbAutoStart.Location = new System.Drawing.Point(319, 62);
            this.chbAutoStart.Name = "chbAutoStart";
            this.chbAutoStart.Size = new System.Drawing.Size(156, 19);
            this.chbAutoStart.TabIndex = 11;
            this.chbAutoStart.Text = "Auto start with Windows";
            this.chbAutoStart.UseVisualStyleBackColor = true;
            this.chbAutoStart.CheckedChanged += new System.EventHandler(this.chbAutoStart_CheckedChanged);
            // 
            // lblInterval
            // 
            this.lblInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(486, 88);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(207, 15);
            this.lblInterval.TabIndex = 12;
            this.lblInterval.Text = "Update Interval (min / Empty = 1 sec):";
            // 
            // tbUpdateInterval
            // 
            this.tbUpdateInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbUpdateInterval.Location = new System.Drawing.Point(699, 85);
            this.tbUpdateInterval.MaxLength = 3;
            this.tbUpdateInterval.Name = "tbUpdateInterval";
            this.tbUpdateInterval.Size = new System.Drawing.Size(68, 23);
            this.tbUpdateInterval.TabIndex = 13;
            this.tbUpdateInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbUpdateInterval.TextChanged += new System.EventHandler(this.tbUpdateInterval_TextChanged);
            this.tbUpdateInterval.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbUpdateInterval_KeyPress);
            // 
            // chbDebugDelay
            // 
            this.chbDebugDelay.AutoSize = true;
            this.chbDebugDelay.Location = new System.Drawing.Point(88, 87);
            this.chbDebugDelay.Name = "chbDebugDelay";
            this.chbDebugDelay.Size = new System.Drawing.Size(181, 19);
            this.chbDebugDelay.TabIndex = 14;
            this.chbDebugDelay.Text = "Test Update Task Delay (8 sec)";
            this.chbDebugDelay.UseVisualStyleBackColor = true;
            this.chbDebugDelay.CheckedChanged += new System.EventHandler(this.chbDebugDelay_CheckedChanged);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(779, 450);
            this.Controls.Add(this.chbAutoStart);
            this.Controls.Add(this.chbAutoHide);
            this.Controls.Add(this.chbDebugDelay);
            this.Controls.Add(this.lblInterval);
            this.Controls.Add(this.lblAlertLevel);
            this.Controls.Add(this.tbAlertLevel);
            this.Controls.Add(this.tbUpdateInterval);
            this.Controls.Add(this.lbLog);
            this.Controls.Add(this.chbLog);
            this.Controls.Add(this.btnEnum);
            this.Controls.Add(this.btnTogleIcon);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.chbAutoStopOnEnumComp);
            this.Controls.Add(this.btnInfoBar);
            this.Controls.Add(this.lvDevices);
            this.Name = "FormMain";
            this.Text = "FormMain";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnEnum;
        private System.Windows.Forms.ListBox lbLog;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.ListView lvDevices;
        private System.Windows.Forms.CheckBox chbAutoStopOnEnumComp;
        private System.Windows.Forms.Button btnTogleIcon;
        private System.Windows.Forms.Timer tmrUpdate;
        private System.Windows.Forms.Button btnInfoBar;
        private System.Windows.Forms.Label lblAlertLevel;
        private System.Windows.Forms.TextBox tbAlertLevel;
        private System.Windows.Forms.CheckBox chbLog;
        private System.Windows.Forms.CheckBox chbAutoHide;
        private System.Windows.Forms.CheckBox chbAutoStart;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.TextBox tbUpdateInterval;
        private System.Windows.Forms.CheckBox chbDebugDelay;
    }
}

