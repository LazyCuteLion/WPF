﻿using System;
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

namespace Demo
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class Page1 : Page
    {
        public Page1()
        {
            InitializeComponent();
        }

        private void ChinesebrushCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            var canvas = sender as ChinesebrushCanvas;
            switch (e.Key)
            {
                case Key.Delete:
                case Key.Back:
                    canvas.Strokes.Clear();
                    break;
            }
        }
    }
}
