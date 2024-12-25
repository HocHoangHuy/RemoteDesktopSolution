using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace WpfApp1
{
    public partial class Form1 : Form
    {
        public AxMSTSCLib.AxMsRdpClient11NotSafeForScripting abc;
        public Form1()
        {
            InitializeComponent();
            axMsRdpClient11NotSafeForScripting1.Server = "192.168.201.10";
            axMsRdpClient11NotSafeForScripting1.UserName = "satoshigekkouga2004@gmail.com";
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings2.ClearTextPassword = "AshGreninja2004";
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings7.EnableCredSspSupport = true;
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings7.SmartSizing = true;
            axMsRdpClient11NotSafeForScripting1.DesktopWidth = 1920/5;
            axMsRdpClient11NotSafeForScripting1.DesktopHeight = 1080/5;
            axMsRdpClient11NotSafeForScripting1.Connect();
            abc = axMsRdpClient11NotSafeForScripting1;
        }

        private void _SizeChanged(object sender, EventArgs e)
        {
            //axMsRdpClient11NotSafeForScripting1.DesktopWidth = this.Width;
            //axMsRdpClient11NotSafeForScripting1.DesktopHeight = this.Height;
        }
    }
}
