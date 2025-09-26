namespace Chem4Word.Searcher.ChEBIPlugin
{
    partial class ChEBISettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChEBISettings));
            this.DisplayOrder = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.ResultsPerCall = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ChebiWsUri = new System.Windows.Forms.TextBox();
            this.SetDefaults = new System.Windows.Forms.Button();
            this.Ok = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.DisplayOrder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ResultsPerCall)).BeginInit();
            this.SuspendLayout();
            // 
            // DisplayOrder
            // 
            this.DisplayOrder.Location = new System.Drawing.Point(144, 13);
            this.DisplayOrder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DisplayOrder.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.DisplayOrder.Name = "DisplayOrder";
            this.DisplayOrder.Size = new System.Drawing.Size(71, 23);
            this.DisplayOrder.TabIndex = 42;
            this.DisplayOrder.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.DisplayOrder.ValueChanged += new System.EventHandler(this.OnValueChanged_DisplayOrder);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 15);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 16);
            this.label2.TabIndex = 41;
            this.label2.Text = "Display Order";
            // 
            // ResultsPerCall
            // 
            this.ResultsPerCall.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.ResultsPerCall.Location = new System.Drawing.Point(144, 77);
            this.ResultsPerCall.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ResultsPerCall.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.ResultsPerCall.Name = "ResultsPerCall";
            this.ResultsPerCall.Size = new System.Drawing.Size(71, 23);
            this.ResultsPerCall.TabIndex = 40;
            this.ResultsPerCall.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ResultsPerCall.ValueChanged += new System.EventHandler(this.OnValueChanged_ResultsPerCall);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 79);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 16);
            this.label1.TabIndex = 39;
            this.label1.Text = "Maximum Results";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 48);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 16);
            this.label3.TabIndex = 36;
            this.label3.Text = "WebService Url";
            // 
            // ChebiWsUri
            // 
            this.ChebiWsUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ChebiWsUri.Location = new System.Drawing.Point(144, 45);
            this.ChebiWsUri.Margin = new System.Windows.Forms.Padding(5);
            this.ChebiWsUri.Name = "ChebiWsUri";
            this.ChebiWsUri.Size = new System.Drawing.Size(370, 23);
            this.ChebiWsUri.TabIndex = 35;
            this.ChebiWsUri.Text = "https://www.ebi.ac.uk/chebi/";
            this.ChebiWsUri.TextChanged += new System.EventHandler(this.OnTextChanged_ChebiWsUri);
            // 
            // SetDefaults
            // 
            this.SetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SetDefaults.Location = new System.Drawing.Point(297, 110);
            this.SetDefaults.Margin = new System.Windows.Forms.Padding(6);
            this.SetDefaults.Name = "SetDefaults";
            this.SetDefaults.Size = new System.Drawing.Size(103, 34);
            this.SetDefaults.TabIndex = 34;
            this.SetDefaults.Text = "Defaults";
            this.SetDefaults.UseVisualStyleBackColor = true;
            this.SetDefaults.Click += new System.EventHandler(this.OnClick_SetDefaults);
            // 
            // Ok
            // 
            this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Ok.Location = new System.Drawing.Point(412, 110);
            this.Ok.Margin = new System.Windows.Forms.Padding(6);
            this.Ok.Name = "Ok";
            this.Ok.Size = new System.Drawing.Size(103, 34);
            this.Ok.TabIndex = 33;
            this.Ok.Text = "OK";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.OnClick_Ok);
            // 
            // ChEBISettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(528, 159);
            this.Controls.Add(this.DisplayOrder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ResultsPerCall);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ChebiWsUri);
            this.Controls.Add(this.SetDefaults);
            this.Controls.Add(this.Ok);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ChEBISettings";
            this.Text = "ChEBI Search - Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing_Settings);
            this.Load += new System.EventHandler(this.OnLoad_Settings);
            ((System.ComponentModel.ISupportInitialize)(this.DisplayOrder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ResultsPerCall)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown DisplayOrder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown ResultsPerCall;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox ChebiWsUri;
        private System.Windows.Forms.Button SetDefaults;
        private System.Windows.Forms.Button Ok;
    }
}
