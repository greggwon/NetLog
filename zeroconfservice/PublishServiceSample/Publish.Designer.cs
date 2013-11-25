namespace PublishServiceSample
{
    partial class Publish
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
			this.label1 = new System.Windows.Forms.Label();
			this.serviceNameTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.serviceTypeTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.portTextBox = new System.Windows.Forms.TextBox();
			this.startStopButton = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.updateTXTButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(77, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Service Name:";
			// 
			// serviceNameTextBox
			// 
			this.serviceNameTextBox.Location = new System.Drawing.Point(95, 6);
			this.serviceNameTextBox.Name = "serviceNameTextBox";
			this.serviceNameTextBox.Size = new System.Drawing.Size(177, 20);
			this.serviceNameTextBox.TabIndex = 1;
			this.serviceNameTextBox.Text = "Service Name";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 35);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(73, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "Service Type:";
			// 
			// serviceTypeTextBox
			// 
			this.serviceTypeTextBox.Location = new System.Drawing.Point(95, 32);
			this.serviceTypeTextBox.Name = "serviceTypeTextBox";
			this.serviceTypeTextBox.Size = new System.Drawing.Size(177, 20);
			this.serviceTypeTextBox.TabIndex = 1;
			this.serviceTypeTextBox.Text = "_http._tcp";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 61);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "Port:";
			// 
			// portTextBox
			// 
			this.portTextBox.Location = new System.Drawing.Point(95, 58);
			this.portTextBox.Name = "portTextBox";
			this.portTextBox.Size = new System.Drawing.Size(177, 20);
			this.portTextBox.TabIndex = 1;
			this.portTextBox.Text = "80";
			// 
			// startStopButton
			// 
			this.startStopButton.Location = new System.Drawing.Point(197, 144);
			this.startStopButton.Name = "startStopButton";
			this.startStopButton.Size = new System.Drawing.Size(75, 23);
			this.startStopButton.TabIndex = 2;
			this.startStopButton.Text = "Publish";
			this.startStopButton.UseVisualStyleBackColor = true;
			this.startStopButton.Click += new System.EventHandler(this.startStopButton_Click);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(12, 81);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(260, 48);
			this.label4.TabIndex = 3;
			this.label4.Text = "Please note: This sample application hard codes the TXT Records. Please edit the " +
				"source code to change them.";
			// 
			// updateTXTButton
			// 
			this.updateTXTButton.Enabled = false;
			this.updateTXTButton.Location = new System.Drawing.Point(13, 143);
			this.updateTXTButton.Name = "updateTXTButton";
			this.updateTXTButton.Size = new System.Drawing.Size(75, 23);
			this.updateTXTButton.TabIndex = 4;
			this.updateTXTButton.Text = "Update TXT";
			this.updateTXTButton.UseVisualStyleBackColor = true;
			this.updateTXTButton.Click += new System.EventHandler(this.updateTXTButton_Click);
			// 
			// Publish
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 179);
			this.Controls.Add(this.updateTXTButton);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.startStopButton);
			this.Controls.Add(this.portTextBox);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.serviceTypeTextBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.serviceNameTextBox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "Publish";
			this.Text = "Publish";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox serviceNameTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox serviceTypeTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button updateTXTButton;
    }
}

