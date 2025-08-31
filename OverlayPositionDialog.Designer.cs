namespace GifProcessorApp
{
    partial class OverlayPositionDialog
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblX = new System.Windows.Forms.Label();
            numX = new System.Windows.Forms.NumericUpDown();
            lblY = new System.Windows.Forms.Label();
            numY = new System.Windows.Forms.NumericUpDown();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)numX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numY).BeginInit();
            SuspendLayout();
            // 
            // lblX
            // 
            lblX.AutoSize = true;
            lblX.Location = new System.Drawing.Point(12, 15);
            lblX.Name = "lblX";
            lblX.Size = new System.Drawing.Size(14, 15);
            lblX.TabIndex = 0;
            lblX.Text = "X";
            // 
            // numX
            // 
            numX.Location = new System.Drawing.Point(40, 12);
            numX.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numX.Name = "numX";
            numX.Size = new System.Drawing.Size(120, 23);
            numX.TabIndex = 1;
            // 
            // lblY
            // 
            lblY.AutoSize = true;
            lblY.Location = new System.Drawing.Point(12, 44);
            lblY.Name = "lblY";
            lblY.Size = new System.Drawing.Size(14, 15);
            lblY.TabIndex = 2;
            lblY.Text = "Y";
            // 
            // numY
            // 
            numY.Location = new System.Drawing.Point(40, 41);
            numY.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numY.Name = "numY";
            numY.Size = new System.Drawing.Size(120, 23);
            numY.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOk.Location = new System.Drawing.Point(40, 80);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(60, 25);
            btnOk.TabIndex = 4;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(110, 80);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(60, 25);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // OverlayPositionDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(200, 120);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(numY);
            Controls.Add(lblY);
            Controls.Add(numX);
            Controls.Add(lblX);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OverlayPositionDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Overlay Position";
            ((System.ComponentModel.ISupportInitialize)numX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numY).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.NumericUpDown numX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.NumericUpDown numY;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
