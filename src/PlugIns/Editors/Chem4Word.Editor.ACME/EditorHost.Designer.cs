namespace Chem4Word.Editor.ACME
{
    partial class EditorHost
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorHost));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.editor1 = new Chem4Word.ACME.Editor();
            this.Buttons = new System.Windows.Forms.Panel();
            this.MessageFromWpf = new System.Windows.Forms.Label();
            this.Save = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.elementHost1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.Buttons);
            this.splitContainer1.Size = new System.Drawing.Size(1184, 761);
            this.splitContainer1.SplitterDistance = 673;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 2;
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(1184, 673);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.editor1;
            // 
            // Buttons
            // 
            this.Buttons.Controls.Add(this.MessageFromWpf);
            this.Buttons.Controls.Add(this.Save);
            this.Buttons.Controls.Add(this.Cancel);
            this.Buttons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Buttons.Location = new System.Drawing.Point(0, 0);
            this.Buttons.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.Buttons.Name = "Buttons";
            this.Buttons.Size = new System.Drawing.Size(1184, 82);
            this.Buttons.TabIndex = 3;
            // 
            // MessageFromWpf
            // 
            this.MessageFromWpf.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageFromWpf.AutoEllipsis = true;
            this.MessageFromWpf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageFromWpf.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.MessageFromWpf.Location = new System.Drawing.Point(12, 40);
            this.MessageFromWpf.Name = "MessageFromWpf";
            this.MessageFromWpf.Size = new System.Drawing.Size(988, 27);
            this.MessageFromWpf.TabIndex = 4;
            this.MessageFromWpf.Text = "...";
            // 
            // Save
            // 
            this.Save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Save.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Save.Location = new System.Drawing.Point(1016, 40);
            this.Save.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.Save.Name = "Save";
            this.Save.Size = new System.Drawing.Size(75, 27);
            this.Save.TabIndex = 1;
            this.Save.Text = "OK";
            this.Save.UseVisualStyleBackColor = true;
            this.Save.Click += new System.EventHandler(this.Save_Click);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Cancel.Location = new System.Drawing.Point(1097, 40);
            this.Cancel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 27);
            this.Cancel.TabIndex = 0;
            this.Cancel.Text = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // EditorHost
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 761);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "EditorHost";
            this.Text = "ACME";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorHost_FormClosing);
            this.Load += new System.EventHandler(this.EditorHost_Load);
            this.LocationChanged += new System.EventHandler(this.EditorHost_LocationChanged);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.Buttons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private System.Windows.Forms.Panel Buttons;
        private System.Windows.Forms.Button Save;
        private System.Windows.Forms.Button Cancel;
        private Chem4Word.ACME.Editor editor1;
        private System.Windows.Forms.Label MessageFromWpf;
    }
}