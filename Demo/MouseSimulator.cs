using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace System.Windows
{

    /// <summary>
    /// Mouse buttons that can be pressed
    /// </summary>
    public enum MouseButton
    {
        Left = 0x2,
        Right = 0x8,
        Middle = 0x20
    }

    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>
    /// Operations that simulate mouse events
    /// </summary>
    public static class MouseSimulator
    {

        #region Windows API Code


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        static extern void mouse_event(int flags, int dX, int dY, int buttons, int extraInfo);

        const int MOUSEEVENTF_MOVE = 0x1;
        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVENTF_LEFTUP = 0x4;
        const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        const int MOUSEEVENTF_MIDDLEUP = 0x40;
        const int MOUSEEVENTF_WHEEL = 0x800;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        #endregion

        #region Methods

        public static Point MousePosition()
        {
            POINT p;
            GetCursorPos(out p);
            return new Point(p.X, p.Y);
        }

        public static void MouseMove(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Press a mouse button down
        /// </summary>
        /// <param name="button"></param>
        public static void MouseDown(MouseButton button)
        {
            mouse_event(((int)button), 0, 0, 0, 0);
        }


        /// <summary>
        /// Let a mouse button up
        /// </summary>
        /// <param name="button"></param>
        public static void MouseUp(MouseButton button)
        {
            mouse_event(((int)button) * 2, 0, 0, 0, 0);
        }

        /// <summary>
        /// Click a mouse button (down then up)
        /// </summary>
        /// <param name="button"></param>
        public static void Click(MouseButton button)
        {
            MouseDown(button);
            MouseUp(button);
        }

        /// <summary>
        /// Double click a mouse button (down then up twice)
        /// </summary>
        /// <param name="button"></param>
        public static void DoubleClick(MouseButton button)
        {
            Click(button);
            Click(button);
        }

        /// <summary>
        /// Roll the mouse wheel. Delta of 120 wheels up once normally, -120 wheels down once normally
        /// </summary>
        /// <param name="delta"></param>
        public static void MouseWheel(int delta)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, 0);
        }


        #endregion

    }

}
