using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Animation;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.ShellProperties;

namespace Demo
{
    /// <summary>
    /// Page3.xaml 的交互逻辑
    /// </summary>
    public partial class Page3 : Page
    {
        public Page3()
        {
            InitializeComponent();

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //box.Children.Clear();
            //for (int i = 0; i < 100; i += 10)
            //{
            //    box.Children.Add(new Rectangle()
            //    {
            //        Fill = new SolidColorBrush(Color.FromRgb((byte)(200 - i), (byte)(100 - i), (byte)i))
            //    });
            //}
        }
    }
}

