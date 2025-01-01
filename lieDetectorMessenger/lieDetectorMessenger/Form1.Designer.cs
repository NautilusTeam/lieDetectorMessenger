// Form1.Designer.cs
using System.Drawing;
using System.Windows.Forms;

public partial class Form1
{
    private PictureBox lieDetectorPicture;
    private TextBox answerBox;
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        lieDetectorPicture = new PictureBox();
        answerBox = new TextBox();
        ((System.ComponentModel.ISupportInitialize)lieDetectorPicture).BeginInit();
        SuspendLayout();
        // 
        // lieDetectorPicture
        // 
        lieDetectorPicture.Location = new Point(24, 41);
        lieDetectorPicture.Name = "lieDetectorPicture";
        lieDetectorPicture.Size = new Size(270, 231);
        lieDetectorPicture.TabIndex = 0;
        lieDetectorPicture.TabStop = false;
        // 
        // answerBox
        // 
        answerBox.Location = new Point(24, 290);
        answerBox.Name = "answerBox";
        answerBox.Size = new Size(270, 23);
        answerBox.TabIndex = 1;
        // 
        // Form1
        // 
        ClientSize = new Size(314, 329);
        Controls.Add(answerBox);
        Controls.Add(lieDetectorPicture);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "Form1";
        ((System.ComponentModel.ISupportInitialize)lieDetectorPicture).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
