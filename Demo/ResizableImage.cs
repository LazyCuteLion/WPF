using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public class ResizableImage : Image
    {

        public Thickness CapInsets
        {
            get { return (Thickness)GetValue(CapInsetsProperty); }
            set { SetValue(CapInsetsProperty, value); }
        }

        public static readonly DependencyProperty CapInsetsProperty =
            DependencyProperty.Register("CapInsets", typeof(Thickness), typeof(ResizableImage),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));


        protected override void OnRender(DrawingContext dc)
        {
            DrawImage(dc, new Rect(0, 0, this.Width, this.Height));
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (!this.CapInsets.Equals(new Thickness()))
            {
                return arrangeSize;
            }
            else
            {
                return base.ArrangeOverride(arrangeSize);
            }
        }

        private void DrawImage(DrawingContext dc, Rect rect)
        {
            if (this.Source != null)
            {
                if (!this.CapInsets.Equals(new Thickness()))
                {
                    var width = this.Source.Width;
                    var height = this.Source.Height;

                    Thickness margin = Clamp(this.CapInsets, new Size(width, height), rect.Size);

                    double[] xGuidelines = { 0, margin.Left, rect.Width - margin.Right, rect.Width };
                    double[] yGuidelines = { 0, margin.Top, rect.Height - margin.Bottom, rect.Height };
                    GuidelineSet guidelineSet = new GuidelineSet(xGuidelines, yGuidelines);
                    guidelineSet.Freeze();
                    dc.PushGuidelineSet(guidelineSet);

                    double[] vx = { 0D, margin.Left / width, (width - margin.Right) / width, 1D };
                    double[] vy = { 0D, margin.Top / height, (height - margin.Bottom) / height, 1D };
                    double[] x = { rect.Left, rect.Left + margin.Left, rect.Right - margin.Right, rect.Right };
                    double[] y = { rect.Top, rect.Top + margin.Top, rect.Bottom - margin.Bottom, rect.Bottom };

                    for (int i = 0; i < 3; ++i)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            var brush = new ImageBrush(this.Source)
                            {
                                //Opacity = this.Opacity,
                                Viewbox = new Rect(vx[j], vy[i], Math.Max(0D, (vx[j + 1] - vx[j])), Math.Max(0D, (vy[i + 1] - vy[i])))
                            };
                            brush.Freeze();

                            dc.DrawRectangle(brush, null,
                                new Rect(x[j], y[i], Math.Max(0D, (x[j + 1] - x[j])), Math.Max(0D, (y[i + 1] - y[i]))));

                            //dc.DrawImage(this.Source, new Rect(x[j], y[i], Math.Max(0D, (x[j + 1] - x[j])), Math.Max(0D, (y[i + 1] - y[i]))));
                        }
                    }

                    dc.Pop();
                }
                else
                {
                    base.OnRender(dc);
                }
            }
        }

        private Thickness Clamp(Thickness margin, Size firstMax, Size secondMax)
        {
            double left = Clamp(margin.Left, firstMax.Width, secondMax.Width);
            double top = Clamp(margin.Top, firstMax.Height, secondMax.Height);
            double right = Clamp(margin.Right, firstMax.Width - left, secondMax.Width - left);
            double bottom = Clamp(margin.Bottom, firstMax.Height - top, secondMax.Height - top);

            return new Thickness(left, top, right, bottom);
        }

        private double Clamp(double value, double firstMax, double secondMax)
        {
            return Math.Max(0, Math.Min(Math.Min(value, firstMax), secondMax));
        }
    }
}
