using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace System.Windows.Input
{
    /// <summary>
    /// 多点触摸 附加属性
    /// </summary>
    public static class MultiTouchHelper
    {
        private readonly static Dictionary<int, double> _currentScale = new Dictionary<int, double>();
        private readonly static Dictionary<int, Storyboard> _recoverStoryboard = new Dictionary<int, Storyboard>();

        public static ManipulationModes GetManipulationMode(UIElement obj)
        {
            return (ManipulationModes)obj.GetValue(ManipulationModeProperty);
        }
        public static void SetManipulationMode(UIElement obj, ManipulationModes value)
        {
            obj.SetValue(ManipulationModeProperty, value);
        }
        public static readonly DependencyProperty ManipulationModeProperty =
            DependencyProperty.RegisterAttached("ManipulationMode", typeof(ManipulationModes), typeof(MultiTouchHelper), new PropertyMetadata(ManipulationModes.None));



        public static double GetMaximumScale(DependencyObject obj)
        {
            return (double)obj.GetValue(MaximumScaleProperty);
        }
        public static void SetMaximumScale(DependencyObject obj, double value)
        {
            obj.SetValue(MaximumScaleProperty, value);
        }
        public static readonly DependencyProperty MaximumScaleProperty =
            DependencyProperty.RegisterAttached("MaximumScale", typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(2.0));



        public static double GetMinimumScale(DependencyObject obj)
        {
            return (double)obj.GetValue(MinimumScaleProperty);
        }
        public static void SetMinimumScale(DependencyObject obj, double value)
        {
            obj.SetValue(MinimumScaleProperty, value);
        }
        public static readonly DependencyProperty MinimumScaleProperty =
            DependencyProperty.RegisterAttached("MinimumScale", typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(1.0));


        public static TimeSpan GetWaitForRecover(UIElement obj)
        {
            return (TimeSpan)obj.GetValue(WaitForRecoverProperty);
        }
        public static void SetWaitForRecover(UIElement obj, TimeSpan value)
        {
            obj.SetValue(WaitForRecoverProperty, value);
        }
        public static readonly DependencyProperty WaitForRecoverProperty =
            DependencyProperty.RegisterAttached("WaitForRecover", typeof(TimeSpan), typeof(MultiTouchHelper), new PropertyMetadata(TimeSpan.Zero));


        public static bool GetIsContenter(Panel obj)
        {
            return (bool)obj.GetValue(IsContenterProperty);
        }
        public static void SetIsContenter(Panel obj, bool value)
        {
            obj.SetValue(IsContenterProperty, value);
        }
        public static readonly DependencyProperty IsContenterProperty =
            DependencyProperty.RegisterAttached("IsContenter", typeof(bool), typeof(MultiTouchHelper),
                new PropertyMetadata(false, new PropertyChangedCallback((s, e) =>
                {
                    var contenter = s as FrameworkElement;
                    if ((bool)e.NewValue == true)
                    {
                        contenter.ManipulationStarting += OnManipulationStarting;
                        contenter.ManipulationDelta += OnManipulationDelta;
                        contenter.ManipulationInertiaStarting += OnManipulationInertiaStarting;
                        contenter.ManipulationCompleted += OnManipulationCompleted;
                        //如果系统不支持触摸，则添加鼠标事件
                        if (!SystemParameters.IsTabletPC)
                        {
                            contenter.MouseWheel += OnMouseWheel;
                            contenter.PreviewMouseDown += OnMouseDown;//Preview
                            contenter.PreviewMouseMove += OnMouseMove;
                            contenter.PreviewMouseUp += OnMouseUp;
                        }
                    }
                    else
                    {
                        contenter.ManipulationStarting -= OnManipulationStarting;
                        contenter.ManipulationDelta -= OnManipulationDelta;
                        contenter.ManipulationInertiaStarting -= OnManipulationInertiaStarting;
                        contenter.ManipulationCompleted -= OnManipulationCompleted;
                        if (!SystemParameters.IsTabletPC)
                        {
                            contenter.MouseWheel -= OnMouseWheel;
                            contenter.PreviewMouseDown -= OnMouseDown;
                            contenter.PreviewMouseMove -= OnMouseMove;
                            contenter.PreviewMouseUp -= OnMouseUp;
                        }
                    }
                })));

        private static void Initialize(FrameworkElement element)
        {
            var key = element.GetHashCode();
            if (_recoverStoryboard.ContainsKey(key))
            {
                _recoverStoryboard[key].Stop();
                //(element.RenderTransform as MatrixTransform).BeginAnimation(MatrixTransform.MatrixProperty, null);
                _recoverStoryboard.Remove(key);
            }
            if (!_currentScale.ContainsKey(key))
            {
                _currentScale[key] = 1.0;
            }
            BringToFront(element);
            SetMatrixTransform(element);
            element.Opacity = 0.7;
        }

        private static void CreateRecoverStoryboard(FrameworkElement element)
        {
            var delay = GetWaitForRecover(element);
            if (delay > TimeSpan.Zero)
            {
                var key = element.GetHashCode();
                var mt = (element.RenderTransform as MatrixTransform);
                var speed = 10 * 96.0 / 1000.0;//速度
                var time = Math.Sqrt(Math.Pow(mt.Matrix.OffsetX, 2) + Math.Pow(mt.Matrix.OffsetY, 2)) / speed;
                //time = 3000;
                var animation = new MatrixAnimation()
                {
                    From = mt.Matrix,
                    To = new Matrix(),
                    FillBehavior = FillBehavior.Stop,
                    Duration = new Duration(TimeSpan.FromMilliseconds(time)),
                    EasingFunction = new BackEase() { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(0).(1)",
                    FrameworkElement.RenderTransformProperty, MatrixTransform.MatrixProperty));

                var booleanAnimation = new BooleanAnimationUsingKeyFrames() { FillBehavior = FillBehavior.Stop };
                booleanAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame(false, TimeSpan.Zero));
                booleanAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame(true, TimeSpan.FromMilliseconds(time)));
                Storyboard.SetTarget(booleanAnimation, element);
                Storyboard.SetTargetProperty(booleanAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));

                var opacityAnimation = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(time), FillBehavior.Stop);
                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

                var s = new Storyboard() { BeginTime = delay };
                s.Children.Add(animation);
                s.Children.Add(booleanAnimation);
                s.Children.Add(opacityAnimation);
                s.Completed += (ss, ee) =>
                {
                    mt.BeginAnimation(MatrixTransform.MatrixProperty, null);
                    mt.Matrix = new Matrix();
                    _currentScale[key] = 1.0;
                    _recoverStoryboard.Remove(key);
                };
                s.Freeze();
                s.Begin();
                _recoverStoryboard[key] = s;
            }
        }

        private static void SetMatrixTransform(FrameworkElement element)
        {
            if (element.RenderTransform == Transform.Identity)
            {
                element.RenderTransform = new MatrixTransform();
            }
            else if (!(element.RenderTransform is MatrixTransform))
            {
                ScaleTransform scaleTransform = null;
                TranslateTransform translateTransform = null;
                RotateTransform rotateTransform = null;
                var mt = new Matrix();

                if (element.RenderTransform is TransformGroup group)
                {
                    scaleTransform = (ScaleTransform)group.Children.FirstOrDefault(f => f is ScaleTransform);
                    translateTransform = (TranslateTransform)group.Children.FirstOrDefault(f => f is TranslateTransform);
                    rotateTransform = (RotateTransform)group.Children.FirstOrDefault(f => f is RotateTransform);
                }
                else if (element.RenderTransform is ScaleTransform)
                {
                    scaleTransform = element.RenderTransform as ScaleTransform;
                }
                else if (element.RenderTransform is TranslateTransform)
                {
                    translateTransform = element.RenderTransform as TranslateTransform;
                }
                else if (element.RenderTransform is RotateTransform)
                {
                    rotateTransform = element.RenderTransform as RotateTransform;
                }

                if (scaleTransform != null)
                {
                    mt.ScaleAt(scaleTransform.ScaleX, scaleTransform.ScaleY, element.RenderSize.Width / 2, element.RenderSize.Height / 2);
                }
                if (translateTransform != null)
                {
                    mt.Translate(translateTransform.X, translateTransform.Y);
                }
                if (rotateTransform != null)
                {
                    mt.RotateAt(rotateTransform.Angle, element.RenderSize.Width / 2, element.RenderSize.Height / 2);
                }

                element.RenderTransform = new MatrixTransform(mt);
            }
        }

        private static int _zindex = 0;

        private static void BringToFront(FrameworkElement element)
        {
            if (_zindex == int.MaxValue)
            {
                var container = GetManipulationContainer(element);
                if (container != null)
                {
                    foreach (var item in container.Children)
                    {
                        Panel.SetZIndex(item as UIElement, 0);
                    }
                    _zindex = 0;
                }
            }

            //置于顶层
            Panel.SetZIndex(element, ++_zindex);
        }

        private static Panel GetManipulationContainer(FrameworkElement element)
        {
            if (element == null)
                return null;
            var parent = VisualTreeHelper.GetParent(element) as FrameworkElement;
            if (parent is Panel p && GetIsContenter(p))
                return p;
            else
                return GetManipulationContainer(parent);
        }

        private static FrameworkElement GetManipulationItem(FrameworkElement element)
        {
            if (element == null || element is Window)
                return null;

            if (GetManipulationMode(element) != ManipulationModes.None)
                return element;
            else
                return GetManipulationItem(VisualTreeHelper.GetParent(element) as FrameworkElement);
        }

        private static void OnManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Handled = true;
            e.Mode = ManipulationModes.All;
            //e.IsSingleTouchEnabled = false;
            e.ManipulationContainer = sender as FrameworkElement;
            var element = GetManipulationItem(e.OriginalSource as FrameworkElement);
            if (element == null)
                return;
            Initialize(element);
        }

        private static void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var element = GetManipulationItem(e.OriginalSource as FrameworkElement);

            if (element == null)
                return;

            //检测边界
            if (e.IsInertial && GetWaitForRecover(element) <= TimeSpan.Zero)
            {
                var container = e.ManipulationContainer as UIElement;
                var containingRect = new Rect(container.RenderSize);
                var location = element.TranslatePoint(new Point(), container);
                var shapeBounds = new Rect(location, element.RenderSize);
                if (!containingRect.Contains(shapeBounds))
                {
                    e.Complete();
                    e.ReportBoundaryFeedback(e.CumulativeManipulation);
                    e.Handled = true;
                    return;
                }
            }

            var matrix = (element.RenderTransform as MatrixTransform).Matrix;
            var center = new Point(element.ActualWidth / 2.0, element.ActualHeight / 2.0);
            center = matrix.Transform(center);
            var mode = GetManipulationMode(element);

            if ((mode & ManipulationModes.TranslateX) == ManipulationModes.TranslateX)
            {
                matrix.Translate(e.DeltaManipulation.Translation.X, 0);
            }
            if ((mode & ManipulationModes.TranslateY) == ManipulationModes.TranslateY)
            {
                matrix.Translate(0, e.DeltaManipulation.Translation.Y);
            }
            if ((mode & ManipulationModes.Scale) == ManipulationModes.Scale)
            {
                var currentScale = _currentScale[element.GetHashCode()];
                var scale = e.DeltaManipulation.Scale.X;
                var maximumScale = GetMaximumScale(element);
                var minimumScale = GetMinimumScale(element);
                if (scale * currentScale > maximumScale)
                {
                    scale = maximumScale / currentScale;
                }
                else if (scale * currentScale < minimumScale)
                {
                    scale = minimumScale / currentScale;
                }
                matrix.ScaleAt(scale, scale, center.X, center.Y);
                _currentScale[element.GetHashCode()] = scale * currentScale;
            }
            if ((mode & ManipulationModes.Rotate) == ManipulationModes.Rotate)
            {
                matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
            }

            (element.RenderTransform as MatrixTransform).Matrix = matrix;

        }

        private static void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
            var element = GetManipulationItem(e.OriginalSource as FrameworkElement);
            if (element == null)
                return;
            CreateRecoverStoryboard(element);
            element.Opacity = 1.0;
        }

        private static void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            #region 设置惯性效果

            //e.TranslationBehavior = new InertiaTranslationBehavior()
            //{
            //    InitialVelocity = e.InitialVelocities.LinearVelocity,
            //    DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0)
            //};

            //e.RotationBehavior = new InertiaRotationBehavior()
            //{
            //    InitialVelocity = e.InitialVelocities.AngularVelocity,
            //    DesiredDeceleration = 720 / (1000.0 * 1000.0)
            //};

            //e.ExpansionBehavior = new InertiaExpansionBehavior()
            //{
            //    InitialVelocity = e.InitialVelocities.ExpansionVelocity,
            //    DesiredDeceleration = 0.1 * 96.0 / (1000.0 * 1000.0)
            //};

            e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
            e.ExpansionBehavior.DesiredDeceleration = 0.1 * 96 / (1000.0 * 1000.0);
            e.RotationBehavior.DesiredDeceleration = 720 / (1000.0 * 1000.0);

            #endregion

            e.Handled = true;
        }

        private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var element = GetManipulationItem(e.OriginalSource as FrameworkElement);
            if (element == null)
                return;
            var mode = GetManipulationMode(element);
            if ((mode & ManipulationModes.Scale) == ManipulationModes.Scale)
            {
                var key = element.GetHashCode();
                SetMatrixTransform(element);
                var matrix = (element.RenderTransform as MatrixTransform).Matrix;
                var center = new Point(element.ActualWidth / 2.0, element.ActualHeight / 2.0);
                center = matrix.Transform(center);
                var scale = e.Delta / 100.0;
                scale = scale > 0 ? scale : Math.Abs(2 + scale);
                var currentScale = _currentScale.ContainsKey(key) ? _currentScale[key] : 1.0;
                var maximumScale = GetMaximumScale(element);
                var minimumScale = GetMinimumScale(element);
                if (scale * currentScale > maximumScale)
                {
                    scale = maximumScale / currentScale;
                }
                else if (scale * currentScale < minimumScale)
                {
                    scale = minimumScale / currentScale;
                }
                matrix.ScaleAt(scale, scale, center.X, center.Y);
                _currentScale[key] = scale * currentScale;
                (element.RenderTransform as MatrixTransform).Matrix = matrix;
            }

        }

        private static Point _startLocation;
        private static FrameworkElement _capturedElement;

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = GetManipulationItem(e.OriginalSource as FrameworkElement);
            if (element == null)
                return;
            Initialize(element);
            _startLocation = e.GetPosition(GetManipulationContainer(element));
            element.CaptureMouse();
            element.MouseLeave += OnMouseLeave;
            _capturedElement = element;

        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _capturedElement != null)
            {
                var point = e.GetPosition(GetManipulationContainer(_capturedElement));
                var change = point - _startLocation;
                _startLocation = point;
                var matrix = (_capturedElement.RenderTransform as MatrixTransform).Matrix;
                var mode = GetManipulationMode(_capturedElement);
                var move = new Point(0, 0);
                if ((mode & ManipulationModes.TranslateX) == ManipulationModes.TranslateX)
                {
                    move.X = change.X;
                }
                if ((mode & ManipulationModes.TranslateY) == ManipulationModes.TranslateY)
                {
                    move.Y = change.Y;
                }
                matrix.Translate(move.X, move.Y);
                (_capturedElement.RenderTransform as MatrixTransform).Matrix = matrix;
                e.Handled = true;
            }

        }

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_capturedElement == null)
                return;
            _capturedElement.MouseLeave -= OnMouseLeave;
            _capturedElement.ReleaseMouseCapture();
            _capturedElement.Opacity = 1.0;
            CreateRecoverStoryboard(_capturedElement);
            _capturedElement = null;
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_capturedElement == null)
                return;
            _capturedElement.MouseLeave -= OnMouseLeave;
            _capturedElement.ReleaseMouseCapture();
            _capturedElement.Opacity = 1.0;
            CreateRecoverStoryboard(_capturedElement);
            _capturedElement = null;
        }
    }
}
