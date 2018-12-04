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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var index = int.Parse(frame.Content.ToString().Substring(9));
            switch (e.Key)
            {
                case Key.Left:
                    if (index > 1)
                        index -= 1;
                    frame.Navigate(new Uri($"page{index}.xaml", UriKind.Relative));
                    break;
                case Key.Right:
                    if (index < 3)
                        index += 1;
                    frame.Navigate(new Uri($"page{index}.xaml", UriKind.Relative));
                    break;
            }
          
            base.OnKeyDown(e);
        }
    }
}
