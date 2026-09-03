namespace Chem4WordUpdater
{
    partial class Updater
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Updater));
            this.ProgressBar1 = new System.Windows.Forms.ProgressBar();
            this.UpdateNow = new System.Windows.Forms.Button();
            this.Information = new System.Windows.Forms.Label();
            this.Timer1 = new System.Windows.Forms.Timer(this.components);
            this.WordInstances = new System.Windows.Forms.ListBox();
            this.ProcessesLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ProgressBar1
            // 
            this.ProgressBar1.Location = new System.Drawing.Point(175, 275);
            this.ProgressBar1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ProgressBar1.MarqueeAnimationSpeed = 0;
            this.ProgressBar1.Name = "ProgressBar1";
            this.ProgressBar1.Size = new System.Drawing.Size(216, 26);
            this.ProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.ProgressBar1.TabIndex = 5;
            // 
            // UpdateNow
            // 
            this.UpdateNow.Location = new System.Drawing.Point(395, 275);
            this.UpdateNow.Margin = new System.Windows.Forms.Padding(5);
            this.UpdateNow.Name = "UpdateNow";
            this.UpdateNow.Size = new System.Drawing.Size(86, 26);
            this.UpdateNow.TabIndex = 6;
            this.UpdateNow.Text = "Cancel";
            this.UpdateNow.UseVisualStyleBackColor = true;
            this.UpdateNow.Click += new System.EventHandler(this.OnClick_Cancel);
            // 
            // Information
            // 
            this.Information.BackColor = System.Drawing.Color.Transparent;
            this.Information.Location = new System.Drawing.Point(175, 197);
            this.Information.Name = "Information";
            this.Information.Size = new System.Drawing.Size(306, 73);
            this.Information.TabIndex = 7;
            this.Information.Text = "Your update is downloading ...";
            // 
            // Timer1
            // 
            this.Timer1.Interval = 250;
            this.Timer1.Tick += new System.EventHandler(this.OnTick_Timer1);
            // 
            // WordInstances
            // 
            this.WordInstances.FormattingEnabled = true;
            this.WordInstances.ItemHeight = 16;
            this.WordInstances.Location = new System.Drawing.Point(175, 29);
            this.WordInstances.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.WordInstances.Name = "WordInstances";
            this.WordInstances.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.WordInstances.Size = new System.Drawing.Size(306, 164);
            this.WordInstances.TabIndex = 8;
            // 
            // ProcessesLabel
            // 
            this.ProcessesLabel.AutoSize = true;
            this.ProcessesLabel.BackColor = System.Drawing.Color.Transparent;
            this.ProcessesLabel.Location = new System.Drawing.Point(172, 7);
            this.ProcessesLabel.Name = "ProcessesLabel";
            this.ProcessesLabel.Size = new System.Drawing.Size(155, 16);
            this.ProcessesLabel.TabIndex = 9;
            this.ProcessesLabel.Text = "Microsoft Word Processes";
            // 
            // Updater
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Chem4WordUpdater.Properties.Resources.WixUIDialog;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(491, 312);
            this.Controls.Add(this.ProcessesLabel);
            this.Controls.Add(this.WordInstances);
            this.Controls.Add(this.Information);
            this.Controls.Add(this.UpdateNow);
            this.Controls.Add(this.ProgressBar1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Updater";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chem4Word Updater";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_Updater);
            this.Load += new System.EventHandler(this.OnLoad_Updater);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar ProgressBar1;
        private System.Windows.Forms.Button UpdateNow;
        private System.Windows.Forms.Label Information;
        private System.Windows.Forms.Timer Timer1;
        private System.Windows.Forms.ListBox WordInstances;
        private System.Windows.Forms.Label ProcessesLabel;
    }
}