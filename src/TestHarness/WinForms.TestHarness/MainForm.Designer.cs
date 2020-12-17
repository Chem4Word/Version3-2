namespace WinForms.TestHarness
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.LoadStructure = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.EditWithAcme = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.Undo = new System.Windows.Forms.Button();
            this.Redo = new System.Windows.Forms.Button();
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.DisplayHost = new System.Windows.Forms.Integration.ElementHost();
            this.Display = new Chem4Word.ACME.Display();
            this.RedoHost = new System.Windows.Forms.Integration.ElementHost();
            this.RedoStack = new WinForms.TestHarness.StackViewer();
            this.UndoHost = new System.Windows.Forms.Integration.ElementHost();
            this.UndoStack = new WinForms.TestHarness.StackViewer();
            this.Information = new System.Windows.Forms.Label();
            this.EditCml = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.EditLabels = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ChangeOoXmlSettings = new System.Windows.Forms.Button();
            this.ChangeAcmeSettings = new System.Windows.Forms.Button();
            this.ShowCml = new System.Windows.Forms.Button();
            this.SaveStructure = new System.Windows.Forms.Button();
            this.ClearChemistry = new System.Windows.Forms.Button();
            this.LayoutStructure = new System.Windows.Forms.Button();
            this.RenderOoXml = new System.Windows.Forms.Button();
            this.SearchOpsin = new System.Windows.Forms.Button();
            this.SearchPubChem = new System.Windows.Forms.Button();
            this.SearchChEBI = new System.Windows.Forms.Button();
            this.LayoutPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadStructure
            // 
            this.LoadStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadStructure.Location = new System.Drawing.Point(12, 501);
            this.LoadStructure.Name = "LoadStructure";
            this.LoadStructure.Size = new System.Drawing.Size(75, 23);
            this.LoadStructure.TabIndex = 0;
            this.LoadStructure.Text = "Load";
            this.LoadStructure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LoadStructure.UseVisualStyleBackColor = true;
            this.LoadStructure.Click += new System.EventHandler(this.LoadStructure_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // EditWithAcme
            // 
            this.EditWithAcme.Location = new System.Drawing.Point(6, 19);
            this.EditWithAcme.Name = "EditWithAcme";
            this.EditWithAcme.Size = new System.Drawing.Size(75, 23);
            this.EditWithAcme.TabIndex = 2;
            this.EditWithAcme.Text = "ACME";
            this.EditWithAcme.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditWithAcme.UseVisualStyleBackColor = true;
            this.EditWithAcme.Click += new System.EventHandler(this.EditWithAcme_Click);
            // 
            // Undo
            // 
            this.Undo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Undo.Enabled = false;
            this.Undo.Location = new System.Drawing.Point(174, 501);
            this.Undo.Name = "Undo";
            this.Undo.Size = new System.Drawing.Size(75, 23);
            this.Undo.TabIndex = 11;
            this.Undo.Text = "Undo";
            this.Undo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Undo.UseVisualStyleBackColor = true;
            this.Undo.Click += new System.EventHandler(this.Undo_Click);
            // 
            // Redo
            // 
            this.Redo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Redo.Enabled = false;
            this.Redo.Location = new System.Drawing.Point(174, 530);
            this.Redo.Name = "Redo";
            this.Redo.Size = new System.Drawing.Size(75, 23);
            this.Redo.TabIndex = 12;
            this.Redo.Text = "Redo";
            this.Redo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Redo.UseVisualStyleBackColor = true;
            this.Redo.Click += new System.EventHandler(this.Redo_Click);
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutPanel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.LayoutPanel.ColumnCount = 3;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 275F));
            this.LayoutPanel.Controls.Add(this.DisplayHost, 1, 0);
            this.LayoutPanel.Controls.Add(this.RedoHost, 2, 0);
            this.LayoutPanel.Controls.Add(this.UndoHost, 0, 0);
            this.LayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.RowCount = 1;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.LayoutPanel.Size = new System.Drawing.Size(1113, 462);
            this.LayoutPanel.TabIndex = 13;
            // 
            // DisplayHost
            // 
            this.DisplayHost.BackColor = System.Drawing.Color.White;
            this.DisplayHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DisplayHost.Location = new System.Drawing.Point(278, 3);
            this.DisplayHost.Name = "DisplayHost";
            this.DisplayHost.Size = new System.Drawing.Size(557, 456);
            this.DisplayHost.TabIndex = 1;
            this.DisplayHost.Text = "centreHost";
            this.DisplayHost.Child = this.Display;
            // 
            // RedoHost
            // 
            this.RedoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RedoHost.Location = new System.Drawing.Point(841, 3);
            this.RedoHost.Name = "RedoHost";
            this.RedoHost.Size = new System.Drawing.Size(269, 456);
            this.RedoHost.TabIndex = 2;
            this.RedoHost.Text = "rightHost";
            this.RedoHost.Child = this.RedoStack;
            // 
            // UndoHost
            // 
            this.UndoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UndoHost.Location = new System.Drawing.Point(3, 3);
            this.UndoHost.Name = "UndoHost";
            this.UndoHost.Size = new System.Drawing.Size(269, 456);
            this.UndoHost.TabIndex = 3;
            this.UndoHost.Text = "leftHost";
            this.UndoHost.Child = this.UndoStack;
            // 
            // Information
            // 
            this.Information.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Information.AutoSize = true;
            this.Information.Location = new System.Drawing.Point(12, 477);
            this.Information.Name = "Information";
            this.Information.Size = new System.Drawing.Size(16, 13);
            this.Information.TabIndex = 14;
            this.Information.Text = "...";
            // 
            // EditCml
            // 
            this.EditCml.Enabled = false;
            this.EditCml.Location = new System.Drawing.Point(6, 77);
            this.EditCml.Name = "EditCml";
            this.EditCml.Size = new System.Drawing.Size(75, 23);
            this.EditCml.TabIndex = 15;
            this.EditCml.Text = "CML";
            this.EditCml.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditCml.UseVisualStyleBackColor = true;
            this.EditCml.Click += new System.EventHandler(this.EditCml_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.SearchOpsin);
            this.groupBox1.Controls.Add(this.EditLabels);
            this.groupBox1.Controls.Add(this.SearchChEBI);
            this.groupBox1.Controls.Add(this.SearchPubChem);
            this.groupBox1.Controls.Add(this.EditWithAcme);
            this.groupBox1.Controls.Add(this.EditCml);
            this.groupBox1.Location = new System.Drawing.Point(952, 477);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(173, 105);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edit / Search ...";
            // 
            // EditLabels
            // 
            this.EditLabels.Enabled = false;
            this.EditLabels.Location = new System.Drawing.Point(6, 48);
            this.EditLabels.Name = "EditLabels";
            this.EditLabels.Size = new System.Drawing.Size(75, 23);
            this.EditLabels.TabIndex = 16;
            this.EditLabels.Text = "Labels";
            this.EditLabels.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.EditLabels.UseVisualStyleBackColor = true;
            this.EditLabels.Click += new System.EventHandler(this.EditLabels_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.ChangeOoXmlSettings);
            this.groupBox2.Controls.Add(this.ChangeAcmeSettings);
            this.groupBox2.Location = new System.Drawing.Point(255, 501);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(108, 81);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Settings ...";
            // 
            // ChangeOoXmlSettings
            // 
            this.ChangeOoXmlSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ChangeOoXmlSettings.Location = new System.Drawing.Point(15, 48);
            this.ChangeOoXmlSettings.Name = "ChangeOoXmlSettings";
            this.ChangeOoXmlSettings.Size = new System.Drawing.Size(75, 23);
            this.ChangeOoXmlSettings.TabIndex = 13;
            this.ChangeOoXmlSettings.Text = "OoXml";
            this.ChangeOoXmlSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ChangeOoXmlSettings.UseVisualStyleBackColor = true;
            this.ChangeOoXmlSettings.Click += new System.EventHandler(this.ChangeOoXmlSettings_Click);
            // 
            // ChangeAcmeSettings
            // 
            this.ChangeAcmeSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ChangeAcmeSettings.Location = new System.Drawing.Point(15, 19);
            this.ChangeAcmeSettings.Name = "ChangeAcmeSettings";
            this.ChangeAcmeSettings.Size = new System.Drawing.Size(75, 23);
            this.ChangeAcmeSettings.TabIndex = 12;
            this.ChangeAcmeSettings.Text = "ACME";
            this.ChangeAcmeSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ChangeAcmeSettings.UseVisualStyleBackColor = true;
            this.ChangeAcmeSettings.Click += new System.EventHandler(this.ChangeAcmeSettings_Click);
            // 
            // ShowCml
            // 
            this.ShowCml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowCml.Enabled = false;
            this.ShowCml.Location = new System.Drawing.Point(174, 559);
            this.ShowCml.Name = "ShowCml";
            this.ShowCml.Size = new System.Drawing.Size(75, 23);
            this.ShowCml.TabIndex = 18;
            this.ShowCml.Text = "Show CML";
            this.ShowCml.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ShowCml.UseVisualStyleBackColor = true;
            this.ShowCml.Click += new System.EventHandler(this.ShowCml_Click);
            // 
            // SaveStructure
            // 
            this.SaveStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SaveStructure.Enabled = false;
            this.SaveStructure.Location = new System.Drawing.Point(12, 530);
            this.SaveStructure.Name = "SaveStructure";
            this.SaveStructure.Size = new System.Drawing.Size(75, 23);
            this.SaveStructure.TabIndex = 19;
            this.SaveStructure.Text = "Save ...";
            this.SaveStructure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.SaveStructure.UseVisualStyleBackColor = true;
            this.SaveStructure.Click += new System.EventHandler(this.SaveStructure_Click);
            // 
            // ClearChemistry
            // 
            this.ClearChemistry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ClearChemistry.Enabled = false;
            this.ClearChemistry.Location = new System.Drawing.Point(12, 559);
            this.ClearChemistry.Name = "ClearChemistry";
            this.ClearChemistry.Size = new System.Drawing.Size(75, 23);
            this.ClearChemistry.TabIndex = 0;
            this.ClearChemistry.Text = "Clear";
            this.ClearChemistry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ClearChemistry.Click += new System.EventHandler(this.ClearChemistry_Click);
            // 
            // LayoutStructure
            // 
            this.LayoutStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LayoutStructure.Enabled = false;
            this.LayoutStructure.Location = new System.Drawing.Point(93, 501);
            this.LayoutStructure.Name = "LayoutStructure";
            this.LayoutStructure.Size = new System.Drawing.Size(75, 23);
            this.LayoutStructure.TabIndex = 20;
            this.LayoutStructure.Text = "Layout (ws)";
            this.LayoutStructure.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LayoutStructure.UseVisualStyleBackColor = true;
            this.LayoutStructure.Click += new System.EventHandler(this.LayoutStructure_Click);
            // 
            // RenderOoXml
            // 
            this.RenderOoXml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RenderOoXml.Enabled = false;
            this.RenderOoXml.Location = new System.Drawing.Point(93, 530);
            this.RenderOoXml.Name = "RenderOoXml";
            this.RenderOoXml.Size = new System.Drawing.Size(75, 23);
            this.RenderOoXml.TabIndex = 21;
            this.RenderOoXml.Text = "OoXml (raw)";
            this.RenderOoXml.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RenderOoXml.UseVisualStyleBackColor = true;
            this.RenderOoXml.Click += new System.EventHandler(this.RenderOoXml_Click);
            // 
            // SearchOpsin
            // 
            this.SearchOpsin.Location = new System.Drawing.Point(87, 77);
            this.SearchOpsin.Name = "SearchOpsin";
            this.SearchOpsin.Size = new System.Drawing.Size(75, 23);
            this.SearchOpsin.TabIndex = 5;
            this.SearchOpsin.Text = "OPSIN";
            this.SearchOpsin.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchOpsin.UseVisualStyleBackColor = true;
            this.SearchOpsin.Click += new System.EventHandler(this.SearchOpsin_Click);
            // 
            // SearchPubChem
            // 
            this.SearchPubChem.Location = new System.Drawing.Point(87, 19);
            this.SearchPubChem.Name = "SearchPubChem";
            this.SearchPubChem.Size = new System.Drawing.Size(75, 23);
            this.SearchPubChem.TabIndex = 4;
            this.SearchPubChem.Text = "PubChem";
            this.SearchPubChem.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchPubChem.UseVisualStyleBackColor = true;
            this.SearchPubChem.Click += new System.EventHandler(this.SearchPubChem_Click);
            // 
            // SearchChEBI
            // 
            this.SearchChEBI.Location = new System.Drawing.Point(87, 48);
            this.SearchChEBI.Name = "SearchChEBI";
            this.SearchChEBI.Size = new System.Drawing.Size(75, 23);
            this.SearchChEBI.TabIndex = 3;
            this.SearchChEBI.Text = "ChEBI";
            this.SearchChEBI.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.SearchChEBI.UseVisualStyleBackColor = true;
            this.SearchChEBI.Click += new System.EventHandler(this.SearchChEBI_Click);
            // 
            // FlexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1137, 587);
            this.Controls.Add(this.RenderOoXml);
            this.Controls.Add(this.LayoutStructure);
            this.Controls.Add(this.ClearChemistry);
            this.Controls.Add(this.SaveStructure);
            this.Controls.Add(this.ShowCml);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Information);
            this.Controls.Add(this.LayoutPanel);
            this.Controls.Add(this.Redo);
            this.Controls.Add(this.Undo);
            this.Controls.Add(this.LoadStructure);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FlexForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ACME Test Bed";
            this.Load += new System.EventHandler(this.FlexForm_Load);
            this.LayoutPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoadStructure;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Integration.ElementHost DisplayHost;
        private System.Windows.Forms.Button EditWithAcme;
        private Chem4Word.ACME.Display Display;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button Undo;
        private System.Windows.Forms.Button Redo;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
        private System.Windows.Forms.Integration.ElementHost RedoHost;
        private System.Windows.Forms.Integration.ElementHost UndoHost;
        private System.Windows.Forms.Label Information;
        private System.Windows.Forms.Button EditCml;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button ShowCml;
        private System.Windows.Forms.Button SaveStructure;
        private System.Windows.Forms.Button ClearChemistry;
        private System.Windows.Forms.Button EditLabels;
        private StackViewer UndoStack;
        private StackViewer RedoStack;
        private System.Windows.Forms.Button ChangeAcmeSettings;
        private System.Windows.Forms.Button LayoutStructure;
        private System.Windows.Forms.Button RenderOoXml;
        private System.Windows.Forms.Button ChangeOoXmlSettings;
        private System.Windows.Forms.Button SearchChEBI;
        private System.Windows.Forms.Button SearchOpsin;
        private System.Windows.Forms.Button SearchPubChem;
    }
}

