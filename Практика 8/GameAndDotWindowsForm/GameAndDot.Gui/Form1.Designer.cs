namespace GameAndDot.Gui
{
    partial class Form1
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
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            listBox1 = new ListBox();
            label5 = new Label();
            button3 = new Button();
            textBox1 = new TextBox();
            label6 = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Times New Roman", 16F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label1.Location = new Point(978, 9);
            label1.Name = "label1";
            label1.Size = new Size(170, 36);
            label1.TabIndex = 0;
            label1.Text = "Ваше имя:";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Times New Roman", 16F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label2.Location = new Point(978, 45);
            label2.Name = "label2";
            label2.Size = new Size(104, 36);
            label2.TabIndex = 1;
            label2.Text = "Color:";
            label2.Click += label2_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Times New Roman", 16F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label3.Location = new Point(1141, 9);
            label3.Name = "label3";
            label3.Size = new Size(97, 36);
            label3.TabIndex = 3;
            label3.Text = "User 1";
            label3.Click += label3_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Times New Roman", 16F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label4.Location = new Point(978, 93);
            label4.Name = "label4";
            label4.Size = new Size(315, 36);
            label4.TabIndex = 4;
            label4.Text = "Список всех игроков";
            label4.Click += label4_Click;
            // 
            // listBox1
            // 
            listBox1.Font = new Font("Times New Roman", 14F, FontStyle.Regular, GraphicsUnit.Point, 204);
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(978, 132);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(768, 565);
            listBox1.TabIndex = 5;
            listBox1.DrawItem += listBox1_DrawItem;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Times New Roman", 14F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label5.Location = new Point(1079, 48);
            label5.Name = "label5";
            label5.Size = new Size(89, 33);
            label5.TabIndex = 6;
            label5.Text = "#black";
            label5.Click += label5_Click;
            // 
            // button3
            // 
            button3.Font = new Font("Times New Roman", 12F, FontStyle.Bold, GraphicsUnit.Point, 204);
            button3.Location = new Point(754, 336);
            button3.Name = "button3";
            button3.Size = new Size(126, 40);
            button3.TabIndex = 9;
            button3.Text = "Войти";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // textBox1
            // 
            textBox1.Font = new Font("Times New Roman", 14F, FontStyle.Regular, GraphicsUnit.Point, 204);
            textBox1.Location = new Point(385, 336);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(363, 40);
            textBox1.TabIndex = 8;
            textBox1.TextChanged += textBox1_TextChanged_1;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Times New Roman", 16F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label6.Location = new Point(46, 336);
            label6.Name = "label6";
            label6.Size = new Size(343, 36);
            label6.TabIndex = 7;
            label6.Text = "Введите пользователя:";
            label6.Click += label6_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(972, 745);
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            pictureBox1.Paint += pictureBox1_Paint;
            pictureBox1.MouseClick += pictureBox1_MouseClick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1758, 757);
            Controls.Add(button3);
            Controls.Add(textBox1);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(listBox1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private ListBox listBox1;
        private Label label5;
        private Button button3;
        private TextBox textBox1;
        private Label label6;
        private PictureBox pictureBox1;
    }
}
