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
        #region Event

        public delegate bool SingleTouchHorizontalHandle(object sender, ManipulationDeltaEventArgs e);
        public delegate bool SingleTouchVerticalHandle(object sender, ManipulationDeltaEventArgs e);
        public delegate bool MouseHorizontalHandle(object sender, double offset);
        public delegate bool MouseVerticalHandle(object sender, double offset);
        public delegate void StoryboardCompletedHandle(object sender, Orientations orientation);

        /// <summary>
        /// 水平方向的单点触摸操作
        /// </summary>
        public static event SingleTouchHorizontalHandle SingleTouchHorizontal;
        /// <summary>
        /// 垂直方向的单点触摸操作
        /// </summary>
        public static event SingleTouchVerticalHandle SingleTouchVertical;
        /// <summary>
        /// 水平方向的鼠标操作
        /// </summary>
        public static event MouseHorizontalHandle MouseHorizontal;
        /// <summary>
        /// 垂直方向的鼠标操作
        /// </summary>
        public static event MouseVerticalHandle MouseVertical;
        /// <summary>
        /// 变换终止
        /// </summary>
        public static event StoryboardCompletedHandle StoryboardCompleted;

        public static event RoutedEventHandler ScaleCompleted;

        /// <summary>
        /// 超出容器范围
        /// </summary>
        public static event RoutedEventHandler OnOutOfContainer;
        #endregion

        #region DependencyProperty

        public static readonly DependencyProperty IsContenterProperty = DependencyProperty.RegisterAttached("IsContenter",
           typeof(bool), typeof(MultiTouchHelper), new PropertyMetadata(false, new PropertyChangedCallback(OnIsMultiContenterChanged)));

        public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.RegisterAttached("ManipulationMode",
           typeof(ManipulationModes), typeof(MultiTouchHelper), new PropertyMetadata(ManipulationModes.None,
           new PropertyChangedCallback(OnIManipulationModeChanged)));

        public static readonly DependencyProperty ChangedLengthProperty = DependencyProperty.RegisterAttached("ChangedLength",
         typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(double.NaN));

        public static readonly DependencyProperty MaxScaleProperty = DependencyProperty.RegisterAttached("MaxScale",
         typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(5.0));

        public static readonly DependencyProperty MinScaleProperty = DependencyProperty.RegisterAttached("MinScale",
         typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(0.5));


        public static readonly DependencyProperty WaitingForRecoverProperty = DependencyProperty.RegisterAttached("WaitingForRecover",
          typeof(int), typeof(MultiTouchHelper), new PropertyMetadata(-1, new PropertyChangedCallback(OnWaitForRecoverChaneged)));

        private static readonly DependencyProperty CurrentScaleProperty = DependencyProperty.RegisterAttached("CurrentScale",
         typeof(double), typeof(MultiTouchHelper), new PropertyMetadata(1.0));

        private static readonly DependencyProperty RecoverThreadProperty = DependencyProperty.RegisterAttached("RecoverThread",
          typeof(Thread), typeof(MultiTouchHelper));


        // Using a DependencyProperty as the backing store for IsSingleTouchEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSingleTouchEnabledProperty =
            DependencyProperty.RegisterAttached("IsSingleTouchEnabled", typeof(bool), typeof(MultiTouchHelper), new PropertyMetadata(true));


        #endregion

        #region GetAndSet

        #region IsMultiContenter
        /// <summary>
        /// 是否为多点操作的容器
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool GetIsContenter(FrameworkElement element)
        {
            return (bool)element.GetValue(IsContenterProperty);
        }

        /// <summary>
        /// 设置一个bool值，指示该控件是否作为多点操作的容器
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetIsContenter(FrameworkElement element, bool value)
        {
            element.SetValue(IsContenterProperty, value);
        }
        #endregion

        #region ManipulationMode
        /// <summary>
        /// 获取多点操作的类型
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static ManipulationModes GetManipulationMode(FrameworkElement element)
        {
            return (ManipulationModes)element.GetValue(ManipulationModeProperty);
        }

        /// <summary>
        /// 设置多点操作的类型
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetManipulationMode(FrameworkElement element, ManipulationModes value)
        {
            element.SetValue(ManipulationModeProperty, value);
        }
        #endregion

        #region WaitingForRecover
        /// <summary>
        /// 获取等待恢复的时间间隔
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static int GetWaitingForRecover(FrameworkElement element)
        {
            return (int)element.GetValue(WaitingForRecoverProperty);
        }

        /// <summary>
        /// 设置等待恢复的时间间隔
        /// 不等待则设置time小于0
        /// </summary>
        /// <param name="element"></param>
        /// <param name="time"></param>
        public static void SetWaitingForRecover(FrameworkElement element, int time)
        {
            element.SetValue(WaitingForRecoverProperty, time);
        }
        #endregion

        #region ChangedLength
        /// <summary>
        /// 获取每次移动的间距
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static double GetChangedLength(FrameworkElement element)
        {
            return (double)element.GetValue(ChangedLengthProperty);
        }

        /// <summary>
        /// 设置每次移动的间距 
        /// 例如ChangedLength=1000，如果触摸移动了600，则自动移动余下的400
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetChangedLength(FrameworkElement element, double value)
        {
            element.SetValue(ChangedLengthProperty, value);
        }
        #endregion

        #region IsSingleTouchEnabled
        public static bool GetIsSingleTouchEnabled(FrameworkElement obj)
        {
            return (bool)obj.GetValue(IsSingleTouchEnabledProperty);
        }

        public static void SetIsSingleTouchEnabled(FrameworkElement obj, bool value)
        {
            obj.SetValue(IsSingleTouchEnabledProperty, value);
        }
        #endregion

        #region MaxScale
        /// <summary>
        /// 可以放大的最大倍数
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static double GetMaxScale(FrameworkElement element)
        {
            return (double)element.GetValue(MaxScaleProperty);
        }

        /// <summary>
        /// 设置可以放大的最大倍数
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetMaxScale(FrameworkElement element, double value)
        {
            element.SetValue(MaxScaleProperty, value);
        }
        #endregion

        #region MinScale
        /// <summary>
        /// 缩小的最小倍数
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static double GetMinScale(FrameworkElement element)
        {
            return (double)element.GetValue(MinScaleProperty);
        }

        /// <summary>
        /// 设置可以缩小的最小倍数
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetMinScale(FrameworkElement element, double value)
        {
            element.SetValue(MinScaleProperty, value);
        }
        #endregion

        #region CurrentScale
        /// <summary>
        /// 获取当前变换的倍数
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static double GetCurrentScale(FrameworkElement element)
        {
            return (double)element.GetValue(CurrentScaleProperty);
        }

        /// <summary>
        /// 记录当前的变化倍数
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        private static void SetCurrentScale(FrameworkElement element, double value)
        {
            element.SetValue(CurrentScaleProperty, value);
        }
        #endregion

        #region RecoverThread

        /// <summary>
        /// 获取 恢复 线程（动画）
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static Thread GetRecoverThread(FrameworkElement element)
        {
            return (Thread)element.GetValue(RecoverThreadProperty);
        }

        /// <summary>
        /// 存储 恢复 线程（动画）
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        private static void SetRecoverThread(FrameworkElement element, Thread value)
        {
            element.SetValue(RecoverThreadProperty, value);
        }
        #endregion

        #endregion

        #region PropertyChangedCallback

        private static void OnWaitForRecoverChaneged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var time = (int)e.NewValue;
            if (time < 0)
            {
                var thread = GetRecoverThread((FrameworkElement)d);
                if (thread != null && thread.ThreadState != ThreadState.Stopped)
                {
                    thread.Abort();
                    thread = null;
                }
            }
        }

        private static void OnIsMultiContenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contenter = d as FrameworkElement;
            if ((bool)e.NewValue == true)
            {
                contenter.ManipulationStarting += ManipulationStarting;
                contenter.ManipulationDelta += ManipulationDelta;
                contenter.ManipulationInertiaStarting += ManipulationInertiaStarting;
                contenter.ManipulationCompleted += ManipulationCompleted;
            }
            else
            {
                contenter.ManipulationStarting -= ManipulationStarting;
                contenter.ManipulationDelta -= ManipulationDelta;
                contenter.ManipulationInertiaStarting -= ManipulationInertiaStarting;
                contenter.ManipulationCompleted -= ManipulationCompleted;
            }

        }

        private static void OnIManipulationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            //element.RenderTransformOrigin = new Point(0.5, 0.5);
            var mode = (ManipulationModes)e.NewValue;

            if (mode != ManipulationModes.None)
            {
                element.IsManipulationEnabled = true;

                if (element.RenderTransform == Transform.Identity)
                {
                    element.RenderTransform = new MatrixTransform();
                }
                else if (!(element.RenderTransform is MatrixTransform))
                {
                    if (element.RenderTransform is ScaleTransform)
                    {
                        var scale = element.RenderTransform as ScaleTransform;
                        var mt = new Matrix();
                        mt.ScaleAt(scale.ScaleX, scale.ScaleY, element.RenderSize.Width / 2, element.RenderSize.Height / 2);
                        element.RenderTransform = new MatrixTransform(mt);

                    }
                }
                SetCurrentScale(element, 1);
                #region Add Mouse Event
                element.MouseLeftButtonDown += MouseDown;
                element.MouseMove += MouseMove;
                element.MouseLeftButtonUp += MouseUp;
                element.MouseWheel += MouseWheel;
                #endregion
            }
            else
            {
                element.IsManipulationEnabled = false;

                #region Remove Mouse Event
                element.MouseLeftButtonDown -= MouseDown;
                element.MouseMove -= MouseMove;
                element.MouseLeftButtonUp -= MouseUp;
                element.MouseWheel -= MouseWheel;
                #endregion
            }

        }

        #endregion

        #region Mouse

        private static Point? _location = null;

        private static int _itemIndex = 0;

        public static int ItemIndex
        {
            get { return MultiTouchHelper._itemIndex; }
        }

        /// <summary>
        /// 移动的方向
        /// </summary>
        private static Orientations _orientation = Orientations.None;

        private static void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)
            {
                var scale = e.Delta / 100.0;
                scale = scale > 0 ? scale : Math.Abs(2 + scale);
                ElementSacle(sender as FrameworkElement, scale);
            }
        }

        private static void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_location.HasValue)
                {
                    var point = e.GetPosition(Application.Current.MainWindow);

                    Point move = new Point(point.X - _location.Value.X, point.Y - _location.Value.Y);

                    #region 鼠标事件
                    if (MouseVertical != null)
                    {
                        if (MouseVertical(sender, move.Y))
                            return;
                    }
                    if (MouseHorizontal != null)
                    {
                        if (MouseHorizontal(sender, move.X))
                            return;
                    }
                    #endregion

                    ElementMove(sender as FrameworkElement, move);

                    _location = point;
                }
            }
            //e.Handled = true;
        }

        private static void MouseUp(object sender, MouseButtonEventArgs e)
        {
            _location = null;

            var parent = VisualTreeHelper.GetParent(sender as UIElement);
            if (parent is ListBoxItem)
                Canvas.SetZIndex(parent as UIElement, ++_itemIndex);
            else
                Canvas.SetZIndex(sender as FrameworkElement, ++_itemIndex);

            if (_itemIndex > int.MaxValue - 2)
            {
                _itemIndex = 0;
            }

            CreateThreadStory(sender as FrameworkElement);
        }

        private static void MouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(sender as UIElement);
            if (parent is ListBoxItem)
                Canvas.SetZIndex(parent as UIElement, int.MaxValue);
            else
                Canvas.SetZIndex(sender as FrameworkElement, int.MaxValue);
            _location = e.GetPosition(Application.Current.MainWindow);

            var thread = GetRecoverThread(sender as FrameworkElement);

            if (thread != null && thread.ThreadState != ThreadState.Stopped)
                thread.Abort();


        }

        #endregion

        #region MultiTouch

        private static void ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var mode = GetManipulationMode(e.Source as FrameworkElement);

            switch (mode)
            {
                case ManipulationModes.TranslateX:
                    if (e.TotalManipulation.Translation.X == 0)
                        _orientation = Orientations.None;
                    else if (e.TotalManipulation.Translation.X < 0)
                        _orientation = Orientations.Left;
                    else
                        _orientation = Orientations.Right;
                    break;
                case ManipulationModes.TranslateY:
                    if (e.TotalManipulation.Translation.Y == 0)
                        _orientation = Orientations.None;
                    else if (e.TotalManipulation.Translation.Y < 0)
                        _orientation = Orientations.Up;
                    else
                        _orientation = Orientations.Down;
                    break;
            }

            CreateThreadStory(e.Source as FrameworkElement);

            //if (OnOutOfContainer != null)
            //{
            //    Rect containingRect = new Rect(((FrameworkElement)e.ManipulationContainer).RenderSize);
            //    var rectToMove = e.Source as UIElement;

            //    Rect shapeBounds = rectToMove.RenderTransform.TransformBounds(new Rect(rectToMove.RenderSize));

            //    shapeBounds.Location = rectToMove.TranslatePoint(new Point(), (FrameworkElement)e.ManipulationContainer);

            //    if (!containingRect.Contains(shapeBounds) && !containingRect.IntersectsWith(shapeBounds))
            //    {
            //        OnOutOfContainer(e.Source, new RoutedEventArgs());
            //    }
            //}

            if (ScaleCompleted != null && e.TotalManipulation.Scale.X > 1)
            {
                ScaleCompleted(e.Source, new RoutedEventArgs());
            }


            var parent = VisualTreeHelper.GetParent(e.Source as UIElement);
            if (parent is ListBoxItem)
                Canvas.SetZIndex(parent as UIElement, ++_itemIndex);
            else
                Canvas.SetZIndex(e.Source as UIElement, ++_itemIndex);
            if (_itemIndex > int.MaxValue - 2)
            {
                _itemIndex = 0;
            }


            e.Handled = true;
        }

        private static void ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            #region 设置惯性效果

            e.TranslationBehavior = new InertiaTranslationBehavior()
               {
                   InitialVelocity = e.InitialVelocities.LinearVelocity,
                   DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0)
               };

            e.RotationBehavior = new InertiaRotationBehavior()
            {
                InitialVelocity = e.InitialVelocities.AngularVelocity,
                DesiredDeceleration = 720 / (1000.0 * 1000.0)
            };

            e.ExpansionBehavior = new InertiaExpansionBehavior()
            {
                InitialVelocity = e.InitialVelocities.ExpansionVelocity,
                DesiredDeceleration = 0.1 * 96.0 / (1000.0 * 1000.0)
            };

            //e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
            //e.ExpansionBehavior.DesiredDeceleration = 0.1 * 96 / (1000.0 * 1000.0);
            //e.RotationBehavior.DesiredDeceleration = 720 / (1000.0 * 1000.0);

            #endregion

            e.Handled = true;
        }

        private static void ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.Manipulators.ToList().Count == 1)
            {
                if (SingleTouchVertical != null)
                {
                    if (SingleTouchVertical(sender, e))
                        return;
                }
                if (SingleTouchHorizontal != null)
                {
                    if (SingleTouchHorizontal(sender, e))
                        return;
                }
            }

            var element = e.Source as FrameworkElement;
            var mode = GetManipulationMode(element);

            #region 设置边界

            if (OnOutOfContainer == null)
            {
                if (e.IsInertial)
                {
                    Rect containingRect = new Rect(((FrameworkElement)e.ManipulationContainer).RenderSize);
                    var location = element.TranslatePoint(new Point(), (FrameworkElement)e.ManipulationContainer);
                    Rect shapeBounds = new Rect(location, element.RenderSize);

                    if (!containingRect.Contains(shapeBounds))
                    {
                        e.Complete();
                        e.ReportBoundaryFeedback(e.CumulativeManipulation);
                        e.Handled = true;
                        return;
                    }
                }
            }
            else
            {
                Rect containingRect = new Rect(((FrameworkElement)e.ManipulationContainer).RenderSize);
                var rectToMove = e.Source as UIElement;

                Rect shapeBounds = rectToMove.RenderTransform.TransformBounds(new Rect(rectToMove.RenderSize));

                shapeBounds.Location = rectToMove.TranslatePoint(new Point(), (FrameworkElement)e.ManipulationContainer);

                if (!containingRect.Contains(shapeBounds) && !containingRect.IntersectsWith(shapeBounds))
                {
                    e.Complete();
                    OnOutOfContainer(e.Source, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }

            #endregion

            #region 变换

            Matrix matrix = (element.RenderTransform as MatrixTransform).Matrix;


            Point center = new Point(element.ActualWidth / 2, element.ActualHeight / 2);

            center = matrix.Transform(center);

            var currentScale = GetCurrentScale(element);
            var maxScale = GetMaxScale(element);
            var minScale = GetMinScale(element);

            switch (mode)
            {
                #region case
                case ManipulationModes.TranslateX:
                    matrix.Translate(e.DeltaManipulation.Translation.X, 0);
                    break;


                case ManipulationModes.TranslateY:
                    matrix.Translate(0, e.DeltaManipulation.Translation.Y);
                    break;


                case ManipulationModes.Translate:
                    matrix.Translate(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                    break;


                case ManipulationModes.Rotate:
                    matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
                    // matrix.Rotate(e.DeltaManipulation.Rotation);
                    break;


                case ManipulationModes.Scale:

                    #region 设置最大最小倍数
                    var scale = e.DeltaManipulation.Scale.X;
                    SetCurrentScale(element, currentScale * scale);

                    if (currentScale * scale > maxScale)
                    {
                        scale = maxScale / currentScale;
                        SetCurrentScale(element, maxScale);
                    }
                    else if (currentScale * scale < minScale)
                    {
                        scale = minScale / currentScale;
                        SetCurrentScale(element, minScale);
                    }
                    #endregion

                    matrix.ScaleAt(scale, scale, center.X, center.Y);
                    // matrix.Scale(scale, scale);
                    break;


                case ManipulationModes.All:

                    #region 设置最大最小倍数
                    var scale2 = e.DeltaManipulation.Scale.X;
                    SetCurrentScale(element, currentScale * scale2);

                    if (currentScale * scale2 > maxScale)
                    {
                        scale2 = maxScale / currentScale;
                        SetCurrentScale(element, maxScale);
                    }
                    else if (currentScale * scale2 < minScale)
                    {
                        scale2 = minScale / currentScale;
                        SetCurrentScale(element, minScale);
                    }
                    #endregion

                    matrix.ScaleAt(scale2, scale2, center.X, center.Y);
                    //  matrix.Scale(scale2, scale2);
                    //  matrix.Rotate(e.DeltaManipulation.Rotation);
                    matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
                    matrix.Translate(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                    break;
                #endregion
            }

            (element.RenderTransform as MatrixTransform).Matrix = matrix;

            #endregion

            e.Handled = true;

        }

        private static void ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = sender as FrameworkElement;
            var element = (FrameworkElement)e.Source;
            e.Mode = ManipulationModes.All;

            e.IsSingleTouchEnabled = GetIsSingleTouchEnabled(element);

            //e.Pivot = new ManipulationPivot();

            //置于顶层
            var parent = VisualTreeHelper.GetParent(element);

            if (parent is ListBoxItem)
            {
                //System.Diagnostics.Debug.WriteLine("改变前：" + Canvas.GetZIndex(parent as UIElement));
                Canvas.SetZIndex(parent as UIElement, int.MaxValue);
                // System.Diagnostics.Debug.WriteLine("改变后：" + Canvas.GetZIndex(parent as UIElement));
            }
            else
                Canvas.SetZIndex(element, int.MaxValue);

            #region 如果设置了恢复，则终止已创建的线程

            var thread = GetRecoverThread(element);
            if (thread != null && thread.ThreadState != ThreadState.Stopped)
            {
                thread.Abort();
                thread = null;
            }

            #endregion


            e.Handled = true;
        }

        #endregion

        #region Private Method

        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="element"></param>
        /// <param name="container"></param>
        /// <param name="overshoot"></param>
        /// <returns></returns>
        private static bool CalculateOvershoot(UIElement element, IInputElement container, out Vector overshoot)
        {
            // Get axis aligned element bounds
            //var d = VisualTreeHelper.GetDrawing(element);
            var elementBounds = element.RenderTransform.TransformBounds(new Rect(element.RenderSize));


            //double extraX = 0.0, extraY = 0.0;
            overshoot = new Vector();

            FrameworkElement parent = container as FrameworkElement;
            if (parent == null)
            {
                return false;
            }

            // Calculate overshoot.  
            if (elementBounds.Left < 0)
                overshoot.X = elementBounds.Left;
            else if (elementBounds.Right > parent.ActualWidth)
                overshoot.X = elementBounds.Right - parent.ActualWidth;

            if (elementBounds.Top < 0)
                overshoot.Y = elementBounds.Top;
            else if (elementBounds.Bottom > parent.ActualHeight)
                overshoot.Y = elementBounds.Bottom - parent.ActualHeight;

            // Return false if Overshoot is empty; otherwsie, return true.
            return !Vector.Equals(overshoot, new Vector());
        }

        /// <summary>
        /// 创建恢复线程及动画
        /// </summary>
        /// <param name="element"></param>
        private static void CreateThreadStory(FrameworkElement element)
        {
            var time = GetWaitingForRecover(element);
            if (time > 0)
            {
                #region 创建恢复线程
                var thread = new Thread(() =>
                {
                    Thread.Sleep(time);

                    element.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //Canvas.SetZIndex(element, 0);
                        #region 创建动画
                        LinearMatrixAnimation animation = new LinearMatrixAnimation();
                        var render = (element.RenderTransform as MatrixTransform);
                        animation.From = render.Matrix;
                        animation.To = new Matrix();
                        animation.Duration = new Duration(TimeSpan.FromSeconds(0.3));
                        animation.FillBehavior = FillBehavior.Stop;
                        animation.Completed += (ss, ee) =>
                        {
                            animation = null;
                            render.Matrix = new Matrix();
                            var parent = VisualTreeHelper.GetParent(element);
                            if (parent is ListBoxItem)
                                Panel.SetZIndex(parent as UIElement, 0);
                            else
                                Panel.SetZIndex(element, 0);
                            SetCurrentScale(element, 1.0);
                        };
                        render.BeginAnimation(MatrixTransform.MatrixProperty, animation);
                        #endregion
                    }));

                });
                SetRecoverThread(element, thread);
                thread.IsBackground = true;
                thread.Start();
                #endregion
            }
            else
            {
                SetRecoverThread(element, null);
                var mode = GetManipulationMode(element);
                switch (mode)
                {
                    case ManipulationModes.TranslateX:
                        Move(element, _orientation);
                        break;
                    case ManipulationModes.TranslateY:
                        Move(element, _orientation);
                        break;
                }
            }
        }

        /// <summary>
        /// 放大缩小
        /// </summary>
        /// <param name="element"></param>
        /// <param name="scale"></param>
        private static void ElementSacle(FrameworkElement element, double scale)
        {
            Matrix matrix = (element.RenderTransform as MatrixTransform).Matrix;
            Point center = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
            center = matrix.Transform(center);

            var currentScale = GetCurrentScale(element);
            var maxScale = GetMaxScale(element);
            var minScale = GetMinScale(element);

            SetCurrentScale(element, currentScale * scale);

            #region 最大最小倍数
            if (currentScale * scale > maxScale)
            {
                scale = maxScale / currentScale;
                SetCurrentScale(element, maxScale);
            }
            if (currentScale * scale < minScale)
            {
                scale = minScale / currentScale;
                SetCurrentScale(element, minScale);
            }
            #endregion

            matrix.ScaleAt(scale, scale, center.X, center.Y);

            (element.RenderTransform as MatrixTransform).Matrix = matrix;
        }

        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="element"></param>
        /// <param name="move"></param>
        private static void ElementMove(FrameworkElement element, Point move)
        {
            //if (element.RenderTransform == null)
            //    element.RenderTransform = new MatrixTransform();

            Matrix matrix = (element.RenderTransform as MatrixTransform).Matrix;
            var mode = GetManipulationMode(element);

            switch (mode)
            {
                case ManipulationModes.TranslateX:
                    matrix.Translate(move.X, 0);
                    if (move.X == 0)
                        _orientation = Orientations.None;
                    else if (move.X < 0)
                        _orientation = Orientations.Left;
                    else
                        _orientation = Orientations.Right;
                    break;

                case ManipulationModes.TranslateY:
                    matrix.Translate(0, move.Y);
                    if (move.Y == 0)
                        _orientation = Orientations.None;
                    else if (move.Y < 0)
                        _orientation = Orientations.Up;
                    else
                        _orientation = Orientations.Down;
                    break;

                default: matrix.Translate(move.X, move.Y); break;
            }

            (element.RenderTransform as MatrixTransform).Matrix = matrix;
        }

        #endregion

        #region Public Method

        /// <summary>
        /// 水平或者垂直移动
        /// </summary>
        /// <param name="element"></param>
        /// <param name="orientation"></param>
        public static void Move(FrameworkElement element, Orientations orientation)
        {
            #region 移动固定长度的动画
            var length = GetChangedLength(element);
            var count = 1;

            try { count = (element as Panel).Children.Count; }
            catch { }

            if (!double.IsNaN(length))
            {
                #region 创建动画
                LinearMatrixAnimation animation = new LinearMatrixAnimation();
                var render = (element.RenderTransform as MatrixTransform);
                animation.From = render.Matrix;

                #region To
                double offsetX = render.Matrix.OffsetX;
                double offsetY = render.Matrix.OffsetY;

                var mode = GetManipulationMode(element);

                if (mode == ManipulationModes.TranslateX)
                {
                    //获得当前位置与长度的倍数
                    int x = (int)(Math.Abs(offsetX) / length);

                    if (x > 0)
                    {
                        switch (orientation)
                        {
                            //case Orientations.None: offsetX = -1 * length * (x - 1); break;
                            //向左要补足
                            case Orientations.Left: offsetX = -1 * length * (x + 1); break;
                            //向右要减去
                            case Orientations.Right: offsetX = -1 * length * x; break;
                        }

                    }
                    else
                    {
                        switch (orientation)
                        {
                            case Orientations.Left: offsetX = -1 * length; break;

                            //offsetX<=0；一般来说，坐标系都是从左上角0,0开始
                            case Orientations.Right: offsetX = 0; break;
                        }
                    }

                    offsetX = Math.Abs(offsetX) > length * (count - 1) ? -1 * length * (count - 1) : offsetX;

                    animation.Duration = new Duration(TimeSpan.FromSeconds((Math.Abs(offsetX - render.Matrix.OffsetX)) / 4000));
                }
                else if (mode == ManipulationModes.TranslateY)
                {
                    int x = (int)(Math.Abs(offsetY) / length);

                    if (x > 0)
                    {
                        switch (orientation)
                        {
                            //case Orientations.None: offsetY = -1 * length * (x - 1); break;
                            case Orientations.Up: offsetY = -1 * length * (x + 1); break;
                            case Orientations.Down: offsetY = -1 * length * x; break;
                        }

                    }
                    else
                    {
                        switch (orientation)
                        {

                            case Orientations.Up: offsetY = -1 * length; break;
                            case Orientations.Down: offsetY = 0; break;
                        }
                    }

                    offsetY = Math.Abs(offsetY) > length * (count - 1) ? -1 * length * (count - 1) : offsetY;

                    animation.Duration = new Duration(TimeSpan.FromSeconds((Math.Abs(offsetY - render.Matrix.OffsetY)) / 4000));
                }

                var matrix = new Matrix(render.Matrix.M11, render.Matrix.M12, render.Matrix.M21,
                    render.Matrix.M22, offsetX, offsetY);
                #endregion

                animation.To = matrix;

                //animation.Duration = new Duration(TimeSpan.FromSeconds(0.3));
                animation.FillBehavior = FillBehavior.Stop;

                animation.Completed += (ss, ee) =>
                {
                    animation = null;
                    (element.RenderTransform as MatrixTransform).Matrix = matrix;
                    StoryboardCompleted?.Invoke(element, orientation);
                };

                render.BeginAnimation(MatrixTransform.MatrixProperty, animation);
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 设置元素的水平位置
        /// </summary>
        /// <param name="element"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void SetLoaction(FrameworkElement element, double x, double y)
        {
            var render = (element.RenderTransform as MatrixTransform).Matrix;
            render.Translate(x, y);
            (element.RenderTransform as MatrixTransform).Matrix = render;
        }

        #endregion

        /// <summary>
        /// 移动操作的方向
        /// </summary>
        public enum Orientations
        {
            /// <summary>
            /// 不能确定
            /// </summary>
            None,
            /// <summary>
            /// 向左
            /// </summary>
            Left,
            /// <summary>
            /// 向右
            /// </summary>
            Right,
            /// <summary>
            /// 向上
            /// </summary>
            Up,
            /// <summary>
            /// 向下
            /// </summary>
            Down
        }


    }
}
