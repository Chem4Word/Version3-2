﻿namespace WinForms.TestHarness
{
    partial class Dumper
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
            this.Dump = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Dump
            // 
            this.Dump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Dump.Location = new System.Drawing.Point(0, 0);
            this.Dump.Multiline = true;
            this.Dump.Name = "Dump";
            this.Dump.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Dump.Size = new System.Drawing.Size(800, 450);
            this.Dump.TabIndex = 0;
            this.Dump.WordWrap = false;
            // 
            // Dumper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Dump);
            this.Name = "Dumper";
            this.Text = "Dumper";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Dump;
    }
}