using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

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

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            stopwatch = new Stopwatch();
            stopwatch.Start();

        }


        int fpsCount = 0;
        Stopwatch stopwatch;

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            fpsCount++;
            if (stopwatch.ElapsedMilliseconds >= 1000)
            {
                tbFPS.Text = fpsCount.ToString();
                fpsCount = 0;
                stopwatch.Restart();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                var index = int.Parse(frame.Content.ToString().Substring(9));

                switch (e.Key)
                {
                    case Key.Left:
                        frame.Navigate(new Uri($"page{--index}.xaml", UriKind.Relative));
                        break;
                    case Key.Right:
                        frame.Navigate(new Uri($"page{++index}.xaml", UriKind.Relative));
                        break;
                    case Key.NumPad1:
                    case Key.NumPad2:
                    case Key.NumPad3:
                    case Key.NumPad4:
                    case Key.NumPad5:
                    case Key.NumPad6:
                    case Key.NumPad7:
                    case Key.NumPad8:
                    case Key.NumPad9:
                        frame.Navigate(new Uri($"page{e.Key.ToString().Substring(6)}.xaml", UriKind.Relative));
                        break;
                }

            }
            catch { }

            base.OnKeyDown(e);
        }

        
    }
}
