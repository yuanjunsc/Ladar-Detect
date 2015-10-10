namespace LiDAR
{
    partial class Display
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
            this.maxDistance = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.maxSet = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.finalx = new System.Windows.Forms.TextBox();
            this.finaly = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.label5 = new System.Windows.Forms.Label();
            this.cmbPortName = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.text_step = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.drawimg = new System.Windows.Forms.Button();
            this.bluered = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.resetstep = new System.Windows.Forms.TextBox();
            this.resetdata = new System.Windows.Forms.Button();
            this.text_x = new System.Windows.Forms.TextBox();
            this.text_y = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // maxDistance
            // 
            this.maxDistance.Location = new System.Drawing.Point(6, 69);
            this.maxDistance.Name = "maxDistance";
            this.maxDistance.Size = new System.Drawing.Size(27, 21);
            this.maxDistance.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "最远距离(m)";
            // 
            // maxSet
            // 
            this.maxSet.Location = new System.Drawing.Point(39, 67);
            this.maxSet.Name = "maxSet";
            this.maxSet.Size = new System.Drawing.Size(45, 23);
            this.maxSet.TabIndex = 2;
            this.maxSet.Text = "apply";
            this.maxSet.UseVisualStyleBackColor = true;
            this.maxSet.Click += new System.EventHandler(this.maxSet_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "范围（1-30）";
            // 
            // finalx
            // 
            this.finalx.Location = new System.Drawing.Point(31, 218);
            this.finalx.Name = "finalx";
            this.finalx.ReadOnly = true;
            this.finalx.Size = new System.Drawing.Size(53, 21);
            this.finalx.TabIndex = 4;
            // 
            // finaly
            // 
            this.finaly.Location = new System.Drawing.Point(31, 245);
            this.finaly.Name = "finaly";
            this.finaly.ReadOnly = true;
            this.finaly.Size = new System.Drawing.Size(53, 21);
            this.finaly.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 222);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 12);
            this.label3.TabIndex = 6;
            this.label3.Text = "Δx:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 248);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 12);
            this.label4.TabIndex = 7;
            this.label4.Text = "Δy:";
            // 
            // serialPort1
            // 
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 138);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(89, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "自动发送端口号";
            // 
            // cmbPortName
            // 
            this.cmbPortName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPortName.FormattingEnabled = true;
            this.cmbPortName.Location = new System.Drawing.Point(15, 159);
            this.cmbPortName.Name = "cmbPortName";
            this.cmbPortName.Size = new System.Drawing.Size(56, 20);
            this.cmbPortName.TabIndex = 9;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 185);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 10;
            this.button1.Text = "连接";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // text_step
            // 
            this.text_step.Location = new System.Drawing.Point(31, 275);
            this.text_step.Name = "text_step";
            this.text_step.ReadOnly = true;
            this.text_step.Size = new System.Drawing.Size(53, 21);
            this.text_step.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 279);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 12);
            this.label6.TabIndex = 12;
            this.label6.Text = "step";
            // 
            // drawimg
            // 
            this.drawimg.Location = new System.Drawing.Point(9, 96);
            this.drawimg.Name = "drawimg";
            this.drawimg.Size = new System.Drawing.Size(81, 39);
            this.drawimg.TabIndex = 13;
            this.drawimg.Text = "显示图像";
            this.drawimg.UseVisualStyleBackColor = true;
            this.drawimg.Click += new System.EventHandler(this.drawimg_Click);
            // 
            // bluered
            // 
            this.bluered.Location = new System.Drawing.Point(6, 1);
            this.bluered.Name = "bluered";
            this.bluered.Size = new System.Drawing.Size(75, 23);
            this.bluered.TabIndex = 14;
            this.bluered.Text = "蓝场";
            this.bluered.UseVisualStyleBackColor = true;
            this.bluered.Click += new System.EventHandler(this.bluered_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(4, 313);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 12);
            this.label7.TabIndex = 15;
            this.label7.Text = "重新校准";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(4, 342);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(29, 12);
            this.label8.TabIndex = 16;
            this.label8.Text = "step";
            // 
            // resetstep
            // 
            this.resetstep.Location = new System.Drawing.Point(36, 339);
            this.resetstep.Name = "resetstep";
            this.resetstep.Size = new System.Drawing.Size(45, 21);
            this.resetstep.TabIndex = 17;
            // 
            // resetdata
            // 
            this.resetdata.Location = new System.Drawing.Point(6, 366);
            this.resetdata.Name = "resetdata";
            this.resetdata.Size = new System.Drawing.Size(75, 23);
            this.resetdata.TabIndex = 18;
            this.resetdata.Text = "开始校准";
            this.resetdata.UseVisualStyleBackColor = true;
            this.resetdata.Click += new System.EventHandler(this.resetdata_Click);
            // 
            // text_x
            // 
            this.text_x.Location = new System.Drawing.Point(6, 398);
            this.text_x.Name = "text_x";
            this.text_x.ReadOnly = true;
            this.text_x.Size = new System.Drawing.Size(36, 21);
            this.text_x.TabIndex = 19;
            // 
            // text_y
            // 
            this.text_y.Location = new System.Drawing.Point(48, 398);
            this.text_y.Name = "text_y";
            this.text_y.ReadOnly = true;
            this.text_y.Size = new System.Drawing.Size(36, 21);
            this.text_y.TabIndex = 20;
            // 
            // Display
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 431);
            this.Controls.Add(this.text_y);
            this.Controls.Add(this.text_x);
            this.Controls.Add(this.resetdata);
            this.Controls.Add(this.resetstep);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.bluered);
            this.Controls.Add(this.drawimg);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.text_step);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.cmbPortName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.finaly);
            this.Controls.Add(this.finalx);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.maxSet);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.maxDistance);
            this.ForeColor = System.Drawing.Color.SteelBlue;
            this.Name = "Display";
            this.Text = "Display";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Display_FormClosing);
            this.Load += new System.EventHandler(this.Display_Load);
            this.SizeChanged += new System.EventHandler(this.Display_SizeChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Display_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox maxDistance;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button maxSet;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox finalx;
        private System.Windows.Forms.TextBox finaly;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbPortName;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox text_step;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button drawimg;
        private System.Windows.Forms.Button bluered;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox resetstep;
        private System.Windows.Forms.Button resetdata;
        private System.Windows.Forms.TextBox text_x;
        private System.Windows.Forms.TextBox text_y;
    }
}