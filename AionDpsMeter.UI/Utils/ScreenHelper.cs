using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace AionDpsMeter.UI.Utils
{
    internal static class ScreenHelper
    {
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

       
        public static Rect GetWorkingAreaForPoint(double x, double y)
        {
            var pt = new POINT { X = (int)x, Y = (int)y };
            var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return GetWorkArea(hMonitor, 96.0, 96.0);
        }

        public static Rect GetWorkingAreaForWindow(Window window)
        {
            GetDpi(window, out double dpiX, out double dpiY);

            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            IntPtr hMonitor;
            if (helper.Handle != IntPtr.Zero)
            {
                hMonitor = MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
            }
            else
            {
                var pt = new POINT
                {
                    X = (int)(window.Left * dpiX / 96.0),
                    Y = (int)(window.Top  * dpiY / 96.0)
                };
                hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            }

            return GetWorkArea(hMonitor, dpiX, dpiY);
        }

        private static Rect GetWorkArea(IntPtr hMonitor, double dpiX, double dpiY)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var wa = mi.rcWork;
                return new Rect(
                    wa.Left   * 96.0 / dpiX,
                    wa.Top    * 96.0 / dpiY,
                    (wa.Right  - wa.Left) * 96.0 / dpiX,
                    (wa.Bottom - wa.Top)  * 96.0 / dpiY);
            }
            return new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
        }

        private static void GetDpi(Visual visual, out double dpiX, out double dpiY)
        {
            try
            {
                var src = PresentationSource.FromVisual(visual);
                if (src?.CompositionTarget != null)
                {
                    dpiX = 96.0 * src.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * src.CompositionTarget.TransformToDevice.M22;
                    return;
                }
            }
            catch { }
            dpiX = 96.0;
            dpiY = 96.0;
        }
    }
}
