using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace System.Windows.Behaviors
{
    public class PngSequenceBehavior : Behavior<Image>
    {
        static readonly Dictionary<string, BitmapSource[]> PngSequenceCache = new Dictionary<string, BitmapSource[]>();
        static readonly Dictionary<string, int> PngSequenceCacheCount = new Dictionary<string, int>();

        public bool AutoStart { get; set; }
        public RepeatBehavior RepeatBehavior { get; set; } = new RepeatBehavior(1);
        public TimeSpan BeginTime { get; set; } = TimeSpan.Zero;
        public bool AutoReverse { get; set; } = false;
        public int FPS { get; set; } = 30;

        public int DecodePixelWidth { get; set; } = 0;

        private string directory;
        public string PngDirectory
        {
            get
            {
                //if (DesignerProperties.GetIsInDesignMode(this))
                //    return;
                if (string.IsNullOrEmpty(directory))
                {
                    var path = AssociatedObject.Source?.ToString();
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

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
            this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Stop();
            this.Clear();
        }

        private void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!AssociatedObject.IsVisible)
            {
                this.Stop();
            }
            else if (this.AutoStart)
            {
                this.Begin();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            this.AssociatedObject.IsVisibleChanged -= AssociatedObject_IsVisibleChanged;
        }

        private void CreateStoryboard()
        {
            if (!string.IsNullOrEmpty(PngDirectory) && Directory.Exists(PngDirectory))
            {
                var width = this.DecodePixelWidth;
                var image = this.AssociatedObject;
                if (width <= 0)
                {
                    if (image.Width > 0)
                        width = (int)image.Width;
                    else if (image.ActualWidth > 0)
                        width = (int)image.ActualWidth;
                }

                BitmapSource[] sources = null;
                if (PngSequenceCache.ContainsKey(PngDirectory))
                {
                    sources = PngSequenceCache[PngDirectory];
                    PngSequenceCacheCount[PngDirectory]++;
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
                        PngSequenceCache[PngDirectory] = sources;
                        PngSequenceCacheCount[PngDirectory] = 1;
                    }
                }

                if (sources != null && sources.Length > 0)
                {
                    if (image.Source == null)
                    {
                        image.Source = sources[0];
                    }
                    var delay = TimeSpan.FromMilliseconds(1000.0 / this.FPS);
                    var time = TimeSpan.Zero;
                    var animation = new ObjectAnimationUsingKeyFrames();
                    foreach (var item in sources)
                    {
                        animation.KeyFrames.Add(new DiscreteObjectKeyFrame(item, time += delay));
                    }
                    Storyboard.SetTarget(animation, image);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(Image.SourceProperty));
                    animation.Freeze();
                    storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    storyboard.RepeatBehavior = this.RepeatBehavior;
                    storyboard.AutoReverse = this.AutoReverse;
                    storyboard.BeginTime = this.BeginTime;
                    storyboard.FillBehavior = FillBehavior.HoldEnd;
                    storyboard.Completed += Storyboard_Completed;
                    storyboard.Freeze();
                }
            }
            else
                throw new Exception("创建序列帧动画失败！");
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                //以此种特殊方式，否则无法释放Storyboard
                this.Dispatcher.Invoke(() =>
                {
                    this.Clear();
                });
            });
        }

        public void Begin()
        {
            if (storyboard == null)
                CreateStoryboard();
            storyboard.Begin();
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

        public void Clear()
        {
            this.AssociatedObject.BeginAnimation(Image.SourceProperty, null); //这一行是关键！清除动画，移除BitmapSource引用
            storyboard = null;
            if (PngSequenceCacheCount.TryGetValue(PngDirectory, out int count) && count > 0)
            {
                count--;
                PngSequenceCacheCount[PngDirectory] = count;
                if (count < 1)
                {
                    var s = PngSequenceCache[PngDirectory];
                    Array.Clear(s, 0, s.Length);
                    s = null;
                    PngSequenceCache.Remove(PngDirectory);
                    GC.Collect();
                }
            }
        }

    }
}
