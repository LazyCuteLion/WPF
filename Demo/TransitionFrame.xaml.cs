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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    /// TransitionFrame.xaml 的交互逻辑
    /// </summary>
    public partial class TransitionFrame : Frame
    {
        public TransitionFrame()
        {
            InitializeComponent();
            _sb = new Storyboard();
            _sb.Completed += OnCompleted;
        }

        public TransitionMode TransitionMode
        {
            get { return (TransitionMode)GetValue(TransitionModeProperty); }
            set { SetValue(TransitionModeProperty, value); }
        }
        public static readonly DependencyProperty TransitionModeProperty =
            DependencyProperty.Register("TransitionMode", typeof(TransitionMode), typeof(TransitionFrame),
                new PropertyMetadata(TransitionMode.Random));

        public TransitionTarget TransitionTarget
        {
            get { return (TransitionTarget)GetValue(TransitionTargetProperty); }
            set { SetValue(TransitionTargetProperty, value); }
        }
        public static readonly DependencyProperty TransitionTargetProperty =
            DependencyProperty.Register("TransitionTarget", typeof(TransitionTarget), typeof(TransitionFrame),
                new PropertyMetadata(TransitionTarget.NewPage));

        public Duration Duration
        {
            get { return (Duration)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(Duration), typeof(TransitionFrame), 
                new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(500))));

        /// <summary>
        /// 当TransitionTarget != TransitionTarget.OldPage时起作用
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static TransitionMode GetInTransition(Page obj)
        {
            return (TransitionMode)obj.GetValue(InTransitionProperty);
        }
        public static void SetInTransition(Page obj, TransitionMode value)
        {
            obj.SetValue(InTransitionProperty, value);
        }
        public static readonly DependencyProperty InTransitionProperty =
            DependencyProperty.RegisterAttached("InTransition", typeof(TransitionMode), typeof(TransitionFrame), 
                new PropertyMetadata(TransitionMode.Inherit));

        /// <summary>
        /// 当TransitionTarget != TransitionTarget.NewPage时起作用
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static TransitionMode GetOutTransition(Page obj)
        {
            return (TransitionMode)obj.GetValue(OutTransitionProperty);
        }
        public static void SetOutTransition(Page obj, TransitionMode value)
        {
            obj.SetValue(OutTransitionProperty, value);
        }
        public static readonly DependencyProperty OutTransitionProperty =
            DependencyProperty.RegisterAttached("OutTransition", typeof(TransitionMode), typeof(TransitionFrame), 
                new PropertyMetadata(TransitionMode.Inherit));


        private Image _thumb;
        private ContentPresenter _content;
        private Storyboard _sb;

        private void TransitionIn(TransitionMode mode)
        {
            if (mode == TransitionMode.None)
                return;
            var modes = mode.ToString().Split(',').Select(m => (TransitionMode)Enum.Parse(typeof(TransitionMode), m)).ToArray();
            Console.WriteLine("int:{0} {1}", (int)mode, mode.ToString());


            Panel.SetZIndex(_thumb, -1);

            if (modes.Contains(TransitionMode.Fade))
            {
                var ani = new DoubleAnimation(0, 1, this.Duration);
                Storyboard.SetTarget(ani, _content);
                Storyboard.SetTargetProperty(ani, new PropertyPath(OpacityProperty));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.TranslateFromLeft))
            {
                var ani = new DoubleAnimation(0 - this.ActualWidth, 0, this.Duration);
                Storyboard.SetTarget(ani, _content);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.X)"));
                _sb.Children.Add(ani);
            }
            else if (modes.Contains(TransitionMode.TranslateFromRight))
            {
                var ani = new DoubleAnimation(this.ActualWidth, 0, this.Duration);
                Storyboard.SetTarget(ani, _content);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.X)"));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.TranslateFromTop))
            {
                var ani = new DoubleAnimation(0 - this.ActualHeight, 0, this.Duration);
                Storyboard.SetTarget(ani, _content);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
                _sb.Children.Add(ani);
            }
            else if (modes.Contains(TransitionMode.TranslateFromBottom))
            {
                var ani = new DoubleAnimation(this.ActualHeight, 0, this.Duration);
                Storyboard.SetTarget(ani, _content);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.Scale))
            {
                var ani1 = new DoubleAnimation(0, 1, this.Duration);
                Storyboard.SetTarget(ani1, _content);
                Storyboard.SetTargetProperty(ani1, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));
                _sb.Children.Add(ani1);
                var ani2 = new DoubleAnimation(0, 1, this.Duration);
                Storyboard.SetTarget(ani2, _content);
                Storyboard.SetTargetProperty(ani2, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));
                _sb.Children.Add(ani2);
            }

        }

        private void TransitionOut(TransitionMode mode)
        {
            if (mode == TransitionMode.None)
                return;
            var modes = mode.ToString().Split(',').Select(m => (TransitionMode)Enum.Parse(typeof(TransitionMode), m)).ToArray();
            Console.WriteLine("int:{0} {1}", (int)mode, mode.ToString());

            Panel.SetZIndex(_thumb, 1);

            if (modes.Contains(TransitionMode.Fade))
            {
                var ani = new DoubleAnimation(1, 0, this.Duration);
                Storyboard.SetTarget(ani, _thumb);
                Storyboard.SetTargetProperty(ani, new PropertyPath(OpacityProperty));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.TranslateFromLeft))
            {
                var ani = new DoubleAnimation(0, this.ActualWidth, this.Duration);
                Storyboard.SetTarget(ani, _thumb);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.X)"));
                _sb.Children.Add(ani);
            }
            else if (modes.Contains(TransitionMode.TranslateFromRight))
            {
                var ani = new DoubleAnimation(0, 0 - this.ActualWidth, this.Duration);
                Storyboard.SetTarget(ani, _thumb);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.X)"));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.TranslateFromTop))
            {
                var ani = new DoubleAnimation(0, this.ActualHeight, this.Duration);
                Storyboard.SetTarget(ani, _thumb);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
                _sb.Children.Add(ani);
            }
            else if (modes.Contains(TransitionMode.TranslateFromBottom))
            {
                var ani = new DoubleAnimation(0, 0 - this.ActualHeight, this.Duration);
                Storyboard.SetTarget(ani, _thumb);
                Storyboard.SetTargetProperty(ani, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
                _sb.Children.Add(ani);
            }

            if (modes.Contains(TransitionMode.Scale))
            {
                var ani1 = new DoubleAnimation(1, 0, this.Duration);
                Storyboard.SetTarget(ani1, _thumb);
                Storyboard.SetTargetProperty(ani1, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));
                _sb.Children.Add(ani1);
                var ani2 = new DoubleAnimation(1, 0, this.Duration);
                Storyboard.SetTarget(ani2, _thumb);
                Storyboard.SetTargetProperty(ani2, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));
                _sb.Children.Add(ani2);
            }
        }

        private TransitionMode GetRandomTransitionMode()
        {
            var modes = new int[]
                {
                    2,4,8,16,32,64,
                    6,10,18,34,66,
                    12,20,36,68,
                    40,72,
                    48,80
                };
            var rd = new Random();
            return (TransitionMode)modes[rd.Next(0, modes.Length)];
        }

        private void TransitionFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (this.TransitionMode == TransitionMode.None)
                return;
            if (this.Content != null)
            {
                var page = this.Content as Page;
                var rtb = new RenderTargetBitmap((int)page.ActualWidth, (int)page.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(page);
                rtb.Freeze();
                _thumb.Source = rtb;
                _thumb.Visibility = Visibility.Visible;

                _sb.Children.Clear();

                var outMode = TransitionFrame.GetOutTransition(page);
                if (outMode == TransitionMode.Inherit)
                {
                    if (TransitionMode == TransitionMode.Random)
                    {
                        outMode = GetRandomTransitionMode();
                    }
                    else
                    {
                        outMode = this.TransitionMode;
                    }
                }

                if (outMode != TransitionMode.None)
                {
                    if (this.TransitionTarget != TransitionTarget.NewPage)
                        TransitionOut(outMode);
                }
            }
        }

        private async void TransitionFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (this.TransitionMode == TransitionMode.None)
                return;

            if (_content == null)
            {
                while (true)
                {
                    _thumb = GetChildObject<Image>(this, "PART_FrameThumb");
                    _content = GetChildObject<ContentPresenter>(this, "PART_FrameCP");
                    if (_thumb != null && _content != null)
                        break;
                    await Task.Delay(100);
                }
            }

            var inMode = GetInTransition(e.Content as Page);
            if (inMode == TransitionMode.Inherit)
            {
                if (TransitionMode == TransitionMode.Random)
                {
                    inMode = GetRandomTransitionMode();
                }
                else
                {
                    inMode = this.TransitionMode;
                }
            }

            if (inMode != TransitionMode.None)
            {
                if (this.TransitionTarget != TransitionTarget.OldPage)
                    TransitionIn(inMode);
            }

            _sb?.Stop();
            //_sb?.Remove();
            _sb?.Begin();
        }

        private void OnCompleted(object sender, EventArgs e)
        {
            _thumb.Source = null;
            _thumb.Visibility = Visibility.Hidden;
        }

        private T GetChildObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject child = null;
            T grandChild = null;
            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)child;
                }
                else
                {
                    // 在下一级中没有找到指定名字的子控件，就再往下一级找
                    grandChild = GetChildObject<T>(child, name);
                    if (grandChild != null)
                        return grandChild;
                }
            }
            return null;
        }
    }

    [Flags]
    public enum TransitionMode
    {
        Inherit = -1,
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        /// <summary>
        /// 随机
        /// </summary>
        Random = 1,

        /// <summary>
        /// 淡入淡出
        /// </summary>
        Fade = 2,

        /// <summary>
        /// 缩放
        /// </summary>
        Scale = 4,

        /// <summary>
        /// 从左往右
        /// </summary>
        TranslateFromLeft = 8,
        /// <summary>
        /// 从右往左
        /// </summary>
        TranslateFromRight = 16,

        /// <summary>
        /// 从上到下
        /// </summary>
        TranslateFromTop = 32,
        /// <summary>
        /// 从下到上
        /// </summary>
        TranslateFromBottom = 64,

    }

    [Flags]
    public enum TransitionTarget
    {
        NewPage = 2,
        OldPage = 4,
        All = NewPage | OldPage
    }
}
