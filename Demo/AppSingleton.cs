using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace System
{
    public class AppSingleton
    {
        static Mutex app;

        [DllImport("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        public static bool IsRunning(string id="")
        {
            var current = Process.GetCurrentProcess();
            if (string.IsNullOrEmpty(id))
                id=current.ProcessName;
            app = new Mutex(true, id, out bool isCreate);
            if (!isCreate)
            {
                var ps = Process.GetProcessesByName(current.ProcessName);
                foreach (var item in ps)
                {
                    if (item.Id != current.Id)
                    {
                        ShowWindowAsync(item.MainWindowHandle, 1);
                        SetForegroundWindow(item.MainWindowHandle);
                        return true;
                    }
                }
            }
            current.Dispose();
            return false;
        }

        public static void LogError()
        {
            //线程错误
            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                LogError(ex.Exception.InnerException.ToString());
                ex.SetObserved();
            };

            //程序内非UI线程
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                LogError(ex.ExceptionObject.ToString());
            };

            //UI线程错误
            Application.Current.DispatcherUnhandledException += (s, ex) =>
            {
                LogError(ex.Exception.ToString());
                ex.Handled = true;
            };

        }

        public static void LogError(string error)
        {
            var lines = new string[]
            {
                DateTime.Now.ToString("------------------------------------------yyyy-MM-dd hh:mm:ss------------------------------------------"),
                error,
                "-----------------------------------------------------------------------------------------------------------",
                Environment.NewLine
            };
            File.AppendAllLines("app.error", lines);
        }
    }
}
