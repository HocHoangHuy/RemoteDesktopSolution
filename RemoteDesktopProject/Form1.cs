using AxMSTSCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDesktopProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            axMsRdpClient11NotSafeForScripting1.Server = "192.168.201.10";
            axMsRdpClient11NotSafeForScripting1.UserName = "satoshigekkouga2004@gmail.com";
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings2.ClearTextPassword = "AshGreninja2004";
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings7.EnableCredSspSupport = true;
            axMsRdpClient11NotSafeForScripting1.AdvancedSettings7.SmartSizing = true;
            axMsRdpClient11NotSafeForScripting1.DesktopWidth = 1280;
            axMsRdpClient11NotSafeForScripting1.DesktopHeight = 720;
            axMsRdpClient11NotSafeForScripting1.Connect();
        }

        private void axMsRdpClient11NotSafeForScripting1_SizeChanged(object sender, EventArgs e)
        {
            //axMsRdpClient11NotSafeForScripting1.SizeChanged -= axMsRdpClient11NotSafeForScripting1_SizeChanged;
            //double RemoteScreenRatio = axMsRdpClient11NotSafeForScripting1.DesktopWidth * 1.0 / axMsRdpClient11NotSafeForScripting1.DesktopHeight;
            //double WidthDifference = axMsRdpClient11NotSafeForScripting1.Width- axMsRdpClient11NotSafeForScripting1.DesktopWidth;
            //double HeightDifference = axMsRdpClient11NotSafeForScripting1.Height - axMsRdpClient11NotSafeForScripting1.DesktopHeight;
            //if (double.Abs(WidthDifference) > double.Abs(HeightDifference))
            //{
            //    axMsRdpClient11NotSafeForScripting1.DesktopHeight = (int)axMsRdpClient11NotSafeForScripting1.Height;





            //    axMsRdpClient11NotSafeForScripting1.DesktopWidth = (int)(axMsRdpClient11NotSafeForScripting1.Height * RemoteScreenRatio);
            //}
            //else
            //{
            //    axMsRdpClient11NotSafeForScripting1.DesktopWidth = (int)axMsRdpClient11NotSafeForScripting1.Width;
            //    axMsRdpClient11NotSafeForScripting1.DesktopHeight = (int)(axMsRdpClient11NotSafeForScripting1.Width / RemoteScreenRatio);
            //}

            //axMsRdpClient11NotSafeForScripting1.SizeChanged += axMsRdpClient11NotSafeForScripting1_SizeChanged;
        }
    }
}
