
namespace PS4_BO3_GSC
{
    partial class MainWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.styleManager = new MetroFramework.Components.MetroStyleManager(this.components);
            this.compilerGroupBox = new System.Windows.Forms.GroupBox();
            this.T7browseOutputPathButton = new System.Windows.Forms.Button();
            this.T7browseGscFolderButton = new System.Windows.Forms.Button();
            this.T7compileGscProjectButton = new System.Windows.Forms.Button();
            this.T7compiledGscOutputLabel = new MetroFramework.Controls.MetroLabel();
            this.T7compiledGscFileOutputTextBox = new MetroFramework.Controls.MetroTextBox();
            this.metroLabel1 = new MetroFramework.Controls.MetroLabel();
            this.T7gscProjectFolderTextBox = new MetroFramework.Controls.MetroTextBox();
            this.metroLabel3 = new MetroFramework.Controls.MetroLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.T8browseOutputPathButton = new System.Windows.Forms.Button();
            this.T8browseGscFolderButton = new System.Windows.Forms.Button();
            this.T8compileGscProjectButton = new System.Windows.Forms.Button();
            this.T8compiledGscOutputLabel = new MetroFramework.Controls.MetroLabel();
            this.T8compiledGscFileOutputTextBox = new MetroFramework.Controls.MetroTextBox();
            this.metroLabel4 = new MetroFramework.Controls.MetroLabel();
            this.T8gscProjectFolderTextBox = new MetroFramework.Controls.MetroTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.styleManager)).BeginInit();
            this.compilerGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // styleManager
            // 
            this.styleManager.Owner = this;
            this.styleManager.Style = MetroFramework.MetroColorStyle.Blue;
            this.styleManager.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // compilerGroupBox
            // 
            this.compilerGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.compilerGroupBox.Controls.Add(this.T7browseOutputPathButton);
            this.compilerGroupBox.Controls.Add(this.T7browseGscFolderButton);
            this.compilerGroupBox.Controls.Add(this.T7compileGscProjectButton);
            this.compilerGroupBox.Controls.Add(this.T7compiledGscOutputLabel);
            this.compilerGroupBox.Controls.Add(this.T7compiledGscFileOutputTextBox);
            this.compilerGroupBox.Controls.Add(this.metroLabel1);
            this.compilerGroupBox.Controls.Add(this.T7gscProjectFolderTextBox);
            this.compilerGroupBox.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.compilerGroupBox.Location = new System.Drawing.Point(24, 58);
            this.compilerGroupBox.Name = "compilerGroupBox";
            this.compilerGroupBox.Size = new System.Drawing.Size(487, 124);
            this.compilerGroupBox.TabIndex = 1;
            this.compilerGroupBox.TabStop = false;
            this.compilerGroupBox.Text = "T7 Compiler";
            // 
            // T7browseOutputPathButton
            // 
            this.T7browseOutputPathButton.BackColor = System.Drawing.Color.Black;
            this.T7browseOutputPathButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T7browseOutputPathButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T7browseOutputPathButton.Location = new System.Drawing.Point(401, 47);
            this.T7browseOutputPathButton.Name = "T7browseOutputPathButton";
            this.T7browseOutputPathButton.Size = new System.Drawing.Size(75, 23);
            this.T7browseOutputPathButton.TabIndex = 11;
            this.T7browseOutputPathButton.Text = "Browse";
            this.T7browseOutputPathButton.UseVisualStyleBackColor = false;
            this.T7browseOutputPathButton.Click += new System.EventHandler(this.T7browseOutputPathButton_Click);
            // 
            // T7browseGscFolderButton
            // 
            this.T7browseGscFolderButton.BackColor = System.Drawing.Color.Black;
            this.T7browseGscFolderButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T7browseGscFolderButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T7browseGscFolderButton.Location = new System.Drawing.Point(401, 18);
            this.T7browseGscFolderButton.Name = "T7browseGscFolderButton";
            this.T7browseGscFolderButton.Size = new System.Drawing.Size(75, 23);
            this.T7browseGscFolderButton.TabIndex = 10;
            this.T7browseGscFolderButton.Text = "Browse";
            this.T7browseGscFolderButton.UseVisualStyleBackColor = false;
            this.T7browseGscFolderButton.Click += new System.EventHandler(this.T7browseGscFolderButton_Click);
            // 
            // T7compileGscProjectButton
            // 
            this.T7compileGscProjectButton.BackColor = System.Drawing.Color.Black;
            this.T7compileGscProjectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T7compileGscProjectButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.T7compileGscProjectButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T7compileGscProjectButton.Location = new System.Drawing.Point(99, 76);
            this.T7compileGscProjectButton.Name = "T7compileGscProjectButton";
            this.T7compileGscProjectButton.Size = new System.Drawing.Size(296, 33);
            this.T7compileGscProjectButton.TabIndex = 8;
            this.T7compileGscProjectButton.Text = "Compile GSC Project";
            this.T7compileGscProjectButton.UseVisualStyleBackColor = false;
            this.T7compileGscProjectButton.Click += new System.EventHandler(this.T7compileGscProjectButton_Click);
            // 
            // T7compiledGscOutputLabel
            // 
            this.T7compiledGscOutputLabel.AutoSize = true;
            this.T7compiledGscOutputLabel.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T7compiledGscOutputLabel.Location = new System.Drawing.Point(11, 48);
            this.T7compiledGscOutputLabel.Name = "T7compiledGscOutputLabel";
            this.T7compiledGscOutputLabel.Size = new System.Drawing.Size(82, 19);
            this.T7compiledGscOutputLabel.TabIndex = 7;
            this.T7compiledGscOutputLabel.Text = "Output Path:";
            this.T7compiledGscOutputLabel.UseStyleColors = true;
            // 
            // T7compiledGscFileOutputTextBox
            // 
            this.T7compiledGscFileOutputTextBox.ForeColor = System.Drawing.Color.Black;
            this.T7compiledGscFileOutputTextBox.Location = new System.Drawing.Point(99, 47);
            this.T7compiledGscFileOutputTextBox.Name = "T7compiledGscFileOutputTextBox";
            this.T7compiledGscFileOutputTextBox.ReadOnly = true;
            this.T7compiledGscFileOutputTextBox.Size = new System.Drawing.Size(296, 23);
            this.T7compiledGscFileOutputTextBox.Style = MetroFramework.MetroColorStyle.Blue;
            this.T7compiledGscFileOutputTextBox.TabIndex = 6;
            this.T7compiledGscFileOutputTextBox.Theme = MetroFramework.MetroThemeStyle.Light;
            this.T7compiledGscFileOutputTextBox.UseStyleColors = true;
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.metroLabel1.Location = new System.Drawing.Point(11, 19);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(82, 19);
            this.metroLabel1.TabIndex = 4;
            this.metroLabel1.Text = "GSC Project:";
            this.metroLabel1.UseStyleColors = true;
            // 
            // T7gscProjectFolderTextBox
            // 
            this.T7gscProjectFolderTextBox.ForeColor = System.Drawing.Color.Black;
            this.T7gscProjectFolderTextBox.Location = new System.Drawing.Point(99, 18);
            this.T7gscProjectFolderTextBox.Name = "T7gscProjectFolderTextBox";
            this.T7gscProjectFolderTextBox.ReadOnly = true;
            this.T7gscProjectFolderTextBox.Size = new System.Drawing.Size(296, 23);
            this.T7gscProjectFolderTextBox.Style = MetroFramework.MetroColorStyle.Blue;
            this.T7gscProjectFolderTextBox.TabIndex = 3;
            this.T7gscProjectFolderTextBox.Theme = MetroFramework.MetroThemeStyle.Light;
            this.T7gscProjectFolderTextBox.UseStyleColors = true;
            // 
            // metroLabel3
            // 
            this.metroLabel3.AutoSize = true;
            this.metroLabel3.Location = new System.Drawing.Point(415, 315);
            this.metroLabel3.Name = "metroLabel3";
            this.metroLabel3.Size = new System.Drawing.Size(96, 19);
            this.metroLabel3.TabIndex = 3;
            this.metroLabel3.Text = "Base By DizzRL";
            this.metroLabel3.UseStyleColors = true;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.T8browseOutputPathButton);
            this.groupBox1.Controls.Add(this.T8browseGscFolderButton);
            this.groupBox1.Controls.Add(this.T8compileGscProjectButton);
            this.groupBox1.Controls.Add(this.T8compiledGscOutputLabel);
            this.groupBox1.Controls.Add(this.T8compiledGscFileOutputTextBox);
            this.groupBox1.Controls.Add(this.metroLabel4);
            this.groupBox1.Controls.Add(this.T8gscProjectFolderTextBox);
            this.groupBox1.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.groupBox1.Location = new System.Drawing.Point(24, 188);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(487, 124);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "T8 Compiler";
            // 
            // T8browseOutputPathButton
            // 
            this.T8browseOutputPathButton.BackColor = System.Drawing.Color.Black;
            this.T8browseOutputPathButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T8browseOutputPathButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T8browseOutputPathButton.Location = new System.Drawing.Point(401, 47);
            this.T8browseOutputPathButton.Name = "T8browseOutputPathButton";
            this.T8browseOutputPathButton.Size = new System.Drawing.Size(75, 23);
            this.T8browseOutputPathButton.TabIndex = 11;
            this.T8browseOutputPathButton.Text = "Browse";
            this.T8browseOutputPathButton.UseVisualStyleBackColor = false;
            this.T8browseOutputPathButton.Click += new System.EventHandler(this.T8browseOutputPathButton_Click);
            // 
            // T8browseGscFolderButton
            // 
            this.T8browseGscFolderButton.BackColor = System.Drawing.Color.Black;
            this.T8browseGscFolderButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T8browseGscFolderButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T8browseGscFolderButton.Location = new System.Drawing.Point(401, 18);
            this.T8browseGscFolderButton.Name = "T8browseGscFolderButton";
            this.T8browseGscFolderButton.Size = new System.Drawing.Size(75, 23);
            this.T8browseGscFolderButton.TabIndex = 10;
            this.T8browseGscFolderButton.Text = "Browse";
            this.T8browseGscFolderButton.UseVisualStyleBackColor = false;
            this.T8browseGscFolderButton.Click += new System.EventHandler(this.T8browseGscFolderButton_Click);
            // 
            // T8compileGscProjectButton
            // 
            this.T8compileGscProjectButton.BackColor = System.Drawing.Color.Black;
            this.T8compileGscProjectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.T8compileGscProjectButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.T8compileGscProjectButton.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T8compileGscProjectButton.Location = new System.Drawing.Point(99, 76);
            this.T8compileGscProjectButton.Name = "T8compileGscProjectButton";
            this.T8compileGscProjectButton.Size = new System.Drawing.Size(296, 33);
            this.T8compileGscProjectButton.TabIndex = 8;
            this.T8compileGscProjectButton.Text = "Compile GSC Project";
            this.T8compileGscProjectButton.UseVisualStyleBackColor = false;
            this.T8compileGscProjectButton.Click += new System.EventHandler(this.T8compileGscProjectButton_Click);
            // 
            // T8compiledGscOutputLabel
            // 
            this.T8compiledGscOutputLabel.AutoSize = true;
            this.T8compiledGscOutputLabel.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.T8compiledGscOutputLabel.Location = new System.Drawing.Point(11, 48);
            this.T8compiledGscOutputLabel.Name = "T8compiledGscOutputLabel";
            this.T8compiledGscOutputLabel.Size = new System.Drawing.Size(82, 19);
            this.T8compiledGscOutputLabel.TabIndex = 7;
            this.T8compiledGscOutputLabel.Text = "Output Path:";
            this.T8compiledGscOutputLabel.UseStyleColors = true;
            // 
            // T8compiledGscFileOutputTextBox
            // 
            this.T8compiledGscFileOutputTextBox.ForeColor = System.Drawing.Color.Black;
            this.T8compiledGscFileOutputTextBox.Location = new System.Drawing.Point(99, 47);
            this.T8compiledGscFileOutputTextBox.Name = "T8compiledGscFileOutputTextBox";
            this.T8compiledGscFileOutputTextBox.ReadOnly = true;
            this.T8compiledGscFileOutputTextBox.Size = new System.Drawing.Size(296, 23);
            this.T8compiledGscFileOutputTextBox.Style = MetroFramework.MetroColorStyle.Blue;
            this.T8compiledGscFileOutputTextBox.TabIndex = 6;
            this.T8compiledGscFileOutputTextBox.Theme = MetroFramework.MetroThemeStyle.Light;
            this.T8compiledGscFileOutputTextBox.UseStyleColors = true;
            // 
            // metroLabel4
            // 
            this.metroLabel4.AutoSize = true;
            this.metroLabel4.ForeColor = System.Drawing.Color.DeepSkyBlue;
            this.metroLabel4.Location = new System.Drawing.Point(11, 19);
            this.metroLabel4.Name = "metroLabel4";
            this.metroLabel4.Size = new System.Drawing.Size(82, 19);
            this.metroLabel4.TabIndex = 4;
            this.metroLabel4.Text = "GSC Project:";
            this.metroLabel4.UseStyleColors = true;
            // 
            // T8gscProjectFolderTextBox
            // 
            this.T8gscProjectFolderTextBox.ForeColor = System.Drawing.Color.Black;
            this.T8gscProjectFolderTextBox.Location = new System.Drawing.Point(99, 18);
            this.T8gscProjectFolderTextBox.Name = "T8gscProjectFolderTextBox";
            this.T8gscProjectFolderTextBox.ReadOnly = true;
            this.T8gscProjectFolderTextBox.Size = new System.Drawing.Size(296, 23);
            this.T8gscProjectFolderTextBox.Style = MetroFramework.MetroColorStyle.Blue;
            this.T8gscProjectFolderTextBox.TabIndex = 3;
            this.T8gscProjectFolderTextBox.Theme = MetroFramework.MetroThemeStyle.Light;
            this.T8gscProjectFolderTextBox.UseStyleColors = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 350);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.metroLabel3);
            this.Controls.Add(this.compilerGroupBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.Resizable = false;
            this.Style = MetroFramework.MetroColorStyle.Blue;
            this.Text = "PS4 T7/T8 GSC Compiler";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.MainWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.styleManager)).EndInit();
            this.compilerGroupBox.ResumeLayout(false);
            this.compilerGroupBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MetroFramework.Components.MetroStyleManager styleManager;
        private System.Windows.Forms.GroupBox compilerGroupBox;
        private MetroFramework.Controls.MetroLabel metroLabel1;
        private MetroFramework.Controls.MetroTextBox T7gscProjectFolderTextBox;
        private MetroFramework.Controls.MetroLabel T7compiledGscOutputLabel;
        private MetroFramework.Controls.MetroTextBox T7compiledGscFileOutputTextBox;
        private System.Windows.Forms.Button T7compileGscProjectButton;
        private System.Windows.Forms.Button T7browseOutputPathButton;
        private System.Windows.Forms.Button T7browseGscFolderButton;
        private MetroFramework.Controls.MetroLabel metroLabel3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button T8browseOutputPathButton;
        private System.Windows.Forms.Button T8browseGscFolderButton;
        private System.Windows.Forms.Button T8compileGscProjectButton;
        private MetroFramework.Controls.MetroLabel T8compiledGscOutputLabel;
        private MetroFramework.Controls.MetroTextBox T8compiledGscFileOutputTextBox;
        private MetroFramework.Controls.MetroLabel metroLabel4;
        private MetroFramework.Controls.MetroTextBox T8gscProjectFolderTextBox;
    }
}

