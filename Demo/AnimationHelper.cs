using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Animation
{
    public class StoryBoardFluentContext : IDisposable
    {
        public UIElement Target { get; private set; }
        private EventWaitHandle _waitHandle;
        private Storyboard _storyboard;
        private TimeSpan? _beginTime = TimeSpan.Zero;
        private PropertyPath _scaleXPropertyPath;
        private PropertyPath _scaleYPropertyPath;
        private PropertyPath _skewXPropertyPath;
        private PropertyPath _skewYPropertyPath;
        private PropertyPath _xPropertyPath;
        private PropertyPath _yPropertyPath;
        private PropertyPath _anglePropertyPath;

        public StoryBoardFluentContext(UIElement element)
        {
            this.Target = element;
            this.SetRenderTransform();
            _storyboard = new Storyboard();
            _storyboard.Completed += Storyboard_Completed;
        }

        private void SetRenderTransform()
        {
            //xaml中定义
            //this.Target.RenderTransformOrigin = new Point(0.5, 0.5); 

            var rt = this.Target.RenderTransform;

            var newGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                     new ScaleTransform(),
                     new SkewTransform(),
                     new RotateTransform(),
                     new TranslateTransform()
                }
            };

            if (rt is TransformGroup group)
            {
                var scaleTransform = group.Children.FirstOrDefault(t => t is ScaleTransform);
                if (scaleTransform != null)
                    newGroup.Children[0] = scaleTransform;

                var skewTransform = group.Children.FirstOrDefault(t => t is SkewTransform);
                if (skewTransform != null)
                    newGroup.Children[1] = skewTransform;

                var rotateTransform = group.Children.FirstOrDefault(t => t is RotateTransform);
                if (rotateTransform != null)
                    newGroup.Children[2] = rotateTransform;

                var translateTransform = group.Children.FirstOrDefault(t => t is TranslateTransform);
                if (translateTransform != null)
                    newGroup.Children[3] = translateTransform;
            }
            else if (rt is ScaleTransform scaleTransform)
            {
                newGroup.Children[0] = scaleTransform;
            }
            else if (rt is SkewTransform skewTransform)
            {
                newGroup.Children[1] = skewTransform;
            }
            else if (rt is RotateTransform rotateTransform)
            {
                newGroup.Children[2] = rotateTransform;
            }
            else if (rt is TranslateTransform translateTransform)
            {
                newGroup.Children[3] = translateTransform;
            }

            this.Target.RenderTransform = newGroup;
            _scaleXPropertyPath = ScaleTransform.ScaleXProperty.ToPropertyPath(0);
            _scaleYPropertyPath = ScaleTransform.ScaleYProperty.ToPropertyPath(0);
            _skewXPropertyPath = SkewTransform.AngleXProperty.ToPropertyPath(1);
            _skewYPropertyPath = SkewTransform.AngleYProperty.ToPropertyPath(1);
            _anglePropertyPath = RotateTransform.AngleProperty.ToPropertyPath(2);
            _xPropertyPath = TranslateTransform.XProperty.ToPropertyPath(3);
            _yPropertyPath = TranslateTransform.YProperty.ToPropertyPath(3);
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            _waitHandle?.Set();
        }

        public StoryBoardFluentContext Start()
        {
            if (_storyboard.CanFreeze)
                _storyboard.Freeze();
            _storyboard.Begin();
            return this;
        }

        public StoryBoardFluentContext Stop()
        {
            _waitHandle?.Set();
            _storyboard?.Stop();
            return this;
        }

        public Task<StoryBoardFluentContext> Wait()
        {
            if (_storyboard == null)
                throw new NullReferenceException("需要先调用 Start() 方法！");
            if (_waitHandle == null)
                _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            return Task.Run(() =>
            {
                _waitHandle.WaitOne();
                return this;
            });
        }

        public StoryBoardFluentContext Delay(TimeSpan time)
        {
            if (_beginTime.HasValue)
                _beginTime += time;
            else
                _beginTime = time;
            return this;
        }

        public StoryBoardFluentContext Delay(double time)
        {
            return this.Delay(TimeSpan.FromMilliseconds(time));
        }

        public StoryBoardFluentContext Repeat(RepeatBehavior repeat)
        {
            _storyboard.RepeatBehavior = repeat;
            return this;
        }

        public StoryBoardFluentContext Reverse()
        {
            _storyboard.AutoReverse = true;
            return this;
        }

        public StoryBoardFluentContext Add(Timeline timeline)
        {
            _storyboard.Children.Add(timeline);
            return this;
        }

        public StoryBoardFluentContext Animate(Duration duration, double from, double to, PropertyPath propertyPath, IEasingFunction easingFunction = null)
        {
            DoubleAnimation ani;
            if (double.IsNaN(from))
                ani = new DoubleAnimation(to, duration)
                {
                    BeginTime = _beginTime,
                    EasingFunction = easingFunction
                };
            else
                ani = new DoubleAnimation(from, to, duration)
                {
                    EasingFunction = easingFunction,
                    BeginTime = _beginTime
                };
            Storyboard.SetTarget(ani, this.Target);
            Storyboard.SetTargetProperty(ani, propertyPath);
            _storyboard.Children.Add(ani);
            return this;
        }

        #region Fade
        public StoryBoardFluentContext Fade(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, new PropertyPath(UIElement.OpacityProperty), easingFunction);
        }

        public StoryBoardFluentContext Fade(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, new PropertyPath(UIElement.OpacityProperty), easingFunction);
        }

        #endregion

        #region Scale

        public StoryBoardFluentContext ScaleX(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _scaleXPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext ScaleX(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _scaleXPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext ScaleY(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _scaleYPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext ScaleY(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _scaleYPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext Scale(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            this.ScaleX(duration, to, easingFunction);
            this.ScaleY(duration, to, easingFunction);
            return this;
        }

        public StoryBoardFluentContext Scale(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            this.ScaleX(duration, from, to, easingFunction);
            this.ScaleY(duration, from, to, easingFunction);
            return this;
        }
        #endregion

        #region Translate

        public StoryBoardFluentContext TranslateX(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _xPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext TranslateX(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _xPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext TranslateY(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _yPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext TranslateY(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _yPropertyPath, easingFunction);
        }

        #endregion

        #region Rotate

        public StoryBoardFluentContext Rotate(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _anglePropertyPath, easingFunction);
        }

        public StoryBoardFluentContext Rotate(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _anglePropertyPath, easingFunction);
        }

        #endregion

        #region Skew

        public StoryBoardFluentContext SkewX(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _skewXPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext SkewX(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _skewXPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext SkewY(Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, double.NaN, to, _skewYPropertyPath, easingFunction);
        }

        public StoryBoardFluentContext SkewY(Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return Animate(duration, from, to, _skewYPropertyPath, easingFunction);
        }

        #endregion

        public void Dispose()
        {
            this.Target = null;
            _waitHandle?.Dispose();
            _waitHandle = null;
            _storyboard = null;
        }
    }

    public static class AnimationHelper
    {
        public static StoryBoardFluentContext StartBuildAnimation(this UIElement element)
        {
            return new StoryBoardFluentContext(element);
        }

        public static PropertyPath ToPropertyPath(this DependencyProperty property, int index = -1)
        {
            if (property.OwnerType.BaseType != typeof(Transform))
                return new PropertyPath(property);

            if (index > -1 && index < 4)
                return new PropertyPath($"(UIElement.RenderTransform).(TransformGroup.Children)[{index}].({property.OwnerType.Name}.{property.Name})");
            else
                return new PropertyPath($"(UIElement.RenderTransform).({property.OwnerType.Name}.{property.Name})");
        }

        public static PropertyPath GetPropertyPath(this UIElement element, DependencyProperty property)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                for (int i = 0; i < transformGroup.Children.Count; i++)
                {
                    if (transformGroup.Children[i].GetType() == property.OwnerType)
                    {
                        return new PropertyPath($"(UIElement.RenderTransform).(TransformGroup.Children)[{i}].({property.OwnerType.Name}.{property.Name})");
                    }
                }
            }
            else if (element.RenderTransform.GetType() == property.OwnerType)
                return new PropertyPath($"(UIElement.RenderTransform).({property.OwnerType.Name}.{property.Name})");

            return new PropertyPath(property);
        }

        public static Storyboard Animate(this Image image, int fps = 30, string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
            {
                var path = image.Source?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith("file:"))
                        directory = System.IO.Path.GetDirectoryName(path.Substring(5).Replace("///", ""));
                    else if (path.StartsWith("pack://"))
                    {
                        var index = path.IndexOf("component/") + 10;
                        directory = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + path.Substring(index));
                    }
                }
            }
            else if (directory.StartsWith("../"))
            {
                directory = AppDomain.CurrentDomain.BaseDirectory + directory.Substring(3);
            }

            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.png");
                if (files.Length > 0)
                {
                    var sources = new BitmapImage[files.Length];

                    for (int i = 0; i < files.Length; i++)
                    {
                        var b = new BitmapImage();
                        b.BeginInit();
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.CreateOptions = BitmapCreateOptions.DelayCreation;
                        b.DecodePixelWidth = 600;
                        b.UriSource = new Uri(files[i], UriKind.Absolute);
                        b.EndInit();
                        b.Freeze();
                        sources[i] = b;
                    }

                    if (image.Source == null)
                        image.Source = sources[0];

                    var s = new Storyboard();
                    var ani = new ObjectAnimationUsingKeyFrames();
                    var delay = TimeSpan.FromMilliseconds(1000.0 / fps);
                    var time = TimeSpan.Zero;
                    foreach (var item in sources)
                    {
                        ani.KeyFrames.Add(new DiscreteObjectKeyFrame(item, time += delay));
                    }
                    Storyboard.SetTarget(ani, image);
                    Storyboard.SetTargetProperty(ani, new PropertyPath(Image.SourceProperty));
                    ani.Freeze();
                    s.Children.Add(ani);
                    return s;
                }
            }

            throw new Exception("请设置Image的Source或者传入参数[directory:序列帧文件夹]");
        }

        public static Storyboard Reverse(this Storyboard s)
        {
            s.AutoReverse = true;
            return s;
        }

        public static Storyboard Repeat(this Storyboard s, RepeatBehavior repeat)
        {
            s.RepeatBehavior = repeat;
            return s;
        }

        public static Storyboard GetFrozen(this Storyboard s)
        {
            if (s.CanFreeze)
                s.Freeze();
            return s;
        }

    }

    public class ImageExt
    {
        public static PngSequenceAnimation GetPngSequenceAnimation(Image obj)
        {
            return (PngSequenceAnimation)obj.GetValue(PngSequenceAnimationProperty);
        }

        public static void SetPngSequenceAnimation(Image obj, PngSequenceAnimation value)
        {
            obj.SetValue(PngSequenceAnimationProperty, value);
        }

        public static readonly DependencyProperty PngSequenceAnimationProperty =
            DependencyProperty.RegisterAttached("PngSequenceAnimation", typeof(PngSequenceAnimation), typeof(ImageExt), new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                if (s is Image img)
                {
                    img.Unloaded += OnUnloaded;
                    var animation = e.NewValue as PngSequenceAnimation;
                    animation.BuildAnimation(img);
                }
            })));

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            img.Unloaded -= OnUnloaded;
            var animation = GetPngSequenceAnimation(img);
            animation.Stop();
        }
    }

    public class PngSequenceAnimation
    {
        public bool AutoStart { get; set; } = true;
        public RepeatBehavior RepeatBehavior { get; set; } = new RepeatBehavior(1);
        public TimeSpan BeginTime { get; set; } = TimeSpan.Zero;
        public bool AutoReverse { get; set; } = false;
        public int FPS { get; set; } = 30;
        public string PngDirectory { get; set; }
        public int DecodePixelWidth { get; set; } = 0;
        public int DecodePixelHeight { get; set; } = 0;

        private Storyboard storyboard;

        public void BuildAnimation(Image image)
        {
            storyboard = image.Animate(this.FPS, this.PngDirectory);
            storyboard.RepeatBehavior = this.RepeatBehavior;
            storyboard.AutoReverse = this.AutoReverse;
            storyboard.BeginTime = this.BeginTime;
            storyboard.Freeze();
            if (this.AutoStart)
                this.Begin();
        }

        public void Begin()
        {
            storyboard?.Begin();
        }

        public void Pause()
        {
            storyboard?.Pause();
        }

        public void Resume()
        {
            storyboard?.Resume();
        }

        public void Stop()
        {
            storyboard?.Stop();
        }

    }
}
