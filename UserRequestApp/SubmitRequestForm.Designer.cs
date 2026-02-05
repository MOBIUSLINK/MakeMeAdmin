// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//
// Make Me Admin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// Make Me Admin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Make Me Admin. If not, see <http://www.gnu.org/licenses/>.
//

namespace SinclairCC.MakeMeAdmin
{
    /// <summary>
    /// This form allows the user to submit a request for administrator-level rights.
    /// </summary>
    internal partial class SubmitRequestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        /// <summary>
        /// The "add me" button.
        /// </summary>
        private System.Windows.Forms.Button addMeButton;

        /// <summary>
        /// Top menu strip.
        /// </summary>
        private System.Windows.Forms.MenuStrip menuStrip;

        /// <summary>
        /// Settings menu item.
        /// </summary>
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;

        /// <summary>
        /// Log viewer menu item.
        /// </summary>
        private System.Windows.Forms.ToolStripMenuItem logViewerMenuItem;

        /// <summary>
        /// Privilege timeout diagnostics menu item.
        /// </summary>
        private System.Windows.Forms.ToolStripMenuItem diagnosticsMenuItem;

        /// <summary>
        /// Exit menu item.
        /// </summary>
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;

        /// <summary>
        /// A tooltip to explain other controls.
        /// </summary>
        private System.Windows.Forms.ToolTip toolTip;

        /// <summary>
        /// The "remove me" button.
        /// </summary>
        private System.Windows.Forms.Button removeMeButton;

        /// <summary>
        /// A status bar strip.
        /// </summary>
        private System.Windows.Forms.StatusStrip statusStrip1;

        /// <summary>
        /// A label to display application status in the strip.
        /// </summary>
        private System.Windows.Forms.ToolStripStatusLabel appStatus;

        /// <summary>
        /// A background worker to control the state of the various buttons.
        /// </summary>
        private System.ComponentModel.BackgroundWorker buttonStateWorker;

        /// <summary>
        /// A background workr to add the current user to the Administrators group.
        /// </summary>
        private System.ComponentModel.BackgroundWorker addUserBackgroundWorker;

        /// <summary>
        /// A background workr to remove the current user from the Administrators group.
        /// </summary>
        private System.ComponentModel.BackgroundWorker removeUserBackgroundWorker;

        /// <summary>
        /// notification area icon
        /// </summary>
        private System.Windows.Forms.NotifyIcon notifyIcon;

        /// <summary>
        /// Main content panel for minimalist layout.
        /// </summary>
        private System.Windows.Forms.Panel mainContentPanel;

        /// <summary>
        /// Status label displayed at the bottom center.
        /// </summary>
        private System.Windows.Forms.Label statusLabel;

        /// <summary>
        /// Countdown label displaying remaining administrator rights time.
        /// </summary>
        private System.Windows.Forms.Label countdownLabel;

        /// <summary>
        /// Timer to update the countdown display.
        /// </summary>
        private System.Windows.Forms.Timer countdownTimer;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubmitRequestForm));
            this.addMeButton = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.removeMeButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.appStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainContentPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.countdownLabel = new System.Windows.Forms.Label();
            this.countdownTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logViewerMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.diagnosticsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buttonStateWorker = new System.ComponentModel.BackgroundWorker();
            this.addUserBackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.removeUserBackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.statusStrip1.SuspendLayout();
            this.mainContentPanel.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // addMeButton
            // 
            resources.ApplyResources(this.addMeButton, "addMeButton");
            this.addMeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(142)))), ((int)(((byte)(60)))));
            this.addMeButton.FlatAppearance.BorderSize = 0;
            this.addMeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.addMeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(160)))), ((int)(((byte)(70)))));
            this.addMeButton.ForeColor = System.Drawing.Color.White;
            this.addMeButton.Name = "addMeButton";
            this.toolTip.SetToolTip(this.addMeButton, resources.GetString("addMeButton.ToolTip"));
            this.addMeButton.UseMnemonic = false;
            this.addMeButton.UseVisualStyleBackColor = false;
            this.addMeButton.Click += new System.EventHandler(this.ClickSubmitButton);
            this.addMeButton.Paint += new System.Windows.Forms.PaintEventHandler(this.addMeButton_Paint);
            this.addMeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Button_MouseDown);
            this.addMeButton.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.addMeButton.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            this.addMeButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Button_MouseUp);
            // 
            // removeMeButton
            // 
            resources.ApplyResources(this.removeMeButton, "removeMeButton");
            this.removeMeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.removeMeButton.FlatAppearance.BorderSize = 0;
            this.removeMeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.removeMeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(57)))), ((int)(((byte)(53)))));
            this.removeMeButton.ForeColor = System.Drawing.Color.White;
            this.removeMeButton.Name = "removeMeButton";
            this.toolTip.SetToolTip(this.removeMeButton, resources.GetString("removeMeButton.ToolTip"));
            this.removeMeButton.UseVisualStyleBackColor = false;
            this.removeMeButton.Click += new System.EventHandler(this.ClickRemoveRightsButton);
            this.removeMeButton.Paint += new System.Windows.Forms.PaintEventHandler(this.removeMeButton_Paint);
            this.removeMeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Button_MouseDown);
            this.removeMeButton.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.removeMeButton.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            this.removeMeButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Button_MouseUp);
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.appStatus});
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Name = "statusStrip1";
            // 
            // appStatus
            // 
            this.appStatus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            resources.ApplyResources(this.appStatus, "appStatus");
            this.appStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.appStatus.Name = "appStatus";
            this.appStatus.Spring = true;
            this.appStatus.Click += new System.EventHandler(this.appStatus_Click);
            // 
            // mainContentPanel
            // 
            this.mainContentPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.mainContentPanel.Controls.Add(this.addMeButton);
            this.mainContentPanel.Controls.Add(this.removeMeButton);
            this.mainContentPanel.Controls.Add(this.statusLabel);
            this.mainContentPanel.Controls.Add(this.countdownLabel);
            resources.ApplyResources(this.mainContentPanel, "mainContentPanel");
            this.mainContentPanel.Name = "mainContentPanel";
            // 
            // statusLabel (hidden - status shown only in bottom status bar)
            // 
            resources.ApplyResources(this.statusLabel, "statusLabel");
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Visible = false;
            // 
            // countdownLabel (hidden - countdown shown only in bottom status bar)
            // 
            resources.ApplyResources(this.countdownLabel, "countdownLabel");
            this.countdownLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(181)))), ((int)(((byte)(246)))));
            this.countdownLabel.Name = "countdownLabel";
            this.countdownLabel.Visible = false;
            // 
            // countdownTimer
            // 
            this.countdownTimer.Interval = 1000;
            this.countdownTimer.Tick += new System.EventHandler(this.countdownTimer_Tick);
            // 
            // menuStrip
            // 
            this.menuStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.menuStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsMenuItem,
            this.logViewerMenuItem,
            this.diagnosticsMenuItem,
            this.exitMenuItem});
            resources.ApplyResources(this.menuStrip, "menuStrip");
            this.menuStrip.Name = "menuStrip";
            // 
            // settingsMenuItem
            // 
            this.settingsMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.settingsMenuItem.Name = "settingsMenuItem";
            resources.ApplyResources(this.settingsMenuItem, "settingsMenuItem");
            this.settingsMenuItem.Click += new System.EventHandler(this.ClickSettingsButton);
            // 
            // logViewerMenuItem
            // 
            this.logViewerMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.logViewerMenuItem.Name = "logViewerMenuItem";
            resources.ApplyResources(this.logViewerMenuItem, "logViewerMenuItem");
            this.logViewerMenuItem.Click += new System.EventHandler(this.ClickLogViewerButton);
            // 
            // diagnosticsMenuItem
            // 
            this.diagnosticsMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.diagnosticsMenuItem.Name = "diagnosticsMenuItem";
            resources.ApplyResources(this.diagnosticsMenuItem, "diagnosticsMenuItem");
            this.diagnosticsMenuItem.Click += new System.EventHandler(this.ClickDiagnosticsButton);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.exitMenuItem.Name = "exitMenuItem";
            resources.ApplyResources(this.exitMenuItem, "exitMenuItem");
            this.exitMenuItem.Click += new System.EventHandler(this.ClickExitButton);
            // 
            // buttonStateWorker
            // 
            this.buttonStateWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.DoButtonStateWork);
            this.buttonStateWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ButtonStateWorkCompleted);
            // 
            // addUserBackgroundWorker
            // 
            this.addUserBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.addUserBackgroundWorker_DoWork);
            this.addUserBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.addUserBackgroundWorker_RunWorkerCompleted);
            // 
            // removeUserBackgroundWorker
            // 
            this.removeUserBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.removeUserBackgroundWorker_DoWork);
            this.removeUserBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.removeUserBackgroundWorker_RunWorkerCompleted);
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            resources.ApplyResources(this.notifyIcon, "notifyIcon");
            this.notifyIcon.BalloonTipClosed += new System.EventHandler(this.notifyIcon_BalloonTipClosed);
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
            // 
            // SubmitRequestForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.mainContentPanel);
            this.Controls.Add(this.menuStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SubmitRequestForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Load += new System.EventHandler(this.FormLoad);
            this.VisibleChanged += new System.EventHandler(this.SubmitRequestForm_VisibleChanged);
            this.HandleCreated += new System.EventHandler(this.SubmitRequestForm_HandleCreated);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.mainContentPanel.ResumeLayout(false);
            this.mainContentPanel.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

    }
}

