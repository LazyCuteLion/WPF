using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public static class VisualTreeHelperExt
    {
        public static T FindParent<T>(this DependencyObject element) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(element);
            if (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                else
                {
                    return parent.FindParent<T>();
                }
            }
            return null;
        }

        public static T FindParent<T>(this DependencyObject element, string name) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(element);

            if (parent != null)
            {
                if (parent is T result && result.Name == name)
                {
                    return result;
                }
                else
                {
                    return parent.FindParent<T>(name);
                }
            }
            return null;
        }

        public static T FindChild<T>(this DependencyObject element) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                   var child2 = child.FindChild<T>();
                    if (child2 != null)
                        return child2;
                }
            }
            return null;
        }

        public static T FindChild<T>(this DependencyObject element, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if (child is T result && result.Name == name)
                {
                    return result;
                }
                else
                {
                    var child2 = child.FindChild<T>(name);
                    if (child2 != null)
                        return child2;
                }
            }
            return null;
        }
    }
}
