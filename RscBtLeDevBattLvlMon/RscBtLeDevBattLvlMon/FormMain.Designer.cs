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
            this.btnEnum = new System.Windows.Forms.Button();
            this.lbLog = new System.Windows.Forms.ListBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.chbAutoStopOnEnumComp = new System.Windows.Forms.CheckBox();
            this.btnTogleIcon = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnEnum
            // 
            this.btnEnum.Location = new System.Drawing.Point(12, 12);
            this.btnEnum.Name = "btnEnum";
            this.btnEnum.Size = new System.Drawing.Size(132, 23);
            this.btnEnum.TabIndex = 0;
            this.btnEnum.Text = "Enumerate";
            this.btnEnum.UseVisualStyleBackColor = true;
            this.btnEnum.Click += new System.EventHandler(this.btnEnum_Click);
            // 
            // lbLog
            // 
            this.lbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbLog.FormattingEnabled = true;
            this.lbLog.ItemHeight = 15;
            this.lbLog.Location = new System.Drawing.Point(12, 296);
            this.lbLog.Name = "lbLog";
            this.lbLog.Size = new System.Drawing.Size(755, 139);
            this.lbLog.TabIndex = 1;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(150, 12);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // lvDevices
            // 
            this.lvDevices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.HideSelection = false;
            this.lvDevices.Location = new System.Drawing.Point(12, 56);
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(755, 221);
            this.lvDevices.TabIndex = 3;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            // 
            // chbAutoStopOnEnumComp
            // 
            this.chbAutoStopOnEnumComp.AutoSize = true;
            this.chbAutoStopOnEnumComp.Checked = true;
            this.chbAutoStopOnEnumComp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbAutoStopOnEnumComp.Location = new System.Drawing.Point(232, 15);
            this.chbAutoStopOnEnumComp.Name = "chbAutoStopOnEnumComp";
            this.chbAutoStopOnEnumComp.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.chbAutoStopOnEnumComp.Size = new System.Drawing.Size(244, 19);
            this.chbAutoStopOnEnumComp.TabIndex = 4;
            this.chbAutoStopOnEnumComp.Text = "Auto stop when enumeration completed.";
            this.chbAutoStopOnEnumComp.UseVisualStyleBackColor = true;
            // 
            // btnTogleIcon
            // 
            this.btnTogleIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTogleIcon.Location = new System.Drawing.Point(608, 12);
            this.btnTogleIcon.Name = "btnTogleIcon";
            this.btnTogleIcon.Size = new System.Drawing.Size(159, 23);
            this.btnTogleIcon.TabIndex = 5;
            this.btnTogleIcon.Text = "Togle Notification Icon";
            this.btnTogleIcon.UseVisualStyleBackColor = true;
            this.btnTogleIcon.Click += new System.EventHandler(this.btnTogleIcon_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(779, 450);
            this.Controls.Add(this.btnTogleIcon);
            this.Controls.Add(this.chbAutoStopOnEnumComp);
            this.Controls.Add(this.lvDevices);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lbLog);
            this.Controls.Add(this.btnEnum);
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
    }
}

