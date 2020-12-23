using WinForms.TestLibrary.Wpf;

namespace WinForms.TestLibrary
{
    partial class CatalogueMode
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CatalogueMode));
            this.LoadData = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.CatalogueHost = new System.Windows.Forms.Integration.ElementHost();
            this.catalogueControl1 = new WinForms.TestLibrary.Wpf.CatalogueControl();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadData
            // 
            this.LoadData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadData.Location = new System.Drawing.Point(15, 676);
            this.LoadData.Name = "LoadData";
            this.LoadData.Size = new System.Drawing.Size(75, 23);
            this.LoadData.TabIndex = 1;
            this.LoadData.Text = "Load Data";
            this.LoadData.UseVisualStyleBackColor = true;
            this.LoadData.Click += new System.EventHandler(this.LoadData_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.CatalogueHost);
            this.panel1.Location = new System.Drawing.Point(15, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1157, 658);
            this.panel1.TabIndex = 3;
            // 
            // CatalogueHost
            // 
            this.CatalogueHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CatalogueHost.Location = new System.Drawing.Point(0, 0);
            this.CatalogueHost.Name = "CatalogueHost";
            this.CatalogueHost.Size = new System.Drawing.Size(1155, 656);
            this.CatalogueHost.TabIndex = 2;
            this.CatalogueHost.Text = "elementHost1";
            this.CatalogueHost.Child = this.catalogueControl1;
            // 
            // CatalogueMode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 711);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.LoadData);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CatalogueMode";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Testbed";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button LoadData;
        private System.Windows.Forms.Integration.ElementHost CatalogueHost;
        private System.Windows.Forms.Panel panel1;
        private CatalogueControl catalogueControl1;
    }
}

