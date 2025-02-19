using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Quasar.Client.Helper
{
    public static class ScreenHelper
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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        public static Bitmap CaptureScreen(int screenNumber)
        {
            Rectangle bounds = GetBounds(screenNumber);
            Bitmap screen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);

            using (Graphics g = Graphics.FromImage(screen))
            {
                IntPtr destDeviceContext = g.GetHdc();
                IntPtr srcDeviceContext = CreateDC("DISPLAY", null, null, IntPtr.Zero);

                BitBlt(destDeviceContext, 0, 0, bounds.Width, bounds.Height, srcDeviceContext, bounds.X, bounds.Y, SRCCOPY);

                var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
                if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                {
                    DrawIcon(destDeviceContext, cursorInfo.ScreenPosition.X - bounds.X, cursorInfo.ScreenPosition.Y - bounds.Y, cursorInfo.hCursor);
                }

                DeleteDC(srcDeviceContext);
                g.ReleaseHdc(destDeviceContext);
            }

            return screen;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            return Screen.AllScreens[screenNumber].Bounds;
        }
    }
}


//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Windows.Forms;
//using System.Runtime.InteropServices;

//namespace Quasar.Client.Helper
//{
//    public static class ScreenHelper
//    {
//        private const int SRCCOPY = 0x00CC0020;
//        private const int CURSOR_SHOWING = 0x00000001;

//        [StructLayout(LayoutKind.Sequential)]
//        public struct POINT
//        {
//            public int X;
//            public int Y;
//        }

//        [StructLayout(LayoutKind.Sequential)]
//        public struct CURSORINFO
//        {
//            public int cbSize;
//            public int flags;
//            public IntPtr hCursor;
//            public POINT ScreenPosition;
//        }

//        [DllImport("user32.dll")]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool GetCursorInfo(out CURSORINFO pci);

//        [DllImport("user32.dll")]
//        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

//        [DllImport("gdi32.dll")]
//        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

//        [DllImport("gdi32.dll")]
//        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

//        [DllImport("gdi32.dll")]
//        private static extern bool DeleteDC(IntPtr hdc);

//        public static Bitmap CaptureScreen(int screenNumber)
//        {
//            // Validate screenNumber
//            if (screenNumber < 0 || screenNumber >= Screen.AllScreens.Length)
//                throw new ArgumentOutOfRangeException(nameof(screenNumber), "Invalid screen number.");

//            Rectangle bounds = GetBounds(screenNumber);
//            Bitmap screenBitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);

//            Graphics g = null;
//            IntPtr destDC = IntPtr.Zero;
//            IntPtr srcDC = IntPtr.Zero;

//            try
//            {
//                g = Graphics.FromImage(screenBitmap);
//                destDC = g.GetHdc();
//                srcDC = CreateDC("DISPLAY", null, null, IntPtr.Zero);

//                if (srcDC == IntPtr.Zero)
//                    throw new InvalidOperationException("Failed to create source device context.");

//                // Copy the screen to the bitmap
//                if (!BitBlt(destDC, 0, 0, bounds.Width, bounds.Height, srcDC, bounds.X, bounds.Y, SRCCOPY))
//                    throw new InvalidOperationException("BitBlt failed.");

//                // Capture cursor if present
//                var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
//                if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
//                {
//                    DrawIcon(destDC, cursorInfo.ScreenPosition.X - bounds.X, cursorInfo.ScreenPosition.Y - bounds.Y, cursorInfo.hCursor);
//                }
//            }
//            finally
//            {
//                // Ensure native resources are cleaned up
//                if (srcDC != IntPtr.Zero)
//                    DeleteDC(srcDC);
//                if (destDC != IntPtr.Zero)
//                    g?.ReleaseHdc(destDC);

//                g?.Dispose();
//            }

//            return screenBitmap;
//        }

//        public static Rectangle GetBounds(int screenNumber)
//        {
//            return Screen.AllScreens[screenNumber].Bounds;
//        }
//    }
//}
