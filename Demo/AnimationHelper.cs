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
using System.Windows.Interactivity;
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
            {
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
        }

        public static PropertyPath ToTransformPropertyPath(this DependencyProperty property, int index = -1)
        {
            if (property.OwnerType.BaseType != typeof(Transform))
                throw new Exception("[property]基类型必须是[Transform]！");

            if (index > -1 && index < 4)
                return new PropertyPath($"(0).(1)[{index}].(2)", UIElement.RenderTransformProperty, TransformGroup.ChildrenProperty, property);
            //return new PropertyPath($"(UIElement.RenderTransform).(TransformGroup.Children)[{index}].({property.OwnerType.Name}.{property.Name})");
            else
                return new PropertyPath("(0).(1)", UIElement.RenderTransformProperty, property);
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
        public static void Begin(this Timeline animation)
        {
            if (animation is Storyboard s)
            {
                s.Begin();
            }
            else
            {
                s = new Storyboard();
                s.Children.Add(animation);
                s.Begin();
            }
        }

        public static Task PlayAsync(this Timeline animation)
        {
            if (animation is Storyboard s)
            {
                s.Begin();
            }
            else
            {
                s = new Storyboard();
                s.Children.Add(animation);
                s.Begin();
            }
            return Task.Delay(animation.Duration.TimeSpan);
        }

        public static Timeline CreateDoubleAnimation(this UIElement element, Duration duration, double from, double to, PropertyPath propertyPath, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation(to, duration)
            {
                EasingFunction = easingFunction
            };

            if (!double.IsNaN(from))
                animation.From = from;

            animation.SetTarget(element).SetTargetProperty(propertyPath);

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
        public static Timeline FadeIn(this UIElement element, int milliseconds)
        {
            return element.Fade(TimeSpan.FromMilliseconds(milliseconds), 1);
        }

        public static Timeline FadeOut(this UIElement element, int milliseconds)
        {
            return element.Fade(TimeSpan.FromMilliseconds(milliseconds), 0);
        }

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

        public static Storyboard Scale(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var s = new Storyboard();
            s.Children.Add(
                element.ScaleX(duration, to, easingFunction),
                element.ScaleY(duration, to, easingFunction));
            return s;
        }

        public static Storyboard Scale(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var s = new Storyboard();
            s.Children.Add(element.ScaleX(duration, from, to, easingFunction));
            s.Children.Add(element.ScaleY(duration, from, to, easingFunction));
            return s;
        }

        public static Timeline ScaleX(this UIElement element, Duration duration, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleXProperty);
            return element.CreateDoubleAnimation(duration, double.NaN, to, p, easingFunction);
        }

        public static Timeline ScaleX(this UIElement element, Duration duration, double from, double to, IEasingFunction easingFunction = null)
        {
            var p = element.GetTransformPropertyPath(ScaleTransform.ScaleXProperty);
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

}
