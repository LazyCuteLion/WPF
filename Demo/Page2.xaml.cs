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

namespace Demo
{
    /// <summary>
    /// Page2.xaml 的交互逻辑
    /// </summary>
    public partial class Page2 : Page
    {
        public Page2()
        {
            InitializeComponent();
        }

        private async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(5000);
            (sender as FrameworkElement).Visibility = Visibility.Visible;
            //await Task.Delay(5000);
            //(sender as FrameworkElement).Visibility = Visibility.Collapsed;
        }
    }
}
