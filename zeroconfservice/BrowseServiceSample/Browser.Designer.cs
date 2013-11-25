namespace BrowseServiceSample
{
    partial class Browser
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
			System.Windows.Forms.ColumnHeader Key;
			System.Windows.Forms.ColumnHeader Value;
			this.label1 = new System.Windows.Forms.Label();
			this.serviceTextBox = new System.Windows.Forms.TextBox();
			this.startStopButton = new System.Windows.Forms.Button();
			this.servicesList = new System.Windows.Forms.ListView();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.resolveButton = new System.Windows.Forms.Button();
			this.txtRecordListView = new System.Windows.Forms.ListView();
			this.txtRecordLabel = new System.Windows.Forms.Label();
			this.addressLabel = new System.Windows.Forms.Label();
			this.addressList = new System.Windows.Forms.ListView();
			this.hostnameLabel = new System.Windows.Forms.Label();
			this.serviceLabel = new System.Windows.Forms.Label();
			Key = new System.Windows.Forms.ColumnHeader();
			Value = new System.Windows.Forms.ColumnHeader();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// Key
			// 
			Key.Text = "Key";
			Key.Width = 100;
			// 
			// Value
			// 
			Value.Text = "Value";
			Value.Width = 150;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(46, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Service:";
			// 
			// serviceTextBox
			// 
			this.serviceTextBox.Location = new System.Drawing.Point(64, 6);
			this.serviceTextBox.Name = "serviceTextBox";
			this.serviceTextBox.Size = new System.Drawing.Size(181, 20);
			this.serviceTextBox.TabIndex = 1;
			this.serviceTextBox.Text = "_http._tcp";
			// 
			// startStopButton
			// 
			this.startStopButton.Location = new System.Drawing.Point(251, 4);
			this.startStopButton.Name = "startStopButton";
			this.startStopButton.Size = new System.Drawing.Size(68, 23);
			this.startStopButton.TabIndex = 2;
			this.startStopButton.Text = "Start";
			this.startStopButton.UseVisualStyleBackColor = true;
			this.startStopButton.Click += new System.EventHandler(this.startStopButton_Click);
			// 
			// servicesList
			// 
			this.servicesList.FullRowSelect = true;
			this.servicesList.HideSelection = false;
			this.servicesList.Location = new System.Drawing.Point(6, 19);
			this.servicesList.MultiSelect = false;
			this.servicesList.Name = "servicesList";
			this.servicesList.Size = new System.Drawing.Size(295, 160);
			this.servicesList.TabIndex = 3;
			this.servicesList.UseCompatibleStateImageBehavior = false;
			this.servicesList.View = System.Windows.Forms.View.List;
			this.servicesList.SelectedIndexChanged += new System.EventHandler(this.servicesList_SelectedIndexChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.servicesList);
			this.groupBox1.Location = new System.Drawing.Point(12, 33);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(307, 185);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Found Services:";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.resolveButton);
			this.groupBox2.Controls.Add(this.txtRecordListView);
			this.groupBox2.Controls.Add(this.txtRecordLabel);
			this.groupBox2.Controls.Add(this.addressLabel);
			this.groupBox2.Controls.Add(this.addressList);
			this.groupBox2.Controls.Add(this.hostnameLabel);
			this.groupBox2.Controls.Add(this.serviceLabel);
			this.groupBox2.Location = new System.Drawing.Point(12, 224);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(307, 252);
			this.groupBox2.TabIndex = 5;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Selected Service:";
			// 
			// resolveButton
			// 
			this.resolveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.resolveButton.Location = new System.Drawing.Point(226, 16);
			this.resolveButton.Name = "resolveButton";
			this.resolveButton.Size = new System.Drawing.Size(75, 23);
			this.resolveButton.TabIndex = 6;
			this.resolveButton.Text = "Resolve";
			this.resolveButton.UseVisualStyleBackColor = true;
			this.resolveButton.Visible = false;
			this.resolveButton.Click += new System.EventHandler(this.resolveButton_Click);
			// 
			// txtRecordListView
			// 
			this.txtRecordListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            Key,
            Value});
			this.txtRecordListView.Location = new System.Drawing.Point(9, 67);
			this.txtRecordListView.Name = "txtRecordListView";
			this.txtRecordListView.Size = new System.Drawing.Size(292, 88);
			this.txtRecordListView.TabIndex = 5;
			this.txtRecordListView.UseCompatibleStateImageBehavior = false;
			this.txtRecordListView.View = System.Windows.Forms.View.Details;
			this.txtRecordListView.Visible = false;
			// 
			// txtRecordLabel
			// 
			this.txtRecordLabel.AutoSize = true;
			this.txtRecordLabel.Location = new System.Drawing.Point(6, 51);
			this.txtRecordLabel.Name = "txtRecordLabel";
			this.txtRecordLabel.Size = new System.Drawing.Size(80, 13);
			this.txtRecordLabel.TabIndex = 4;
			this.txtRecordLabel.Text = "0 TXT Records";
			this.txtRecordLabel.Visible = false;
			// 
			// addressLabel
			// 
			this.addressLabel.AutoSize = true;
			this.addressLabel.Location = new System.Drawing.Point(6, 167);
			this.addressLabel.Name = "addressLabel";
			this.addressLabel.Size = new System.Drawing.Size(65, 13);
			this.addressLabel.TabIndex = 3;
			this.addressLabel.Text = "0 Addresses";
			this.addressLabel.Visible = false;
			// 
			// addressList
			// 
			this.addressList.Location = new System.Drawing.Point(9, 183);
			this.addressList.MultiSelect = false;
			this.addressList.Name = "addressList";
			this.addressList.Size = new System.Drawing.Size(292, 63);
			this.addressList.TabIndex = 2;
			this.addressList.UseCompatibleStateImageBehavior = false;
			this.addressList.View = System.Windows.Forms.View.List;
			this.addressList.Visible = false;
			// 
			// hostnameLabel
			// 
			this.hostnameLabel.AutoSize = true;
			this.hostnameLabel.Location = new System.Drawing.Point(6, 29);
			this.hostnameLabel.Name = "hostnameLabel";
			this.hostnameLabel.Size = new System.Drawing.Size(53, 13);
			this.hostnameLabel.TabIndex = 1;
			this.hostnameLabel.Text = "hostname";
			this.hostnameLabel.Visible = false;
			// 
			// serviceLabel
			// 
			this.serviceLabel.AutoSize = true;
			this.serviceLabel.Location = new System.Drawing.Point(6, 16);
			this.serviceLabel.Name = "serviceLabel";
			this.serviceLabel.Size = new System.Drawing.Size(101, 13);
			this.serviceLabel.TabIndex = 0;
			this.serviceLabel.Text = "No service selected";
			// 
			// Browser
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(331, 488);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.startStopButton);
			this.Controls.Add(this.serviceTextBox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "Browser";
			this.Text = "Browser";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox serviceTextBox;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.ListView servicesList;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label serviceLabel;
        private System.Windows.Forms.Label hostnameLabel;
        private System.Windows.Forms.Label addressLabel;
        private System.Windows.Forms.ListView addressList;
        private System.Windows.Forms.Label txtRecordLabel;
        private System.Windows.Forms.ListView txtRecordListView;
		private System.Windows.Forms.Button resolveButton;
    }
}

