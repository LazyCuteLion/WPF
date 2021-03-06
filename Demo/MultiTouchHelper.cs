﻿using System;
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
        private readonly static Dictionary<int, Matrix> _origin = new Dictionary<int, Matrix>();

        public static ManipulationModes GetManipulationMode(UIElement obj)
        {
            return (ManipulationModes)obj.GetValue(ManipulationModeProperty);
        }
        public static void SetManipulationMode(UIElement obj, ManipulationModes value)
        {
            obj.SetValue(ManipulationModeProperty, value);
        }
        public static readonly DependencyProperty ManipulationModeProperty =
            DependencyProperty.RegisterAttached("ManipulationMode", typeof(ManipulationModes), typeof(MultiTouchHelper),
                new PropertyMetadata(ManipulationModes.None, new PropertyChangedCallback((s, e) =>
                {
                    //如果系统不支持触摸，则添加鼠标事件
                    //if (!SystemParameters.IsTabletPC)
                    //{
                    var element = s as UIElement;
                    if (e.NewValue.Equals(ManipulationModes.None))
                    {
                        element.MouseWheel -= OnMouseWheel;
                        element.PreviewMouseDown -= OnMouseDown;
                        element.MouseMove -= OnMouseMove;
                        element.PreviewMouseUp -= OnMouseUp;
                        element.IsManipulationEnabled = false;
                    }
                    else
                    {
                        element.MouseWheel += OnMouseWheel;
                        element.PreviewMouseDown += OnMouseDown;
                        element.MouseMove += OnMouseMove;
                        element.PreviewMouseUp += OnMouseUp;
                        element.IsManipulationEnabled = true;
                    }
                    //}
                })));

        public static double GetMaximumScale(UIElement obj)
        {
            return (double)obj.GetValue(MaximumScaleProperty);
        }
        public static void SetMaximumScale(UIElement obj, double value)
        {
            obj.SetValue(MaximumScaleProperty, value);
        }
        public static readonly DependencyProperty MaximumScaleProperty =
            DependencyProperty.RegisterAttached("MaximumScale", typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(2.0));


        public static double GetMinimumScale(UIElement obj)
        {
            return (double)obj.GetValue(MinimumScaleProperty);
        }
        public static void SetMinimumScale(UIElement obj, double value)
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
                    }
                    else
                    {
                        contenter.ManipulationStarting -= OnManipulationStarting;
                        contenter.ManipulationDelta -= OnManipulationDelta;
                        contenter.ManipulationInertiaStarting -= OnManipulationInertiaStarting;
                        contenter.ManipulationCompleted -= OnManipulationCompleted;
                    }
                })));

        private static void Initialize(FrameworkElement element)
        {
            var key = element.GetHashCode();
            if (_recoverStoryboard.ContainsKey(key))
            {
                _recoverStoryboard[key].Stop();
                _recoverStoryboard.Remove(key);
                element.BeginAnimation(UIElement.OpacityProperty, null);
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
                var to = _origin.ContainsKey(key) ? _origin[key] : new Matrix();
                var speed = 10 * 96.0 / 1000.0;//速度
                var time = Math.Sqrt(Math.Pow(mt.Matrix.OffsetX - to.OffsetX, 2) + Math.Pow(mt.Matrix.OffsetY - to.OffsetY, 2)) / speed;
                var animation = new MatrixAnimation()
                {
                    From = mt.Matrix,
                    To = to,
                    FillBehavior = FillBehavior.HoldEnd,
                    Duration = new Duration(TimeSpan.FromMilliseconds(time)),
                    EasingFunction = new BackEase() { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(0).(1)", FrameworkElement.RenderTransformProperty, MatrixTransform.MatrixProperty));

                var booleanAnimation = new BooleanAnimationUsingKeyFrames() { FillBehavior = FillBehavior.Stop };
                booleanAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame(false, TimeSpan.Zero));
                booleanAnimation.KeyFrames.Add(new DiscreteBooleanKeyFrame(true, animation.Duration.TimeSpan));
                Storyboard.SetTarget(booleanAnimation, element);
                Storyboard.SetTargetProperty(booleanAnimation, new PropertyPath(UIElement.IsHitTestVisibleProperty));

                var opacityAnimation = new DoubleAnimation(0.7, 1, animation.Duration, FillBehavior.Stop);
                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

                var s = new Storyboard() { BeginTime = delay };
                s.Children.Add(animation);
                s.Children.Add(booleanAnimation);
                s.Children.Add(opacityAnimation);
                s.Completed += (ss, ee) =>
                {
                    _currentScale[key] = 1.0;
                    _recoverStoryboard.Remove(key);
                };
                s.Freeze();
                s.Begin();
                _recoverStoryboard[key] = s;
            }
        }

        private static void SetMatrixTransform(ref Matrix mt, Transform transform, Point? center = null)
        {
            switch (transform)
            {
                case ScaleTransform scale:
                    if (center.HasValue)
                        mt.ScaleAt(scale.ScaleX, scale.ScaleY, center.Value.X, center.Value.Y);
                    else
                        mt.Scale(scale.ScaleX, scale.ScaleY);
                    break;
                case TranslateTransform translate:
                    mt.Translate(translate.X, translate.Y);
                    break;
                case RotateTransform rotate:
                    if (center.HasValue)
                        mt.RotateAt(rotate.Angle, center.Value.X, center.Value.Y);
                    else
                        mt.Rotate(rotate.Angle);
                    break;
                case TransformGroup group:
                    foreach (var item in group.Children)
                    {
                        SetMatrixTransform(ref mt, item, center);
                    }
                    break;
            }
        }

        private static void SetMatrixTransform(FrameworkElement element)
        {
            if (element.RenderTransform == Transform.Identity)
            {
                element.RenderTransform = new MatrixTransform(_origin.ContainsKey(element.GetHashCode())
                                                                                                        ? _origin[element.GetHashCode()]
                                                                                                        : new Matrix());
            }
            else if (element.RenderTransform is MatrixTransform)
            {
                try
                {
                    var mt = (element.RenderTransform as MatrixTransform);
                    var current = mt.Matrix;
                    mt.BeginAnimation(MatrixTransform.MatrixProperty, null);
                    mt.Matrix = current;
                }
                catch
                {
                    element.RenderTransform = new MatrixTransform(_origin.ContainsKey(element.GetHashCode())
                                                                                                        ? _origin[element.GetHashCode()]
                                                                                                        : new Matrix());
                }
            }
            else
            {
                var mt = new Matrix();
                SetMatrixTransform(ref mt, element.RenderTransform);
                element.RenderTransform = new MatrixTransform(mt);
                _origin[element.GetHashCode()] = mt;
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
            var element = sender as FrameworkElement;
            var mode = GetManipulationMode(element);
            var key = element.GetHashCode();
            SetMatrixTransform(element);
            var matrix = (element.RenderTransform as MatrixTransform).Matrix;
            var center = new Point(element.ActualWidth / 2.0, element.ActualHeight / 2.0);
            center = matrix.Transform(center);
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if ((mode & ManipulationModes.Rotate) == ManipulationModes.Rotate)
                {
                    matrix.RotateAt(e.Delta / 10, center.X, center.Y);
                    (element.RenderTransform as MatrixTransform).Matrix = matrix;
                }
            }
            else
            {
                if ((mode & ManipulationModes.Scale) == ManipulationModes.Scale)
                {
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

        }

        private static Point _startLocation;

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            Initialize(element);
            _startLocation = e.GetPosition(GetManipulationContainer(element));
            element.CaptureMouse();
            element.MouseLeave += OnMouseLeave;
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var element = sender as FrameworkElement;
                var point = e.GetPosition(GetManipulationContainer(element));
                var change = point - _startLocation;
                _startLocation = point;
                var matrix = (element.RenderTransform as MatrixTransform).Matrix;
                var mode = GetManipulationMode(element);
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
                (element.RenderTransform as MatrixTransform).Matrix = matrix;
                e.Handled = true;
            }
        }

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            element.ReleaseMouseCapture();
            element.MouseLeave -= OnMouseLeave;
            element.Opacity = 1.0;
            CreateRecoverStoryboard(element);
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            element.ReleaseMouseCapture();
            element.MouseLeave -= OnMouseLeave;
            element.Opacity = 1.0;
            CreateRecoverStoryboard(element);
        }
    }
}
