using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class CollectionExtension
    {
        /// <summary>
        /// 分成若干行，返回新的List
        /// 从左到右，从上往下
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static List<T> SplitRow<T>(this List<T> s, int row)
        {
            if (s.Count < row)
                return s;
            var n = s.Count / row;
            if (s.Count % row != 0)
                n++;
            var list = new List<T>();
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < row; i++)
                {
                    var index = i * row + j;
                    if (index < s.Count)
                        list.Add(s[index]);
                }
            }
            return list;
        }

        /// <summary>
        /// 分成若干行，返回新的数组
        /// 从左到右，从上往下
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static T[] SplitRow<T>(this T[] s, int row)
        {
            if (s.Length < row)
                return s;
            var n = s.Length / row;
            if (s.Length % row != 0)
                n++;
            var list = new List<T>();
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < row; i++)
                {
                    var index = i * row + j;
                    if (index < s.Length)
                        list.Add(s[index]);
                }
            }
            return list.ToArray();
        }
    }
}
