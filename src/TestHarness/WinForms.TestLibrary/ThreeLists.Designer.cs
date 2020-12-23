using WinForms.TestLibrary.Wpf;

namespace WinForms.TestLibrary
{
    partial class ThreeLists
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
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.LibraryHost = new System.Windows.Forms.Integration.ElementHost();
            this.libraryView1 = new WinForms.TestLibrary.Wpf.LibraryControl();
            this.CatalogueHost = new System.Windows.Forms.Integration.ElementHost();
            this._catalogueView1 = new WinForms.TestLibrary.Wpf.CatalogueControl();
            this.NavigatorHost = new System.Windows.Forms.Integration.ElementHost();
            this.navigatorView1 = new WinForms.TestLibrary.Wpf.NavigatorControl();
            this.LoadData = new System.Windows.Forms.Button();
            this.FindLastItem = new System.Windows.Forms.Button();
            this.LayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutPanel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.LayoutPanel.ColumnCount = 3;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.LayoutPanel.Controls.Add(this.LibraryHost, 0, 0);
            this.LayoutPanel.Controls.Add(this.CatalogueHost, 1, 0);
            this.LayoutPanel.Controls.Add(this.NavigatorHost, 2, 0);
            this.LayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 1;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.LayoutPanel.Size = new System.Drawing.Size(1314, 603);
            this.LayoutPanel.TabIndex = 0;
            // 
            // LibraryHost
            // 
            this.LibraryHost.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.LibraryHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LibraryHost.Location = new System.Drawing.Point(3, 3);
            this.LibraryHost.Name = "LibraryHost";
            this.LibraryHost.Size = new System.Drawing.Size(322, 597);
            this.LibraryHost.TabIndex = 0;
            this.LibraryHost.Text = "elementHost1";
            this.LibraryHost.Child = this.libraryView1;
            // 
            // CatalogueHost
            // 
            this.CatalogueHost.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.CatalogueHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CatalogueHost.Location = new System.Drawing.Point(331, 3);
            this.CatalogueHost.Name = "CatalogueHost";
            this.CatalogueHost.Size = new System.Drawing.Size(651, 597);
            this.CatalogueHost.TabIndex = 1;
            this.CatalogueHost.Text = "elementHost2";
            this.CatalogueHost.Child = this._catalogueView1;
            // 
            // NavigatorHost
            // 
            this.NavigatorHost.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.NavigatorHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NavigatorHost.Location = new System.Drawing.Point(988, 3);
            this.NavigatorHost.Name = "NavigatorHost";
            this.NavigatorHost.Size = new System.Drawing.Size(323, 597);
            this.NavigatorHost.TabIndex = 2;
            this.NavigatorHost.Text = "elementHost3";
            this.NavigatorHost.Child = this.navigatorView1;
            // 
            // LoadData
            // 
            this.LoadData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadData.Location = new System.Drawing.Point(15, 621);
            this.LoadData.Name = "LoadData";
            this.LoadData.Size = new System.Drawing.Size(75, 23);
            this.LoadData.TabIndex = 1;
            this.LoadData.Text = "Load Data";
            this.LoadData.UseVisualStyleBackColor = true;
            this.LoadData.Click += new System.EventHandler(this.LoadData_Click);
            // 
            // FindLastItem
            // 
            this.FindLastItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.FindLastItem.Location = new System.Drawing.Point(1248, 621);
            this.FindLastItem.Name = "FindLastItem";
            this.FindLastItem.Size = new System.Drawing.Size(75, 23);
            this.FindLastItem.TabIndex = 2;
            this.FindLastItem.Text = "Find";
            this.FindLastItem.UseVisualStyleBackColor = true;
            this.FindLastItem.Click += new System.EventHandler(this.FindLastItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1338, 656);
            this.Controls.Add(this.FindLastItem);
            this.Controls.Add(this.LoadData);
            this.Controls.Add(this.LayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CatalogueMode";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Testbed";
            this.LayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost LibraryHost;
        private System.Windows.Forms.Integration.ElementHost CatalogueHost;
        private System.Windows.Forms.Integration.ElementHost NavigatorHost;
        private LibraryControl libraryView1;
        private CatalogueControl _catalogueView1;
        private NavigatorControl navigatorView1;
        private System.Windows.Forms.Button LoadData;
        private System.Windows.Forms.Button FindLastItem;
    }
}

