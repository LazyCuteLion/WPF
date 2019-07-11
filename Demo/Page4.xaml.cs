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
    /// Page4.xaml 的交互逻辑
    /// </summary>
    public partial class Page4 : Page
    {
        public Page4()
        {
            InitializeComponent();

            
        }

        private void lb_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var list = new object[]
           {
                new { Color=Brushes.Yellow,Id=1},
                new { Color=Brushes.Blue,Id=2},
                new { Color=Brushes.Green,Id=3},
                new { Color=Brushes.Black,Id=4},
                new { Color=Brushes.Yellow,Id=5},
                new { Color=Brushes.Blue,Id=6},
                new { Color=Brushes.Green,Id=7},
                new { Color=Brushes.Black,Id=8}
           };
            lb.ItemsSource = list;
        }
    }
}
