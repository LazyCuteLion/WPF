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
    public static class TransformHelper
    {
        public static void SetRenderTransform(this UIElement element)
        {
            //xaml中定义
            //this.Target.RenderTransformOrigin = new Point(0.5, 0.5); 
            var rt = element.RenderTransform;
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
            element.RenderTransform = newGroup;
        }

        public static void SetRenderTransform(this UIElement element, DependencyProperty property, double value)
        {
            Transform find = null;
            if (element.RenderTransform is TransformGroup group)
            {
                find = group.Children.FirstOrDefault(f => f.GetType() == property.OwnerType);
            }
            else if (element.RenderTransform != Transform.Identity && element.RenderTransform.GetType() == property.OwnerType)
            {
                find = element.RenderTransform;
            }
            if (find != null)
                switch (property.Name)
                {
                    case "ScaleX":
                        (find as ScaleTransform).ScaleX = value;
                        break;
                    case "ScaleY":
                        (find as ScaleTransform).ScaleY = value;
                        break;
                    case "AngleX":
                        (find as SkewTransform).AngleX = value;
                        break;
                    case "AngleY":
                        (find as SkewTransform).AngleX = value;
                        break;
                    case "Angle":
                        (find as RotateTransform).Angle = value;
                        break;
                    case "X":
                        (find as TranslateTransform).X = value;
                        break;
                    case "Y":
                        (find as TranslateTransform).Y = value;
                        break;
                }
        }

        public static PropertyPath ToTransformPropertyPath(this DependencyProperty property, int index = -1)
        {
            if (property.OwnerType.BaseType != typeof(Transform))
                throw new Exception("[property]基类型必须是[Transform]！");

            if (index > -1 && index < 4)
                return new PropertyPath($"(0).(1)[{index}].(2)", UIElement.RenderTransformProperty, TransformGroup.ChildrenProperty, property);
            //return new PropertyPath($"(UIElement.RenderTransform).(TransformGroup.Children)[{index}].({property.OwnerType.Name}.{property.Name})");
            else
                return new PropertyPath($"(0).(1)", UIElement.RenderTransformProperty, property);
            //return new PropertyPath($"(UIElement.RenderTransform).({property.OwnerType.Name}.{property.Name})");
        }

        public static PropertyPath GetTransformPropertyPath(this UIElement element, DependencyProperty property)
        {
            if (property.OwnerType.BaseType == typeof(Transform))
            {
                if (element.RenderTransform == Transform.Identity)
                {
                    element.SetRenderTransform();
                }
                if (element.RenderTransform is TransformGroup transformGroup)
                {
                    for (int i = 0; i < transformGroup.Children.Count; i++)
                    {
                        if (transformGroup.Children[i].GetType() == property.OwnerType)
                        {
                            return property.ToTransformPropertyPath(i);
                        }
                    }
                }
                else if (element.RenderTransform.GetType() == property.OwnerType)
                {
                    return property.ToTransformPropertyPath();
                }
            }
            throw new Exception("[property]基类型必须是[Transform]！");
        }
    }

    public static class StoryBoardHelper
    {

        #region Timeline
        public static Timeline CreateDoubleAnimation(this UIElement element, Duration duration, double from, double to, PropertyPath propertyPath, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation(to, duration)
            {
                EasingFunction = easingFunction
            };

            if (!double.IsNaN(from))
                animation.From = from;

            animation.SetTarget(element)
                             .SetTargetProperty(propertyPath);

            return animation;
        }

        public static Timeline Delay(this Timeline timeline, TimeSpan time)
        {
            timeline.BeginTime = time;
            return timeline;
        }

        public static Timeline Delay(this Timeline timeline, double milliseconds)
        {
            timeline.BeginTime = TimeSpan.FromMilliseconds(milliseconds);
            return timeline;
        }

        public static Timeline Repeat(this Timeline timeline, RepeatBehavior repeat)
        {
            timeline.RepeatBehavior = repeat;
            return timeline;
        }

        public static Timeline Repeat(this Timeline timeline, int repeat = 0)
        {
            timeline.RepeatBehavior = new RepeatBehavior(repeat);
            return timeline;
        }

        public static Timeline Reverse(this Timeline timeline)
        {
            timeline.AutoReverse = true;
            return timeline;
        }

        public static Timeline SetTarget(this Timeline timeline, UIElement element)
        {
            Storyboard.SetTarget(timeline, element);
            return timeline;
        }

        public static Timeline SetTargetProperty(this Timeline timeline, PropertyPath propertyPath)
        {
            Storyboard.SetTargetProperty(timeline, propertyPath);
            return timeline;
        }

        public static void Add(this TimelineCollection timelines, params Timeline[] items)
        {
            foreach (var item in items)
            {
                timelines.Add(item);
            }
        }
        #endregion

        #region Fade
        public static Timeline Fade(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            return element.CreateDoubleAnimation(duration, double.NaN, to, new PropertyPath(UIElement.OpacityProperty), easingFunction);
        }

        public static Timeline Fade(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            return element.CreateDoubleAnimation(duration, from, to, new PropertyPath(UIElement.OpacityProperty), easingFunction);
        }
        #endregion

        #region Scale

        public static Timeline ScaleX(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleXProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline ScaleX(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleYProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        public static Timeline ScaleY(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleYProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline ScaleY(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleYProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        #endregion

        #region Translate

        public static Timeline TranslateX(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(TranslateTransform.XProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline TranslateX(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(TranslateTransform.XProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        public static Timeline TranslateY(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(TranslateTransform.YProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline TranslateY(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(TranslateTransform.YProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        #endregion

        #region Rotate

        public static Timeline Rotate(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(RotateTransform.AngleProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline Rotate(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(RotateTransform.AngleProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        #endregion

        #region Skew

        public static Timeline SkewX(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(SkewTransform.AngleXProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline SkewX(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(SkewTransform.AngleXProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        public static Timeline SkewY(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(SkewTransform.AngleYProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline SkewY(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(SkewTransform.AngleYProperty);
            return element.CreateDoubleAnimation(duration, from, to, p, easingFunction);
        }

        #endregion

    }

    public static class AnimationHelper
    {
        private static readonly Dictionary<string, ObjectAnimationUsingKeyFrames> AnimationCache =
            new Dictionary<string, ObjectAnimationUsingKeyFrames>();

        public static async Task<Storyboard> Animate(this Image image, int width, int fps = 30, string directory = "")
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
                ObjectAnimationUsingKeyFrames animation = null;
                if (AnimationCache.ContainsKey(directory))
                {
                    animation = AnimationCache[directory];
                }
                else
                {
                    var files = Directory.GetFiles(directory, "*.png");
                    if (files.Length > 0)
                    {
                        var sources = new BitmapImage[files.Length];
                        if (width <= 0)
                        {
                            if (image.Width > 0)
                                width = (int)image.Width;
                            else if (image.ActualWidth > 0)
                                width = (int)image.ActualWidth;
                        }

                        await Task.Run(() =>
                        {
                            for (int i = 0; i < files.Length; i++)
                            {
                                var b = new BitmapImage();
                                b.BeginInit();
                                b.CacheOption = BitmapCacheOption.OnLoad;
                                //b.CreateOptions = BitmapCreateOptions.DelayCreation;
                                if (width > 0)
                                    b.DecodePixelWidth = width;
                                b.UriSource = new Uri(files[i], UriKind.Absolute);
                                b.EndInit();
                                b.Freeze();
                                sources[i] = b;
                            }
                        });
                        if (image.Source == null)
                            image.Source = sources[0];
                        animation = new ObjectAnimationUsingKeyFrames();
                        var delay = TimeSpan.FromMilliseconds(1000.0 / fps);
                        var time = TimeSpan.Zero;
                        foreach (var item in sources)
                        {
                            animation.KeyFrames.Add(new DiscreteObjectKeyFrame(item, time += delay));
                        }
                        AnimationCache[directory] = animation;
                    }
                }

                var s = new Storyboard();
                Storyboard.SetTarget(animation, image);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));
                s.Children.Add(animation);

                return s;
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
            DependencyProperty.RegisterAttached("PngSequenceAnimation", typeof(PngSequenceAnimation), typeof(ImageExt),
                new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                if (s is Image img)
                {
                    img.IsVisibleChanged += OnIsVisibleChanged;
                    img.Unloaded += OnUnloaded;
                }
            })));

        private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var img = sender as Image;
            var animation = GetPngSequenceAnimation(img);
            if (animation != null)
            {
                if (img.IsVisible)
                {
                    animation.BuildAnimation(img);
                }
                else
                {
                    animation.Stop();
                }
            }
        }


        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            img.Unloaded -= OnUnloaded;
            var animation = GetPngSequenceAnimation(img);
            animation?.Stop();
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

        private Storyboard storyboard;

        public async void BuildAnimation(Image image)
        {
            storyboard = await image.Animate(this.DecodePixelWidth, this.FPS, this.PngDirectory);
            storyboard.RepeatBehavior = this.RepeatBehavior;
            storyboard.AutoReverse = this.AutoReverse;
            storyboard.BeginTime = this.BeginTime;
            //storyboard.Freeze();
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
