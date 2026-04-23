using Chem4Word.Core.UI.Controls;

namespace Chem4Word.Searcher.ExamplePlugIn
{
    partial class ExampleSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExampleSettings));
            this.tabControlEx = new Chem4Word.Core.UI.Controls.TabControlEx();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.Property1 = new System.Windows.Forms.CheckBox();
            this.Property2 = new System.Windows.Forms.CheckBox();
            this.SetDefaults = new System.Windows.Forms.Button();
            this.Ok = new System.Windows.Forms.Button();
            this.tabControlEx.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlEx
            // 
            this.tabControlEx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlEx.Controls.Add(this.tabPage1);
            this.tabControlEx.Location = new System.Drawing.Point(17, 20);
            this.tabControlEx.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabControlEx.Name = "tabControlEx";
            this.tabControlEx.SelectedIndex = 0;
            this.tabControlEx.Size = new System.Drawing.Size(544, 308);
            this.tabControlEx.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tabPage1.Controls.Add(this.Property1);
            this.tabPage1.Controls.Add(this.Property2);
            this.tabPage1.Location = new System.Drawing.Point(0, 27);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPage1.Size = new System.Drawing.Size(544, 281);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Display";
            // 
            // Property1
            // 
            this.Property1.AutoSize = true;
            this.Property1.Checked = true;
            this.Property1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Property1.Location = new System.Drawing.Point(16, 18);
            this.Property1.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Property1.Name = "Property1";
            this.Property1.Size = new System.Drawing.Size(92, 24);
            this.Property1.TabIndex = 7;
            this.Property1.Text = "Property1";
            this.Property1.UseVisualStyleBackColor = true;
            this.Property1.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_Property1);
            // 
            // Property2
            // 
            this.Property2.AutoSize = true;
            this.Property2.Checked = true;
            this.Property2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Property2.Location = new System.Drawing.Point(16, 75);
            this.Property2.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Property2.Name = "Property2";
            this.Property2.Size = new System.Drawing.Size(92, 24);
            this.Property2.TabIndex = 8;
            this.Property2.Text = "Property2";
            this.Property2.UseVisualStyleBackColor = true;
            this.Property2.CheckedChanged += new System.EventHandler(this.OnCheckedChanged_Property2);
            // 
            // SetDefaults
            // 
            this.SetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SetDefaults.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.SetDefaults.Location = new System.Drawing.Point(391, 360);
            this.SetDefaults.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.SetDefaults.Name = "SetDefaults";
            this.SetDefaults.Size = new System.Drawing.Size(80, 27);
            this.SetDefaults.TabIndex = 11;
            this.SetDefaults.Text = "Defaults";
            this.SetDefaults.UseVisualStyleBackColor = true;
            this.SetDefaults.Click += new System.EventHandler(this.OnClick_SetDefaults);
            // 
            // Ok
            // 
            this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Ok.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Ok.Location = new System.Drawing.Point(481, 360);
            this.Ok.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(80, 27);
            this.Ok.TabIndex = 10;
            this.Ok.Text = "OK";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.OnClick_Ok);
            // 
            // ExampleSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 402);
            this.Controls.Add(this.SetDefaults);
            this.Controls.Add(this.Ok);
            this.Controls.Add(this.tabControlEx);
            this.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExampleSettings";
            this.Text = "Example - Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_Settings);
            this.Load += new System.EventHandler(this.OnLoad_Settings);
            this.tabControlEx.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControlEx tabControlEx;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button SetDefaults;
        private System.Windows.Forms.Button Ok;
        private System.Windows.Forms.CheckBox Property1;
        private System.Windows.Forms.CheckBox Property2;
    }
}