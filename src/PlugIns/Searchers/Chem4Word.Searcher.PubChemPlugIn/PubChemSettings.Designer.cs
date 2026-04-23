using Chem4Word.Core.UI.Controls;

namespace Chem4Word.Searcher.PubChemPlugIn
{
    partial class PubChemSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PubChemSettings));
            this.SetDefaults = new System.Windows.Forms.Button();
            this.Ok = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.PubChemRestUri = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PubChemWsUri = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ResultsPerCall = new System.Windows.Forms.NumericUpDown();
            this.DisplayOrder = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ResultsPerCall)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DisplayOrder)).BeginInit();
            this.SuspendLayout();
            // 
            // SetDefaults
            // 
            this.SetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SetDefaults.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.SetDefaults.Location = new System.Drawing.Point(386, 178);
            this.SetDefaults.Margin = new System.Windows.Forms.Padding(6);
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
            this.Ok.Location = new System.Drawing.Point(479, 178);
            this.Ok.Margin = new System.Windows.Forms.Padding(6);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(80, 27);
            this.Ok.TabIndex = 10;
            this.Ok.Text = "OK";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.OnClick_Ok);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 102);
            this.label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 20);
            this.label5.TabIndex = 28;
            this.label5.Text = "Rest API Url";
            // 
            // PubChemRestUri
            // 
            this.PubChemRestUri.Location = new System.Drawing.Point(165, 99);
            this.PubChemRestUri.Margin = new System.Windows.Forms.Padding(5);
            this.PubChemRestUri.Name = "PubChemRestUri";
            this.PubChemRestUri.Size = new System.Drawing.Size(331, 27);
            this.PubChemRestUri.TabIndex = 27;
            this.PubChemRestUri.Text = "https://pubchem.ncbi.nlm.nih.gov/";
            this.PubChemRestUri.TextChanged += new System.EventHandler(this.OnTextChanged_PubChemRestUri);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 61);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 20);
            this.label3.TabIndex = 26;
            this.label3.Text = "WebService Url";
            // 
            // PubChemWsUri
            // 
            this.PubChemWsUri.Location = new System.Drawing.Point(165, 58);
            this.PubChemWsUri.Margin = new System.Windows.Forms.Padding(5);
            this.PubChemWsUri.Name = "PubChemWsUri";
            this.PubChemWsUri.Size = new System.Drawing.Size(331, 27);
            this.PubChemWsUri.TabIndex = 25;
            this.PubChemWsUri.Text = "https://eutils.ncbi.nlm.nih.gov/";
            this.PubChemWsUri.TextChanged += new System.EventHandler(this.OnTextChanged_PubChemWsUri);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 145);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 20);
            this.label1.TabIndex = 29;
            this.label1.Text = "Results per call";
            // 
            // ResultsPerCall
            // 
            this.ResultsPerCall.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.ResultsPerCall.Location = new System.Drawing.Point(165, 142);
            this.ResultsPerCall.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ResultsPerCall.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.ResultsPerCall.Name = "ResultsPerCall";
            this.ResultsPerCall.Size = new System.Drawing.Size(70, 27);
            this.ResultsPerCall.TabIndex = 30;
            this.ResultsPerCall.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ResultsPerCall.ValueChanged += new System.EventHandler(this.OnValueChanged_ResultsPerCall);
            // 
            // DisplayOrder
            // 
            this.DisplayOrder.Location = new System.Drawing.Point(165, 16);
            this.DisplayOrder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DisplayOrder.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.DisplayOrder.Name = "DisplayOrder";
            this.DisplayOrder.Size = new System.Drawing.Size(70, 27);
            this.DisplayOrder.TabIndex = 32;
            this.DisplayOrder.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.DisplayOrder.ValueChanged += new System.EventHandler(this.OnValueChanged_DisplayOrder);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 19);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 20);
            this.label2.TabIndex = 31;
            this.label2.Text = "Display Order";
            // 
            // PubChemSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 220);
            this.Controls.Add(this.DisplayOrder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ResultsPerCall);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.PubChemRestUri);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.PubChemWsUri);
            this.Controls.Add(this.SetDefaults);
            this.Controls.Add(this.Ok);
            this.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PubChemSettings";
            this.Text = "PubChem Search - Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_Settings);
            this.Load += new System.EventHandler(this.OnLoad_Settings);
            ((System.ComponentModel.ISupportInitialize)(this.ResultsPerCall)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DisplayOrder)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button SetDefaults;
        private System.Windows.Forms.Button Ok;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox PubChemRestUri;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox PubChemWsUri;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown ResultsPerCall;
        private System.Windows.Forms.NumericUpDown DisplayOrder;
        private System.Windows.Forms.Label label2;
    }
}