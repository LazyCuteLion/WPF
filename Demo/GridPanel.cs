using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace System.Windows.Controls
{
    public class GridPanel : Panel
    {
        public int Rows { get; set; } = 1;

        public int Columns { get; set; } = 1;

        private int pageCount = -1;

        public int PageCount
        {
            get
            {
                if (pageCount > 0)
                    return pageCount;
                var pageSize = Rows * Columns;
                pageCount = InternalChildren.Count / pageSize;
                if (InternalChildren.Count % pageSize != 0)
                    pageCount++;
                return pageCount;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (InternalChildren.Count < 1)
                return new Size();
            //子元素尺寸
            foreach (UIElement item in InternalChildren)
            {
                item.Measure(availableSize);
            }

            var w = InternalChildren[0].DesiredSize.Width;
            var h = InternalChildren[0].DesiredSize.Height;
            return new Size(w * Columns * PageCount, h * Rows);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //容器布局
            if (InternalChildren.Count < 1)
                return finalSize;
            //每个元素的大小一样，取第一个
            var w = InternalChildren[0].DesiredSize.Width;
            var h = InternalChildren[0].DesiredSize.Height;
            var size = new Size(w * Columns * PageCount, h * Rows);
            var n = 0;
            for (int page = 0; page < PageCount; page++)//页
            {
                for (int row = 0; row < Rows; row++)//行
                {
                    for (int column = 0; column < Columns; column++)//列
                    {
                        var rect = new Rect(page * Columns * w + column * w, row * h, w, h);
                        InternalChildren[n].Arrange(rect);
                        n++;
                        if (n >= InternalChildren.Count)
                            return size;
                    }
                }
            }
            return size;
        }

    }
}
