using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    /// FlipView.xaml 的交互逻辑
    /// </summary>
    public partial class FlipView : TreeView
    {
        public FlipView()
        {
            InitializeComponent();
        }

        public Duration Duration
        {
            get { return (Duration)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(Duration), typeof(FlipView), new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200))));


        public event EventHandler<int> IndexChanged;

        private int _index = 0;
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    this.Scroll().ContinueWith(t =>
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (this.ItemContainerGenerator.ContainerFromIndex(_index) is TreeViewItem item)
                                IndexChanged?.Invoke(VisualTreeHelper.GetChild(item, 0), _index);
                        });
                    });
                }
            }
        }

        private Task Scroll()
        {
            if (orientation == Orientation.Horizontal)
                return scrollViewer.SmoothScrollToHorizontalOffset(scrollViewer.ViewportWidth * _index, Duration);
            else
                return scrollViewer.SmoothScrollToVerticalOffset(scrollViewer.ViewportHeight * _index, Duration);
        }

        ScrollViewer scrollViewer;
        Orientation orientation = Orientation.Horizontal;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var style = this.ItemContainerStyle;
            if (style == null)
            {
                style = this.Resources["BaseItemStyle"] as Style;
            }
            else
            {
                style.BasedOn = this.Resources["BaseItemStyle"] as Style;
            }

            var b = VisualTreeHelper.GetChild(root, 0) as Border;
            scrollViewer = b.Child as ScrollViewer;
            scrollViewer.PanningMode = PanningMode.None;

            try
            {
                dynamic panel = this.ItemsPanel.LoadContent();
                orientation = panel.Orientation;
            }
            catch { }
           
        }

        int startTime;
        double start, last;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //Console.WriteLine("OnPreviewMouseLeftButtonDown");
            startTime = e.Timestamp;
            var p = e.GetPosition(this);
            if (orientation == Orientation.Horizontal)
            {
                start = p.X;
                last = p.X;
            }
            else
            {
                start = p.Y;
                last = p.Y;
            }
            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == Input.MouseButtonState.Pressed)
            {
                try
                {
                    var p = e.GetPosition(this);

                    if (p.X < 0 || p.X > this.ActualWidth || p.Y < 0 || p.Y > this.ActualHeight)
                    {
                        OnPreviewMouseLeftButtonUp(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, Input.MouseButton.Left));
                        return;
                    }

                    if (double.IsNaN(last) || double.IsNaN(start))
                    {
                        startTime = e.Timestamp;
                        if (orientation == Orientation.Horizontal)
                        {
                            start = p.X;
                            last = p.X;
                        }
                        else
                        {
                            start = p.Y;
                            last = p.Y;
                        }
                        return;
                    }
                    if (orientation == Orientation.Horizontal)
                    {
                        var x = p.X;
                        var length = scrollViewer.HorizontalOffset - x + last;
                        if (length < 0)
                            length = 0;
                        else if (length > scrollViewer.ScrollableWidth)
                            length = scrollViewer.ScrollableWidth;

                        scrollViewer.ScrollToHorizontalOffset(length);
                        last = x;
                    }
                    else
                    {
                        var y = p.Y;
                        var length = scrollViewer.VerticalOffset - y + last;
                        if (length < 0)
                            length = 0;
                        else if (length > scrollViewer.ScrollableHeight)
                            length = scrollViewer.ScrollableHeight;
                        scrollViewer.ScrollToVerticalOffset(length);
                        last = y;
                    }
                    e.Handled = true;
                }
                catch { }
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (this.Index > 0)
                    this.Index--;
            }
            else
            {
                if (this.Index < this.Items.Count - 1)
                    this.Index++;
            }
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;
                OnPreviewMouseLeftButtonUp(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, Input.MouseButton.Left));
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            //Console.WriteLine("OnPreviewMouseLeftButtonUp");
            try
            {
                var p = e.GetPosition(this);
                double value = orientation == Orientation.Horizontal ? p.X : p.Y;

                if (Math.Abs(value - start) < 4)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        scrollViewer.ScrollToHorizontalOffset(scrollViewer.ViewportWidth * Index);
                    }
                    else
                    {
                        scrollViewer.ScrollToVerticalOffset(scrollViewer.ViewportHeight * Index);
                    }
                    return;
                }

                if (e.Timestamp - startTime < 200)
                {
                    if (value < start)
                    {
                        if (Index < this.Items.Count - 1)
                        {
                            Index++;
                        }
                    }
                    else
                    {
                        if (Index > 0)
                        {
                            Index--;
                        }
                    }
                }
                else
                {
                    var pageSize = orientation == Orientation.Horizontal
                                         ? scrollViewer.ViewportWidth / 2.0
                                         : scrollViewer.ViewportHeight / 2.0;
                    var length = orientation == Orientation.Horizontal
                                      ? p.X - start
                                      : p.Y - start;

                    if (length > pageSize)
                    {
                        if (Index > 0)
                        {
                            Index--;
                        }
                    }
                    else if (length < 0 - pageSize)
                    {
                        if (Index < this.Items.Count - 1)
                        {
                            Index++;
                        }
                    }
                    else
                    {
                        this.Scroll();
                    }
                }
                start = double.NaN;
                last = double.NaN;
                startTime = e.Timestamp;
                e.Handled = true;
            }
            catch { }
        }

    }

}
