using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows.Controls
{
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.RegisterAttached(
            "SyncHorizontalOffset", typeof(double), typeof(ScrollViewerHelper),
            new PropertyMetadata(default(double), new PropertyChangedCallback((s, e) =>
            {
                if (s is ScrollViewer scrollViewer)
                {
                    var v = (double)e.NewValue;
                    if (v < 0)
                        v = 0;
                    else if (v > scrollViewer.ScrollableWidth)
                        v = scrollViewer.ScrollableWidth;
                    scrollViewer.ScrollToHorizontalOffset(v);
                }
            })));

        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached(
           "SyncVerticalOffset", typeof(double), typeof(ScrollViewerHelper),
           new PropertyMetadata(default(double), new PropertyChangedCallback((s, e) =>
           {
               if (s is ScrollViewer scrollViewer)
               {
                   var v = (double)e.NewValue;
                   if (v < 0)
                       v = 0;
                   else if (v > scrollViewer.ScrollableHeight)
                       v = scrollViewer.ScrollableHeight;

                   scrollViewer.ScrollToVerticalOffset(v);
               }
           })));

        public static Task SmoothScrollToHorizontalOffset(this ScrollViewer scrollViewer, double offset, Duration duration)
        {
            var tcs = new TaskCompletionSource<bool>();
            var ani = new DoubleAnimation()
            {
                From = scrollViewer.HorizontalOffset,
                To = offset,
                Duration = duration,
                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
            };
            ani.Completed += (s, e) =>
            {
                scrollViewer.IsHitTestVisible = true;
                tcs.SetResult(true);
            };
            scrollViewer.IsHitTestVisible = false;
            scrollViewer.BeginAnimation(HorizontalOffsetProperty, ani);
            return tcs.Task;
        }

        public static Task SmoothScrollToHorizontalOffset(this ScrollViewer scrollViewer, double offset, int milliseconds = 200)
        {
            return scrollViewer.SmoothScrollToHorizontalOffset(offset, TimeSpan.FromMilliseconds(milliseconds));
        }

        public static Task SmoothScrollToVerticalOffset(this ScrollViewer scrollViewer, double offset, Duration duration)
        {
            var tcs = new TaskCompletionSource<bool>();
            var ani = new DoubleAnimation()
            {
                From = scrollViewer.VerticalOffset,
                To = offset,
                Duration = duration,
                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
            };
            ani.Completed += (s, e) =>
            {
                scrollViewer.IsHitTestVisible = true;
                tcs.SetResult(true);
            };
            scrollViewer.IsHitTestVisible = false;
            scrollViewer.BeginAnimation(VerticalOffsetProperty, ani);
            return tcs.Task;
        }

        public static Task SmoothScrollToVerticalOffset(this ScrollViewer scrollViewer, double offset, int milliseconds = 200)
        {
            return scrollViewer.SmoothScrollToVerticalOffset(offset, TimeSpan.FromMilliseconds(milliseconds));
        }


        public static bool GetUseMouse(DependencyObject obj)
        {
            return (bool)obj.GetValue(UseMouseProperty);
        }

        public static void SetUseMouse(DependencyObject obj, bool value)
        {
            obj.SetValue(UseMouseProperty, value);
        }

        public static readonly DependencyProperty UseMouseProperty =
            DependencyProperty.RegisterAttached("UseMouse", typeof(bool), typeof(ScrollViewerHelper),
                                               new PropertyMetadata(false, new PropertyChangedCallback((s, e) =>
                                               {
                                                   if (e.NewValue.Equals(true))
                                                   {
                                                       if (s is ScrollViewer scrollViewer)
                                                       {
                                                           Load(scrollViewer);
                                                       }
                                                       else
                                                           (s as FrameworkElement).Loaded += OnLoaded;
                                                   }
                                                   else
                                                   {
                                                       if (s is ScrollViewer scrollViewer)
                                                       {
                                                       }
                                                       else
                                                           scrollViewer = s.FindChild<ScrollViewer>();

                                                       if (scrollViewer != null)
                                                       {
                                                           scrollViewer.PreviewMouseLeftButtonDown -= ScrollViewer_MouseLeftButtonDown;
                                                           scrollViewer.PreviewMouseMove -= ScrollViewer_MouseMove;
                                                           scrollViewer.PreviewMouseLeftButtonUp -= ScrollViewer_MouseLeftButtonUp;
                                                           scrollViewer.MouseLeave -= ScrollViewer_MouseLeave;
                                                           if (mouseData.ContainsKey(scrollViewer.GetHashCode()))
                                                               mouseData.Remove(scrollViewer.GetHashCode());
                                                       }
                                                   }
                                               })));

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = (sender as UIElement).FindChild<ScrollViewer>();
            Load(scrollViewer);
        }

        private static void Load(ScrollViewer scrollViewer)
        {
            if (scrollViewer != null)
            {
                if (SystemParameters.IsTabletPC)
                {
                    scrollViewer.PanningMode = PanningMode.None;
                    Stylus.SetIsPressAndHoldEnabled(scrollViewer, false);
                }
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                scrollViewer.PreviewMouseLeftButtonDown += ScrollViewer_MouseLeftButtonDown;
                scrollViewer.PreviewMouseMove += ScrollViewer_MouseMove;
                scrollViewer.PreviewMouseLeftButtonUp += ScrollViewer_MouseLeftButtonUp;
                scrollViewer.MouseLeave += ScrollViewer_MouseLeave;
            }
        }

        class MouseData
        {
            public int Id { get; private set; }

            public int Timestamp { get; set; }

            public Vector Move { get; set; } = new Vector(0, 0);

            public Point Last { get; set; }

            public MouseData(int id)
            {
                this.Id = id;
            }
        }

        static readonly Dictionary<int, MouseData> mouseData = new Dictionary<int, MouseData>();

        private static void ScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
            var scrollViewer = sender as ScrollViewer;
            var id = scrollViewer.GetHashCode();
            var p = e.GetPosition(scrollViewer);
            if (mouseData.TryGetValue(id, out MouseData d))
            {
                d.Timestamp = e.Timestamp;
                d.Last = p;
            }
            else
            {
                mouseData[id] = new MouseData(id)
                {
                    Timestamp = e.Timestamp,
                    Last = p,
                };
            }
            e.Handled = true;
        }

        private static void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var scrollViewer = sender as ScrollViewer;
                var id = scrollViewer.GetHashCode();
                var p = e.GetPosition(scrollViewer);
                if (p.X < 0 || p.X > scrollViewer.ViewportWidth || p.Y < 0 || p.Y > scrollViewer.ViewportHeight)
                {
                    ScrollViewer_MouseLeftButtonUp(sender, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, Input.MouseButton.Left));
                    return;
                }

                if (mouseData.TryGetValue(id, out MouseData d))
                {
                    double x = 0, y = 0;
                    if (scrollViewer.ScrollableWidth > 0)
                    {
                        var move = p.X - d.Last.X;
                        var length = scrollViewer.HorizontalOffset - move;

                        if (length < 0)
                            length = 0;
                        else if (length > scrollViewer.ScrollableWidth)
                            length = scrollViewer.ScrollableWidth;

                        scrollViewer.ScrollToHorizontalOffset(length);

                        x = d.Move.X + move;
                    }
                    if (scrollViewer.ScrollableHeight > 0)
                    {
                        var move = p.Y - d.Last.Y;
                        var length = scrollViewer.VerticalOffset - move;
                        if (length < 0)
                            length = 0;
                        else if (length > scrollViewer.ScrollableHeight)
                            length = scrollViewer.ScrollableHeight;

                        scrollViewer.ScrollToVerticalOffset(length);
                        y = d.Move.Y + move;
                    }
                    d.Last = p;
                    d.Move = new Vector(x, y);
                }
                else
                {
                    mouseData[id] = new MouseData(id)
                    {
                        Timestamp = e.Timestamp,
                        Last = p
                    };
                }

                e.Handled = true;
            }
        }

        private static void ScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
            try
            {
                var scrollViewer = sender as ScrollViewer;
                var p = e.GetPosition(scrollViewer);
                var id = scrollViewer.GetHashCode();
                if (mouseData.TryGetValue(id, out MouseData d))
                {
                    var time = e.Timestamp - d.Timestamp;
                    //Console.WriteLine(time);
                    if (time < 600)
                    {
                        if (scrollViewer.ScrollableWidth > 0)
                        {
                            var speed = d.Move.X / time;
                            scrollViewer.BeginAnimation(HorizontalOffsetProperty, new DoubleAnimation()
                            {
                                From = scrollViewer.HorizontalOffset,
                                By = speed * -500,
                                Duration = TimeSpan.FromMilliseconds(500),
                                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
                            });
                        }

                        if (scrollViewer.ScrollableHeight > 0)
                        {
                            var speed = d.Move.Y / time;
                            scrollViewer.BeginAnimation(VerticalOffsetProperty, new DoubleAnimation()
                            {
                                From = scrollViewer.VerticalOffset,
                                By = speed * -500,
                                Duration = TimeSpan.FromMilliseconds(500),
                                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
                            });
                        }

                        mouseData.Remove(id);
                    }
                }
                e.Handled = true;
            }
            catch { }
        }

        private static void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ScrollViewer_MouseLeftButtonUp(sender, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, Input.MouseButton.Left));
            }
        }
    }


}
