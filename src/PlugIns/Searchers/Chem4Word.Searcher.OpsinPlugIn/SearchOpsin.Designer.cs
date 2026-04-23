namespace Chem4Word.Searcher.OpsinPlugIn
{
    partial class SearchOpsin
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchOpsin));
            this.SearchFor = new System.Windows.Forms.TextBox();
            this.SearchButton = new System.Windows.Forms.Button();
            this.ImportButton = new System.Windows.Forms.Button();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.display1 = new Chem4Word.ACME.Display();
            this.LabelInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SearchFor
            // 
            this.SearchFor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchFor.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.SearchFor.Location = new System.Drawing.Point(17, 16);
            this.SearchFor.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.SearchFor.Name = "SearchFor";
            this.SearchFor.Size = new System.Drawing.Size(462, 22);
            this.SearchFor.TabIndex = 0;
            this.SearchFor.TextChanged += new System.EventHandler(this.OnTextChanged_SearchFor);
            // 
            // SearchButton
            // 
            this.SearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchButton.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.SearchButton.Location = new System.Drawing.Point(485, 13);
            this.SearchButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.SearchButton.Name = "SearchButton";
            this.SearchButton.Size = new System.Drawing.Size(87, 29);
            this.SearchButton.TabIndex = 3;
            this.SearchButton.Text = "Search";
            this.SearchButton.UseVisualStyleBackColor = true;
            this.SearchButton.Click += new System.EventHandler(this.OnClick_SearchButton);
            // 
            // ImportButton
            // 
            this.ImportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportButton.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ImportButton.Location = new System.Drawing.Point(485, 514);
            this.ImportButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ImportButton.Name = "ImportButton";
            this.ImportButton.Size = new System.Drawing.Size(87, 29);
            this.ImportButton.TabIndex = 5;
            this.ImportButton.Text = "Import";
            this.ImportButton.UseVisualStyleBackColor = true;
            this.ImportButton.Click += new System.EventHandler(this.OnClick_ImportButton);
            // 
            // elementHost1
            // 
            this.elementHost1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.elementHost1.BackColor = System.Drawing.Color.White;
            this.elementHost1.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.elementHost1.Location = new System.Drawing.Point(14, 54);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(558, 447);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.display1;
            // 
            // LabelInfo
            // 
            this.LabelInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LabelInfo.AutoSize = true;
            this.LabelInfo.Location = new System.Drawing.Point(14, 520);
            this.LabelInfo.Name = "LabelInfo";
            this.LabelInfo.Size = new System.Drawing.Size(19, 16);
            this.LabelInfo.TabIndex = 9;
            this.LabelInfo.Text = "...";
            // 
            // SearchOpsin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 561);
            this.Controls.Add(this.LabelInfo);
            this.Controls.Add(this.ImportButton);
            this.Controls.Add(this.SearchFor);
            this.Controls.Add(this.SearchButton);
            this.Controls.Add(this.elementHost1);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "SearchOpsin";
            this.Text = "Search Opsin public database";
            this.Load += new System.EventHandler(this.OnLoad_SearchOpsin);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private ACME.Display display1;
        private System.Windows.Forms.TextBox SearchFor;
        private System.Windows.Forms.Button SearchButton;
        private System.Windows.Forms.Button ImportButton;
        private System.Windows.Forms.Label LabelInfo;
    }
}