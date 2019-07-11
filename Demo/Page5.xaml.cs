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
    /// Page5.xaml 的交互逻辑
    /// </summary>
    public partial class Page5 : Page
    {
        public Page5()
        {
            InitializeComponent();
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("item mousedown");
        }

        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Rectangle_PreviewMouseDown");
        }

        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Rectangle_MouseUp");
        }

        private void Rectangle_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Rectangle_PreviewMouseUp");
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var view = sender as ItemsControl;
            var list = new List<object>();
            for (int i = 0; i < 10000; i++)
            {
                list.Add(new { Index = i, Content = i % 2 == 0 ? "Blue" : "Green" });
            }
            view.ItemsSource = list;
        }

        private void FlipView_IndexChanged(object sender, int index)
        {
            Console.WriteLine(index + "," + sender);
        }


        private void FlipView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as FlipView).Index = 10;
        }
    }
}
