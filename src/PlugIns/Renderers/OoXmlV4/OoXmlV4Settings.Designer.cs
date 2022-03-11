using Chem4Word.Core.UI.Controls;

namespace Chem4Word.Renderer.OoXmlV4
{
    partial class OoXmlV4Settings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OoXmlV4Settings));
            this.tabControlEx = new Chem4Word.Core.UI.Controls.TabControlEx();
            this.tabRendering = new System.Windows.Forms.TabPage();
            this.ClipCrossingBonds = new System.Windows.Forms.CheckBox();
            this.ShowAllCarbonAtoms = new System.Windows.Forms.CheckBox();
            this.ShowMoleculeCaptions = new System.Windows.Forms.CheckBox();
            this.ShowMoleculeGrouping = new System.Windows.Forms.CheckBox();
            this.ShowHydrogens = new System.Windows.Forms.CheckBox();
            this.ColouredAtoms = new System.Windows.Forms.CheckBox();
            this.tabDebug = new System.Windows.Forms.TabPage();
            this.ShowBondCrossingPoints = new System.Windows.Forms.CheckBox();
            this.ShowCharacterGroupsBox = new System.Windows.Forms.CheckBox();
            this.ShowBondDirection = new System.Windows.Forms.CheckBox();
            this.ShowDoubleBondTrimmingLines = new System.Windows.Forms.CheckBox();
            this.ShowConvexHulls = new System.Windows.Forms.CheckBox();
            this.ShowAtomPositions = new System.Windows.Forms.CheckBox();
            this.ShowRingCentres = new System.Windows.Forms.CheckBox();
            this.ShowCharacterBox = new System.Windows.Forms.CheckBox();
            this.ShowMoleculeBox = new System.Windows.Forms.CheckBox();
            this.ClipBondLines = new System.Windows.Forms.CheckBox();
            this.btnSetDefaults = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tabControlEx.SuspendLayout();
            this.tabRendering.SuspendLayout();
            this.tabDebug.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlEx
            // 
            this.tabControlEx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlEx.Controls.Add(this.tabRendering);
            this.tabControlEx.Controls.Add(this.tabDebug);
            this.tabControlEx.Location = new System.Drawing.Point(13, 13);
            this.tabControlEx.Name = "tabControlEx";
            this.tabControlEx.SelectedIndex = 0;
            this.tabControlEx.Size = new System.Drawing.Size(459, 189);
            this.tabControlEx.TabIndex = 0;
            // 
            // tabRendering
            // 
            this.tabRendering.BackColor = System.Drawing.SystemColors.Control;
            this.tabRendering.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tabRendering.Controls.Add(this.ClipCrossingBonds);
            this.tabRendering.Controls.Add(this.ShowAllCarbonAtoms);
            this.tabRendering.Controls.Add(this.ShowMoleculeCaptions);
            this.tabRendering.Controls.Add(this.ShowMoleculeGrouping);
            this.tabRendering.Controls.Add(this.ShowHydrogens);
            this.tabRendering.Controls.Add(this.ColouredAtoms);
            this.tabRendering.Location = new System.Drawing.Point(0, 20);
            this.tabRendering.Name = "tabRendering";
            this.tabRendering.Padding = new System.Windows.Forms.Padding(3);
            this.tabRendering.Size = new System.Drawing.Size(459, 169);
            this.tabRendering.TabIndex = 0;
            this.tabRendering.Text = "Rendering";
            // 
            // ClipCrossingBonds
            // 
            this.ClipCrossingBonds.AutoSize = true;
            this.ClipCrossingBonds.Checked = true;
            this.ClipCrossingBonds.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ClipCrossingBonds.Location = new System.Drawing.Point(12, 87);
            this.ClipCrossingBonds.Margin = new System.Windows.Forms.Padding(4);
            this.ClipCrossingBonds.Name = "ClipCrossingBonds";
            this.ClipCrossingBonds.Size = new System.Drawing.Size(142, 17);
            this.ClipCrossingBonds.TabIndex = 26;
            this.ClipCrossingBonds.Text = "Clip Crossing Bond Lines";
            this.ClipCrossingBonds.UseVisualStyleBackColor = true;
            this.ClipCrossingBonds.CheckedChanged += new System.EventHandler(this.ClipCrossingBonds_CheckedChanged);
            // 
            // ShowAllCarbonAtoms
            // 
            this.ShowAllCarbonAtoms.AutoSize = true;
            this.ShowAllCarbonAtoms.Checked = true;
            this.ShowAllCarbonAtoms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowAllCarbonAtoms.Location = new System.Drawing.Point(12, 62);
            this.ShowAllCarbonAtoms.Margin = new System.Windows.Forms.Padding(4);
            this.ShowAllCarbonAtoms.Name = "ShowAllCarbonAtoms";
            this.ShowAllCarbonAtoms.Size = new System.Drawing.Size(165, 17);
            this.ShowAllCarbonAtoms.TabIndex = 25;
            this.ShowAllCarbonAtoms.Text = "Show All Carbon Atom Labels";
            this.ShowAllCarbonAtoms.UseVisualStyleBackColor = true;
            this.ShowAllCarbonAtoms.CheckedChanged += new System.EventHandler(this.ShowAllCarbonAtoms_CheckedChanged);
            // 
            // ShowMoleculeCaptions
            // 
            this.ShowMoleculeCaptions.AutoSize = true;
            this.ShowMoleculeCaptions.Checked = true;
            this.ShowMoleculeCaptions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowMoleculeCaptions.Location = new System.Drawing.Point(12, 137);
            this.ShowMoleculeCaptions.Margin = new System.Windows.Forms.Padding(4);
            this.ShowMoleculeCaptions.Name = "ShowMoleculeCaptions";
            this.ShowMoleculeCaptions.Size = new System.Drawing.Size(143, 17);
            this.ShowMoleculeCaptions.TabIndex = 23;
            this.ShowMoleculeCaptions.Text = "Show Molecule Captions";
            this.ShowMoleculeCaptions.UseVisualStyleBackColor = true;
            this.ShowMoleculeCaptions.CheckedChanged += new System.EventHandler(this.ShowMoleculeCaptions_CheckedChanged);
            // 
            // ShowMoleculeGrouping
            // 
            this.ShowMoleculeGrouping.AutoSize = true;
            this.ShowMoleculeGrouping.Checked = true;
            this.ShowMoleculeGrouping.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowMoleculeGrouping.Location = new System.Drawing.Point(12, 112);
            this.ShowMoleculeGrouping.Margin = new System.Windows.Forms.Padding(4);
            this.ShowMoleculeGrouping.Name = "ShowMoleculeGrouping";
            this.ShowMoleculeGrouping.Size = new System.Drawing.Size(136, 17);
            this.ShowMoleculeGrouping.TabIndex = 24;
            this.ShowMoleculeGrouping.Text = "Show Molecule Groups";
            this.ShowMoleculeGrouping.UseVisualStyleBackColor = true;
            this.ShowMoleculeGrouping.CheckedChanged += new System.EventHandler(this.ShowMoleculeGrouping_CheckedChanged);
            // 
            // ShowHydrogens
            // 
            this.ShowHydrogens.AutoSize = true;
            this.ShowHydrogens.Checked = true;
            this.ShowHydrogens.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowHydrogens.Location = new System.Drawing.Point(12, 12);
            this.ShowHydrogens.Margin = new System.Windows.Forms.Padding(4);
            this.ShowHydrogens.Name = "ShowHydrogens";
            this.ShowHydrogens.Size = new System.Drawing.Size(142, 17);
            this.ShowHydrogens.TabIndex = 7;
            this.ShowHydrogens.Text = "Show Implicit Hydrogens";
            this.ShowHydrogens.UseVisualStyleBackColor = true;
            this.ShowHydrogens.CheckedChanged += new System.EventHandler(this.ShowHydrogens_CheckedChanged);
            // 
            // ColouredAtoms
            // 
            this.ColouredAtoms.AutoSize = true;
            this.ColouredAtoms.Checked = true;
            this.ColouredAtoms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ColouredAtoms.Location = new System.Drawing.Point(12, 37);
            this.ColouredAtoms.Margin = new System.Windows.Forms.Padding(4);
            this.ColouredAtoms.Name = "ColouredAtoms";
            this.ColouredAtoms.Size = new System.Drawing.Size(158, 17);
            this.ColouredAtoms.TabIndex = 8;
            this.ColouredAtoms.Text = "Show Atom Labels in Colour";
            this.ColouredAtoms.UseVisualStyleBackColor = true;
            this.ColouredAtoms.CheckedChanged += new System.EventHandler(this.ColouredAtoms_CheckedChanged);
            // 
            // tabDebug
            // 
            this.tabDebug.BackColor = System.Drawing.SystemColors.Control;
            this.tabDebug.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tabDebug.Controls.Add(this.ShowBondCrossingPoints);
            this.tabDebug.Controls.Add(this.ShowCharacterGroupsBox);
            this.tabDebug.Controls.Add(this.ShowBondDirection);
            this.tabDebug.Controls.Add(this.ShowDoubleBondTrimmingLines);
            this.tabDebug.Controls.Add(this.ShowConvexHulls);
            this.tabDebug.Controls.Add(this.ShowAtomPositions);
            this.tabDebug.Controls.Add(this.ShowRingCentres);
            this.tabDebug.Controls.Add(this.ShowCharacterBox);
            this.tabDebug.Controls.Add(this.ShowMoleculeBox);
            this.tabDebug.Controls.Add(this.ClipBondLines);
            this.tabDebug.Location = new System.Drawing.Point(0, 20);
            this.tabDebug.Name = "tabDebug";
            this.tabDebug.Padding = new System.Windows.Forms.Padding(3);
            this.tabDebug.Size = new System.Drawing.Size(459, 169);
            this.tabDebug.TabIndex = 1;
            this.tabDebug.Text = "Debug";
            // 
            // ShowBondClippingPoints
            // 
            this.ShowBondCrossingPoints.AutoSize = true;
            this.ShowBondCrossingPoints.Checked = true;
            this.ShowBondCrossingPoints.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowBondCrossingPoints.Location = new System.Drawing.Point(246, 90);
            this.ShowBondCrossingPoints.Margin = new System.Windows.Forms.Padding(4);
            this.ShowBondCrossingPoints.Name = "ShowBondCrossingPoints";
            this.ShowBondCrossingPoints.Size = new System.Drawing.Size(193, 17);
            this.ShowBondCrossingPoints.TabIndex = 26;
            this.ShowBondCrossingPoints.Text = "Show crossing bond clipping Points";
            this.ShowBondCrossingPoints.UseVisualStyleBackColor = true;
            this.ShowBondCrossingPoints.CheckedChanged += new System.EventHandler(this.ShowBondCrossingPoints_CheckedChanged);
            // 
            // ShowCharacterGroupsBox
            // 
            this.ShowCharacterGroupsBox.AutoSize = true;
            this.ShowCharacterGroupsBox.Checked = true;
            this.ShowCharacterGroupsBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowCharacterGroupsBox.Location = new System.Drawing.Point(7, 90);
            this.ShowCharacterGroupsBox.Margin = new System.Windows.Forms.Padding(4);
            this.ShowCharacterGroupsBox.Name = "ShowCharacterGroupsBox";
            this.ShowCharacterGroupsBox.Size = new System.Drawing.Size(214, 17);
            this.ShowCharacterGroupsBox.TabIndex = 25;
            this.ShowCharacterGroupsBox.Text = "Show BoundingBox of character groups";
            this.ShowCharacterGroupsBox.UseVisualStyleBackColor = true;
            this.ShowCharacterGroupsBox.CheckedChanged += new System.EventHandler(this.ShowCharacterGroupsBox_CheckedChanged);
            // 
            // ShowBondDirection
            // 
            this.ShowBondDirection.AutoSize = true;
            this.ShowBondDirection.Checked = true;
            this.ShowBondDirection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowBondDirection.Location = new System.Drawing.Point(246, 64);
            this.ShowBondDirection.Margin = new System.Windows.Forms.Padding(4);
            this.ShowBondDirection.Name = "ShowBondDirection";
            this.ShowBondDirection.Size = new System.Drawing.Size(123, 17);
            this.ShowBondDirection.TabIndex = 24;
            this.ShowBondDirection.Text = "Show bond direction";
            this.ShowBondDirection.UseVisualStyleBackColor = true;
            this.ShowBondDirection.CheckedChanged += new System.EventHandler(this.ShowBondDirection_CheckedChanged);
            // 
            // ShowBondClippingLines
            // 
            this.ShowDoubleBondTrimmingLines.AutoSize = true;
            this.ShowDoubleBondTrimmingLines.Checked = true;
            this.ShowDoubleBondTrimmingLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowDoubleBondTrimmingLines.Location = new System.Drawing.Point(246, 38);
            this.ShowDoubleBondTrimmingLines.Margin = new System.Windows.Forms.Padding(4);
            this.ShowDoubleBondTrimmingLines.Name = "ShowDoubleBondTrimmingLines";
            this.ShowDoubleBondTrimmingLines.Size = new System.Drawing.Size(180, 17);
            this.ShowDoubleBondTrimmingLines.TabIndex = 23;
            this.ShowDoubleBondTrimmingLines.Text = "Show double bond trimming lines";
            this.ShowDoubleBondTrimmingLines.UseVisualStyleBackColor = true;
            this.ShowDoubleBondTrimmingLines.CheckedChanged += new System.EventHandler(this.ShowDoubleBondTrimmingLines_CheckedChanged);
            // 
            // ShowConvexHulls
            // 
            this.ShowConvexHulls.AutoSize = true;
            this.ShowConvexHulls.Checked = true;
            this.ShowConvexHulls.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowConvexHulls.Location = new System.Drawing.Point(7, 64);
            this.ShowConvexHulls.Margin = new System.Windows.Forms.Padding(4);
            this.ShowConvexHulls.Name = "ShowConvexHulls";
            this.ShowConvexHulls.Size = new System.Drawing.Size(175, 17);
            this.ShowConvexHulls.TabIndex = 22;
            this.ShowConvexHulls.Text = "Show ConvexHull of characters";
            this.ShowConvexHulls.UseVisualStyleBackColor = true;
            this.ShowConvexHulls.CheckedChanged += new System.EventHandler(this.ShowConvexHulls_CheckedChanged);
            // 
            // ShowAtomPositions
            // 
            this.ShowAtomPositions.AutoSize = true;
            this.ShowAtomPositions.Checked = true;
            this.ShowAtomPositions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowAtomPositions.Location = new System.Drawing.Point(7, 12);
            this.ShowAtomPositions.Margin = new System.Windows.Forms.Padding(4);
            this.ShowAtomPositions.Name = "ShowAtomPositions";
            this.ShowAtomPositions.Size = new System.Drawing.Size(123, 17);
            this.ShowAtomPositions.TabIndex = 21;
            this.ShowAtomPositions.Text = "Show atom positions";
            this.ShowAtomPositions.UseVisualStyleBackColor = true;
            this.ShowAtomPositions.CheckedChanged += new System.EventHandler(this.ShowAtomCentres_CheckedChanged);
            // 
            // ShowRingCentres
            // 
            this.ShowRingCentres.AutoSize = true;
            this.ShowRingCentres.Checked = true;
            this.ShowRingCentres.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowRingCentres.Location = new System.Drawing.Point(246, 116);
            this.ShowRingCentres.Margin = new System.Windows.Forms.Padding(4);
            this.ShowRingCentres.Name = "ShowRingCentres";
            this.ShowRingCentres.Size = new System.Drawing.Size(168, 17);
            this.ShowRingCentres.TabIndex = 20;
            this.ShowRingCentres.Text = "Show centre of detected rings";
            this.ShowRingCentres.UseVisualStyleBackColor = true;
            this.ShowRingCentres.CheckedChanged += new System.EventHandler(this.ShowRingCentres_CheckedChanged);
            // 
            // ShowCharacterBox
            // 
            this.ShowCharacterBox.AutoSize = true;
            this.ShowCharacterBox.Checked = true;
            this.ShowCharacterBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowCharacterBox.Location = new System.Drawing.Point(7, 38);
            this.ShowCharacterBox.Margin = new System.Windows.Forms.Padding(4);
            this.ShowCharacterBox.Name = "ShowCharacterBox";
            this.ShowCharacterBox.Size = new System.Drawing.Size(184, 17);
            this.ShowCharacterBox.TabIndex = 19;
            this.ShowCharacterBox.Text = "Show BoundingBox of characters";
            this.ShowCharacterBox.UseVisualStyleBackColor = true;
            this.ShowCharacterBox.CheckedChanged += new System.EventHandler(this.ShowCharacterBox_CheckedChanged);
            // 
            // ShowMoleculeBox
            // 
            this.ShowMoleculeBox.AutoSize = true;
            this.ShowMoleculeBox.Checked = true;
            this.ShowMoleculeBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowMoleculeBox.Location = new System.Drawing.Point(7, 116);
            this.ShowMoleculeBox.Margin = new System.Windows.Forms.Padding(4);
            this.ShowMoleculeBox.Name = "ShowMoleculeBox";
            this.ShowMoleculeBox.Size = new System.Drawing.Size(187, 17);
            this.ShowMoleculeBox.TabIndex = 18;
            this.ShowMoleculeBox.Text = "Show BoundingBox of molecule(s)";
            this.ShowMoleculeBox.UseVisualStyleBackColor = true;
            this.ShowMoleculeBox.CheckedChanged += new System.EventHandler(this.ShowMoleculeBox_CheckedChanged);
            // 
            // ClipBondLines
            // 
            this.ClipBondLines.AutoSize = true;
            this.ClipBondLines.Checked = true;
            this.ClipBondLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ClipBondLines.Location = new System.Drawing.Point(246, 12);
            this.ClipBondLines.Margin = new System.Windows.Forms.Padding(4);
            this.ClipBondLines.Name = "ClipBondLines";
            this.ClipBondLines.Size = new System.Drawing.Size(187, 17);
            this.ClipBondLines.TabIndex = 13;
            this.ClipBondLines.Text = "Clip bond lines [Atom Convex Hull]";
            this.ClipBondLines.UseVisualStyleBackColor = true;
            this.ClipBondLines.CheckedChanged += new System.EventHandler(this.ClipLines_CheckedChanged);
            // 
            // btnSetDefaults
            // 
            this.btnSetDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetDefaults.Location = new System.Drawing.Point(291, 209);
            this.btnSetDefaults.Margin = new System.Windows.Forms.Padding(4);
            this.btnSetDefaults.Name = "btnSetDefaults";
            this.btnSetDefaults.Size = new System.Drawing.Size(88, 28);
            this.btnSetDefaults.TabIndex = 13;
            this.btnSetDefaults.Text = "Defaults";
            this.btnSetDefaults.UseVisualStyleBackColor = true;
            this.btnSetDefaults.Click += new System.EventHandler(this.SetDefaults_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(385, 209);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(88, 28);
            this.btnOk.TabIndex = 12;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.Ok_Click);
            // 
            // OoXmlV4Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 250);
            this.Controls.Add(this.btnSetDefaults);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabControlEx);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OoXmlV4Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.tabControlEx.ResumeLayout(false);
            this.tabRendering.ResumeLayout(false);
            this.tabRendering.PerformLayout();
            this.tabDebug.ResumeLayout(false);
            this.tabDebug.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControlEx tabControlEx;
        private System.Windows.Forms.TabPage tabRendering;
        private System.Windows.Forms.TabPage tabDebug;
        private System.Windows.Forms.Button btnSetDefaults;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox ShowHydrogens;
        private System.Windows.Forms.CheckBox ColouredAtoms;
        private System.Windows.Forms.CheckBox ClipBondLines;
        private System.Windows.Forms.CheckBox ShowRingCentres;
        private System.Windows.Forms.CheckBox ShowCharacterBox;
        private System.Windows.Forms.CheckBox ShowMoleculeBox;
        private System.Windows.Forms.CheckBox ShowAtomPositions;
        private System.Windows.Forms.CheckBox ShowConvexHulls;
        private System.Windows.Forms.CheckBox ShowMoleculeGrouping;
        private System.Windows.Forms.CheckBox ShowMoleculeCaptions;
        private System.Windows.Forms.CheckBox ShowAllCarbonAtoms;
        private System.Windows.Forms.CheckBox ShowDoubleBondTrimmingLines;
        private System.Windows.Forms.CheckBox ShowBondDirection;
        private System.Windows.Forms.CheckBox ShowCharacterGroupsBox;
        private System.Windows.Forms.CheckBox ClipCrossingBonds;
        private System.Windows.Forms.CheckBox ShowBondCrossingPoints;
    }
}