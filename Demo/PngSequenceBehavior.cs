using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace System.Windows.Media.Animation
{
    public static class ImageExt
    {
        public static readonly Dictionary<string, BitmapSource[]> FrameCache = new Dictionary<string, BitmapSource[]>();
        public static readonly Dictionary<string, int> FrameCacheReferences = new Dictionary<string, int>();

        public static SequenceFrameAnimation GetSequenceFrameAnimation(Image obj)
        {
            return (SequenceFrameAnimation)obj.GetValue(SequenceFrameAnimationProperty);
        }

        public static void SetSequenceFrameAnimation(Image obj, SequenceFrameAnimation value)
        {
            obj.SetValue(SequenceFrameAnimationProperty, value);
        }

        public static readonly DependencyProperty SequenceFrameAnimationProperty =
            DependencyProperty.RegisterAttached("SequenceFrameAnimation",
                typeof(SequenceFrameAnimation), typeof(ImageExt), new PropertyMetadata(new PropertyChangedCallback((s, e) =>
                {
                    if (e.OldValue is SequenceFrameAnimation animation && animation != null)
                    {
                        animation.Stop();
                        animation.Dispose();
                    }

                    if (s is Image img)
                    {
                        img.IsVisibleChanged += OnIsVisibleChanged;
                        img.Unloaded += OnUnloaded;
                        animation = e.NewValue as SequenceFrameAnimation;
                        animation.CreateStoryboard(img);
                    }

                })));

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var animation = GetSequenceFrameAnimation(sender as Image);
            animation.Dispose();
        }

        private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var img = sender as Image;
            var animation = GetSequenceFrameAnimation(img);
            if (!img.IsVisible)
            {
                animation.Stop();
            }
            else if (animation.AutoStart)
            {
                animation.Begin();
            }
        }
    }

    public class SequenceFrameAnimation
    {
        public bool AutoStart { get; set; }
        public RepeatBehavior RepeatBehavior { get; set; } = new RepeatBehavior(1);
        public TimeSpan BeginTime { get; set; } = TimeSpan.Zero;
        public bool AutoReverse { get; set; } = false;
        public int FPS { get; set; } = 30;

        public int DecodePixelWidth { get; set; } = 0;

        /// <summary>
        /// 动画是否暂停
        /// 未播放前为null
        /// </summary>
        public bool? IsPaused
        {
            get
            {
                try
                {
                    if (storyboard == null)
                        return false;
                    return storyboard.GetIsPaused();
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsComplete
        {
            get
            {
                if (storyboard == null)
                    return true;
                return storyboard.GetCurrentState() == ClockState.Stopped;
            }
        }

        private string directory;
        public string PngDirectory
        {
            get
            {
                if (DesignerProperties.GetIsInDesignMode(Target))
                    return directory;
                if (string.IsNullOrEmpty(directory))
                {
                    var path = Target.Source?.ToString();
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith("file:"))
                            directory = System.IO.Path.GetDirectoryName(path.Substring(5).Replace("///", "")) + "\\";
                        else if (path.StartsWith("pack://"))
                        {
                            var index = path.IndexOf("component/") + 10;
                            directory = AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.GetDirectoryName(path.Substring(index)) + "\\";
                        }
                    }
                }
                return directory;
            }
            set
            {
                directory = value;
                if (directory.StartsWith("../"))
                {
                    directory = AppDomain.CurrentDomain.BaseDirectory + directory.Substring(3);
                }
                if (!directory.EndsWith(@"\"))
                {
                    directory += @"\";
                }
            }
        }

        private Storyboard storyboard;
        public Image Target { get; private set; }

        public void CreateStoryboard(Image target)
        {
            this.Target = target;
            if (!string.IsNullOrEmpty(PngDirectory) && Directory.Exists(PngDirectory))
            {
                var width = this.DecodePixelWidth;
                if (width <= 0)
                {
                    if (Target.Width > 0)
                        width = (int)Target.Width;
                    else if (Target.ActualWidth > 0)
                        width = (int)Target.ActualWidth;
                }

                BitmapSource[] sources = null;
                if (ImageExt.FrameCache.ContainsKey(PngDirectory))
                {
                    sources = ImageExt.FrameCache[PngDirectory];
                    ImageExt.FrameCacheReferences[PngDirectory]++;
                }
                else
                {
                    var files = Directory.GetFiles(PngDirectory, "*.png");
                    if (files.Length > 0)
                    {
                        sources = new BitmapImage[files.Length];
                        for (int i = 0; i < files.Length; i++)
                        {
                            var b = new BitmapImage();
                            b.BeginInit();
                            b.CacheOption = BitmapCacheOption.OnLoad;
                            b.CreateOptions = BitmapCreateOptions.DelayCreation;
                            if (width > 0)
                                b.DecodePixelWidth = width;
                            b.UriSource = new Uri(files[i], UriKind.Absolute);
                            b.EndInit();
                            b.Freeze();
                            sources[i] = b;
                        }
                        ImageExt.FrameCache[PngDirectory] = sources;
                        ImageExt.FrameCacheReferences[PngDirectory] = 1;
                    }
                }

                if (sources != null && sources.Length > 0)
                {
                    if (Target.Source == null)
                    {
                        Target.Source = sources[0];
                    }
                    var delay = TimeSpan.FromMilliseconds(1000.0 / this.FPS);
                    var time = TimeSpan.Zero;
                    var animation = new ObjectAnimationUsingKeyFrames();
                    foreach (var item in sources)
                    {
                        animation.KeyFrames.Add(new DiscreteObjectKeyFrame(item, time += delay));
                    }
                    Storyboard.SetTarget(animation, Target);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));
                    animation.Freeze();
                    storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    storyboard.RepeatBehavior = this.RepeatBehavior;
                    storyboard.AutoReverse = this.AutoReverse;
                    storyboard.BeginTime = this.BeginTime;
                    storyboard.FillBehavior = FillBehavior.Stop;
                    storyboard.Completed += Storyboard_Completed;
                    storyboard.Freeze();
                }
            }
            else
                throw new Exception("创建序列帧动画失败！");
        }

        public event EventHandler Completed;

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                //以此种特殊方式，否则无法释放Storyboard
                this.Target.Dispatcher.Invoke(() =>
                {
                    Completed?.Invoke(this.Target, e);
                });
            });
        }

        public void Begin()
        {
            if (storyboard == null)
                throw new Exception("Storyboard未初始化或已经释放！");
            storyboard.Begin();
        }

        public void Pause()
        {
            //if (storyboard == null)
            //    throw new Exception("Storyboard未初始化或已经释放！");
            storyboard?.Pause();
        }

        public void Resume()
        {
            //if (storyboard == null)
            //    throw new Exception("Storyboard未初始化或已经释放！");
            storyboard?.Resume();
        }

        public void Stop()
        {
            //if (storyboard == null)
            //    throw new Exception("Storyboard未初始化或已经释放！");
            storyboard?.Stop();
        }

        public void Dispose()
        {
            Target.BeginAnimation(Image.SourceProperty, null); //这一行是关键！清除动画，移除BitmapSource引用
            storyboard = null;
            if (ImageExt.FrameCacheReferences.TryGetValue(PngDirectory, out int count) && count > 0)
            {
                count--;
                ImageExt.FrameCacheReferences[PngDirectory] = count;
                if (count < 1)
                {
                    var s = ImageExt.FrameCache[PngDirectory];
                    Array.Clear(s, 0, s.Length);
                    s = null;
                    ImageExt.FrameCache.Remove(PngDirectory);
                    GC.Collect();
                }
            }
        }

    }
}
