using LiveSplit.UI;
using System.Xml;

namespace LiveSplit.TimeAttackPause.UI.Components
{
    partial class TimeAttackPauseSettings
    {
        public string DefaultSavePath { get; set; }

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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.SaveFilePathTextBox = new System.Windows.Forms.TextBox();
            this.SetDefaultSaveFileButton = new System.Windows.Forms.Button();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.EnableAutosaveCheckBox = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.AutoSize = true;
            this.tableLayoutPanel.ColumnCount = 1;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel.Controls.Add(this.SaveFilePathTextBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.BrowseButton, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.EnableAutosaveCheckBox, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.SetDefaultSaveFileButton, 0, 3);
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(203, 110);
            this.tableLayoutPanel.TabIndex = 0;
            this.tableLayoutPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel_Paint);
            // 
            // SaveFilePathTextBox
            // 
            this.SaveFilePathTextBox.Location = new System.Drawing.Point(3, 3);
            this.SaveFilePathTextBox.Name = "SaveFilePathTextBox";
            this.SaveFilePathTextBox.Size = new System.Drawing.Size(197, 20);
            this.SaveFilePathTextBox.TabIndex = 0;
            // 
            // BrowseButton
            // 
            this.BrowseButton.Location = new System.Drawing.Point(3, 27);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(197, 23);
            this.BrowseButton.TabIndex = 2;
            this.BrowseButton.Text = "Browse...";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // EnableAutosaveCheckBox
            // 
            this.EnableAutosaveCheckBox.AutoSize = true;
            this.EnableAutosaveCheckBox.Location = new System.Drawing.Point(3, 58);
            this.EnableAutosaveCheckBox.Name = "EnableAutosaveCheckBox";
            this.EnableAutosaveCheckBox.Size = new System.Drawing.Size(104, 17);
            this.EnableAutosaveCheckBox.TabIndex = 3;
            this.EnableAutosaveCheckBox.Text = "Enable Autosave";
            this.EnableAutosaveCheckBox.UseVisualStyleBackColor = true;
            this.EnableAutosaveCheckBox.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // SetDefaultSaveFileButton
            // 
            this.SetDefaultSaveFileButton.Location = new System.Drawing.Point(3, 80);
            this.SetDefaultSaveFileButton.Name = "SetDefaultSaveFileButton";
            this.SetDefaultSaveFileButton.Size = new System.Drawing.Size(197, 23);
            this.SetDefaultSaveFileButton.TabIndex = 1;
            this.SetDefaultSaveFileButton.Text = "Save Default Path";
            this.SetDefaultSaveFileButton.UseVisualStyleBackColor = true;
            this.SetDefaultSaveFileButton.Click += new System.EventHandler(this.SetDefaultSaveFileButton_Click);
            // 
            // TimeAttackPauseSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.tableLayoutPanel);
            this.Name = "TimeAttackPauseSettings";
            this.Size = new System.Drawing.Size(206, 150);
            this.Load += new System.EventHandler(this.TimeAttackPauseSettings_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.TextBox SaveFilePathTextBox;
        private System.Windows.Forms.Button SetDefaultSaveFileButton;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.CheckBox EnableAutosaveCheckBox;
    }
}
