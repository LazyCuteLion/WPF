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
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            //for (int i = 0; i < 3; i++)
            //{
            //await Task.Delay(500);
            //var b = Interaction.GetBehaviors(sender as UIElement).FirstOrDefault() as PngSequenceBehavior;
            //b.Begin();
            //b.Clear();
            //(sender as FrameworkElement).Visibility = Visibility.Hidden;
            //    await Task.Delay(5000);
            //    (sender as FrameworkElement).Visibility = Visibility.Visible;
            //}
        }


        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            box.Children.Clear();
            base.OnMouseRightButtonDown(e);
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var animation = ImageExt.GetSequenceFrameAnimation(sender as Image);
            if (animation.IsPaused == true)
                animation.Resume();
            else if (animation.IsPaused == false)
            {
                if (animation.IsComplete)
                    animation.Begin();
                else
                    animation.Pause();
            }
            else
                animation.Begin();
        }

        private void SequenceFrameAnimation_Completed(object sender, EventArgs e)
        {
            var animation = ImageExt.GetSequenceFrameAnimation(sender as Image);
            animation.Dispose();
        }
    }
}
