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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Animation;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.ShellProperties;

namespace Demo
{
    /// <summary>
    /// Page3.xaml 的交互逻辑
    /// </summary>
    public partial class Page3 : Page
    {
        public Page3()
        {
            InitializeComponent();

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //var animation = new DoubleAnimation()
            //{
            //    To = 2000,
            //    //By = 50.0,
            //    //BeginTime = TimeSpan.FromSeconds(3)
            //    //Duration = TimeSpan.FromMilliseconds(3000)
            //};
            //animation.Completed += (ss, ee) =>
            //{
            //    Console.WriteLine(DateTime.Now.ToString("HH:mm.ss.fff"));
            //};
            //rect.BeginAnimation(Canvas.LeftProperty, animation);
            //Console.WriteLine(DateTime.Now.ToString("HH:mm.ss.fff"));

            //var shellFile = ShellFile.FromFilePath(@"D:\Videos\big_buck_bunny_1080p_h264.mov");//D:\Videos\big_buck_bunny_1080p_h264_udp.txt
            //var p = shellFile.Properties;
            //var p_video = new ShellProperties.PropertySystemVideo();
            //var w = p.GetProperty("System.Video.FrameWidth").ValueAsObject;
            //var kind = p.System.KindText.Value;
            //var size = new Point();
            //uint? w = null, h = null;
            //switch (kind)
            //{
            //    case "图片":
            //        w = p.System.Image.HorizontalSize.Value;
            //        h = p.System.Image.VerticalSize.Value;
            //        break;
            //    case "视频":
            //        w = p.System.Video.FrameWidth.Value;
            //        h = p.System.Video.FrameHeight.Value;
            //        break;
            //    case "文档":
            //        break;
            //    default:

            //        break;
            //}
            //if (w.HasValue && h.HasValue)
            //    size = new Point((double)w.Value, (double)h.Value);

            //foreach (var item in p.DefaultPropertyCollection)
            //{
            //    Console.WriteLine("{0} {1}", item.CanonicalName, item.ValueAsObject);
            //}
            //img.Source = shellFile.Thumbnail.ExtraLargeBitmapSource;
            //shellFile.Dispose();
            //img.RenderTransformOrigin = new Point(0.5, 0.5);
            //img.Scale(TimeSpan.FromMilliseconds(5000), 0).Begin();
            //var wb = new WriteableBitmap(BitmapFrame.Create(new Uri(@"D:\项目\北京悦康\发展大事记\BigEvents\bin\Debug\Images\乘风启航\乘风启航_00001.png", UriKind.Absolute)));
            //img.Source = wb;
            //var files = Directory.GetFiles(@"D:\项目\北京悦康\发展大事记\BigEvents\bin\Debug\Images\乘风启航\", "*.png");
            //var sources = new BitmapSource[files.Length];
            //for (int i = 0; i < files.Length; i++)
            //{
            //    sources[i] = BitmapFrame.Create(new Uri(files[i], UriKind.Absolute), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            //}
            //var rect = new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight);
            //for (int i = 0; i < sources.Length; i++)
            //{
            //    await Task.Delay(33);
            //    wb.Lock();
            //    sources[i].CopyPixels(rect, wb.BackBuffer, (int)(wb.BackBufferStride * wb.Height), wb.BackBufferStride);
            //    wb.AddDirtyRect(rect);
            //    wb.Unlock();
            //    if (i == sources.Length - 1)
            //        i = 0;
            //}
        }
    }
}
