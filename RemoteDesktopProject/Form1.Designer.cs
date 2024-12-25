namespace RemoteDesktopProject
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            axMsRdpClient11NotSafeForScripting1 = new AxMSTSCLib.AxMsRdpClient11NotSafeForScripting();
            ((System.ComponentModel.ISupportInitialize)axMsRdpClient11NotSafeForScripting1).BeginInit();
            SuspendLayout();
            // 
            // axMsRdpClient11NotSafeForScripting1
            // 
            axMsRdpClient11NotSafeForScripting1.Dock = DockStyle.Fill;
            axMsRdpClient11NotSafeForScripting1.Enabled = true;
            axMsRdpClient11NotSafeForScripting1.Location = new Point(0, 0);
            axMsRdpClient11NotSafeForScripting1.Margin = new Padding(0);
            axMsRdpClient11NotSafeForScripting1.Name = "axMsRdpClient11NotSafeForScripting1";
            axMsRdpClient11NotSafeForScripting1.OcxState = (AxHost.State)resources.GetObject("axMsRdpClient11NotSafeForScripting1.OcxState");
            axMsRdpClient11NotSafeForScripting1.Size = new Size(802, 453);
            axMsRdpClient11NotSafeForScripting1.TabIndex = 0;
            axMsRdpClient11NotSafeForScripting1.SizeChanged += axMsRdpClient11NotSafeForScripting1_SizeChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(802, 453);
            Controls.Add(axMsRdpClient11NotSafeForScripting1);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)axMsRdpClient11NotSafeForScripting1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private AxMSTSCLib.AxMsRdpClient11NotSafeForScripting axMsRdpClient11NotSafeForScripting1;
    }
}