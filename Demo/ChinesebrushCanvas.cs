using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    public class ChinesebrushCanvas : InkCanvas
    {
        private ImageSource imageSource;
        //原图分辨率为90x90
        private readonly int imageSize = 90;

        public ChinesebrushCanvas()
        {
            this.EditingMode = InkCanvasEditingMode.Ink;
            this.DynamicRenderer = new ChinesebrushRenderer();
            CreateBrush();
            this.DefaultDrawingAttributes.AttributeChanged += DefaultDrawingAttributes_AttributeChanged;
        }

        private void DefaultDrawingAttributes_AttributeChanged(object sender, PropertyDataChangedEventArgs e)
        {
            if (e.NewValue is Color)
            {
                CreateBrush();
            }
        }

        protected override void OnDefaultDrawingAttributesReplaced(DrawingAttributesReplacedEventArgs e)
        {
            CreateBrush();
            e.NewDrawingAttributes.AttributeChanged += DefaultDrawingAttributes_AttributeChanged;
            e.PreviousDrawingAttributes.AttributeChanged -= DefaultDrawingAttributes_AttributeChanged;
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            this.Strokes.Remove(e.Stroke);
            this.Strokes.Add(new ChinesebrushStroke(e.Stroke.StylusPoints, imageSource));
        }

        private void CreateBrush()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            var dv = new DrawingVisual();
            using (var conext = dv.RenderOpen())
            {
                conext.PushOpacityMask(new ImageBrush(new BitmapImage(new Uri("pen.png", UriKind.Relative))));
                conext.PushOpacity(0.7);
                conext.DrawRectangle(new SolidColorBrush(this.DefaultDrawingAttributes.Color), null, new Rect(0, 0, imageSize, imageSize));
                conext.Close();
            }
            var rtb = new RenderTargetBitmap(imageSize, imageSize, 96d, 96d, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            dv = null;
            imageSource = rtb;
            if (this.DynamicRenderer is ChinesebrushRenderer renderer)
            {
                renderer.ImageSource = imageSource;
            }
        }

    }

    public class ChinesebrushRenderer : DynamicRenderer
    {
        private readonly double minWidth = 8;
        private readonly double width = 24;

        public ImageSource ImageSource { get; set; }

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints, Geometry geometry, Brush fillBrush)
        {
            var p1 = stylusPoints[0].ToPoint();
            for (int i = 1; i < stylusPoints.Count; i++)
            {
                var p2 = stylusPoints[i].ToPoint();

                var vector = p1 - p2;

                var w = this.width - vector.Length;

                if (w < this.minWidth)
                    w = this.minWidth;

                var dx = (p2.X - p1.X) / vector.Length;
                var dy = (p2.Y - p1.Y) / vector.Length;

                for (int j = 0; j < vector.Length; j += 1)
                {
                    var x = p1.X + dx;
                    var y = p1.Y + dy;
                    drawingContext.DrawImage(this.ImageSource, new Rect(x - w / 2.0, y - w / 2.0, w, w));
                    p1 = new Point(x, y);
                }
            }
            stylusPoints = null;
            geometry = null;
            fillBrush = null;
        }
    }

    public class ChinesebrushStroke : Stroke
    {
        private readonly ImageSource imageSource;
        private readonly double minWidth = 8;
        private readonly double width = 24;
        //笔画粗细的最大变化值
        private readonly double maxChange = 2;

        public ChinesebrushStroke(StylusPointCollection stylusPointCollection, ImageSource source) : base(stylusPointCollection)
        {
            imageSource = source;
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            #region 把所有墨迹转化成图片再画，不然会造成CPU占用过高，导致卡顿，似乎是DrawingContext的bug？
            var dv = new DrawingVisual();
            using (var context = dv.RenderOpen())
            {
                var p1 = StylusPoints[0].ToPoint();
                double change = double.PositiveInfinity;
                for (int i = 1; i < StylusPoints.Count; i++)
                {
                    var p2 = StylusPoints[i].ToPoint();
                    var vector = p1 - p2;
                    var dx = (p2.X - p1.X) / vector.Length;
                    var dy = (p2.Y - p1.Y) / vector.Length;
                    if (!double.IsPositiveInfinity(change))
                    {
                        //变化太大
                        if (change - vector.Length > maxChange)
                        {
                            change -= maxChange;
                        }
                        else if (vector.Length - change > maxChange)
                        {
                            change += maxChange;
                        }
                    }
                    else
                    {
                        change = vector.Length;
                    }

                    var w = this.width - change;

                    if (w < this.minWidth)
                        w = this.minWidth;

                    for (int j = 0; j < vector.Length; j += 1)
                    {
                        var x = p1.X + dx;
                        var y = p1.Y + dy;
                        context.DrawImage(imageSource, new Rect(x - w / 2.0, y - w / 2.0, w, w));
                        p1 = new Point(x, y);
                    }
                }
                context.Close();
            }
            var rect = dv.DescendantBounds;
            if (Rect.Empty == rect)
                return;
            dv.Offset = new Vector(0 - rect.X, 0 - rect.Y);
            var rtb = new RenderTargetBitmap((int)Math.Round(rect.Width, MidpointRounding.AwayFromZero), 
                                                                    (int)Math.Round(rect.Height, MidpointRounding.AwayFromZero), 
                                                                    96d, 96d, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            #endregion
            drawingContext.DrawImage(rtb, rect);
            drawingContext.Close();
            dv = null;
            rtb = null;
             
        }

    }


}
