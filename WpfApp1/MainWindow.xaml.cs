using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Form1 form1 = new Form1();
            //form1.Show();
            windowsFormsHost.Child = form1.abc;

        }
    }
}