using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Quasar.Client.Helper
{
    public static class ScreenHelper
    {
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

        public static Bitmap CaptureScreen(int screenNumber)
        {
            var bounds = Screen.AllScreens[screenNumber].Bounds;
            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = IntPtr.Zero;
                try
                {
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                    var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
                    if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                    {
                        hdc = graphics.GetHdc();
                        DrawIcon(hdc, cursorInfo.ScreenPosition.X - bounds.X, cursorInfo.ScreenPosition.Y - bounds.Y, cursorInfo.hCursor);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing screen: {ex.Message}");
                    return new Bitmap(bounds.Width, bounds.Height);
                }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        graphics.ReleaseHdc(hdc);
                    }
                }
            }

            return bitmap;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            return Screen.AllScreens[screenNumber].Bounds;
        }
    }
}
